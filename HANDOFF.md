# Belmont's Curse ultrawide plugin: handoff

Updated 2026-10-23. A BepInEx 6 (be.788) IL2CPP plugin for *Castlevania: Belmont's Curse* (Unity 2022.3.62f2, game 1.1.3) that makes the game
work on ultrawide and non-16:9 screens, plus quality-of-life features (dialogue history, voice replay, auto-advance, mod settings menu).
Repo: https://github.com/moistman7/belmontscurse-ultrawide. Separate from the Spirit of the Samurai plugin.

## Current state

- **0.8.17 camera timing prerelease (2026-10-23):** CameraTimingHooks.cs adds Harmony prefixes on CinemachineBrain.UpdateVirtualCameras/ProcessActiveCamera (logic aspect before solving) and UniversalRenderPipeline.InitializeCameraData (render aspect and projection reasserted before URP snapshots camera data, only inside an active begin/end scope). The begin-camera callback always builds the projection itself so a hook that applies but never fires cannot leave a stale 16:9 matrix. UwBehaviour tracks the brain's OutputCamera. SaveRoomMasks filters its renderer snapshot incrementally (128 entries/step, best-effort 0.25 ms; snapshot call still synchronous) and queues mid-scan refreshes. FrameIntervals keeps session-wide worst frames. F9 adds hook counters, matrices, time values and focus-return details. Candidate only: hooks NOT observed running in game, reporter's cause unconfirmed (their only log is 0.8.12; no 0.8.13+ log), no measured frametime gain. Do not force Time.timeScale. 37 pure tests pass with fake Unity types. DLL SHA-256 BE22C12C2CB704338C34FDF3515B780DAC0BE6459DC30DEC71C20B570901D909. Packages/checksums/notes: chat outputs/release-0.8.17. Private 0.8.14-0.8.16 candidates were never public; 0.8.16 DLL differs from this build. Published GitHub prerelease v0.8.17 (tag/source f889413, not marked latest); all four freshly downloaded assets checksum-matched, ZIPs extracted (7 plain / 234 AllInOne files) and DLL hash matched. Nexus NOT updated. Follow-up needed: a reporter log from a 0.8.17 run (startup line 'solver hooks=2/2 final render hook=True', F9 dump counters) to confirm the hooks fire and whether snapping/Alt-Tab black sides persist.

- **0.8.13 camera recovery test (2026-10-23):** stable active-camera selection; releases invalid cameras and rejects disabled brains. Repairs tracked gameplay pillarbox viewport each projection pass, retaining menu/loading ownership exclusions. Builds final lens projection in beginCameraRendering; LateUpdate remains only when render-hook setup fails. Reuses computed aspect/matrix for render-time aspect swap. Focus return clears interrupted swap state and refreshes jobs. Full viewport skips target-texture/ownership queries. Candidate only: reporter's 0.8.12 log includes 5120x1440 dumps with full viewport, tracked camera, active aspect limit and no plugin errors. Root cause and Alt-Tab recovery unconfirmed. Build clean, 34 pure tests pass; no runtime frametime measurement. Test DLL SHA-256 84DC61E22FEE21EDF7EAF0A6224420115C4F77081D5238DE310518E60571D4ED. Published GitHub prerelease v0.8.13 (tag/source 147aa0a); all four freshly downloaded assets hash-matched, ZIPs extracted and Windows ZIP visibility verified. Nexus Main/Primary file 375 and Optional Full Install 376 published at 0.8.13; prior 0.8.12 files archived. Plugin virus scan cleared; Full Install scan pending at public verification. Nexus descriptions/changelog and public summary label this a test. Packages/checksums/evidence are in this chat outputs/release-0.8.13. Follow-up user report says BOTH snapping and Alt-Tab black sides persist; no affected 0.8.13 log received yet. Do not describe this candidate as resolving the report. Do not claim a confirmed fix or measured gain.

- **0.8.12 ZIP repair (2026-10-21):** fresh original GitHub downloads showed zero entries in Windows Shell ZIP namespace despite .NET/tar extracting successfully. Original archives contained a `./` root entry. Rebuilt all four GitHub/Nexus ZIPs with `System.IO.Compression.ZipFile.CreateFromDirectory` (no containing directory); Windows ZIP handler lists all repaired archives. Replaced both GitHub ZIPs/checksums and updated release notes; fresh downloads extracted and every file hash matches staging (7 plain / 234 AllInOne files). Nexus replacement files 328 (Main/Primary) and 329 (Optional) published at 0.8.12, prior broken files archived; both virus scans pending when verified. Nexus packages omit scripts and keep manual Proton README. Plugin DLL unchanged. Repaired Nexus packages/checksums and screenshot: chat `outputs/nexus-0.8.12-repaired/`. GitHub evidence: ignored `dist/github-repair-0.8.12/`.

- **Ending credits candidate fix (2026-10-21):** user reports main-menu credits fixed but ending credits still blue at 5120x1440. Added `EndingCreditsPresentation.cs`: tracks standalone `CreditsViewManager` through a Start postfix and 2-second scan, draws existing `ScreenSideBars` at centered 16:9 while active (including final Continue screen), refreshes visibility per frame and adds F9 diagnostics. Does not depend on the default-blue SolidColor camera fix. Release build clean; 34 existing logic tests pass, which do not exercise the new Unity/Harmony path. Visual verification pending. Released as v0.8.12 with side-bar visibility caching and inactive-loading screen-query avoidance; no measured frametime gain. Both GitHub archives extracted and DLL hash-verified; installed DLL SHA-256 `9F1F493817DEDAFE23514C9D3682A60A74207DEEACF1D9BB5A3FDDC33D6AD404`. Nexus also updated to 0.8.12 on 2026-10-21: plugin Main/Primary file 311 and Optional Full Install file 312, both prior versions archived. Public versions/descriptions verified; plugin scan cleared, Full Install scan pending at completion. Nexus archives omit scripts and include manual Proton README; extracted DLL hashes match release. Packages/checksums and publication screenshot are in the chat outputs/nexus-0.8.12 folder.

- **v0.8.11 Nexus publication (2026-10-20):** updated both existing files in place via the Edit > Files "Update" flow (Edge + Claude extension), archiving the 0.8.10 files and syncing the mod version to 0.8.11. Main/Primary plugin = `dist/BelmontsCurseUltrawide-v0.8.11-nexus.zip`; Optional Full Install = `dist/BelmontsCurseUltrawide-AllInOne-v0.8.11-nexus.zip`. Both repacked without `steamdeck/` (no .sh/.desktop/.awk), README uses manual Proton steps; DLL hash in both extracted ZIPs matches the build. Public page shows 0.8.11 for both; virus scanning was in progress at completion. The extension's file_upload is capped at 10 MB, so the user picked the 34 MB AllInOne ZIP in the browser dialog.
- **v0.8.11 GitHub release (2026-10-20):** version strings bumped; contains the default-blue fix below plus all 0.8.10 changes (first GitHub release since 0.8.9). Plain + AllInOne ZIPs built from `dist/plain-0.8.11` / `dist/aio-0.8.11` (steamdeck/ included; GitHub only), extracted and hash-verified. DLL SHA-256 `3F20C4E93CCB70883358C933B29CB173F6A0839EC879A24BE46A27438705BF97`, also deployed and hash-verified in the game. Not yet published to Nexus (Nexus packages must omit steamdeck/). Post-credits fix still not visually verified.
- **Default-blue camera background (2026-10-20):** user screenshot of the post-credits "Castlevania Belmont's Curse / Credits end" screen showed Unity's default camera background (49, 77, 121) as blue side bars. `MiscJob` now calls `BlackenDefaultBackground` for every polled camera whenever the plugin is on (independent of `FixCameraRect`): SolidColor cameras without a render texture whose background matches that default within 0.004 get black, preserving alpha. Not restored on disable. Polled once per second, so a brief blue flash on scene entry is possible. Release build clean; 34 logic tests pass. Deployed to D:\SteamLibrary install, DLL SHA-256 `5DC1B0C0EE505E86CDA0760BEDFDAAA1830D65007428F6225FB466A448121923` (hash-verified). Not visually verified in game; not on Nexus.

- **Nexus packaging correction (2026-10-19):** first uploads 255/256 were quarantined. User identified the past Steam Deck installer scan issue and requested removal. Repacked BOTH Nexus ZIPs without `steamdeck/`, `.desktop`, `.sh` or `.awk` files; plugin SHA-256 unchanged. Verified every archive entry and extracted DLL hash. Published replacements 257 (Main/Primary plugin) and 258 (Optional Full Install), archived 255/256 without deleting them. New uploads were still scanning when checked. Public page and package READMEs now provide manual Proton install instructions and explicitly state that Nexus packages omit installer scripts. Removed one-click installer/GitHub installer reference and corrected stale version check. Do not claim the scanner has approved these files or that scripts were the confirmed scanner trigger. Future Nexus packages MUST omit Steam Deck scripts/launchers; GitHub packaging may retain them. Final archives use `*-v0.8.10-nexus.zip` in chat outputs/nexus-0.8.10 with SHA-256 files. Public page screenshot: Nexus-manual-install.png.


- **Nexus publication (2026-10-19):** published plugin-only and Full Install version 0.8.10 to https://www.nexusmods.com/castlevaniabelmontscurse/mods/21?tab=files (file IDs 255 and 256). Updated existing files, archived their predecessors, retained Main/Primary and Optional categories, and set listing version to 0.8.10. Both public file entries verified; virus scanning was pending at completion. Descriptions/changelogs explicitly call this a test build with candidate Escape/loading fixes, no measured frametime claim, and pending Unity/Harmony/scene verification. Archives contain DLL SHA-256 `8786F4252BDAA283FFCF0E4AB1F9004280A49E5380DABCFD2CFF323F87F79016`, extracted and verified from each ZIP. This DLL was installed and hash-verified after the user closed the game in the preceding turn. Packages, checksums, notes and publication screenshot are under the chat outputs/nexus-0.8.10 folder; staging is dist/nexus-plain-0.8.10 and dist/nexus-aio-0.8.10. No GitHub release was created. No translation feature has been implemented.


- **Background frametime review / Git publication (2026-10-19):** reduced repeated native Unity calls without changing polling intervals: auto-advance reader validity is reused within each Tick; narrow-screen world-canvas transforms are cached in value-type entries during scanning and each camera reference is read once per pass; loading-camera refresh skips screen reads before cameras are discovered and checks activity before querying render targets. No runtime timing capture or measured improvement. Release build with SkipDeploy passes cleanly; 34 logic tests pass. Latest optimized DLL SHA-256: `8786F4252BDAA283FFCF0E4AB1F9004280A49E5380DABCFD2CFF323F87F79016`. Deployment attempt was blocked by the running game locking its DLL; the previous installed test build remains. User requested background operation and publication to Git; no game launch, exit, or desktop input was performed. Version 0.8.10 remains an unreleased test build, with Unity/Harmony and reported scenes still unverified.


- **Escape blank-tab candidate fix (same local 0.8.10 build):** `ModMenu.Init` installs a prefix on `InputActionsToUnityEvent.OnInputPerformed` to isolate game callbacks while the panel is open or waiting to restore input. `ModMenu.Tick` now handles Escape as Back. EventSystem and its previous active selection are restored only after closing controls have been released for a full frame. Release build has zero warnings/errors, 34 existing logic tests pass, installed DLL SHA-256 matches `D37D463C9EDF99B1E69144AFE1DCF83D03A64D40193854737AEC9A946F2515EB`. No live game validation; user asked to use their PC, so all work stayed in background files/builds with no game launch or desktop input. Verify Graphics tab > Mod settings > Escape, held Escape, Backspace/controller Back, Close button, and F1 open/close when available. These unit tests do not exercise menu input or Harmony.


- **Local 0.8.10 test build (2026-10-19; unreleased):** added `LoadingScreenPresentation.cs`, which tracks only cameras referenced by `LoadingScreenCameraManager`, applies a centered 16:9 viewport and separate opaque overlay bars while active on ultrawide, exempts these cameras from `MiscJob`, and restores original rects on deactivation/disable/aspect changes. Harmony enable/disable postfixes update immediately; scan/per-frame fallback remains. `MainMenuBars.cs` now holds reusable instance-based `ScreenSideBars`; menu behavior is preserved. F9 includes loading camera/artwork data, save-room mask bounds and black-named renderers. 34 pure tests pass; release build passes. No runtime/visual verification of new Unity or Harmony paths. Post-ending loading-art/blue-side report is distinct from the previously recorded final-boss cinematic-bar report. Other-room edge exposure remains unresolved without scene evidence. Existing 0.8.8 room/menu changes were retained. Do not claim all reported bugs fixed or publish as verified.


- **v0.8.9 frametime review (2026-10-19):** camera polling now reuses an IL2CPP camera array via `GetAllCameras` in the 0.1-second render-camera job and 1-second misc job, growing only when necessary and clearing stale tail references. Auto-advance fallback reader lookup uses unsorted `FindObjectsByType` instead of sorted `FindObjectsOfType`; inactive readers remain excluded. Polling intervals and main-menu viewport exemption are preserved. Release build clean, 28 existing logic tests pass (these do not exercise Unity camera APIs). No new runtime timing capture; no measured frametime improvement claimed. Previous capture: plugin Update average 0.301 ms; scan jobs can peak around 3 ms. The 283 ms frame cannot be attributed to the mod from this data.

- **v0.8.8**: save-room opaque side-mask removal, centered 16:9 main-menu artwork with pure-black side bars, and F9 feature-state diagnostics. Plain + AllInOne ZIPs and SHA-256 files are the release assets.
- User visually confirmed the save room and main menu on Windows at 3440x1440. New changes are unverified at 32:9 and on Steam Deck.
- New files: `SaveRoomMasks.cs` caches exact-name, parent and material matches; disables only the black save-room mesh, restores owned changes, and excludes the active main menu. `MainMenuBars.cs` draws two non-interactive opaque UI bars above the game's tint but below mod windows/toasts.
- `UwBehaviour.MainMenuViewport` limits only live camera `CamMainMenu` to a centered 16:9 viewport. Existing menu UI scaling and gameplay camera aspect logic remain intact. It restores the original viewport on leaving the menu. `MiscJob` must not expand this viewport.
- Earlier attempts to exempt the main-menu camera and StartMenuView from all existing fixes worsened the menu; those changes were reverted. Do not repeat that broad exclusion.
- Frametime review before release: latest gameplay/game-menu sample n=2048, average 6.377 ms, p99 10.074 ms, max 283.348 ms; plugin Update average 0.301 ms. A 21.2 ms dialogue-history maximum may include startup prewarming; no repeatable cause established. These are Unity Update intervals, not GPU timings. No measured performance gain is claimed.
- Small optimizations: cache the main camera's CinemachineBrain, write the menu viewport only when changed, and avoid redundant mask visibility writes. Release build clean; 28 tests pass. Local validation captures are in ignored `dist/validation-0.8.8`.
- The 0.8.7 camera follow fix still needs 32:9 verification; preserve the guidance below.

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
- Nexus uploads: omit the entire `steamdeck/` folder and its scripts/desktop launchers from BOTH packages; use manual Proton instructions in the Nexus README. Prior Nexus uploads containing installers were quarantined; approval of repacks is not guaranteed.
- GitHub zips: plain = `README.txt`, `BepInEx/plugins/*.dll`, `steamdeck/`; AllInOne also has `.doorstop_version`, `doorstop_config.ini`, `winhttp.dll`,
  `BepInEx/core`, `dotnet/` (copy the previous `dist/aio-<ver>` folder, replace the DLL and README). Build ZIPs with
  `System.IO.Compression.ZipFile.CreateFromDirectory(stageAbsolutePath, zipAbsolutePath, CompressionLevel.Optimal, false)`.
  Do not archive `.` with tar: its root entry can make Windows Explorer display an empty ZIP. Validate conventional paths (no `./` root),
  extract and compare file hashes, and verify Windows Shell ZIP namespace lists expected root contents. Repeat on fresh published downloads.
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

0. **Bad-ending final-boss in-game cutscene bars (reported 2026-10-19).** Reporter suspects left/right bars; user cannot replay because they have not reached that scene. No affected-scene dump is available. The v0.8.9 DLL also scans `CutsceneLetterBoxing._uiView` (the view can be outside the controller hierarchy), deduplicates graphics, and includes per-graphic letterbox diagnostics in F9. Release build/deploy succeeded and installed DLL hash matches. This is a candidate coverage fix, NOT a confirmed fix for this report. Existing `MiscJob` already expands full-height camera viewports, but excludes render textures and the owned main-menu viewport. Do not remove arbitrary black graphics/meshes without scene evidence. Need a screenshot or F9 dump from someone who can reach the cutscene to distinguish a viewport, UI mask, scene geometry, or video.

1. **Verify the 0.8.7 camera fix on 32:9** (pool secret room and other event cameras). If it still stops, use the F9 `physical=` / `tracked=` data (see above).
2. **Real dialogue capture and voice replay.** Every inspected dump had `lines=0`; `lines=1` after "History opened" is the placeholder. Trigger a real cutscene,
   check speaker/text/portrait capture and replay, then F9. This also covers the `DialogueHistory.Poll` stale-entry handling (0.8.4) and saved history.
3. Controller disconnect/reconnect and binding changes after the 0.8.6 caching; confirm Select + Left Shoulder opens mod settings.
4. Steam Deck hardware, 16:10, PlayStation/Switch glyphs, panel padding at other aspects. Not every settings slider/mouse/reset path was exercised.
5. Speech-bubble entries may lack speakers (the log names the first few label paths). Which change fixed the earlier cutscene-skip report was never proven.

## Releases

0.4.9, 0.5.0, 0.5.1, 0.6.0, 0.7.0, 0.7.1, 0.8.0, 0.8.1, 0.8.2 (TAA), 0.8.3 (mod menu, F1), 0.8.4, 0.8.5 (hide mouse always),
0.8.6 (glyph fix, performance, frame-interval diagnostics), 0.8.7 (always-on camera aspect limit, physical-camera fix). See `CHANGELOG.md`.
