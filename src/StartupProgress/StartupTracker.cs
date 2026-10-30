using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace StartupProgress
{
    /// <summary>The load steps, in the order the game runs them.</summary>
    public enum StepId
    {
        VerifyFiles,
        MountBundles,
        BuildInterface,
        LoginScenes,
        Connect,
        ItemDatabase,
        IconCache,
        GameSettings,
        Profiles,
        SelectProfile,
        Handbook,
        WeaponBuilds,
        Voice,
        World,
        MainMenu
    }

    /// <summary>
    /// One pass of loading, until the main menu replaces it. A launch starts at the game's splash
    /// screen and runs every step; a later visit to the profile loading screen starts at
    /// <see cref="StepId.Connect"/>, and the steps before it are not part of that run.
    /// Steps only move forward: several hooked methods (GetProfiles, LoadVoice) run again later,
    /// and a later call must not drag the readout back.
    ///
    /// How long each step took is kept between launches, so the bar can be weighted by real time
    /// instead of step count.
    /// </summary>
    internal static class StartupTracker
    {
        internal static readonly string[] Labels =
        {
            "Verifying game files",
            "Mounting asset bundles",
            "Building interface",
            "Loading login scenes",
            "Connecting to server",
            "Building item database",
            "Reading icon cache",
            "Fetching game settings",
            "Loading profiles",
            "Selecting profile",
            "Loading handbook",
            "Loading weapon builds",
            "Loading voice",
            "Loading weather and maps",
            "Opening main menu"
        };

        /// <summary>
        /// For a first launch, in seconds: one real SPT 4.1 launch (the four boot steps are
        /// guesses). Replaced by this machine's own times after one.
        /// </summary>
        private static readonly float[] DefaultSeconds = { 0.5f, 6f, 1f, 3f, 0.1f, 9f, 0.4f, 0.7f, 0.6f, 0.5f, 2.7f, 0.2f, 1.6f, 1f, 1.5f };

        internal static int StepCount => Labels.Length;

        private static readonly Stopwatch Clock = Stopwatch.StartNew();
        private static readonly float[] Expected = (float[])DefaultSeconds.Clone();
        private static readonly float[] Took = new float[Labels.Length];
        private static readonly bool[] Skipped = new bool[Labels.Length];
        private static readonly string[] Details = new string[Labels.Length];

        // Frame times within the running step, for the log: where the game itself stalls.
        private static int _stepFrames;
        private static int _stepLongFrames;
        private static float _stepWorstFrame;
        private const float LongFrame = 0.05f;

        private static float _runStart;
        private static float _stepStart;
        private static float _shownFraction;

        internal static bool Active { get; private set; }
        internal static int Current { get; private set; } = -1;

        /// <summary>The step this run started at: 0 for a launch, <see cref="StepId.Connect"/> otherwise.</summary>
        internal static int FirstStep { get; private set; }

        /// <summary>Total of the last launch that ran from the splash to the main menu, or 0 if none is known.</summary>
        internal static float LastTotal { get; private set; }

        private static string TimingsPath =>
            // Qualified: Assembly-CSharp has a global Paths class of its own.
            Path.Combine(BepInEx.Paths.ConfigPath, StartupProgressPlugin.PluginGuid + ".timings.txt");

        private static float Now => (float)Clock.Elapsed.TotalSeconds;

        internal static float Elapsed => Active ? Now - _runStart : 0f;
        internal static float StepElapsed => Active && Current >= 0 ? Now - _stepStart : 0f;

        internal static float TookSeconds(int step) => Took[step];
        internal static bool WasSkipped(int step) => Skipped[step];
        internal static string Detail(int step) => Details[step];

        internal static void BeginRun(StepId first)
        {
            if (Active)
            {
                return;
            }

            Array.Clear(Took, 0, Took.Length);
            Array.Clear(Skipped, 0, Skipped.Length);
            Array.Clear(Details, 0, Details.Length);
            FirstStep = (int)first;
            Current = FirstStep - 1;
            _shownFraction = 0f;
            _runStart = Now;
            Active = true;
            Reach(first);
        }

        internal static void Reach(StepId id)
        {
            int step = (int)id;
            if (!Active || step <= Current)
            {
                return;
            }

            float now = Now;
            if (Current >= 0)
            {
                FinishStep(now);
            }
            for (int i = Current + 1; i < step; i++)
            {
                Skipped[i] = true;
            }
            Current = step;
            _stepStart = now;
            _stepFrames = 0;
            _stepLongFrames = 0;
            _stepWorstFrame = 0f;
        }

        internal static void SetDetail(StepId id, string detail)
        {
            if (Active)
            {
                Details[(int)id] = detail;
            }
        }

        internal static void EndRun()
        {
            if (!Active)
            {
                return;
            }

            float now = Now;
            if (Current >= 0)
            {
                FinishStep(now);
            }
            Active = false;

            float total = now - _runStart;
            StartupProgressPlugin.Log.LogInfo($"Loading took {total:0.00}s from '{Labels[FirstStep]}', reached '{Labels[Math.Max(Current, 0)]}'.");

            // Only a run that reached the menu is a fair yardstick for the next one.
            if (Current == StepCount - 1)
            {
                for (int i = FirstStep; i < StepCount; i++)
                {
                    if (!Skipped[i])
                    {
                        Expected[i] = Took[i];
                    }
                }
                if (FirstStep == 0)
                {
                    LastTotal = total;
                }
                SaveTimings();
            }
        }

        /// <summary>
        /// How full the bar is: finished steps count at their expected time, the running step
        /// counts up to just short of its own. Never moves backwards.
        /// </summary>
        internal static float Fraction()
        {
            if (!Active || Current < 0)
            {
                return _shownFraction;
            }

            float total = 0f;
            float done = 0f;
            for (int i = FirstStep; i < StepCount; i++)
            {
                float expected = Math.Max(Expected[i], 0.05f);
                total += expected;
                if (i < Current)
                {
                    done += expected;
                }
                else if (i == Current)
                {
                    done += Math.Min(StepElapsed, expected * 0.9f);
                }
            }

            _shownFraction = Math.Max(_shownFraction, Math.Min(done / total, 0.99f));
            return _shownFraction;
        }

        /// <summary>How far the running step is toward its expected time, held short of the end.</summary>
        internal static float CurrentStepFraction()
        {
            if (!Active || Current < 0)
            {
                return 0f;
            }
            return Math.Min(StepElapsed / Math.Max(Expected[Current], 0.05f), 0.95f);
        }

        /// <summary>Called once per drawn frame while a run is going.</summary>
        internal static void NoteFrame(float seconds)
        {
            if (!Active)
            {
                return;
            }
            _stepFrames++;
            if (seconds > LongFrame)
            {
                _stepLongFrames++;
            }
            _stepWorstFrame = Math.Max(_stepWorstFrame, seconds);
        }

        private static void FinishStep(float now)
        {
            Took[Current] = now - _stepStart;
            StartupProgressPlugin.Log.LogInfo(
                $"{Labels[Current]}: {Took[Current]:0.00}s ({_stepFrames} frames, {_stepLongFrames} over {LongFrame * 1000f:0} ms, worst {_stepWorstFrame * 1000f:0} ms)");
        }

        internal static void LoadTimings()
        {
            try
            {
                if (!File.Exists(TimingsPath))
                {
                    return;
                }
                foreach (string line in File.ReadAllLines(TimingsPath))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length != 2
                        || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds))
                    {
                        continue;
                    }
                    // "Total" (before 0.2) timed only the profile part, so it is not read.
                    if (parts[0] == "LaunchTotal")
                    {
                        LastTotal = seconds;
                    }
                    else if (Enum.TryParse(parts[0], out StepId id))
                    {
                        Expected[(int)id] = seconds;
                    }
                }
            }
            catch (Exception e)
            {
                StartupProgressPlugin.Log.LogWarning($"Could not read {TimingsPath}: {e.Message}");
            }
        }

        private static void SaveTimings()
        {
            try
            {
                var lines = new List<string> { "LaunchTotal=" + LastTotal.ToString("0.000", CultureInfo.InvariantCulture) };
                for (int i = 0; i < StepCount; i++)
                {
                    lines.Add((StepId)i + "=" + Expected[i].ToString("0.000", CultureInfo.InvariantCulture));
                }
                File.WriteAllLines(TimingsPath, lines);
            }
            catch (Exception e)
            {
                StartupProgressPlugin.Log.LogWarning($"Could not write {TimingsPath}: {e.Message}");
            }
        }
    }
}
