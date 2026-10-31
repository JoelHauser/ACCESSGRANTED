using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ACCESSGRANTED
{
    /// <summary>
    /// Each department's ambient drawing: one large, faint, slow thing behind the terminal that
    /// belongs to that department and, where it can, follows the load, the way Finance's share
    /// chart climbs. No widgets; at the chart's opacity so the logo and readout stay the subject.
    ///
    /// Nothing is drawn under text. Each layout names the areas its text leaves free (ArtZones):
    /// two zones, and a strip under the readout above the ticker that both zones reach down to,
    /// so a drawing can run from one zone along the strip into the other without crossing text.
    /// The drawings size themselves to those areas. Built on the full-screen layer, in 1080p
    /// units around the screen's centre.
    /// </summary>
    internal partial class TerraOverlay
    {
        private const float ArtFloor = -405f;
        private const float ArtStripTop = -322f;
        private const float ArtStripBottom = -408f;
        /// <summary>Under the top corner lines.</summary>
        private const float ArtTop = 400f;
        /// <summary>Under the memo in the top-right corner.</summary>
        private const float ArtUnderMemo = 170f;

        /// <summary>
        /// The text-free areas of the current layout, in 1080p units, y up: a left and a right
        /// zone, both reaching down to the strip. False where the layout leaves no room (the dock,
        /// and screens too narrow for zones beside the text).
        /// </summary>
        private bool ArtZones(out Rect left, out Rect right, out Rect strip)
        {
            float w = _laidOutSize.x / _u;
            float edge = w * 0.5f - 72f;
            float stripHalf = w * 0.5f - 118f;
            left = right = strip = default;
            strip = Rect.MinMaxRect(-stripHalf, ArtStripBottom, stripHalf, ArtStripTop);
            switch (_layout)
            {
                case LayoutKind.Centered:
                    // Either side of the logo and readout column (within 320 of the centre line).
                    if (edge - 360f < 180f)
                    {
                        return false;
                    }
                    left = Rect.MinMaxRect(-edge, ArtFloor, -360f, ArtTop);
                    right = Rect.MinMaxRect(360f, ArtFloor, edge, ArtUnderMemo);
                    return true;
                case LayoutKind.Sidebar:
                {
                    // The open area right of the sign-in panel, in two halves.
                    float open = -w * 0.5f + 620f + 40f;
                    if (edge - open < 440f)
                    {
                        return false;
                    }
                    float middle = (open + edge) * 0.5f;
                    left = Rect.MinMaxRect(open, ArtFloor, middle - 20f, ArtTop);
                    right = Rect.MinMaxRect(middle + 20f, ArtFloor, edge, ArtUnderMemo);
                    strip = Rect.MinMaxRect(open, ArtStripBottom, Mathf.Min(edge, stripHalf), ArtStripTop);
                    return true;
                }
                case LayoutKind.Split:
                case LayoutKind.SplitMirror:
                {
                    // Under the logo and wordmark on one side, under the readout on the other.
                    bool brandLeft = _layout == LayoutKind.Split;
                    const float UnderBrand = -160f;
                    const float UnderReadout = -120f;
                    left = Rect.MinMaxRect(-edge, ArtFloor, -30f, brandLeft ? UnderBrand : UnderReadout);
                    right = Rect.MinMaxRect(30f, ArtFloor, edge, brandLeft ? UnderReadout : UnderBrand);
                    return true;
                }
                default:
                    return false;
            }
        }

        private bool AmbientRoom => ArtZones(out _, out _, out _);

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

        /// <summary>Shared strengths, shapes and the text-free areas for the ambient drawings.</summary>
        private abstract class Ambient : Signature
        {
            private bool _zoned;
            private Rect _left;
            private Rect _right;
            private Rect _strip;

            private void Zones()
            {
                if (!_zoned)
                {
                    O.ArtZones(out _left, out _right, out _strip);
                    _zoned = true;
                }
            }

            /// <summary>The left zone.</summary>
            protected Rect Lz { get { Zones(); return _left; } }
            /// <summary>The right zone.</summary>
            protected Rect Rz { get { Zones(); return _right; } }
            /// <summary>The strip under the readout, above the ticker.</summary>
            protected Rect Sz { get { Zones(); return _strip; } }
            protected Rect Zone(int side) => side == 0 ? Lz : Rz;
            /// <summary>The taller zone, for the one big piece; the other gets the rest.</summary>
            protected Rect Tall => Lz.height >= Rz.height ? Lz : Rz;
            protected Rect Other => Lz.height >= Rz.height ? Rz : Lz;

            protected override bool Seeded => true;

            protected Color32 Line => A(T.Accent, 70);
            protected Color32 Soft => A(T.Accent, 30);
            protected Color32 Fill => A(T.Accent, 10);

            /// <summary>A masked area: what is drawn in it stops at its edges. Draw in it relative to its centre.</summary>
            protected RectTransform Clip(Rect area)
            {
                RectTransform rect = MakeRect("Clip", R);
                Place(rect, area.center.x * U, area.center.y * U, area.width * U, area.height * U);
                rect.gameObject.AddComponent<RectMask2D>();
                return rect;
            }

            /// <summary>A head-and-shoulders silhouette filling a photo h tall, centred at (x, y).</summary>
            protected void Person(Shape shape, float x, float y, float h, Color32 color)
            {
                shape.Disc(P(x, y + 0.16f * h), 0.2f * h * U, color);
                float r = 0.17f * h * U;
                shape.RoundedRect(P(x, y - 0.3f * h), 0.72f * h * U, 0.34f * h * U, r, r, 0f, 0f, color);
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
                Rect strip = Sz;
                float half = strip.width * 0.5f;
                float h = strip.height * 0.5f;
                RectTransform clip = Clip(strip);
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

        /// <summary>Research: a double helix rising up each zone.</summary>
        private sealed class Helix : Ambient
        {
            private const float Wavelength = 360f;
            private readonly Shape[] _helices = new Shape[2];
            private float _offset;

            protected override void Build()
            {
                for (int s = 0; s < 2; s++)
                {
                    Rect zone = Zone(s);
                    float amplitude = Mathf.Min(64f, zone.width * 0.3f);
                    float h = zone.height;
                    RectTransform clip = Clip(zone);
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

        /// <summary>Security: a wing of the building in each zone, a guard patrolling each corridor.</summary>
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
                    Rect zone = Zone(s);
                    float left = zone.xMin + 10f;
                    float right = zone.xMax - 10f;
                    float top = zone.yMax - 10f;
                    float bottom = zone.yMin + 10f;
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
        /// Logistics: a dotted shipping route down the left zone, along the strip and up the right
        /// one, a marker travelling it as the load goes.
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
                Rect l = Lz;
                Rect r = Rz;
                float y = Sz.center.y;
                float jl = l.width * 0.28f;
                float jr = r.width * 0.28f;
                // A stop where a zone meets the strip is in both, so every leg stays in one of them.
                _points = new[]
                {
                    new Vector2(l.center.x + Range(-jl, jl), Mathf.Lerp(l.yMin, l.yMax, 0.92f)),
                    new Vector2(l.center.x + Range(-jl, jl), Mathf.Lerp(l.yMin, l.yMax, 0.62f)),
                    new Vector2(l.center.x + Range(-jl, jl), Mathf.Lerp(l.yMin, l.yMax, 0.32f)),
                    new Vector2(l.center.x, y + Range(-20f, 20f)),
                    new Vector2(Mathf.Lerp(l.center.x, r.center.x, 1f / 3f), y + Range(-25f, 25f)),
                    new Vector2(Mathf.Lerp(l.center.x, r.center.x, 2f / 3f), y + Range(-25f, 25f)),
                    new Vector2(r.center.x, y + Range(-20f, 20f)),
                    new Vector2(r.center.x + Range(-jr, jr), Mathf.Lerp(r.yMin, r.yMax, 0.45f)),
                    new Vector2(r.center.x + Range(-jr, jr), Mathf.Lerp(r.yMin, r.yMax, 0.92f)),
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

        /// <summary>Records: a page of document text in each zone, a redaction bar landing on another as the load goes.</summary>
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
                    Rect zone = Zone(s);
                    float left = zone.xMin + 10f;
                    float right = zone.xMax - 10f;
                    for (float y = zone.yMax - 20f; y > zone.yMin + 10f; y -= 26f)
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
        /// Legal: the company seal, embossed faintly in the taller zone. Its outer ring presses in
        /// round the circle as the load goes, and the lettering on its rim appears with it.
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
                Rect zone = Tall;
                _r = Mathf.Min(200f, zone.width * 0.4f, zone.height * 0.4f);
                float k = _r / 220f;
                float x = zone.center.x;
                float y = zone.center.y;
                Shape seal = Draw(x, y);
                seal.Arc((_r + 26f * k) * U, 1.5f * U, 0f, 360f, Soft);
                seal.Arc((_r - 30f * k) * U, 2f * U, 0f, 360f, Soft);
                seal.Arc((_r - 40f * k) * U, 1f * U, 0f, 360f, Soft);
                for (int i = 0; i < 72; i++)
                {
                    seal.Line(Polar((_r - 56f * k) * U, i * 5f), Polar((_r - (i % 6 == 0 ? 70f : 62f) * k) * U, i * 5f), (i % 6 == 0 ? 2f : 1f) * U, Soft);
                }
                float r = 70f * k * U;
                float gap = r * LogoGap;
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

        /// <summary>Executive: a quarterly bar chart in each zone, its bars rising with the load, left to right.</summary>
        private sealed class QuarterlyBars : Ambient
        {
            private readonly List<Image> _bars = new List<Image>();
            private readonly List<float> _heights = new List<float>();

            protected override void Build()
            {
                float tallest = Mathf.Min(Lz.height, Rz.height) - 30f;
                float trend = 0.2f;
                Shape axis = Draw();
                for (int s = 0; s < 2; s++)
                {
                    Rect zone = Zone(s);
                    int count = Mathf.Max(4, Mathf.RoundToInt(zone.width / 56f));
                    float slot = zone.width * 0.9f / count;
                    float left = zone.xMin + zone.width * 0.05f;
                    axis.Line(P(left, zone.yMin), P(left + zone.width * 0.9f, zone.yMin), 1.5f * U, Soft);
                    for (int i = 0; i < count; i++)
                    {
                        trend = Mathf.Clamp(trend + Range(-0.06f, 0.14f), 0.12f, 1f);
                        _heights.Add(trend * tallest);
                        Image bar = MakeImage("Bar", R, A(T.Accent, 14));
                        RectTransform rect = bar.rectTransform;
                        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                        rect.pivot = new Vector2(0.5f, 0f);
                        rect.anchoredPosition = P(left + slot * (i + 0.5f), zone.yMin);
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
        /// Internal Affairs: a fingerprint in the taller zone, a scan line passing over it, and an
        /// investigation board in the other: photos pinned up round the subject, string run
        /// between them as the load goes, the subject circled at the end. The print is a picture
        /// (Resources/fingerprint.png, made by scripts/fingerprint.py the way synthetic
        /// fingerprint datasets are, not taken from a real person), white with the ridges in its
        /// alpha, tinted to the department.
        /// </summary>
        private sealed class Fingerprint : Ambient
        {
            private const float Aspect = 440f / 560f;
            private const float CardW = 72f;
            private const float CardH = 90f;
            private static Texture2D _texture;
            private Image _scan;
            private float _x;
            private float _y;
            private float _h;
            private Vector2[] _pins;
            private int[][] _links;
            private Shape _strings;
            private Shape _ring;
            private float _ringR;
            private int _drawn = -1;

            protected override void Build()
            {
                BuildPrint(Tall);
                BuildBoard(Other);
            }

            private void BuildPrint(Rect zone)
            {
                _x = zone.center.x;
                _y = zone.center.y;
                _h = Mathf.Min(520f, zone.height * 0.75f, zone.width * 0.9f / Aspect);
                float w = _h * Aspect;
                Texture2D texture = Texture();
                if (texture != null)
                {
                    RawImage print = MakeRaw("Print", R, texture, A(T.Accent, 72));
                    Place(print.rectTransform, _x * U, _y * U, w * U, _h * U);
                }
                _scan = Box(_x, _y, w * 1.15f, 2f, A(T.Accent, 70));
            }

            /// <summary>Five photos, the subject larger in the middle, each with a pin and a redacted name.</summary>
            private void BuildBoard(Rect zone)
            {
                float cx = zone.center.x;
                float cy = zone.center.y;
                float dx = zone.width * 0.3f;
                float dy = zone.height * 0.3f;
                // Smaller cards in a smaller zone, so none overlap.
                float scale = Mathf.Clamp(Mathf.Min(zone.height / 560f, zone.width / 500f), 0.55f, 1.1f);
                float jx = 15f * scale;
                float jy = 15f * scale;
                var cards = new[]
                {
                    new Vector2(cx, cy),
                    new Vector2(cx - dx + Range(-jx, jx), cy + dy + Range(-jy, 0f)),
                    new Vector2(cx + dx + Range(-jx, jx), cy + dy + Range(-jy, 0f)),
                    new Vector2(cx + dx + Range(-jx, jx), cy - dy + Range(0f, jy)),
                    new Vector2(cx - dx + Range(-jx, jx), cy - dy + Range(0f, jy)),
                };
                Shape board = Draw();
                _pins = new Vector2[cards.Length];
                for (int i = 0; i < cards.Length; i++)
                {
                    float k = (i == 0 ? 1.25f : 1f) * scale;
                    float w = CardW * k;
                    float h = CardH * k;
                    Vector2 c = cards[i];
                    board.RoundedRect(P(c.x, c.y), w * U, h * U, 2f * U, A(T.Accent, 12));
                    Outline(board, c.x, c.y, w, h, 1.5f, Soft);
                    float photo = w - 12f * k;
                    float photoY = c.y + h * 0.5f - 6f * k - photo * 0.5f;
                    board.RoundedRect(P(c.x, photoY), photo * U, photo * U, U, A(T.Accent, 14));
                    Person(board, c.x, photoY, photo, A(T.Accent, 40));
                    // The name, redacted.
                    float nameY = c.y - h * 0.5f + 11f * k;
                    board.Line(P(c.x - w * 0.3f, nameY), P(c.x + w * 0.3f, nameY), 7f * k * U, A(new Color32(0, 0, 0, 255), 150));
                    _pins[i] = new Vector2(c.x, c.y + h * 0.5f - 3f);
                    board.Disc(P(_pins[i].x, _pins[i].y), 4f * U, A(T.Warn, 150));
                }
                board.Commit();

                // Every photo to the subject, then two between the others.
                _links = new[] { new[] { 0, 1 }, new[] { 0, 3 }, new[] { 0, 2 }, new[] { 1, 2 }, new[] { 0, 4 }, new[] { 3, 4 } };
                _strings = Draw();
                _ring = Draw(cards[0].x, cards[0].y);
                _ringR = CardH * 1.25f * scale * 0.68f;
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
                    using (Stream stream = typeof(TerraOverlay).Assembly.GetManifestResourceStream("ACCESSGRANTED.fingerprint.png"))
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

                // The string runs out link by link over the first 85% of the load; then the subject is circled.
                int step = Mathf.FloorToInt(Progress * 400f);
                if (step == _drawn)
                {
                    return;
                }
                _drawn = step;
                float strung = Mathf.Clamp01(Progress / 0.85f) * _links.Length;
                _strings.Clear();
                for (int i = 0; i < _links.Length && i < strung; i++)
                {
                    Vector2 a = _pins[_links[i][0]];
                    Vector2 b = Vector2.Lerp(a, _pins[_links[i][1]], Mathf.Clamp01(strung - i));
                    _strings.Line(P(a.x, a.y), P(b.x, b.y), 1.5f * U, A(T.Warn, 90));
                }
                _strings.Commit();
                float ring = Mathf.Clamp01((Progress - 0.85f) / 0.15f);
                _ring.Clear();
                if (ring > 0f)
                {
                    _ring.Arc(_ringR * U, 2.5f * U, -40f, 360f * ring, A(T.Warn, 120));
                }
                _ring.Commit();
            }
        }

        /// <summary>IT: a network in each zone and along the strip, packets travelling between its nodes.</summary>
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
                // where each zone meets the strip belongs to both and joins them up.
                int left = Area(Lz, 20f);
                int right = Area(Rz, 20f);
                int stripEnd = Area(Sz, 14f);
                _nodes.Add(new Vector2(Lz.center.x, Sz.center.y));
                _nodes.Add(new Vector2(Rz.center.x, Sz.center.y));
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

            /// <summary>Scatters nodes in an area, by its size; returns the index after the last.</summary>
            private int Area(Rect area, float inset)
            {
                int count = Mathf.Clamp(Mathf.RoundToInt(area.width * area.height / 32000f), 4, 10);
                for (int i = 0; i < count; i++)
                {
                    _nodes.Add(new Vector2(Range(area.xMin + inset, area.xMax - inset), Range(area.yMin + inset, area.yMax - inset)));
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

        /// <summary>
        /// HR: an organisation chart in each zone, staff photo cards whose vacancies fill one by
        /// one as the load goes, and a headcount row along the strip filling with it.
        /// </summary>
        private sealed class OrgChart : Ambient
        {
            private const float Icon = 26f;
            private const float IconStep = 24f;
            // x, y, width, height of each card, in the order they fill.
            private readonly List<Vector4> _cards = new List<Vector4>();
            private Vector2 _headcountStart;
            private int _headcount;
            private Shape _filled;
            private Shape _hired;
            private int _drawn = -1;
            private int _hiredDrawn = -1;

            protected override void Build()
            {
                // Both charts from the same height, so they sit level. Where there is room they
                // stop above the strip and the headcount runs along it; where not, they take the
                // whole zone and there is no headcount.
                float top = Mathf.Min(Lz.yMax, Rz.yMax);
                bool headcount = top - Sz.yMax >= 260f;
                float floor = headcount ? Sz.yMax + 10f : Mathf.Max(Lz.yMin, Rz.yMin);
                Shape chart = Draw();
                var levels = new List<Vector4>[4];
                for (int l = 0; l < levels.Length; l++)
                {
                    levels[l] = new List<Vector4>();
                }
                for (int s = 0; s < 2; s++)
                {
                    Rect zone = Zone(s);
                    float height = top - floor;
                    int depth = height >= 380f ? 4 : 3;
                    float spacing = zone.width * 0.9f / 4f;
                    float step = Mathf.Min(112f, (height - 40f) / (depth - 1 + 0.9f));
                    float w = Mathf.Min(58f, spacing - 12f);
                    float h = Mathf.Min(w * 1.1f, step - 24f);
                    float cx = zone.center.x;
                    float y0 = top - 20f - h * 0.5f;
                    var head = new Vector4(cx, y0, w, h);
                    levels[0].Add(head);
                    for (int m = 0; m < 2; m++)
                    {
                        var manager = new Vector4(cx + (m == 0 ? -1f : 1f) * spacing, y0 - step, w, h);
                        levels[1].Add(manager);
                        Connect(chart, head, manager);
                        for (int r = 0; r < 2; r++)
                        {
                            var report = new Vector4(manager.x + (r == 0 ? -0.5f : 0.5f) * spacing, y0 - 2f * step, w, h);
                            levels[2].Add(report);
                            Connect(chart, manager, report);
                            if (depth == 4)
                            {
                                var junior = new Vector4(report.x, y0 - 3f * step, w, h);
                                levels[3].Add(junior);
                                Connect(chart, report, junior);
                            }
                        }
                    }
                }
                foreach (List<Vector4> level in levels)
                {
                    foreach (Vector4 card in level)
                    {
                        _cards.Add(card);
                        // A vacancy: the outline and a faint silhouette.
                        Outline(chart, card.x, card.y, card.z, card.w, 1.5f, Soft);
                        float photo = Mathf.Min(card.z - 10f, card.w - 16f);
                        Person(chart, card.x, PhotoY(card, photo), photo, A(T.Accent, 16));
                    }
                }

                // The headcount: a row of figures along the strip.
                Rect strip = Sz;
                _headcount = headcount ? Mathf.Max(0, Mathf.FloorToInt((strip.width - 20f) / IconStep)) : 0;
                _headcountStart = new Vector2(strip.center.x - (_headcount - 1) * IconStep * 0.5f, strip.center.y);
                for (int i = 0; i < _headcount; i++)
                {
                    Person(chart, _headcountStart.x + i * IconStep, _headcountStart.y, Icon, A(T.Accent, 16));
                }
                chart.Commit();
                _filled = Draw();
                _hired = Draw();
            }

            private static float PhotoY(Vector4 card, float photo) => card.y + card.w * 0.5f - 5f - photo * 0.5f;

            /// <summary>An elbow line from a card's bottom to the top of one below it.</summary>
            private void Connect(Shape chart, Vector4 from, Vector4 to)
            {
                float bottom = from.y - from.w * 0.5f;
                float topOf = to.y + to.w * 0.5f;
                float mid = (bottom + topOf) * 0.5f;
                chart.Line(P(from.x, bottom), P(from.x, mid), U, Soft);
                chart.Line(P(from.x, mid), P(to.x, mid), U, Soft);
                chart.Line(P(to.x, mid), P(to.x, topOf), U, Soft);
            }

            internal override void Animate(float t)
            {
                int count = Mathf.FloorToInt(Progress * _cards.Count);
                if (count != _drawn)
                {
                    _drawn = count;
                    _filled.Clear();
                    for (int i = 0; i < count; i++)
                    {
                        Vector4 card = _cards[i];
                        _filled.RoundedRect(P(card.x, card.y), (card.z - 3f) * U, (card.w - 3f) * U, 2f * U, A(T.Accent, 22));
                        float photo = Mathf.Min(card.z - 10f, card.w - 16f);
                        Person(_filled, card.x, PhotoY(card, photo), photo, A(T.Accent, 75));
                        float nameY = card.y - card.w * 0.5f + 6f;
                        _filled.Line(P(card.x - card.z * 0.3f, nameY), P(card.x + card.z * 0.3f, nameY), 3f * U, A(T.Accent, 60));
                    }
                    _filled.Commit();
                }

                int hired = Mathf.FloorToInt(Progress * _headcount);
                if (hired != _hiredDrawn)
                {
                    _hiredDrawn = hired;
                    _hired.Clear();
                    for (int i = 0; i < hired; i++)
                    {
                        Person(_hired, _headcountStart.x + i * IconStep, _headcountStart.y, Icon, A(T.Accent, 60));
                    }
                    _hired.Commit();
                }
            }
        }

        /// <summary>
        /// Facilities: the building's service pipework, a schematic. Two pipes come down the left
        /// zone, run along the strip and go up the right one, with flanges, valves and a pressure
        /// gauge at each end; the flow in them quickens and the gauges climb as the load goes.
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
                Rect l = Lz;
                Rect r = Rz;
                float leftInner = l.xMax - l.width * 0.3f;
                float leftOuter = l.xMax - l.width * 0.72f;
                float rightInner = r.xMin + r.width * 0.3f;
                float rightOuter = r.xMin + r.width * 0.72f;
                float leftTop = l.yMax - 30f - Gauge;
                float rightTop = r.yMax - 30f - Gauge;
                float innerY = Sz.yMax - 22f;
                float outerY = Sz.yMin + 22f;
                AddRun(new[] { new Vector2(leftInner, leftTop), new Vector2(leftInner, innerY), new Vector2(rightInner, innerY), new Vector2(rightInner, rightTop) });
                AddRun(new[] { new Vector2(leftOuter, leftTop), new Vector2(leftOuter, outerY), new Vector2(rightOuter, outerY), new Vector2(rightOuter, rightTop) });

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
                    // A valve halfway down each upright, its wheel towards the middle.
                    Valve(pipes, Vector2.Lerp(run[0], run[1], 0.45f), 1f);
                    Valve(pipes, Vector2.Lerp(run[3], run[2], 0.45f), -1f);
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
            private void Valve(Shape pipes, Vector2 at, float side)
            {
                float s = 11f;
                pipes.Tri(P(at.x - s, at.y + s), P(at.x + s, at.y + s), P(at.x, at.y), Line);
                pipes.Tri(P(at.x - s, at.y - s), P(at.x + s, at.y - s), P(at.x, at.y), Line);
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
            private float _tallest;

            protected override void Build()
            {
                Rect strip = Sz;
                _tallest = strip.height - 10f;
                int count = Mathf.FloorToInt(strip.width / 14f);
                float left = strip.center.x - (count - 1) * 7f;
                _bars = new Image[count];
                _seed = new float[count];
                for (int i = 0; i < count; i++)
                {
                    Image bar = _bars[i] = MakeImage("Level", R, A(T.Accent, 26));
                    RectTransform rect = bar.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = P(left + i * 14f, strip.center.y);
                    rect.sizeDelta = new Vector2(Mathf.Round(6f * U), 0f);
                    _seed[i] = Range(0f, 10f);
                }
            }

            internal override void Animate(float t)
            {
                // Speech-like: a slow envelope over quicker syllables, symmetric about the line.
                float envelope = 0.5f + 0.5f * Mathf.Sin(t * 0.9f);
                for (int i = 0; i < _bars.Length; i++)
                {
                    float s = _seed[i];
                    float level = Mathf.Abs(Mathf.Sin(t * 3.1f + s) * Mathf.Sin(t * 1.7f + i * 0.21f)) * (0.35f + 0.65f * envelope);
                    _bars[i].rectTransform.sizeDelta = new Vector2(_bars[i].rectTransform.sizeDelta.x, Mathf.Round((6f + level * (_tallest - 6f)) * U));
                }
            }
        }
    }
}
