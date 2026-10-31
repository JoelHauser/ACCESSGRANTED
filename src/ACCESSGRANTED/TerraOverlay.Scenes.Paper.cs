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
        /// Records: the launch as a typed file on a manila folder. Each step is typed onto the
        /// page as it starts and filed when it ends; on some lines, once filed, a black bar
        /// slides over part of the entry.
        /// </summary>
        private sealed class LaunchRecord : Scene
        {
            private const float Size = 14f;
            private TextMeshProUGUI[] _lines;
            private TextMeshProUGUI[] _results;
            private Image[] _redactions;
            private float[] _redactWidth;
            private float[] _redactDrawn;
            private float[] _typedAt;
            private int[] _state;
            private TextMeshProUGUI _subject;
            private string _shownNick = "\0";
            private Color32 _ink;

            protected override void Build()
            {
                BrandMark(-HW + 80f, 398f, "RECORDS MANAGEMENT");
                float pw = Mathf.Min(980f, 2f * HW - 360f);
                float left = -pw * 0.5f;
                Color32 manila = Color32.Lerp(new Color32(150, 128, 84, 255), T.Ink, 0.35f);
                Color32 paper = Color32.Lerp(T.Text, new Color32(255, 255, 255, 255), 0.3f);
                _ink = Color32.Lerp(T.Ink, new Color32(0, 0, 0, 255), 0.4f);

                Box(26f, -32f, pw + 40f, 724f, manila);
                Box(left + 150f, 356f, 240f, 34f, manila);
                string[] boxes = { "ARCHIVE BOX 2017-C", "BASEMENT 2  //  SHELF 14", "DO NOT REMOVE", "RETENTION R-0" };
                LabelAt(boxes[Rng.Next(boxes.Length)], left + 46f, 358f, 220f, 11f, A(_ink, 200));
                Box(0f, -20f, pw, 720f, paper);

                LabelAt("CONFIDENTIAL", -150f, 322f, 300f, 12f, T.Warn, TextAlignmentOptions.Center, 14f);
                float l = left + 44f;
                float w = pw - 88f;
                LabelAt("TERRAGROUP  //  RECORDS MANAGEMENT  //  DOCUMENT CONTROL", l, 292f, w, 11f, A(_ink, 170));
                string[] kinds = { "LAUNCH RECORD", "INCIDENT REPORT", "PERSONNEL FILE", "ACCESS AUDIT" };
                LabelAt($"{kinds[Rng.Next(kinds.Length)]}  //  FILE 0x{Rng.Next(0x1000, 0x10000):X4}", l, 264f, w, 21f, _ink, spacing: 8f);
                Shape rule = Draw();
                rule.Line(P(l, 244f), P(l + w, 244f), 1.5f * U, A(_ink, 140));
                _subject = LabelAt(string.Empty, l, 222f, w * 0.5f, 12f, _ink);
                LabelAt($"OPENED  {DateTime.Now.AddSeconds(-StartupTracker.Elapsed):dd.MM.yyyy HH:mm}", l + w * 0.5f, 222f, w * 0.5f, 12f, _ink, TextAlignmentOptions.Right);
                LabelAt("CLASSIFICATION  CONFIDENTIAL  //  RETENTION  R-0", l, 200f, w, 12f, A(_ink, 170));
                rule.Line(P(l, 184f), P(l + w, 184f), U, A(_ink, 80));
                rule.Commit();

                int n = Steps - First;
                float rowH = Mathf.Min(32f, 450f / n);
                float charW = 0.6f * Size;
                Shape rows = Draw();
                _lines = new TextMeshProUGUI[n];
                _results = new TextMeshProUGUI[n];
                _redactions = new Image[n];
                _redactWidth = new float[n];
                _redactDrawn = new float[n];
                _typedAt = new float[n];
                _state = new int[n];
                for (int k = 0; k < n; k++)
                {
                    float y = 158f - k * rowH;
                    _lines[k] = LabelAt(string.Empty, l, y, w * 0.68f, Size, _ink, spacing: 0f);
                    _lines[k].overflowMode = TextOverflowModes.Overflow;
                    _results[k] = LabelAt(string.Empty, l + w * 0.6f, y, w * 0.4f, Size, _ink, TextAlignmentOptions.Right, 0f);
                    rows.Line(P(l, y - rowH * 0.5f), P(l + w, y - rowH * 0.5f), U, A(_ink, 30));
                    _state[k] = -1;
                    // Some entries lose part of their name to the censor once filed.
                    if (Rng.NextDouble() < 0.45)
                    {
                        int start = 4 + Rng.Next(0, 8);
                        int length = Rng.Next(6, 14);
                        _redactions[k] = Bar(l + start * charW, y, 15f, new Color32(10, 10, 12, 255));
                        _redactWidth[k] = length * charW * U;
                    }
                }

                rows.Commit();
                Status(l, -338f, w * 0.6f, 15f, false, ink: _ink);
                LabelAt("PAGE 1 OF 1  //  CONFIDENTIAL", l, -360f, w, 10f, A(_ink, 150), TextAlignmentOptions.Right);
            }

            private static string Entry(int number, int step) => $"<mspace=0.6em>{number:00}  {StepName(step)}</mspace>";

            internal override void Animate(float t)
            {
                string nick = StartupTracker.Nickname;
                if (nick != _shownNick)
                {
                    _shownNick = nick;
                    _subject.text = "SUBJECT  " + (string.IsNullOrEmpty(nick) ? "PENDING IDENTIFICATION" : nick.ToUpperInvariant())
                        + "  //  " + (StartupTracker.Side ?? "UNAFFILIATED");
                }
                for (int k = 0; k < _lines.Length; k++)
                {
                    int i = First + k;
                    int state = State(i);
                    if (state != _state[k])
                    {
                        _state[k] = state;
                        switch (state)
                        {
                            case Running:
                            case Slow:
                                _lines[k].text = Entry(k + 1, i);
                                _lines[k].color = state == Slow ? T.Warn : _ink;
                                _results[k].text = state == Slow ? "<mspace=0.6em>DELAYED</mspace>" : "<mspace=0.6em>IN PROGRESS</mspace>";
                                _results[k].color = state == Slow ? T.Warn : A(_ink, 150);
                                if (_typedAt[k] == 0f)
                                {
                                    _typedAt[k] = t;
                                    _lines[k].maxVisibleCharacters = 0;
                                }
                                break;
                            case Done:
                                _lines[k].text = Entry(k + 1, i);
                                _lines[k].color = _ink;
                                _lines[k].maxVisibleCharacters = 99999;
                                _results[k].text = $"<mspace=0.6em>{StartupTracker.TookSeconds(i):0.0}S   FILED</mspace>";
                                _results[k].color = _ink;
                                break;
                            case Skipped:
                                _lines[k].text = Entry(k + 1, i);
                                _lines[k].color = A(_ink, 120);
                                _lines[k].maxVisibleCharacters = 99999;
                                _results[k].text = "<mspace=0.6em>WITHDRAWN</mspace>";
                                _results[k].color = A(_ink, 120);
                                break;
                            default:
                                _lines[k].text = string.Empty;
                                _results[k].text = string.Empty;
                                break;
                        }
                    }
                    if ((state == Running || state == Slow) && _lines[k].maxVisibleCharacters < 200)
                    {
                        // Typed out at a typewriter's pace.
                        _lines[k].maxVisibleCharacters = Mathf.FloorToInt((t - _typedAt[k]) * 45f);
                    }
                    if (_redactions[k] != null)
                    {
                        float target = state == Done ? _redactWidth[k] : 0f;
                        float w = Mathf.MoveTowards(_redactDrawn[k], target, _redactWidth[k] * Dt / 0.4f);
                        if (w != _redactDrawn[k])
                        {
                            _redactDrawn[k] = w;
                            _redactions[k].rectTransform.sizeDelta = new Vector2(Mathf.Round(w), _redactions[k].rectTransform.sizeDelta.y);
                        }
                    }
                }
                MirrorStatus();
            }
        }

        /// <summary>
        /// Legal: a service agreement between TerraGroup and the operator. Each step is a clause,
        /// drafted as it starts and initialled when it ends; TerraGroup signs as the load closes,
        /// and the operator's signature follows the grant.
        /// </summary>
        private sealed class Agreement : Scene
        {
            private static readonly string[] Titles = { "SERVICE AGREEMENT", "NON-DISCLOSURE AGREEMENT", "LIABILITY WAIVER", "EMPLOYMENT CONTRACT" };
            private static readonly string[] SecrecyTerms =
            {
                "THE CONTRACTOR SHALL NOT DISCLOSE {0} TO ANY PERSON, ORGANISATION OR PEACEKEEPING FORCE.",
                "ALL INFORMATION ARISING FROM {0} REMAINS THE PROPERTY OF TERRAGROUP IN PERPETUITY.",
                "THE CONTRACTOR SHALL NOT RECALL {0} IF ASKED BY ANY COURT.",
                "BREACH OF CONFIDENTIALITY REGARDING {0} SHALL BE DEALT WITH BY TERRAGROUP SECURITY.",
                "{0} DID NOT TAKE PLACE FOR THE PURPOSES OF ANY EXTERNAL INQUIRY.",
                "THE CONTRACTOR'S OBLIGATIONS REGARDING {0} SURVIVE THE CONTRACTOR."
            };
            private static readonly string[] Terms =
            {
                "THE CONTRACTOR ACKNOWLEDGES THAT {0} IS UNDERTAKEN AT THE CONTRACTOR'S OWN RISK.",
                "TERRAGROUP ACCEPTS NO LIABILITY FOR ANY DELAY ARISING FROM {0}.",
                "{0} SHALL BE DEEMED COMPLETE AT TERRAGROUP'S SOLE DETERMINATION.",
                "ANY DISPUTE ARISING FROM {0} SHALL BE SETTLED BY TERRAGROUP ARBITRATION IN NORVINSK.",
                "THE CONTRACTOR WAIVES ALL CLAIMS IN RESPECT OF {0}, KNOWN OR UNKNOWN.",
                "{0} IS CONFIDENTIAL AND SHALL NOT BE DISCLOSED TO ANY THIRD PARTY, INCLUDING THE UN."
            };
            private const int SignaturePoints = 60;
            private TextMeshProUGUI[] _heads;
            private TextMeshProUGUI[] _bodies;
            private TextMeshProUGUI[] _initials;
            private int[] _state;
            private TextMeshProUGUI _parties;
            private Shape _ourSignature;
            private Shape _theirSignature;
            private Vector2[] _ourPath;
            private Vector2[] _theirPath;
            private int _ourDrawn = -1;
            private int _theirDrawn = -1;
            private float _grantedAt = -1f;
            private string _shownNick = "\0";
            private Color32 _ink;

            protected override void Build()
            {
                BrandMark(-HW + 80f, 398f, "LEGAL DEPARTMENT");
                float pw = Mathf.Min(1200f, 2f * HW - 360f);
                float left = -pw * 0.5f;
                Color32 paper = Color32.Lerp(T.Accent, new Color32(255, 255, 255, 255), 0.6f);
                _ink = Color32.Lerp(T.Primary, new Color32(0, 0, 0, 255), 0.55f);
                Box(0f, -20f, pw, 720f, paper);
                Shape border = Draw();
                Outline(border, 0f, -20f, pw - 30f, 690f, 1f, A(_ink, 90));
                Outline(border, 0f, -20f, pw - 40f, 680f, 1f, A(_ink, 50));

                int kind = Rng.Next(Titles.Length);
                string[] terms = kind == 1 ? SecrecyTerms : Terms;
                Label(Titles[kind], 0f, 300f, pw, 26f, _ink, TextAlignmentOptions.Center, null, 18f);
                _parties = Label(string.Empty, 0f, 270f, pw, 12f, A(_ink, 200), TextAlignmentOptions.Center, null, 6f);
                Label("SCHEDULE B  //  PRIVILEGED AND CONFIDENTIAL", 0f, 250f, pw, 10f, A(_ink, 150), TextAlignmentOptions.Center, null, 8f);
                border.Line(P(left + 60f, 234f), P(-left - 60f, 234f), U, A(_ink, 90));

                int n = Steps - First;
                int rows = Mathf.CeilToInt(n / 2f);
                float columnWidth = (pw - 140f) / 2f;
                float blockH = Mathf.Min(64f, 440f / rows);
                _heads = new TextMeshProUGUI[n];
                _bodies = new TextMeshProUGUI[n];
                _initials = new TextMeshProUGUI[n];
                _state = new int[n];
                for (int k = 0; k < n; k++)
                {
                    int i = First + k;
                    float x = left + 60f + (k / rows) * (columnWidth + 20f);
                    float top = 216f - (k % rows) * blockH;
                    _heads[k] = LabelAt($"CLAUSE {k + 1}.  {StepName(i)}", x, top - 8f, columnWidth - 70f, 11f, _ink, spacing: 4f);
                    _bodies[k] = Paragraph(string.Format(terms[k % terms.Length], StepName(i)), x, top - 18f, columnWidth - 70f, blockH - 20f, 9.5f, A(_ink, 210));
                    _initials[k] = LabelAt(string.Empty, x + columnWidth - 64f, top - 8f, 60f, 10f, T.Primary, TextAlignmentOptions.Right);
                    _state[k] = -1;
                }

                // The signature block.
                float sy = -300f;
                float sw = (pw - 200f) / 2f;
                float ourLeft = left + 70f;
                float theirLeft = left + 130f + sw;
                border.Line(P(ourLeft, sy), P(ourLeft + sw, sy), U, A(_ink, 160));
                border.Line(P(theirLeft, sy), P(theirLeft + sw, sy), U, A(_ink, 160));
                border.Commit();
                LabelAt("FOR TERRAGROUP HOLDINGS", ourLeft, sy - 16f, sw, 10f, A(_ink, 170));
                LabelAt("THE CONTRACTOR", theirLeft, sy - 16f, sw, 10f, A(_ink, 170));
                _ourPath = Path(ourLeft + 20f, sy + 18f, sw * 0.6f, 0.3f);
                _theirPath = Path(theirLeft + 20f, sy + 18f, sw * 0.5f, 1.7f);
                _ourSignature = Draw();
                _theirSignature = Draw();
                Status(left + 70f, -352f, pw * 0.55f, 13f, true, ink: _ink);
                LabelAt($"DATED  {DateTime.Now:dd.MM.yyyy}", -left - 70f - 300f, -352f, 300f, 10f, A(_ink, 160), TextAlignmentOptions.Right);
            }

            /// <summary>A looping scrawl, different for each hand.</summary>
            private Vector2[] Path(float left, float y, float width, float hand)
            {
                var path = new Vector2[SignaturePoints];
                for (int i = 0; i < SignaturePoints; i++)
                {
                    float s = i / (SignaturePoints - 1f);
                    path[i] = P(left + width * s + 7f * Mathf.Cos(s * (22f + hand * 3f)),
                        y + 9f * Mathf.Sin(s * (22f + hand * 3f)) + 4f * Mathf.Sin(s * (55f + hand * 9f)));
                }
                return path;
            }

            private static void Sign(Shape shape, Vector2[] path, int count, float width, Color32 color)
            {
                shape.Clear();
                for (int i = 0; i < count - 1; i++)
                {
                    shape.Line(path[i], path[i + 1], width, color);
                }
                shape.Commit();
            }

            internal override void Animate(float t)
            {
                string nick = StartupTracker.Nickname;
                if (nick != _shownNick)
                {
                    _shownNick = nick;
                    _parties.text = "BETWEEN TERRAGROUP HOLDINGS (\"TERRAGROUP\") AND "
                        + (string.IsNullOrEmpty(nick) ? "THE UNDERSIGNED" : nick.ToUpperInvariant()) + " (\"THE CONTRACTOR\")";
                }
                for (int k = 0; k < _state.Length; k++)
                {
                    int state = State(First + k);
                    float alpha = state == Pending ? 0.18f : state == Running || state == Slow ? 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(t * 2.5f)) : 1f;
                    _heads[k].canvasRenderer.SetAlpha(alpha);
                    _bodies[k].canvasRenderer.SetAlpha(alpha);
                    if (state == _state[k])
                    {
                        continue;
                    }
                    _state[k] = state;
                    _initials[k].text = state == Done ? "TG / " + Initials(nick) : state == Slow ? "DISPUTED" : state == Skipped ? "STRUCK" : string.Empty;
                    _initials[k].color = state == Slow ? T.Warn : T.Primary;
                }

                int ours = Mathf.Clamp(Mathf.FloorToInt((Progress - 0.7f) / 0.3f * SignaturePoints), 0, SignaturePoints);
                if (ours != _ourDrawn)
                {
                    _ourDrawn = ours;
                    Sign(_ourSignature, _ourPath, ours, 2f * U, T.Primary);
                }
                if (Granted && _grantedAt < 0f)
                {
                    _grantedAt = t;
                }
                int theirs = _grantedAt < 0f ? 0 : Mathf.Clamp(Mathf.FloorToInt((t - _grantedAt) / 0.6f * SignaturePoints), 0, SignaturePoints);
                if (theirs != _theirDrawn)
                {
                    _theirDrawn = theirs;
                    Sign(_theirSignature, _theirPath, theirs, 2f * U, _ink);
                }
                MirrorStatus();
            }
        }

        /// <summary>
        /// Executive: the load presented to the board, a slide per step. Each slide charts the
        /// real time every finished step took, and the strip below tracks the deck.
        /// </summary>
        private sealed class BoardDeck : Scene
        {
            private static readonly string[] Points =
            {
                "DELIVERY: ON SCHEDULE", "STAKEHOLDER EXPOSURE: MINIMAL", "RECOMMENDATION: PROCEED",
                "RISK: ACCEPTED BY THE BOARD", "COST: ABSORBED BY OPERATIONS", "PUBLIC STATEMENT: NOT REQUIRED",
                "LEGAL REVIEW: WAIVED", "OUTCOME: AS PLANNED"
            };
            private const int Bars = 7;
            private string _deck;
            private RectTransform _slide;
            private const float FadeOut = 0.18f;
            private const float FadeIn = 0.4f;
            private RectTransform _copy;
            private CanvasGroup _copyGroup;
            private readonly float[] _barHeight = new float[Bars];
            private readonly float[] _barTarget = new float[Bars];
            private int _pendingStep;
            private bool _swapped = true;
            private TextMeshProUGUI _item;
            private TextMeshProUGUI _title;
            private TextMeshProUGUI _bullets;
            private TextMeshProUGUI _page;
            private TextMeshProUGUI _upNext;
            private readonly Image[] _bars = new Image[Bars];
            private readonly TextMeshProUGUI[] _barLabels = new TextMeshProUGUI[Bars];
            private readonly TextMeshProUGUI[] _barValues = new TextMeshProUGUI[Bars];
            private Image[] _thumbs;
            private float _chartLeft;
            private float _chartBottom;
            private float _chartHeight;
            private float _barWidth;
            private int _shownStep = -2;
            private float _changedAt = -10f;

            protected override void Build()
            {
                BrandMark(-HW + 80f, 398f, "EXECUTIVE BOARD");
                string[] decks = { "BOARD BRIEFING", "QUARTERLY REVIEW", "CRISIS COMMITTEE", "SHAREHOLDER UPDATE" };
                _deck = decks[Rng.Next(decks.Length)];
                float total = Mathf.Min(2f * HW - 200f, 1640f);
                float left = -total * 0.5f;
                float sw = Mathf.Min(total * 0.72f, 1100f);
                float sh = sw * 9f / 16f;
                float sx = left + sw * 0.5f;
                float sy = 330f - sh * 0.5f;

                // The projected slide.
                Box(sx + 8f, sy - 8f, sw, sh, new Color32(0, 0, 0, 120));
                Box(sx, sy, sw, sh, Color32.Lerp(T.Ink, new Color32(255, 255, 255, 255), 0.06f));
                Shape frame = Draw();
                Outline(frame, sx, sy, sw, sh, 1f, A(T.Accent, 120));
                frame.Commit();
                _slide = MakeRect("Slide", R);
                _slide.anchoredPosition = P(sx, sy);
                _copy = MakeRect("Copy", _slide);
                _copyGroup = _copy.gameObject.AddComponent<CanvasGroup>();
                float l = -sw * 0.5f + 40f;
                Box(0f, sh * 0.5f - 6f, sw, 4f, T.Accent, _slide);
                _item = LabelAt(string.Empty, l, sh * 0.5f - 40f, sw - 80f, 12f, T.Accent, spacing: 10f, parent: _copy);
                _title = LabelAt(string.Empty, l, sh * 0.5f - 78f, sw - 80f, 30f, T.Text, spacing: 6f, parent: _copy);
                _bullets = Paragraph(string.Empty, l, sh * 0.5f - 120f, sw * 0.4f, sh - 200f, 15f, T.Text, _copy);
                _bullets.lineSpacing = 30f;

                // The chart: real step times.
                _chartLeft = 20f;
                _chartBottom = -sh * 0.5f + 70f;
                _chartHeight = sh - 250f;
                float chartWidth = sw * 0.5f - 60f;
                LabelAt("TIME PER ITEM, SECONDS", _chartLeft, _chartBottom + _chartHeight + 20f, chartWidth, 10f, T.Dim, parent: _slide);
                float slot = chartWidth / Bars;
                _barWidth = slot * 0.6f;
                for (int b = 0; b < Bars; b++)
                {
                    float x = _chartLeft + slot * (b + 0.2f);
                    Image bar = MakeImage("Bar", _slide, A(T.Accent, 200));
                    RectTransform rect = bar.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0f, 0f);
                    rect.anchoredPosition = P(x, _chartBottom);
                    rect.sizeDelta = new Vector2(_barWidth * U, 0f);
                    _bars[b] = bar;
                    _barLabels[b] = LabelAt(string.Empty, x - 4f, _chartBottom - 14f, _barWidth + 8f, 9f, T.Dim, TextAlignmentOptions.Center, 0f, _copy);
                    _barValues[b] = LabelAt(string.Empty, x - 6f, _chartBottom + 10f, _barWidth + 12f, 10f, T.Text, TextAlignmentOptions.Center, 0f, _copy);
                }
                Box(_chartLeft + chartWidth * 0.5f, _chartBottom, chartWidth, 1f, T.Dim, _slide);
                LabelAt("STRICTLY CONFIDENTIAL  //  BOARD USE ONLY", l, -sh * 0.5f + 24f, sw * 0.6f, 10f, T.Dim, parent: _slide);
                _page = LabelAt(string.Empty, sw * 0.5f - 240f, -sh * 0.5f + 24f, 200f, 10f, T.Dim, TextAlignmentOptions.Right, parent: _copy);

                // The deck, as a strip of thumbnails under the slide.
                int n = Steps - First;
                _thumbs = new Image[n];
                float tw = (sw - (n - 1) * 6f) / n;
                for (int k = 0; k < n; k++)
                {
                    _thumbs[k] = Box(left + tw * 0.5f + k * (tw + 6f), sy - sh * 0.5f - 34f, tw, 26f, T.Faint);
                }
                Status(left, sy - sh * 0.5f - 74f, sw, 16f, true);

                // Presenter notes, to the right.
                float nl = left + sw + 40f;
                float nw = total * 0.5f - nl;
                if (nw >= 220f)
                {
                    LabelAt("PRESENTER NOTES", nl, 316f, nw, 11f, T.Dim, spacing: 10f);
                    Paragraph(Lore.Fill(T.Memos[Rng.Next(T.Memos.Length)], Rng), nl, 298f, nw, 180f, 13f, T.Text);
                    LabelAt("UP NEXT", nl, 90f, nw, 11f, T.Dim, spacing: 10f);
                    _upNext = Paragraph(string.Empty, nl, 72f, nw, 140f, 13f, T.Text);
                    _upNext.lineSpacing = 20f;
                    LabelAt("ATTENDEES", nl, -100f, nw, 11f, T.Dim, spacing: 10f);
                    LabelAt($"{Rng.Next(6, 11)} PRESENT  //  QUORUM: YES", nl, -124f, nw, 13f, T.Text);
                    LabelAt("MINUTES", nl, -160f, nw, 11f, T.Dim, spacing: 10f);
                    LabelAt("NOT TAKEN, AT THE CHAIR'S REQUEST", nl, -184f, nw, 13f, T.Text);
                }
            }

            private void ShowSlide(int step)
            {
                int n = Steps - First;
                int number = Granted ? n : step - First + 1;
                _item.text = Granted ? "RESOLUTION" : $"{_deck}  //  ITEM {number:00}";
                _title.text = Granted ? "THE BOARD APPROVES THE OPERATOR'S ACCESS" : StepName(step);
                _page.text = $"{number:00} / {n:00}";
                var builder = new StringBuilder();
                for (int p = 0; p < 3; p++)
                {
                    builder.Append("-  ").Append(Points[(step * 3 + p) % Points.Length]).Append('\n');
                }
                _bullets.text = builder.ToString();

                // The latest finished steps, oldest on the left.
                float max = 0.5f;
                int[] shown = new int[Bars];
                int count = 0;
                for (int i = step - 1; i >= First && count < Bars; i--)
                {
                    if (State(i) == Done)
                    {
                        shown[count++] = i;
                        max = Mathf.Max(max, StartupTracker.TookSeconds(i));
                    }
                }
                for (int b = 0; b < Bars; b++)
                {
                    bool on = b < count;
                    int i = on ? shown[count - 1 - b] : 0;
                    _barTarget[b] = on ? StartupTracker.TookSeconds(i) / max * _chartHeight : 0f;
                    _barLabels[b].text = on ? $"{i - First + 1:00}" : string.Empty;
                    _barValues[b].text = on ? StartupTracker.TookSeconds(i).ToString("0.0") : string.Empty;
                }

                if (_upNext != null)
                {
                    builder.Length = 0;
                    for (int i = step + 1; i < Steps && i <= step + 3; i++)
                    {
                        builder.Append($"{i - First + 1:00}  ").Append(StepName(i)).Append('\n');
                    }
                    _upNext.text = Granted ? "ADJOURNMENT" : builder.ToString();
                }
            }

            internal override void Animate(float t)
            {
                // A new item: the words fade out, swap, and ease back in from the right, while
                // the slide stays and the chart's bars move to their new heights.
                int step = Granted ? Steps : StartupTracker.Current;
                if (step != _shownStep)
                {
                    bool first = _shownStep == -2;
                    _shownStep = step;
                    _pendingStep = Mathf.Min(step, Steps - 1);
                    _changedAt = first ? t - FadeOut : t;
                    _swapped = false;
                    for (int k = 0; k < _thumbs.Length; k++)
                    {
                        int state = State(First + k);
                        _thumbs[k].color = state == Running || state == Slow ? (Color)T.Accent : state == Done ? (Color)A(T.Dim, 160) : (Color)T.Faint;
                    }
                }
                float age = t - _changedAt;
                float alpha;
                float shift = 0f;
                if (age < FadeOut)
                {
                    alpha = 1f - age / FadeOut;
                }
                else
                {
                    if (!_swapped)
                    {
                        _swapped = true;
                        ShowSlide(_pendingStep);
                    }
                    float p = Mathf.Clamp01((age - FadeOut) / FadeIn);
                    float ease = 1f - Mathf.Pow(1f - p, 3f);
                    alpha = ease;
                    shift = (1f - ease) * 24f;
                }
                _copyGroup.alpha = alpha;
                _copy.anchoredPosition = new Vector2(Mathf.Round(shift * U), 0f);

                float follow = 1f - Mathf.Exp(-Dt * 7f);
                for (int b = 0; b < Bars; b++)
                {
                    _barHeight[b] = Mathf.Lerp(_barHeight[b], _barTarget[b], follow);
                    _bars[b].rectTransform.sizeDelta = new Vector2(_barWidth * U, Mathf.Round(_barHeight[b] * U));
                    RectTransform value = _barValues[b].rectTransform;
                    value.anchoredPosition = new Vector2(value.anchoredPosition.x, Mathf.Round((_chartBottom + _barHeight[b] + 10f) * U));
                }
                MirrorStatus();
            }
        }

        /// <summary>
        /// Internal Affairs: the case file on the operator. The subject card on the left; a case
        /// header across the top; the session as a timeline of the subject's actions, every step
        /// listed from the start; and an evidence column of surveillance stills (the operator's
        /// own screenshots), each pinned as the timeline tags it as an exhibit.
        /// </summary>
        private sealed class CaseTimeline : Scene
        {
            private const int Exhibits = 3;
            private static readonly string[] Verbs = { "ACCESSED", "REQUESTED", "OPENED", "QUERIED", "INITIATED" };
            private TextMeshProUGUI _name;
            private TextMeshProUGUI _side;
            private TextMeshProUGUI _statusWord;
            private TextMeshProUGUI _risk;
            private TextMeshProUGUI _header;
            private Image _riskBar;
            private float _riskWidth;
            private Image _scan;
            private TextMeshProUGUI[] _times;
            private TextMeshProUGUI[] _entries;
            private Shape[] _nodes;
            private int[] _state;
            private string[] _verb;
            private int[] _exhibitOf;
            private DateTime[] _at;
            private RectTransform[] _cards;
            private CanvasGroup[] _cardGroups;
            private TextMeshProUGUI[] _cardTimes;
            private float[] _revealedAt;
            private string _case;
            private string _opened;
            private string _shownNick = "\0";
            private int _shownRisk = -1;
            private float _photoX;

            protected override void Build()
            {
                BrandMark(-HW + 80f, 398f, "INTERNAL AFFAIRS");
                float total = Mathf.Min(2f * HW - 200f, 1600f);
                float left = -total * 0.5f;
                _case = $"{Rng.Next(100, 1000)}-{Rng.Next(10, 100)}";
                _opened = DateTime.Now.AddSeconds(-StartupTracker.Elapsed).ToString("HH:mm");

                // The subject card.
                float cw = 360f;
                float cx = left + cw * 0.5f;
                Box(cx, -20f, cw, 720f, A(Color32.Lerp(T.Ink, T.Primary, 0.1f), 245));
                Shape card = Draw();
                Outline(card, cx, -20f, cw, 720f, 1f, A(T.Accent, 100));
                LabelAt($"CASE {_case}  //  OPEN", left + 24f, 316f, cw - 48f, 11f, T.Accent, spacing: 10f);
                _photoX = cx;
                Box(cx, 196f, 180f, 200f, Dark);
                Silhouette(card, cx, 186f, 1.6f, A(T.Dim, 150));
                _scan = Box(cx, 196f, 180f, 2f, A(T.Accent, 170));
                float fl = left + 30f;
                float fw = cw - 60f;
                LabelAt("SUBJECT", fl, 70f, fw, 10f, T.Dim);
                _name = LabelAt(string.Empty, fl, 48f, fw, 18f, T.Text);
                LabelAt("AFFILIATION", fl, 14f, fw, 10f, T.Dim);
                _side = LabelAt(string.Empty, fl, -6f, fw, 14f, T.Text);
                LabelAt("CLEARANCE", fl, -40f, fw, 10f, T.Dim);
                LabelAt("B  //  PROVISIONAL", fl, -60f, fw, 14f, T.Text);
                LabelAt("STATUS", fl, -94f, fw, 10f, T.Dim);
                string[] statuses = { "UNDER REVIEW", "SUSPENDED PENDING INQUIRY", "FLAGGED", "UNDER SURVEILLANCE" };
                _statusWord = LabelAt(statuses[Rng.Next(statuses.Length)], fl, -114f, fw, 14f, T.Warn);
                LabelAt("RISK SCORE", fl, -160f, fw, 10f, T.Dim);
                _risk = LabelAt(string.Empty, fl, -196f, fw, 34f, T.Text);
                Box(cx, -236f, fw, 6f, T.Faint);
                _riskBar = Bar(fl, -236f, 6f, T.Warn);
                _riskWidth = fw * U;
                Paragraph("THE SUBJECT IS NOT TO BE INFORMED OF THIS REVIEW. ALL ACTIVITY IS LOGGED FOR QUALITY PURPOSES.", fl, -268f, fw, 80f, 11f, T.Dim);
                card.Commit();

                // The evidence column, where there is room for it beside a readable timeline.
                float ew = 300f;
                float ex = left + total - ew;
                float tx = left + cw + 70f;
                bool evidence = ex - 40f - tx >= 360f;
                float tw = (evidence ? ex - 40f : left + total) - tx;

                // The case header across the top.
                float hl = tx - 20f;
                float hw = left + total - hl;
                Box(hl + hw * 0.5f, 316f, hw, 34f, A(Color32.Lerp(T.Ink, T.Primary, 0.16f), 240));
                _header = LabelAt(string.Empty, hl + 14f, 316f, hw - 28f, 11f, T.Text, spacing: 8f);

                // The timeline: every step listed, the ones not reached yet faint.
                string[] headings = { "SESSION TIMELINE", "OBSERVED ACTIVITY", "SURVEILLANCE LOG", "SEQUENCE OF EVENTS" };
                LabelAt(headings[Rng.Next(headings.Length)], tx, 270f, tw, 11f, T.Dim, spacing: 10f);
                int n = Steps - First;
                float rowH = Mathf.Min(40f, 580f / n);
                float top = 238f;
                Shape spine = Draw();
                spine.Line(P(tx, top + 6f), P(tx, top - (n - 1) * rowH - 6f), 2f * U, A(T.Dim, 120));
                spine.Commit();
                _times = new TextMeshProUGUI[n];
                _entries = new TextMeshProUGUI[n];
                _nodes = new Shape[n];
                _state = new int[n];
                _verb = new string[n];
                _exhibitOf = new int[n];
                _at = new DateTime[n];
                for (int k = 0; k < n; k++)
                {
                    float y = top - k * rowH;
                    Shape node = _nodes[k] = Draw(tx, y);
                    node.Disc(Vector2.zero, 6f * U, T.Accent);
                    node.Commit();
                    _times[k] = LabelAt(string.Empty, tx + 22f, y, 90f, 12f, T.Accent);
                    _entries[k] = LabelAt(string.Empty, tx + 112f, y, tw - 112f, 13f, T.Text);
                    _verb[k] = Verbs[Rng.Next(Verbs.Length)];
                    _exhibitOf[k] = -1;
                    _state[k] = -1;
                }
                // Three exhibits, spread over the session.
                for (int e = 0; e < Exhibits && n >= 4; e++)
                {
                    int k = Mathf.Clamp(Mathf.RoundToInt((e + 1) * n / (Exhibits + 1f)) - 1 + Rng.Next(-1, 2), 0, n - 1);
                    while (_exhibitOf[k] >= 0)
                    {
                        k = (k + 1) % n;
                    }
                    _exhibitOf[k] = e;
                }
                Status(tx + 22f, -372f, tw - 22f, 14f, true);

                _cards = new RectTransform[Exhibits];
                _cardGroups = new CanvasGroup[Exhibits];
                _cardTimes = new TextMeshProUGUI[Exhibits];
                _revealedAt = new float[Exhibits];
                if (!evidence)
                {
                    return;
                }
                LabelAt("EVIDENCE", ex, 270f, ew, 11f, T.Dim, spacing: 10f);
                Color32 paper = Color32.Lerp(T.Ink, T.Text, 0.1f);
                for (int e = 0; e < Exhibits; e++)
                {
                    float cy = 150f - e * 206f;
                    RectTransform exhibit = _cards[e] = MakeRect("Exhibit", R);
                    exhibit.anchoredPosition = P(ex + ew * 0.5f, cy);
                    _cardGroups[e] = exhibit.gameObject.AddComponent<CanvasGroup>();
                    _cardGroups[e].alpha = 0f;
                    Shape frame = Draw(0f, 0f, exhibit);
                    frame.RoundedRect(P(5f, -6f), ew * U, 190f * U, 6f * U, new Color32(0, 0, 0, 90));
                    frame.RoundedRect(Vector2.zero, ew * U, 190f * U, 6f * U, paper);
                    frame.RoundedRect(P(0f, 14f), (ew - 20f) * U, 142f * U, 3f * U, Dark);
                    Silhouette(frame, 0f, 10f, 1.1f, A(T.Dim, 90));
                    frame.Commit();
                    RectTransform view = MakeRect("Still", exhibit);
                    view.anchoredPosition = P(0f, 14f);
                    view.sizeDelta = P(ew - 20f, 142f);
                    view.gameObject.AddComponent<RectMask2D>();
                    Shape tag = Draw(-ew * 0.5f + 34f, 82f, exhibit);
                    tag.RoundedRect(Vector2.zero, 52f * U, 20f * U, 3f * U, T.Warn);
                    tag.Commit();
                    Label($"{(char)('A' + e)}", -ew * 0.5f + 34f, 82f, 52f, 11f, T.Ink, TextAlignmentOptions.Center, exhibit, 4f);
                    LabelAt($"EXHIBIT {(char)('A' + e)}  //  SURVEILLANCE STILL", -ew * 0.5f + 10f, -74f, ew - 20f, 10f, T.Text, parent: exhibit);
                    _cardTimes[e] = LabelAt(string.Empty, -ew * 0.5f + 10f, -88f, ew - 20f, 9f, T.Dim, parent: exhibit);
                    _revealedAt[e] = -1f;
                }
            }

            internal override void Animate(float t)
            {
                string nick = StartupTracker.Nickname;
                if (nick != _shownNick)
                {
                    _shownNick = nick;
                    string subject = string.IsNullOrEmpty(nick) ? "UNIDENTIFIED" : nick.ToUpperInvariant();
                    _name.text = subject;
                    _side.text = StartupTracker.Side != null ? StartupTracker.Side + " CONTRACTOR" : "UNKNOWN";
                    _header.text = $"CASE {_case}  //  SUBJECT {subject}  //  OPENED {_opened}  //  INVESTIGATOR [REDACTED]  //  PRIORITY HIGH";
                }
                _scan.rectTransform.anchoredPosition = P(_photoX, 196f + Mathf.Sin(t * 1.4f) * 96f);
                _statusWord.canvasRenderer.SetAlpha(t % 1.2f < 0.8f ? 1f : 0.35f);

                for (int k = 0; k < _state.Length; k++)
                {
                    int i = First + k;
                    int state = State(i);
                    bool live = state == Running || state == Slow;
                    _nodes[k].canvasRenderer.SetAlpha(state == Pending ? 0.15f : live ? 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(t * 4f)) : 1f);
                    if (state == _state[k])
                    {
                        continue;
                    }
                    _state[k] = state;
                    _at[k] = DateTime.Now;
                    int exhibit = _exhibitOf[k];
                    string time = _at[k].ToString("HH:mm:ss");
                    switch (state)
                    {
                        case Done:
                            _times[k].text = time;
                            _entries[k].text = $"SUBJECT {_verb[k]} {StepName(i)}"
                                + (exhibit >= 0 ? $"   <color={T.WarnHex}>EXHIBIT {(char)('A' + exhibit)}</color>" : string.Empty);
                            _entries[k].color = T.Text;
                            if (exhibit >= 0 && _cards[exhibit] != null && _revealedAt[exhibit] < 0f)
                            {
                                _revealedAt[exhibit] = t;
                                _cardTimes[exhibit].text = $"LOGGED {time}  //  CASE {_case}";
                            }
                            break;
                        case Running:
                            _times[k].text = time;
                            _entries[k].text = $"SUBJECT IS ACCESSING {StepName(i)}";
                            _entries[k].color = T.Accent;
                            break;
                        case Slow:
                            _times[k].text = time;
                            _entries[k].text = $"SUBJECT IS TAKING LONGER THAN EXPECTED: {StepName(i)}";
                            _entries[k].color = T.Warn;
                            break;
                        case Skipped:
                            _times[k].text = "--:--:--";
                            _entries[k].text = $"SUBJECT BYPASSED {StepName(i)}";
                            _entries[k].color = T.Dim;
                            break;
                        default:
                            _times[k].text = "PENDING";
                            _entries[k].text = StepName(i);
                            _entries[k].color = A(T.Dim, 90);
                            break;
                    }
                    _times[k].color = state == Pending ? A(T.Dim, 90) : state == Slow ? T.Warn : T.Accent;
                }

                // The exhibits: pinned as they are tagged.
                for (int e = 0; e < Exhibits; e++)
                {
                    if (_cards[e] == null)
                    {
                        break;
                    }
                    float age = _revealedAt[e] < 0f ? -1f : t - _revealedAt[e];
                    float shown = age < 0f ? 0f : Mathf.Clamp01(age / 0.5f);
                    float ease = 1f - Mathf.Pow(1f - shown, 3f);
                    _cardGroups[e].alpha = ease;
                    float baseY = 150f - e * 206f;
                    _cards[e].anchoredPosition = new Vector2(_cards[e].anchoredPosition.x, Mathf.Round((baseY + (1f - ease) * 18f) * U));
                }

                int risk = Mathf.Clamp(Mathf.RoundToInt(18f + 62f * Progress + 18f * O._alert), 0, 100);
                if (risk != _shownRisk)
                {
                    _shownRisk = risk;
                    _risk.text = risk + " / 100";
                    _riskBar.rectTransform.sizeDelta = new Vector2(Mathf.Round(_riskWidth * risk / 100f), _riskBar.rectTransform.sizeDelta.y);
                }
                MirrorStatus();
            }
        }
    }
}
