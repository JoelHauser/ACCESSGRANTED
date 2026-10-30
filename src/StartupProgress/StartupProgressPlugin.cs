using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace StartupProgress
{
    /// <summary>
    /// Replaces the bare "Loading profile data..." wait with a TerraGroup terminal showing what
    /// the game is actually doing: each load step as it starts, how long the finished ones took,
    /// and progress timed from the last launch. See <see cref="StartupTracker"/> for the steps
    /// and <see cref="TerraOverlay"/> for the drawing.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class StartupProgressPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.mybutthasarash.startupprogress";
        public const string PluginName = "Startup Progress";

        /// <summary>Must match the csproj's Version. Two places, and they have to agree.</summary>
        public const string PluginVersion = "0.2.0";

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> Enabled;

        private void Awake()
        {
            Log = Logger;

            Enabled = Config.Bind("General", "Enabled", true,
                "Show the TerraGroup loading terminal instead of the plain loading text.");

            StartupTracker.LoadTimings();
            Patches.Apply(new Harmony(PluginGuid));

            // If the splash woke before this plugin loaded, its Awake patch never ran.
            var splash = FindObjectOfType<EFT.UI.SplashScreenPanel>();
            if (splash != null)
            {
                Patches.SplashAwake(splash);
            }
        }
    }
}
