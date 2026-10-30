using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StartupProgress
{
    /// <summary>
    /// Departments with a scene of their own: instead of the shared dashboard, the whole screen
    /// becomes that department's world (a departures board, a patient monitor, a news
    /// broadcast...) and the load's steps are told in its language. Built on a full-screen layer
    /// in 1080p units around the screen's centre; the corner texts, ticker, notices, intro,
    /// ending and glitches stay shared. Each scene mirrors the readout's current line, so the
    /// grant and the fake denial play out in it too.
    /// </summary>
    internal partial class TerraOverlay
    {
        private bool _sceneActive;

        private static Scene CreateScene(ThemeChoice id)
        {
            switch (id)
            {
                case ThemeChoice.Corporate: return new LobbyWelcome();
                case ThemeChoice.Holiday: return new GreetingCard();
                case ThemeChoice.Logistics: return new Departures();
                case ThemeChoice.Medical: return new PatientMonitor();
                case ThemeChoice.PublicRelations: return new Newsroom();
                case ThemeChoice.Facilities: return new LiftLobby();
                case ThemeChoice.Finance: return new TradingDesk();
                case ThemeChoice.Security: return new MonitorWall();
                case ThemeChoice.Records: return new LaunchRecord();
                case ThemeChoice.Legal: return new Agreement();
                case ThemeChoice.Executive: return new BoardDeck();
                case ThemeChoice.InternalAffairs: return new CaseTimeline();
                case ThemeChoice.ItDepartment: return new TicketBoard();
                case ThemeChoice.HumanResources: return new Onboarding();
                case ThemeChoice.Research: return new Experiment();
                default: return null;
            }
        }

        private abstract class Scene : Signature
        {
            protected const int Pending = 0;
            protected const int Running = 1;
            protected const int Done = 2;
            protected const int Skipped = 3;
            protected const int Slow = 4;

            private TextMeshProUGUI _status;
            private string _statusSource;
            private bool _statusStrip;
            private Color32? _statusInk;

            /// <summary>Half the screen's width, in 1080p units.</summary>
            protected float HW => O._laidOutSize.x * 0.5f / U;
            protected static int First => StartupTracker.FirstStep;
            protected static int Steps => StartupTracker.StepCount;
            protected bool Granted => O._phase != Phase.Loading;

            protected int State(int i)
            {
                int current = StartupTracker.Current;
                if (Granted || i < current)
                {
                    return StartupTracker.WasSkipped(i) ? Skipped : Done;
                }
                if (i == current)
                {
                    return O._slow ? Slow : Running;
                }
                return Pending;
            }

            protected int DoneCount()
            {
                int done = 0;
                for (int i = First; i < Steps; i++)
                {
                    done += State(i) == Done || State(i) == Skipped ? 1 : 0;
                }
                return done;
            }

            protected static string StepName(int i) => StartupTracker.Labels[i].ToUpperInvariant();

            /// <summary>A label whose box starts at <paramref name="left"/>.</summary>
            protected TextMeshProUGUI LabelAt(string text, float left, float y, float w, float size, Color32 color,
                TextAlignmentOptions align = TextAlignmentOptions.Left, float spacing = 6f, Transform parent = null)
            {
                TextMeshProUGUI label = Label(text, left + w * 0.5f, y, w, size, color, align, parent, spacing);
                label.overflowMode = TextOverflowModes.Ellipsis;
                return label;
            }

            /// <summary>Wrapping text in a box whose top-left corner is (<paramref name="left"/>, <paramref name="top"/>).</summary>
            protected TextMeshProUGUI Paragraph(string text, float left, float top, float w, float h, float size, Color32 color, Transform parent = null)
            {
                TextMeshProUGUI label = Label(text, left + w * 0.5f, top - h * 0.5f, w, size, color, TextAlignmentOptions.TopLeft, parent, 3f);
                label.rectTransform.sizeDelta = P(w, h);
                label.enableWordWrapping = true;
                label.overflowMode = TextOverflowModes.Truncate;
                return label;
            }

            /// <summary>
            /// The line that shows the readout's current text: the running step, then the grant
            /// (or the fake denial). <paramref name="strip"/> drops the "&gt; " prompt.
            /// </summary>
            /// <param name="ink">For light paper: the line keeps this colour (the warning colour still shows a denial).</param>
            protected TextMeshProUGUI Status(float left, float y, float w, float size, bool strip,
                TextAlignmentOptions align = TextAlignmentOptions.Left, Color32? ink = null)
            {
                _status = LabelAt(string.Empty, left, y, w, size, T.Text, align);
                _statusStrip = strip;
                _statusInk = ink;
                return _status;
            }

            protected void MirrorStatus()
            {
                if (_status == null)
                {
                    return;
                }
                TextMeshProUGUI source = O._currentLabel;
                string text = source.text;
                if (!ReferenceEquals(text, _statusSource))
                {
                    _statusSource = text;
                    _status.text = _statusStrip && text.StartsWith("> ") ? text.Substring(2) : text;
                }
                Color warn = (Color32)T.Warn;
                _status.color = _statusInk.HasValue && source.color != warn ? (Color)_statusInk.Value : source.color;
                _status.maxVisibleCharacters = Mathf.Max(0, source.maxVisibleCharacters - (_statusStrip ? 2 : 0));
            }

            /// <summary>The small TerraGroup mark a scene puts in its top-left corner.</summary>
            protected void BrandMark(float x, float y, string sub)
            {
                Shape logo = Draw(x, y);
                float r = 20f * U;
                float gap = 2f * U;
                AddPiece(logo, new Vector2(-r, 0f), new Vector2(0f, r), gap, T.Primary, null);
                AddPiece(logo, new Vector2(0f, r), new Vector2(r, 0f), gap, T.Primary, null);
                AddPiece(logo, new Vector2(-r, 0f), new Vector2(0f, -r), gap, T.Primary, null);
                AddPiece(logo, new Vector2(r, 0f), new Vector2(0f, -r), gap, T.Primary, T.Text);
                logo.Commit();
                LabelAt("TERRAGROUP", x + 34f, y + 7f, 340f, 22f, T.Text, spacing: 14f);
                LabelAt(sub, x + 34f, y - 17f, 340f, 11f, T.Accent, spacing: 8f);
            }

            /// <summary>A head-and-shoulders silhouette, for a photo box.</summary>
            protected void Silhouette(Shape shape, float x, float y, float scale, Color32 color)
            {
                shape.Disc(P(x, y + 20f * scale), 18f * scale * U, color);
                var shoulders = new Vector2[13];
                for (int i = 0; i <= 12; i++)
                {
                    float a = Mathf.PI * i / 12f;
                    shoulders[i] = P(x + Mathf.Cos(a) * 38f * scale, y - 58f * scale + Mathf.Sin(a) * 52f * scale);
                }
                shape.Polygon(shoulders, color);
            }

            /// <summary>A tick mark centred on (x, y).</summary>
            protected void Tick(Shape shape, float x, float y, float size, Color32 color)
            {
                shape.Line(P(x - 5f * size, y), P(x - 1f * size, y - 4f * size), 2.2f * size * U, color);
                shape.Line(P(x - 1f * size, y - 4f * size), P(x + 6f * size, y + 6f * size), 2.2f * size * U, color);
            }

            protected static string Initials(string name) =>
                string.IsNullOrEmpty(name) ? "TG" : name.Substring(0, Math.Min(2, name.Length)).ToUpperInvariant();

            protected Color32 StateColor(int state)
            {
                switch (state)
                {
                    case Running: return T.Accent;
                    case Slow: return T.Warn;
                    case Pending: return A(T.Text, 170);
                    default: return T.Dim;
                }
            }
        }

        /// <summary>
        /// Logistics: the freight terminal's departures board. Each load step is a shipment;
        /// its status flips like split-flap letters from ON TIME to BOARDING to DEPARTED, or to
        /// DELAYED when the step runs slow.
        /// </summary>
        private sealed class Departures : Scene
        {
            private const string Flaps = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            private TextMeshProUGUI[] _status;
            private TextMeshProUGUI[][] _cells;
            private Image[] _glow;
            private string[] _target;
            private float[] _flipUntil;
            private float[] _flipNext;
            private int[] _state;
            private TextMeshProUGUI _clock;
            private TextMeshProUGUI _count;
            private Color32 _led;
            private Image _bar;
            private float _barWidth;
            private int _second = -1;
            private int _shownCount = -1;

            protected override void Build()
            {
                BrandMark(-HW + 80f, 398f, "LOGISTICS AND SUPPLY");
                float bw = Mathf.Min(2f * HW - 200f, 1640f);
                float left = -bw * 0.5f;
                float right = bw * 0.5f;
                Box(0f, -15f, bw, 700f, A(Dark, 245));
                Shape frame = Draw();
                Outline(frame, 0f, -15f, bw, 700f, 2f, A(T.Accent, 120));
                // Which terminal, and whether its board is the old amber LED kind.
                string[] titles = { "DEPARTURES", "OUTBOUND FREIGHT", "CONVOY SCHEDULE" };
                string[] places = { "NORVINSK FREIGHT TERMINAL  //  GATES A-D", "PORT OF TARKOV  //  CARGO PIERS 1-4", "RESERVE RAIL YARD  //  PLATFORMS 1-6" };
                _led = Rng.Next(2) == 0 ? new Color32(255, 176, 40, 255) : T.Accent;
                int pick = Rng.Next(titles.Length);
                LabelAt(titles[pick], left + 24f, 296f, 700f, 30f, _led, spacing: 18f);
                LabelAt(places[pick], left + 24f, 264f, 700f, 12f, T.Dim);
                _clock = LabelAt(string.Empty, right - 324f, 290f, 300f, 30f, _led, TextAlignmentOptions.Right);

                float[] columns = { 0.02f, 0.11f, 0.50f, 0.70f, 0.81f };
                string[] heads = { "TIME", "SHIPMENT", "DESTINATION", "GATE", "STATUS" };
                for (int c = 0; c < heads.Length; c++)
                {
                    LabelAt(heads[c], left + bw * columns[c], 232f, 260f, 11f, T.Dim);
                }
                frame.Line(P(left + 12f, 218f), P(right - 12f, 218f), U, A(T.Dim, 90));

                int n = Steps - First;
                float rowH = Mathf.Min(34f, 500f / n);
                _status = new TextMeshProUGUI[n];
                _cells = new TextMeshProUGUI[n][];
                _glow = new Image[n];
                _target = new string[n];
                _flipUntil = new float[n];
                _flipNext = new float[n];
                _state = new int[n];
                DateTime start = DateTime.Now.AddSeconds(-StartupTracker.Elapsed);
                float at = 0f;
                for (int r = 0; r < n; r++)
                {
                    int i = First + r;
                    float y = 200f - r * rowH;
                    _glow[r] = Box(0f, y, bw - 12f, rowH - 2f, T.Accent);
                    _glow[r].canvasRenderer.SetAlpha(0f);
                    Lore.Location(Rng, out string map, out string place);
                    string time = start.AddSeconds(at).ToString("HH:mm");
                    at += StartupTracker.ExpectedSeconds(i);
                    _cells[r] = new[]
                    {
                        LabelAt($"<mspace=0.62em>{time}</mspace>", left + bw * columns[0], y, bw * 0.08f, 14f, T.Text),
                        LabelAt(StepName(i), left + bw * columns[1], y, bw * 0.37f, 14f, T.Text),
                        LabelAt($"{place}, {map}", left + bw * columns[2], y, bw * 0.19f, 14f, T.Text),
                        LabelAt($"<mspace=0.62em>{(char)('A' + Rng.Next(4))}{Rng.Next(1, 20):00}</mspace>", left + bw * columns[3], y, bw * 0.1f, 14f, T.Text)
                    };
                    _status[r] = LabelAt(string.Empty, left + bw * columns[4], y, bw * 0.17f, 14f, T.Text);
                    _state[r] = -1;
                    frame.Line(P(left + 12f, y - rowH * 0.5f), P(right - 12f, y - rowH * 0.5f), U, A(T.Dim, 28));
                }
                frame.Commit();

                Status(left + 24f, -318f, bw * 0.6f, 18f, false);
                _count = LabelAt(string.Empty, right - 424f, -318f, 400f, 13f, T.Dim, TextAlignmentOptions.Right);
                Box(0f, -350f, bw - 48f, 4f, T.Faint);
                _bar = Bar(left + 24f, -350f, 4f, T.Accent);
                _barWidth = (bw - 48f) * U;
            }

            private static string Word(int state)
            {
                switch (state)
                {
                    case Running: return "BOARDING";
                    case Slow: return "DELAYED";
                    case Done: return "DEPARTED";
                    case Skipped: return "CANCELLED";
                    default: return "ON TIME";
                }
            }

            internal override void Animate(float t)
            {
                DateTime now = DateTime.Now;
                if (now.Second != _second)
                {
                    _second = now.Second;
                    _clock.text = $"<mspace=0.62em>{now:HH:mm:ss}</mspace>";
                }

                for (int r = 0; r < _status.Length; r++)
                {
                    int state = State(First + r);
                    if (state != _state[r])
                    {
                        _state[r] = state;
                        _target[r] = Word(state);
                        _flipUntil[r] = t + 0.4f;
                        _status[r].color = state == Running ? _led : StateColor(state);
                        float alpha = state == Done || state == Skipped ? 0.5f : 1f;
                        foreach (TextMeshProUGUI cell in _cells[r])
                        {
                            cell.canvasRenderer.SetAlpha(alpha);
                        }
                    }
                    if (t < _flipUntil[r])
                    {
                        // The flaps spin through letters before settling.
                        if (t >= _flipNext[r])
                        {
                            _flipNext[r] = t + 0.05f;
                            char[] chars = new char[_target[r].Length];
                            for (int k = 0; k < chars.Length; k++)
                            {
                                chars[k] = Flaps[Rng.Next(Flaps.Length)];
                            }
                            _status[r].text = new string(chars);
                        }
                    }
                    else if (!ReferenceEquals(_status[r].text, _target[r]))
                    {
                        _status[r].text = _target[r];
                    }
                    bool live = state == Running || state == Slow;
                    _glow[r].canvasRenderer.SetAlpha(live ? 0.07f + 0.04f * Mathf.Sin(t * 3f) : 0f);
                    if (live)
                    {
                        _status[r].canvasRenderer.SetAlpha(0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t * 3f)));
                    }
                }

                int done = DoneCount();
                int percent = Mathf.FloorToInt(Progress * 100f);
                int key = done * 1000 + percent;
                if (key != _shownCount)
                {
                    _shownCount = key;
                    _count.text = $"DEPARTED {done} / {_status.Length}     {percent}%";
                }
                _bar.rectTransform.sizeDelta = new Vector2(Mathf.Round(_barWidth * Progress), _bar.rectTransform.sizeDelta.y);
                MirrorStatus();
            }
        }
    }
}
