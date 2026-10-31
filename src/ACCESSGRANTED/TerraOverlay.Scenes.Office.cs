using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ACCESSGRANTED
{
    internal partial class TerraOverlay
    {
        /// <summary>
        /// IT: the service desk's ticket board. Every step is a ticket that moves from QUEUED to
        /// IN PROGRESS to RESOLVED as the load works through it; a slow step breaches its SLA.
        /// </summary>
        private sealed class TicketBoard : Scene
        {
            private const float CardH = 54f;
            private const float CardGap = 8f;
            private RectTransform[] _cards;
            private Image[] _stripes;
            private TextMeshProUGUI[] _slas;
            private Vector2[] _at;
            private int[] _state;
            private float[] _column;
            private float _columnWidth;
            private TextMeshProUGUI[] _counts;
            private TextMeshProUGUI[] _more;
            private TextMeshProUGUI _summary;
            private int _shownTenths = -1;
            private int _layoutKey = -1;
            private readonly float[] _targetY = new float[StartupTracker.StepCount];
            private readonly float[] _targetAlpha = new float[StartupTracker.StepCount];
            private readonly int[] _targetColumn = new int[StartupTracker.StepCount];

            protected override void Build()
            {
                BrandMark(-HW + 80f, 398f, "IT SERVICE DESK");
                float total = Mathf.Min(2f * HW - 200f, 1600f);
                float left = -total * 0.5f;
                _columnWidth = (total - 48f) / 3f;
                _column = new float[3];
                _counts = new TextMeshProUGUI[3];
                _more = new TextMeshProUGUI[3];
                string[] heads = { "QUEUED", "IN PROGRESS", "RESOLVED" };
                for (int c = 0; c < 3; c++)
                {
                    float x = left + _columnWidth * 0.5f + c * (_columnWidth + 24f);
                    _column[c] = x;
                    Box(x, -25f, _columnWidth, 650f, A(Dark, 200));
                    LabelAt(heads[c], x - _columnWidth * 0.5f + 16f, 280f, _columnWidth * 0.6f, 13f, c == 1 ? T.Accent : T.Dim, spacing: 10f);
                    _counts[c] = LabelAt(string.Empty, x + _columnWidth * 0.5f - 116f, 280f, 100f, 13f, T.Dim, TextAlignmentOptions.Right);
                    _more[c] = LabelAt(string.Empty, x - _columnWidth * 0.5f + 16f, -330f, _columnWidth - 32f, 11f, T.Dim, TextAlignmentOptions.Center);
                }
                _summary = LabelAt(string.Empty, left + total - 500f, 330f, 500f, 12f, T.Dim, TextAlignmentOptions.Right);
                string[] queues = { "QUEUE: SYSTEM STARTUP", "QUEUE: CRITICAL INCIDENTS", "QUEUE: LABS NETWORK", "QUEUE: EXECUTIVE SUPPORT" };
                LabelAt("SERVICE DESK  //  " + queues[Rng.Next(queues.Length)], left, 330f, 700f, 13f, T.Text, spacing: 8f);

                int n = Steps - First;
                _cards = new RectTransform[n];
                _stripes = new Image[n];
                _slas = new TextMeshProUGUI[n];
                _at = new Vector2[n];
                _state = new int[n];
                float cw = _columnWidth - 24f;
                for (int k = 0; k < n; k++)
                {
                    int i = First + k;
                    RectTransform card = _cards[k] = MakeRect("Ticket", R);
                    card.gameObject.AddComponent<CanvasGroup>();
                    Box(0f, 0f, cw, CardH, Color32.Lerp(T.Ink, T.Primary, 0.2f), card);
                    _stripes[k] = Box(-cw * 0.5f + 3f, 0f, 6f, CardH, T.Dim, card);
                    LabelAt($"#{4000 + Rng.Next(100, 999)}  //  P{Rng.Next(1, 4)}", -cw * 0.5f + 16f, 14f, cw * 0.5f, 10f, T.Dim, parent: card);
                    LabelAt($"SRV-{Rng.Next(1, 9):00}", cw * 0.5f - 116f, 14f, 100f, 10f, T.Dim, TextAlignmentOptions.Right, parent: card);
                    LabelAt(StepName(i), -cw * 0.5f + 16f, -8f, cw - 120f, 13f, T.Text, parent: card);
                    _slas[k] = LabelAt(string.Empty, cw * 0.5f - 116f, -8f, 100f, 11f, T.Dim, TextAlignmentOptions.Right, parent: card);
                    _state[k] = -1;
                    _at[k] = new Vector2(_column[0], 240f);
                    card.anchoredPosition = P(_at[k].x, _at[k].y);
                }
                Status(left, -378f, total * 0.6f, 14f, true);
            }

            /// <summary>Where every ticket should be: its column, and its place in the column's stack.</summary>
            private void Arrange()
            {
                int[] counts = new int[3];
                int rows = Mathf.FloorToInt(580f / (CardH + CardGap));
                // Queued and in progress run in step order; resolved shows the newest first.
                for (int pass = 0; pass < 2; pass++)
                {
                    for (int k = pass == 0 ? 0 : _state.Length - 1; pass == 0 ? k < _state.Length : k >= 0; k += pass == 0 ? 1 : -1)
                    {
                        int state = _state[k];
                        int column = state == Pending ? 0 : state == Running || state == Slow ? 1 : 2;
                        if ((pass == 0) == (column == 2))
                        {
                            continue;
                        }
                        int index = counts[column]++;
                        _targetColumn[k] = column;
                        _targetY[k] = 240f - index * (CardH + CardGap);
                        _targetAlpha[k] = index < rows ? 1f : 0f;
                    }
                }
                for (int c = 0; c < 3; c++)
                {
                    _counts[c].text = counts[c].ToString();
                    _more[c].text = counts[c] > rows ? $"+ {counts[c] - rows} MORE" : string.Empty;
                }
                _summary.text = $"{counts[2]} OF {_state.Length} RESOLVED";
            }

            internal override void Animate(float t)
            {
                int key = 0;
                for (int k = 0; k < _state.Length; k++)
                {
                    int state = State(First + k);
                    key = key * 5 + state;
                    if (state == _state[k])
                    {
                        continue;
                    }
                    _state[k] = state;
                    _stripes[k].color = state == Slow ? T.Warn : state == Running ? T.Accent : state == Done ? A(T.Dim, 120) : T.Dim;
                    _slas[k].text = state == Done ? $"{StartupTracker.TookSeconds(First + k):0.0}S" : state == Skipped ? "WONT FIX" : string.Empty;
                    _slas[k].color = T.Dim;
                }
                if (key != _layoutKey)
                {
                    _layoutKey = key;
                    Arrange();
                }

                // The running ticket's SLA clock; past its usual time, it is in breach.
                int current = StartupTracker.Current - First;
                if (!Granted && current >= 0 && current < _state.Length)
                {
                    int tenths = Mathf.FloorToInt(StartupTracker.StepElapsed * 10f);
                    if (tenths != _shownTenths)
                    {
                        _shownTenths = tenths;
                        _slas[current].text = O._slow ? "SLA BREACHED" : $"{tenths / 10f:0.0}S";
                        _slas[current].color = O._slow ? T.Warn : T.Accent;
                    }
                }

                float follow = 1f - Mathf.Exp(-Dt * 8f);
                for (int k = 0; k < _cards.Length; k++)
                {
                    Vector2 target = new Vector2(_column[_targetColumn[k]], _targetY[k]);
                    _at[k] = Vector2.Lerp(_at[k], target, follow);
                    _cards[k].anchoredPosition = P(_at[k].x, _at[k].y);
                    CanvasGroup group = _cards[k].GetComponent<CanvasGroup>();
                    group.alpha = Mathf.Lerp(group.alpha, _targetAlpha[k], follow);
                }
                MirrorStatus();
            }
        }

        /// <summary>
        /// Human Resources: the operator's onboarding. The steps are the onboarding checklist,
        /// each with its HR task; beside it, the operator's staff badge prints, coming out of
        /// the printer as the load goes.
        /// </summary>
        private sealed class Onboarding : Scene
        {
            private static readonly string[][] Processes =
            {
                new[] { "NEW STARTER ONBOARDING", "ONBOARDING" },
                new[] { "ANNUAL PERFORMANCE REVIEW", "REVIEW" },
                new[] { "SECURITY CLEARANCE RENEWAL", "RENEWAL" }
            };
            private static readonly string[] ReviewTasks =
            {
                "SELF-ASSESSMENT", "MANAGER SIGN-OFF", "OBJECTIVES REVIEW", "ATTENDANCE RECORD", "INCIDENT HISTORY",
                "PEER FEEDBACK", "TRAINING RECORD", "LOYALTY ASSESSMENT", "SALARY BAND CHECK", "DISCIPLINARY CHECK",
                "REMEDIAL ACTIONS", "LOADOUT AUDIT", "VOICE STRESS TEST", "FINAL RATING", "REPORT TO YOUR DESK"
            };
            private static readonly string[] ClearanceTasks =
            {
                "IDENTITY CHECK", "BACKGROUND CHECK", "FINANCIAL CHECK", "FAMILY CONTACTS LISTED", "TRAVEL HISTORY",
                "POLYGRAPH", "FACTION DECLARATION", "MEDICAL CLEARANCE", "KEYCARD RE-ISSUE", "SYSTEMS RE-ACCESS",
                "NDA RE-SIGNED", "LOADOUT APPROVAL", "VOICE SAMPLE", "SITE RE-INDUCTION", "CLEARANCE GRANTED"
            };
            private int _process;
            private static readonly string[] Tasks =
            {
                "RIGHT-TO-WORK CHECK", "NON-DISCLOSURE AGREEMENT", "UNIFORM ISSUE", "DESK ASSIGNMENT", "KEYCARD COLLECTION",
                "SYSTEMS ACCESS", "EQUIPMENT ISSUE", "HEALTH AND SAFETY BRIEFING", "NEXT-OF-KIN FORM", "PAYROLL SET-UP",
                "STAFF HANDBOOK RECEIPT", "LOADOUT APPROVAL", "VOICE SAMPLE", "SITE INDUCTION: NORVINSK", "REPORT TO YOUR DESK"
            };
            private Shape[] _ticks;
            private TextMeshProUGUI[] _tasks;
            private TextMeshProUGUI[] _times;
            private int[] _state;
            private TextMeshProUGUI _title;
            private TextMeshProUGUI _percent;
            private TextMeshProUGUI _printing;
            private TextMeshProUGUI _badgeName;
            private Shape _led;
            private Shape _hologram;
            private Image _head;
            private Image _bar;
            private float _barWidth;
            private RectTransform _badge;
            private float _issuedAt = -1f;
            private int _shownPrinted = -1;
            private float _slotY;
            private float _badgeH;
            private string _shownNick = "\0";
            private int _shownPct = -1;

            protected override void Build()
            {
                BrandMark(-HW + 80f, 398f, "HUMAN RESOURCES");
                _process = Rng.Next(Processes.Length);
                float total = Mathf.Min(2f * HW - 200f, 1600f);
                float left = -total * 0.5f;
                float fw = Mathf.Min(total * 0.58f, 900f);

                // The checklist form.
                Box(left + fw * 0.5f, -20f, fw, 720f, A(Color32.Lerp(T.Ink, T.Primary, 0.08f), 245));
                Shape form = Draw();
                Outline(form, left + fw * 0.5f, -20f, fw, 720f, 1f, A(T.Accent, 90));
                float l = left + 30f;
                float w = fw - 60f;
                _title = LabelAt(string.Empty, l, 312f, w, 18f, T.Text, spacing: 8f);
                _percent = LabelAt(string.Empty, l, 284f, w, 12f, T.Accent);
                Box(left + fw * 0.5f, 262f, w, 4f, T.Faint);
                _bar = Bar(l, 262f, 4f, T.Accent);
                _barWidth = w * U;
                int n = Steps - First;
                float rowH = Mathf.Min(40f, 580f / n);
                _ticks = new Shape[n];
                _tasks = new TextMeshProUGUI[n];
                _times = new TextMeshProUGUI[n];
                _state = new int[n];
                for (int k = 0; k < n; k++)
                {
                    int i = First + k;
                    float y = 226f - k * rowH;
                    Outline(form, l + 8f, y, 16f, 16f, 1.5f, A(T.Dim, 170));
                    Shape tick = _ticks[k] = Draw(l + 8f, y);
                    Tick(tick, 0f, 0f, 1f, T.Accent);
                    tick.Commit();
                    tick.gameObject.SetActive(false);
                    string[] tasks = _process == 1 ? ReviewTasks : _process == 2 ? ClearanceTasks : Tasks;
                    _tasks[k] = LabelAt($"{k + 1:00}   {tasks[i]}", l + 30f, y + 6f, w - 150f, 13f, T.Text);
                    LabelAt(StepName(i), l + 72f, y - 10f, w - 190f, 9f, T.Dim);
                    _times[k] = LabelAt(string.Empty, l + w - 120f, y, 120f, 11f, T.Dim, TextAlignmentOptions.Right);
                    _state[k] = -1;
                }
                form.Commit();

                // The card printer, and the staff card feeding out of it.
                float px = left + fw + (total - fw) * 0.5f;
                _slotY = 262f;
                Color32 casing = Color32.Lerp(T.Ink, T.Text, 0.16f);
                Shape printer = Draw(px, 0f);
                printer.RoundedRect(P(0f, 300f), 420f * U, 120f * U, 14f * U, casing);
                printer.RoundedRect(P(0f, 344f), 420f * U, 32f * U, 14f * U, 14f * U, 0f, 0f, Color32.Lerp(casing, new Color32(0, 0, 0, 255), 0.25f));
                printer.RoundedRect(P(-110f, 306f), 150f * U, 34f * U, 5f * U, new Color32(8, 12, 10, 255));
                printer.RoundedRect(P(0f, _slotY), 370f * U, 12f * U, 4f * U, new Color32(0, 0, 0, 255));
                printer.Line(P(-190f, _slotY - 10f), P(190f, _slotY - 10f), 2f * U, Color32.Lerp(casing, new Color32(255, 255, 255, 255), 0.15f));
                printer.Commit();
                _led = Draw(px + 170f, 306f);
                _led.Disc(Vector2.zero, 5f * U, T.Accent);
                _led.Commit();
                LabelAt("TG-CP300", px + 40f, 306f, 110f, 10f, A(T.Text, 120));
                _printing = LabelAt(string.Empty, px - 178f, 306f, 136f, 11f, T.Accent, TextAlignmentOptions.Center);
                _head = Box(px, _slotY, 360f, 3f, T.Accent);

                RectTransform tray = MakeRect("Tray", R);
                tray.anchoredPosition = P(px, (_slotY - 6f + -390f) * 0.5f);
                tray.sizeDelta = P(400f, _slotY - 6f + 390f);
                tray.gameObject.AddComponent<RectMask2D>();
                _badge = MakeRect("Card", tray);
                _badgeH = 214f;

                // A landscape staff card: branded header, photo, details, chip, hologram, barcode,
                // all with soft edges and rounded corners.
                Color32 card = Color32.Lerp(T.Text, new Color32(255, 255, 255, 255), 0.5f);
                Color32 ink = Color32.Lerp(T.Ink, new Color32(0, 0, 0, 255), 0.35f);
                Color32 gold = new Color32(214, 176, 92, 255);
                Color32 goldLine = new Color32(170, 132, 60, 255);
                Shape face = Draw(0f, 0f, _badge);
                face.RoundedRect(P(7f, -9f), 340f * U, 214f * U, 14f * U, new Color32(0, 0, 0, 80));
                face.RoundedRect(Vector2.zero, 340f * U, 214f * U, 14f * U, card);
                face.RoundedRect(P(0f, 82f), 340f * U, 50f * U, 14f * U, 14f * U, 0f, 0f, T.Primary);
                face.RoundedRect(P(-112f, -26f), 96f * U, 112f * U, 6f * U, Color32.Lerp(card, T.Primary, 0.14f));
                Silhouette(face, -112f, -26f, 0.92f, A(T.Primary, 120));
                face.RoundedRect(P(128f, 24f), 42f * U, 32f * U, 5f * U, gold);
                face.Line(P(107f, 24f), P(149f, 24f), U, goldLine);
                face.Line(P(128f, 8f), P(128f, 40f), U, goldLine);
                face.Line(P(116f, 8f), P(116f, 40f), U, A(goldLine, 120));
                face.Line(P(140f, 8f), P(140f, 40f), U, A(goldLine, 120));
                for (float x = -50f; x < 92f;)
                {
                    float bw = Rng.Next(1, 4);
                    face.Line(P(x + bw * 0.5f, -70f), P(x + bw * 0.5f, -92f), bw * U, ink);
                    x += bw + Rng.Next(1, 4);
                }
                face.Commit();

                // The TerraGroup mark in the header.
                Shape mark = Draw(-148f, 82f, _badge);
                float r = 13f * U;
                AddPiece(mark, new Vector2(-r, 0f), new Vector2(0f, r), r * LogoGap, card, null);
                AddPiece(mark, new Vector2(0f, r), new Vector2(r, 0f), r * LogoGap, card, null);
                AddPiece(mark, new Vector2(-r, 0f), new Vector2(0f, -r), r * LogoGap, card, null);
                AddPiece(mark, new Vector2(r, 0f), new Vector2(0f, -r), r * LogoGap, card, T.Primary);
                mark.Commit();

                _hologram = Draw(130f, -66f, _badge);
                _hologram.Disc(Vector2.zero, 15f * U, card);
                _hologram.Arc(13f * U, 2f * U, 0f, 360f, A(T.Accent, 200));
                _hologram.Arc(9f * U, 2f * U, 0f, 360f, A(T.Warn, 180));
                _hologram.Arc(5f * U, 2f * U, 0f, 360f, A(T.Primary, 180));
                _hologram.Commit();

                LabelAt("TERRAGROUP", -128f, 90f, 200f, 15f, card, spacing: 10f, parent: _badge);
                LabelAt("STAFF IDENTIFICATION", -128f, 72f, 200f, 8f, A(card, 210), spacing: 8f, parent: _badge);
                _badgeName = LabelAt(string.Empty, -50f, 22f, 150f, 17f, ink, spacing: 4f, parent: _badge);
                LabelAt($"EMPLOYEE {Rng.Next(10, 100)}-{Rng.Next(1000, 10000)}", -50f, 0f, 160f, 10f, A(ink, 180), parent: _badge);
                LabelAt((StartupTracker.Side ?? "FIELD") + " CONTRACTOR", -50f, -16f, 160f, 10f, T.Primary, parent: _badge);
                LabelAt($"CLEARANCE B  //  VALID {DateTime.Now.AddYears(1):MM/yyyy}", -50f, -32f, 200f, 8f, A(ink, 150), parent: _badge);
                Status(left, -376f, fw, 14f, true);
            }

            internal override void Animate(float t)
            {
                string nick = StartupTracker.Nickname;
                if (nick != _shownNick)
                {
                    _shownNick = nick;
                    bool known = !string.IsNullOrEmpty(nick);
                    _title.text = Processes[_process][0] + "  //  " + (known ? nick.ToUpperInvariant() : "PENDING");
                    _badgeName.text = known ? nick.ToUpperInvariant() : "NEW STARTER";
                }
                for (int k = 0; k < _state.Length; k++)
                {
                    int state = State(First + k);
                    if (state == _state[k])
                    {
                        continue;
                    }
                    _state[k] = state;
                    _ticks[k].gameObject.SetActive(state == Done);
                    _tasks[k].color = StateColor(state);
                    _times[k].text = state == Done ? "COMPLETE" : state == Running ? "IN PROGRESS" : state == Slow ? "AWAITING SIGN-OFF" : state == Skipped ? "WAIVED" : string.Empty;
                    _times[k].color = state == Slow ? T.Warn : state == Running ? T.Accent : T.Dim;
                }
                int percent = Mathf.FloorToInt(Progress * 100f);
                if (percent != _shownPct)
                {
                    _shownPct = percent;
                    _percent.text = $"{Processes[_process][1]} {percent}% COMPLETE";
                    _bar.rectTransform.sizeDelta = new Vector2(Mathf.Round(_barWidth * Progress), _bar.rectTransform.sizeDelta.y);
                }
                // The card feeds out of the slot with the load, and drops into the tray when issued.
                float trayTop = (_slotY - 6f - (-390f)) * 0.5f;
                float y = trayTop + _badgeH * 0.5f - Progress * (_badgeH + 20f);
                if (Granted && _issuedAt < 0f)
                {
                    _issuedAt = t;
                }
                if (_issuedAt >= 0f)
                {
                    // Dropped into the tray: a short fall and a small, damped bounce, kept upright.
                    float age = t - _issuedAt;
                    y -= Mathf.Min(age / 0.25f, 1f) * 14f - Mathf.Abs(Mathf.Sin(age * 14f)) * Mathf.Exp(-age * 6f) * 6f;
                }
                _badge.anchoredPosition = P(0f, Mathf.Round(y));
                // The printer: its display, its status light, the glow of the print head at the slot.
                int printed = Mathf.FloorToInt(Progress * 100f);
                if (printed != _shownPrinted)
                {
                    _shownPrinted = printed;
                    _printing.text = Granted ? "COMPLETE" : $"PRINTING {printed}%";
                }
                _led.canvasRenderer.SetAlpha(Granted ? 1f : t % 0.8f < 0.4f ? 1f : 0.25f);
                _head.canvasRenderer.SetAlpha(Granted ? 0f : 0.35f + 0.35f * Mathf.Abs(Mathf.Sin(t * 9f)));
                // Once printed, light catches the hologram now and then.
                _hologram.canvasRenderer.SetAlpha(Granted ? 0.75f + 0.25f * Mathf.Sin(t * 5f) : 0.8f);
                MirrorStatus();
            }
        }
    }
}
