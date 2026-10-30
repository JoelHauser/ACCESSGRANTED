using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StartupProgress
{
    internal partial class TerraOverlay
    {
        /// <summary>
        /// A scene built around one calm centrepiece, with the same quiet readout under it: the
        /// current step, a detail line, a thin progress bar with its percentage, and a dot per
        /// step.
        /// </summary>
        private abstract class CalmScene : Scene
        {
            private Shape[] _dots;
            private int[] _dotState;
            private Image _bar;
            private float _barWidth;
            private TextMeshProUGUI _percent;
            private TextMeshProUGUI _detail;
            private int _shownPercent = -1;
            private string _shownDetail = "\0";

            /// <summary>The readout, centred, its step line at <paramref name="y"/>.</summary>
            protected void Readout(float y, float width)
            {
                Status(-width * 0.5f, y, width, 22f, true, TextAlignmentOptions.Center);
                _detail = LabelAt(string.Empty, -width * 0.5f, y - 32f, width, 12f, T.Dim, TextAlignmentOptions.Center);
                float barWidth = width * 0.7f;
                Box(0f, y - 66f, barWidth, 3f, T.Faint);
                _bar = Bar(-barWidth * 0.5f, y - 66f, 3f, T.Accent);
                _barWidth = barWidth * U;
                _percent = LabelAt(string.Empty, barWidth * 0.5f + 14f, y - 66f, 80f, 12f, T.Dim);

                int n = Steps - First;
                _dots = new Shape[n];
                _dotState = new int[n];
                float gap = Mathf.Min(26f, barWidth / Mathf.Max(1, n - 1));
                float start = -gap * (n - 1) * 0.5f;
                for (int k = 0; k < n; k++)
                {
                    Shape dot = _dots[k] = Draw(start + k * gap, y - 96f);
                    dot.Disc(Vector2.zero, 4f * U, T.Accent);
                    dot.Commit();
                    _dotState[k] = -1;
                }
            }

            /// <summary>The detail under the step: what the scene says about the running step.</summary>
            protected abstract string Detail();

            protected void AnimateReadout(float t)
            {
                int percent = Mathf.FloorToInt(Progress * 100f);
                if (percent != _shownPercent)
                {
                    _shownPercent = percent;
                    _percent.text = percent + "%";
                    _bar.rectTransform.sizeDelta = new Vector2(Mathf.Round(_barWidth * Progress), _bar.rectTransform.sizeDelta.y);
                }
                string detail = Detail();
                if (detail != _shownDetail)
                {
                    _shownDetail = detail;
                    _detail.text = detail;
                    _detail.color = O._slow && !Granted ? T.Warn : T.Dim;
                }
                for (int k = 0; k < _dots.Length; k++)
                {
                    int state = State(First + k);
                    bool live = state == Running || state == Slow;
                    if (live)
                    {
                        _dots[k].canvasRenderer.SetAlpha(0.4f + 0.6f * Mathf.Abs(Mathf.Sin(t * 3f)));
                    }
                    else if (state != _dotState[k])
                    {
                        _dots[k].canvasRenderer.SetAlpha(state == Done ? 1f : state == Skipped ? 0.4f : 0.15f);
                    }
                    _dotState[k] = state;
                }
                MirrorStatus();
            }
        }

        /// <summary>
        /// Health: a single heart trace sweeping across the screen, the heart rate beside it,
        /// the readout under it. A slow step quickens the pulse.
        /// </summary>
        private sealed class PatientMonitor : CalmScene
        {
            private const float Period = 260f;
            private Shape _trace;
            private float _offset;
            private TextMeshProUGUI _rate;
            private TextMeshProUGUI _patient;
            private int _shownRate = -1;
            private string _shownNick = "\0";
            private string _ward;

            protected override void Build()
            {
                BrandMark(-HW + 80f, 398f, "TERRAGROUP HEALTH");
                float width = Mathf.Min(2f * HW - 260f, 1500f);
                string[] wards = { "WARD 3", "ICU", "RECOVERY 2", "ISOLATION 1" };
                _ward = wards[Rng.Next(wards.Length)] + "  //  BED " + Rng.Next(1, 30);

                _patient = LabelAt(string.Empty, -width * 0.5f, 268f, width * 0.6f, 13f, T.Dim, spacing: 8f);
                LabelAt("HR  BPM", width * 0.5f - 200f, 290f, 200f, 11f, T.Accent, TextAlignmentOptions.Right);
                _rate = LabelAt(string.Empty, width * 0.5f - 200f, 250f, 200f, 52f, T.Accent, TextAlignmentOptions.Right);

                // The trace, clipped to a band across the middle, over a faint baseline.
                float height = 240f;
                float y = 90f;
                Shape baseline = Draw();
                baseline.Line(P(-width * 0.5f, y - 10f), P(width * 0.5f, y - 10f), U, A(T.Accent, 30));
                baseline.Commit();
                RectTransform mask = MakeRect("Trace", R);
                mask.anchoredPosition = P(0f, y);
                mask.sizeDelta = P(width, height);
                mask.gameObject.AddComponent<RectMask2D>();
                _trace = Draw(0f, 0f, mask);
                Vector2 last = Vector2.zero;
                float start = -width * 0.5f;
                for (float x = start; x <= width * 0.5f + Period * 2f; x += 4f)
                {
                    float p = ((x - start) % Period) / Period;
                    Vector2 point = P(x, Ecg.Wave(p) * 1.7f - 10f);
                    if (x > start)
                    {
                        _trace.Line(last, point, 2.5f * U, T.Accent);
                    }
                    last = point;
                }
                _trace.Commit();

                Readout(-110f, Mathf.Min(width, 900f));
            }

            protected override string Detail() =>
                Granted ? "DISCHARGED  //  FIT FOR DUTY"
                : O._slow ? "ELEVATED HEART RATE  //  " + T.SlowNote
                : "VITALS STABLE  //  " + _ward;

            internal override void Animate(float t)
            {
                float bpm = 66f + 44f * O._alert + 8f * O._beat + 3f * Mathf.Sin(t * 0.3f);
                _offset = (_offset + Dt * Period * U * bpm / 60f) % (Period * U);
                _trace.rectTransform.anchoredPosition = new Vector2(-_offset, 0f);
                int rate = Mathf.RoundToInt(bpm);
                if (rate != _shownRate)
                {
                    _shownRate = rate;
                    _rate.text = rate.ToString();
                    _rate.color = O._alert > 0.3f ? T.Warn : T.Accent;
                }
                string nick = StartupTracker.Nickname;
                if (nick != _shownNick)
                {
                    _shownNick = nick;
                    _patient.text = "PATIENT  " + (string.IsNullOrEmpty(nick) ? "UNIDENTIFIED" : nick.ToUpperInvariant())
                        + "  //  " + (StartupTracker.Side ?? "CONTRACTOR");
                }
                AnimateReadout(t);
            }
        }

        /// <summary>
        /// Research: one flask in the middle of the screen, filling with the compound as the
        /// load goes, bubbles rising through it; the readout under it.
        /// </summary>
        private sealed class Experiment : CalmScene
        {
            // The flask, in scene units: a tapered body and a neck.
            private const float BodyBottom = -60f;
            private const float BodyTop = 170f;
            private const float BodyHalfBottom = 160f;
            private const float BodyHalfTop = 46f;
            private const float NeckTop = 250f;
            private const float NeckHalf = 30f;
            private const int BubbleCount = 14;
            private Shape _liquid;
            private float _drawnLevel = -1f;
            private Color32 _colour;
            private readonly Shape[] _bubbles = new Shape[BubbleCount];
            private readonly Vector2[] _bubbleAt = new Vector2[BubbleCount];
            private readonly float[] _bubbleSpeed = new float[BubbleCount];
            private float _level;

            protected override void Build()
            {
                BrandMark(-HW + 80f, 398f, "RESEARCH DIVISION");
                string[] compounds = { "COMPOUND TG-12", "PROJECT 6-HELIX", "SAMPLE OLI-7", "STIMULANT SJ-9" };
                Color32[] liquids = { T.Accent, new Color32(120, 220, 140, 255), new Color32(120, 170, 255, 255) };
                _colour = liquids[Rng.Next(liquids.Length)];
                Label($"{compounds[Rng.Next(compounds.Length)]}  //  TRIAL {Math.Max(StartupTracker.Launches, 1):0000}",
                    0f, 318f, 900f, 16f, T.Text, TextAlignmentOptions.Center, null, 10f);

                RawImage glow = MakeRaw("Glow", R, Art.Radial, A(_colour, 40));
                Place(glow.rectTransform, 0f, 40f * U, 620f * U, 620f * U);
                _liquid = Draw();
                for (int i = 0; i < BubbleCount; i++)
                {
                    Shape bubble = _bubbles[i] = Draw();
                    bubble.Arc(Range(3f, 6f) * U, 1.2f * U, 0f, 360f, A(new Color32(255, 255, 255, 255), 170));
                    bubble.Commit();
                    _bubbleAt[i] = new Vector2(0f, BodyBottom - 50f);
                    _bubbleSpeed[i] = Range(30f, 70f);
                }

                // The glass over the liquid.
                Shape glass = Draw();
                Color32 rim = A(T.Text, 200);
                float w = 3f * U;
                glass.Line(P(-BodyHalfBottom, BodyBottom), P(BodyHalfBottom, BodyBottom), w, rim);
                glass.Line(P(-BodyHalfBottom, BodyBottom), P(-BodyHalfTop, BodyTop), w, rim);
                glass.Line(P(BodyHalfBottom, BodyBottom), P(BodyHalfTop, BodyTop), w, rim);
                glass.Line(P(-BodyHalfTop, BodyTop), P(-NeckHalf, BodyTop + 14f), w, rim);
                glass.Line(P(BodyHalfTop, BodyTop), P(NeckHalf, BodyTop + 14f), w, rim);
                glass.Line(P(-NeckHalf, BodyTop + 14f), P(-NeckHalf, NeckTop), w, rim);
                glass.Line(P(NeckHalf, BodyTop + 14f), P(NeckHalf, NeckTop), w, rim);
                glass.Line(P(-NeckHalf - 10f, NeckTop), P(NeckHalf + 10f, NeckTop), 4f * U, rim);
                glass.Line(P(-BodyHalfBottom + 30f, BodyBottom + 20f), P(-BodyHalfTop - 6f, BodyTop - 30f), 2f * U, A(new Color32(255, 255, 255, 255), 60));
                for (int m = 1; m <= 4; m++)
                {
                    float y = Mathf.Lerp(BodyBottom, BodyTop, m / 5f);
                    float half = Half(y);
                    glass.Line(P(half - 26f, y), P(half - 8f, y), 1.5f * U, A(T.Text, 120));
                }
                glass.Commit();

                Readout(-130f, 900f);
            }

            /// <summary>Half the flask's inside width at height <paramref name="y"/>.</summary>
            private static float Half(float y) =>
                Mathf.Lerp(BodyHalfBottom, BodyHalfTop, Mathf.InverseLerp(BodyBottom, BodyTop, y)) - 4f;

            protected override string Detail() =>
                Granted ? "SYNTHESIS COMPLETE  //  SAMPLE STABLE"
                : O._slow ? "ANOMALY  //  REACTION SLOWER THAN EXPECTED"
                : "SYNTHESIS IN PROGRESS  //  SAMPLE STABLE";

            internal override void Animate(float t)
            {
                // The level follows the load, a touch below the shoulder when full.
                float target = Mathf.Lerp(BodyBottom + 6f, BodyTop - 18f, Progress);
                _level = _level == 0f ? target : Mathf.Lerp(_level, target, 1f - Mathf.Exp(-Dt * 3f));
                float surface = _level + Mathf.Sin(t * 2.2f) * 1.5f;
                if (Mathf.Abs(surface - _drawnLevel) > 0.4f)
                {
                    _drawnLevel = surface;
                    float bottom = BodyBottom + 4f;
                    _liquid.Clear();
                    if (surface > bottom + 1f)
                    {
                        _liquid.Polygon(new[] { P(-Half(bottom), bottom), P(Half(bottom), bottom), P(Half(surface), surface), P(-Half(surface), surface) }, A(_colour, 170));
                        _liquid.Line(P(-Half(surface), surface), P(Half(surface), surface), 2f * U, A(new Color32(255, 255, 255, 255), 90));
                    }
                    _liquid.Commit();
                }

                // Bubbles rise from the bottom and vanish at the surface.
                float speed = 1f + 0.6f * O._shownFraction + O._alert;
                for (int i = 0; i < BubbleCount; i++)
                {
                    Vector2 at = _bubbleAt[i];
                    at.y += _bubbleSpeed[i] * speed * Dt;
                    if (at.y > surface - 6f || at.y < BodyBottom)
                    {
                        float y = BodyBottom + Range(6f, 20f);
                        at = new Vector2(Range(-0.8f, 0.8f) * Half(y), y);
                    }
                    at.x += Mathf.Sin(t * 3f + i) * 6f * Dt;
                    // The flask narrows as they rise; keep them inside the glass.
                    float inside = Half(at.y) * 0.85f;
                    at.x = Mathf.Clamp(at.x, -inside, inside);
                    _bubbleAt[i] = at;
                    _bubbles[i].rectTransform.anchoredPosition = P(at.x, at.y);
                    _bubbles[i].canvasRenderer.SetAlpha(surface > BodyBottom + 20f ? 1f : 0f);
                }
                AnimateReadout(t);
            }
        }
    }
}
