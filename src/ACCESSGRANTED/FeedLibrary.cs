using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using DrawingGraphics = System.Drawing.Graphics;

namespace ACCESSGRANTED
{
    /// <summary>
    /// Footage for Security's camera wall: the player's own game screenshots, shrunk to small
    /// grey frames. A full-size screenshot takes a good fraction of a second to decode, so it is
    /// done on a worker thread (System.Drawing, which the game ships) and the result is cached
    /// under BepInEx\cache, so each screenshot is only ever decoded once. Only the step that
    /// turns the pixels into a texture runs on the main thread, and it is tiny.
    /// </summary>
    internal static class FeedLibrary
    {
        internal const int Count = 4;
        private const int Width = 240;
        private const int Height = 180;

        private static readonly object Gate = new object();
        private static readonly byte[][] Frames = new byte[Count][];
        private static readonly Texture2D[] Textures = new Texture2D[Count];
        private static bool _started;
        private static volatile bool _finished;

        /// <summary>True once the worker has done all it will: feeds still missing then never come.</summary>
        internal static bool Finished => _finished;

        private static string CacheDirectory => Path.Combine(BepInEx.Paths.CachePath, "ACCESSGRANTED");

        /// <summary>Starts loading footage, once per game session. Cheap to call again.</summary>
        internal static void Request()
        {
            if (_started)
            {
                return;
            }
            _started = true;
            if (!AccessGrantedPlugin.CctvScreenshots.Value)
            {
                _finished = true;
                return;
            }
            string[] folders =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Escape from Tarkov", "Screenshots"),
                Path.Combine(BepInEx.Paths.GameRootPath, "Screenshots")
            };
            // Low priority, so decoding never competes with the game's own loading.
            var worker = new Thread(() => Work(folders))
            {
                IsBackground = true,
                Priority = System.Threading.ThreadPriority.BelowNormal,
                Name = "ACCESSGRANTED CCTV"
            };
            worker.Start();
        }

        /// <summary>The frame for camera <paramref name="slot"/>, made into a texture on first use; null if there is none (yet).</summary>
        internal static Texture2D Texture(int slot)
        {
            if (Textures[slot] != null)
            {
                return Textures[slot];
            }
            byte[] grey;
            lock (Gate)
            {
                grey = Frames[slot];
            }
            if (grey == null)
            {
                return null;
            }
            var pixels = new Color32[Width * Height];
            for (int i = 0; i < pixels.Length; i++)
            {
                byte g = grey[i];
                pixels[i] = new Color32(g, g, g, 255);
            }
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                // EFT unloads unused assets between scenes; this is only held by a static field.
                hideFlags = HideFlags.DontUnloadUnusedAsset
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Textures[slot] = texture;
            return texture;
        }

        private static void Work(string[] folders)
        {
            try
            {
                var files = new List<FileInfo>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string folder in folders)
                {
                    if (!Directory.Exists(folder))
                    {
                        continue;
                    }
                    foreach (string path in Directory.GetFiles(folder))
                    {
                        string extension = Path.GetExtension(path);
                        bool image = extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
                        if (image && seen.Add(Path.GetFullPath(path)))
                        {
                            files.Add(new FileInfo(path));
                        }
                    }
                }

                var random = new System.Random();
                for (int i = files.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    (files[i], files[j]) = (files[j], files[i]);
                }

                Directory.CreateDirectory(CacheDirectory);
                int slot = 0;
                foreach (FileInfo file in files)
                {
                    if (slot >= Count)
                    {
                        break;
                    }
                    try
                    {
                        byte[] grey = Load(file);
                        lock (Gate)
                        {
                            Frames[slot] = grey;
                        }
                        slot++;
                    }
                    catch (Exception e)
                    {
                        AccessGrantedPlugin.Log.LogWarning($"CCTV: could not use {file.Name}: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                AccessGrantedPlugin.Log.LogWarning($"CCTV: no screenshot footage: {e.Message}");
            }
            finally
            {
                _finished = true;
            }
        }

        /// <summary>The cached frame if there is one for this exact file, else a fresh decode, cached.</summary>
        private static byte[] Load(FileInfo file)
        {
            string key = $"{Path.GetFileNameWithoutExtension(file.Name)}_{file.Length}_{file.LastWriteTimeUtc.Ticks}.grey";
            foreach (char bad in Path.GetInvalidFileNameChars())
            {
                key = key.Replace(bad, '_');
            }
            string cached = Path.Combine(CacheDirectory, key);
            if (File.Exists(cached))
            {
                byte[] bytes = File.ReadAllBytes(cached);
                if (bytes.Length == Width * Height)
                {
                    return bytes;
                }
            }
            byte[] grey = Decode(file.FullName);
            File.WriteAllBytes(cached, grey);
            return grey;
        }

        /// <summary>
        /// The middle of the screenshot, cropped to 4:3, shrunk to a camera frame and turned to
        /// grey with a little extra contrast. Rows bottom first, the way Unity's textures run.
        /// </summary>
        private static byte[] Decode(string path)
        {
            using (var source = new Bitmap(path))
            using (var frame = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
            {
                using (DrawingGraphics graphics = DrawingGraphics.FromImage(frame))
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                    float aspect = Width / (float)Height;
                    int sw = source.Width;
                    int sh = source.Height;
                    Rectangle crop = sw / (float)sh > aspect
                        ? new Rectangle((sw - (int)(sh * aspect)) / 2, 0, (int)(sh * aspect), sh)
                        : new Rectangle(0, (sh - (int)(sw / aspect)) / 2, sw, (int)(sw / aspect));
                    graphics.DrawImage(source, new Rectangle(0, 0, Width, Height), crop, GraphicsUnit.Pixel);
                }

                BitmapData data = frame.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                var bytes = new byte[data.Stride * Height];
                try
                {
                    Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
                }
                finally
                {
                    frame.UnlockBits(data);
                }

                var grey = new byte[Width * Height];
                for (int y = 0; y < Height; y++)
                {
                    int row = y * data.Stride;
                    int target = (Height - 1 - y) * Width;
                    for (int x = 0; x < Width; x++)
                    {
                        int i = row + x * 4;
                        float l = 0.11f * bytes[i] + 0.59f * bytes[i + 1] + 0.30f * bytes[i + 2];
                        grey[target + x] = (byte)Mathf.Clamp((l - 128f) * 1.3f + 128f, 0f, 255f);
                    }
                }
                return grey;
            }
        }
    }
}
