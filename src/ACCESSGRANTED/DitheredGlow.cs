using System;
using System.Threading;
using UnityEngine;

namespace ACCESSGRANTED
{
    /// <summary>
    /// A radial glow drawn one texel per screen pixel with the dither built in.
    ///
    /// A wide, dark gradient on an 8-bit screen shows rings: each draw is rounded to whole steps
    /// of brightness as it is blended, and a glow across the whole screen changes by less than a
    /// step over dozens of pixels. Noise added afterwards (a grain layer) cannot help, because the
    /// glow has already been rounded by then. The noise has to be in the glow itself, so the
    /// rounding happens on a value that varies pixel to pixel: then neighbouring pixels round
    /// different ways and the eye sees the smooth average. That needs a texture as large as the
    /// glow is on screen, made once per layout on a worker thread (a few million pixels) and
    /// swapped in when ready; until then the stretched Art.Radial stands in.
    ///
    /// The noise is triangular, in steps of the texture's alpha, sized so that it moves the
    /// final colour by about one step: a glow of colour C over a background B at strength A
    /// changes the screen by A * (C - B) per alpha step, so a faint or low-contrast glow needs
    /// more alpha steps of noise than a bright one.
    /// </summary>
    internal sealed class DitheredGlow
    {
        private readonly object _lock = new object();
        private Color32[] _ready;
        private int _readyWidth;
        private int _readyHeight;
        private int _wanted;
        private int _readyFor;
        private Texture2D _texture;
        private string _key;
        private string _readyKey;
        private string _textureKey;

        /// <summary>The finished texture, or null while none is ready for the current request.</summary>
        internal Texture2D Texture => _texture;

        /// <summary>
        /// Asks for a glow width x height pixels whose ellipse is centred at (cx, cy) from the
        /// bottom-left with radii (rx, ry); strength is the alpha the glow is drawn at and
        /// contrast the largest difference between its colour and what lies under it, 0 to 255.
        /// Returns true if the current texture already matches.
        /// </summary>
        internal bool Request(int width, int height, float cx, float cy, float rx, float ry, float strength, float contrast, int seed)
        {
            float perStep = Mathf.Max(0.001f, strength * contrast / 255f);
            int steps = Mathf.Clamp(Mathf.CeilToInt(1.2f / perStep), 1, 48);
            string key = $"{width}x{height} {cx:0.0},{cy:0.0} {rx:0.0},{ry:0.0} n{steps} s{seed}";
            if (key == _textureKey && _texture != null)
            {
                return true;
            }
            if (key == _key)
            {
                // Already being made.
                return false;
            }
            _key = key;
            int job = Interlocked.Increment(ref _wanted);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Color32[] pixels;
                try
                {
                    pixels = Bake(width, height, cx, cy, rx, ry, steps, seed);
                }
                catch (Exception e)
                {
                    AccessGrantedPlugin.Log.LogWarning("Dithered glow not made: " + e.Message);
                    return;
                }
                lock (_lock)
                {
                    if (job != _wanted)
                    {
                        return;
                    }
                    _ready = pixels;
                    _readyWidth = width;
                    _readyHeight = height;
                    _readyFor = job;
                    _readyKey = key;
                }
            });
            return false;
        }

        /// <summary>On the main thread: turns a finished bake into the texture. True when a new one was made.</summary>
        internal bool Collect()
        {
            Color32[] pixels;
            int width, height;
            lock (_lock)
            {
                if (_ready == null || _readyFor != _wanted)
                {
                    return false;
                }
                pixels = _ready;
                width = _readyWidth;
                height = _readyHeight;
                _textureKey = _readyKey;
                _ready = null;
            }
            if (_texture != null)
            {
                UnityEngine.Object.Destroy(_texture);
            }
            _texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.DontUnloadUnusedAsset
            };
            _texture.SetPixels32(pixels);
            _texture.Apply(false, true);
            return true;
        }

        /// <summary>Forgets any request in flight, so a stale bake is never swapped in.</summary>
        internal void Cancel()
        {
            Interlocked.Increment(ref _wanted);
            _key = null;
            _textureKey = null;
        }

        private static Color32[] Bake(int width, int height, float cx, float cy, float rx, float ry, int steps, int seed)
        {
            var pixels = new Color32[width * height];
            uint state = (uint)seed * 2654435761u + 1u;
            float irx = 1f / rx;
            float iry = 1f / ry;
            for (int y = 0; y < height; y++)
            {
                float dy = (y + 0.5f - cy) * iry;
                float dy2 = dy * dy;
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    float dx = (x + 0.5f - cx) * irx;
                    float d2 = dx * dx + dy2;
                    if (d2 >= 1f)
                    {
                        pixels[row + x] = new Color32(255, 255, 255, 0);
                        continue;
                    }
                    float a = 1f - (float)Math.Sqrt(d2);
                    // xorshift: two draws make a triangular noise of -steps..+steps.
                    state ^= state << 13; state ^= state >> 17; state ^= state << 5;
                    int n1 = (int)(state % (uint)(steps + 1));
                    state ^= state << 13; state ^= state >> 17; state ^= state << 5;
                    int n2 = (int)(state % (uint)(steps + 1));
                    float value = a * a * 255f + (n1 - n2);
                    int alpha = (int)(value + 0.5f);
                    pixels[row + x] = new Color32(255, 255, 255, (byte)(alpha < 0 ? 0 : alpha > 255 ? 255 : alpha));
                }
            }
            return pixels;
        }
    }
}
