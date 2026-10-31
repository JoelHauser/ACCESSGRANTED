using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ACCESSGRANTED
{
    internal partial class TerraOverlay
    {
        /// <summary>
        /// Finance: the trading desk. TGRP's candlestick chart forms as the load goes, the last
        /// candle live; the order book flickers beside it; each finished step is an executed
        /// trade.
        /// </summary>
        private sealed class TradingDesk : Scene
        {
            private const int Candles = 40;
            private readonly float[] _open = new float[Candles];
            private readonly float[] _close = new float[Candles];
            private readonly float[] _high = new float[Candles];
            private readonly float[] _low = new float[Candles];
            private float _min;
            private float _max;
            private float _chartLeft;
            private float _chartWidth;
            private float _chartTop;
            private float _chartHeight;
            /// <summary>A line chart with a shaded area, instead of candles.</summary>
            private bool _line;
            private Shape _candles;
            private Shape _volume;
            private Shape _forming;
            private int _shown = -1;
            private float _nextTick;
            private float _live;
            private TextMeshProUGUI _price;
            private TextMeshProUGUI _change;
            private TextMeshProUGUI _session;
            private TextMeshProUGUI _trades;
            private Image[] _depth;
            private float _depthWidth;
            private float _nextBook;
            private int _tradesKey = -1;
            private DateTime[] _executed;
            private int[] _state;
            private Image _bar;
            private float _barWidth;
            private readonly StringBuilder _builder = new StringBuilder();

            protected override void Build()
            {
                float cw = Mathf.Min(2f * HW - 160f, 1700f);
                float left = -cw * 0.5f;
                float right = cw * 0.5f;
                string[][] instruments =
                {
                    new[] { "TGRP", "TERRAGROUP HOLDINGS  //  NORVINSK EXCHANGE" },
                    new[] { "TG-LABS", "TERRAGROUP LABORATORIES  //  RESEARCH SUBSIDIARY" },
                    new[] { "TG-PHARMA", "TERRAGROUP PHARMACEUTICALS  //  HEALTH SUBSIDIARY" },
                    new[] { "NVSK IDX", "NORVINSK COMPOSITE INDEX  //  REGIONAL MARKET" }
                };
                string[] instrument = instruments[Rng.Next(instruments.Length)];
                LabelAt(instrument[0], -HW + 80f, 404f, 300f, 30f, T.Text, spacing: 10f);
                LabelAt(instrument[1], -HW + 80f, 374f, 700f, 12f, T.Dim);
                _line = Rng.Next(2) == 0;
                _price = LabelAt(string.Empty, left, 330f, 300f, 34f, T.Text);
                _change = LabelAt(string.Empty, left + 230f, 326f, 300f, 16f, T.Accent);

                // A random walk that climbs: the load is good news.
                float price = Range(360f, 420f);
                _min = float.MaxValue;
                _max = float.MinValue;
                for (int k = 0; k < Candles; k++)
                {
                    _open[k] = price;
                    price *= 1f + Range(-0.012f, 0.016f);
                    _close[k] = price;
                    _high[k] = Mathf.Max(_open[k], _close[k]) * (1f + Range(0.001f, 0.008f));
                    _low[k] = Mathf.Min(_open[k], _close[k]) * (1f - Range(0.001f, 0.008f));
                    _min = Mathf.Min(_min, _low[k]);
                    _max = Mathf.Max(_max, _high[k]);
                }
                float pad = (_max - _min) * 0.08f;
                _min -= pad;
                _max += pad;

                _chartLeft = left;
                _chartWidth = cw * 0.64f;
                _chartTop = 290f;
                _chartHeight = 470f;
                Box(_chartLeft + _chartWidth * 0.5f, _chartTop - _chartHeight * 0.5f, _chartWidth, _chartHeight, A(Dark, 235));
                Shape grid = Draw();
                for (int g = 0; g <= 5; g++)
                {
                    float y = _chartTop - g * _chartHeight / 5f;
                    grid.Line(P(_chartLeft, y), P(_chartLeft + _chartWidth, y), U, A(T.Dim, 30));
                    float value = Mathf.Lerp(_max, _min, g / 5f);
                    LabelAt(value.ToString("0.00"), _chartLeft + _chartWidth + 6f, y, 80f, 10f, T.Dim);
                }
                for (int g = 1; g < 8; g++)
                {
                    float x = _chartLeft + g * _chartWidth / 8f;
                    grid.Line(P(x, _chartTop), P(x, _chartTop - _chartHeight), U, A(T.Dim, 18));
                }
                grid.Commit();
                _volume = Draw();
                _candles = Draw();
                _forming = Draw();

                // The order book and the trades, to the right.
                float bookLeft = _chartLeft + _chartWidth + 100f;
                float bookWidth = right - bookLeft;
                LabelAt("ORDER BOOK", bookLeft, 290f, bookWidth, 12f, T.Dim, spacing: 10f);
                _depth = new Image[10];
                _depthWidth = bookWidth * U;
                for (int i = 0; i < 10; i++)
                {
                    bool ask = i < 5;
                    float y = ask ? 258f - i * 24f : 128f - (i - 5) * 24f;
                    _depth[i] = Bar(bookLeft, y, 18f, A(ask ? T.Warn : T.Accent, 40));
                    float level = ask ? 1f + (5 - i) * 0.0015f : 1f - (i - 4) * 0.0015f;
                    LabelAt((_close[Candles - 1] * level).ToString("0.00"), bookLeft + 6f, y, bookWidth * 0.5f, 12f, ask ? T.Warn : T.Accent);
                }
                LabelAt("EXECUTED", bookLeft, -6f, bookWidth, 12f, T.Dim, spacing: 10f);
                _trades = Label(string.Empty, bookLeft + bookWidth * 0.5f, -120f, bookWidth, 11f, T.Text);
                _trades.rectTransform.sizeDelta = P(bookWidth, 210f);
                _trades.alignment = TextAlignmentOptions.TopLeft;
                _trades.lineSpacing = 14f;
                _executed = new DateTime[Steps];
                _state = new int[Steps];
                for (int i = 0; i < Steps; i++)
                {
                    _state[i] = -1;
                }

                Status(left, -300f, cw * 0.6f, 18f, false);
                _session = LabelAt(string.Empty, right - 420f, -300f, 400f, 13f, T.Dim, TextAlignmentOptions.Right);
                Box(0f, -334f, cw, 4f, T.Faint);
                _bar = Bar(left, -334f, 4f, T.Accent);
                _barWidth = cw * U;
            }

            private float Y(float value) => _chartTop - (_max - value) / (_max - _min) * _chartHeight;

            private void DrawCandle(Shape shape, int k, float close)
            {
                float slot = _chartWidth / Candles;
                float x = _chartLeft + (k + 0.5f) * slot;
                float open = _open[k];
                Color32 color = close >= open ? T.Accent : T.Warn;
                shape.Line(P(x, Y(Mathf.Max(_high[k], close))), P(x, Y(Mathf.Min(_low[k], close))), 1.2f * U, color);
                float top = Y(Mathf.Max(open, close));
                float bottom = Mathf.Min(Y(Mathf.Min(open, close)), top - 1.5f);
                shape.Line(P(x, top), P(x, bottom), slot * 0.6f * U, color);
            }

            internal override void Animate(float t)
            {
                int shown = Mathf.Clamp(1 + Mathf.FloorToInt(Progress * (Candles - 1)), 1, Candles);
                if (shown != _shown)
                {
                    _shown = shown;
                    _candles.Clear();
                    _volume.Clear();
                    float slot = _chartWidth / Candles;
                    for (int k = 0; k < shown - 1; k++)
                    {
                        if (_line)
                        {
                            float x0 = _chartLeft + (k + 0.5f) * slot;
                            float x1 = _chartLeft + (k + 1.5f) * slot;
                            float floor = _chartTop - _chartHeight;
                            _candles.Polygon(new[] { P(x0, floor), P(x0, Y(_open[k])), P(x1, Y(_close[k])), P(x1, floor) }, A(T.Accent, 22));
                            _candles.Line(P(x0, Y(_open[k])), P(x1, Y(_close[k])), 2f * U, T.Accent);
                        }
                        else
                        {
                            DrawCandle(_candles, k, _close[k]);
                        }
                        float x = _chartLeft + (k + 0.5f) * slot;
                        float h = 10f + Mathf.Abs(_close[k] - _open[k]) / (_max - _min) * 300f;
                        _volume.Line(P(x, _chartTop - _chartHeight), P(x, _chartTop - _chartHeight + Mathf.Min(h, 60f)), slot * 0.6f * U, A(T.Dim, 50));
                    }
                    _candles.Commit();
                    _volume.Commit();
                }

                // The candle still forming wobbles about its close.
                if (t >= _nextTick)
                {
                    _nextTick = t + 0.15f;
                    int k = shown - 1;
                    _live = _close[k] * (1f + Range(-0.004f, 0.004f));
                    _forming.Clear();
                    if (_line)
                    {
                        float slot = _chartWidth / Candles;
                        _forming.Line(P(_chartLeft + (k + 0.5f) * slot, Y(_open[k])), P(_chartLeft + (k + 1.5f) * slot, Y(_live)), 2f * U, T.Accent);
                        _forming.Disc(P(_chartLeft + (k + 1.5f) * slot, Y(_live)), 4f * U, T.Text);
                    }
                    else
                    {
                        DrawCandle(_forming, k, _live);
                    }
                    _forming.Commit();
                    float change = (_live - _open[0]) / _open[0] * 100f;
                    _price.text = _live.ToString("0.00");
                    _change.text = (change >= 0f ? "+" : string.Empty) + change.ToString("0.00") + "%";
                    _change.color = change >= 0f ? T.Accent : T.Warn;
                }

                if (t >= _nextBook)
                {
                    _nextBook = t + 0.3f;
                    for (int n = 0; n < 3; n++)
                    {
                        Image depth = _depth[Rng.Next(_depth.Length)];
                        depth.rectTransform.sizeDelta = new Vector2(Mathf.Round(_depthWidth * Range(0.15f, 0.95f)), depth.rectTransform.sizeDelta.y);
                    }
                }

                // Each finished step is a trade.
                int key = StartupTracker.Current * 10 + (Granted ? 1 : 0);
                for (int i = First; i < Steps; i++)
                {
                    int state = State(i);
                    if (state != _state[i])
                    {
                        if (state == Done)
                        {
                            _executed[i] = DateTime.Now;
                        }
                        _state[i] = state;
                    }
                }
                if (key != _tradesKey)
                {
                    _tradesKey = key;
                    _builder.Length = 0;
                    int lines = 0;
                    for (int i = Steps - 1; i >= First && lines < 8; i--)
                    {
                        if (_state[i] != Done)
                        {
                            continue;
                        }
                        lines++;
                        _builder.Append("<color=").Append(T.AccentHex).Append(">BUY</color>  ")
                            .Append(_executed[i].ToString("HH:mm:ss")).Append("  ")
                            .Append(StepName(i)).Append("  ")
                            .Append(StartupTracker.TookSeconds(i).ToString("0.0")).Append("S\n");
                    }
                    _trades.text = _builder.ToString();
                    _session.text = Granted ? "MARKET CLOSED  //  POSITION FILLED" : $"MARKET OPEN  //  {Mathf.FloorToInt(Progress * 100f)}% FILLED";
                }
                _bar.rectTransform.sizeDelta = new Vector2(Mathf.Round(_barWidth * Progress), _bar.rectTransform.sizeDelta.y);
                MirrorStatus();
            }
        }

        /// <summary>
        /// Security: the monitor wall. Eight camera feeds (a drawn corridor
        /// with someone walking up it) around an access-control panel where each load step is a badge scan.
        /// </summary>
        private sealed class MonitorWall : Scene
        {
            private const int Cells = 9;
            private int Middle;
            private readonly Shape[] _corridors = new Shape[Cells];
            private readonly Shape[] _walkers = new Shape[Cells];
            private readonly TextMeshProUGUI[] _times = new TextMeshProUGUI[Cells];
            private readonly Image[] _scans = new Image[Cells];
            private readonly Vector2[] _centre = new Vector2[Cells];
            private float _cellW;
            private float _cellH;
            private int _dead;
            private TextMeshProUGUI _noSignal;
            private Image _noSignalCover;
            private TextMeshProUGUI _log;
            private int _logKey = -1;
            private int _second = -1;
            private DateTime[] _scanned;
            private int[] _state;
            private readonly StringBuilder _builder = new StringBuilder();

            protected override void Build()
            {
                BrandMark(-HW + 80f, 398f, "SECURITY DEPARTMENT");
                int[] spots = { 4, 4, 0, 2, 6, 8 };
                Middle = spots[Rng.Next(spots.Length)];
                float tw = Mathf.Min(2f * HW - 160f, 1600f);
                float gap = 12f;
                _cellW = (tw - 2f * gap) / 3f;
                _cellH = (720f - 2f * gap) / 3f;
                _dead = (Middle + 1 + Rng.Next(Cells - 1)) % Cells;
                Shape frames = Draw();
                for (int c = 0; c < Cells; c++)
                {
                    float x = -tw * 0.5f + _cellW * 0.5f + (c % 3) * (_cellW + gap);
                    float y = 340f - _cellH * 0.5f - (c / 3) * (_cellH + gap);
                    _centre[c] = new Vector2(x, y);
                    Box(x, y, _cellW, _cellH, A(Dark, 245));
                    Outline(frames, x, y, _cellW, _cellH, 1f, A(T.Accent, (byte)(c == Middle ? 160 : 80)));
                    if (c == Middle)
                    {
                        continue;
                    }

                    RectTransform view = MakeRect("View", R);
                    view.anchoredPosition = P(x, y);
                    view.sizeDelta = P(_cellW - 2f, _cellH - 2f);
                    view.gameObject.AddComponent<RectMask2D>();

                    float hx = _cellW * 0.5f;
                    float hy = _cellH * 0.5f;
                    float ix = hx * 0.45f;
                    float iy = hy * 0.43f;
                    Shape corridor = _corridors[c] = Draw(0f, 0f, view);
                    corridor.Line(P(-hx, hy), P(-ix, iy), U, A(T.Dim, 70));
                    corridor.Line(P(hx, hy), P(ix, iy), U, A(T.Dim, 70));
                    corridor.Line(P(-hx, -hy), P(-ix, -iy), U, A(T.Dim, 70));
                    corridor.Line(P(hx, -hy), P(ix, -iy), U, A(T.Dim, 70));
                    Outline(corridor, 0f, 0f, ix * 2f, iy * 2f, 1f, A(T.Dim, 70));
                    corridor.Commit();
                    Shape walker = _walkers[c] = Draw(0f, 0f, view);
                    walker.Disc(P(0f, 15f), 4.5f * U, A(T.Dim, 210));
                    walker.Polygon(new[] { P(-6f, 9f), P(6f, 9f), P(4f, -8f), P(-4f, -8f) }, A(T.Dim, 210));
                    walker.Line(P(-2.5f, -8f), P(-3.5f, -22f), 2.5f * U, A(T.Dim, 210));
                    walker.Line(P(2.5f, -8f), P(3.5f, -22f), 2.5f * U, A(T.Dim, 210));
                    walker.Commit();

                    LabelAt($"CAM {c + 1:00}", x - hx + 10f, y + hy - 14f, _cellW - 70f, 10f, T.Dim);
                    _times[c] = LabelAt(string.Empty, x + hx - 130f, y - hy + 14f, 120f, 10f, T.Accent, TextAlignmentOptions.Right);
                    _scans[c] = Box(x, y, _cellW - 2f, 5f, A(T.Accent, 26));
                }
                frames.Commit();

                // The access-control panel in the middle.
                Vector2 m = _centre[Middle];
                Box(m.x, m.y, _cellW - 2f, _cellH - 2f, A(Color32.Lerp(T.Ink, T.Primary, 0.12f), 250));
                float left = m.x - _cellW * 0.5f + 14f;
                string[] gates = { "ACCESS CONTROL  //  GATE 3", "ACCESS CONTROL  //  LABS LIFT", "ACCESS CONTROL  //  LOADING BAY", "ACCESS CONTROL  //  EXECUTIVE FLOOR" };
                LabelAt(gates[Rng.Next(gates.Length)], left, m.y + _cellH * 0.5f - 16f, _cellW - 28f, 11f, T.Accent, spacing: 8f);
                _log = Label(string.Empty, left + (_cellW - 28f) * 0.5f, m.y + 4f, _cellW - 28f, 10f, T.Text);
                _log.rectTransform.sizeDelta = P(_cellW - 28f, _cellH - 70f);
                _log.alignment = TextAlignmentOptions.BottomLeft;
                _log.lineSpacing = 10f;
                _log.overflowMode = TextOverflowModes.Truncate;
                Status(left, m.y - _cellH * 0.5f + 16f, _cellW - 28f, 13f, true);

                _noSignalCover = Box(_centre[_dead].x, _centre[_dead].y, _cellW - 2f, _cellH - 2f, A(Dark, 252));
                _noSignal = Label("NO SIGNAL", _centre[_dead].x, _centre[_dead].y, 200f, 16f, T.Warn, TextAlignmentOptions.Center);
                _scanned = new DateTime[Steps];
                _state = new int[Steps];
                for (int i = 0; i < Steps; i++)
                {
                    _state[i] = -1;
                }
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
                        if (time != null)
                        {
                            time.text = stamp;
                        }
                    }
                }

                for (int c = 0; c < Cells; c++)
                {
                    if (c == Middle)
                    {
                        continue;
                    }
                    float sweep = (t * 0.5f + c * 0.19f) % 1f;
                    _scans[c].rectTransform.anchoredPosition = P(_centre[c].x, _centre[c].y + _cellH * 0.5f - 4f - sweep * (_cellH - 8f));
                    Walk(c, t);
                }
                float cycle = t % 7f;
                bool lost = cycle > 4f && cycle < 5.6f;
                SetActive(_noSignal.gameObject, lost);
                SetActive(_noSignalCover.gameObject, lost);

                // The access log: each finished step is a granted badge scan.
                for (int i = First; i < Steps; i++)
                {
                    int state = State(i);
                    if (state != _state[i])
                    {
                        _state[i] = state;
                        _scanned[i] = DateTime.Now;
                    }
                }
                int key = StartupTracker.Current * 4 + (O._slow ? 1 : 0) + (Granted ? 2 : 0);
                if (key != _logKey)
                {
                    _logKey = key;
                    _builder.Length = 0;
                    for (int i = First; i < Steps; i++)
                    {
                        int state = _state[i];
                        if (state == Pending)
                        {
                            break;
                        }
                        string verdict = state == Done ? $"<color={T.AccentHex}>GRANTED</color>"
                            : state == Skipped ? "WAIVED"
                            : state == Slow ? $"<color={T.WarnHex}>HOLD</color>"
                            : "SCANNING...";
                        _builder.Append(_scanned[i].ToString("HH:mm:ss")).Append("  ").Append(StepName(i)).Append("  ").Append(verdict).Append('\n');
                    }
                    _log.text = _builder.ToString();
                }
                MirrorStatus();
            }

            private void Walk(int c, float t)
            {
                float p = ((t + c * 1.3f) % 7f) / 7f;
                float scale = _cellH / 112f;
                RectTransform rect = _walkers[c].rectTransform;
                rect.anchoredPosition = P(Mathf.Lerp(0f, 18f * (c % 2 == 0 ? 1f : -1f), p) * scale, (Mathf.Lerp(6f, -26f, p) + Mathf.Abs(Mathf.Sin(t * 7f)) * 1.2f * p) * scale);
                rect.localScale = Vector3.one * (Mathf.Lerp(0.3f, 1.8f, p * p) * scale);
                _walkers[c].canvasRenderer.SetAlpha(p < 0.85f ? Mathf.Clamp01(p / 0.1f) : (1f - p) / 0.15f);
            }
        }
    }
}
