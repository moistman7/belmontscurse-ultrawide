# Belmont's Curse ultrawide plugin: handoff

Updated 2026-10-17. A BepInEx 6 (be.788) IL2CPP plugin for *Castlevania: Belmont's Curse* (Unity 2022.3.62f2, game 1.1.3) that makes the game
work on ultrawide and non-16:9 screens, plus quality-of-life features (dialogue history, voice replay, auto-advance, mod settings menu).
Repo: https://github.com/moistman7/belmontscurse-ultrawide. Separate from the Spirit of the Samurai plugin.

## Current state

- **v0.8.7 is published** and marked latest (plain + AllInOne zips, SHA-256 files):
  https://github.com/moistman7/belmontscurse-ultrawide/releases/tag/v0.8.7. Release commit `614b9c4` on `main`. Build clean, 28 tests pass.
- Tested on Windows, 3440x1440 (21:9), Xbox controller. 0.8.7 itself has **not been played** on 32:9.

### What 0.8.7 changed (camera)

- Reported bug: on 5120x1440 (32:9) the camera stopped following the player (seen in the pool secret room, live camera `GPE_CameraPoolSecretFound`).
  The F9 dump from that session had `cameraLimit=False` and the main camera at aspect 3.556, so the aspect spoof was not applied even though
  the config had it on. The session log was overwritten, so the exact cause was never seen.
- Likely cause (**unconfirmed**): `UwBehaviour.GameplayCamera()` skipped any Cinemachine-brain camera with `usePhysicalProperties`, so if an event
  camera switched the main camera to physical lens settings, no camera was tracked and the spoof never started. That exclusion (added in 0.4.9 with no
  recorded reason) was removed.
- The aspect limit is now **always on** (16:9 logic aspect on wider screens). The mod menu switch and the `LimitCameraLogicAspect` setting were removed;
  `CameraLogicAspect` (auto = 16:9, or a number) still sets the cap. Old configs keep a harmless orphan `LimitCameraLogicAspect` line.
- F9 now prints `physical=` and `tracked=` on each `CinemachineBrain` line. If the camera stops again: press F9 immediately, keep `LogOutput.log`
  and the dump before relaunching. `physical=True tracked=False` confirms the cause above; `tracked=True` with `cameraLimit=False` means look elsewhere.
- Observed oddity worth a look if it recurs: a log printed "Camera logic limited to 1.778 ... 'Main Camera'" twice in one session, meaning the tracked
  camera was swapped (`GameplayCamera` picks the first brain found, then re-searches every 5 s). If two brain cameras exist and the order changes, the
  spoof could flip between them; spoofing every qualifying brain camera would be the next step.

## Paths and environment

- Repo: `C:\Users\aids7\dev\belmontscurse-ultrawide`. Game: `D:\SteamLibrary\steamapps\common\Castlevania Belmont's Curse`.
- Plugin DLL: `<game>\BepInEx\plugins\BelmontsCurseUltrawide.dll`. Config: `<game>\BepInEx\config\local.belmontscurse.ultrawide.cfg`.
- Log: `<game>\BepInEx\LogOutput.log` (overwritten every launch). F9 dump: `<game>\BepInEx\BelmontsCurseUltrawide_dump.txt`.
  Optional saved history: `<game>\BepInEx\BelmontsCurseUltrawide_history.txt`.
- Release staging: `dist/plain-<ver>`, `dist/aio-<ver>`, notes `dist/notes-<ver>.md`. `dist/` is git-ignored. Shell: PowerShell. User timezone: America/Chicago.

## Build, test, release

- Build: `dotnet build -c Release -p:GameDir="D:\SteamLibrary\steamapps\common\Castlevania Belmont's Curse"`. It copies the DLL into the game's
  `plugins` folder, which **fails while the game runs**; use `-p:SkipDeploy=true` to build without deploying. Without `GameDir` the build looks in the
  default Steam path and fails. Compare SHA-256 of `bin/Release` and the installed DLL.
- Tests: `dotnet test tests/Logic.Tests` (28 cases: `Logic.cs` helpers and the frame-interval sampler; no game needed).
- Version strings: `Plugin.cs` (attribute + startup log), `UwBehaviour.cs` (dump header), `README.md`, `README.txt`, `CHANGELOG.md`.
- Zips: plain = `README.txt`, `BepInEx/plugins/*.dll`, `steamdeck/`; AllInOne also has `.doorstop_version`, `doorstop_config.ini`, `winhttp.dll`,
  `BepInEx/core`, `dotnet/` (copy the previous `dist/aio-<ver>` folder, replace the DLL and README). Use **Windows**
  `C:\Windows\System32\tar.exe -a -c -f out.zip *` from inside the staging folder (Git Bash tar writes broken zips). Extract and compare the DLL hash.
  Never package configs, logs, saved history or interop output. Checksum format `<sha256> *<file>`.
- Publish: commit/push, then `gh release create vX.Y.Z <4 assets> --target main --latest --title vX.Y.Z --notes-file dist/notes-X.Y.Z.md`; verify all four assets.
  `.claude/settings.local.json` (excluded via `.git/info/exclude`) allows `git push origin main` and `gh release create/view` in this repo folder.
- Tooling: `.ps1` scripts are blocked by execution policy (run inline); Python and `jq` are not installed; avoid sed/heredocs for C# containing backslashes
  and check `git diff` after scripted edits. Decompiler: `C:\Users\aids7\.dotnet\tools\ilspycmd.exe -t <Type> "<path>\Assembly-CSharp.dll"`.

## Code map

| File | Purpose |
|---|---|
| `Plugin.cs` | Entry point, every config entry, old-setting migrations, startup log. |
| `UwBehaviour.cs` | Main MonoBehaviour: job scheduler, camera (`GameplayCamera`, `CameraLogic`, `UpdateProjection`, render-time aspect swap), width-based zoom fix, confiners, menu/HUD anchoring, videos, letterbox, resolution, hotkeys, controller shortcuts, F9 dump, per-feature timing. |
| `FrameIntervals.cs` | Bounded frame-interval sampler (2048 samples per mode); pure .NET, linked into tests. |
| `DialogueHistory.cs` | Harmony hooks on the dialogue reader; records lines, names, portraits, voice; history input; optional disk save; conditional skip hook. |
| `HistoryView.cs` | History window (cards, scrolling, header, mouse hit-testing) and shared drawing helpers. |
| `ModMenu.cs`, `ModSettingsRow.cs` | Mod settings panel (toggles, sliders, reset, mouse); opened from the Graphics tab row or F1 / Select + Left Shoulder. |
| `GamepadGlyphs.cs` | Controller button sprites: descriptor/sprite retention, per-descriptor cache, diagnostics. |
| `GameUiFont.cs`, `GameUiSprites.cs` | Game font/material and panel lookups; portrait copy and margin trim. |
| `HistoryVoice.cs`, `VoiceAdvance.cs`, `Toast.cs`, `DialogueDim.cs`, `MouseHide.cs`, `AntiAliasing.cs` | Voice replay, auto-advance, messages, dim-bar widening, mouse hiding, TAA option. |
| `Logic.cs`, `tests/Logic.Tests` | Pure helpers and tests. `steamdeck/` holds the Steam Deck installer scripts. |

## Things learned

- Interop assemblies expose generated wrappers, not game code. Settings rows use Doozy UI components under
  `MainMenu/StartMenuView/SettingsView/TabsLayout_Settings/TabsContent_Group/TabContent_*`.
- **Glyph disappearance (fixed in 0.8.6):** the cached `GamepadDescriptor` became Unity-null after entering a level but was returned because the layout string
  matched. Fix: reuse a cached descriptor only if still valid, set `DontUnloadUnusedAsset` on descriptor/sprites/textures, cache per descriptor.
  Field reads, backing fields and `GetSprite` all agree; the earlier struct-offset theory was not supported.
- **Do not flip menu layout switches while menus exist.** `CenterMenus` / `FixNarrowScreens` are startup-only (`Plugin.MenusOn`, `NarrowOn`); live toggling mis-scaled the pause menu.
- **Do not hook `UILocalizedString.UpdateString`** (runs for every localized string, measurable cost). Dialogue text is read by polling the active labels.
- History is built once with placeholder text at launch (`HistoryView.Prewarm`) to avoid a ~30 ms hitch. The dump itself costs 30-60 ms; not a regression.
- Frame timing (3440x1440, 0.8.5 pre-release build): history and settings average ~6.06 ms, p99 < 6.86 ms; plugin Update ~0.16 ms. These are Unity Update
  intervals, not GPU timings. A 1.02 s gameplay/menu stall in one capture was never explained; do not claim a regression without a repeatable capture.
  Avoid speculative optimization.
- The history window's panel art has transparent padding; `HistoryView.Inflate` factors (0.35 x height sideways, 0.045 vertically) were estimated at 21:9.
- `LogOutput.log` is overwritten each launch; builds of one version have identical version strings, so compare the DLL's hash or modified time.
- The game's camera logic wants a 16:9 aspect; `CameraLogic` sets `camera.aspect` to the cap while the render callbacks swap the real aspect in for drawing
  (door light shafts and fullscreen quads need it). Width-based `CameraScreenSize` zooms are converted to height (`FixWidthBasedZoom`).

## Open items

1. **Verify the 0.8.7 camera fix on 32:9** (pool secret room and other event cameras). If it still stops, use the F9 `physical=` / `tracked=` data (see above).
2. **Real dialogue capture and voice replay.** Every inspected dump had `lines=0`; `lines=1` after "History opened" is the placeholder. Trigger a real cutscene,
   check speaker/text/portrait capture and replay, then F9. This also covers the `DialogueHistory.Poll` stale-entry handling (0.8.4) and saved history.
3. Controller disconnect/reconnect and binding changes after the 0.8.6 caching; confirm Select + Left Shoulder opens mod settings.
4. Steam Deck hardware, 16:10, PlayStation/Switch glyphs, panel padding at other aspects. Not every settings slider/mouse/reset path was exercised.
5. Speech-bubble entries may lack speakers (the log names the first few label paths). Which change fixed the earlier cutscene-skip report was never proven.

## Releases

0.4.9, 0.5.0, 0.5.1, 0.6.0, 0.7.0, 0.7.1, 0.8.0, 0.8.1, 0.8.2 (TAA), 0.8.3 (mod menu, F1), 0.8.4, 0.8.5 (hide mouse always),
0.8.6 (glyph fix, performance, frame-interval diagnostics), 0.8.7 (always-on camera aspect limit, physical-camera fix). See `CHANGELOG.md`.
