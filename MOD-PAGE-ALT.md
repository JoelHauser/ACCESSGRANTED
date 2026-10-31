<!--
  The alternative mod-page description: the same information as MOD-PAGE.md, dressed up.
  Paste everything below this comment into the listing.

  The Forge allows no background or styling of its own (raw HTML is stripped), so the look
  comes from images, plus three Forge extras:
    - a blockquote followed by a line "{.is-info}" becomes a blue notice box;
    - "## Title {.tabset}" turns the ### headings under it into tabs, up to "{.endtabset}";
    - YouTube links embed (none used yet; a short clip of a launch would fit under the screenshot).
  Check it in the Forge editor's preview before saving.

  Images are served from the repository (docs/images), so they show once pushed to main.
  alt-hero-v2.webp is animated (renamed so caches drop the old, banded one) (7 s loop). Thumbnail: a square image, the Forge crops to 1:1.

  Short-description field:
    "Your launch becomes a TerraGroup sign-in terminal: the real load steps, timed, in a different department every time."
-->

![ACCESS GRANTED: the TerraGroup sign-in terminal](https://raw.githubusercontent.com/JoelHauser/ACCESSGRANTED/main/docs/images/alt-hero-v2.webp)

From the splash screen to the main menu, your launch becomes a TerraGroup sign-in terminal. It shows what EFT is really loading, timed against your own launches, and it's a different department every time.

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

{.is-info}

## Why you're seeing this

TerraGroup's network is locked down tight. Nobody gets in from outside.

So you have someone on the inside. A contact at the IT Service Desk set up the computer in your hideout's Intelligence Center with a real company sign-in. Every time you start EFT, it uses a different one, borrowed from a different department, so no single account draws attention. You see what that employee would see: their memos, their news and their take on what's happening in Norvinsk.

Somewhere along the way, the system learned your name. Your contact says not to worry about it.

Your contact also runs the quarterly sign-in review.

## Department directory

Fourteen departments, shuffled so the same one won't come back for several launches. The fifteenth only opens on 25 December.

![Fifteen TerraGroup employee badges, one per department](https://raw.githubusercontent.com/JoelHauser/ACCESSGRANTED/main/docs/images/alt-badges.png)

## Personnel file {.tabset}

### What it does

- **The real load.** Fifteen steps, from verifying game files to opening the main menu, each with a live timer and a log of the ones done. The bar is timed against your own previous launches, and a step that runs long is flagged.
- **A different department every launch.** Each has its own colours, logo frame, background, intro, progress bar, ending and wording.
- **Five layouts**, shuffled the same way.
- **A background drawing for each department** that moves with the load: Investor Relations' share price climbs, Legal's seal presses in, Facilities' pipework speeds up, Health's heartbeat runs under the readout.
- **Company traffic.** Hundreds of internal memos, priority intel on the region (boss sightings, PMC movements, trader and flea prices) and a news and stock ticker. Nothing repeats until you've seen the rest. Some of it knows your nickname and your faction.
- **Restrained glitches**, kept away from the moments EFT itself freezes. Now and then your badge gets flagged. Hold still.

The terminal only draws over the load. It doesn't hold the game up, and it never takes your mouse or keyboard.

### Install

Unzip into your SPT folder, so `ACCESSGRANTED.dll` lands in `BepInEx\plugins`. Then launch the game. Client only, no server part.

For SPT 4.1.x.

### Settings

In the F12 menu, under **ACCESS GRANTED**:

- **Theme** keeps one department, or leaves it on Random.
- **Glitches** can be Off, Subtle or Heavy.
- **Layout, Background, Logo Frame, Intro, Progress Bar, Ending** each pin one part instead of letting the department choose.

**Want a look without relaunching?** Press **Ctrl+F9** in the main menu to play the terminal over a pretend load. Left/Right changes department, Up/Down changes layout, Space re-rolls the rest, Esc closes.

### Good to know

- The first launch uses rough step times. From the second launch on, the bar is timed against your own PC.
- If you set the main-menu background to Random and use a mod that adds backgrounds, this also stops the load error that combination can cause.
- If EFT shows an error while loading, the terminal gets out of the way at once.

### Uninstall

Delete `BepInEx\plugins\ACCESSGRANTED.dll`. To remove its settings and launch history too, delete the files starting with `com.mybutthasarash.accessgranted` in `BepInEx\config`.

{.endtabset}

![](https://raw.githubusercontent.com/JoelHauser/ACCESSGRANTED/main/docs/images/departments-strip.png)

*Session closed. Have a productive day.*
