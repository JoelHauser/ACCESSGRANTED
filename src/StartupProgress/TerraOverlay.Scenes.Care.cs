using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StartupProgress
{
    internal partial class TerraOverlay
    {
        /// <summary>
        /// Communications: TerraGroup News 24, on air. The top story rotates through regional
        /// news; the running step is the BREAKING lower third; a live window shows the operator's
        /// own screenshots as field footage.
        /// </summary>
        private sealed class Newsroom : Scene
        {
            private static readonly string[] Subs =
            {
                "REPORTS FROM {place}, {map}. TERRAGROUP HAS DECLINED TO COMMENT.",
                "UNCONFIRMED. OUR CORRESPONDENT IN {map} WAS UNAVAILABLE.",
                "A TERRAGROUP SPOKESPERSON CALLED THE REPORTS 'SPECULATIVE'.",
                "MARKETS REACTED CALMLY. RESIDENTS OF {map} DID NOT.",
                "MORE ON THIS STORY AS IT DEVELOPS."
            };
            private string _banner;
            private TextMeshProUGUI _headline;
            private TextMeshProUGUI _sub;
            private TextMeshProUGUI _clock;
            private TextMeshProUGUI _detail;
            private TextMeshProUGUI _caption;
            private TextMeshProUGUI[] _marketValues;
            private float[] _market;
            private RawImage _feed;
            private Shape _live;
            private Image _bar;
            private float _barWidth;
            private float _storyAt = -10f;
            private float _nextMarket;
            private int _minute = -1;
            private string _shownDetail = "\0";
            private float _pan;

            protected override void Build()
            {
                FeedLibrary.Request();
                float total = Mathf.Min(2f * HW - 200f, 1560f);
                float left = -total * 0.5f;
                float right = total * 0.5f;

                // The channel's bug, top left.
                string[][] channels =
                {
                    new[] { "TGN", "24", "TERRAGROUP NEWS NETWORK", "BREAKING" },
                    new[] { "NVT", "1", "NORVINSK TODAY", "DEVELOPING" },
                    new[] { "TGB", "", "TERRAGROUP BUSINESS", "LIVE" }
                };
                string[] channel = channels[Rng.Next(channels.Length)];
                float bugX = -HW + 70f;
                Box(bugX + 40f, 398f, 80f, 44f, T.Primary);
                Label(channel[0], bugX + 40f, 398f, 80f, 20f, T.Text, TextAlignmentOptions.Center, null, 6f);
                LabelAt(channel[1], bugX + 88f, 398f, 60f, 22f, T.Accent);
                LabelAt(channel[2], bugX + 130f, 406f, 420f, 14f, T.Text, spacing: 8f);
                _banner = channel[3];
                LabelAt(DateTime.Now.ToString("dd MMM yyyy").ToUpperInvariant(), bugX + 130f, 386f, 420f, 11f, T.Dim);

                // The story panel.
                float storyWidth = total * 0.62f;
                Box(left + storyWidth * 0.5f, 62f, storyWidth, 476f, A(Color32.Lerp(T.Ink, T.Primary, 0.14f), 235));
                RawImage glow = MakeRaw("Glow", R, Art.Radial, A(T.Primary, 70));
                Place(glow.rectTransform, (left + storyWidth * 0.7f) * U, 40f * U, storyWidth * 0.9f * U, 500f * U);
                LabelAt("TOP STORY", left + 30f, 270f, 300f, 13f, T.Warn, spacing: 10f);
                _headline = Wrapped(left + 30f, 168f, storyWidth - 60f, 150f, 34f, T.Text);
                _sub = Wrapped(left + 30f, 52f, storyWidth - 60f, 70f, 15f, T.Dim);

                // The live window and the markets beside it.
                float liveLeft = left + storyWidth + 30f;
                float liveWidth = right - liveLeft;
                float liveHeight = Mathf.Min(240f, liveWidth * 0.62f);
                float liveY = 300f - liveHeight * 0.5f;
                Box(liveLeft + liveWidth * 0.5f, liveY, liveWidth, liveHeight, Dark);
                RectTransform view = MakeRect("Live", R);
                view.anchoredPosition = P(liveLeft + liveWidth * 0.5f, liveY);
                view.sizeDelta = P(liveWidth - 4f, liveHeight - 4f);
                view.gameObject.AddComponent<RectMask2D>();
                _feed = MakeRaw("Feed", view, null, A(Color32.Lerp(T.Text, T.Accent, 0.3f), 235));
                _feed.rectTransform.sizeDelta = P(liveWidth - 4f, liveHeight - 4f);
                _feed.gameObject.SetActive(false);
                _caption = LabelAt("LIVE", liveLeft + 26f, liveY - liveHeight * 0.5f + 16f, liveWidth - 40f, 11f, T.Text);
                _live = Draw(liveLeft + 14f, liveY - liveHeight * 0.5f + 16f);
                _live.Disc(Vector2.zero, 4f * U, T.Warn);
                _live.Commit();

                float marketsTop = liveY - liveHeight * 0.5f - 34f;
                LabelAt("MARKETS", liveLeft, marketsTop, liveWidth, 12f, T.Dim);
                string[] names = { "TGRP", "NVSK IDX", "BTC", "RUB/USD" };
                _marketValues = new TextMeshProUGUI[names.Length];
                _market = new float[names.Length];
                for (int i = 0; i < names.Length; i++)
                {
                    float y = marketsTop - 32f - i * 30f;
                    LabelAt(names[i], liveLeft, y, liveWidth, 14f, T.Text);
                    _market[i] = Range(80f, 900f);
                    _marketValues[i] = LabelAt(_market[i].ToString("0.00"), liveLeft, y, liveWidth, 14f, T.Accent, TextAlignmentOptions.Right);
                }

                // The lower third.
                Box(left + 110f, -250f, 220f, 58f, T.Warn);
                Label(_banner, left + 110f, -250f, 220f, 22f, T.Ink, TextAlignmentOptions.Center, null, 8f);
                Box(left + 220f + (total - 220f) * 0.5f, -250f, total - 220f, 58f, A(Color32.Lerp(T.Ink, T.Primary, 0.3f), 245));
                Status(left + 240f, -250f, total - 420f, 26f, true);
                _clock = LabelAt(string.Empty, right - 160f, -250f, 140f, 18f, T.Text, TextAlignmentOptions.Right);
                Box(0f, -298f, total, 36f, A(Dark, 240));
                _detail = LabelAt(string.Empty, left + 20f, -298f, total - 40f, 13f, T.Dim);
                Box(0f, -322f, total, 3f, T.Faint);
                _bar = Bar(left, -322f, 3f, T.Accent);
                _barWidth = total * U;
            }

            private TextMeshProUGUI Wrapped(float left, float y, float w, float h, float size, Color32 color)
            {
                TextMeshProUGUI label = Label(string.Empty, left + w * 0.5f, y, w, size, color);
                label.rectTransform.sizeDelta = P(w, h);
                label.enableWordWrapping = true;
                label.alignment = TextAlignmentOptions.TopLeft;
                label.overflowMode = TextOverflowModes.Truncate;
                return label;
            }

            internal override void Animate(float t)
            {
                // A new top story every few seconds, faded in.
                float age = t - _storyAt;
                if (age > 6.5f)
                {
                    _storyAt = t;
                    age = 0f;
                    int roll = Rng.Next(100);
                    _headline.text = roll < 6 ? Lore.EasterEgg(Rng)
                        : roll < 70 ? Lore.IntelLine(Rng)
                        : T.Headlines[Rng.Next(T.Headlines.Length)];
                    _sub.text = Lore.Fill(Subs[Rng.Next(Subs.Length)], Rng);
                }
                float alpha = Mathf.Clamp01(age / 0.4f) * Mathf.Clamp01((6.5f - age) / 0.3f);
                _headline.canvasRenderer.SetAlpha(alpha);
                _sub.canvasRenderer.SetAlpha(alpha);

                if (!_feed.gameObject.activeSelf)
                {
                    Texture2D frame = FeedLibrary.Texture(0);
                    if (frame != null)
                    {
                        _feed.texture = frame;
                        _feed.gameObject.SetActive(true);
                    }
                }
                else
                {
                    _pan += Dt * 0.02f;
                    float s = 0.8f;
                    _feed.uvRect = new Rect((Mathf.Sin(_pan) * 0.5f + 0.5f) * (1f - s), (Mathf.Cos(_pan * 0.7f) * 0.5f + 0.5f) * (1f - s), s, s);
                }
                _live.canvasRenderer.SetAlpha(t % 1f < 0.6f ? 1f : 0.2f);

                if (t >= _nextMarket)
                {
                    _nextMarket = t + 0.8f;
                    int i = Rng.Next(_market.Length);
                    float move = Range(-0.01f, 0.012f);
                    _market[i] *= 1f + move;
                    _marketValues[i].text = _market[i].ToString("0.00");
                    _marketValues[i].color = move >= 0f ? T.Accent : T.Warn;
                }

                DateTime now = DateTime.Now;
                if (now.Minute != _minute)
                {
                    _minute = now.Minute;
                    _clock.text = now.ToString("HH:mm");
                }
                string detail = Granted ? "COVERAGE CONTINUES ON THE MAIN MENU"
                    : O._slow ? T.SlowNote
                    : StartupTracker.Detail(StartupTracker.Current) ?? "LIVE COVERAGE  //  STEP " + (StartupTracker.Current - First + 1) + " OF " + (Steps - First);
                if (detail != _shownDetail)
                {
                    _shownDetail = detail;
                    _detail.text = detail.ToUpperInvariant();
                    _detail.color = O._slow ? T.Warn : T.Dim;
                }
                _bar.rectTransform.sizeDelta = new Vector2(Mathf.Round(_barWidth * Progress), _bar.rectTransform.sizeDelta.y);
                MirrorStatus();
            }
        }

        /// <summary>
        /// Facilities: the lift lobby. The indicator counts the floors up as the load goes, the
        /// building directory lists the steps as floors, and when access is granted the doors
        /// open.
        /// </summary>
        private sealed class LiftLobby : Scene
        {
            private TextMeshProUGUI _floor;
            private Shape _arrow;
            private RectTransform _leftDoor;
            private RectTransform _rightDoor;
            private TextMeshProUGUI[] _rows;
            private Image[] _rowGlow;
            private Shape[] _rowTicks;
            private int[] _state;
            private int _shownFloor = -1;
            private float _grantedAt = -1f;

            protected override void Build()
            {
                BrandMark(-HW + 80f, 398f, "FACILITIES MANAGEMENT");
                Color32 steel = Color32.Lerp(T.Ink, T.Text, 0.13f);

                // The floor indicator over the doors.
                Box(0f, 318f, 380f, 96f, Dark);
                Shape frame = Draw();
                Outline(frame, 0f, 318f, 380f, 96f, 2f, A(T.Accent, 140));
                string[] liftNames = { "LIFT A  //  NORTH CORE", "LIFT 3  //  LABS ANNEX", "EXECUTIVE LIFT  //  TOWER B", "GOODS LIFT  //  LOADING BAY" };
                LabelAt(liftNames[Rng.Next(liftNames.Length)], -176f, 352f, 360f, 10f, T.Dim);
                _floor = Label(string.Empty, 30f, 316f, 220f, 64f, T.Accent, TextAlignmentOptions.Center, null, 4f);
                _arrow = Draw(-110f, 318f);
                _arrow.Tri(P(0f, 16f), P(-14f, -10f), P(14f, -10f), T.Accent);
                _arrow.Commit();

                // The doors, with the light of the car behind them.
                Box(0f, -50f, 380f, 520f, A(Color32.Lerp(T.Accent, new Color32(255, 240, 210, 255), 0.6f), 140));
                _leftDoor = Box(-95f, -50f, 188f, 516f, steel).rectTransform;
                _rightDoor = Box(95f, -50f, 188f, 516f, steel).rectTransform;
                foreach (RectTransform door in new[] { _leftDoor, _rightDoor })
                {
                    Shape brush = Draw(0f, 0f, door);
                    for (float x = -80f; x <= 80f; x += 16f)
                    {
                        brush.Line(P(x, 250f), P(x, -250f), U, A(T.Text, 14));
                    }
                    brush.Commit();
                }
                Outline(frame, 0f, -50f, 384f, 524f, 6f, A(T.Dim, 200));
                frame.Commit();
                Status(-190f, -340f, 380f, 16f, true, TextAlignmentOptions.Center);

                // The building directory: the steps, top floor first.
                float side = Mathf.Min(420f, HW - 190f - 60f - 70f);
                int n = Steps - First;
                _rows = new TextMeshProUGUI[n];
                _rowGlow = new Image[n];
                _rowTicks = new Shape[n];
                _state = new int[n];
                if (side >= 260f)
                {
                    float x = -(190f + 60f + side * 0.5f);
                    Box(x, -5f, side, 610f, A(Dark, 235));
                    Shape edge = Draw();
                    Outline(edge, x, -5f, side, 610f, 1f, A(T.Dim, 90));
                    edge.Commit();
                    LabelAt("BUILDING DIRECTORY", x - side * 0.5f + 20f, 272f, side - 40f, 12f, T.Dim, spacing: 10f);
                    float rowH = Mathf.Min(34f, 520f / n);
                    for (int k = 0; k < n; k++)
                    {
                        int floor = n - k;
                        int i = First + floor - 1;
                        float y = 236f - k * rowH;
                        _rowGlow[floor - 1] = Box(x, y, side - 16f, rowH - 2f, T.Accent);
                        _rowGlow[floor - 1].canvasRenderer.SetAlpha(0f);
                        _rows[floor - 1] = LabelAt($"<mspace=0.62em>{floor,2}</mspace>    {StepName(i)}", x - side * 0.5f + 20f, y, side - 70f, 12f, T.Text);
                        Shape tick = _rowTicks[floor - 1] = Draw(x + side * 0.5f - 26f, y);
                        tick.Line(P(-5f, 0f), P(-1f, -4f), 2f * U, T.Accent);
                        tick.Line(P(-1f, -4f), P(6f, 6f), 2f * U, T.Accent);
                        tick.Commit();
                        tick.gameObject.SetActive(false);
                        _state[floor - 1] = -1;
                    }

                    // Building services, on the other side.
                    float sx = -x;
                    Box(sx, -5f, side, 610f, A(Dark, 235));
                    Shape edge2 = Draw();
                    Outline(edge2, sx, -5f, side, 610f, 1f, A(T.Dim, 90));
                    edge2.Commit();
                    float l = sx - side * 0.5f + 20f;
                    float w = side - 40f;
                    LabelAt("BUILDING SERVICES", l, 272f, w, 12f, T.Dim, spacing: 10f);
                    string[] lifts = { "LIFT A", "LIFT B", "LIFT C", "SERVICE LIFT" };
                    string[] states = { "IN SERVICE", "OUT OF SERVICE", "IN SERVICE", "RESTRICTED" };
                    for (int i = 0; i < lifts.Length; i++)
                    {
                        float y = 232f - i * 30f;
                        LabelAt(lifts[i], l, y, w, 13f, T.Text);
                        LabelAt(states[i], l, y, w, 13f, i == 1 ? T.Warn : i == 3 ? T.Dim : T.Accent, TextAlignmentOptions.Right);
                    }
                    LabelAt("DAYS WITHOUT INCIDENT", l, 90f, w, 11f, T.Dim);
                    LabelAt(Rng.Next(3).ToString(), l, 40f, w, 56f, T.Warn);
                    LabelAt("NOTICE", l, -30f, w, 11f, T.Dim);
                    TextMeshProUGUI notice = Label(Lore.Fill(T.Memos[Rng.Next(T.Memos.Length)], Rng), l + w * 0.5f, -110f, w, 14f, T.Text);
                    notice.rectTransform.sizeDelta = P(w, 140f);
                    notice.enableWordWrapping = true;
                    notice.alignment = TextAlignmentOptions.TopLeft;
                }
            }

            internal override void Animate(float t)
            {
                int floor = Granted ? Steps - First : Mathf.Clamp(StartupTracker.Current - First + 1, 1, Steps - First);
                if (floor != _shownFloor)
                {
                    _shownFloor = floor;
                    _floor.text = floor.ToString();
                }
                _arrow.canvasRenderer.SetAlpha(Granted ? 0.2f : 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(t * 3f)));

                // The doors part once access is granted.
                if (Granted && _grantedAt < 0f)
                {
                    _grantedAt = t;
                }
                float open = _grantedAt < 0f ? 0f : Mathf.Clamp01((t - _grantedAt - 0.2f) / 0.9f);
                float ease = open * open * (3f - 2f * open);
                // Each door folds into its side of the frame: its outer edge stays put while it narrows.
                _leftDoor.anchoredPosition = P(-95f - 93f * ease, -50f);
                _rightDoor.anchoredPosition = P(95f + 93f * ease, -50f);
                _leftDoor.localScale = _rightDoor.localScale = new Vector3(1f - ease, 1f, 1f);

                for (int k = 0; k < _rows.Length; k++)
                {
                    if (_rows[k] == null)
                    {
                        break;
                    }
                    int state = State(First + k);
                    bool live = state == Running || state == Slow;
                    _rowGlow[k].canvasRenderer.SetAlpha(live ? 0.08f + 0.04f * Mathf.Sin(t * 3f) : 0f);
                    if (state == _state[k])
                    {
                        continue;
                    }
                    _state[k] = state;
                    _rows[k].color = StateColor(state);
                    _rowTicks[k].gameObject.SetActive(state == Done);
                }
                MirrorStatus();
            }
        }
    }
}
