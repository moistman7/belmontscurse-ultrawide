# Changelog

## 0.8.4 (unreleased)

- **Controller button sprites in the history window** are repaired. 0.8.3 loaded only the matching glyph descriptor, which could leave the shoulder and face-button sprites empty so only the D-pad showed; it now falls back to loading the full glyph folder and logs when it has to.
- **Mod menu**: sliders (bigger-HUD size, HUD margins, auto-advance timing, mouse idle time), a two-step "Reset to defaults", and mouse support (hover, click, wheel).
- **History** can be scrolled and selected with the mouse; clicking the selected card replays its voice.
- **Optional history across sessions** (`Dialogue.SaveHistoryToDisk`, off by default): text and speaker names only, in `BepInEx/BelmontsCurseUltrawide_history.txt`.
- **Menu positions** the mod has recorded are re-read if the game moves them in the meantime, instead of being restored to a stale position.
- The log now says when the dialogue-skip hook is switched on or off, and where a line without a speaker label (a speech bubble) lives.
- Unit tests for the pure helpers (`tests/Logic.Tests`, `dotnet test`).

### Known issues
- Speech-bubble lines are recorded without a speaker; saved history has no portraits or voice.
- Changing "Center menus" or "Fit narrow screens" needs a restart to take effect.
- Untested: Steam Deck, 16:10, 32:9, PlayStation / Switch controller sprites, and the F1 / controller shortcut for the mod menu.

## 0.8.3

- **Mod settings menu**: a "Mod settings" row at the bottom of Settings > Graphics opens a panel (in the game's own style, with its controller button sprites) where you can switch the mod's options on and off. Most apply immediately; each option that doesn't says when it takes effect. **F1** (controller: Select + Left Shoulder) opens the same panel from anywhere and pauses the game while it is open. Remappable under `ModMenu`.
- **Hide mouse when idle** (`Input.HideMouseOnIdle`, `Input.MouseIdleSeconds`, off by default): the cursor hides after it has been still and returns on movement or click.
- **Menu layout switches** (`CenterMenus`, `FixNarrowScreens`) are read at startup. Changing them while menus were open could leave a menu half re-scaled.
- History opens instantly: the window and its fonts are warmed up in the background after launch. The history now also shows each line's size without a layout pass. The log prints `History opened: open=… show=…` and the F9 dump lists the slowest run of each feature.
- Dialogue-skip handling is only touched while history or the mod menu is open.

## 0.8.2

- **TAA** now appears in the Video menu's anti-aliasing choices (`Graphics.EnableTAAOption`, on by default). It is the game's own TAA mode, added to the menu; the game applies it.
- Fixed a frame-time cost: the history feature no longer hooks every localized text update in the game, and its window is built ahead of time instead of the first time you open it. Portrait handling is cheaper.
- The dialogue-skip suppression now only exists while history is open, so cutscene and dialogue skipping run unhooked in normal play. Setting `Dialogue.HistoryEnabled = false` installs no history hooks at all.
- Auto-advance picks up each line as it starts (fixes it stopping after the dialogue alternated between the left and right boxes).
- History: the "Current" button's contents are centered; scroll is smoother; speaker names have a fallback source.
- F9 dump now lists the slowest single run of each per-frame feature, to help find stutters.

## 0.8.1

- Dialogue history: the "Current" button's glyph and label are now centered in its panel.
- Tested on 3440x1440 (21:9) Windows only. Steam Deck, 16:10 and 32:9 are untested.

## 0.8.0

- **Dialogue history** (F10, or Select + North). Records each dialogue line for the session with the speaker's name, portrait and voice. Drawn with the game's own panel art, font and controller button sprites. Smooth scrolling, opens at the newest line; PageUp/PageDown or the shoulders move a screenful. A header lists Select / Older / Newer / Voice / Close; a "Current" button under the cards selects the line on screen now. Auto-advance and dialogue skipping pause while it is open. `Dialogue.HistoryEnabled`, `Dialogue.HistoryLimit` (default 200); nothing is written to disk.
- **Voice replay** for recorded lines: R, or X/Square. Uses its own audio player and the game's voice data; stops when you move, close, or a live voice starts. Remappable (`ReplayHistoryVoice`).
- **Controller glyphs**: prompts use the game's real Xbox / PlayStation / Switch button sprites, with key names on keyboard.
- **HUD safe area**: `UI.HUDSafeAreaHorizontal` and `UI.HUDSafeAreaVertical` inset the HUD by 0-20% per edge (default 0).
- **TAA option** in the Video menu's anti-aliasing choices (`Graphics.EnableTAAOption`). Untested in game; please report how it behaves.
- **Auto-advance** fix: it could stop working after the dialogue box alternated between left and right portraits. It now picks up the active line as it starts.
- Toast messages use the game's font and panel. Small performance trims (fewer scene scans, less per-frame text work).
- Tested on 3440x1440 (21:9) Windows only. Steam Deck, 16:10 and 32:9 are untested.

## 0.7.1

- On-screen message when you toggle auto-advance, the bigger HUD or the centered HUD (and when a diagnostic dump is written). Setting: `UI.ShowMessages`.
- Optional auto-advance for lines without voice, after a reading-time delay (`Dialogue.AutoAdvanceUnvoiced`, off by default; tune with `UnvoicedBaseSeconds` and `UnvoicedSecondsPerCharacter`).
- New key layout, grouped by purpose: F5 / F6 auto-advance (voiced / no voice), F7 bigger HUD, F8 centered HUD, F9 diagnostic dump. Controller: Select + R3 / Right Shoulder / L3 / Start (bigger HUD stays L3 + R3). Settings that still have the 0.7.0 keys are moved to the new ones.
- Full-screen dialogue: the dark bar along the bottom now reaches the left edge of ultrawide screens (`UI.DialogueDimOverscan`).
- Windows all-in-one zip (BepInEx included) is published again.

## 0.7.0

- Voiced dialogue can advance automatically once the voice line and text are done: `Dialogue.AutoAdvanceVoiced` (F5, or Select + R3 on a controller). Off by default.
- All keyboard keys are remappable (`[Hotkeys]`), and every shortcut has a controller button combo (`[Controller]`). Defaults: F5 auto-advance (voiced), F6 auto-advance (no voice), F7 bigger HUD, F8 centered HUD, F9 diagnostic dump. The testing keys (F8, End) are removed.
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
