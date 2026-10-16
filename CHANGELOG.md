# Changelog

## 0.7.1

- On-screen message when you toggle auto-advance, the bigger HUD or the centered HUD (and when a diagnostic dump is written). Setting: `UI.ShowMessages`.
- Optional auto-advance for lines without voice, after a reading-time delay (`Dialogue.AutoAdvanceUnvoiced`, off by default; tune with `UnvoicedBaseSeconds` and `UnvoicedSecondsPerCharacter`).
- Windows all-in-one zip (BepInEx included) is published again.

## 0.7.0

- Voiced dialogue can advance automatically once the voice line and text are done: `Dialogue.AutoAdvanceVoiced` (F5, or Select + R3 on a controller). Off by default.
- All keyboard keys are remappable (`[Hotkeys]`), and every shortcut has a controller button combo (`[Controller]`). Defaults: F5 auto-advance, F6 bigger HUD, F7 centered HUD, F9 diagnostic dump. The testing keys (F8, End) are removed.
- The title and save screens are now kept inside the centered 16:9 area, so the save slot labels line up with their cards (`StartMenuView` added to `UI.MenuCanvases`).

## 0.6.0

- Controller shortcut for the bigger HUD: press L3 + R3 (both stick buttons) together. Useful on the
  Steam Deck without a keyboard. Setting: `Input.GamepadShortcuts`.
- On small screens (height 900 or less, e.g. the Deck's 1280x800) the bigger HUD is turned on once, the
  first time the game runs there. Your own choice is kept afterwards. Setting: `UI.AutoBigHUD`.
- Logs the game version and warns when the game has been updated since the last run.
- Steam Deck: added an uninstaller (`Uninstall from Steam Deck`).
- The all-in-one zip (BepInEx included) installs offline.

## 0.5.1

- 16:10 (Steam Deck, 1280x800): the pause, map, equipment and journal menus were zoomed in, with the
  tabs and footer cut off. They are world-space canvases seen by a camera sized for 16:9; on screens
  narrower than 16:9 that camera is now sized to the canvas.
- The F7 dump now lists each canvas's render mode, scale factor and camera.

## 0.5.0

- 16:10 (Steam Deck, 1280x800): on screens narrower than 16:9 the UI now scales by screen width so
  the full layout fits. New setting `UI.FixNarrowScreens` (default on).
- Easy Steam Deck install: double-click launcher, sets the Steam launch option automatically.

## 0.4.9

- F7 dump also lists the lights near the screen, everything drawn at a few screen points, and the
  game's room-edge darkening, and is saved on its own to `BepInEx/BelmontsCurseUltrawide_dump.txt`.
- Documented the lit circle at the screen sides on 32:9 as the game's own room-edge darkening.

## 0.4.7

- 60 fps cap during startup (fixes the occasional black screen at launch).

## 0.4.6

- Startup logos skipped properly (no more fast-forwarded logos or black screen).

## 0.4.x

- Bigger HUD option (F6), centered HUD option (F10), camera fixes for 32:9 and area transitions,
  map/journal layout, smoother frame pacing.
