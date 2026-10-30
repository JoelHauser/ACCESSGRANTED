using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StartupProgress
{
    /// <summary>
    /// Each department's ambient drawing: one large, faint, slow thing behind the terminal that
    /// belongs to that department and, where it can, follows the load, the way Finance's share
    /// chart climbs. No labels, no widgets; at the chart's opacity so the logo and readout stay
    /// the subject. Built on the full-screen layer, in 1080p units around the screen's centre.
    /// </summary>
    internal partial class TerraOverlay
    {
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
                case ThemeChoice.Facilities: return new Elevation();
                case ThemeChoice.PublicRelations: return new Waveform();
                default: return null;
            }
        }

        /// <summary>Shared sizes and strengths for the ambient drawings.</summary>
        private abstract class Ambient : Signature
        {
            protected float W => O._laidOutSize.x / U;
            protected float H => O._laidOutSize.y / U;
            protected Color32 Line => A(T.Accent, 70);
            protected Color32 Soft => A(T.Accent, 30);
            protected Color32 Fill => A(T.Accent, 10);
        }

        /// <summary>Health: a heartbeat across the whole width, quickening when a step runs slow.</summary>
        private sealed class Heartbeat : Ambient
        {
            private const float Period = 340f;
            private Shape _trace;
            private float _offset;

            protected override void Build()
            {
                _trace = Draw(0f, -170f);
                float start = -W * 0.5f - Period;
                Vector2 last = Vector2.zero;
                for (float x = start; x <= W * 0.5f + Period; x += 5f)
                {
                    float p = ((x - start) % Period) / Period;
                    Vector2 point = P(x, Ecg.Wave(p) * 2.6f);
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
                _trace.rectTransform.anchoredPosition = P(-_offset + Period * 0.5f, -170f);
            }
        }

        /// <summary>Research: a double helix drifting across the lower screen.</summary>
        private sealed class Helix : Ambient
        {
            private const float Wavelength = 420f;
            private Shape _helix;
            private float _offset;

            protected override void Build()
            {
                _helix = Draw(0f, -180f);
                float amplitude = 70f;
                float start = -W * 0.5f - Wavelength;
                Vector2 lastA = Vector2.zero;
                Vector2 lastB = Vector2.zero;
                int rung = 0;
                for (float x = start; x <= W * 0.5f + Wavelength; x += 6f)
                {
                    float a = (x - start) / Wavelength * Mathf.PI * 2f;
                    Vector2 pa = P(x, Mathf.Sin(a) * amplitude);
                    Vector2 pb = P(x, Mathf.Sin(a + Mathf.PI) * amplitude);
                    if (x > start)
                    {
                        // The strand in front is brighter than the one behind.
                        _helix.Line(lastA, pa, 2f * U, Mathf.Cos(a) > 0f ? Line : Soft);
                        _helix.Line(lastB, pb, 2f * U, Mathf.Cos(a) > 0f ? Soft : Line);
                    }
                    if (rung++ % 5 == 0)
                    {
                        _helix.Line(pa, pb, U, Soft);
                    }
                    lastA = pa;
                    lastB = pb;
                }
                _helix.Commit();
            }

            internal override void Animate(float t)
            {
                _offset = (_offset + Dt * (18f + 30f * O._shownFraction)) % Wavelength;
                _helix.rectTransform.anchoredPosition = P(-_offset + Wavelength * 0.5f, -180f);
            }
        }

        /// <summary>Security: a building's floor plan, a patrol walking its corridors.</summary>
        private sealed class FloorPlan : Ambient
        {
            private Shape _patrol;
            private Vector2[] _route;
            private float _routeLength;
            private float _walked;

            protected override void Build()
            {
                float w = W * 0.86f;
                float h = 380f;
                float cy = -170f;
                Shape plan = Draw();
                Outline(plan, 0f, cy, w, h, 2f, Line);
                // Rooms either side of a ring corridor.
                float corridor = 40f;
                float innerW = w - 2f * corridor - 120f;
                float innerH = h - 2f * corridor - 100f;
                Outline(plan, 0f, cy, innerW, innerH, 1.5f, Line);
                int rooms = Mathf.Max(4, Mathf.RoundToInt(w / 220f));
                for (int i = 1; i < rooms; i++)
                {
                    float x = -w * 0.5f + w * i / rooms;
                    plan.Line(P(x, cy + h * 0.5f), P(x, cy + innerH * 0.5f + corridor), 1.5f * U, Line);
                    plan.Line(P(x, cy - h * 0.5f), P(x, cy - innerH * 0.5f - corridor), 1.5f * U, Line);
                }
                for (int i = 1; i < rooms - 1; i++)
                {
                    float x = -innerW * 0.5f + innerW * i / (rooms - 1);
                    plan.Line(P(x, cy + innerH * 0.5f), P(x, cy - innerH * 0.5f), U, Soft);
                }
                plan.Commit();

                // The patrol goes round the corridor.
                float rx = innerW * 0.5f + corridor * 0.5f;
                float ry = innerH * 0.5f + corridor * 0.5f;
                _route = new[] { new Vector2(-rx, cy + ry), new Vector2(rx, cy + ry), new Vector2(rx, cy - ry), new Vector2(-rx, cy - ry), new Vector2(-rx, cy + ry) };
                for (int i = 0; i < _route.Length - 1; i++)
                {
                    _routeLength += Vector2.Distance(_route[i], _route[i + 1]);
                }
                _patrol = Draw();
                _patrol.Disc(Vector2.zero, 5f * U, A(T.Accent, 160));
                _patrol.Commit();
            }

            internal override void Animate(float t)
            {
                _walked = (_walked + Dt * 60f) % _routeLength;
                float d = _walked;
                for (int i = 0; i < _route.Length - 1; i++)
                {
                    float length = Vector2.Distance(_route[i], _route[i + 1]);
                    if (d <= length)
                    {
                        Vector2 at = Vector2.Lerp(_route[i], _route[i + 1], d / length);
                        _patrol.rectTransform.anchoredPosition = P(at.x, at.y);
                        break;
                    }
                    d -= length;
                }
                _patrol.canvasRenderer.SetAlpha(t % 1.2f < 0.8f ? 1f : 0.4f);
            }
        }

        /// <summary>Logistics: a dotted shipping route, a marker travelling it as the load goes.</summary>
        private sealed class ShippingRoute : Ambient
        {
            private Vector2[] _points;
            private Shape _marker;
            private Shape _done;
            private int _drawn = -1;

            protected override void Build()
            {
                int stops = 6;
                _points = new Vector2[stops];
                for (int i = 0; i < stops; i++)
                {
                    float x = Mathf.Lerp(-W * 0.44f, W * 0.44f, i / (stops - 1f));
                    _points[i] = new Vector2(x, -170f + Range(-110f, 110f));
                }
                Shape dashes = Draw();
                for (int i = 0; i < stops - 1; i++)
                {
                    Vector2 a = _points[i];
                    Vector2 b = _points[i + 1];
                    float length = Vector2.Distance(a, b);
                    for (float d = 0f; d < length; d += 14f)
                    {
                        Vector2 from = Vector2.Lerp(a, b, d / length);
                        Vector2 to = Vector2.Lerp(a, b, Mathf.Min(d + 7f, length) / length);
                        dashes.Line(P(from.x, from.y), P(to.x, to.y), 1.5f * U, Soft);
                    }
                    dashes.Disc(P(a.x, a.y), 5f * U, Line);
                }
                dashes.Disc(P(_points[stops - 1].x, _points[stops - 1].y), 5f * U, Line);
                dashes.Commit();
                _done = Draw();
                _marker = Draw();
                _marker.Disc(Vector2.zero, 6f * U, A(T.Accent, 170));
                _marker.Commit();
            }

            internal override void Animate(float t)
            {
                float along = Progress * (_points.Length - 1);
                int leg = Mathf.Min(Mathf.FloorToInt(along), _points.Length - 2);
                Vector2 at = Vector2.Lerp(_points[leg], _points[leg + 1], along - leg);
                _marker.rectTransform.anchoredPosition = P(at.x, at.y);
                if (leg != _drawn)
                {
                    _drawn = leg;
                    _done.Clear();
                    for (int i = 0; i < leg; i++)
                    {
                        _done.Line(P(_points[i].x, _points[i].y), P(_points[i + 1].x, _points[i + 1].y), 2f * U, Line);
                    }
                    _done.Commit();
                }
            }
        }

        /// <summary>Records: rows of document text, a redaction bar landing on another as the load goes.</summary>
        private sealed class RedactedText : Ambient
        {
            private readonly List<Vector4> _bars = new List<Vector4>();
            private Shape _redactions;
            private int _drawn = -1;

            protected override void Build()
            {
                Shape text = Draw();
                float left = -W * 0.45f;
                float right = W * 0.45f;
                for (float y = -40f; y > -390f; y -= 26f)
                {
                    float x = left;
                    while (x < right - 40f)
                    {
                        float word = Range(30f, 110f);
                        float end = Mathf.Min(x + word, right);
                        text.Line(P(x, y), P(end, y), 7f * U, A(T.Accent, 16));
                        if (Rng.NextDouble() < 0.12)
                        {
                            _bars.Add(new Vector4(x - 3f, y, end + 3f, 0f));
                        }
                        x = end + 14f;
                    }
                }
                text.Commit();
                // Shuffled, so the censor works across the page rather than down it.
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
        /// Legal: the company seal, embossed faintly beside the terminal. Its outer ring presses
        /// in round the circle as the load goes, and the lettering on its rim appears with it.
        /// </summary>
        private sealed class CompanySeal : Ambient
        {
            private const string Rim = "TERRAGROUP HOLDINGS  //  OFFICE OF GENERAL COUNSEL  //  ";
            private const float Radius = 220f;
            private Shape _ring;
            private TMPro.TextMeshProUGUI[] _letters;
            private float[] _angles;
            private int _drawn = -1;
            private float _x;
            private float _y;

            protected override void Build()
            {
                _x = W * 0.3f;
                _y = -50f;
                Shape seal = Draw(_x, _y);
                seal.Arc((Radius + 26f) * U, 1.5f * U, 0f, 360f, Soft);
                seal.Arc((Radius - 30f) * U, 2f * U, 0f, 360f, Soft);
                seal.Arc((Radius - 40f) * U, 1f * U, 0f, 360f, Soft);
                for (int i = 0; i < 72; i++)
                {
                    seal.Line(Polar((Radius - 56f) * U, i * 5f), Polar((Radius - (i % 6 == 0 ? 70f : 62f)) * U, i * 5f), (i % 6 == 0 ? 2f : 1f) * U, Soft);
                }
                float r = 70f * U;
                float gap = 6f * U;
                Color32 mark = A(T.Accent, 40);
                AddPiece(seal, new Vector2(-r, 0f), new Vector2(0f, r), gap, mark, null);
                AddPiece(seal, new Vector2(0f, r), new Vector2(r, 0f), gap, mark, null);
                AddPiece(seal, new Vector2(-r, 0f), new Vector2(0f, -r), gap, mark, null);
                AddPiece(seal, new Vector2(r, 0f), new Vector2(0f, -r), gap, mark, A(T.Ink, 200));
                seal.Commit();
                _ring = Draw(_x, _y);

                // The rim lettering, one character at a time round the circle, upright to it.
                _letters = new TMPro.TextMeshProUGUI[Rim.Length];
                _angles = new float[Rim.Length];
                for (int i = 0; i < Rim.Length; i++)
                {
                    float angle = i * 360f / Rim.Length;
                    Vector2 at = Polar(Radius * U, angle);
                    TMPro.TextMeshProUGUI letter = _letters[i] = Label(Rim[i].ToString(), 0f, 0f, 30f, 22f, A(T.Accent, 60), TMPro.TextAlignmentOptions.Center, null, 0f);
                    letter.rectTransform.anchoredPosition = new Vector2(_x * U, _y * U) + at;
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
                _ring.Arc((Radius + 34f) * U, 4f * U, 0f, degrees, A(T.Accent, 60));
                _ring.Commit();
                for (int i = 0; i < _letters.Length; i++)
                {
                    _letters[i].canvasRenderer.SetAlpha(_angles[i] <= degrees ? 1f : 0f);
                }
            }
        }


        /// <summary>Executive: a quarterly bar chart, its bars rising with the load.</summary>
        private sealed class QuarterlyBars : Ambient
        {
            private Image[] _bars;
            private float[] _heights;

            protected override void Build()
            {
                int count = Mathf.Max(10, Mathf.RoundToInt(W / 110f));
                float slot = W * 0.88f / count;
                float left = -W * 0.44f;
                float floor = -400f;
                Shape axis = Draw();
                axis.Line(P(left, floor), P(-left, floor), 1.5f * U, Soft);
                axis.Commit();
                _bars = new Image[count];
                _heights = new float[count];
                float trend = 90f;
                for (int i = 0; i < count; i++)
                {
                    trend += Range(-20f, 40f);
                    _heights[i] = Mathf.Clamp(trend, 60f, 330f);
                    Image bar = _bars[i] = MakeImage("Bar", R, A(T.Accent, 14));
                    RectTransform rect = bar.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = P(left + slot * (i + 0.5f), floor);
                    rect.sizeDelta = new Vector2(Mathf.Round(slot * 0.6f * U), 0f);
                }
            }

            internal override void Animate(float t)
            {
                // Bars rise left to right, each in its turn.
                for (int i = 0; i < _bars.Length; i++)
                {
                    float share = Mathf.Clamp01(Progress * _bars.Length - i);
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
        /// Internal Affairs: a large fingerprint, a scan line passing over it. The ridges are the
        /// contour lines of one smooth height field (a gentle slope with a rise at the core), so
        /// like real ridges they never cross: closed whorls at the core, arches sweeping over it,
        /// all inside the fingertip's outline and fading toward its edge.
        /// </summary>
        private sealed class Fingerprint : Ambient
        {
            private const float Rx = 165f;
            private const float Ry = 220f;
            private const float Spacing = 10f;
            private const float Cell = 5f;
            private Image _scan;
            private float _x;

            protected override void Build()
            {
                _x = W * 0.3f;
                float cy = -60f;
                Shape print = Draw(_x, cy);

                int nx = Mathf.CeilToInt(2f * Rx / Cell) + 1;
                int ny = Mathf.CeilToInt(2f * Ry / Cell) + 1;
                var field = new float[nx, ny];
                for (int i = 0; i < nx; i++)
                {
                    for (int j = 0; j < ny; j++)
                    {
                        field[i, j] = Height(-Rx + i * Cell, -Ry + j * Cell);
                    }
                }

                // Marching squares: in each cell, a segment for every ridge level crossing it.
                for (int i = 0; i < nx - 1; i++)
                {
                    for (int j = 0; j < ny - 1; j++)
                    {
                        float x0 = -Rx + i * Cell;
                        float y0 = -Ry + j * Cell;
                        float a = field[i, j], b = field[i + 1, j], c = field[i + 1, j + 1], d = field[i, j + 1];
                        float low = Mathf.Min(Mathf.Min(a, b), Mathf.Min(c, d));
                        float high = Mathf.Max(Mathf.Max(a, b), Mathf.Max(c, d));
                        for (float level = Mathf.Ceil(low / Spacing) * Spacing; level <= high; level += Spacing)
                        {
                            var hits = new List<Vector2>(4);
                            Cross(hits, a, b, level, new Vector2(x0, y0), new Vector2(x0 + Cell, y0));
                            Cross(hits, b, c, level, new Vector2(x0 + Cell, y0), new Vector2(x0 + Cell, y0 + Cell));
                            Cross(hits, c, d, level, new Vector2(x0 + Cell, y0 + Cell), new Vector2(x0, y0 + Cell));
                            Cross(hits, d, a, level, new Vector2(x0, y0 + Cell), new Vector2(x0, y0));
                            for (int h = 0; h + 1 < hits.Count; h += 2)
                            {
                                Segment(print, hits[h], hits[h + 1]);
                            }
                        }
                    }
                }
                print.Commit();
                print.rectTransform.localEulerAngles = new Vector3(0f, 0f, -8f);
                _scan = Box(_x, cy, 400f, 3f, A(T.Accent, 60));
            }

            /// <summary>The field the ridges follow: a slope, a rise at the core, a faint ripple.</summary>
            private static float Height(float x, float y)
            {
                float dx = x - 4f;
                float dy = y - 40f;
                float core = 210f * Mathf.Exp(-(dx * dx / (2f * 60f * 60f) + dy * dy / (2f * 80f * 80f)));
                return y + 0.14f * x + core + 2.5f * Mathf.Sin(x * 0.03f + y * 0.017f);
            }

            private static void Cross(List<Vector2> hits, float va, float vb, float level, Vector2 pa, Vector2 pb)
            {
                if ((va < level) != (vb < level))
                {
                    hits.Add(Vector2.Lerp(pa, pb, (level - va) / (vb - va)));
                }
            }

            private void Segment(Shape print, Vector2 a, Vector2 b)
            {
                float e = Mathf.Max(a.x * a.x / (Rx * Rx) + a.y * a.y / (Ry * Ry), b.x * b.x / (Rx * Rx) + b.y * b.y / (Ry * Ry));
                if (e > 1f || Rng.NextDouble() < 0.02)
                {
                    return;
                }
                byte alpha = (byte)Mathf.Lerp(48f, 10f, e * e);
                print.Line(P(a.x, a.y), P(b.x, b.y), 2.4f * U, A(T.Accent, alpha));
            }

            internal override void Animate(float t)
            {
                float y = -60f + Mathf.Sin(t * 0.6f) * 230f;
                _scan.rectTransform.anchoredPosition = P(_x, y);
            }
        }

        /// <summary>IT: a network map, packets travelling between its nodes.</summary>
        private sealed class NetworkMap : Ambient
        {
            private const int Packets = 10;
            private Vector2[] _nodes;
            private int[][] _links;
            private readonly Shape[] _packets = new Shape[Packets];
            private readonly int[] _from = new int[Packets];
            private readonly int[] _to = new int[Packets];
            private readonly float[] _along = new float[Packets];

            protected override void Build()
            {
                int count = 22;
                _nodes = new Vector2[count];
                for (int i = 0; i < count; i++)
                {
                    _nodes[i] = new Vector2(Range(-W * 0.46f, W * 0.46f), Range(-400f, 40f));
                }
                var links = new List<int[]>();
                Shape map = Draw();
                for (int i = 0; i < count; i++)
                {
                    // Each node links to its two nearest neighbours.
                    int a = -1, b = -1;
                    float da = float.MaxValue, db = float.MaxValue;
                    for (int j = 0; j < count; j++)
                    {
                        if (j == i)
                        {
                            continue;
                        }
                        float d = Vector2.Distance(_nodes[i], _nodes[j]);
                        if (d < da) { db = da; b = a; da = d; a = j; }
                        else if (d < db) { db = d; b = j; }
                    }
                    foreach (int j in new[] { a, b })
                    {
                        links.Add(new[] { i, j });
                        map.Line(P(_nodes[i].x, _nodes[i].y), P(_nodes[j].x, _nodes[j].y), 1.2f * U, Soft);
                    }
                    map.Disc(P(_nodes[i].x, _nodes[i].y), 4f * U, Line);
                }
                map.Commit();
                _links = links.ToArray();
                for (int p = 0; p < Packets; p++)
                {
                    _packets[p] = Draw();
                    _packets[p].Disc(Vector2.zero, 3.5f * U, A(T.Accent, 170));
                    _packets[p].Commit();
                    NewTrip(p);
                    _along[p] = Range(0f, 1f);
                }
            }

            private void NewTrip(int p)
            {
                int[] link = _links[Rng.Next(_links.Length)];
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

        /// <summary>HR: an organisation chart, its positions filled one by one as the load goes.</summary>
        private sealed class OrgChart : Ambient
        {
            private readonly List<Vector2> _boxes = new List<Vector2>();
            private Shape _filled;
            private int _drawn = -1;

            protected override void Build()
            {
                float bw = 70f;
                float bh = 34f;
                Shape chart = Draw();
                Vector2 top = new Vector2(0f, -40f);
                _boxes.Add(top);
                int heads = 3;
                float span = W * 0.6f;
                for (int h = 0; h < heads; h++)
                {
                    float hx = Mathf.Lerp(-span * 0.5f, span * 0.5f, h / (heads - 1f));
                    Vector2 head = new Vector2(hx, -150f);
                    _boxes.Add(head);
                    chart.Line(P(top.x, top.y - bh * 0.5f), P(top.x, -95f), U, Soft);
                    chart.Line(P(hx, -95f), P(hx, head.y + bh * 0.5f), U, Soft);
                    int staff = 4;
                    float sub = span / heads * 0.85f;
                    for (int s = 0; s < staff; s++)
                    {
                        float sx = hx + Mathf.Lerp(-sub * 0.5f, sub * 0.5f, s / (staff - 1f));
                        Vector2 member = new Vector2(sx, -270f - (s % 2) * 60f);
                        _boxes.Add(member);
                        chart.Line(P(hx, head.y - bh * 0.5f), P(sx, member.y + bh * 0.5f), U, Soft);
                    }
                }
                chart.Line(P(-span * 0.5f, -95f), P(span * 0.5f, -95f), U, Soft);
                foreach (Vector2 box in _boxes)
                {
                    Outline(chart, box.x, box.y, bw, bh, 1.5f, Line);
                }
                chart.Commit();
                _filled = Draw();
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
                    _filled.RoundedRect(P(_boxes[i].x, _boxes[i].y), 66f * U, 30f * U, 2f * U, A(T.Accent, 40));
                }
                _filled.Commit();
            }
        }

        /// <summary>
        /// Facilities: the headquarters tower in elevation, a shorter annex beside it. Every
        /// floor's windows are drawn faintly; they light floor by floor from the lobby up as the
        /// load goes.
        /// </summary>
        private sealed class Elevation : Ambient
        {
            private const int Floors = 15;
            private const float FloorH = 36f;
            private const float Bottom = -380f;
            private const float TowerW = 250f;
            private const int Columns = 6;
            private Shape _lights;
            private int _drawn = -1;
            private float _x;

            protected override void Build()
            {
                _x = W * 0.3f;
                float top = Bottom + 30f + Floors * FloorH;
                Shape building = Draw();

                // The annex, lower and to the left, its windows always dark.
                float annexW = 150f;
                float annexX = _x - TowerW * 0.5f - annexW * 0.5f - 10f;
                float annexTop = Bottom + 30f + 7 * FloorH;
                Outline(building, annexX, (Bottom + annexTop) * 0.5f, annexW, annexTop - Bottom, 1.5f, Soft);
                for (int f = 0; f < 7; f++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        building.RoundedRect(P(annexX - 44f + c * 44f, Bottom + 30f + f * FloorH + FloorH * 0.5f), 22f * U, 18f * U, 2f * U, A(T.Accent, 12));
                    }
                }

                // The tower: lobby, floors, a setback crown, an antenna.
                Outline(building, _x, (Bottom + top) * 0.5f, TowerW, top - Bottom, 2f, Line);
                building.Line(P(_x - TowerW * 0.5f, Bottom + 30f), P(_x + TowerW * 0.5f, Bottom + 30f), 1.5f * U, Soft);
                Outline(building, _x, Bottom + 13f, 44f, 26f, 1.5f, Soft);
                for (int f = 0; f < Floors; f++)
                {
                    float y = Bottom + 30f + f * FloorH + FloorH * 0.5f;
                    for (int c = 0; c < Columns; c++)
                    {
                        building.RoundedRect(P(Window(c), y), 26f * U, 20f * U, 2f * U, A(T.Accent, 14));
                    }
                }
                float crownW = TowerW * 0.62f;
                Outline(building, _x, top + 24f, crownW, 48f, 1.5f, Line);
                Outline(building, _x, top + 60f, crownW * 0.5f, 24f, 1.5f, Soft);
                building.Line(P(_x, top + 72f), P(_x, top + 140f), 2f * U, Soft);
                building.Commit();
                _lights = Draw();
            }

            private float Window(int column) => _x - TowerW * 0.5f + 28f + column * (TowerW - 56f) / (Columns - 1);

            internal override void Animate(float t)
            {
                int lit = Mathf.FloorToInt(Progress * Floors);
                if (lit == _drawn)
                {
                    return;
                }
                _drawn = lit;
                _lights.Clear();
                for (int f = 0; f < lit; f++)
                {
                    float y = Bottom + 30f + f * FloorH + FloorH * 0.5f;
                    for (int c = 0; c < Columns; c++)
                    {
                        _lights.RoundedRect(P(Window(c), y), 26f * U, 20f * U, 2f * U, A(T.Accent, 50));
                    }
                }
                _lights.Commit();
            }
        }

        /// <summary>Communications: a broadcast's audio waveform pulsing along the bottom.</summary>
        private sealed class Waveform : Ambient
        {
            private Image[] _bars;
            private float[] _seed;

            protected override void Build()
            {
                int count = Mathf.RoundToInt(W * 0.9f / 14f);
                float left = -W * 0.45f;
                _bars = new Image[count];
                _seed = new float[count];
                for (int i = 0; i < count; i++)
                {
                    Image bar = _bars[i] = MakeImage("Level", R, A(T.Accent, 26));
                    RectTransform rect = bar.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = P(left + i * 14f, -230f);
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
                    _bars[i].rectTransform.sizeDelta = new Vector2(_bars[i].rectTransform.sizeDelta.x, Mathf.Round((8f + level * 150f) * U));
                }
            }
        }
    }
}
