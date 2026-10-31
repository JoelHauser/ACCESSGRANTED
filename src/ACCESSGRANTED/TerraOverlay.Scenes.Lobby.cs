using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ACCESSGRANTED
{
    internal partial class TerraOverlay
    {
        /// <summary>
        /// Corporate HQ: the lobby's welcome screen. A personal greeting, the company values,
        /// the sign-in as a stepper across the screen, and on the right the clock, the weather
        /// in Norvinsk and the day at headquarters.
        /// </summary>
        private sealed class LobbyWelcome : Scene
        {
            private static readonly string[] Words = { "INTEGRITY", "INNOVATION", "DISCRETION", "SECURITY", "EXCELLENCE", "LOYALTY" };
            private static readonly string[] Lines =
            {
                "WE DO WHAT WE SAY.", "TOMORROW, TODAY.", "WHAT HAPPENS HERE STAYS HERE.",
                "YOUR SAFETY IS OUR PRIORITY.", "NOTHING LESS IS ACCEPTED.", "WE REMEMBER WHO STAYED."
            };
            private static readonly string[] Events =
            {
                "08:30|BOARD BREAKFAST  //  FLOOR 40", "10:00|SAFETY BRIEFING  //  AUDITORIUM", "11:30|SHAREHOLDER CALL  //  ROOM 2B",
                "13:00|CAFETERIA B CLOSED FOR CLEANING", "14:00|VISITOR TOUR  //  CANCELLED", "15:30|TOWN HALL  //  POSTPONED",
                "16:00|LABS STATUS REVIEW  //  RESTRICTED", "17:30|FIRE DRILL  //  ALL FLOORS", "18:00|SECURITY HANDOVER",
                "19:00|CLEANING CREW ON SITE", "20:30|PERIMETER LIGHTS ON", "22:00|BUILDING LOCKDOWN"
            };
            private static readonly string[] Weather = { "OVERCAST", "LIGHT SNOW", "FOG", "RAIN", "CLEAR", "SLEET" };
            private static readonly string[] Advisories =
            {
                "STAFF ARE ADVISED TO STAY INDOORS", "AVOID THE EAST WING", "NO ADVISORIES IN EFFECT",
                "CHECKPOINT DELAYS ON THE CITY ROAD", "POOR VISIBILITY ON THE COAST ROAD"
            };
            private TextMeshProUGUI _greeting;
            private TextMeshProUGUI _word;
            private TextMeshProUGUI _line;
            private TextMeshProUGUI _clock;
            private TextMeshProUGUI _step;
            private Shape[] _nodes;
            private Shape _ring;
            private Image _track;
            private float _trackLeft;
            private float _trackWidth;
            private int[] _state;
            private int _shownWord = -1;
            private int _minute = -1;
            private int _shownKey = -1;
            private string _shownNick = "\0";

            protected override void Build()
            {
                float total = Mathf.Min(2f * HW - 200f, 1640f);
                float left = -total * 0.5f;
                float main = total * 0.6f;

                // The company mark, large.
                Shape logo = Draw(left + 60f, 280f);
                float r = 56f * U;
                float gap = r * LogoGap;
                AddPiece(logo, new Vector2(-r, 0f), new Vector2(0f, r), gap, T.Primary, null);
                AddPiece(logo, new Vector2(0f, r), new Vector2(r, 0f), gap, T.Primary, null);
                AddPiece(logo, new Vector2(-r, 0f), new Vector2(0f, -r), gap, T.Primary, null);
                AddPiece(logo, new Vector2(r, 0f), new Vector2(0f, -r), gap, T.Primary, T.Text);
                logo.Commit();
                LabelAt("TERRAGROUP", left + 140f, 296f, main, 44f, T.Text, spacing: 24f);
                LabelAt("HEADQUARTERS  //  NORVINSK", left + 142f, 254f, main, 13f, T.Accent, spacing: 12f);

                _greeting = LabelAt(string.Empty, left, 140f, main, 50f, T.Text, spacing: 4f);
                LabelAt("WELCOME TO TERRAGROUP HEADQUARTERS. PLEASE HAVE YOUR BADGE READY.", left, 88f, main, 15f, T.Dim);
                LabelAt("OUR VALUES", left, 30f, main, 11f, T.Dim, spacing: 10f);
                _word = LabelAt(string.Empty, left, -6f, main, 30f, T.Accent, spacing: 12f);
                _line = LabelAt(string.Empty, left, -40f, main, 15f, T.Text);

                // The sign-in, as a stepper across the bottom of the main column.
                int n = Steps - First;
                _trackLeft = left + 10f;
                _trackWidth = main - 20f;
                float sy = -190f;
                Box(left + main * 0.5f, sy, _trackWidth, 2f, T.Faint);
                _track = Bar(_trackLeft, sy, 2f, T.Accent);
                _nodes = new Shape[n];
                _state = new int[n];
                for (int k = 0; k < n; k++)
                {
                    float x = _trackLeft + _trackWidth * k / Mathf.Max(1, n - 1);
                    Shape node = _nodes[k] = Draw(x, sy);
                    node.Disc(Vector2.zero, 7f * U, T.Accent);
                    node.Commit();
                    _state[k] = -1;
                }
                _ring = Draw(_trackLeft, sy);
                _ring.Arc(14f * U, 2f * U, 0f, 360f, T.Accent);
                _ring.Commit();
                Status(left, -240f, main, 22f, true);
                _step = LabelAt(string.Empty, left, -276f, main, 12f, T.Dim);

                // The right column: clock, weather, the day.
                float rl = left + main + 60f;
                float rw = left + total - rl;
                _clock = LabelAt(string.Empty, rl, 296f, rw, 64f, T.Text, TextAlignmentOptions.Right, 4f);
                LabelAt(DateTime.Now.ToString("dddd  dd MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant(),
                    rl, 244f, rw, 13f, T.Dim, TextAlignmentOptions.Right);
                Box(rl + rw * 0.5f, 170f, rw, 84f, A(Color32.Lerp(T.Ink, T.Primary, 0.12f), 230));
                LabelAt("NORVINSK", rl + 16f, 194f, rw - 32f, 11f, T.Dim);
                LabelAt($"{Rng.Next(-8, 7)}  C   {Weather[Rng.Next(Weather.Length)]}", rl + 16f, 168f, rw - 32f, 22f, T.Text);
                LabelAt(Advisories[Rng.Next(Advisories.Length)], rl + 16f, 142f, rw - 32f, 10f, T.Accent);

                LabelAt("TODAY AT HEADQUARTERS", rl, 92f, rw, 11f, T.Dim, spacing: 10f);
                var picks = new List<string>(Events);
                for (int i = picks.Count - 1; i > 0; i--)
                {
                    int j = Rng.Next(i + 1);
                    (picks[i], picks[j]) = (picks[j], picks[i]);
                }
                picks = picks.GetRange(0, 6);
                picks.Sort(StringComparer.Ordinal);
                string now = DateTime.Now.ToString("HH:mm");
                for (int i = 0; i < picks.Count; i++)
                {
                    string[] parts = picks[i].Split('|');
                    bool past = string.CompareOrdinal(parts[0], now) < 0;
                    float y = 60f - i * 34f;
                    LabelAt(parts[0], rl, y, 70f, 13f, past ? T.Dim : T.Accent);
                    LabelAt(parts[1], rl + 76f, y, rw - 76f, 13f, past ? T.Dim : T.Text);
                }
                LabelAt($"STAFF ON SITE  {Rng.Next(1800, 2600):N0}  //  VISITORS  0", rl, -170f, rw, 11f, T.Dim);
            }

            internal override void Animate(float t)
            {
                DateTime now = DateTime.Now;
                if (now.Minute != _minute)
                {
                    _minute = now.Minute;
                    _clock.text = now.ToString("HH:mm");
                }
                string nick = StartupTracker.Nickname;
                if (nick != _shownNick)
                {
                    _shownNick = nick;
                    string part = now.Hour < 12 ? "GOOD MORNING" : now.Hour < 18 ? "GOOD AFTERNOON" : "GOOD EVENING";
                    _greeting.text = string.IsNullOrEmpty(nick) ? part + "." : $"{part}, {nick.ToUpperInvariant()}.";
                }

                const float Cycle = 3.4f;
                int word = (int)(t / Cycle) % Words.Length;
                float p = t % Cycle;
                float alpha = Mathf.Clamp01(p / 0.4f) * Mathf.Clamp01((Cycle - p) / 0.4f);
                if (word != _shownWord)
                {
                    _shownWord = word;
                    _word.text = Words[word];
                    _line.text = Lines[word];
                }
                _word.canvasRenderer.SetAlpha(alpha);
                _line.canvasRenderer.SetAlpha(alpha);

                int n = _nodes.Length;
                int current = Mathf.Clamp(StartupTracker.Current - First, 0, n - 1);
                for (int k = 0; k < n; k++)
                {
                    int state = State(First + k);
                    if (state != _state[k])
                    {
                        _state[k] = state;
                        _nodes[k].canvasRenderer.SetAlpha(state == Pending ? 0.18f : state == Skipped ? 0.4f : 1f);
                    }
                }
                _track.rectTransform.sizeDelta = new Vector2(Mathf.Round(_trackWidth * Progress * U), _track.rectTransform.sizeDelta.y);
                float pulse = (t * 1.2f) % 1f;
                _ring.rectTransform.anchoredPosition = P(_trackLeft + _trackWidth * (Granted ? n - 1 : current) / Mathf.Max(1, n - 1), -190f);
                _ring.rectTransform.localScale = Vector3.one * (1f + pulse * 0.5f);
                _ring.canvasRenderer.SetAlpha(Granted ? 0f : 1f - pulse);
                int key = current * 1000 + Mathf.FloorToInt(Progress * 100f);
                if (key != _shownKey)
                {
                    _shownKey = key;
                    _step.text = Granted ? "SIGN-IN COMPLETE  //  100%" : $"STEP {current + 1:00} OF {n:00}  //  {Mathf.FloorToInt(Progress * 100f)}%";
                }
                MirrorStatus();
            }
        }

        /// <summary>
        /// Holiday: TerraGroup's season's greetings. Snow falls; a tree's ornaments light one per
        /// load step and its star when access is granted; the card's message is from the board.
        /// </summary>
        private sealed class GreetingCard : Scene
        {
            private Shape[] _ornaments;
            private Shape _star;
            private int[] _state;
            private RawImage[] _flakes;
            private Vector2[] _flakeAt;
            private float[] _flakeSpeed;
            private float[] _flakePhase;
            private TextMeshProUGUI _count;
            private int _shownDone = -1;

            protected override void Build()
            {
                float total = Mathf.Min(2f * HW - 200f, 1600f);
                float left = -total * 0.5f;

                // Snow, behind everything.
                Vector2 size = O._laidOutSize;
                _flakes = new RawImage[60];
                _flakeAt = new Vector2[60];
                _flakeSpeed = new float[60];
                _flakePhase = new float[60];
                for (int i = 0; i < _flakes.Length; i++)
                {
                    float s = Range(3f, 9f) * U;
                    _flakes[i] = MakeRaw("Flake", R, Art.Radial, A(T.Text, (byte)Rng.Next(90, 200)));
                    _flakes[i].rectTransform.sizeDelta = new Vector2(s, s);
                    _flakeAt[i] = new Vector2(Range(-0.5f, 0.5f) * size.x, Range(-0.5f, 0.5f) * size.y);
                    _flakeSpeed[i] = Range(25f, 70f) * U;
                    _flakePhase[i] = Range(0f, 6.28f);
                }

                // The card's words, on the left.
                float cw = total * 0.34f;
                BrandMark(left + 20f, 320f, "SEASON'S GREETINGS");
                LabelAt("SEASON'S", left, 220f, cw, 46f, T.Text, spacing: 16f);
                LabelAt("GREETINGS", left, 166f, cw, 46f, T.Accent, spacing: 16f);
                LabelAt("FROM ALL OF US AT TERRAGROUP", left, 120f, cw, 13f, T.Dim, spacing: 8f);
                Paragraph("THANK YOU FOR YOUR SERVICE THIS YEAR. YOUR LOYALTY HAS BEEN NOTED. THE OFFICE CLOSES ON 25 DECEMBER; STAFF ON SITE WILL REMAIN ON SITE.",
                    left, 90f, cw, 150f, 15f, T.Text);
                LabelAt("- THE BOARD", left, -80f, cw, 14f, T.Accent);

                // The tree: three tiers, a trunk, a garland, an ornament per step.
                Color32 needles = Color32.Lerp(T.Primary, new Color32(30, 120, 80, 255), 0.6f);
                Shape tree = Draw();
                tree.Tri(P(0f, 290f), P(-130f, 140f), P(130f, 140f), needles);
                tree.Tri(P(0f, 200f), P(-210f, -20f), P(210f, -20f), needles);
                tree.Tri(P(0f, 80f), P(-290f, -210f), P(290f, -210f), needles);
                tree.Commit();
                Box(0f, -250f, 60f, 80f, Color32.Lerp(new Color32(110, 70, 40, 255), T.Ink, 0.3f));
                // A garland drapes across each tier from slope to slope, and the ornaments hang
                // from it: an ornament per step, the top tier's share smallest.
                int n = Steps - First;
                _ornaments = new Shape[n];
                _state = new int[n];
                Color32 gold = Color32.Lerp(T.Warn, new Color32(255, 220, 90, 255), 0.6f);
                Color32[] colours = { T.Warn, T.Accent, T.Text, gold };
                float[][] tiers = { new[] { 290f, 140f, 130f }, new[] { 200f, -20f, 210f }, new[] { 80f, -210f, 290f } };
                int top = Mathf.Max(1, Mathf.RoundToInt(n * 0.2f));
                int middle = Mathf.Max(1, Mathf.RoundToInt(n * 0.33f));
                int[] perTier = { top, middle, n - top - middle };
                Shape garland = Draw();
                int k = 0;
                for (int tier = 0; tier < 3; tier++)
                {
                    float apex = tiers[tier][0];
                    float baseY = tiers[tier][1];
                    float half = tiers[tier][2];
                    float rise = 24f + tier * 6f;
                    float y0 = baseY + (apex - baseY) * 0.3f;
                    Func<float, float> drape = f => y0 - rise * Mathf.Sin(f * Mathf.PI) + (f - 0.5f) * 26f;
                    Func<float, float> edge = y => half * (apex - y) / (apex - baseY) * 0.94f;
                    Vector2 last = Vector2.zero;
                    for (int s = 0; s <= 30; s++)
                    {
                        float f = s / 30f;
                        float y = drape(f);
                        float w = edge(y);
                        Vector2 next = P(Mathf.Lerp(-w, w, f), y);
                        if (s > 0)
                        {
                            garland.Line(last, next, 2f * U, A(T.Accent, 150));
                        }
                        last = next;
                    }
                    int count = perTier[tier];
                    for (int j = 0; j < count && k < n; j++, k++)
                    {
                        float f = (j + 1f) / (count + 1f);
                        float y = drape(f);
                        float x = Mathf.Lerp(-edge(y), edge(y), f);
                        Shape ornament = _ornaments[k] = Draw(x, y - 12f);
                        ornament.Line(P(0f, 12f), P(0f, 6f), U, A(T.Text, 160));
                        ornament.Disc(Vector2.zero, 9f * U, colours[k % colours.Length]);
                        ornament.Disc(P(-3f, 3f), 2.5f * U, A(new Color32(255, 255, 255, 255), 200));
                        ornament.Commit();
                        _state[k] = -1;
                    }
                }
                garland.Commit();

                // A five-pointed star on top: a point on each side of a centre pentagon.
                _star = Draw(0f, 306f);
                var inner = new Vector2[5];
                for (int p = 0; p < 5; p++)
                {
                    inner[p] = Polar(10f * U, p * 72f + 36f);
                }
                for (int p = 0; p < 5; p++)
                {
                    _star.Tri(Polar(26f * U, p * 72f), inner[(p + 4) % 5], inner[p], gold);
                }
                _star.Polygon(inner, gold);
                _star.Commit();

                // The right column: the holiday schedule, and the load's tally.
                float rl = 360f;
                float rw = left + total - rl;
                if (rw >= 220f)
                {
                    LabelAt("HOLIDAY SCHEDULE", rl, 230f, rw, 11f, T.Dim, spacing: 10f);
                    string[] schedule =
                    {
                        "24 DEC|OFFICE PARTY (CANCELLED)", "25 DEC|OFFICE CLOSED", "26 DEC|SKELETON CREW", "31 DEC|FIREWORKS (SECURITY ONLY)", "01 JAN|ROAD CLOSURES CONTINUE"
                    };
                    for (int i = 0; i < schedule.Length; i++)
                    {
                        string[] parts = schedule[i].Split('|');
                        LabelAt(parts[0], rl, 196f - i * 32f, 80f, 13f, T.Accent);
                        LabelAt(parts[1], rl + 84f, 196f - i * 32f, rw - 84f, 13f, T.Text);
                    }
                }
                _count = LabelAt(string.Empty, -250f, -312f, 500f, 12f, T.Dim, TextAlignmentOptions.Center);
                Status(-400f, -348f, 800f, 18f, true, TextAlignmentOptions.Center);
            }

            internal override void Animate(float t)
            {
                Vector2 size = O._laidOutSize;
                for (int i = 0; i < _flakes.Length; i++)
                {
                    _flakeAt[i].y -= _flakeSpeed[i] * Dt;
                    _flakeAt[i].x += Mathf.Sin(t * 0.7f + _flakePhase[i]) * 12f * U * Dt;
                    if (_flakeAt[i].y < -size.y * 0.5f - 10f)
                    {
                        _flakeAt[i] = new Vector2(Range(-0.5f, 0.5f) * size.x, size.y * 0.5f + 10f);
                    }
                    _flakes[i].rectTransform.anchoredPosition = _flakeAt[i];
                }

                int done = 0;
                for (int k = 0; k < _ornaments.Length; k++)
                {
                    int state = State(First + k);
                    done += state == Done ? 1 : 0;
                    if (state == Running || state == Slow)
                    {
                        _ornaments[k].canvasRenderer.SetAlpha(0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * 4f)));
                    }
                    else if (state != _state[k])
                    {
                        _ornaments[k].canvasRenderer.SetAlpha(state == Done ? 1f : 0.15f);
                    }
                    _state[k] = state;
                }
                _star.canvasRenderer.SetAlpha(Granted ? 0.8f + 0.2f * Mathf.Sin(t * 6f) : 0.18f);
                if (done != _shownDone)
                {
                    _shownDone = done;
                    _count.text = $"ORNAMENTS HUNG  {done} / {_ornaments.Length}";
                }
                MirrorStatus();
            }
        }
    }
}
