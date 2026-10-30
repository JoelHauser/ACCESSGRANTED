using UnityEngine;

namespace StartupProgress
{
    /// <summary>
    /// The few textures the overlay needs, drawn once in code so the mod ships as a single DLL.
    /// All are white with the shape in alpha, and tinted where they are used.
    /// </summary>
    internal static class Art
    {
        private static Texture2D _radial;
        private static Texture2D _grid;
        private static Texture2D _scanlines;
        private static Texture2D _band;

        /// <summary>
        /// Opaque in the middle, fading to nothing at the edge of the circle. Big and dithered,
        /// because a dark gradient stretched across the screen otherwise shows bands.
        /// </summary>
        internal static Texture2D Radial => _radial ?? (_radial = Make(256, 256, TextureWrapMode.Clamp, FilterMode.Bilinear, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(128f, 128f)) / 128f;
            float a = Mathf.Clamp01(1f - d);
            return a * a + Dither(x, y);
        }));

        /// <summary>
        /// One 64-pixel grid cell: a one-pixel line along the bottom and left, brighter where they
        /// meet. Point-filtered and drawn one texel per screen pixel, so the lines stay one pixel.
        /// </summary>
        internal static Texture2D Grid => _grid ?? (_grid = Make(64, 64, TextureWrapMode.Repeat, FilterMode.Point, (x, y) =>
            x < 2 && y < 2 ? 1f : (x == 0 || y == 0 ? 0.45f : 0f)));

        /// <summary>One dark line in every three pixels.</summary>
        internal static Texture2D Scanlines => _scanlines ?? (_scanlines = Make(1, 3, TextureWrapMode.Repeat, FilterMode.Point, (x, y) =>
            y == 0 ? 1f : 0f));

        /// <summary>A vertical streak, clear at both ends.</summary>
        internal static Texture2D Band => _band ?? (_band = Make(1, 128, TextureWrapMode.Clamp, FilterMode.Bilinear, (x, y) =>
        {
            float s = Mathf.Sin(Mathf.PI * (y + 0.5f) / 128f);
            return s * s;
        }));

        /// <summary>Under half a step of 8-bit alpha, varying pixel to pixel.</summary>
        private static float Dither(int x, int y)
        {
            float n = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f;
            return (n - Mathf.Floor(n) - 0.5f) / 255f;
        }

        private static Texture2D Make(int width, int height, TextureWrapMode wrap, FilterMode filter, System.Func<int, int, float> alpha)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = wrap,
                filterMode = filter,
                // EFT unloads unused assets between scenes; these are only held by a static field.
                hideFlags = HideFlags.DontUnloadUnusedAsset
            };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha(x, y)) * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
