using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace ACCESSGRANTED
{
    /// <summary>What the raid-loading terminal does when Deploy Screen Overhaul is installed too.</summary>
    public enum DeployScreenMode
    {
        StandAside,
        TakeTurns,
        AlwaysTerminal
    }

    /// <summary>
    /// Replaces the bare "Loading profile data..." wait with a TerraGroup terminal showing what
    /// the game is actually doing: each load step as it starts, how long the finished ones took,
    /// and progress timed from the last launch. See <see cref="StartupTracker"/> for the steps
    /// and <see cref="TerraOverlay"/> for the drawing. Optionally the same terminal shows raid
    /// loads too.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class AccessGrantedPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.mybutthasarash.accessgranted";
        public const string PluginName = "ACCESS GRANTED";

        /// <summary>Must match the csproj's Version. Two places, and they have to agree.</summary>
        public const string PluginVersion = "1.0.2";

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
        internal static ConfigEntry<bool> RaidLoading;
        internal static ConfigEntry<DeployScreenMode> WithDeployScreen;
        internal static ConfigEntry<bool> ShowMapName;
        internal static ConfigEntry<bool> AfterRaids;

        private const string DeployScreenGuid = "com.mybutthasarash.deployscreen";
        private const string ClassifiedGuid = "com.mybutthasarash.classified";

        private void Update()
        {
            if (PreviewKey.Value.IsDown())
            {
                TerraOverlay.StartPreview();
            }
            TerraOverlay.PreviewInput();
            TerraOverlay.DeployInput();
        }

        /// <summary>
        /// A raid is loading: whether this terminal shows it. Off unless switched on. With Deploy
        /// Screen Overhaul installed it stands aside by default, or they take turns. With
        /// CLASSIFIED installed, raid loads go to whichever terminal showed this launch, so the
        /// player keeps the one they signed in to; the other takes them only if that one has raid
        /// loads off. Counts the raid either way, so taking turns stays in step.
        /// </summary>
        internal static bool ShowThisDeploy()
        {
            StartupTracker.CountDeploy();
            if (!Enabled.Value || !RaidLoading.Value)
            {
                return false;
            }
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(DeployScreenGuid))
            {
                switch (WithDeployScreen.Value)
                {
                    case DeployScreenMode.StandAside:
                        return false;
                    case DeployScreenMode.TakeTurns:
                        if (StartupTracker.Deploys % 2 == 0)
                        {
                            Log.LogInfo("Deploy Screen Overhaul is installed: this raid load is its turn.");
                            return false;
                        }
                        break;
                }
            }
            if (ClassifiedShowsThisSession("Show While Loading Raids"))
            {
                Log.LogInfo("CLASSIFIED showed this launch: its terminal shows the raid loads too.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// The game is going back to the main menu after a raid: whether this terminal shows it.
        /// With CLASSIFIED installed it goes, like raid loads, to whichever terminal showed this
        /// launch; the other takes it only if that one has it off.
        /// </summary>
        internal static bool ShowThisReturn()
        {
            if (!Enabled.Value || !AfterRaids.Value)
            {
                return false;
            }
            if (ClassifiedShowsThisSession("Show After Raids"))
            {
                Log.LogInfo("CLASSIFIED showed this launch: its terminal shows the way back from the raid too.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Whether CLASSIFIED showed this launch and has <paramref name="setting"/> (under Raid
        /// Loading) on, from its plugin and its settings. Asked as each load starts, since it may
        /// load after this plugin.
        /// </summary>
        private static bool ClassifiedShowsThisSession(string setting)
        {
            try
            {
                if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(ClassifiedGuid, out PluginInfo info) || info.Instance == null)
                {
                    return false;
                }
                ConfigFile config = info.Instance.Config;
                if (!config.TryGetEntry(new ConfigDefinition("General", "Enabled"), out ConfigEntry<bool> enabled) || !enabled.Value
                    || !config.TryGetEntry(new ConfigDefinition("Raid Loading", setting), out ConfigEntry<bool> shows) || !shows.Value)
                {
                    return false;
                }
                object owns = info.Instance.GetType().GetProperty("OwnsThisLaunch")?.GetValue(null, null);
                return owns is bool b && b;
            }
            catch (System.Exception e)
            {
                Log.LogDebug("Could not read CLASSIFIED's raid setting: " + e.Message);
                return false;
            }
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
                "The TerraGroup department the terminal belongs to, with its own colours, drawing and memos. Random picks one each launch, never one shown in the last few launches.");
            Glitches = Config.Bind("Style", "Glitches", GlitchLevel.Subtle,
                "How often the terminal glitches: tears, colour splits, garbled text. Off for none.");
            // The parts each department picks for itself, and the preview: under Advanced in F12.
            Layout = Config.Bind("Style", "Layout", LayoutKind.Random, Advanced(
                "How the logo and the readout are arranged. Random picks a different one each launch; pick one to use it every launch."));
            Backdrop = Config.Bind("Style", "Background", BackdropKind.Random, Advanced(
                "The pattern behind everything. Random lets each department pick one of its own."));
            Frame = Config.Bind("Style", "Logo Frame", FrameKind.Random, Advanced(
                "What surrounds the logo. Random lets each department pick one of its own."));
            Intro = Config.Bind("Style", "Intro", IntroKind.Random, Advanced(
                "How the screen comes on. Random lets each department pick one of its own."));
            Bar = Config.Bind("Style", "Progress Bar", BarKind.Random, Advanced(
                "The progress bar. Random lets each department pick one of its own."));
            Ending = Config.Bind("Style", "Ending", OutroKind.Random, Advanced(
                "How the screen leaves for the main menu. Random lets each department pick one of its own."));
            PreviewKey = Config.Bind("Preview", "Preview Key", new KeyboardShortcut(UnityEngine.KeyCode.F9, UnityEngine.KeyCode.LeftControl), Advanced(
                "In the main menu, plays the terminal with a pretend load. While it shows: Left/Right change department, "
                + "Up/Down change layout, Space re-rolls the rest, T switches between a launch and a raid load, R replays, Esc closes."));
            RaidLoading = Config.Bind("Raid Loading", "Show While Loading Raids", false,
                "Also show the terminal while a raid loads, from the deploy screen to the raid: the same department you signed in to, "
                + "the game's real load steps, the countdown, then into the raid. One notice at most. Esc cancels the raid while the game allows it.");
            WithDeployScreen = Config.Bind("Raid Loading", "With Deploy Screen Overhaul", DeployScreenMode.StandAside,
                "Only matters when Deploy Screen Overhaul is installed too, since both draw the raid loading screen. "
                + "Stand Aside leaves raid loads to it; Take Turns alternates raids; Always Terminal shows this terminal every raid.");
            ShowMapName = Config.Bind("Raid Loading", "Show Map Name", false,
                "Name the map on the raid-loading terminal. Off gives it a site code instead.");
            AfterRaids = Config.Bind("Raid Loading", "Show After Raids", true,
                "Show the terminal while the game goes back to the main menu after a raid, once the after-action report is done: "
                + "the same department you signed in to, from the raid unloading to the menu.");

            StartupTracker.LoadTimings();
            Patches.Apply(new Harmony(PluginGuid));

            // If the splash woke before this plugin loaded, its Awake patch never ran.
            var splash = FindObjectOfType<EFT.UI.SplashScreenPanel>();
            if (splash != null)
            {
                Patches.SplashAwake(splash);
            }
        }

        /// <summary>A setting shown in F12 only with "Advanced settings" ticked.</summary>
        private static ConfigDescription Advanced(string description)
        {
            return new ConfigDescription(description, null, new ConfigurationManagerAttributes { IsAdvanced = true });
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
