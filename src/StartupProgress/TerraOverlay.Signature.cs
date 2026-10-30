using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StartupProgress
{
    /// <summary>
    /// The two dashboard panels: the division's signature, one animated piece only that division
    /// has (a CCTV wall for Security, an ECG for Health, a contract signing itself for Legal...),
    /// and a Session Details panel of live facts. Each is built at a nominal 320 x 300 inside a
    /// framed card, placed where the layout puts its panels and scaled to fit, and rebuilt with
    /// the layout. Animation is transforms, alphas and the odd short text; the drawings that
    /// grow (the signature, the route) are rebuilt only when they move.
    /// </summary>
    internal partial class TerraOverlay
    {
        /// <summary>The card around a panel, in nominal units.</summary>
        private const float PanelWidth = 344f;
        private const float PanelHeight = 324f;

        /// <summary>Card slots the layout can offer: four where there is room, else two.</summary>
        private const int MaxCards = 4;

        // Root 0 is a full-screen layer (Holiday's snow); roots 1 to 4 are the card slots.
        private readonly RectTransform[] _panelRoots = new RectTransform[MaxCards + 1];
        private readonly Signature[] _panels = new Signature[MaxCards + 1];
        private readonly Vector2[] _panelSpots = new Vector2[MaxCards];
        private float _panelScale;

        private void BuildSignatureRoot(Transform c)
        {
            for (int i = 0; i <= MaxCards; i++)
            {
                _panelRoots[i] = MakeRect(i == 0 ? "ScreenLayer" : "Card" + i, c);
                Isolate(_panelRoots[i]);
            }
        }

        private void LayoutSignature()
        {
            for (int p = 0; p <= MaxCards; p++)
            {
                RectTransform root = _panelRoots[p];
                for (int i = root.childCount - 1; i >= 0; i--)
                {
                    Destroy(root.GetChild(i).gameObject);
                }
                _panels[p] = null;
            }

            // The terminal is the screen: logo, frame and readout, with at most one quiet detail
            // from the department beside them. (The full-screen scenes and the extra cards are
            // switched off; see TerraOverlay.Scenes*.cs.)
            _sceneActive = false;
            SetActive(_readoutRoot.gameObject, true);
            SetActive(_brand.gameObject, true);
            SetActive(_subtitle.gameObject, true);

            // Behind it, the department's ambient drawing (Holiday's is the snow). The details
            // beside the readout are switched off as clutter; CreateSignature still builds them.
            _panelRoots[0].anchoredPosition = Vector2.zero;
            _panelRoots[0].localScale = Vector3.one;
            _panels[0] = _variant.Theme.Id == ThemeChoice.Holiday ? new Snow() : CreateAmbient(_variant.Theme.Id);
            _panels[0]?.Init(this, _panelRoots[0], _u, false);
        }

        private void AnimatePanels(float t)
        {
            for (int i = 0; i <= MaxCards; i++)
            {
                _panels[i]?.Animate(t);
            }
        }

        private static Signature CreateSignature(ThemeChoice id)
        {
            switch (id)
            {
                case ThemeChoice.Corporate: return new Values();
                case ThemeChoice.Research: return new Vials();
                case ThemeChoice.Security: return new Cctv();
                case ThemeChoice.Finance: return new Market();
                case ThemeChoice.Records: return new Redacted();
                case ThemeChoice.Executive: return new Clocks();
                case ThemeChoice.InternalAffairs: return new Dossier();
                case ThemeChoice.Logistics: return new Route();
                case ThemeChoice.HumanResources: return new Badge();
                case ThemeChoice.ItDepartment: return new Rack();
                case ThemeChoice.Medical: return new Ecg();
                case ThemeChoice.Facilities: return new Elevator();
                case ThemeChoice.Legal: return new Contract();
                case ThemeChoice.PublicRelations: return new Approval();
                case ThemeChoice.Holiday: return new Snow();
                default: return null;
            }
        }

        private abstract class Signature
        {
            protected TerraOverlay O;
            protected RectTransform R;
            protected float U;
            protected Theme T;

            internal void Init(TerraOverlay overlay, RectTransform root, float u, bool framed)
            {
                O = overlay;
                R = root;
                U = u;
                T = overlay._variant.Theme;
                if (framed)
                {
                    Frame();
                }
                Build();
            }

            /// <summary>The card: a tinted panel, a fine border, brighter corner ticks.</summary>
            private void Frame()
            {
                float w = PanelWidth;
                float h = PanelHeight;
                Box(0f, 0f, w, h, A(Color32.Lerp(T.Ink, T.Primary, 0.09f), 215));
                Shape edge = Draw();
                Outline(edge, 0f, 0f, w, h, 1f, A(T.Dim, 60));
                float arm = 14f;
                for (int sx = -1; sx <= 1; sx += 2)
                {
                    for (int sy = -1; sy <= 1; sy += 2)
                    {
                        float x = sx * w * 0.5f;
                        float y = sy * h * 0.5f;
                        edge.Line(P(x + sx, y), P(x - sx * arm, y), 2f * U, A(T.Accent, 200));
                        edge.Line(P(x, y + sy), P(x, y - sy * arm), 2f * U, A(T.Accent, 200));
                    }
                }
                edge.Commit();
            }

            protected abstract void Build();
            internal abstract void Animate(float t);

            protected float Progress => O._phase == Phase.Loading ? O._shownFraction : 1f;
            protected float Dt => O._frameDt;
            protected System.Random Rng => O._random;
            protected Color32 Dark => Color32.Lerp(T.Ink, new Color32(0, 0, 0, 255), 0.45f);

            protected Vector2 P(float x, float y) => new Vector2(x * U, y * U);

            protected static Color32 A(Color32 color, byte alpha) => Theme.WithAlpha(color, alpha);

            protected float Range(float min, float max) => min + (float)Rng.NextDouble() * (max - min);

            protected Image Box(float x, float y, float w, float h, Color32 color, Transform parent = null)
            {
                Image image = MakeImage("Box", parent != null ? parent : R, color);
                Place(image.rectTransform, x * U, y * U, w * U, h * U);
                return image;
            }

            /// <summary>A box growing from its left edge; set its width through sizeDelta.x.</summary>
            protected Image Bar(float left, float y, float h, Color32 color)
            {
                Image image = MakeImage("Bar", R, color);
                RectTransform rect = image.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = P(left, y);
                rect.sizeDelta = new Vector2(0f, Mathf.Round(h * U));
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

        /// <summary>Corporate HQ: the company values, one at a time.</summary>
        private sealed class Values : Signature
        {
            private static readonly string[] Words = { "INTEGRITY", "INNOVATION", "DISCRETION", "SECURITY", "EXCELLENCE", "LOYALTY" };
            private static readonly string[] Lines =
            {
                "WE DO WHAT WE SAY.", "TOMORROW, TODAY.", "WHAT HAPPENS HERE STAYS HERE.",
                "YOUR SAFETY IS OUR PRIORITY.", "NOTHING LESS IS ACCEPTED.", "WE REMEMBER WHO STAYED."
            };
            private TextMeshProUGUI _word;
            private TextMeshProUGUI _line;
            private Vector2 _wordAt;
            private int _shown = -1;

            protected override void Build()
            {
                Label("OUR VALUES", 0f, 62f, 320f, 12f, T.Dim);
                Box(0f, 48f, 320f, 1f, T.Faint);
                _word = Label(string.Empty, 0f, 12f, 320f, 34f, T.Text, spacing: 10f);
                _line = Label(string.Empty, 0f, -26f, 320f, 13f, T.Accent);
                _wordAt = _word.rectTransform.anchoredPosition;
            }

            internal override void Animate(float t)
            {
                const float cycle = 3.2f;
                int index = (int)(t / cycle) % Words.Length;
                float p = t % cycle;
                float alpha = Mathf.Clamp01(p / 0.4f) * Mathf.Clamp01((cycle - p) / 0.4f);
                if (index != _shown)
                {
                    _shown = index;
                    _word.text = Words[index];
                    _line.text = Lines[index];
                }
                _word.canvasRenderer.SetAlpha(alpha);
                _line.canvasRenderer.SetAlpha(alpha);
                _word.rectTransform.anchoredPosition = _wordAt + new Vector2(Mathf.Round((1f - alpha) * 14f * U), 0f);
            }
        }

        /// <summary>Research: a rack of sample tubes, one per load step, filling as the steps finish.</summary>
        private sealed class Vials : Signature
        {
            private readonly Image[] _fills = new Image[StartupTracker.StepCount];
            private readonly float[] _drawn = new float[StartupTracker.StepCount];

            protected override void Build()
            {
                Label("SAMPLE SET 6-HELIX", 0f, 140f, 320f, 12f, T.Dim);
                Shape tubes = Draw();
                for (int i = 0; i < _fills.Length; i++)
                {
                    float x = -120f + (i % 5) * 60f;
                    float y = 85f - (i / 5) * 85f;
                    Outline(tubes, x, y, 22f, 70f, 1.5f, A(T.Accent, 120));
                    tubes.Line(P(x - 15f, y + 35f), P(x + 15f, y + 35f), 3f * U, A(T.Accent, 190));
                    _fills[i] = Bar(0f, 0f, 0f, A(T.Accent, 200));
                    RectTransform rect = _fills[i].rectTransform;
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = P(x, y - 33f);
                    rect.sizeDelta = new Vector2(Mathf.Round(18f * U), 0f);
                    _drawn[i] = -1f;
                }
                tubes.Commit();
            }

            internal override void Animate(float t)
            {
                int current = StartupTracker.Current;
                bool done = O._phase != Phase.Loading;
                for (int i = 0; i < _fills.Length; i++)
                {
                    float f = done || i < current ? (StartupTracker.WasSkipped(i) ? 0.15f : 1f)
                        : i == current ? StartupTracker.CurrentStepFraction() : 0f;
                    float h = Mathf.Round(f * 66f * U);
                    if (h != _drawn[i])
                    {
                        _drawn[i] = h;
                        _fills[i].rectTransform.sizeDelta = new Vector2(_fills[i].rectTransform.sizeDelta.x, h);
                    }
                    _fills[i].canvasRenderer.SetAlpha(!done && i == current ? 0.7f + 0.3f * Mathf.Sin(t * 5f) : 1f);
                }
            }
        }

        /// <summary>
        /// Security: a wall of four camera feeds showing the player's own screenshots as grey,
        /// slowly panning footage (see <see cref="FeedLibrary"/>), timestamps running, one losing
        /// signal now and then. A feed still loading says so; with no screenshots, the camera
        /// shows a drawn corridor with someone walking up it.
        /// </summary>
        private sealed class Cctv : Signature
        {
            private static readonly Vector2[] Tiles = { new Vector2(-80f, 62f), new Vector2(80f, 62f), new Vector2(-80f, -62f), new Vector2(80f, -62f) };
            private readonly TextMeshProUGUI[] _times = new TextMeshProUGUI[4];
            private readonly Image[] _scans = new Image[4];
            private readonly RawImage[] _feeds = new RawImage[4];
            private readonly Shape[] _corridors = new Shape[4];
            private readonly Shape[] _walkers = new Shape[4];
            private readonly TextMeshProUGUI[] _connecting = new TextMeshProUGUI[4];
            private readonly float[] _pan = new float[4];
            private TextMeshProUGUI _noSignal;
            private Image _noSignalCover;
            private Shape _rec;
            private int _dead;
            private int _second = -1;

            protected override void Build()
            {
                FeedLibrary.Request();
                Label("CCTV  //  CHECKPOINT FEEDS", 0f, 140f, 320f, 12f, T.Dim);
                _dead = Rng.Next(4);
                Color32 tint = Color32.Lerp(T.Text, T.Accent, 0.45f);
                for (int k = 0; k < 4; k++)
                {
                    Vector2 c = Tiles[k];
                    Box(c.x, c.y, 150f, 112f, A(Dark, 245));

                    // Everything inside the frame is clipped to it.
                    RectTransform view = MakeRect("View", R);
                    view.anchoredPosition = P(c.x, c.y);
                    view.sizeDelta = P(148f, 110f);
                    view.gameObject.AddComponent<RectMask2D>();

                    _feeds[k] = MakeRaw("Feed", view, null, A(tint, 225));
                    _feeds[k].rectTransform.sizeDelta = P(148f, 110f);
                    _feeds[k].gameObject.SetActive(false);
                    _pan[k] = Range(0f, 6.28f);

                    // The stand-in: a corridor in perspective, and someone walking up it.
                    Shape corridor = _corridors[k] = Draw(0f, 0f, view);
                    float iw = 34f, ih = 24f;
                    corridor.Line(P(-75f, 56f), P(-iw, ih), U, A(T.Dim, 70));
                    corridor.Line(P(75f, 56f), P(iw, ih), U, A(T.Dim, 70));
                    corridor.Line(P(-75f, -56f), P(-iw, -ih), U, A(T.Dim, 70));
                    corridor.Line(P(75f, -56f), P(iw, -ih), U, A(T.Dim, 70));
                    Outline(corridor, 0f, 0f, iw * 2f, ih * 2f, 1f, A(T.Dim, 70));
                    corridor.Commit();
                    Shape walker = _walkers[k] = Draw(0f, 0f, view);
                    walker.Disc(P(0f, 15f), 4.5f * U, A(T.Dim, 210));
                    walker.Polygon(new[] { P(-6f, 9f), P(6f, 9f), P(4f, -8f), P(-4f, -8f) }, A(T.Dim, 210));
                    walker.Line(P(-2.5f, -8f), P(-3.5f, -22f), 2.5f * U, A(T.Dim, 210));
                    walker.Line(P(2.5f, -8f), P(3.5f, -22f), 2.5f * U, A(T.Dim, 210));
                    walker.Commit();

                    _connecting[k] = Label("CONNECTING...", 0f, 0f, 140f, 10f, T.Dim, TextAlignmentOptions.Center, view);
                }

                Shape frames = Draw();
                for (int k = 0; k < 4; k++)
                {
                    Outline(frames, Tiles[k].x, Tiles[k].y, 150f, 112f, 1f, A(T.Accent, 90));
                }
                frames.Commit();
                for (int k = 0; k < 4; k++)
                {
                    Vector2 c = Tiles[k];
                    Label($"CAM 0{k + 1}", c.x, c.y + 44f, 136f, 10f, T.Dim);
                    _times[k] = Label(string.Empty, c.x, c.y - 46f, 136f, 10f, T.Accent, TextAlignmentOptions.Right);
                    _scans[k] = Box(c.x, c.y, 148f, 5f, A(T.Accent, 28));
                }
                Vector2 dead = Tiles[_dead];
                _noSignalCover = Box(dead.x, dead.y, 148f, 110f, A(Dark, 250));
                _noSignal = Label("NO SIGNAL", dead.x, dead.y, 150f, 14f, T.Warn, TextAlignmentOptions.Center);
                _rec = Draw();
                _rec.Disc(P(Tiles[0].x + 62f, Tiles[0].y + 44f), 3.5f * U, T.Warn);
                _rec.Commit();
            }

            internal override void Animate(float t)
            {
                DateTime now = DateTime.Now;
                if (now.Second != _second)
                {
                    _second = now.Second;
                    string stamp = now.ToString("HH:mm:ss");
                    foreach (TextMeshProUGUI time in _times)
                    {
                        time.text = stamp;
                    }
                }

                for (int k = 0; k < 4; k++)
                {
                    float y = Tiles[k].y + 54f - ((t * 0.6f + k * 0.27f) % 1f) * 108f;
                    _scans[k].rectTransform.anchoredPosition = P(Tiles[k].x, y);

                    RawImage feed = _feeds[k];
                    if (!feed.gameObject.activeSelf)
                    {
                        Texture2D frame = FeedLibrary.Texture(k);
                        if (frame != null)
                        {
                            // The feed comes through: the stand-in goes.
                            feed.texture = frame;
                            feed.gameObject.SetActive(true);
                            _corridors[k].gameObject.SetActive(false);
                            _walkers[k].gameObject.SetActive(false);
                            _connecting[k].gameObject.SetActive(false);
                        }
                        else
                        {
                            bool waiting = !FeedLibrary.Finished;
                            SetActive(_connecting[k].gameObject, waiting);
                            SetActive(_corridors[k].gameObject, !waiting);
                            SetActive(_walkers[k].gameObject, !waiting);
                            if (waiting)
                            {
                                _connecting[k].canvasRenderer.SetAlpha(t % 0.8f < 0.5f ? 1f : 0.3f);
                            }
                            else
                            {
                                Walk(k, t);
                            }
                        }
                    }
                    if (feed.gameObject.activeSelf)
                    {
                        // A slow pan across a slightly zoomed frame, and the odd flicker.
                        float s = 0.86f;
                        float px = (Mathf.Sin(t * 0.13f + _pan[k]) * 0.5f + 0.5f) * (1f - s);
                        float py = (Mathf.Cos(t * 0.11f + _pan[k]) * 0.5f + 0.5f) * (1f - s);
                        feed.uvRect = new Rect(px, py, s, s);
                        feed.canvasRenderer.SetAlpha(Rng.NextDouble() < 0.04 ? 0.7f : 0.92f);
                    }
                }

                float cycle = t % 6f;
                bool lost = cycle > 3.5f && cycle < 5f;
                SetActive(_noSignal.gameObject, lost);
                SetActive(_noSignalCover.gameObject, lost);
                if (lost)
                {
                    _noSignal.canvasRenderer.SetAlpha(0.6f + 0.4f * (float)Rng.NextDouble());
                }
                _rec.canvasRenderer.SetAlpha(t % 1f < 0.6f ? 1f : 0.15f);
            }

            /// <summary>Someone walks up the drawn corridor towards the camera, every seven seconds or so.</summary>
            private void Walk(int k, float t)
            {
                float p = ((t + k * 1.7f) % 7f) / 7f;
                float side = k % 2 == 0 ? 1f : -1f;
                RectTransform rect = _walkers[k].rectTransform;
                rect.anchoredPosition = P(Mathf.Lerp(0f, 18f * side, p), Mathf.Lerp(6f, -26f, p) + Mathf.Abs(Mathf.Sin(t * 7f)) * 1.2f * p);
                rect.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.8f, p * p);
                _walkers[k].canvasRenderer.SetAlpha(p < 0.85f ? Mathf.Clamp01(p / 0.1f) : (1f - p) / 0.15f);
            }
        }

        /// <summary>Finance: a market board whose prices keep moving.</summary>
        private sealed class Market : Signature
        {
            private static readonly string[] Names = { "TGRP", "TG-LABS", "NVSK IDX", "BTC", "RUB/USD", "TG-PHARMA" };
            private readonly TextMeshProUGUI[] _prices = new TextMeshProUGUI[6];
            private readonly TextMeshProUGUI[] _changes = new TextMeshProUGUI[6];
            private readonly Image[] _flashes = new Image[6];
            private readonly float[] _price = new float[6];
            private readonly float[] _change = new float[6];
            private readonly float[] _flash = new float[6];
            private float _next;

            protected override void Build()
            {
                Label("MARKET WATCH", 0f, 132f, 320f, 12f, T.Dim);
                Box(0f, 118f, 320f, 1f, T.Faint);
                for (int i = 0; i < 6; i++)
                {
                    float y = 92f - i * 38f;
                    _flashes[i] = Box(0f, y, 330f, 30f, T.Accent);
                    _flashes[i].canvasRenderer.SetAlpha(0f);
                    Label(Names[i], 0f, y, 320f, 13f, T.Text);
                    _prices[i] = Label(string.Empty, -10f, y, 160f, 13f, T.Dim, TextAlignmentOptions.Right);
                    _changes[i] = Label(string.Empty, 0f, y, 320f, 13f, T.Accent, TextAlignmentOptions.Right);
                    _price[i] = Range(40f, 900f);
                    _change[i] = Range(-2f, 3f);
                    Show(i);
                }
            }

            private void Show(int i)
            {
                _prices[i].text = _price[i].ToString("0.00");
                _changes[i].text = (_change[i] >= 0f ? "+" : string.Empty) + _change[i].ToString("0.00") + "%";
                _changes[i].color = _change[i] >= 0f ? T.Accent : T.Warn;
            }

            internal override void Animate(float t)
            {
                if (t >= _next)
                {
                    _next = t + 0.6f;
                    for (int n = 0; n < 2; n++)
                    {
                        int i = Rng.Next(6);
                        float move = Range(-0.012f, 0.013f) + 0.004f * Progress;
                        _price[i] *= 1f + move;
                        _change[i] += move * 100f;
                        _flash[i] = 1f;
                        Show(i);
                    }
                }
                for (int i = 0; i < 6; i++)
                {
                    _flash[i] = Mathf.Max(0f, _flash[i] - Dt * 2.5f);
                    _flashes[i].canvasRenderer.SetAlpha(_flash[i] * 0.12f);
                }
            }
        }

        /// <summary>Records: a confidential file whose lines are blacked out as the load goes.</summary>
        private sealed class Redacted : Signature
        {
            private readonly Image[] _bars = new Image[12];
            private readonly float[] _widths = new float[12];
            private readonly float[] _drawn = new float[12];

            protected override void Build()
            {
                Box(0f, 0f, 260f, 300f, Color32.Lerp(T.Ink, T.Text, 0.07f));
                Shape edge = Draw();
                Outline(edge, 0f, 0f, 260f, 300f, 1f, A(T.Dim, 90));
                edge.Commit();
                Label($"FILE 0x{Rng.Next(0x1000, 0x10000):X4}  //  CONFIDENTIAL", 0f, 128f, 230f, 11f, T.Dim);
                for (int i = 0; i < 12; i++)
                {
                    float y = 100f - i * 18f;
                    float w = Range(110f, 230f);
                    Box(-115f + w * 0.5f, y, w, 5f, A(T.Dim, 80));
                    _bars[i] = Bar(-117f, y, 10f, new Color32(0, 0, 0, 240));
                    _widths[i] = (w + 4f) * U;
                }
                Label("PAGE 1 OF 1", 0f, -135f, 230f, 10f, T.Dim, TextAlignmentOptions.Right);
            }

            internal override void Animate(float t)
            {
                float p = Progress;
                for (int i = 0; i < _bars.Length; i++)
                {
                    float target = p >= 0.05f + i / 12f * 0.9f ? _widths[i] : 0f;
                    float w = Mathf.MoveTowards(_drawn[i], target, _widths[i] * Dt / 0.35f);
                    if (Mathf.Abs(w - _drawn[i]) > 0.01f)
                    {
                        _drawn[i] = w;
                        _bars[i].rectTransform.sizeDelta = new Vector2(Mathf.Round(w), _bars[i].rectTransform.sizeDelta.y);
                    }
                }
            }
        }

        /// <summary>Executive: the board's world clocks, on real time.</summary>
        private sealed class Clocks : Signature
        {
            private static readonly string[] Cities = { "NORVINSK", "LONDON", "NEW YORK" };
            private static readonly int[] Offsets = { 0, 0, -5 };
            private readonly Shape[] _hours = new Shape[3];
            private readonly Shape[] _minutes = new Shape[3];
            private readonly Shape[] _seconds = new Shape[3];
            private readonly TextMeshProUGUI[] _digital = new TextMeshProUGUI[3];
            private int _minute = -1;

            protected override void Build()
            {
                Label("WORLD OFFICES", 0f, 112f, 320f, 12f, T.Dim, TextAlignmentOptions.Center);
                for (int k = 0; k < 3; k++)
                {
                    float x = -105f + k * 105f;
                    Shape face = Draw(x, 20f);
                    face.Arc(44f * U, 1.5f * U, 0f, 360f, A(T.Accent, 170));
                    for (int i = 0; i < 12; i++)
                    {
                        face.Line(Polar((i % 3 == 0 ? 33f : 37f) * U, i * 30f), Polar(41f * U, i * 30f), (i % 3 == 0 ? 2f : 1f) * U, A(T.Accent, 200));
                    }
                    face.Commit();
                    _hours[k] = Hand(x, 22f, 3f, T.Text);
                    _minutes[k] = Hand(x, 32f, 2f, T.Text);
                    _seconds[k] = Hand(x, 37f, 1f, T.Accent);
                    Shape hub = Draw(x, 20f);
                    hub.Disc(Vector2.zero, 3.5f * U, T.Accent);
                    hub.Commit();
                    Label(Cities[k], x, -40f, 104f, 10f, T.Dim, TextAlignmentOptions.Center);
                    _digital[k] = Label(string.Empty, x, -57f, 104f, 11f, T.Accent, TextAlignmentOptions.Center);
                }
            }

            private Shape Hand(float x, float length, float width, Color32 color)
            {
                Shape hand = Draw(x, 20f);
                hand.Line(P(0f, -length * 0.18f), P(0f, length), width * U, color);
                hand.Commit();
                return hand;
            }

            internal override void Animate(float t)
            {
                DateTime utc = DateTime.UtcNow;
                bool newMinute = utc.Minute != _minute;
                _minute = utc.Minute;
                for (int k = 0; k < 3; k++)
                {
                    DateTime time = k == 0 ? DateTime.Now : utc.AddHours(Offsets[k]);
                    float s = time.Second + time.Millisecond / 1000f;
                    float m = time.Minute + s / 60f;
                    float h = time.Hour % 12 + m / 60f;
                    _hours[k].rectTransform.localEulerAngles = new Vector3(0f, 0f, -h * 30f);
                    _minutes[k].rectTransform.localEulerAngles = new Vector3(0f, 0f, -m * 6f);
                    _seconds[k].rectTransform.localEulerAngles = new Vector3(0f, 0f, -s * 6f);
                    if (newMinute)
                    {
                        _digital[k].text = time.ToString("HH:mm");
                    }
                }
            }
        }

        /// <summary>Internal Affairs: a personnel file on the player, the photo under a moving scan.</summary>
        private sealed class Dossier : Signature
        {
            private Image _scan;
            private TextMeshProUGUI _name;
            private TextMeshProUGUI _status;
            private string _shownName = "\0";

            protected override void Build()
            {
                Box(0f, 0f, 320f, 230f, A(Color32.Lerp(T.Ink, T.Primary, 0.12f), 240));
                Shape edge = Draw();
                Outline(edge, 0f, 0f, 320f, 230f, 1f, A(T.Accent, 90));
                edge.Commit();
                Label($"PERSONNEL FILE  //  {Rng.Next(10, 100)}-{Rng.Next(1000, 10000)}", 0f, 96f, 296f, 11f, T.Dim);
                Box(-95f, -4f, 96f, 120f, Dark);
                Shape person = Draw(-95f, -4f);
                person.Disc(P(0f, 20f), 18f * U, A(T.Dim, 150));
                var shoulders = new Vector2[13];
                for (int i = 0; i <= 12; i++)
                {
                    float a = Mathf.PI * i / 12f;
                    shoulders[i] = P(Mathf.Cos(a) * 38f, -58f + Mathf.Sin(a) * 52f);
                }
                person.Polygon(shoulders, A(T.Dim, 150));
                person.Commit();
                _scan = Box(-95f, 0f, 96f, 2f, A(T.Accent, 170));
                Label("SUBJECT", 60f, 44f, 180f, 10f, T.Dim);
                _name = Label(string.Empty, 60f, 27f, 180f, 15f, T.Text);
                Label("CLEARANCE", 60f, 2f, 180f, 10f, T.Dim);
                Label("B  //  PROVISIONAL", 60f, -15f, 180f, 13f, T.Text);
                Label("STATUS", 60f, -40f, 180f, 10f, T.Dim);
                _status = Label("UNDER REVIEW", 60f, -57f, 180f, 14f, T.Warn);
            }

            internal override void Animate(float t)
            {
                _scan.rectTransform.anchoredPosition = P(-95f, -4f + Mathf.Sin(t * 1.6f) * 56f);
                _status.canvasRenderer.SetAlpha(t % 1.2f < 0.8f ? 1f : 0.35f);
                string name = StartupTracker.Nickname;
                if (name != _shownName)
                {
                    _shownName = name;
                    _name.text = string.IsNullOrEmpty(name) ? "UNIDENTIFIED" : name.ToUpperInvariant();
                }
            }
        }

        /// <summary>Logistics: a shipment crossing a route map, carried by the load's progress.</summary>
        private sealed class Route : Signature
        {
            private static readonly Vector2[] Points = { new Vector2(-130f, -95f), new Vector2(-55f, 30f), new Vector2(55f, -40f), new Vector2(130f, 95f) };
            private static readonly string[] Stops = { "PORT", "CUSTOMS", "DEPOT", "HQ" };
            private Shape _done;
            private Shape _marker;
            private Shape _halo;
            private TextMeshProUGUI _status;
            private int _segment = -1;
            private int _percent = -1;

            protected override void Build()
            {
                Label($"SHIPMENT TG-{Rng.Next(100, 1000)}  //  IN TRANSIT", 0f, 140f, 320f, 12f, T.Dim);
                Shape dashes = Draw();
                for (int i = 0; i < Points.Length - 1; i++)
                {
                    Vector2 a = Points[i], b = Points[i + 1];
                    float length = Vector2.Distance(a, b);
                    for (float d = 0f; d < length; d += 10f)
                    {
                        dashes.Line(P(Vector2.Lerp(a, b, d / length).x, Vector2.Lerp(a, b, d / length).y),
                            P(Vector2.Lerp(a, b, Mathf.Min(d + 5f, length) / length).x, Vector2.Lerp(a, b, Mathf.Min(d + 5f, length) / length).y),
                            1.5f * U, A(T.Dim, 120));
                    }
                }
                dashes.Commit();
                _done = Draw();
                Shape stops = Draw();
                for (int i = 0; i < Points.Length; i++)
                {
                    stops.Disc(P(Points[i].x, Points[i].y), 5f * U, A(T.Accent, 210));
                    Label(Stops[i], Points[i].x, Points[i].y + (i % 2 == 0 ? -18f : 18f), 100f, 10f, T.Dim, TextAlignmentOptions.Center);
                }
                stops.Commit();
                _halo = Draw();
                _halo.Arc(11f * U, 1.5f * U, 0f, 360f, A(T.Accent, 200));
                _halo.Commit();
                _marker = Draw();
                _marker.Disc(Vector2.zero, 6f * U, T.Text);
                _marker.Commit();
                _status = Label(string.Empty, 0f, -140f, 320f, 11f, T.Accent);
            }

            internal override void Animate(float t)
            {
                float total = 0f;
                for (int i = 0; i < Points.Length - 1; i++)
                {
                    total += Vector2.Distance(Points[i], Points[i + 1]);
                }
                float d = Progress * total;
                int segment = 0;
                Vector2 at = Points[Points.Length - 1];
                for (int i = 0; i < Points.Length - 1; i++)
                {
                    float length = Vector2.Distance(Points[i], Points[i + 1]);
                    if (d <= length || i == Points.Length - 2)
                    {
                        segment = i;
                        at = Vector2.Lerp(Points[i], Points[i + 1], Mathf.Clamp01(d / length));
                        break;
                    }
                    d -= length;
                }
                _marker.rectTransform.anchoredPosition = P(at.x, at.y);
                _halo.rectTransform.anchoredPosition = P(at.x, at.y);
                float pulse = (t * 1.2f) % 1f;
                _halo.rectTransform.localScale = Vector3.one * (1f + pulse * 0.8f);
                _halo.canvasRenderer.SetAlpha(1f - pulse);

                if (segment != _segment)
                {
                    _segment = segment;
                    _done.Clear();
                    for (int i = 0; i < segment; i++)
                    {
                        _done.Line(P(Points[i].x, Points[i].y), P(Points[i + 1].x, Points[i + 1].y), 2.5f * U, T.Accent);
                    }
                    _done.Commit();
                }
                int percent = Mathf.FloorToInt(Progress * 100f);
                if (percent != _percent)
                {
                    _percent = percent;
                    _status.text = percent >= 100 ? "DELIVERED  //  SIGNED FOR" : $"{percent}% EN ROUTE  //  NEXT STOP {Stops[segment + 1]}";
                }
            }
        }

        /// <summary>Human Resources: the player's staff badge, swinging gently on its lanyard.</summary>
        private sealed class Badge : Signature
        {
            private RectTransform _swing;
            private TextMeshProUGUI _initials;
            private TextMeshProUGUI _name;
            private string _shownName = "\0";

            protected override void Build()
            {
                // Hung from the card's top edge: the lanyard's pivot is just inside it, and the
                // badge stays well within the card even at the end of its sway.
                _swing = MakeRect("Swing", R);
                _swing.anchoredPosition = P(0f, 150f);
                Shape cord = Draw(0f, 0f, _swing);
                cord.Line(P(-22f, 10f), P(-4f, -30f), 2f * U, A(T.Accent, 200));
                cord.Line(P(22f, 10f), P(4f, -30f), 2f * U, A(T.Accent, 200));
                cord.Commit();
                Color32 card = Color32.Lerp(T.Text, new Color32(255, 255, 255, 255), 0.4f);
                Color32 ink = Color32.Lerp(T.Ink, new Color32(0, 0, 0, 255), 0.3f);
                Box(0f, -150f, 176f, 236f, card, _swing);
                Box(0f, -40f, 30f, 5f, A(ink, 150), _swing);
                Box(0f, -64f, 176f, 30f, T.Primary, _swing);
                Label("TERRAGROUP", 0f, -64f, 176f, 13f, card, TextAlignmentOptions.Center, _swing, 10f);
                Box(0f, -118f, 64f, 64f, A(T.Primary, 60), _swing);
                _initials = Label("TG", 0f, -118f, 64f, 22f, T.Primary, TextAlignmentOptions.Center, _swing);
                _name = Label(string.Empty, 0f, -168f, 166f, 14f, ink, TextAlignmentOptions.Center, _swing, 4f);
                Label($"EMPLOYEE {Rng.Next(10, 100)}-{Rng.Next(1000, 10000)}", 0f, -186f, 166f, 10f, A(ink, 170), TextAlignmentOptions.Center, _swing);
                Label("HUMAN RESOURCES", 0f, -201f, 166f, 10f, T.Primary, TextAlignmentOptions.Center, _swing);
                Shape code = Draw(0f, 0f, _swing);
                for (float x = -60f; x < 60f;)
                {
                    float w = Rng.Next(1, 4);
                    code.Line(P(x + w * 0.5f, -218f), P(x + w * 0.5f, -246f), w * U, ink);
                    x += w + Rng.Next(1, 4);
                }
                code.Commit();
            }

            internal override void Animate(float t)
            {
                _swing.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * 1.1f) * 1.2f + O._beat * 0.8f);
                string name = StartupTracker.Nickname;
                if (name != _shownName)
                {
                    _shownName = name;
                    bool known = !string.IsNullOrEmpty(name);
                    _name.text = known ? name.ToUpperInvariant() : "NEW STARTER";
                    _initials.text = known ? name.Substring(0, Math.Min(2, name.Length)).ToUpperInvariant() : "TG";
                }
            }
        }

        /// <summary>IT: a server rack whose lights get busier as the load goes, and in alert.</summary>
        private sealed class Rack : Signature
        {
            private readonly Image[] _leds = new Image[48];
            private readonly bool[] _on = new bool[48];

            protected override void Build()
            {
                Label("RACK B-07  //  NORVINSK DC", 0f, 140f, 320f, 12f, T.Dim);
                Box(0f, -5f, 220f, 270f, A(Dark, 240));
                Shape rails = Draw();
                rails.Line(P(-104f, 130f), P(-104f, -140f), 2f * U, A(T.Dim, 90));
                rails.Line(P(104f, 130f), P(104f, -140f), 2f * U, A(T.Dim, 90));
                rails.Commit();
                for (int i = 0; i < 8; i++)
                {
                    float y = 110f - i * 32f;
                    Box(0f, y, 200f, 26f, Color32.Lerp(T.Ink, T.Primary, 0.15f));
                    Label($"SRV-{i + 1:00}", 0f, y, 190f, 10f, T.Dim);
                    for (int j = 0; j < 6; j++)
                    {
                        int k = i * 6 + j;
                        _leds[k] = Box(30f + j * 12f, y, 6f, 6f, j == 5 ? T.Warn : T.Accent);
                        _on[k] = Rng.Next(2) == 0;
                        _leds[k].canvasRenderer.SetAlpha(_on[k] ? 1f : 0.12f);
                    }
                }
            }

            internal override void Animate(float t)
            {
                float chance = (1.5f + 5f * Progress + 6f * O._alert) * Dt;
                for (int k = 0; k < _leds.Length; k++)
                {
                    // The amber ones are rare.
                    float own = k % 6 == 5 ? chance * 0.15f : chance;
                    if (Rng.NextDouble() < own)
                    {
                        _on[k] = !_on[k];
                        _leds[k].canvasRenderer.SetAlpha(_on[k] ? 1f : 0.12f);
                    }
                }
            }
        }

        /// <summary>Health: a heart monitor; the pulse quickens when a step runs slow.</summary>
        private sealed class Ecg : Signature
        {
            private const float Period = 160f;
            private Shape _trace;
            private TextMeshProUGUI _rate;
            private float _offset;
            private int _shownRate = -1;

            protected override void Build()
            {
                Label("STAFF VITALS  //  MONITOR 3", 0f, 104f, 320f, 12f, T.Dim);
                Box(0f, 0f, 320f, 160f, A(Dark, 240));
                Shape grid = Draw();
                for (float x = -140f; x < 160f; x += 20f)
                {
                    grid.Line(P(x, 78f), P(x, -78f), U, A(T.Accent, 18));
                }
                for (float y = -60f; y < 80f; y += 20f)
                {
                    grid.Line(P(-158f, y), P(158f, y), U, A(T.Accent, 18));
                }
                grid.Commit();
                RectTransform mask = MakeRect("Mask", R);
                mask.sizeDelta = P(316f, 156f);
                mask.gameObject.AddComponent<RectMask2D>();
                _trace = Draw(0f, 0f, mask);
                Vector2 last = Vector2.zero;
                for (float x = -160f; x <= 160f + Period * 2f; x += 2f)
                {
                    Vector2 point = P(x, Wave(((x + 160f) % Period) / Period) - 10f);
                    if (x > -160f)
                    {
                        _trace.Line(last, point, 2f * U, T.Accent);
                    }
                    last = point;
                }
                _trace.Commit();
                Label("SPO2  98%", 0f, -100f, 320f, 13f, T.Dim);
                _rate = Label(string.Empty, 0f, -100f, 320f, 13f, T.Accent, TextAlignmentOptions.Right);
            }

            /// <summary>One heartbeat: P wave, the QRS spike, T wave, flat between.</summary>
            internal static float Wave(float p)
            {
                if (p >= 0.10f && p < 0.18f) return 8f * Mathf.Sin(Mathf.PI * (p - 0.10f) / 0.08f);
                if (p >= 0.24f && p < 0.27f) return -10f * (p - 0.24f) / 0.03f;
                if (p >= 0.27f && p < 0.30f) return Mathf.Lerp(-10f, 58f, (p - 0.27f) / 0.03f);
                if (p >= 0.30f && p < 0.33f) return Mathf.Lerp(58f, -18f, (p - 0.30f) / 0.03f);
                if (p >= 0.33f && p < 0.36f) return Mathf.Lerp(-18f, 0f, (p - 0.33f) / 0.03f);
                if (p >= 0.46f && p < 0.60f) return 14f * Mathf.Sin(Mathf.PI * (p - 0.46f) / 0.14f);
                return 0f;
            }

            internal override void Animate(float t)
            {
                float bpm = 68f + 40f * O._alert + 8f * O._beat + 3f * Mathf.Sin(t * 0.3f);
                _offset = (_offset + Dt * Period * U * bpm / 60f) % (Period * U);
                _trace.rectTransform.anchoredPosition = new Vector2(-_offset, 0f);
                int rate = Mathf.RoundToInt(bpm);
                if (rate != _shownRate)
                {
                    _shownRate = rate;
                    _rate.text = $"HR  {rate} BPM";
                }
            }
        }

        /// <summary>Facilities: a lift panel climbing floor by floor with the load.</summary>
        private sealed class Elevator : Signature
        {
            private static readonly string[] Floors = { "B2", "B1", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };
            private readonly TextMeshProUGUI[] _buttons = new TextMeshProUGUI[12];
            private TextMeshProUGUI _display;
            private Shape _lit;
            private Shape _arrow;
            private int _floor = -1;

            /// <summary>Two columns of six, clear of the display above and the panel's edges.</summary>
            private static Vector2 Button(int i) => new Vector2(i % 2 == 0 ? -32f : 32f, -110f + (i / 2) * 30f);

            protected override void Build()
            {
                Label("LIFT A  //  NORTH CORE", 0f, 142f, 320f, 12f, T.Dim, TextAlignmentOptions.Center);
                Box(0f, -5f, 170f, 270f, A(Dark, 240));
                Shape frame = Draw();
                Outline(frame, 0f, -5f, 170f, 270f, 1.5f, A(T.Dim, 120));
                for (int i = 0; i < 12; i++)
                {
                    Vector2 b = Button(i);
                    frame.Disc(P(b.x, b.y), 12f * U, A(T.Dim, 45));
                }
                frame.Commit();
                Box(0f, 92f, 130f, 56f, new Color32(0, 0, 0, 255));
                _display = Label(string.Empty, 12f, 92f, 100f, 40f, T.Accent, TextAlignmentOptions.Center);
                _arrow = Draw(-40f, 92f);
                _arrow.Tri(P(0f, 9f), P(-8f, -6f), P(8f, -6f), T.Accent);
                _arrow.Commit();
                _lit = Draw();
                _lit.Disc(Vector2.zero, 12f * U, T.Accent);
                _lit.Commit();
                for (int i = 0; i < 12; i++)
                {
                    Vector2 b = Button(i);
                    _buttons[i] = Label(Floors[i], b.x, b.y, 30f, 11f, T.Dim, TextAlignmentOptions.Center);
                }
            }

            internal override void Animate(float t)
            {
                int floor = Mathf.Clamp(Mathf.RoundToInt(Progress * 11f), 0, 11);
                if (floor != _floor)
                {
                    if (_floor >= 0)
                    {
                        _buttons[_floor].color = T.Dim;
                    }
                    _floor = floor;
                    _buttons[floor].color = T.Ink;
                    Vector2 b = Button(floor);
                    _lit.rectTransform.anchoredPosition = P(b.x, b.y);
                    _display.text = Floors[floor];
                }
                bool moving = O._phase == Phase.Loading;
                _arrow.canvasRenderer.SetAlpha(moving ? 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(t * 3f)) : 0.15f);
            }
        }

        /// <summary>Legal: a service agreement that signs itself over the last part of the load.</summary>
        private sealed class Contract : Signature
        {
            private const int Points = 70;
            private readonly Vector2[] _path = new Vector2[Points];
            private Shape _signature;
            private int _drawn = -1;

            protected override void Build()
            {
                Box(0f, 0f, 260f, 300f, Color32.Lerp(T.Ink, T.Accent, 0.07f));
                Shape edge = Draw();
                Outline(edge, 0f, 0f, 260f, 300f, 1f, A(T.Accent, 80));
                edge.Commit();
                Label("SERVICE AGREEMENT", 0f, 124f, 230f, 13f, T.Text);
                Label("SCHEDULE B  //  PRIVILEGED", 0f, 106f, 230f, 9f, T.Dim);
                for (int i = 0; i < 7; i++)
                {
                    float w = Range(140f, 220f);
                    Box(-115f + w * 0.5f, 84f - i * 16f, w, 5f, A(T.Dim, 80));
                }
                Label("SIGNATURE", 0f, -56f, 230f, 9f, T.Dim);
                Box(-20f, -102f, 190f, 1f, A(T.Dim, 170));
                Label($"DATE  {DateTime.Now:dd.MM.yyyy}", 0f, -120f, 230f, 9f, T.Dim, TextAlignmentOptions.Right);
                for (int i = 0; i < Points; i++)
                {
                    float s = i / (Points - 1f);
                    _path[i] = P(-105f + 170f * s + 7f * Mathf.Cos(s * 24f), -84f + 9f * Mathf.Sin(s * 24f) + 4f * Mathf.Sin(s * 61f));
                }
                _signature = Draw();
            }

            internal override void Animate(float t)
            {
                int count = Mathf.Clamp(Mathf.FloorToInt((Progress - 0.6f) / 0.4f * Points), 0, Points);
                if (count == _drawn)
                {
                    return;
                }
                _drawn = count;
                _signature.Clear();
                for (int i = 0; i < count - 1; i++)
                {
                    _signature.Line(_path[i], _path[i + 1], 2f * U, T.Accent);
                }
                _signature.Commit();
            }
        }

        /// <summary>Communications: the approval rating, needle wobbling, climbing with the load.</summary>
        private sealed class Approval : Signature
        {
            private Shape _needle;
            private TextMeshProUGUI _value;
            private int _shown = -1;

            protected override void Build()
            {
                Label("PUBLIC APPROVAL", 0f, 124f, 320f, 12f, T.Dim, TextAlignmentOptions.Center);
                Shape dial = Draw(0f, -10f);
                dial.Arc(105f * U, 10f * U, -90f, 59f, A(T.Warn, 170));
                dial.Arc(105f * U, 10f * U, -30f, 59f, A(T.Dim, 140));
                dial.Arc(105f * U, 10f * U, 30f, 60f, A(T.Accent, 210));
                for (int v = 0; v <= 10; v++)
                {
                    dial.Line(Polar(86f * U, -90f + v * 18f), Polar(93f * U, -90f + v * 18f), (v % 5 == 0 ? 2f : 1f) * U, A(T.Text, 160));
                }
                dial.Commit();
                _needle = Draw(0f, -10f);
                _needle.Line(P(0f, -6f), P(0f, 88f), 3f * U, T.Text);
                _needle.Disc(Vector2.zero, 7f * U, T.Text);
                _needle.Commit();
                _value = Label(string.Empty, 0f, -52f, 320f, 30f, T.Text, TextAlignmentOptions.Center);
                Label("SENTIMENT INDEX", 0f, -80f, 320f, 10f, T.Dim, TextAlignmentOptions.Center);
            }

            internal override void Animate(float t)
            {
                float v = Mathf.Clamp(52f + 38f * Progress + 3f * Mathf.Sin(t * 1.9f) + 2f * Mathf.Sin(t * 4.3f) - 25f * O._alert, 0f, 100f);
                _needle.rectTransform.localEulerAngles = new Vector3(0f, 0f, 90f - v * 1.8f);
                int shown = Mathf.RoundToInt(v);
                if (shown != _shown)
                {
                    _shown = shown;
                    _value.text = shown + "%";
                }
            }
        }

        /// <summary>
        /// Four of the division's own numbers, each in a tile with its change and a sparkline,
        /// drifting as if live. Made up, apart from the one that tracks the load.
        /// </summary>
        private sealed class KeyFigures : Signature
        {
            private const int History = 12;
            private static readonly Vector2[] Tiles = { new Vector2(-80f, 52f), new Vector2(80f, 52f), new Vector2(-80f, -84f), new Vector2(80f, -84f) };
            private readonly TextMeshProUGUI[] _values = new TextMeshProUGUI[4];
            private readonly TextMeshProUGUI[] _deltas = new TextMeshProUGUI[4];
            private readonly Shape[] _sparks = new Shape[4];
            private readonly float[][] _history = new float[4][];
            private readonly float[] _base = new float[4];
            private readonly float[] _value = new float[4];
            private readonly string[] _suffix = new string[4];
            private readonly int[] _decimals = new int[4];
            private float _next;

            /// <summary>Label, starting value, unit, decimals: four per division.</summary>
            private static string[] Figures(ThemeChoice id)
            {
                switch (id)
                {
                    case ThemeChoice.Research: return new[] { "ACTIVE TRIALS|14||0", "SAMPLES|2316||0", "SUCCESS RATE|63|%|1", "OPEN INCIDENTS|3||0" };
                    case ThemeChoice.Security: return new[] { "BADGE SCANS|8412||0", "GUARDS ON DUTY|46||0", "OPEN INCIDENTS|2||0", "GATES SECURED|98|%|1" };
                    case ThemeChoice.Finance: return new[] { "REVENUE Q4|412.8| M|1", "EXPENSES|301.2| M|1", "MARGIN|27.0|%|1", "CASH ON HAND|88.4| M|1" };
                    case ThemeChoice.Records: return new[] { "FILES HELD|1.24| M|2", "REQUESTS|318||0", "PAGES SHREDDED|9640||0", "DIGITISED|71|%|1" };
                    case ThemeChoice.Executive: return new[] { "MARKET CAP|38.6| B|1", "BONUS POOL|112| M|0", "SHARE PRICE|412.80||2", "BOARD APPROVAL|94|%|1" };
                    case ThemeChoice.InternalAffairs: return new[] { "OPEN CASES|27||0", "STAFF FLAGGED|112||0", "INTERVIEWS|9||0", "CASES CLOSED|61|%|1" };
                    case ThemeChoice.Logistics: return new[] { "SHIPMENTS|1284||0", "ON TIME|91.5|%|1", "CONTAINERS|362||0", "DELAYED|17||0" };
                    case ThemeChoice.HumanResources: return new[] { "HEADCOUNT|12408||0", "OPEN ROLES|231||0", "TURNOVER|14.2|%|1", "MORALE|58|%|1" };
                    case ThemeChoice.ItDepartment: return new[] { "OPEN TICKETS|412||0", "UPTIME|99.4|%|2", "PATCHES DUE|38||0", "PHISHING CLICKS|12|%|1" };
                    case ThemeChoice.Medical: return new[] { "PATIENTS|86||0", "BEDS FREE|23||0", "VACCINATED|96|%|1", "SICK DAYS|412||0" };
                    case ThemeChoice.Facilities: return new[] { "WORK ORDERS|143||0", "LIFTS IN SERVICE|5||0", "ENERGY USE|3.42| MW|2", "DAYS SAFE|0||0" };
                    case ThemeChoice.Legal: return new[] { "OPEN CASES|64||0", "NDAS SIGNED|2310||0", "SETTLEMENTS|18||0", "BILLABLE HOURS|9412||0" };
                    case ThemeChoice.PublicRelations: return new[] { "MENTIONS|18.2| K|1", "SENTIMENT|62|%|1", "RELEASES|41||0", "NO COMMENTS|128||0" };
                    case ThemeChoice.Holiday: return new[] { "GIFTS WRAPPED|412||0", "DAYS OFF|2||0", "PARTIES|0||0", "SNOWFALL|14| CM|0" };
                    default: return new[] { "HEADCOUNT|12408||0", "SHARE PRICE|412.80||2", "OFFICES|37||0", "SATISFACTION|71|%|1" };
                }
            }

            protected override void Build()
            {
                Label("KEY FIGURES", 0f, 138f, 320f, 12f, T.Dim);
                Box(0f, 124f, 320f, 1f, T.Faint);
                string[] figures = Figures(T.Id);
                for (int i = 0; i < 4; i++)
                {
                    string[] parts = figures[i].Split('|');
                    Vector2 c = Tiles[i];
                    Box(c.x, c.y, 150f, 124f, Color32.Lerp(T.Ink, T.Primary, 0.14f));
                    Label(parts[0], c.x, c.y + 46f, 136f, 9f, T.Dim);
                    _values[i] = Label(string.Empty, c.x, c.y + 18f, 136f, 22f, T.Text, spacing: 2f);
                    _deltas[i] = Label(string.Empty, c.x, c.y - 8f, 136f, 10f, T.Accent);
                    _sparks[i] = Draw(c.x, c.y - 38f);
                    _base[i] = _value[i] = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                    _suffix[i] = parts[2];
                    _decimals[i] = int.Parse(parts[3]);
                    _history[i] = new float[History];
                    for (int h = 0; h < History; h++)
                    {
                        _history[i][h] = _base[i] * (1f + Range(-0.04f, 0.04f));
                    }
                    _history[i][History - 1] = _value[i];
                    Show(i);
                }
            }

            private void Show(int i)
            {
                string format = _decimals[i] == 0 ? "N0" : "N" + _decimals[i];
                _values[i].text = _value[i].ToString(format, System.Globalization.CultureInfo.InvariantCulture) + _suffix[i];
                float change = _base[i] != 0f ? (_value[i] - _base[i]) / _base[i] * 100f : 0f;
                _deltas[i].text = (change >= 0f ? "+" : string.Empty) + change.ToString("0.0") + "% TODAY";
                _deltas[i].color = change >= 0f ? T.Accent : T.Warn;

                float[] h = _history[i];
                float min = float.MaxValue, max = float.MinValue;
                foreach (float v in h)
                {
                    min = Mathf.Min(min, v);
                    max = Mathf.Max(max, v);
                }
                float span = Mathf.Max(max - min, Mathf.Abs(_base[i]) * 0.01f + 0.0001f);
                Shape s = _sparks[i];
                s.Clear();
                for (int k = 0; k < History - 1; k++)
                {
                    Vector2 a = P(-65f + k * 130f / (History - 1), (h[k] - min) / span * 22f - 11f);
                    Vector2 b = P(-65f + (k + 1) * 130f / (History - 1), (h[k + 1] - min) / span * 22f - 11f);
                    s.Line(a, b, 1.5f * U, A(T.Accent, 170));
                }
                s.Commit();
            }

            internal override void Animate(float t)
            {
                if (t < _next)
                {
                    return;
                }
                _next = t + 1.1f;
                int i = Rng.Next(4);
                _value[i] = Mathf.Max(0f, _value[i] * (1f + Range(-0.025f, 0.03f)));
                if (_decimals[i] == 0)
                {
                    _value[i] = Mathf.Round(_value[i] + Rng.Next(-2, 4));
                    _value[i] = Mathf.Max(0f, _value[i]);
                }
                Array.Copy(_history[i], 1, _history[i], 0, History - 1);
                _history[i][History - 1] = _value[i];
                Show(i);
            }
        }

        /// <summary>
        /// The division's notice board: timestamped items, a new one arriving at the top every
        /// few seconds and pushing the oldest off.
        /// </summary>
        private sealed class Bulletin : Signature
        {
            private const int Rows = 5;
            private readonly TextMeshProUGUI[] _times = new TextMeshProUGUI[Rows];
            private readonly TextMeshProUGUI[] _items = new TextMeshProUGUI[Rows];
            private readonly string[] _timeText = new string[Rows];
            private readonly string[] _itemText = new string[Rows];
            private Image _flash;
            private Chatter _source;
            private float _next;
            private float _arrived = -10f;

            protected override void Build()
            {
                _source = new Chatter(Rng, T);
                Label("BULLETIN", 0f, 138f, 320f, 12f, T.Dim);
                Box(0f, 124f, 320f, 1f, T.Faint);
                _flash = Box(0f, 96f, 330f, 46f, T.Accent);
                _flash.canvasRenderer.SetAlpha(0f);
                DateTime now = DateTime.Now;
                int minutesAgo = 0;
                for (int i = 0; i < Rows; i++)
                {
                    float y = 104f - i * 50f;
                    _times[i] = Label(string.Empty, 0f, y, 320f, 10f, T.Accent);
                    _items[i] = Label(string.Empty, 0f, y - 17f, 320f, 12f, T.Text, spacing: 3f);
                    _items[i].overflowMode = TextOverflowModes.Ellipsis;
                    if (i < Rows - 1)
                    {
                        Box(0f, y - 33f, 320f, 1f, A(T.Dim, 28));
                    }
                    minutesAgo += Rng.Next(3, 40);
                    _timeText[i] = now.AddMinutes(-minutesAgo).ToString("HH:mm") + "  //  " + Sender();
                    _itemText[i] = Notice();
                }
                Refresh();
            }

            private static readonly string[] Senders = { "HR", "FACILITIES", "LEGAL", "IT", "SECURITY", "COMMS", "THE BOARD" };

            private string Sender() => Senders[Rng.Next(Senders.Length)];

            private string Notice()
            {
                // Office chatter and the division's headlines, read as notices.
                if (Rng.Next(3) == 0 && T.Headlines.Length > 0)
                {
                    return T.Headlines[Rng.Next(T.Headlines.Length)];
                }
                string line = _source.Next();
                return line.StartsWith(":: ") ? line.Substring(3) : line;
            }

            private void Refresh()
            {
                for (int i = 0; i < Rows; i++)
                {
                    _times[i].text = _timeText[i];
                    _items[i].text = _itemText[i];
                    _times[i].canvasRenderer.SetAlpha(1f - i * 0.12f);
                    _items[i].canvasRenderer.SetAlpha(1f - i * 0.14f);
                }
            }

            internal override void Animate(float t)
            {
                if (_next == 0f)
                {
                    _next = t + Range(2f, 4f);
                }
                if (t >= _next)
                {
                    _next = t + Range(3.5f, 6f);
                    for (int i = Rows - 1; i > 0; i--)
                    {
                        _timeText[i] = _timeText[i - 1];
                        _itemText[i] = _itemText[i - 1];
                    }
                    _timeText[0] = DateTime.Now.ToString("HH:mm") + "  //  " + Sender() + "  //  NEW";
                    _itemText[0] = Notice();
                    Refresh();
                    _arrived = t;
                }
                _flash.canvasRenderer.SetAlpha(Mathf.Clamp01(1f - (t - _arrived) / 0.8f) * 0.1f);
            }
        }

        /// <summary>
        /// Every division's second panel: live facts about this sign-in. Values are rewritten
        /// only when they change: the clocks once a second or tenth, the rest rarely.
        /// </summary>
        private sealed class SessionPanel : Signature
        {
            private static readonly string[] Keys =
            {
                "OPERATOR", "DATE", "LOCAL TIME", "SESSION", "PLUGINS", "ITEM DATABASE", "THIS SYNC", "LAST SYNC"
            };
            private readonly TextMeshProUGUI[] _values = new TextMeshProUGUI[8];
            private readonly string[] _shown = new string[8];
            private int _second = -1;
            private int _tenths = -1;
            private float _nextSlow;

            protected override void Build()
            {
                Label("SESSION DETAILS", 0f, 138f, 320f, 12f, T.Dim);
                Box(0f, 124f, 320f, 1f, T.Faint);
                for (int i = 0; i < Keys.Length; i++)
                {
                    float y = 98f - i * 31f;
                    Label(Keys[i], 0f, y, 320f, 11f, T.Dim);
                    _values[i] = Label(string.Empty, 0f, y, 320f, 13f, T.Text, TextAlignmentOptions.Right);
                    Box(0f, y - 15.5f, 320f, 1f, A(T.Dim, 28));
                }
                Show(1, DateTime.Now.ToString("dd MMM yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant());
                Show(3, StartupTracker.Launches > 0 ? StartupTracker.Launches.ToString("0000") : "--");
                Show(7, StartupTracker.LastTotal > 0f ? $"{StartupTracker.LastTotal:0.0} S" : "--");
            }

            private void Show(int row, string value)
            {
                if (_shown[row] != value)
                {
                    _shown[row] = value;
                    _values[row].text = value;
                }
            }

            internal override void Animate(float t)
            {
                DateTime now = DateTime.Now;
                if (now.Second != _second)
                {
                    _second = now.Second;
                    Show(2, now.ToString("HH:mm:ss"));
                }
                if (StartupTracker.Active)
                {
                    int tenths = Mathf.FloorToInt(StartupTracker.Elapsed * 10f);
                    if (tenths != _tenths)
                    {
                        _tenths = tenths;
                        Show(6, $"{tenths / 10f:0.0} S");
                    }
                }
                if (t < _nextSlow)
                {
                    return;
                }
                _nextSlow = t + 0.5f;
                string name = StartupTracker.Nickname;
                Show(0, string.IsNullOrEmpty(name) ? "SIGNING IN..." : name.ToUpperInvariant());
                Show(4, BepInEx.Bootstrap.Chainloader.PluginInfos.Count + " LOADED");
                string items = StartupTracker.Detail((int)StepId.ItemDatabase);
                Show(5, !string.IsNullOrEmpty(items) ? items.ToUpperInvariant()
                    : StartupTracker.Current >= (int)StepId.ItemDatabase ? "INDEXING..." : "PENDING");
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
