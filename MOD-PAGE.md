<!--
  The mod-page description. Paste everything below this comment into the listing.

  This is not the README: the README is for the repository, this is for someone deciding
  whether to download. When a release changes what a player sees, update both.

  The Forge shows the word "Tarkov" as "T*****", so say "EFT".

  Images are served from the repository (docs/images), so they show once pushed to main.
  The department logos are docs/images/departments (512 px) and departments/small (96 px),
  made from the in-game logo and each department's own colours.

  Short-description field:
    "Replaces EFT's loading screens with a TerraGroup sign-in terminal: the game's real load steps and timings, in a different department every launch."
-->

# ACCESS GRANTED

> **What this mod does:** it replaces EFT's loading screens with a TerraGroup sign-in terminal.
>
> - **When you start the game**, from the splash screen to the main menu.
> - **When you go back to the menu after a raid**, once the after-action report is done. On by default.
> - **When you load into a raid**, from pressing Ready to spawning in. Optional, off by default.
>
> The terminal shows what the game is actually loading, step by step and timed, with company memos, intel and a news ticker to read while you wait. It only changes what's on screen during loading: no gameplay changes, and loading takes as long as it always did.

![The fifteen TerraGroup department logos](https://raw.githubusercontent.com/JoelHauser/ACCESSGRANTED/main/docs/images/departments-strip.png)

## What you see

- **The real load.** Fifteen steps, from verifying game files to opening the main menu, each with a live timer and a log of the ones done. The bar is timed against your own previous launches, and a step that runs long is flagged.
- **A different department every launch.** Fourteen of them, shuffled so the same one won't come back for several launches. Each has its own colours, logo frame, background, intro, progress bar, ending and wording.
- **Five layouts**, shuffled the same way.
- **A background drawing for each department** that moves with the load: Investor Relations' share price climbs, Legal's seal presses in, Facilities' pipework speeds up, Health's heartbeat runs under the readout.
- **Things to read.** Internal memos, intel on the region (boss sightings, PMC movements, trader and flea prices) and a news and stock ticker. Nothing repeats until you've seen the rest, and some of it knows your nickname and faction.
- **Light glitches**, kept away from the moments EFT itself freezes.
- **On 25 December**, the holiday schedule.

## The departments

| | Department | On file |
|---|---|---|
| ![](https://raw.githubusercontent.com/JoelHauser/ACCESSGRANTED/main/docs/images/departments/small/corporate.png) | **Corporate Headquarters** | Virtus in scientia |
| ![](https://raw.githubusercontent.com/JoelHauser/ACCESSGRANTED/main/docs/images/departments/small/research.png) | **Research Division** | TerraGroup Labs |
| ![](https://raw.githubusercontent.com/JoelHauser/ACCESSGRANTED/main/docs/images/departments/small/security.png) | **Security Department** | Authorised personnel only |
| ![](https://raw.githubusercontent.com/JoelHauser/ACCESSGRANTED/main/docs/images/departments/small/finance.png) | **Investor Relations** | TGRP, Norvinsk Exchange |
| ![](https://raw.githubusercontent.com/JoelHauser/ACCESSGRANTED/main/docs/images/departments/small/records.png) | **Records Management** | Confidential |
| ![](https://raw.githubusercontent.com/JoelHauser/ACCESSGRANTED/main/docs/images/departments/small/executive.png) | **Executive Board** | Executive suite |
| ![](https://raw.githubusercontent.com/JoelHauser/ACCESSGRANTED/main/docs/images/departments/small/internal-affairs.png) | **Internal Affairs** | This session is being reviewed |
| ![](https://raw.githubusercontent.com/JoelHauser/ACCESSGRANTED/main/docs/images/departments/small/logistics.png) | **Logistics and Supply** | Moving what matters |
| ![](https://raw.githubusercontent.com/JoelHauser/ACCESSGRANTED/main/docs/images/departments/small/human-resources.png) | **Human Resources** | Our people are our greatest asset |
| ![](https://raw.githubusercontent.com/JoelHauser/ACCESSGRANTED/main/docs/images/departments/small/it-department.png) | **IT Service Desk** | Have you tried turning it off and on again |
| ![](https://raw.githubusercontent.com/JoelHauser/ACCESSGRANTED/main/docs/images/departments/small/medical.png) | **TerraGroup Health** | Your health. Our science. |
| ![](https://raw.githubusercontent.com/JoelHauser/ACCESSGRANTED/main/docs/images/departments/small/facilities.png) | **Facilities Management** | Safety first |
| ![](https://raw.githubusercontent.com/JoelHauser/ACCESSGRANTED/main/docs/images/departments/small/legal.png) | **Legal Department** | Privileged and confidential |
| ![](https://raw.githubusercontent.com/JoelHauser/ACCESSGRANTED/main/docs/images/departments/small/public-relations.png) | **Communications** | Building a better tomorrow |
| ![](https://raw.githubusercontent.com/JoelHauser/ACCESSGRANTED/main/docs/images/departments/small/holiday.png) | **Holiday Schedule** | 25 December only |

## The idea

You have a contact inside TerraGroup. Each time you start the game, your hideout's computer signs in to the company network with a borrowed employee account, and you see what that employee would see while the game loads.

## Install

Unzip into your SPT folder, so `ACCESSGRANTED.dll` lands in `BepInEx\plugins`. Then launch the game. Client only, no server part.

For SPT 4.1.x.

## Settings

In the F12 menu, under **ACCESS GRANTED**:

- **Theme** keeps one department, or leaves it on Random.
- **Glitches** can be Off, Subtle or Heavy.
- **Raid Loading** switches the terminal on for raid loads (off by default) and for the way back after a raid (on by default), and can show the map's name.

Tick **Advanced settings** for the rest: **Layout, Background, Logo Frame, Intro, Progress Bar, Ending** each pin one part instead of letting the department choose, and **Preview Key** changes the preview's key.

**Want a look without relaunching?** Press **Ctrl+F9** in the main menu to play the terminal over a pretend load. Left/Right changes department, Up/Down changes layout, Space re-rolls the rest, T switches between a launch and a raid load, Esc closes.

## Compatibility

- [CLASSIFIED](https://github.com/JoelHauser/CLASSIFIED): the two take turns, launch by launch, and CLASSIFIED's **With ACCESS GRANTED** setting can always show one of them instead. Raid loads and the way back go to whichever terminal you signed in to.
- [Deploy Screen Overhaul](https://github.com/JoelHauser/LoadingRaid): both replace the raid loading screen, so this terminal stands aside for raid loads by default. **With Deploy Screen Overhaul** can take turns, or always show the terminal.

## Good to know

- **Does it make loading faster?** No. It shows you what's loading and how long each step takes; the load itself is untouched.
- **Does it change gameplay?** No. It only draws over the loading screens and never takes your mouse or keyboard (except Esc to cancel a raid load, while the game allows it).
- The first launch uses rough step times. From the second launch on, the bar is timed against your own PC.
- If you set the main-menu background to Random and use a mod that adds backgrounds, this also stops the load error that combination can cause.
- If EFT shows an error while loading, the terminal gets out of the way at once.

## Uninstall

Delete `BepInEx\plugins\ACCESSGRANTED.dll`. To remove its settings and launch history too, delete the files starting with `com.mybutthasarash.accessgranted` in `BepInEx\config`.
