# ACCESS GRANTED: notes for Claude

BepInEx client plugin for SPT 4.1.x: a TerraGroup sign-in terminal over the game's launch, and optionally over raid loads and the way back from a raid. Sister mod of CLASSIFIED (`..\CLASSIFIED`), built from the same code. Player-facing docs are `README.md` and `MOD-PAGE*.md`; this file is for working on it.

## Layout

- `src/ACCESSGRANTED/AccessGrantedPlugin.cs`: config (F12), `PluginVersion`, and who shows a raid load (`ShowThisDeploy`: Deploy Screen Overhaul and CLASSIFIED interplay).
- `Patches.cs`: Harmony hooks on the launch steps, the deploy screen (`MatchmakerTimeHasCome`), the countdown, raid start and `ComebackToMainMenu`.
- `TerraOverlay*.cs`: the terminal (one partial class). `TerraOverlay.Deploy.cs` is the raid load, `.Preview.cs` is the Ctrl+F9 preview (T switches to a raid load).
- `DeployScene.cs`: what is switched off under the raid-loading terminal (see below). **Identical copy in CLASSIFIED**: change both, differing only in namespace and `AccessGrantedPlugin`/`ClassifiedPlugin`.
- `StartupTracker.cs`: steps, timings and runs (Launch, Deploy, Return).
- `MenuBackgroundFix.cs`: settles a Random menu background early so background mods load theirs.

## Build and release

- The csproj needs `-p:SPTPath=<SPT root>`, and refuses to build unless `Assembly-CSharp.dll.spt-bak` exists (that is, SPT has patched the DLL). `C:\HUH` on the work laptop is patched (since November 2026), so real builds work there. `-p:CompileCheckOnly=true` skips the check, but those DLLs must never ship.
- Release: `scripts\pack.ps1 -SPTPath C:\HUH` from **PowerShell** (Bash mangles the path). It checks that the csproj `<Version>` and `PluginVersion` agree, builds and writes `dist\ACCESSGRANTED-<ver>.zip`.
- The version lives in two places: the csproj `<Version>` and `AccessGrantedPlugin.PluginVersion`.
- Zips hold only `BepInEx/plugins/ACCESSGRANTED.dll`. Never a README.
- GitHub releases (JoelHauser/ACCESSGRANTED): tag `vX.Y.Z`, title `ACCESS GRANTED X.Y.Z`, notes start with the hero image (`docs/images/alt-hero-v2.webp`), then "New in", "Install". Untested builds go out as pre-releases with " (pre-release)" in the title and a "Test build" line.
- To read game code: `ilspycmd -t <Type> C:\HUH\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll`, or `-p -o <dir>` for the whole thing (a few minutes).

## Raid loads: DeployScene

While this terminal covers a raid load (`DeployScene.Cover` from `BeginDeploy`, `Uncover` from `Hide` and `GrantDeploy`):

- The game's `MatchmakerTimeHasCome.ShowPlayerModel` (a turntable PMC) is skipped.
- Once the intro is over (`Covered`, every frame from `WatchDeploy`): the menu's `EnvironmentUI` scene and camera go off (`EnvironmentUIRoot.SetCameraActive`, not `ShowCameraContainer`, which would switch the game's main camera on). The deploy screen, final countdown, `MenuTaskBar` and `_bottomPanelBackground` stop drawing: their Canvas is disabled, or a disabled Canvas is added and destroyed afterwards.
- Setting **Frame Rate While Loading Raids** (Unlocked by default, which changes nothing). The user's rule: **the default must never lower FPS**. A 30/20/15 limit raises the `JobScheduler` force-mode multiplier (the game's loader runs `FrameTicks/2 * multiplier` per frame; the game uses 3) and `Application.backgroundLoadingPriority`. Both are restored on Uncover, the priority only if nothing changed it since.
- On Uncover, the canvases always come back. The background and the model come back only if `TimeHasCome`/`FinalCountdown` is still the current screen and the raid has not started: anywhere else the game sets the environment itself.
- The deploy screen keeps running underneath. The terminal never takes input; Esc calls `AbortMatching` while the game allows cancelling.

## Status

- 1.2.0 (DeployScene and the frame-rate setting) went out as a pre-release on 2026-11-23, **before any in-game test**. 1.1.0 is still Latest.
- To check before promoting it: the log lines from DeployScene; Esc during a load; no menu flash at raid start; a later raid with the terminal off still shows the game's loading screen normally (the canvases came back); load times with a limit against Unlocked.
