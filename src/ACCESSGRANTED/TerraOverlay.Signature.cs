using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ACCESSGRANTED
{
    /// <summary>
    /// The department's ambient drawing (TerraOverlay.Ambient.cs; Holiday's is the snow), on a
    /// full-screen layer behind the terminal, rebuilt with the layout. Animation is transforms,
    /// alphas and the odd short text; the drawings that grow are rebuilt only when they move.
    /// </summary>
    internal partial class TerraOverlay
    {
        private RectTransform _ambientRoot;
        private Signature _ambient;

        private void BuildSignatureRoot(Transform c)
        {
            _ambientRoot = MakeRect("ScreenLayer", c);
            Isolate(_ambientRoot);
        }

        private void LayoutSignature()
        {
            ClearAmbient();
            // The ambient drawings keep to the space the layout leaves free of text.
            _ambient = _variant.Theme.Id == ThemeChoice.Holiday ? new Snow()
                : AmbientRoom ? CreateAmbient(_variant.Theme.Id) : null;
            _ambient?.Init(this, _ambientRoot);
        }

        /// <summary>Takes the drawing down: its meshes and images go, and the next layout builds a new one.</summary>
        private void ClearAmbient()
        {
            for (int i = _ambientRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_ambientRoot.GetChild(i).gameObject);
            }
            _ambient = null;
        }

        private void AnimatePanels(float t)
        {
            _ambient?.Animate(t);
        }

        private abstract class Signature
        {
            protected TerraOverlay O;
            protected RectTransform R;
            protected float U;
            protected Theme T;

            internal void Init(TerraOverlay overlay, RectTransform root)
            {
                O = overlay;
                R = root;
                U = overlay._u;
                T = overlay._variant.Theme;
                _rng = Seeded ? new System.Random(overlay._variant.ChartSeed) : null;
                Build();
            }

            protected abstract void Build();
            internal abstract void Animate(float t);

            protected float Progress => O._phase == Phase.Loading ? O._shownFraction : 1f;
            protected float Dt => O._frameDt;
            protected System.Random Rng => _rng ?? O._random;

            /// <summary>
            /// Draws from a generator seeded per launch, so a rebuild (the terminal lays itself out again when
            /// the game's font replaces the boot font) draws the same picture instead of a new one.
            /// </summary>
            protected virtual bool Seeded => false;

            private System.Random _rng;

            protected Vector2 P(float x, float y) => new Vector2(x * U, y * U);

            protected static Color32 A(Color32 color, byte alpha) => Theme.WithAlpha(color, alpha);

            protected float Range(float min, float max) => min + (float)Rng.NextDouble() * (max - min);

            protected Image Box(float x, float y, float w, float h, Color32 color, Transform parent = null)
            {
                Image image = MakeImage("Box", parent != null ? parent : R, color);
                Place(image.rectTransform, x * U, y * U, w * U, h * U);
                return image;
            }

            protected TextMeshProUGUI Label(string text, float x, float y, float w, float size, Color32 color,
                TextAlignmentOptions align = TextAlignmentOptions.Left, Transform parent = null, float spacing = 6f)
            {
                TextMeshProUGUI label = O.MakeText("Label", parent != null ? parent : R, align, color);
                label.text = text;
                SetText(label, x * U, y * U, w * U, (size + 8f) * U, size * U, spacing);
                return label;
            }

            protected Shape Draw(float x = 0f, float y = 0f, Transform parent = null)
            {
                Shape shape = Shape.Create("Shape", parent != null ? parent : R);
                shape.rectTransform.anchoredPosition = P(x, y);
                return shape;
            }

            protected void Outline(Shape shape, float x, float y, float w, float h, float width, Color32 color)
            {
                float l = x - w * 0.5f, r = x + w * 0.5f, b = y - h * 0.5f, t = y + h * 0.5f;
                float half = width * 0.5f;
                shape.Line(P(l - half, t), P(r + half, t), width * U, color);
                shape.Line(P(l - half, b), P(r + half, b), width * U, color);
                shape.Line(P(l, t), P(l, b), width * U, color);
                shape.Line(P(r, t), P(r, b), width * U, color);
            }
        }

        /// <summary>Holiday: snow falling across the whole screen, behind everything else.</summary>
        private sealed class Snow : Signature
        {
            private RawImage[] _flakes;
            private Vector2[] _at;
            private float[] _speed;
            private float[] _phase;

            protected override void Build()
            {
                Vector2 size = O._laidOutSize;
                _flakes = new RawImage[60];
                _at = new Vector2[60];
                _speed = new float[60];
                _phase = new float[60];
                for (int i = 0; i < _flakes.Length; i++)
                {
                    float s = Range(3f, 9f) * U;
                    _flakes[i] = MakeRaw("Flake", R, Art.Radial, A(T.Text, (byte)Rng.Next(90, 200)));
                    _flakes[i].rectTransform.sizeDelta = new Vector2(s, s);
                    _at[i] = new Vector2(Range(-0.5f, 0.5f) * size.x, Range(-0.5f, 0.5f) * size.y);
                    _speed[i] = Range(25f, 70f) * U;
                    _phase[i] = Range(0f, 6.28f);
                }
            }

            internal override void Animate(float t)
            {
                Vector2 size = O._laidOutSize;
                for (int i = 0; i < _flakes.Length; i++)
                {
                    _at[i].y -= _speed[i] * Dt;
                    _at[i].x += Mathf.Sin(t * 0.7f + _phase[i]) * 12f * U * Dt;
                    if (_at[i].y < -size.y * 0.5f - 10f)
                    {
                        _at[i] = new Vector2(Range(-0.5f, 0.5f) * size.x, size.y * 0.5f + 10f);
                    }
                    _flakes[i].rectTransform.anchoredPosition = _at[i];
                }
            }
        }
    }
}
