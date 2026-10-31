using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace ACCESSGRANTED
{
    /// <summary>
    /// Replaces the bare "Loading profile data..." wait with a TerraGroup terminal showing what
    /// the game is actually doing: each load step as it starts, how long the finished ones took,
    /// and progress timed from the last launch. See <see cref="StartupTracker"/> for the steps
    /// and <see cref="TerraOverlay"/> for the drawing.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class AccessGrantedPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.mybutthasarash.accessgranted";
        public const string PluginName = "ACCESS GRANTED";

        /// <summary>Must match the csproj's Version. Two places, and they have to agree.</summary>
        public const string PluginVersion = "0.3.0";

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<ThemeChoice> Theme;
        internal static ConfigEntry<GlitchLevel> Glitches;
        internal static ConfigEntry<LayoutKind> Layout;
        internal static ConfigEntry<BackdropKind> Backdrop;
        internal static ConfigEntry<FrameKind> Frame;
        internal static ConfigEntry<IntroKind> Intro;
        internal static ConfigEntry<BarKind> Bar;
        internal static ConfigEntry<OutroKind> Ending;
        internal static ConfigEntry<KeyboardShortcut> PreviewKey;
        internal static ConfigEntry<bool> CctvScreenshots;

        private void Update()
        {
            if (PreviewKey.Value.IsDown())
            {
                TerraOverlay.StartPreview();
            }
            TerraOverlay.PreviewInput();
        }

        /// <summary>The plugin's GUID while it was called Startup Progress.</summary>
        private const string OldGuid = "com.mybutthasarash.startupprogress";

        private void Awake()
        {
            Log = Logger;
            CarryOverOldFiles();

            Enabled = Config.Bind("General", "Enabled", true,
                "Show the TerraGroup loading terminal instead of the plain loading text.");
            Theme = Config.Bind("Style", "Theme", ThemeChoice.Random,
                "The terminal's colours. Random picks one each launch, some rarer than others.");
            Glitches = Config.Bind("Style", "Glitches", GlitchLevel.Subtle,
                "How often the terminal glitches: tears, colour splits, garbled text. Off for none.");
            Layout = Config.Bind("Style", "Layout", LayoutKind.Random,
                "How the logo and the readout are arranged. Random picks one each launch.");
            Backdrop = Config.Bind("Style", "Background", BackdropKind.Random,
                "The pattern behind everything. Random lets each department pick one of its own.");
            Frame = Config.Bind("Style", "Logo Frame", FrameKind.Random,
                "What surrounds the logo. Random lets each department pick one of its own.");
            Intro = Config.Bind("Style", "Intro", IntroKind.Random,
                "How the screen comes on. Random lets each department pick one of its own.");
            Bar = Config.Bind("Style", "Progress Bar", BarKind.Random,
                "The progress bar. Random lets each department pick one of its own.");
            Ending = Config.Bind("Style", "Ending", OutroKind.Random,
                "How the screen leaves for the main menu. Random lets each department pick one of its own.");
            CctvScreenshots = Config.Bind("Style", "CCTV Shows My Screenshots", true,
                "Security's camera wall plays your own game screenshots as grey security footage. They are only read "
                + "from your screenshot folders and shrunk into BepInEx/cache/ACCESSGRANTED; nothing leaves your PC.");
            PreviewKey = Config.Bind("Preview", "Preview Key", new KeyboardShortcut(UnityEngine.KeyCode.F9, UnityEngine.KeyCode.LeftControl),
                "In the main menu, plays the terminal with a pretend load. While it shows: Left/Right change department, "
                + "Up/Down change layout, Space re-rolls the rest, R replays, Esc closes.");

            StartupTracker.LoadTimings();
            Patches.Apply(new Harmony(PluginGuid));

            // If the splash woke before this plugin loaded, its Awake patch never ran.
            var splash = FindObjectOfType<EFT.UI.SplashScreenPanel>();
            if (splash != null)
            {
                Patches.SplashAwake(splash);
            }
        }

        /// <summary>
        /// The settings and launch timings are named after the GUID, which changed with the rename
        /// from Startup Progress. Copies the old files once, where there are no new ones yet, so a
        /// player keeps their settings and the progress bar keeps its baseline. The old files are
        /// left where they are.
        /// </summary>
        private void CarryOverOldFiles()
        {
            try
            {
                string oldConfig = System.IO.Path.Combine(BepInEx.Paths.ConfigPath, OldGuid + ".cfg");
                if (!System.IO.File.Exists(Config.ConfigFilePath) && System.IO.File.Exists(oldConfig))
                {
                    System.IO.File.Copy(oldConfig, Config.ConfigFilePath);
                    Config.Reload();
                    Log.LogInfo("Carried over the settings from Startup Progress.");
                }
                string oldTimings = System.IO.Path.Combine(BepInEx.Paths.ConfigPath, OldGuid + ".timings.txt");
                string timings = System.IO.Path.Combine(BepInEx.Paths.ConfigPath, PluginGuid + ".timings.txt");
                if (!System.IO.File.Exists(timings) && System.IO.File.Exists(oldTimings))
                {
                    System.IO.File.Copy(oldTimings, timings);
                    Log.LogInfo("Carried over the launch timings from Startup Progress.");
                }
            }
            catch (System.Exception e)
            {
                Log.LogWarning("Could not carry over Startup Progress's settings or timings: " + e.Message);
            }
        }
    }
}
