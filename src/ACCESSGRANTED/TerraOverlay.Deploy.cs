using EFT;
using EFT.UI.Matchmaker;
using EFT.UI.Screens;
using TMPro;
using UnityEngine;

namespace ACCESSGRANTED
{
    /// <summary>
    /// The terminal over a raid's load, when switched on: it comes up over the deploy screen,
    /// follows the statuses the game sends that screen (see StartupTracker), counts down with
    /// the final countdown, and leaves for the raid the moment the raid starts. It is the same
    /// division and look the player signed in to (see <see cref="Variant.KeepLook"/>), notices
    /// are kept to one, and it never holds the raid back.
    ///
    /// The deploy screen stays underneath, live: the terminal never takes input, so the game
    /// behaves exactly as without it. Its Cancel button is hidden behind the terminal, so Esc
    /// cancels instead, only while the game itself allows it. If the load stops without a raid
    /// (cancelled, or an error), the terminal fades back to whatever screen the game opens.
    /// </summary>
    internal partial class TerraOverlay
    {
        /// <summary>Whether the game would let the player cancel the raid right now.</summary>
        internal static bool CanCancel { get; set; }

        private MatchmakerTimeHasCome _deployScreen;
        private string _deployTopRight;
        private bool _raidStarted;
        private bool _cancelled;

        /// <summary>Whether the deploy screen (or the countdown) has been the current screen yet: until then, the one before it still is.</summary>
        private bool _sawDeployScreen;
        private float _countdownEnds = -1f;

        /// <summary>After the countdown, how long to wait for the raid to start before getting out of the way regardless.</summary>
        private const float RaidStartGrace = 20f;

        /// <summary>A deploy ending never holds the raid back longer than this.</summary>
        private const float DeployOutroTime = 1f;

        private static readonly string[] DeployLines =
        {
            "DEPLOYMENT AUTHORIZED", "FIELD ACCESS GRANTED", "EXIT PERMIT ISSUED", "LIABILITY WAIVER ON FILE", "ACCESS GRANTED"
        };

        // The screens the game goes back to when a raid load stops: the load is over, no raid.
        private static readonly EEftScreenType[] BackScreens =
        {
            EEftScreenType.MainMenu, EEftScreenType.SelectRaidSide, EEftScreenType.SelectLocation,
            EEftScreenType.OfflineRaid, EEftScreenType.Insurance, EEftScreenType.MatchMakerAccept,
            EEftScreenType.MapPoints, EEftScreenType.KeyAccess, EEftScreenType.Hideout
        };

        private bool Deploying => StartupTracker.Kind == RunKind.Deploy;

        /// <summary>Called as the deploy screen shows, before it sends its first status.</summary>
        internal static void BeginDeploy(MatchmakerTimeHasCome screen, RaidSettings raidSettings)
        {
            if (_instance == null)
            {
                FindBootFont(null, out TMP_FontAsset font, out Material material);
                Create(font, material);
            }
            TerraOverlay o = _instance;
            if (o._previewing || o._phase != Phase.Hidden)
            {
                o.Hide(release: false);
            }
            CanCancel = false;
            o._deployScreen = screen;
            o._raidStarted = false;
            o._cancelled = false;
            o._sawDeployScreen = false;
            o._countdownEnds = -1f;
            o._deployTopRight = DeployHeader(raidSettings);
            o.StartLoading(null);
            AccessGrantedPlugin.Log.LogInfo("Raid load: the terminal is showing it.");
        }

        /// <summary>The final countdown showed, <paramref name="seconds"/> before the raid starts.</summary>
        internal static void Countdown(float seconds)
        {
            if (_instance != null && _instance._phase == Phase.Loading && _instance.Deploying && !_instance._previewing)
            {
                _instance._countdownEnds = Time.unscaledTime + Mathf.Max(0f, seconds);
                CanCancel = false;
            }
        }

        /// <summary>The raid has started: leave at once.</summary>
        internal static void RaidStarted()
        {
            if (_instance != null && _instance._phase == Phase.Loading && _instance.Deploying && !_instance._previewing)
            {
                _instance._raidStarted = true;
            }
        }

        /// <summary>Esc cancels the raid while the game allows it. Called every frame by the plugin.</summary>
        internal static void DeployInput()
        {
            TerraOverlay o = _instance;
            if (o == null || o._phase != Phase.Loading || !o.Deploying || o._previewing || !CanCancel
                || !Input.GetKeyDown(KeyCode.Escape) || o._deployScreen == null)
            {
                return;
            }
            AccessGrantedPlugin.Log.LogInfo("Raid load: Esc pressed, asking the game to cancel.");
            try
            {
                o._deployScreen.AbortMatching();
            }
            catch (System.Exception e)
            {
                AccessGrantedPlugin.Log.LogWarning("Raid load: the game would not cancel: " + e.Message);
            }
        }

        /// <summary>What the corner says on a raid load: the destination (a site code unless names are on) and who is going.</summary>
        private static string DeployHeader(RaidSettings raidSettings)
        {
            string where = null;
            bool scav = false;
            try
            {
                var location = raidSettings?.SelectedLocation;
                if (location != null)
                {
                    where = AccessGrantedPlugin.ShowMapName.Value && !string.IsNullOrEmpty(location.LocalizedName)
                        ? location.LocalizedName.ToUpperInvariant()
                        : SiteCode(location.Id);
                }
                scav = raidSettings != null && raidSettings.IsScav;
            }
            catch (System.Exception e)
            {
                AccessGrantedPlugin.Log.LogDebug("Raid load: no destination: " + e.Message);
            }
            return (where ?? "--") + "|" + (scav ? "SCAV" : "PMC");
        }

        /// <summary>The same code for the same map every time, from its id.</summary>
        private static string SiteCode(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return "--";
            }
            int hash = 17;
            foreach (char c in id.ToLowerInvariant())
            {
                hash = unchecked(hash * 31 + c);
            }
            int n = (hash & 0x7fffffff) % 90 + 10;
            char letter = (char)('A' + (hash >> 8 & 0x7fffffff) % 26);
            return $"{n}{letter}";
        }

        /// <summary>What a raid load changes in a roll: no fake denial or greeting holding up the raid, its own lines, one notice at most.</summary>
        private void DeployRoll()
        {
            if (!Deploying)
            {
                return;
            }
            _variant.FakeDenial = false;
            _variant.Greet = false;
            _variant.GrantText = DeployLines[_random.Next(DeployLines.Length)];
            string[] header = (_previewing ? SiteCode("preview") + "|PMC" : _deployTopRight ?? "--|PMC").Split('|');
            _variant.TopRight = $"FIELD ACCESS  //  {(header[0].Length <= 4 ? "SITE " : string.Empty)}{header[0]}  //  {header[1]}";
            if (_variant.Notices != null && _variant.Notices.Length > 1)
            {
                _variant.Notices = new[] { _variant.Notices[0] };
            }
        }

        /// <summary>The subtitle under the wordmark: the division and what stage the game is at.</summary>
        private string Subtitle(bool boot) =>
            _variant.Theme.Division + (Deploying ? "  //  DEPLOYING"
                : StartupTracker.Kind == RunKind.Return && StartupTracker.Active ? "  //  DEBRIEFING COMPLETE"
                : boot ? "  //  STARTING UP" : "  //  SIGNING IN");

        /// <summary>The running step's time: elapsed, or on the countdown the time left.</summary>
        private static string StepTimeText(int current, int tenths)
        {
            if (current == (int)StepId.DeployCountdown)
            {
                float left = Mathf.Max(0f, StartupTracker.ExpectedSeconds(current) - StartupTracker.StepElapsed);
                return $"T-<mspace=0.62em>{left:0.0}</mspace>s";
            }
            return $"<mspace=0.62em>{tenths / 10f:0.0}</mspace>s";
        }

        /// <summary>The countdown runs to a set time; it is never "slow".</summary>
        private static bool CanRunSlow(int current) => current != (int)StepId.DeployCountdown;

        /// <summary>The corner clock, with the cancel key while the game allows it.</summary>
        private string ClockText(int tenths)
        {
            string clock = $"ELAPSED <mspace=0.62em>{tenths / 10f:0.0}</mspace>s";
            return Deploying && !_previewing && CanCancel
                ? clock + $"  //  <color={_variant.Theme.WarnHex}>ESC  CANCEL DEPLOYMENT</color>"
                : clock;
        }

        /// <summary>Decides when a raid load is over: the raid started, or it stopped without one.</summary>
        private void WatchDeploy(float realTime)
        {
            if (_raidStarted)
            {
                Grant(realTime);
                return;
            }
            EftScreenManager manager = EftScreenManager._instance;
            EEftScreenType current = manager != null && manager.CurrentScreenController != null
                ? manager.CurrentScreenController.ScreenType
                : EEftScreenType.None;
            if (current == EEftScreenType.TimeHasCome || current == EEftScreenType.FinalCountdown)
            {
                _sawDeployScreen = true;
            }
            else if (_sawDeployScreen && System.Array.IndexOf(BackScreens, current) >= 0)
            {
                // Back in the menus without a raid: cancelled, or the game gave up.
                _cancelled = true;
                StartupTracker.AbortRun("no raid, the game went back to its menus");
                Grant(realTime);
                return;
            }
            if (_countdownEnds >= 0f && realTime > _countdownEnds + RaidStartGrace)
            {
                AccessGrantedPlugin.Log.LogWarning("Raid load: the countdown ended but the raid never said it started; stepping aside.");
                Grant(realTime);
            }
        }

        /// <summary>
        /// The raid load's version of the closing line: no sequence and no waiting for smooth
        /// frames, since the raid is live; straight into the ending.
        /// </summary>
        private void GrantDeploy()
        {
            if (_cancelled)
            {
                _grantText = "DEPLOYMENT CANCELLED";
            }
            _chatter.text = _cancelled ? ":: REQUEST WITHDRAWN" : ":: TERRAGROUP ACCEPTS NO LIABILITY FOR LOSSES IN THE FIELD";
            FinishReveal();
            if (_cancelled)
            {
                ShowLabel("> " + _grantText, Dim);
                SetSegments(Faint);
            }
            _fading = true;
            _fadeProgress = 0f;
            _deployScreen = null;
            CanCancel = false;
        }
    }
}
