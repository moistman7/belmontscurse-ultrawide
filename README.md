# Castlevania: Belmont's Curse — Ultrawide

A BepInEx 6 (IL2CPP) plugin that makes *Castlevania: Belmont's Curse* play properly on ultrawide
monitors (21:9, 32:9 and wider). Everything is automatic: no launch options, no setup. Tested at
3440x1440 and 5120x1440.

> Unofficial community mod. It contains no game files. Use at your own risk and keep a backup of
> your saves.

## What it does

- Uses your monitor's full resolution and removes the side bars; the picture fills the screen.
- Keeps the camera following and framing like it does on 16:9: no camera stopping, swinging off
  when you enter an area, or zoomed-in boss fights.
- Scales the UI correctly. The pause menu, journal and map stay in a centered 16:9 area (their
  backgrounds still fill the screen), and the map isn't squished.
- Removes the cutscene black bars and shows pre-rendered videos uncropped.
- Skips the startup company logos.
- Caps the frame rate at 60 while the game starts up, which stops the occasional black screen at
  launch. Your own frame rate and V-Sync settings take over at the title screen.
- Optional: HUD in the centered 16:9 area, and a bigger HUD (see Keys).

## Requirements

- Castlevania: Belmont's Curse, Windows x64 (tested on Unity 2022.3.62).
- [BepInEx 6 bleeding edge, build be.788, Unity IL2CPP x64](https://builds.bepinex.dev/projects/bepinex_be).
  Not BepInEx 5.

## Install

1. Close the game and install BepInEx 6 IL2CPP x64 into the game folder (where
   `Castlevania Belmont's Curse.exe` is). The first launch with BepInEx can sit on a black screen
   for a few minutes while it sets itself up; later launches are quick.
2. Copy `BelmontsCurseUltrawide.dll` to `BepInEx/plugins/` (replace any older version). The release
   zip already has it in that folder, so you can extract it straight into the game folder.
3. Launch the game. `BepInEx/LogOutput.log` should contain
   `Belmont's Curse Ultrawide 0.7.1 loaded`.

To uninstall, delete the DLL.

## Steam Deck / Linux (Proton)

The Deck's own screen is 16:10 (the mod fixes the cropped menus there), and it also handles
docking to an ultrawide monitor. Install takes a few clicks:

1. Switch to Desktop Mode and download the release zip (the `BelmontsCurseUltrawide-v*.zip`) from
   the Releases page. Extract it (right-click > Extract).
2. Open the extracted folder, then `steamdeck`, and double-click **Install on Steam Deck**. (If the
   Deck asks, choose to run it / "Execute".) It finds the game (including on an SD card), installs
   BepInEx 6 be.788 and the plugin, and sets the Steam launch option for you. If Steam is open it
   asks to close it first.
3. Go back to Gaming Mode and play. The first launch can sit on a black screen for several minutes;
   later ones are quick.

Prefer a terminal? Run `./steamdeck/install.sh` (add the game folder as an argument if it can't
find it). If you'd rather set the launch option yourself, or the script says it couldn't, open the
game's Properties > General > Launch Options and paste:

    WINEDLLOVERRIDES="winhttp=n,b" %command%

No keyboard needed: press the left and right stick buttons (L3 + R3) together to toggle the bigger
HUD. On the Deck's small screen the bigger HUD is turned on automatically the first time. To remove
the mod, double-click **Uninstall from Steam Deck** in the `steamdeck` folder.

## Keys

| Key | Action |
|---|---|
| F5 | Auto-advance voiced dialogue on/off (remembered) |
| F6 | Auto-advance dialogue without voice on/off (remembered) |
| F7 | Bigger HUD on/off (remembered) |
| F8 | HUD in the centered 16:9 area, or at the screen edges (remembered) |
| F9 | Write a diagnostic dump to the log and to `BepInEx/BelmontsCurseUltrawide_dump.txt` |
| Select + R3 (controller) | Auto-advance voiced dialogue, same as F5 |
| Select + Right Shoulder (controller) | Auto-advance dialogue without voice, same as F6 |
| L3 + R3 (controller) | Bigger HUD on/off, same as F7 (press both stick buttons together) |
| Select + L3 (controller) | Centered HUD on/off, same as F8 |
| Select + Start (controller) | Diagnostic dump, same as F9 |

Keyboard keys can be changed in the `[Hotkeys]` section of the settings file, and the controller buttons in
`[Controller]` (see below). Use a key name such as
`F10`, `Home`, `Backquote` or `Numpad5`.

## Settings

`BepInEx/config/local.belmontscurse.ultrawide.cfg` is created on the first launch. Edit it while the
game is closed. The defaults are meant to just work.

| Setting | Default | Meaning |
|---|---|---|
| `General.SkipIntros` | `true` | `false` keeps the startup logos. |
| `General.StartupFpsCap` | `60` | Frame rate limit during startup only. `0` = off. Still getting a black screen at launch? Try `30`. |
| `Camera.CameraLogicAspect` | `auto` | Recommended. `2.4` lets the camera use 21:9 framing (wider view, but it may misbehave in some rooms). |
| `UI.CenterMenus` | `true` | Pause menu, journal and popups in the centered 16:9 area. |
| `UI.FixNarrowScreens` | `true` | On screens narrower than 16:9 (16:10, e.g. the Steam Deck) the UI matches the screen width so menus are not cropped. |
| `UI.CenterHUD` | `false` | HUD in the centered 16:9 area (same as F8). |
| `UI.BigHUD` | `false` | Bigger HUD (same as F7). |
| `UI.AutoBigHUD` | `true` | On small screens (height 900 or less, e.g. the Steam Deck) turns the bigger HUD on once, the first time. Your choice is kept afterwards. |
| `Dialogue.AutoAdvanceVoiced` | `false` | Advances voiced dialogue once the voice and text are done (same as F5). Lines without voice wait for you unless `AutoAdvanceUnvoiced` is on. |
| `Dialogue.AutoAdvanceDelay` | `0.4` | Seconds to wait before advancing. |
| `Dialogue.AutoAdvanceUnvoiced` | `false` | Also advance lines without voice, after a reading-time delay (same as F6). |
| `Dialogue.UnvoicedBaseSeconds` / `Dialogue.UnvoicedSecondsPerCharacter` | `1.0` / `0.05` | Delay for unvoiced lines: base seconds plus this per character. |
| `UI.ShowMessages` | `true` | Short on-screen message when you toggle auto-advance or the HUD options. |
| `Input.GamepadShortcuts` | `true` | L3 + R3 toggles the bigger HUD. |
| `UI.HUDScale` | `1.25` | How much bigger BigHUD makes it (1.1 to 1.6 is sensible). |
| `Resolution.ForceResolution` | `true` | Use the monitor's resolution at startup. |
| `Resolution.Width` / `Height` | `0` | `0` = native; set both to force a specific resolution. |
| `Resolution.ScreenMode` | `FullScreenWindow` | Or `ExclusiveFullScreen`, `MaximizedWindow`, `Windowed`. |

Other settings in the file are for troubleshooting and can be left alone.

## Known behaviour

- On very wide screens (32:9) you can see a soft lit circle with darkness around it at the sides,
  on walls and trees outside the room you're in. That's the game's own darkening of what lies
  outside the current room; on 16:9 its edge is just off-screen. Turning it off makes the outside
  of rooms look flat and overly bright, so the mod leaves it as the game draws it.
- The camera logic is told the screen is 16:9 so rooms frame as designed. The extra width shows
  what's beside the room, which the game never expected to be on screen.
- A game update can change the classes the mod relies on. Each fix switches itself off if it keeps
  failing, and the others keep running.

## Troubleshooting

- Black screen on the very first launch: wait a few minutes (BepInEx setup).
- Occasional black screen at launch afterwards: set `StartupFpsCap = 30`.
- Nothing changed in game: check that `winhttp.dll` is next to the game's .exe and that
  `BepInEx/LogOutput.log` exists and mentions "Belmont's Curse Ultrawide".
- Settings got messed up: close the game and delete `local.belmontscurse.ultrawide.cfg`; a fresh
  one with the defaults is created on the next launch.

## Reporting a problem

Press F9 in game while the problem is on screen, then attach `BepInEx/BelmontsCurseUltrawide_dump.txt`,
`BepInEx/LogOutput.log` and a screenshot if you can. Mention your resolution. These files stay
local; nothing is transmitted.

## Build

Needs the .NET 6 SDK or newer, the game, and BepInEx 6 IL2CPP installed and run once (so it has
generated `BepInEx/interop`).

    dotnet build -c Release -p:GameDir="<path to the game folder>"

or set the `BELMONTS_CURSE_DIR` environment variable. Add `-p:SkipDeploy=true` to skip copying the
DLL into `BepInEx/plugins`. The output is `bin/Release/BelmontsCurseUltrawide.dll`.

## How it works

- **Camera:** the gameplay camera's `aspect` is held at 16:9 so Cinemachine confiners, follow and
  zoom behave as on 16:9, while a full-width projection matrix is set each frame so the picture
  fills the screen. During rendering the real aspect is restored so full-screen effects (door
  light shafts and similar) cover the whole width.
- **Width-based zoom:** `CameraScreenSize` components that size the camera by width are converted
  to the height a 16:9 screen would show.
- **UI:** canvas scalers match height on wide screens; menu and (optionally) HUD canvases are
  anchored into a centered 16:9 area, with full-screen backdrops left stretched.
- **Videos and render-texture cameras** are fitted to their panels instead of stretched.

## License

[MIT](LICENSE)
