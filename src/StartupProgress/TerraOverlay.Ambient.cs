using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace StartupProgress
{
    /// <summary>
    /// Each department's ambient drawing: one large, faint, slow thing behind the terminal that
    /// belongs to that department and, where it can, follows the load, the way Finance's share
    /// chart climbs. No widgets; at the chart's opacity so the logo and readout stay the subject.
    ///
    /// Nothing is drawn under text. The centred layout's text is the logo and wordmark, the
    /// readout column under them (within 320 of the centre line), the memo in the top-right
    /// corner, the corner lines and the ticker. The drawings keep to what that leaves: a zone
    /// either side of the column, and a strip under the readout, above the ticker. Built on the
    /// full-screen layer, in 1080p units around the screen's centre.
    /// </summary>
    internal partial class TerraOverlay
    {
        /// <summary>
        /// Whether the ambient drawings have room: only the centred layout leaves the zones free,
        /// and a screen much narrower than 5:4 leaves too little beside the column.
        /// </summary>
        private bool AmbientRoom => _layout == LayoutKind.Centered && _laidOutSize.x >= 1250f * _u;

        /// <summary>The department's ambient drawing, or null where the backdrop already has one (Finance, Corporate).</summary>
        private static Signature CreateAmbient(ThemeChoice id)
        {
            switch (id)
            {
                case ThemeChoice.Medical: return new Heartbeat();
                case ThemeChoice.Research: return new Helix();
                case ThemeChoice.Security: return new FloorPlan();
                case ThemeChoice.Logistics: return new ShippingRoute();
                case ThemeChoice.Records: return new RedactedText();
                case ThemeChoice.Legal: return new CompanySeal();
                case ThemeChoice.Executive: return new QuarterlyBars();
                case ThemeChoice.InternalAffairs: return new Fingerprint();
                case ThemeChoice.ItDepartment: return new NetworkMap();
                case ThemeChoice.HumanResources: return new OrgChart();
                case ThemeChoice.Facilities: return new Pipework();
                case ThemeChoice.PublicRelations: return new Waveform();
                default: return null;
            }
        }

        /// <summary>Shared sizes, strengths and the text-free zones for the ambient drawings.</summary>
        private abstract class Ambient : Signature
        {
            /// <summary>Inner edge of the side zones: clear of the readout column and the subtitle.</summary>
            protected const float In = 360f;
            /// <summary>Top of the left zone, under the top-left corner line.</summary>
            protected const float LeftTop = 400f;
            /// <summary>Top of the right zone, under the memo.</summary>
            protected const float RightTop = 170f;
            /// <summary>Bottom of the side zones, above the ticker.</summary>
            protected const float Floor = -405f;
            /// <summary>The strip under the readout's last line and above the ticker.</summary>
            protected const float StripTop = -322f;
            protected const float StripBottom = -408f;

            protected float W => O._laidOutSize.x / U;
            protected float H => O._laidOutSize.y / U;
            /// <summary>Outer edge of the side zones, inside the corner brackets.</summary>
            protected float Out => W * 0.5f - 72f;
            protected float ZoneW => Out - In;
            /// <summary>Centre of the right zone; the left one is at minus this.</summary>
            protected float ZoneX => (In + Out) * 0.5f;
            protected float StripHalf => W * 0.5f - 118f;
            protected float StripY => (StripTop + StripBottom) * 0.5f;
            protected float StripH => StripTop - StripBottom;

            protected static float Top(int side) => side < 0 ? LeftTop : RightTop;

            protected Color32 Line => A(T.Accent, 70);
            protected Color32 Soft => A(T.Accent, 30);
            protected Color32 Fill => A(T.Accent, 10);

            /// <summary>A masked area, centred at (x, y): what is drawn in it stops at its edges.</summary>
            protected RectTransform Clip(float x, float y, float w, float h)
            {
                RectTransform rect = MakeRect("Clip", R);
                Place(rect, x * U, y * U, w * U, h * U);
                rect.gameObject.AddComponent<RectMask2D>();
                return rect;
            }
        }

        /// <summary>Health: a heartbeat on ECG paper along the strip, quickening when a step runs slow.</summary>
        private sealed class Heartbeat : Ambient
        {
            private const float Period = 340f;
            private const float Baseline = -12f;
            private Shape _trace;
            private float _offset;

            protected override void Build()
            {
                float half = StripHalf;
                float h = StripH * 0.5f;
                RectTransform clip = Clip(0f, StripY, half * 2f, StripH);
                Shape paper = Draw(0f, 0f, clip);
                for (float x = -half; x <= half; x += 40f)
                {
                    paper.Line(P(x, -h), P(x, h), U, A(T.Accent, 9));
                }
                for (float y = -40f; y <= 40f; y += 20f)
                {
                    paper.Line(P(-half, y), P(half, y), U, A(T.Accent, 9));
                }
                paper.Commit();

                _trace = Draw(0f, Baseline, clip);
                float start = -half - Period;
                Vector2 last = Vector2.zero;
                for (float x = start; x <= half + Period; x += 5f)
                {
                    float p = ((x - start) % Period) / Period;
                    Vector2 point = P(x, Ecg.Wave(p) * 0.72f);
                    if (x > start)
                    {
                        _trace.Line(last, point, 2f * U, Line);
                    }
                    last = point;
                }
                _trace.Commit();
            }

            internal override void Animate(float t)
            {
                float bpm = 60f + 40f * O._alert + 6f * O._beat;
                _offset = (_offset + Dt * Period * bpm / 60f) % Period;
                _trace.rectTransform.anchoredPosition = P(-_offset + Period * 0.5f, Baseline);
            }
        }

        /// <summary>Research: a double helix rising up each side of the terminal.</summary>
        private sealed class Helix : Ambient
        {
            private const float Wavelength = 360f;
            private readonly Shape[] _helices = new Shape[2];
            private float _offset;

            protected override void Build()
            {
                float amplitude = Mathf.Min(64f, ZoneW * 0.3f);
                for (int s = 0; s < 2; s++)
                {
                    int side = s == 0 ? -1 : 1;
                    float top = Top(side);
                    float h = top - Floor;
                    RectTransform clip = Clip(side * ZoneX, (top + Floor) * 0.5f, ZoneW, h);
                    Shape helix = _helices[s] = Draw(0f, 0f, clip);
                    float start = -h * 0.5f - Wavelength;
                    float phase = s * Mathf.PI * 0.5f;
                    Vector2 lastA = Vector2.zero;
                    Vector2 lastB = Vector2.zero;
                    int rung = 0;
                    for (float y = start; y <= h * 0.5f + Wavelength; y += 6f)
                    {
                        float a = (y - start) / Wavelength * Mathf.PI * 2f + phase;
                        Vector2 pa = P(Mathf.Sin(a) * amplitude, y);
                        Vector2 pb = P(Mathf.Sin(a + Mathf.PI) * amplitude, y);
                        if (y > start)
                        {
                            // The strand in front is brighter than the one behind.
                            helix.Line(lastA, pa, 2f * U, Mathf.Cos(a) > 0f ? Line : Soft);
                            helix.Line(lastB, pb, 2f * U, Mathf.Cos(a) > 0f ? Soft : Line);
                        }
                        if (rung++ % 5 == 0)
                        {
                            helix.Line(pa, pb, U, Soft);
                        }
                        lastA = pa;
                        lastB = pb;
                    }
                    helix.Commit();
                }
            }

            internal override void Animate(float t)
            {
                _offset = (_offset + Dt * (14f + 26f * O._shownFraction)) % Wavelength;
                for (int s = 0; s < 2; s++)
                {
                    _helices[s].rectTransform.anchoredPosition = P(0f, _offset - Wavelength * 0.5f);
                }
            }
        }

        /// <summary>Security: a wing of the building either side, a guard patrolling each corridor.</summary>
        private sealed class FloorPlan : Ambient
        {
            private const float Corridor = 44f;
            private readonly Shape[] _guards = new Shape[2];
            private readonly float[] _x = new float[2];
            private readonly float[] _low = new float[2];
            private readonly float[] _high = new float[2];
            private readonly float[] _walked = new float[2];

            protected override void Build()
            {
                Shape plan = Draw();
                for (int s = 0; s < 2; s++)
                {
                    int side = s == 0 ? -1 : 1;
                    float left = side < 0 ? -Out + 10f : In + 10f;
                    float right = left + ZoneW - 20f;
                    float top = Top(side) - 10f;
                    float bottom = Floor + 10f;
                    float cx = (left + right) * 0.5f;
                    Outline(plan, cx, (top + bottom) * 0.5f, right - left, top - bottom, 2f, Line);

                    // Rooms either side of a corridor down the middle, each with a door onto it.
                    Rooms(plan, left, cx - Corridor * 0.5f, top, bottom, cx - Corridor * 0.5f);
                    Rooms(plan, cx + Corridor * 0.5f, right, top, bottom, cx + Corridor * 0.5f);

                    _x[s] = cx;
                    _low[s] = bottom + 20f;
                    _high[s] = top - 20f;
                    _walked[s] = Range(0f, 1f);
                    _guards[s] = Draw();
                    _guards[s].Disc(Vector2.zero, 5f * U, A(T.Accent, 160));
                    _guards[s].Commit();
                }
                plan.Commit();
            }

            /// <summary>Rooms between x0 and x1, divided at random heights, the doors in the wall at doorX.</summary>
            private void Rooms(Shape plan, float x0, float x1, float top, float bottom, float doorX)
            {
                float y = top;
                while (y > bottom)
                {
                    float next = Mathf.Max(bottom, y - Range(90f, 150f));
                    if (next - bottom < 60f)
                    {
                        next = bottom;
                    }
                    if (next > bottom)
                    {
                        plan.Line(P(x0, next), P(x1, next), 1.5f * U, Line);
                    }
                    float door = (y + next) * 0.5f + Range(-15f, 15f);
                    plan.Line(P(doorX, y), P(doorX, door + 13f), 1.5f * U, Line);
                    plan.Line(P(doorX, door - 13f), P(doorX, next), 1.5f * U, Line);
                    y = next;
                }
            }

            internal override void Animate(float t)
            {
                for (int s = 0; s < 2; s++)
                {
                    float length = _high[s] - _low[s];
                    _walked[s] = (_walked[s] + Dt * 45f / length) % 2f;
                    float along = _walked[s] < 1f ? _walked[s] : 2f - _walked[s];
                    _guards[s].rectTransform.anchoredPosition = P(_x[s], Mathf.Lerp(_low[s], _high[s], along));
                    _guards[s].canvasRenderer.SetAlpha((t + s * 0.6f) % 1.2f < 0.8f ? 1f : 0.4f);
                }
            }
        }

        /// <summary>
        /// Logistics: a dotted shipping route down the left, along the strip and up the right, a
        /// marker travelling it as the load goes.
        /// </summary>
        private sealed class ShippingRoute : Ambient
        {
            private Vector2[] _points;
            private float[] _at;
            private Shape _marker;
            private Shape _done;
            private float _drawn = -1f;

            protected override void Build()
            {
                float jitter = ZoneW * 0.28f;
                float x = ZoneX;
                // A stop where a zone meets the strip is in both, so every leg stays in one of them.
                _points = new[]
                {
                    new Vector2(-x + Range(-jitter, jitter), LeftTop - 40f),
                    new Vector2(-x + Range(-jitter, jitter), 160f),
                    new Vector2(-x + Range(-jitter, jitter), -90f),
                    new Vector2(-x, StripY + Range(-20f, 20f)),
                    new Vector2(-StripHalf * 0.3f, StripY + Range(-25f, 25f)),
                    new Vector2(StripHalf * 0.3f, StripY + Range(-25f, 25f)),
                    new Vector2(x, StripY + Range(-20f, 20f)),
                    new Vector2(x + Range(-jitter, jitter), -60f),
                    new Vector2(x + Range(-jitter, jitter), RightTop - 30f),
                };
                _at = new float[_points.Length];
                Shape dashes = Draw();
                for (int i = 0; i < _points.Length - 1; i++)
                {
                    Vector2 a = _points[i];
                    Vector2 b = _points[i + 1];
                    float length = Vector2.Distance(a, b);
                    _at[i + 1] = _at[i] + length;
                    for (float d = 0f; d < length; d += 14f)
                    {
                        Vector2 from = Vector2.Lerp(a, b, d / length);
                        Vector2 to = Vector2.Lerp(a, b, Mathf.Min(d + 7f, length) / length);
                        dashes.Line(P(from.x, from.y), P(to.x, to.y), 1.5f * U, Soft);
                    }
                }
                foreach (Vector2 stop in _points)
                {
                    dashes.Disc(P(stop.x, stop.y), 5f * U, Line);
                }
                dashes.Commit();
                _done = Draw();
                _marker = Draw();
                _marker.Disc(Vector2.zero, 6f * U, A(T.Accent, 170));
                _marker.Commit();
            }

            internal override void Animate(float t)
            {
                float total = _at[_at.Length - 1];
                float d = Progress * total;
                int leg = 0;
                while (leg < _points.Length - 2 && d > _at[leg + 1])
                {
                    leg++;
                }
                float share = Mathf.Clamp01((d - _at[leg]) / Mathf.Max(1f, _at[leg + 1] - _at[leg]));
                Vector2 at = Vector2.Lerp(_points[leg], _points[leg + 1], share);
                _marker.rectTransform.anchoredPosition = P(at.x, at.y);
                if (Mathf.Abs(d - _drawn) < 4f)
                {
                    return;
                }
                _drawn = d;
                _done.Clear();
                for (int i = 0; i < leg; i++)
                {
                    _done.Line(P(_points[i].x, _points[i].y), P(_points[i + 1].x, _points[i + 1].y), 2f * U, Line);
                }
                _done.Line(P(_points[leg].x, _points[leg].y), P(at.x, at.y), 2f * U, Line);
                _done.Commit();
            }
        }

        /// <summary>Records: a page of document text either side, a redaction bar landing on another as the load goes.</summary>
        private sealed class RedactedText : Ambient
        {
            private readonly List<Vector4> _bars = new List<Vector4>();
            private Shape _redactions;
            private int _drawn = -1;

            protected override void Build()
            {
                Shape text = Draw();
                for (int s = 0; s < 2; s++)
                {
                    int side = s == 0 ? -1 : 1;
                    float left = side < 0 ? -Out + 10f : In + 10f;
                    float right = left + ZoneW - 20f;
                    for (float y = Top(side) - 20f; y > Floor + 10f; y -= 26f)
                    {
                        // Now and then a short line ends a paragraph.
                        float end = Rng.NextDouble() < 0.15 ? Mathf.Lerp(left, right, Range(0.3f, 0.7f)) : right;
                        float x = left;
                        while (x < end - 30f)
                        {
                            float wordEnd = Mathf.Min(x + Range(30f, 100f), end);
                            text.Line(P(x, y), P(wordEnd, y), 7f * U, A(T.Accent, 16));
                            if (Rng.NextDouble() < 0.12)
                            {
                                _bars.Add(new Vector4(x - 3f, y, wordEnd + 3f, 0f));
                            }
                            x = wordEnd + 14f;
                        }
                    }
                }
                text.Commit();
                // Shuffled, so the censor works across the pages rather than down them.
                for (int i = _bars.Count - 1; i > 0; i--)
                {
                    int j = Rng.Next(i + 1);
                    (_bars[i], _bars[j]) = (_bars[j], _bars[i]);
                }
                _redactions = Draw();
            }

            internal override void Animate(float t)
            {
                int count = Mathf.FloorToInt(Progress * _bars.Count);
                if (count == _drawn)
                {
                    return;
                }
                _drawn = count;
                _redactions.Clear();
                for (int i = 0; i < count; i++)
                {
                    Vector4 bar = _bars[i];
                    _redactions.Line(P(bar.x, bar.y), P(bar.z, bar.y), 13f * U, A(new Color32(0, 0, 0, 255), 170));
                }
                _redactions.Commit();
            }
        }

        /// <summary>
        /// Legal: the company seal, embossed faintly to the left of the terminal. Its outer ring
        /// presses in round the circle as the load goes, and the lettering on its rim appears with it.
        /// </summary>
        private sealed class CompanySeal : Ambient
        {
            private const string Rim = "TERRAGROUP HOLDINGS  //  OFFICE OF GENERAL COUNSEL  //  ";
            private Shape _ring;
            private TMPro.TextMeshProUGUI[] _letters;
            private float[] _angles;
            private int _drawn = -1;
            private float _r;

            protected override void Build()
            {
                // Sized to the zone; everything on it scales with the radius.
                _r = Mathf.Min(200f, ZoneW * 0.4f);
                float k = _r / 220f;
                float x = -ZoneX;
                float y = (LeftTop + Floor) * 0.5f;
                Shape seal = Draw(x, y);
                seal.Arc((_r + 26f * k) * U, 1.5f * U, 0f, 360f, Soft);
                seal.Arc((_r - 30f * k) * U, 2f * U, 0f, 360f, Soft);
                seal.Arc((_r - 40f * k) * U, 1f * U, 0f, 360f, Soft);
                for (int i = 0; i < 72; i++)
                {
                    seal.Line(Polar((_r - 56f * k) * U, i * 5f), Polar((_r - (i % 6 == 0 ? 70f : 62f) * k) * U, i * 5f), (i % 6 == 0 ? 2f : 1f) * U, Soft);
                }
                float r = 70f * k * U;
                float gap = 6f * k * U;
                Color32 mark = A(T.Accent, 40);
                AddPiece(seal, new Vector2(-r, 0f), new Vector2(0f, r), gap, mark, null);
                AddPiece(seal, new Vector2(0f, r), new Vector2(r, 0f), gap, mark, null);
                AddPiece(seal, new Vector2(-r, 0f), new Vector2(0f, -r), gap, mark, null);
                AddPiece(seal, new Vector2(r, 0f), new Vector2(0f, -r), gap, mark, A(T.Ink, 200));
                seal.Commit();
                _ring = Draw(x, y);

                // The rim lettering, one character at a time round the circle, upright to it.
                _letters = new TMPro.TextMeshProUGUI[Rim.Length];
                _angles = new float[Rim.Length];
                for (int i = 0; i < Rim.Length; i++)
                {
                    float angle = i * 360f / Rim.Length;
                    Vector2 at = Polar(_r * U, angle);
                    TMPro.TextMeshProUGUI letter = _letters[i] = Label(Rim[i].ToString(), 0f, 0f, 30f * k, 22f * k, A(T.Accent, 60), TMPro.TextAlignmentOptions.Center, null, 0f);
                    letter.rectTransform.anchoredPosition = P(x, y) + at;
                    letter.rectTransform.localEulerAngles = new Vector3(0f, 0f, -angle);
                    letter.canvasRenderer.SetAlpha(0f);
                    _angles[i] = angle;
                }
            }

            internal override void Animate(float t)
            {
                int degrees = Mathf.FloorToInt(Progress * 360f);
                if (degrees == _drawn)
                {
                    return;
                }
                _drawn = degrees;
                _ring.Clear();
                _ring.Arc((_r + 34f * _r / 220f) * U, 4f * U, 0f, degrees, A(T.Accent, 60));
                _ring.Commit();
                for (int i = 0; i < _letters.Length; i++)
                {
                    _letters[i].canvasRenderer.SetAlpha(_angles[i] <= degrees ? 1f : 0f);
                }
            }
        }

        /// <summary>Executive: a quarterly bar chart either side, its bars rising with the load, left to right.</summary>
        private sealed class QuarterlyBars : Ambient
        {
            private readonly List<Image> _bars = new List<Image>();
            private readonly List<float> _heights = new List<float>();

            protected override void Build()
            {
                int perSide = Mathf.Max(4, Mathf.RoundToInt(ZoneW / 56f));
                float slot = ZoneW * 0.9f / perSide;
                float tallest = RightTop - Floor - 30f;
                float trend = 0.2f;
                Shape axis = Draw();
                for (int s = 0; s < 2; s++)
                {
                    float left = (s == 0 ? -Out : In) + ZoneW * 0.05f;
                    axis.Line(P(left, Floor), P(left + ZoneW * 0.9f, Floor), 1.5f * U, Soft);
                    for (int i = 0; i < perSide; i++)
                    {
                        trend = Mathf.Clamp(trend + Range(-0.06f, 0.14f), 0.12f, 1f);
                        _heights.Add(trend * tallest);
                        Image bar = MakeImage("Bar", R, A(T.Accent, 14));
                        RectTransform rect = bar.rectTransform;
                        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                        rect.pivot = new Vector2(0.5f, 0f);
                        rect.anchoredPosition = P(left + slot * (i + 0.5f), Floor);
                        rect.sizeDelta = new Vector2(Mathf.Round(slot * 0.6f * U), 0f);
                        _bars.Add(bar);
                    }
                }
                axis.Commit();
            }

            internal override void Animate(float t)
            {
                // Bars rise left to right, each in its turn.
                for (int i = 0; i < _bars.Count; i++)
                {
                    float share = Mathf.Clamp01(Progress * _bars.Count - i);
                    float h = Mathf.Round(_heights[i] * share * U);
                    RectTransform rect = _bars[i].rectTransform;
                    if (rect.sizeDelta.y != h)
                    {
                        rect.sizeDelta = new Vector2(rect.sizeDelta.x, h);
                    }
                }
            }
        }

        /// <summary>
        /// Internal Affairs: a fingerprint to the left of the terminal, a scan line passing over
        /// it. The print is a picture (Resources/fingerprint.png, made by scripts/fingerprint.py
        /// the way synthetic fingerprint datasets are, not taken from a real person), white with
        /// the ridges in its alpha, tinted to the department.
        /// </summary>
        private sealed class Fingerprint : Ambient
        {
            private const float Aspect = 440f / 560f;
            private static Texture2D _texture;
            private Image _scan;
            private float _x;
            private float _y;
            private float _h;

            protected override void Build()
            {
                _x = -ZoneX;
                _y = (LeftTop + Floor) * 0.5f;
                _h = Mathf.Min(520f, (LeftTop - Floor) * 0.75f, ZoneW * 0.9f / Aspect);
                float w = _h * Aspect;
                Texture2D texture = Texture();
                if (texture != null)
                {
                    RawImage print = MakeRaw("Print", R, texture, A(T.Accent, 72));
                    Place(print.rectTransform, _x * U, _y * U, w * U, _h * U);
                }
                _scan = Box(_x, _y, w * 1.15f, 2f, A(T.Accent, 70));
            }

            /// <summary>The print, read from the plugin once and kept.</summary>
            private static Texture2D Texture()
            {
                if (_texture != null)
                {
                    return _texture;
                }
                try
                {
                    using (Stream stream = typeof(TerraOverlay).Assembly.GetManifestResourceStream("StartupProgress.fingerprint.png"))
                    {
                        if (stream == null)
                        {
                            return null;
                        }
                        var bytes = new byte[stream.Length];
                        int read = 0;
                        while (read < bytes.Length)
                        {
                            int n = stream.Read(bytes, read, bytes.Length - read);
                            if (n <= 0)
                            {
                                break;
                            }
                            read += n;
                        }
                        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                        if (!ImageConversion.LoadImage(texture, bytes, true))
                        {
                            UnityEngine.Object.Destroy(texture);
                            return null;
                        }
                        texture.wrapMode = TextureWrapMode.Clamp;
                        texture.filterMode = FilterMode.Trilinear;
                        texture.hideFlags = HideFlags.HideAndDontSave;
                        _texture = texture;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
                return _texture;
            }

            internal override void Animate(float t)
            {
                _scan.rectTransform.anchoredPosition = P(_x, _y + Mathf.Sin(t * 0.6f) * _h * 0.46f);
            }
        }

        /// <summary>IT: a network either side and along the strip, packets travelling between its nodes.</summary>
        private sealed class NetworkMap : Ambient
        {
            private const int Packets = 10;
            private readonly List<Vector2> _nodes = new List<Vector2>();
            private readonly List<int[]> _links = new List<int[]>();
            private readonly Shape[] _packets = new Shape[Packets];
            private readonly int[] _from = new int[Packets];
            private readonly int[] _to = new int[Packets];
            private readonly float[] _along = new float[Packets];

            protected override void Build()
            {
                // Three separate networks, one per area, so no link crosses the text; a node
                // where each side zone meets the strip belongs to both and joins them up.
                int left = Area(-Out + 20f, -In - 20f, Floor + 20f, LeftTop - 20f, 9);
                int right = Area(In + 20f, Out - 20f, Floor + 20f, RightTop - 20f, 7);
                int stripEnd = Area(-StripHalf + 20f, StripHalf - 20f, StripBottom + 14f, StripTop - 14f, Mathf.Max(5, Mathf.RoundToInt(StripHalf / 110f)));
                _nodes.Add(new Vector2(-ZoneX, StripY));
                _nodes.Add(new Vector2(ZoneX, StripY));
                Link(stripEnd, Nearest(_nodes[stripEnd], 0, left));
                Link(stripEnd + 1, Nearest(_nodes[stripEnd + 1], left, right));
                Link(stripEnd, Nearest(_nodes[stripEnd], right, stripEnd));
                Link(stripEnd + 1, Nearest(_nodes[stripEnd + 1], right, stripEnd));
                Join(0, left);
                Join(left, right);
                Join(right, stripEnd);

                Shape map = Draw();
                foreach (int[] link in _links)
                {
                    map.Line(P(_nodes[link[0]].x, _nodes[link[0]].y), P(_nodes[link[1]].x, _nodes[link[1]].y), 1.2f * U, Soft);
                }
                foreach (Vector2 node in _nodes)
                {
                    map.Disc(P(node.x, node.y), 4f * U, Line);
                }
                map.Commit();
                for (int p = 0; p < Packets; p++)
                {
                    _packets[p] = Draw();
                    _packets[p].Disc(Vector2.zero, 3.5f * U, A(T.Accent, 170));
                    _packets[p].Commit();
                    NewTrip(p);
                    _along[p] = Range(0f, 1f);
                }
            }

            /// <summary>Scatters nodes in a box; returns the index after the last.</summary>
            private int Area(float x0, float x1, float y0, float y1, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    _nodes.Add(new Vector2(Range(x0, x1), Range(y0, y1)));
                }
                return _nodes.Count;
            }

            /// <summary>Links each node in [from, to) to its two nearest neighbours in the same range.</summary>
            private void Join(int from, int to)
            {
                for (int i = from; i < to; i++)
                {
                    int a = -1, b = -1;
                    float da = float.MaxValue, db = float.MaxValue;
                    for (int j = from; j < to; j++)
                    {
                        if (j == i)
                        {
                            continue;
                        }
                        float d = Vector2.Distance(_nodes[i], _nodes[j]);
                        if (d < da) { db = da; b = a; da = d; a = j; }
                        else if (d < db) { db = d; b = j; }
                    }
                    Link(i, a);
                    Link(i, b);
                }
            }

            private int Nearest(Vector2 to, int from, int end)
            {
                int best = -1;
                float distance = float.MaxValue;
                for (int j = from; j < end; j++)
                {
                    float d = Vector2.Distance(to, _nodes[j]);
                    if (d < distance)
                    {
                        distance = d;
                        best = j;
                    }
                }
                return best;
            }

            private void Link(int a, int b)
            {
                if (a >= 0 && b >= 0 && a != b)
                {
                    _links.Add(new[] { a, b });
                }
            }

            private void NewTrip(int p)
            {
                int[] link = _links[Rng.Next(_links.Count)];
                bool flip = Rng.Next(2) == 0;
                _from[p] = flip ? link[1] : link[0];
                _to[p] = flip ? link[0] : link[1];
                _along[p] = 0f;
            }

            internal override void Animate(float t)
            {
                float speed = 0.4f + 0.8f * O._shownFraction + O._alert;
                for (int p = 0; p < Packets; p++)
                {
                    _along[p] += Dt * speed;
                    if (_along[p] >= 1f)
                    {
                        NewTrip(p);
                    }
                    Vector2 at = Vector2.Lerp(_nodes[_from[p]], _nodes[_to[p]], _along[p]);
                    _packets[p].rectTransform.anchoredPosition = P(at.x, at.y);
                }
            }
        }

        /// <summary>HR: an organisation chart either side, its positions filled one by one as the load goes.</summary>
        private sealed class OrgChart : Ambient
        {
            private const float BoxH = 28f;
            private const float Step = 112f;
            private readonly List<Vector2> _boxes = new List<Vector2>();
            private Shape _filled;
            private float _boxW;
            private int _drawn = -1;

            protected override void Build()
            {
                float spacing = ZoneW * 0.9f / 4f;
                _boxW = Mathf.Min(66f, spacing - 10f);
                float top = RightTop - 50f;
                Shape chart = Draw();
                // Both charts' positions, a level at a time, so they fill side by side.
                var levels = new List<Vector2>[4];
                for (int l = 0; l < levels.Length; l++)
                {
                    levels[l] = new List<Vector2>();
                }
                for (int s = 0; s < 2; s++)
                {
                    float cx = s == 0 ? -ZoneX : ZoneX;
                    Vector2 head = new Vector2(cx, top);
                    levels[0].Add(head);
                    for (int m = 0; m < 2; m++)
                    {
                        Vector2 manager = new Vector2(cx + (m == 0 ? -1f : 1f) * spacing, top - Step);
                        levels[1].Add(manager);
                        Connect(chart, head, manager);
                        for (int r = 0; r < 2; r++)
                        {
                            Vector2 report = new Vector2(manager.x + (r == 0 ? -0.5f : 0.5f) * spacing, top - 2f * Step);
                            Vector2 junior = new Vector2(report.x, top - 3f * Step);
                            levels[2].Add(report);
                            levels[3].Add(junior);
                            Connect(chart, manager, report);
                            Connect(chart, report, junior);
                        }
                    }
                }
                foreach (List<Vector2> level in levels)
                {
                    foreach (Vector2 box in level)
                    {
                        _boxes.Add(box);
                        Outline(chart, box.x, box.y, _boxW, BoxH, 1.5f, Line);
                    }
                }
                chart.Commit();
                _filled = Draw();
            }

            /// <summary>An elbow line from a box's bottom to the top of one below it.</summary>
            private void Connect(Shape chart, Vector2 from, Vector2 to)
            {
                float mid = (from.y - BoxH * 0.5f + to.y + BoxH * 0.5f) * 0.5f;
                chart.Line(P(from.x, from.y - BoxH * 0.5f), P(from.x, mid), U, Soft);
                chart.Line(P(from.x, mid), P(to.x, mid), U, Soft);
                chart.Line(P(to.x, mid), P(to.x, to.y + BoxH * 0.5f), U, Soft);
            }

            internal override void Animate(float t)
            {
                int count = Mathf.FloorToInt(Progress * _boxes.Count);
                if (count == _drawn)
                {
                    return;
                }
                _drawn = count;
                _filled.Clear();
                for (int i = 0; i < count; i++)
                {
                    _filled.RoundedRect(P(_boxes[i].x, _boxes[i].y), (_boxW - 4f) * U, (BoxH - 4f) * U, 2f * U, A(T.Accent, 40));
                }
                _filled.Commit();
            }
        }

        /// <summary>
        /// Facilities: the building's service pipework, a schematic. Two pipes come down the left,
        /// run along the strip and go up the right, with flanges, valves and a pressure gauge at
        /// each end; the flow in them quickens and the gauges climb as the load goes.
        /// </summary>
        private sealed class Pipework : Ambient
        {
            private const float Gauge = 18f;
            private readonly List<Vector2[]> _runs = new List<Vector2[]>();
            private readonly List<float> _lengths = new List<float>();
            private readonly List<Shape> _needles = new List<Shape>();
            private Shape _flow;
            private float _offset;
            private int _needleDrawn = -1;

            protected override void Build()
            {
                // Nested, so neither pipe crosses the other: the inner one turns higher up.
                float inner = In + ZoneW * 0.3f;
                float outer = In + ZoneW * 0.72f;
                float innerY = StripTop - 22f;
                float outerY = StripBottom + 22f;
                AddRun(new[] { new Vector2(-inner, LeftTop - 30f - Gauge), new Vector2(-inner, innerY), new Vector2(inner, innerY), new Vector2(inner, RightTop - 30f - Gauge) });
                AddRun(new[] { new Vector2(-outer, LeftTop - 30f - Gauge), new Vector2(-outer, outerY), new Vector2(outer, outerY), new Vector2(outer, RightTop - 30f - Gauge) });

                Shape pipes = Draw();
                foreach (Vector2[] run in _runs)
                {
                    for (int i = 0; i < run.Length - 1; i++)
                    {
                        // Each length overlaps the next at the bend: a square elbow fitting.
                        Vector2 dir = (run[i + 1] - run[i]).normalized;
                        Vector2 a = run[i] - (i > 0 ? dir * 5f : Vector2.zero);
                        Vector2 b = run[i + 1] + (i < run.Length - 2 ? dir * 5f : Vector2.zero);
                        pipes.Line(P(a.x, a.y), P(b.x, b.y), 10f * U, A(T.Accent, 16));
                        Flanges(pipes, run[i], run[i + 1]);
                    }
                    // A valve halfway down each upright.
                    Valve(pipes, Vector2.Lerp(run[0], run[1], 0.45f));
                    Valve(pipes, Vector2.Lerp(run[3], run[2], 0.45f));
                    // A gauge on top of each upright.
                    GaugeAt(run[0] + new Vector2(0f, Gauge));
                    GaugeAt(run[3] + new Vector2(0f, Gauge));
                }
                pipes.Commit();
                _flow = Draw();
            }

            private void AddRun(Vector2[] run)
            {
                float length = 0f;
                for (int i = 0; i < run.Length - 1; i++)
                {
                    length += Vector2.Distance(run[i], run[i + 1]);
                }
                _runs.Add(run);
                _lengths.Add(length);
            }

            /// <summary>Flange pairs across the pipe every so often along a straight length.</summary>
            private void Flanges(Shape pipes, Vector2 a, Vector2 b)
            {
                float length = Vector2.Distance(a, b);
                Vector2 dir = (b - a) / length;
                Vector2 across = new Vector2(-dir.y, dir.x) * 9f;
                for (float d = 70f; d < length - 40f; d += 150f)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        Vector2 at = a + dir * (d + k * 5f);
                        pipes.Line(P(at.x - across.x, at.y - across.y), P(at.x + across.x, at.y + across.y), 1.5f * U, Soft);
                    }
                }
            }

            /// <summary>A gate valve on an upright: two triangles tip to tip, its wheel to one side.</summary>
            private void Valve(Shape pipes, Vector2 at)
            {
                float s = 11f;
                pipes.Tri(P(at.x - s, at.y + s), P(at.x + s, at.y + s), P(at.x, at.y), Line);
                pipes.Tri(P(at.x - s, at.y - s), P(at.x + s, at.y - s), P(at.x, at.y), Line);
                float side = at.x < 0f ? 1f : -1f;
                pipes.Line(P(at.x, at.y), P(at.x + side * 20f, at.y), 1.5f * U, Line);
                pipes.Line(P(at.x + side * 20f, at.y - 9f), P(at.x + side * 20f, at.y + 9f), 2f * U, Line);
            }

            private void GaugeAt(Vector2 at)
            {
                Shape dial = Draw(at.x, at.y);
                dial.Arc(Gauge * U, 1.5f * U, 0f, 360f, Line);
                for (int i = 0; i <= 6; i++)
                {
                    float a = -120f + i * 40f;
                    dial.Line(Polar((Gauge - 3f) * U, a), Polar((Gauge - 7f) * U, a), U, Soft);
                }
                dial.Commit();
                _needles.Add(Draw(at.x, at.y));
            }

            /// <summary>The point d along a run.</summary>
            private static Vector2 Along(Vector2[] run, float d)
            {
                for (int i = 0; i < run.Length - 1; i++)
                {
                    float length = Vector2.Distance(run[i], run[i + 1]);
                    if (d <= length)
                    {
                        return Vector2.Lerp(run[i], run[i + 1], d / length);
                    }
                    d -= length;
                }
                return run[run.Length - 1];
            }

            internal override void Animate(float t)
            {
                const float spacing = 60f;
                float speed = 30f + 110f * Progress + 80f * O._alert;
                _offset = (_offset + Dt * speed) % spacing;
                _flow.Clear();
                for (int r = 0; r < _runs.Count; r++)
                {
                    Vector2[] run = _runs[r];
                    // The inner pipe flows one way, the outer the other.
                    float start = r == 0 ? _offset : spacing - _offset;
                    for (float d = start; d + 14f < _lengths[r]; d += spacing)
                    {
                        Vector2 a = Along(run, d);
                        Vector2 b = Along(run, d + 14f);
                        _flow.Line(P(a.x, a.y), P(b.x, b.y), 3f * U, A(T.Accent, 80));
                    }
                }
                _flow.Commit();

                int degrees = Mathf.RoundToInt(-120f + 240f * Mathf.Clamp01(Progress * 0.85f + 0.08f * O._alert));
                if (degrees == _needleDrawn)
                {
                    return;
                }
                _needleDrawn = degrees;
                for (int i = 0; i < _needles.Count; i++)
                {
                    Shape needle = _needles[i];
                    needle.Clear();
                    needle.Line(Vector2.zero, Polar((Gauge - 5f) * U, degrees + (i % 2 == 0 ? 0f : -6f)), 1.5f * U, A(T.Accent, 120));
                    needle.Disc(Vector2.zero, 2.5f * U, Line);
                    needle.Commit();
                }
            }
        }

        /// <summary>Communications: a broadcast's audio waveform pulsing along the strip.</summary>
        private sealed class Waveform : Ambient
        {
            private Image[] _bars;
            private float[] _seed;

            protected override void Build()
            {
                int count = Mathf.FloorToInt(StripHalf * 2f / 14f);
                float left = -(count - 1) * 7f;
                _bars = new Image[count];
                _seed = new float[count];
                for (int i = 0; i < count; i++)
                {
                    Image bar = _bars[i] = MakeImage("Level", R, A(T.Accent, 26));
                    RectTransform rect = bar.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = P(left + i * 14f, StripY);
                    rect.sizeDelta = new Vector2(Mathf.Round(6f * U), 0f);
                    _seed[i] = Range(0f, 10f);
                }
            }

            internal override void Animate(float t)
            {
                // Speech-like: a slow envelope over quicker syllables, symmetric about the line.
                float envelope = 0.5f + 0.5f * Mathf.Sin(t * 0.9f);
                float tallest = StripH - 10f;
                for (int i = 0; i < _bars.Length; i++)
                {
                    float s = _seed[i];
                    float level = Mathf.Abs(Mathf.Sin(t * 3.1f + s) * Mathf.Sin(t * 1.7f + i * 0.21f)) * (0.35f + 0.65f * envelope);
                    _bars[i].rectTransform.sizeDelta = new Vector2(_bars[i].rectTransform.sizeDelta.x, Mathf.Round((6f + level * (tallest - 6f)) * U));
                }
            }
        }
    }
}
