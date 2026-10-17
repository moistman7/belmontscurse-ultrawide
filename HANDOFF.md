# Belmont's Curse ultrawide plugin: handoff

Status as of 2026-10-17: **v0.8.5 is the latest published release** (the
dialogue-capture change in `Poll` was released without an in-game dialogue test). Tested on 3440x1440 (21:9) Windows only. Not tested: Steam Deck hardware, 16:10, 32:9,
PlayStation / Switch controller sprites, the F1 / controller shortcut for the mod menu, hide-mouse.

A BepInEx 6 (be.788) IL2CPP plugin for *Castlevania: Belmont's Curse* (Unity 2022.3.62f2, game 1.1.3) that makes the game
work on ultrawide and non-16:9 screens, plus quality-of-life features. Repo: https://github.com/moistman7/belmontscurse-ultrawide.
Separate from the Spirit of the Samurai plugin.

## Paths

- Game: `D:\SteamLibrary\steamapps\common\Castlevania Belmont's Curse`. Plugin DLL goes to `BepInEx\plugins`.
- Config: `BepInEx\config\local.belmontscurse.ultrawide.cfg`. Log: `BepInEx\LogOutput.log`.
  F9 dump: `BepInEx\BelmontsCurseUltrawide_dump.txt`. Optional saved history: `BepInEx\BelmontsCurseUltrawide_history.txt`.

## Build, test, release

- Build: `dotnet build -c Release -p:GameDir="D:\SteamLibrary\steamapps\common\Castlevania Belmont's Curse"`.
  The build copies the DLL into the game's `plugins` folder, which **fails while the game runs** (DLL locked). Check
  `tasklist | grep -i castle`, then `cmp` the built and installed DLL.
- Tests: `dotnet test tests/Logic.Tests` (no game needed). They cover `Logic.cs` only: slider stepping, text-size estimate,
  saved-history format, menu-position drift check, text cleaning. The main csproj excludes `tests/**`.
- Version strings: `Plugin.cs` (attribute + startup log), `UwBehaviour.cs` (dump header), `README.md`, `README.txt`, `CHANGELOG.md`.
- Release zips (`dist/`, not committed): plain = `README.txt`, `BepInEx/plugins/BelmontsCurseUltrawide.dll`, `steamdeck/`;
  AllInOne = adds `.doorstop_version`, `doorstop_config.ini`, `winhttp.dll`, `BepInEx/` core, `dotnet/`. Stage the AllInOne from the
  previous `dist/aio-<version>` folder. Make zips with **Windows** `C:\Windows\System32\tar.exe -a -c -f out.zip ...`
  (Git Bash tar writes a broken zip). Checksum format `<sha256> *<file>`. Publish with `gh release create vX.Y.Z ... --notes-file`.
- Shell notes: no Python. When writing C# that contains backslashes, use the file tools, not shell heredocs or `sed` (they strip them).

## Code map

| File | Purpose |
|---|---|
| `Plugin.cs` | Entry point, every config entry, old-setting migrations, startup log. |
| `UwBehaviour.cs` | Main MonoBehaviour: job scheduler, camera, menu/HUD anchoring (`CenterCanvas`, `UiOrig`, drift check), videos, letterbox, resolution, hotkeys, controller shortcuts, F9 dump, per-feature timing. |
| `DialogueHistory.cs` | Harmony hooks on the dialogue reader; records lines, names, portraits, voice; history input; optional disk save; skip-hook handling. |
| `HistoryView.cs` | The history window (game panel art, scrolling card list, header buttons, mouse hit-testing) and shared drawing helpers other overlays use. |
| `ModMenu.cs` | The mod settings panel: toggles, sliders, reset, mouse. Opened by `ModSettingsRow.cs` or F1. |
| `ModSettingsRow.cs` | Adds the "Mod settings" row to the game's Graphics tab (a copy of the V-Sync row). |
| `AntiAliasing.cs` | Makes the game's own (unsupported) TAA option selectable. |
| `GamepadGlyphs.cs` | The game's controller button sprites (`GamepadDescriptor`), picked per pad. |
| `GameUiFont.cs`, `GameUiSprites.cs` | Game font/material and panel lookups; portrait copy (`Keep`) and transparent-margin trim. |
| `HistoryVoice.cs`, `VoiceAdvance.cs`, `Toast.cs`, `DialogueDim.cs`, `MouseHide.cs` | Voice replay, auto-advance, on-screen messages, dim-bar widening, idle mouse hiding. |
| `Logic.cs` | Pure helpers with no game types (tested). |
| `steamdeck/` | Steam Deck installer scripts (unchanged since 0.6.0). |

## Things learned

- Interop DLLs show names and signatures only. Decompile with `~/.dotnet/tools/ilspycmd.exe -t <Type> "<path>\Assembly-CSharp.dll"`.
  Settings UI types: `UIToggle`/`UISelectable` are in `Doozy.Runtime.UIManager.Components`; settings rows are `UIBoolSettingBinding` etc.
- The game's panel sprite has transparent padding around the visible frame; `HistoryView.Inflate` compensates with factors
  (0.35 x height sideways, 0.045 x height vertically) estimated from 21:9 screenshots. Unverified at other aspects.
- Settings screens live under `MainMenu/StartMenuView/SettingsView/TabsLayout_Settings/TabsContent_Group/TabContent_*`; rows are
  children of `ContentLayout` (which also holds the `VerticalNavigationGroup`).
- The TAA option already exists in the game's AA setting but is `supported=false`; the mod sets it supported and relabels the row.
- **Do not flip menu layout switches while menus exist.** `CenterMenus` / `FixNarrowScreens` are read once at startup (`Plugin.MenusOn`,
  `NarrowOn`); live toggling once left the pause menu mis-scaled. The mod menu labels them "next launch".
- `UILocalizedString.UpdateString` must not be hooked: it runs for every localized string in the game (measurable frame cost).
  Dialogue text is read by polling the active line labels instead.
- The first open of the history window used to hitch (~30 ms); it is built and shown once with placeholder text in the background
  after launch (`HistoryView.Prewarm`). F9 dump timing lines name the slowest run of each per-frame feature.
- The dump itself costs 30-60 ms when run; that is expected, not a regression.

## Open items

1. Verify in game: F1 and Select + Left Shoulder, hide-mouse, mouse use in the history and mod menus, saved history, sliders.
2. Verify the restored controller glyphs in the history window (0.8.3 regression; 0.8.4 falls back to loading the whole folder).
3. Steam Deck, 16:10, 32:9, PlayStation / Switch glyphs.
4. Speech-bubble lines have no speaker; the log prints where the first few live ("line without a speaker label at ...").
5. Which change fixed the earlier cutscene-skip report was never proven (removed always-on skip hook, removed the `UpdateString` hook).
   The log now prints "Dialogue skip hook on/off".
6. Panel-art padding factors at other aspect ratios.
7. The `DialogueHistory.Poll` stale-entry handling (0.8.4) has not been run against real dialogue: the test session captured no lines (`lines=0`).

## Releases

0.4.9, 0.5.0, 0.5.1, 0.6.0, 0.7.0 (auto-advance, remappable keys), 0.7.1, 0.8.0 (pre-release, history), 0.8.1, 0.8.2 (TAA, frame-time fixes),
0.8.3 (mod settings menu, F1, hide mouse), 0.8.4 (mod menu sliders/mouse, history mouse, glyph fallback), 0.8.5 (keep mouse always hidden). See `CHANGELOG.md`.
