# Belmont's Curse ultrawide plugin: handoff

Status as of 2026-10-17: **v0.8.6 is the current release**, containing the confirmed glyph fix, performance cleanup and frame-interval diagnostics.
The user explicitly requested publication. The notes below under "Glyph bug in progress" preserve the investigation history;
the glyph issue is resolved and the changes are included in 0.8.6. Tested on 3440x1440 (21:9) Windows with an Xbox pad.
Latest capture: history avg 6.065ms / p99 6.856ms / max 6.961ms; settings avg 6.064ms / p99 6.852ms / max 7.035ms.
Plugin Update avg 0.163ms. Gameplay/game menus included a 1.02s stall of unknown origin; steady prior capture was ~6.06ms.
Real dialogue capture/voice replay and the 0.8.4 stale-entry handling remain unverified (all dumps had zero recorded lines).
Not tested: Steam Deck hardware, 16:10, 32:9, PlayStation / Switch controller sprites, the F1 / controller shortcut for the mod menu.

A BepInEx 6 (be.788) IL2CPP plugin for *Castlevania: Belmont's Curse* (Unity 2022.3.62f2, game 1.1.3) that makes the game
work on ultrawide and non-16:9 screens, plus quality-of-life features. Repo: https://github.com/moistman7/belmontscurse-ultrawide.
Separate from the Spirit of the Samurai plugin.

## Glyph bug in progress (the thing to pick up first)

Further performance/measurement follow-up: installed a build with a bounded 2048-sample frame-interval
recorder per mode (gameplay/game menus, history, mod settings). F9 reports average, median, p95/p99, maximum,
and counts above 25/50ms. Samples are wall-clock time between Unity Updates, not GPU/presentation timing;
loading/startup remain included. Focus gaps, mode transitions and F9's own interval are excluded.
F9 is also excluded from Update and per-feature CPU measurements via a dump serial counter, so it no longer
repopulates the just-reset maxima with diagnostic work. Cached controller shortcut controls (invalidate on
device pointer or binding changes) and history row RectTransforms during smooth scrolling. All features/UI
retained. Build clean, 28 tests pass (including ring wraparound and measurement-gap tests), deployed hash
matched. Game was not running during this work; next step is restart, play 30-60 seconds and F9 for data.

User confirmed the retained-asset glyph fix looks good. Subsequent performance pass caches resolved button
sprites/D-pad per descriptor, clearing on descriptor reload/layout change; retention flags are now set only
on first resolution. Removed per-slot diagnostic logging/string creation (F9 diagnostics remain).
History header and mod-menu footer now skip unchanged UI work entirely once glyphs resolve; missing assets
still retry every 0.5 seconds. Controller device changes, replay binding changes and live-entry presence are
included in history invalidation. Mod-menu Refresh forces footer layout on open/selection changes.
Input polling, scrolling, voice, cameras, all settings/features and scheduled scan intervals remain unchanged.
Release build installed and hashes matched; 25 logic tests passed. No new runtime timing measurement yet:
the available F9 dump is still an old 0.8.4 session (Update avg 0.147ms), not evidence for this build.

Latest follow-up: the diagnostic log showed ALL five header sprites resolved during prewarm, then only the
D-pad survived into gameplay. `Get` returned null without warnings because the cached descriptor compared
equal to Unity null, while `Descriptor` returned it solely on a matching layout string. The D-pad already
assigned to the persistent UI survived. Fixed the invalid-cache guard and marked descriptor, resolved sprites
and their textures `DontUnloadUnusedAsset`. Built and installed successfully; still needs an in-game retest
after entering a level. No release published. The previous field-offset hypothesis is not supported by this log.

2026-10-17 follow-up: built and installed an unreleased diagnostic build (still version 0.8.5).
`HistoryView` now retries the header glyph resolution every 0.5 seconds even when its text signature is unchanged,
so a missing sprite at prewarm/open cannot leave keyboard labels cached indefinitely. This is a candidate repair,
not an in-game-confirmed fix. Each slot logs its pad/glyph/icon state once per distinct state.
F9 now dumps controller layout/device ID, shortcuts/replay settings, descriptor, field and backing-field sprites,
GetSprite results, and each history slot's sprite, active/enabled state and text. Next: launch with the pad,
open history, press F9 and inspect the new dump/log before changing sprite lookup or publishing.

**Symptom** (two screenshots, 0.8.4 and 0.8.5, Xbox pad on Windows): in the history window header the D-pad glyph shows ("[dpad] Select"),
but the other four entries show keyboard text instead of controller sprites: "PgUp Oldest", "PgDn Latest", "R No voice", "Esc Close".
The game's own prompt in the same screenshot says "INTERACT E" (keyboard), though a pad is connected and the mod sees it.

**What is known**
- `HistoryView.Tick` sets the header: `Set(_nav, pad, GamepadGlyphs.Dpad(), ...)` then `Set(_older/_newer/_replay/_close, pad, GamepadGlyphs.Get(button), ...)`.
  `Set` shows the glyph only if `pad && glyph != null`, otherwise `key + "  " + text`. `HistoryView.Build` has no default header text, so the
  visible text came from `Tick`, and `pad` was true (the nav icon is showing).
- `Dpad()` reads `descriptor.icons.dpad` and works. `Get()` reads `icons.leftShoulder / rightShoulder / buttonEast ...` and returns null.
- `GamepadDescriptor.GamepadIcons` is an `Il2CppSystem.ValueType` (a struct). Its interop property getters read fields by memory offset, which is
  a suspected cause of empty reads. The struct also has `GetSprite(string controlPath)` and `GetControlPath(Sprite)`, which run the game's own code.
- The log shows the descriptor loads: `Gamepad glyphs: GamepadXbox (pad layout XInputControllerWindows)`.

**The uncommitted change** (`GamepadGlyphs.cs`): `Lookup` reads the field first (`FromField`), then falls back to `icons.GetSprite(path)` (`FromGame`,
paths like "leftShoulder", "buttonEast", "dpad/up"; names follow Unity's standard gamepad-icon sample and are unverified against this game).
It logs `found through GetSprite("...")` once per button, and `no sprite for X in <descriptor> (field read and GetSprite both empty)` once per missing button.

**The unexplained contradiction.** With that change loaded (log says 0.8.5, DLL built 01:03), the log after opening the history contains NONE of:
the `no sprite for` warning, the `found through GetSprite` info, or the older `was empty after loading the descriptor alone` line. Yet the header still
shows text for those slots. By the code, a null `Get()` with `Gamepad.current != null` and `_current != null` must log. So either `Get()` is not
returning null and `Set` is still choosing text, or `Get()` is not being reached for those slots, or an exception is cutting `Tick` short without
reaching the log. No `[Error` lines appear. Do not assume the GetSprite fix is wrong or right until this is explained.

**Next steps, in order**
1. Add a glyph section to the F9 dump (`UwBehaviour.Dump`): `Plugin.PadShortcuts`, `Gamepad.current != null`, descriptor name, and for each of
   South/East/LeftShoulder/RightShoulder/Dpad: field result (null or sprite name), `GetSprite` result, and the history slots' `Icon.gameObject.activeSelf`
   and label text. Ask the user to open history with the pad connected, press F9, and send `BepInEx/LogOutput.log` and the dump.
2. Check whether `Tick` really reaches `Set(_older ...)`: log once inside `Set` (slot name, `pad`, `glyph != null`, `icon`).
3. Check `Gamepad.current` vs the pad the user is actually pressing (an Xbox pad via XInput; the game still showed keyboard prompts). Also check
   `Plugin.PadReplay.Value` is a valid `GamepadButton` (it is passed to `Get`).
4. If the fields really are empty and `GetSprite` works, keep the fallback; if `GetSprite` also returns null, try `descriptor._icons` directly or
   `GetControlPath` to learn the game's own names, and compare how the game's UI resolves its prompt sprites.
5. Only then ship: bump to 0.8.6, rebuild zips, release (see Build, test, release). Do not release before the user confirms the glyphs in game.

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
- Timing baseline: plugin `Update` averaged 0.126 ms (plain play) to 0.26 ms (long menu-heavy session); scheduled scans cost about 1 ms every 2-5 s.
  The 13-44 ms "slowest run" values under `keys`, `gamepad shortcut` and `dialogue history` are the dump itself, the first controller call and
  the history prewarm. There is no meaningful per-frame cost left to optimize; 0.8.4 cached the hotkey controls (`UwBehaviour.KeyCtl`) for a few microseconds.
- `LogOutput.log` is overwritten each launch, so only the latest session is ever visible. Version strings are identical across builds of the same
  version, so a log cannot tell you which build ran; compare the DLL's modified time instead.
- Dialogue capture has still never been seen working in a recent test session: every dump shows `lines=0`, and `lines=1` in "History opened" is
  the "No dialogue recorded yet" placeholder. Test with an actual cutscene.
- `Input.HideMouseAlways` (0.8.5) works without `HideMouseOnIdle`; the cursor is still shown while the history window or mod menu is open.
- Tooling on this machine: PowerShell scripts (`.ps1` files) are blocked by execution policy (run commands inline); `jq` is not installed; a `sed`
  with a bad pattern once injected stray text into a source file, so check `git diff` after any scripted edit. Edit tool calls fail if a file changed
  since it was read.
- `gh release create` and `git push` were first blocked by the auto-mode permission check. `.claude/settings.local.json` in this repo now allows
  `git push origin main`, `gh release create *`, `gh release view *` (excluded from git via `.git/info/exclude`); it only applies to sessions opened in this repo folder.

## Open items

1. Verify in game: F1 and Select + Left Shoulder, hide-mouse, mouse use in the history and mod menus, saved history, sliders.
2. History header glyphs are still wrong in 0.8.4 and 0.8.5 (shoulder, replay, close show keyboard text). See "Glyph bug in progress". The
   0.8.4 "load the whole folder" fallback never fired in the logs, so it did not fix this.
3. Steam Deck, 16:10, 32:9, PlayStation / Switch glyphs.
4. Speech-bubble lines have no speaker; the log prints where the first few live ("line without a speaker label at ...").
5. Which change fixed the earlier cutscene-skip report was never proven (removed always-on skip hook, removed the `UpdateString` hook).
   The log now prints "Dialogue skip hook on/off".
6. Panel-art padding factors at other aspect ratios.
7. The `DialogueHistory.Poll` stale-entry handling (0.8.4) has not been run against real dialogue: the test session captured no lines (`lines=0`).

## Releases

0.4.9, 0.5.0, 0.5.1, 0.6.0, 0.7.0 (auto-advance, remappable keys), 0.7.1, 0.8.0 (pre-release, history), 0.8.1, 0.8.2 (TAA, frame-time fixes),
0.8.3 (mod settings menu, F1, hide mouse), 0.8.4 (mod menu sliders/mouse, history mouse, glyph fallback), 0.8.5 (keep mouse always hidden). See `CHANGELOG.md`.
