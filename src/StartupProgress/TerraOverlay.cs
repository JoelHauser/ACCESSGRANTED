using System;
using System.Text;
using EFT.UI;
using EFT.UI.Screens;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StartupProgress
{
    /// <summary>
    /// A full-screen TerraGroup terminal over the game's whole launch: it switches on like a CRT
    /// in place of the game's splash, then shows an emblem that assembles
    /// itself inside turning rings and a progress ring, the wordmark, a segment per load step, the
    /// running step typed out, and a log of the finished ones.
    ///
    /// It lives on its own top-level canvas rather than inside the game's screen, because the game
    /// switches that screen off two steps before the menu is ready. The overlay carries on through
    /// those, shows ACCESS GRANTED when the main menu replaces the loading screen, and fades out
    /// over the live menu (it never takes input, so the menu is usable during the fade).
    ///
    /// The canvas has no scaler, so one unit is one screen pixel: shapes get a one-pixel soft edge
    /// and the grid and scanlines land exactly on pixels. Everything that moves is a transform or
    /// an alpha; text is rewritten only when what it shows changes. All motion follows the clock,
    /// so a hitch in the game's own loading skips frames instead of slowing the animation.
    /// </summary>
    internal class TerraOverlay : MonoBehaviour
    {
        private enum Phase
        {
            Hidden,
            Loading,
            Granted
        }

        // TerraGroup blues.
        private static readonly Color32 Ink = new Color32(2, 6, 16, 255);
        private static readonly Color32 Glow = new Color32(18, 64, 160, 255);
        private static readonly Color32 Blue = new Color32(47, 107, 255, 255);
        private static readonly Color32 MidBlue = new Color32(38, 86, 205, 255);
        private static readonly Color32 Cyan = new Color32(111, 211, 255, 255);
        private static readonly Color32 Text = new Color32(207, 233, 255, 255);
        private static readonly Color32 Dim = new Color32(95, 134, 184, 255);
        private static readonly Color32 Faint = new Color32(95, 134, 184, 60);

        private const int LogLines = 4;
        private const float TypeSpeed = 70f;
        private const float GrantedHold = 0.55f;
        private const float FadeTime = 0.7f;

        /// <summary>A frame quicker than this counts as smooth.</summary>
        private const float SmoothFrame = 0.05f;
        private const int SmoothFramesNeeded = 10;

        /// <summary>Fade regardless after this long on ACCESS GRANTED, however rough the frames.</summary>
        private const float MaxHold = 2.5f;

        /// <summary>If the game hides its screen and the menu never comes, give up after this long.</summary>
        private const float GiveUpAfter = 15f;

        private static TerraOverlay _instance;

        private Phase _phase = Phase.Hidden;
        private ProfileLoadingScreen _screen;
        private TextMeshProUGUI _status;
        private bool _sawLoadingScreen;
        private float _screenGoneFor;
        private float _begunAt;
        private float _introClock;

        /// <summary>
        /// What every animation runs on: it advances by the frame's time, but never by more than
        /// a frame at 30 fps. The launch is full of long frames while the game loads on the main
        /// thread; on the real clock each one made the rings, grid and band jump ahead, which is
        /// what looked laggy. On this clock a stall slows the motion for a moment instead.
        /// </summary>
        private float _animClock;
        private float _frameDt;
        private const float MaxFrameStep = 1f / 30f;
        private int _laidOutFirstStep = -1;
        private float _grantedAt;
        private bool _fading;
        private float _fadeProgress;
        private float _held;
        private int _smoothFrames;

        private RectTransform _rect;
        private CanvasGroup _group;
        private Vector2 _laidOutSize;
        private float _u = 1f;

        private RawImage _glow;
        private RawImage _grid;
        private RawImage _scanlines;
        private RawImage _band;
        private Shape _corners;
        private RectTransform _shutterTop;
        private RectTransform _shutterBottom;
        private Image _powerLine;

        private RectTransform _emblem;
        private readonly Shape[] _pieces = new Shape[4];
        private readonly Vector2[] _pieceDirections =
        {
            new Vector2(-1f, 1f).normalized, new Vector2(1f, 1f).normalized,
            new Vector2(-1f, -1f).normalized, new Vector2(1f, -1f).normalized
        };
        private RawImage _emblemGlow;
        private Shape _ringDashes;
        private Shape _ringArcs;
        private Shape _ringTrack;
        private Shape _ringProgress;
        private float _ringProgressDrawn = -1f;

        private TextMeshProUGUI _wordmark;
        private TextMeshProUGUI _wordmarkGhost;
        private TextMeshProUGUI _subtitle;
        private TextMeshProUGUI _stepCounter;
        private TextMeshProUGUI _percent;
        private Image[] _segments;
        private Image[] _segmentFills;
        private TextMeshProUGUI _currentLabel;
        private TextMeshProUGUI _currentTime;
        private TextMeshProUGUI _currentDetail;
        private TextMeshProUGUI _logLabels;
        private TextMeshProUGUI _logTimes;
        private TextMeshProUGUI _topLeft;
        private TextMeshProUGUI _topRight;
        private TextMeshProUGUI _bottomLeft;
        private TextMeshProUGUI _bottomRight;

        private bool _bootSubtitle;
        private int _shownStep = -2;
        private string _shownDetail;
        private float _stepShownAt;
        private int _shownStepTenths = -1;
        private int _shownElapsedTenths = -1;
        private int _shownPercent = -1;
        private float _shownFraction;
        private readonly StringBuilder _builder = new StringBuilder();
        private readonly StringBuilder _timesBuilder = new StringBuilder();

        /// <summary>Called when the game's splash wakes: the start of a launch.</summary>
        internal static void BeginBoot(SplashScreenPanel splash)
        {
            if (_instance == null)
            {
                FindBootFont(splash, out TMP_FontAsset font, out Material material);
                Create(font, material);
            }
            if (_instance._phase == Phase.Hidden)
            {
                _instance.StartLoading(null);
            }
        }

        /// <summary>Called when the game shows its loading screen in loading mode.</summary>
        internal static void Begin(ProfileLoadingScreen screen)
        {
            TextMeshProUGUI status = screen._statusField;
            if (status == null)
            {
                return;
            }
            if (_instance == null)
            {
                Create(status.font, status.fontSharedMaterial);
            }
            else
            {
                // The boot font was a guess; the status text's is the one the game uses here.
                _instance.ApplyFont(status.font, status.fontSharedMaterial);
            }

            if (_instance._phase == Phase.Loading)
            {
                // A launch already under way from the splash: just take charge of the screen.
                _instance.TakeScreen(screen);
            }
            else
            {
                _instance.StartLoading(screen);
            }
        }

        private static void Create(TMP_FontAsset font, Material material)
        {
            var go = new GameObject("TerraGroupLoading", typeof(RectTransform), typeof(CanvasGroup));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<TerraOverlay>();
            _instance.Build(font, material);
        }

        /// <summary>
        /// The game's own font, if anything loaded so far uses it: the splash's text first, then
        /// any loaded font named Bender. Null leaves TextMeshPro's default.
        /// </summary>
        private static void FindBootFont(SplashScreenPanel splash, out TMP_FontAsset font, out Material material)
        {
            font = null;
            material = null;
            TMP_Text text = splash != null ? splash.GetComponentInChildren<TMP_Text>(true) : null;
            if (text != null && text.font != null)
            {
                font = text.font;
                material = text.fontSharedMaterial;
                return;
            }
            foreach (TMP_FontAsset candidate in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                if (candidate.name.IndexOf("bender", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    font = candidate;
                    material = candidate.material;
                    return;
                }
            }
        }

        private void ApplyFont(TMP_FontAsset font, Material material)
        {
            if (font == null)
            {
                return;
            }
            foreach (TextMeshProUGUI text in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.font != font)
                {
                    text.font = font;
                    text.fontSharedMaterial = material;
                }
            }
        }

        /// <summary>The game is showing an error: get out of its way at once.</summary>
        internal static void HideNow()
        {
            if (_instance != null && _instance._phase != Phase.Hidden)
            {
                StartupTracker.EndRun();
                _instance.Hide();
            }
        }

        private void Build(TMP_FontAsset font, Material material)
        {
            _rect = (RectTransform)transform;

            // Top-level, over everything, one unit per pixel.
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            _group = GetComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 0f;

            Stretch(MakeImage("Background", transform, Ink).rectTransform);
            _glow = MakeRaw("Glow", transform, Art.Radial, Glow);
            _grid = MakeRaw("Grid", transform, Art.Grid, new Color32(111, 211, 255, 16));
            Stretch(_grid.rectTransform);
            _corners = Shape.Create("Corners", transform);

            _emblem = MakeRect("Emblem", transform);
            _emblemGlow = MakeRaw("EmblemGlow", _emblem, Art.Radial, Blue);
            _ringTrack = Shape.Create("RingTrack", _emblem);
            _ringProgress = Shape.Create("RingProgress", _emblem);
            _ringDashes = Shape.Create("RingDashes", _emblem);
            _ringArcs = Shape.Create("RingArcs", _emblem);
            for (int i = 0; i < _pieces.Length; i++)
            {
                _pieces[i] = Shape.Create("Piece" + i, _emblem);
            }

            _wordmarkGhost = MakeText("WordmarkGhost", font, material, TextAlignmentOptions.Center, Cyan);
            _wordmark = MakeText("Wordmark", font, material, TextAlignmentOptions.Center, Text);
            _wordmark.text = _wordmarkGhost.text = "TERRAGROUP";
            _subtitle = MakeText("Subtitle", font, material, TextAlignmentOptions.Center, Cyan);
            _subtitle.text = "LABS  //  PROFILE SYNCHRONIZATION";

            _stepCounter = MakeText("StepCounter", font, material, TextAlignmentOptions.Left, Dim);
            _percent = MakeText("Percent", font, material, TextAlignmentOptions.Right, Dim);
            _segments = new Image[StartupTracker.StepCount];
            _segmentFills = new Image[StartupTracker.StepCount];
            for (int i = 0; i < _segments.Length; i++)
            {
                _segments[i] = MakeImage("Segment" + i, transform, Faint);
                _segmentFills[i] = MakeImage("Fill", _segments[i].transform, Cyan);
                RectTransform fill = _segmentFills[i].rectTransform;
                fill.anchorMin = Vector2.zero;
                fill.anchorMax = new Vector2(0f, 1f);
                fill.offsetMin = fill.offsetMax = Vector2.zero;
            }

            _currentLabel = MakeText("CurrentLabel", font, material, TextAlignmentOptions.Left, Text);
            _currentTime = MakeText("CurrentTime", font, material, TextAlignmentOptions.Right, Text);
            _currentDetail = MakeText("CurrentDetail", font, material, TextAlignmentOptions.Left, Dim);
            _logLabels = MakeText("LogLabels", font, material, TextAlignmentOptions.TopLeft, Dim);
            _logTimes = MakeText("LogTimes", font, material, TextAlignmentOptions.TopRight, Dim);

            _topLeft = MakeText("TopLeft", font, material, TextAlignmentOptions.TopLeft, Dim);
            _topLeft.text = "TERRAGROUP LABS  //  SECURE ACCESS TERMINAL";
            _topRight = MakeText("TopRight", font, material, TextAlignmentOptions.TopRight, Dim);
            _topRight.text = "NODE TG-04  //  " + Application.version;
            _bottomLeft = MakeText("BottomLeft", font, material, TextAlignmentOptions.BottomLeft, Dim);
            _bottomRight = MakeText("BottomRight", font, material, TextAlignmentOptions.BottomRight, Dim);

            _scanlines = MakeRaw("Scanlines", transform, Art.Scanlines, new Color32(0, 0, 0, 70));
            Stretch(_scanlines.rectTransform);
            _band = MakeRaw("Band", transform, Art.Band, new Color32(111, 211, 255, 10));

            // The power-on: two black shutters that part from a bright line across the middle.
            _shutterTop = MakeImage("ShutterTop", transform, Color.black).rectTransform;
            _shutterBottom = MakeImage("ShutterBottom", transform, Color.black).rectTransform;
            _powerLine = MakeImage("PowerLine", transform, new Color32(200, 240, 255, 255));

            gameObject.SetActive(false);
        }

        private void StartLoading(ProfileLoadingScreen screen)
        {
            if (_phase == Phase.Loading && _screen == screen)
            {
                return;
            }

            _screen = null;
            _status = null;
            _sawLoadingScreen = false;
            _screenGoneFor = 0f;
            _begunAt = Time.unscaledTime;
            _introClock = 0f;
            _phase = Phase.Loading;

            _shownStep = -2;
            _shownDetail = null;
            _shownStepTenths = -1;
            _shownElapsedTenths = -1;
            _shownPercent = -1;
            _shownFraction = 0f;
            _ringProgressDrawn = -1f;
            _emblem.localScale = Vector3.one;
            _laidOutSize = Vector2.zero;
            _group.alpha = 0f;
            _currentLabel.text = string.Empty;
            _logLabels.text = _logTimes.text = string.Empty;

            TakeScreen(screen);
            gameObject.SetActive(true);
        }

        private void TakeScreen(ProfileLoadingScreen screen)
        {
            if (screen == null || screen == _screen)
            {
                return;
            }
            _screen = screen;
            _status = screen._statusField;
            _screenGoneFor = 0f;
            // The game's own "Loading profile data..." would show through the overlay's fade-in.
            if (_status != null)
            {
                _status.enabled = false;
            }
        }

        private void Hide()
        {
            _phase = Phase.Hidden;
            if (_status != null)
            {
                _status.enabled = true;
            }
            _screen = null;
            _status = null;
            gameObject.SetActive(false);
        }

        private void Grant(float realTime)
        {
            // Close the run first: the times are how long it took to reach the menu.
            float elapsed = StartupTracker.Elapsed;
            StartupTracker.EndRun();
            _phase = Phase.Granted;
            _grantedAt = realTime;
            _fading = false;
            _fadeProgress = 0f;
            _held = 0f;
            _smoothFrames = 0;
            if (_status != null)
            {
                _status.enabled = true;
            }

            int steps = StartupTracker.StepCount - StartupTracker.FirstStep;
            _stepCounter.text = $"STEP <mspace=0.62em>{steps:00}</mspace> / {steps:00}";
            _percent.text = "<mspace=0.62em>100</mspace>%";
            _currentLabel.text = "> ACCESS GRANTED";
            _currentLabel.color = Cyan;
            _currentLabel.maxVisibleCharacters = 0;
            _stepShownAt = _animClock;
            _currentTime.text = string.Empty;
            _currentDetail.text = string.Empty;
            RebuildLog(StartupTracker.StepCount);
            for (int i = 0; i < _segments.Length; i++)
            {
                _segments[i].color = Cyan;
                _segmentFills[i].enabled = false;
            }
            _bottomLeft.text = $"ELAPSED <mspace=0.62em>{elapsed:0.0}</mspace>s";
        }

        private void LateUpdate()
        {
            if (_phase == Phase.Hidden)
            {
                return;
            }

            float real = Time.unscaledTime;
            _frameDt = Mathf.Min(Time.unscaledDeltaTime, MaxFrameStep);
            _animClock += _frameDt;
            float t = _animClock;
            if (_phase == Phase.Loading)
            {
                StartupTracker.NoteFrame(Time.unscaledDeltaTime);
                WatchGame(real);
                if (_phase == Phase.Hidden)
                {
                    return;
                }
            }

            Vector2 size = _rect.rect.size;
            if ((size - _laidOutSize).sqrMagnitude > 0.25f || _laidOutFirstStep != StartupTracker.FirstStep)
            {
                _laidOutSize = size;
                _laidOutFirstStep = StartupTracker.FirstStep;
                Layout(size);
            }

            // The intro runs on drawn frames, not the clock: the first seconds of a launch are full
            // of long frames, and a clock-timed intro would be over before anyone saw it.
            _introClock += _frameDt;

            AnimateBackdrop(t);
            AnimatePowerOn();
            AnimateEmblem(t, _introClock - PowerOnTime);

            if (_phase == Phase.Loading)
            {
                _group.alpha = Mathf.Clamp01(_introClock * 8f);
                UpdateProgress(StartupTracker.Fraction());
                UpdateSteps(t);
                UpdateClock(StartupTracker.Elapsed);
            }
            else
            {
                UpdateProgress(1f);
                TypeOut(t);
                float fade = AdvanceFade(real);
                float ease = fade * fade * (3f - 2f * fade);
                _group.alpha = 1f - ease;
                _emblem.localScale = Vector3.one * (1f + 0.08f * ease);
                if (fade >= 1f)
                {
                    Hide();
                }
            }
        }

        /// <summary>
        /// How far the fade-out has gone, 0 to 1. Not by the clock: the menu's first frames are a
        /// burst of work from the game and other mods that can freeze it for a second, and a
        /// clock-timed fade would finish inside that freeze and look like a hard cut. So ACCESS
        /// GRANTED holds until frames come smoothly again, and the fade then moves only as far
        /// per frame as a frame at 30 fps would take it.
        /// </summary>
        private float AdvanceFade(float realTime)
        {
            float dt = Time.unscaledDeltaTime;
            if (!_fading)
            {
                _smoothFrames = dt < SmoothFrame ? _smoothFrames + 1 : 0;
                _held += Mathf.Min(dt, SmoothFrame);
                bool settled = _smoothFrames >= SmoothFramesNeeded && _held >= GrantedHold;
                if (settled || realTime - _grantedAt > MaxHold)
                {
                    _fading = true;
                    _fadeProgress = 0f;
                }
                return 0f;
            }

            _fadeProgress += _frameDt / FadeTime;
            return Mathf.Clamp01(_fadeProgress);
        }

        /// <summary>Decides when loading is over, or when to stand aside for the game.</summary>
        private void WatchGame(float realTime)
        {
            if (!StartupProgressPlugin.Enabled.Value)
            {
                StartupTracker.EndRun();
                Hide();
                return;
            }

            bool haveScreen = _screen != null;
            bool screenUp = haveScreen && _screen.gameObject.activeInHierarchy;
            if (screenUp && _screen._loadingPanel != null && !_screen._loadingPanel.gameObject.activeSelf)
            {
                // The screen switched to its error panel.
                StartupTracker.EndRun();
                Hide();
                return;
            }

            // _instance, not Instance: the property creates a manager if there is none.
            EftScreenManager manager = EftScreenManager._instance;
            bool loadingIsCurrent = manager != null && manager.CheckCurrentScreen(EEftScreenType.ProfileLoading);
            if (loadingIsCurrent)
            {
                _sawLoadingScreen = true;
            }
            else if (manager != null && manager.CurrentScreenController != null
                     && (_sawLoadingScreen || manager.CheckCurrentScreen(EEftScreenType.MainMenu)))
            {
                // Another screen, normally the main menu, has taken over. A moment with no
                // screen at all, between two, does not count.
                Grant(realTime);
                return;
            }

            // Before the profile screen first shows (the boot steps) there is nothing to lose.
            _screenGoneFor = !haveScreen || screenUp ? 0f : _screenGoneFor + Time.unscaledDeltaTime;
            if (_screenGoneFor > GiveUpAfter)
            {
                Grant(realTime);
            }
        }

        /// <summary>Everything that depends only on the screen size. Units are 1080p pixels scaled to the screen.</summary>
        private void Layout(Vector2 size)
        {
            float u = _u = size.y / 1080f;

            // One texel per pixel, so the lines stay one pixel wide.
            _scanlines.uvRect = new Rect(0f, 0f, 1f, size.y / 3f);
            Place(_glow.rectTransform, 0f, 60f * u, size.x * 1.3f, size.y * 1.5f);
            Place(_band.rectTransform, 0f, 0f, size.x, 260f * u);

            float inset = Mathf.Round(48f * u);
            float arm = Mathf.Round(46f * u);
            float width = Mathf.Max(2f, Mathf.Round(2f * u));
            float hx = Mathf.Round(size.x * 0.5f) - inset;
            float hy = Mathf.Round(size.y * 0.5f) - inset;
            _corners.Clear();
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    var corner = new Vector2(sx * hx, sy * hy);
                    var half = new Vector2(sx * width * 0.5f, sy * width * 0.5f);
                    _corners.Line(corner + new Vector2(half.x, 0f), corner - new Vector2(sx * arm, 0f), width, Dim);
                    _corners.Line(corner - new Vector2(0f, sy * width * 0.5f), corner - new Vector2(0f, sy * arm), width, Dim);
                }
            }
            _corners.Commit();

            // Emblem: the TerraGroup diamond in four pieces, one of them only outlined.
            _emblem.anchoredPosition = new Vector2(0f, Mathf.Round(175f * u));
            Place(_emblemGlow.rectTransform, 0f, 0f, 460f * u, 460f * u);
            float r = 58f * u;
            float gap = 5f * u;
            BuildPiece(_pieces[0], new Vector2(-r, 0f), new Vector2(0f, r), gap, Blue, false);
            BuildPiece(_pieces[1], new Vector2(0f, r), new Vector2(r, 0f), gap, Cyan, false);
            BuildPiece(_pieces[2], new Vector2(-r, 0f), new Vector2(0f, -r), gap, MidBlue, false);
            BuildPiece(_pieces[3], new Vector2(r, 0f), new Vector2(0f, -r), gap, Cyan, true);

            _ringDashes.Clear();
            for (int i = 0; i < 48; i++)
            {
                _ringDashes.Arc(96f * u, (i % 4 == 0 ? 7f : 3f) * u, i * 7.5f, 2.2f, new Color32(111, 211, 255, 130));
            }
            _ringDashes.Commit();

            _ringArcs.Clear();
            for (int i = 0; i < 3; i++)
            {
                _ringArcs.Arc(110f * u, 2.5f * u, i * 120f + 10f, 70f, new Color32(47, 107, 255, 220));
            }
            _ringArcs.Commit();

            _ringTrack.Clear();
            _ringTrack.Arc(124f * u, 2f * u, 0f, 360f, new Color32(111, 211, 255, 32));
            _ringTrack.Commit();
            _ringProgressDrawn = -1f;

            SetText(_wordmark, 0f, -2f * u, 900f * u, 60f * u, 50f * u, 26f);
            SetText(_wordmarkGhost, 0f, -2f * u, 900f * u, 60f * u, 50f * u, 26f);
            SetText(_subtitle, 0f, -42f * u, 900f * u, 24f * u, 15f * u, 10f);

            float barWidth = Mathf.Round(600f * u);
            float barTop = -98f * u;
            SetText(_stepCounter, -barWidth * 0.25f, barTop + 20f * u, barWidth * 0.5f, 18f * u, 13f * u, 8f);
            SetText(_percent, barWidth * 0.25f, barTop + 20f * u, barWidth * 0.5f, 18f * u, 13f * u, 8f);
            // One segment per step in this run; a run that started at Connect has no boot segments.
            int first = StartupTracker.FirstStep;
            int shown = _segments.Length - first;
            float segmentGap = Mathf.Round((shown > 12 ? 4f : 6f) * u);
            float segmentWidth = Mathf.Floor((barWidth - segmentGap * (shown - 1)) / shown);
            float segmentHeight = Mathf.Max(3f, Mathf.Round(4f * u));
            float rowWidth = segmentWidth * shown + segmentGap * (shown - 1);
            for (int i = 0; i < _segments.Length; i++)
            {
                _segments[i].gameObject.SetActive(i >= first);
                float x = -rowWidth * 0.5f + (i - first) * (segmentWidth + segmentGap) + segmentWidth * 0.5f;
                Place(_segments[i].rectTransform, x, barTop, segmentWidth, segmentHeight);
            }

            SetText(_currentLabel, -barWidth * 0.15f, -136f * u, barWidth * 0.7f, 30f * u, 21f * u, 4f);
            SetText(_currentTime, barWidth * 0.35f, -136f * u, barWidth * 0.3f, 30f * u, 21f * u, 4f);
            SetText(_currentDetail, 0f, -162f * u, barWidth, 20f * u, 13f * u, 6f);
            SetText(_logLabels, -barWidth * 0.15f, -205f * u, barWidth * 0.7f, LogLines * 22f * u, 13f * u, 6f);
            SetText(_logTimes, barWidth * 0.35f, -205f * u, barWidth * 0.3f, LogLines * 22f * u, 13f * u, 6f);
            _logLabels.lineSpacing = _logTimes.lineSpacing = 18f;

            float textInset = inset + 16f * u;
            float halfW = size.x * 0.5f - textInset;
            float halfH = size.y * 0.5f - textInset;
            float cornerWidth = size.x * 0.45f;
            SetText(_topLeft, -halfW + cornerWidth * 0.5f, halfH - 9f * u, cornerWidth, 18f * u, 12f * u, 8f);
            SetText(_topRight, halfW - cornerWidth * 0.5f, halfH - 9f * u, cornerWidth, 18f * u, 12f * u, 8f);
            SetText(_bottomLeft, -halfW + cornerWidth * 0.5f, -halfH + 9f * u, cornerWidth, 18f * u, 12f * u, 8f);
            SetText(_bottomRight, halfW - cornerWidth * 0.5f, -halfH + 9f * u, cornerWidth, 18f * u, 12f * u, 8f);
            _bottomRight.text = StartupTracker.LastTotal > 0f
                ? $"LAST SYNC <mspace=0.62em>{StartupTracker.LastTotal:0.0}</mspace>s"
                : "LAST SYNC  --";
        }

        private void BuildPiece(Shape piece, Vector2 a, Vector2 b, float gap, Color32 color, bool outline)
        {
            // Each quarter of the diamond is the triangle centre-a-b, pulled off the centre lines
            // by the gap.
            Vector2 shift = ((a + b) / 3f).normalized * gap;
            Vector2 c0 = shift;
            Vector2 c1 = a + shift;
            Vector2 c2 = b + shift;
            piece.Clear();
            if (outline)
            {
                // Three strokes, each inset by half its width so the outline fills the same
                // triangle as its solid neighbours.
                float w = Mathf.Max(2f, 3f * _u);
                Vector2 centre = (c0 + c1 + c2) / 3f;
                Vector2 i0 = Inset(c0, centre, w);
                Vector2 i1 = Inset(c1, centre, w);
                Vector2 i2 = Inset(c2, centre, w);
                piece.Line(i0, i1, w, color);
                piece.Line(i1, i2, w, color);
                piece.Line(i2, i0, w, color);
            }
            else
            {
                piece.Tri(c0, c1, c2, color);
            }
            piece.Commit();
        }

        private static Vector2 Inset(Vector2 corner, Vector2 centre, float width)
        {
            Vector2 towards = centre - corner;
            return corner + towards.normalized * Mathf.Min(width * 1.2f, towards.magnitude * 0.5f);
        }

        private const float PowerOnTime = 0.55f;

        /// <summary>
        /// A CRT switching on: a line flicks across the middle of a black screen, then the picture
        /// opens out from it. Over in about half a second.
        /// </summary>
        private void AnimatePowerOn()
        {
            float clock = _introClock;
            bool done = clock >= PowerOnTime + 0.1f;
            if (_shutterTop.gameObject.activeSelf == done)
            {
                _shutterTop.gameObject.SetActive(!done);
                _shutterBottom.gameObject.SetActive(!done);
                _powerLine.gameObject.SetActive(!done);
            }
            if (done)
            {
                return;
            }

            float w = _laidOutSize.x;
            float h = _laidOutSize.y;
            float line = Mathf.Clamp01(clock / 0.18f);
            float lineEase = 1f - Mathf.Pow(1f - line, 3f);
            float open = Mathf.Clamp01((clock - 0.2f) / 0.35f);
            float openEase = open * open * (3f - 2f * open);

            float gap = Mathf.Round(h * 0.5f * openEase);
            float shutter = h * 0.5f - gap + 2f;
            Place(_shutterTop, 0f, gap + shutter * 0.5f - 1f, w + 4f, shutter);
            Place(_shutterBottom, 0f, -(gap + shutter * 0.5f - 1f), w + 4f, shutter);

            float thickness = Mathf.Max(2f, Mathf.Round(3f * _u));
            Place(_powerLine.rectTransform, 0f, 0f, w * lineEase, thickness);
            _powerLine.canvasRenderer.SetAlpha(1f - openEase);
        }

        private void AnimateBackdrop(float t)
        {
            // The grid drifts diagonally, slowly enough to read as depth rather than motion.
            _grid.uvRect = new Rect(t * 0.012f, t * 0.02f, _laidOutSize.x / 64f, _laidOutSize.y / 64f);

            // A faint bright band rolls down the screen every four seconds.
            float sweep = (t / 4f) % 1f;
            _band.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(_laidOutSize.y * 0.6f, -_laidOutSize.y * 0.6f, sweep));
            _glow.canvasRenderer.SetAlpha(0.75f + 0.1f * Mathf.Sin(t * 0.8f));
        }

        private void AnimateEmblem(float t, float elapsed)
        {
            // The four pieces fly in from outside and lock together over the first second.
            for (int i = 0; i < _pieces.Length; i++)
            {
                float own = Mathf.Clamp01((elapsed - i * 0.08f) / 0.8f);
                float ease = 1f - Mathf.Pow(1f - own, 3f);
                _pieces[i].rectTransform.anchoredPosition = _pieceDirections[i] * (70f * _u * (1f - ease));
                _pieces[i].canvasRenderer.SetAlpha(ease);
            }

            float rings = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / 0.9f), 3f);
            _ringDashes.rectTransform.localEulerAngles = new Vector3(0f, 0f, -t * 14f);
            _ringArcs.rectTransform.localEulerAngles = new Vector3(0f, 0f, t * 32f);
            _ringDashes.canvasRenderer.SetAlpha(rings);
            _ringArcs.canvasRenderer.SetAlpha(rings);
            _emblemGlow.canvasRenderer.SetAlpha((0.22f + 0.08f * Mathf.Sin(t * 2.2f)) * rings);

            // Every few seconds the wordmark glitches: a cyan ghost jumps sideways for a moment.
            float cycle = t % 5.3f;
            bool glitch = cycle < 0.12f || (cycle > 0.2f && cycle < 0.26f);
            _wordmarkGhost.canvasRenderer.SetAlpha(glitch ? 0.7f : 0f);
            _wordmarkGhost.rectTransform.anchoredPosition = new Vector2(Mathf.Round(glitch ? (cycle < 0.12f ? 6f : -4f) * _u : 0f), Mathf.Round(-2f * _u));
        }

        private void UpdateProgress(float fraction)
        {
            _shownFraction = Mathf.Lerp(_shownFraction, fraction, 1f - Mathf.Exp(-_frameDt * 6f));
            if (fraction >= 1f && 1f - _shownFraction < 0.002f)
            {
                _shownFraction = 1f;
            }

            // The ring is redrawn only when it has visibly moved.
            if (Mathf.Abs(_shownFraction - _ringProgressDrawn) > 0.002f || (_shownFraction >= 1f && _ringProgressDrawn < 1f))
            {
                _ringProgressDrawn = _shownFraction;
                _ringProgress.Clear();
                _ringProgress.Arc(124f * _u, 3f * _u, 0f, 360f * _shownFraction, Cyan);
                _ringProgress.Commit();
            }

            if (_phase != Phase.Loading)
            {
                return;
            }
            int percent = Mathf.FloorToInt(_shownFraction * 100f);
            if (percent != _shownPercent)
            {
                _shownPercent = percent;
                _percent.text = $"<mspace=0.62em>{percent}</mspace>%";
            }
        }

        private void UpdateSteps(float t)
        {
            int current = StartupTracker.Current;
            if (current < 0)
            {
                return;
            }
            if (current != _shownStep)
            {
                _shownStep = current;
                _stepShownAt = t;
                _shownStepTenths = -1;
                int first = StartupTracker.FirstStep;
                _stepCounter.text = $"STEP <mspace=0.62em>{current - first + 1:00}</mspace> / {StartupTracker.StepCount - first:00}";
                bool boot = current < (int)StepId.Connect;
                if (boot != _bootSubtitle)
                {
                    _bootSubtitle = boot;
                    _subtitle.text = boot ? "LABS  //  SYSTEM BOOT" : "LABS  //  PROFILE SYNCHRONIZATION";
                }
                _currentLabel.text = "> " + StartupTracker.Labels[current].ToUpperInvariant();
                _currentLabel.color = Text;
                _currentLabel.maxVisibleCharacters = 0;
                RebuildLog(current);
                for (int i = 0; i < _segments.Length; i++)
                {
                    _segments[i].color = i < current ? (Color)Cyan : (Color)Faint;
                    _segmentFills[i].rectTransform.anchorMax = new Vector2(0f, 1f);
                    _segmentFills[i].enabled = i == current;
                }
            }

            TypeOut(t);

            // The running segment fills toward where this step usually ends, and pulses.
            RectTransform fill = _segmentFills[current].rectTransform;
            float target = StartupTracker.CurrentStepFraction();
            fill.anchorMax = new Vector2(Mathf.Lerp(fill.anchorMax.x, target, 1f - Mathf.Exp(-_frameDt * 6f)), 1f);
            _segmentFills[current].canvasRenderer.SetAlpha(0.65f + 0.35f * Mathf.Sin(t * 6f));

            string detail = StartupTracker.Detail(current);
            if (detail != _shownDetail)
            {
                _shownDetail = detail;
                _currentDetail.text = string.IsNullOrEmpty(detail) ? string.Empty : "  " + detail.ToUpperInvariant();
            }

            int tenths = Mathf.FloorToInt(StartupTracker.StepElapsed * 10f);
            if (tenths != _shownStepTenths)
            {
                _shownStepTenths = tenths;
                _currentTime.text = $"<mspace=0.62em>{tenths / 10f:0.0}</mspace>s";
            }
        }

        /// <summary>The current line types itself out.</summary>
        private void TypeOut(float t)
        {
            if (_currentLabel.maxVisibleCharacters < _currentLabel.text.Length)
            {
                _currentLabel.maxVisibleCharacters = Mathf.FloorToInt((t - _stepShownAt) * TypeSpeed) + 2;
            }
        }

        /// <summary>The steps before <paramref name="current"/>, newest first, older lines fading out.</summary>
        private void RebuildLog(int current)
        {
            _builder.Length = 0;
            _timesBuilder.Length = 0;
            int lines = 0;
            for (int i = current - 1; i >= StartupTracker.FirstStep && lines < LogLines; i--)
            {
                // Steps the game skipped (SPT turns some off) are left out rather than listed empty.
                if (StartupTracker.WasSkipped(i))
                {
                    continue;
                }
                int alpha = 255 - lines * 50;
                lines++;
                string open = $"<alpha=#{alpha:X2}>";
                _builder.Append(open).Append("<color=#6FD3FF>OK</color>   ").Append(StartupTracker.Labels[i].ToUpperInvariant());
                string detail = StartupTracker.Detail(i);
                if (!string.IsNullOrEmpty(detail))
                {
                    _builder.Append("   <alpha=#").Append((alpha / 2).ToString("X2")).Append('>').Append(detail.ToUpperInvariant());
                }
                _builder.Append('\n');
                _timesBuilder.Append(open)
                    .Append($"<mspace=0.62em>{StartupTracker.TookSeconds(i):0.0}</mspace>s")
                    .Append('\n');
            }
            _logLabels.text = _builder.ToString();
            _logTimes.text = _timesBuilder.ToString();
        }

        private void UpdateClock(float elapsed)
        {
            int tenths = Mathf.FloorToInt(elapsed * 10f);
            if (tenths != _shownElapsedTenths)
            {
                _shownElapsedTenths = tenths;
                _bottomLeft.text = $"ELAPSED <mspace=0.62em>{tenths / 10f:0.0}</mspace>s";
            }
        }

        private TextMeshProUGUI MakeText(string name, TMP_FontAsset font, Material material, TextAlignmentOptions alignment, Color32 color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSharedMaterial = material;
            text.alignment = alignment;
            text.color = color;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.richText = true;
            return text;
        }

        private static Image MakeImage(string name, Transform parent, Color32 color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RawImage MakeRaw(string name, Transform parent, Texture texture, Color32 color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>Centre at (x, y) from the middle of the screen, snapped to whole pixels.</summary>
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            width = Mathf.Round(width);
            height = Mathf.Round(height);
            rect.sizeDelta = new Vector2(width, height);
            // An odd size centres on a half pixel; offset it so its edges still land on pixels.
            rect.anchoredPosition = new Vector2(
                Mathf.Round(x) + (width % 2f) * 0.5f,
                Mathf.Round(y) + (height % 2f) * 0.5f);
        }

        private static void SetText(TextMeshProUGUI text, float x, float y, float width, float height, float fontSize, float spacing)
        {
            Place(text.rectTransform, x, y, width, height);
            text.fontSize = Mathf.Round(fontSize);
            text.characterSpacing = spacing;
        }
    }
}
