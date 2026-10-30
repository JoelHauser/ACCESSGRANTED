# ACCESS GRANTED

A BepInEx plugin for SPT 4.1.x that turns the game's launch, from the splash screen to the main menu, into a TerraGroup sign-in terminal.

Instead of "Loading profile data...", you see what the game is actually doing: each load step as it starts, how long the finished ones took, and a progress bar timed against your own previous launches. When the main menu is ready, the terminal grants you access and fades out over it.

## What you get

- **A real loading readout.** Fifteen load steps, from verifying game files to opening the main menu, with live timers and a log of finished steps. A step that runs well past its usual time is flagged.
- **A different TerraGroup department each launch.** Corporate HQ, Research, Security, Finance, Records, Legal, IT, HR, Health, Facilities, Communications, Logistics, plus rare Executive and Internal Affairs terminals. Each has its own colours, background, logo frame, intro, progress bar, ending and wording.
- **A faint background drawing per department** that follows the load and stays clear of the text: Finance's share price climbs, Legal's seal presses in, Facilities' pipework runs faster, Health's heartbeat runs under the readout, and so on.
- **The company talking.** Internal memos, priority intel on the region (bosses, PMC movements, traders, flea prices), a news and stock ticker, and the odd notice nobody admits to sending. Some of it knows your nickname and faction.
- **Restrained glitches**, kept away from the moments the game itself freezes.
- **A seasonal terminal** on 25 December.

The terminal never takes input and never holds the game up. It draws over the load; it does not slow it down.

## Install

Extract the release zip into your SPT folder (the one containing `EscapeFromTarkov.exe`). It adds `BepInEx/plugins/StartupProgress.dll`.

## Settings

In the F12 menu (BepInEx Configuration Manager), under **Startup Progress**:

| Setting | What it does |
|---|---|
| Enabled | Turns the terminal on or off. |
| Theme | A fixed department, or Random. |
| Glitches | Off, Subtle or Heavy. |
| Layout, Background, Logo Frame, Intro, Progress Bar, Ending | Pin any part instead of letting the department choose. |
| Preview Key | Plays the terminal in the main menu (default Ctrl+F9). |

## Preview

Press **Ctrl+F9** in the main menu to watch the terminal over a pretend load. While it plays: **Left/Right** changes department, **Up/Down** changes layout, **Space** re-rolls the rest, **R** replays, **Esc** closes. Nothing from a preview is saved.

## Building

```powershell
scripts\pack.ps1 -SPTPath C:\SPT           # build and pack dist\StartupProgress-<version>.zip
scripts\pack.ps1 -SPTPath C:\SPT -Install  # also copy the DLL into the install
```

The SPT install must have been started through the SPT Launcher once, so its Assembly-CSharp is the patched one.
