<!--
  The mod-page description. Paste everything below this comment into the listing.

  This is not the README: the README is for the repository, this is for someone deciding
  whether to download. When a release changes what a player sees, update both.

  The Forge shows the word "Tarkov" as "T*****", so say "EFT".

  Images are served from the repository (docs/images), so they show once pushed to main.
  The department logos are docs/images/departments (512 px) and departments/small (96 px),
  made from the in-game logo and each department's own colours.

  Short-description field:
    "Your launch becomes a TerraGroup sign-in terminal: the real load steps, timed, in a different department every time."
-->

# ACCESS GRANTED

**TerraGroup Employee Network. Authorised personnel only.**

From the splash screen to the main menu, your launch becomes a TerraGroup sign-in terminal. It shows what EFT is really loading, timed against your own launches, and it's a different department every time.

![The fifteen TerraGroup department logos](https://raw.githubusercontent.com/JoelHauser/ACCESSGRANTED/main/docs/images/departments-strip.png)

> **INTERNAL MEMO  //  IT SERVICE DESK**
>
> **TO:** All staff
>
> **RE:** Signing in at start-up
>
> ---
>
> A reminder that every company workstation signs in to the employee network when it starts up. Your session opens your department's terminal, with the day's memos, intelligence briefings and news feed.
>
> Staff working away from the office are reminded that company equipment remains company property and should be returned when no longer needed.
>
> A routine review found nothing unusual in sign-in activity this quarter. Thank you for your continued cooperation.
>
> *SIGNED: [REDACTED], IT SERVICE DESK  //  REF ITS-0417*

## Why you're seeing this

The computer in your hideout's Intelligence Center was pulled out of a TerraGroup office, and it still thinks it works there. Every time you start EFT, it boots up and signs in to TerraGroup's employee network, which never went offline.

It never gets the same desk twice. The network sends each sign-in to whichever department answers first, so one day you're on Security's terminal and the next on HR's, with their memos, their news and their take on what's happening in Norvinsk. It has even started greeting you by name.

## What the terminal shows

- **The real load.** Fifteen steps, from verifying game files to opening the main menu, each with a live timer and a log of the ones done. The bar is timed against your own previous launches, and a step that runs long is flagged.
- **A different department every launch.** Fourteen of them, shuffled so the same one won't come back for several launches. Each has its own colours, logo frame, background, intro, progress bar, ending and wording.
- **Five layouts**, shuffled the same way.
- **A background drawing for each department** that moves with the load: Investor Relations' share price climbs, Legal's seal presses in, Facilities' pipework speeds up, Health's heartbeat runs under the readout.
- **Company traffic.** Hundreds of internal memos, priority intel on the region (boss sightings, PMC movements, trader and flea prices) and a news and stock ticker. Nothing repeats until you've seen the rest. Some of it knows your nickname and your faction.
- **Restrained glitches**, kept away from the moments EFT itself freezes. Now and then your badge gets flagged. Hold still.
- **On 25 December**, the holiday schedule.

The terminal only draws over the load. It doesn't hold the game up, and it never takes your mouse or keyboard.

## Department directory

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

## Install

Unzip into your SPT folder, so `ACCESSGRANTED.dll` lands in `BepInEx\plugins`. Then launch the game. Client only, no server part.

For SPT 4.1.x.

## Settings

In the F12 menu, under **ACCESS GRANTED**:

- **Theme** keeps one department, or leaves it on Random.
- **Glitches** can be Off, Subtle or Heavy.
- **Layout, Background, Logo Frame, Intro, Progress Bar, Ending** each pin one part instead of letting the department choose.

**Want a look without relaunching?** Press **Ctrl+F9** in the main menu to play the terminal over a pretend load. Left/Right changes department, Up/Down changes layout, Space re-rolls the rest, Esc closes.

## Good to know

- The first launch uses rough step times. From the second launch on, the bar is timed against your own PC.
- If you set the main-menu background to Random and use a mod that adds backgrounds, this also stops the load error that combination can cause.
- If EFT shows an error while loading, the terminal gets out of the way at once.

## Uninstall

Delete `BepInEx\plugins\ACCESSGRANTED.dll`. To remove its settings and launch history too, delete the files starting with `com.mybutthasarash.accessgranted` in `BepInEx\config`.
