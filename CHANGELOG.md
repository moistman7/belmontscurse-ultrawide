# Changelog

## 0.8.21 (camera timing update)

- A reporter's dump from a busy late-game room (5120x1440, 0.8.13) showed the scene scans costing 4-9 ms each every 2-5 s, with one save-room mask scan spike of 205 ms and 6 frames over 25 ms in 18 s. The save-room mask, loading-camera and ending-credits scans (the costliest; each is also driven by a game hook or by the scene-load kick) now run every 10 s instead of every 2 s once a scene has been stable for 15 s. For 15 s after any scene load, focus return or settings change they still run at the normal rate. If the game hook cannot be installed the scan keeps the 2 s rate. The other scans are unchanged.
- Expected effect: roughly 60% less scan cost in heavy scenes and rarer spikes. Not measured in that room and not confirmed to fix the reported hitching; a single scan can still take tens of milliseconds in a very heavy scene.
- Includes 0.8.20. Release build clean; 37 pure tests pass; played in gameplay on 3440x1440 without crashes.

## 0.8.20 (camera timing update)

- Measured on a 3440x1440 screen: auto-advance's idle cost fell from 0.095 ms to 0.024 ms per frame and the plugin's average Update from 0.159 ms to 0.090 ms. With the dialogue Begin hook installed, the scene-wide search for the active line reader is only a fallback, so it now runs every 0.5 s instead of 0.1 s. Picking up a line already in progress (for example, switching auto-advance on mid-dialogue) can take up to half a second longer.
- The F9 dump now lists every per-frame feature's average time, run count and total, sorted by total.
- Includes 0.8.19 (the fix for the startup crash in 0.8.17 and 0.8.18). Played in gameplay without crashes: solver hooks fire and the begin/end render callbacks pair exactly.
- Not verified: auto-advance firing after the interval change (a test run did not reach dialogue), the original Alt-Tab and snapping reports, and any frametime change beyond the plugin's own cost. Release build clean; 37 pure tests pass.

## 0.8.19 (camera timing prerelease)

- Fix a startup crash in 0.8.17 and 0.8.18: the Harmony patch on `UniversalRenderPipeline.InitializeCameraData` (a static method with an `out` struct parameter) made the game fault with an access violation about a second after the plugin loaded. The patch is removed; the begin-camera render callback builds and applies the final projection as it did before. Found by installing the build locally and bisecting: no camera hooks and the two Cinemachine solver hooks alone both run; the URP hook alone crashes. Do not use 0.8.17 or 0.8.18.
- Keep the two Cinemachine solver hooks (`UpdateVirtualCameras` and `ProcessActiveCamera`). They install and are invoked at the title screen; their gameplay behaviour is not yet tested.
- Includes the 0.8.18 changes (exact time scale restored when the mod menu closes, panels closed if their feature is switched off while open, hook exception safety, executable Steam Deck scripts).
- Candidate only. Verified: Release build clean, 37 pure tests pass, game launches and stays up at the title screen with the hooks installed. Not verified: gameplay, the original snapping and Alt-Tab reports, any frametime change.

## 0.8.18 (camera timing prerelease)

- Closing the mod menu (F1 / controller shortcut) now restores the exact time scale it found. Opened over the game's own pause (scale 0) it used to set the scale to 1 and unpause the game behind its pause menu.
- If the mod menu or dialogue history is switched off after repeated failures while open, close it and hand input back at once; their Tick, which normally does that, no longer runs. Previously game input stayed blocked and the time scale stayed at 0.
- Wrap the ending-credits start hook in a try/catch (as the loading hook already was) and move the pointer checks of the new camera hooks inside their try blocks, so an unexpected exception cannot propagate into the game.
- Steam Deck: the ZIPs now carry Unix permissions, so the install/uninstall scripts and launchers are executable after extraction (`./steamdeck/install.sh` works, and the desktop launchers need no manual trust step).
- Includes all 0.8.17 changes. Still a candidate only: the hooks have not been observed in game, the cause of the reported snapping and Alt-Tab black sides is not confirmed, and no frametime gain is claimed. Release build clean; 37 pure logic tests pass (they do not exercise Unity, Harmony or the menus).

## 0.8.17 (camera timing prerelease)

- Add Harmony prefixes on `CinemachineBrain.UpdateVirtualCameras` and `ProcessActiveCamera` that apply the configured logic aspect immediately before camera solving, and on `UniversalRenderPipeline.InitializeCameraData` that reasserts the render aspect and ultrawide projection just before URP snapshots camera data. Hooks install independently and log a warning if one is unavailable; the begin/end camera-render callbacks remain as the fallback and always build the projection themselves. The final hook only acts inside an active begin/end scope.
- Track the camera actually output by the Cinemachine brain (including a controlled-object override) and keep the selected camera while it stays valid.
- Cache the parsed logic-aspect setting and avoid scans or allocations in the normal solver path. Do not re-enable the aspect limit after the camera-picture feature has been switched off by repeated failures.
- Filter the save-room mask scan incrementally (up to 128 renderers per step with a best-effort 0.25 ms check) instead of in one pass. The object snapshot itself is still synchronous. Existing masks stay active until a complete new set is published; refresh requests that arrive mid-scan are queued, not dropped.
- Keep the worst frame and count of frames over 25 ms per mode for the whole session, and add timing hook counters, projection/culling matrices, pixel sizes, time-scale values and frame-rate settings to the F9 dump. Focus-return log lines now include screen mode, display size and whether the aspect limit is active.
- Candidate only: the cause of the reported snapping, black sides after Alt-Tab and slowdown is not confirmed, and the hooks have not been observed running in the game. No measured frametime improvement is claimed. Release build clean; 37 pure logic tests pass (they use fake Unity types and do not exercise Unity, Harmony or rendering).
## 0.8.13 (camera recovery test)

- Keep a valid active gameplay camera instead of periodically choosing the first brain in an unordered scan. Release inactive cameras and reject disabled brains.
- Repair a narrowed full-height gameplay viewport each frame and rebuild the ultrawide projection immediately before rendering from the final camera lens. Preserve the menu/loading viewport exclusions and the logic/render aspect swap.
- Refresh scheduled fixes and discard interrupted render-swap state when focus returns after Alt-Tab.
- Avoid the duplicate LateUpdate projection rebuild when render callbacks are installed, reuse computed projection values for the aspect swap, and skip unnecessary render-target/ownership checks for an already full viewport. LateUpdate remains the fallback if render-hook setup fails.
- Candidate fix for reported camera snapping and loss of ultrawide presentation after Alt-Tab at 5120x1440. The supplied 0.8.12 dumps showed full resolution, a full viewport, an active aspect limit and no plugin errors; the cause is not confirmed. In-game verification and comparative frametime measurements are pending. No measured performance gain is claimed.
- Release build clean; all 34 existing pure logic tests pass. These tests do not exercise Unity rendering, focus changes or camera selection.

## 0.8.12

- Add opaque centered-16:9 side bars while the standalone `CreditsViewManager` is active, including the final Continue artwork. This covers the reported ending-credits blue sides independently of camera clear flags. Start hook plus polling fallback; restores visibility on exit, mod disable, and aspect changes. In-game verification pending.
- Cache owned side-bar visibility to avoid repeated native active-state reads; skip screen-size queries when all tracked loading cameras are inactive. Polling intervals unchanged. No measured frametime gain claimed.
- Release build clean; 34 existing logic tests pass. These tests do not exercise Unity/Harmony presentation paths.

## 0.8.11

- Turn Unity's untouched default camera background (49, 77, 121) black on solid-color screen cameras. On ultrawide this color showed as blue side bars wherever a narrower viewport left the screen uncovered, as reported on the post-credits "Credits end" screen. Checked in the once-per-second camera job whenever the mod is on, so a brief blue flash on scene entry is possible; not restored when the mod is disabled.
- First GitHub release to include the 0.8.10 changes below.
- Release build clean; 34 logic tests pass. The post-credits screen fix is not yet visually verified in game.

## 0.8.10 (test build)

- Reduce repeated Unity API calls in per-frame paths: reuse the current auto-advance reader's validity result, cache narrow-screen world-canvas transforms during scanning, read each canvas camera once per pass, and skip loading render-target queries for inactive cameras. No polling intervals changed and no measured frametime gain is claimed.
- Handle Escape inside the mod settings panel and suppress the game's separate InputActionsToUnityEvent callbacks while it is open. Restore EventSystem input and the previous selection after the closing controls have been released for a full frame, preventing the closing press from also navigating the underlying Graphics tab. Addresses the reported blank tab after Escape; in-game verification remains pending.
- Keep cameras referenced by LoadingScreenCameraManager in a centered 16:9 viewport on ultrawide screens, exempt them from general viewport expansion, and draw opaque black side bars. Restore the original viewport when the camera becomes inactive, the mod is disabled, or the display is no longer ultrawide.
- Hook loading-camera enable/disable for immediate updates, with scene scanning and per-frame refresh as a fallback. Main-menu and loading-screen bars share the same drawing code with separate visibility state.
- Add F9 loading-camera/artwork details, save-room mask bounds, and black-named scene renderers to help identify remaining room-edge problems.
- Preserve the existing v0.8.8 save-room and main-menu fixes. The post-ending loading-art report is distinct from the final-boss cinematic-bar report; neither is visually verified by this change. Other rooms still need affected-scene evidence before a targeted masking fix can be chosen.
- 34 logic tests pass, including presentation geometry at 3440x1440, 5120x1440, 16:9 and narrower/invalid screen sizes. Tests do not exercise Unity or Harmony.

## 0.8.9

- Scan the cutscene controller's referenced UI view as well as its own hierarchy when removing cinematic bars. Deduplicate graphics shared by both paths.
- Add per-graphic cutscene-bar diagnostics to F9, including visibility, size, color and whether the mod disabled the graphic.
- Reuse the camera enumeration buffer in recurring camera jobs and remove unnecessary sorting from the auto-advance reader search. Polling intervals and the main-menu viewport exemption are unchanged.
- Release build passes with zero warnings/errors; all 28 existing logic tests pass. These tests do not exercise Unity camera APIs. No measured frametime improvement is claimed.
- The reported bad-ending final-boss cutscene bars (suspected left/right) remain unverified: no affected-scene screenshot or diagnostic dump is available. The cutscene change is a candidate fix for that report.

## 0.8.8

- Remove the opaque save-room side-mask meshes on ultrawide screens. Decorative columns and statues remain visible; masks are restored when the mod is disabled or the screen returns to 16:9.
- Preserve the side masks while the main menu is active, including when it reuses save-room scenery.
- Keep the existing main-menu UI layout and constrain CamMainMenu to a centered 16:9 viewport on ultrawide screens. Restore the previous viewport when returning to gameplay.
- Draw opaque black side bars over the main-menu overlay to keep the margins consistently black. The bars do not intercept input and disappear during gameplay.
- Cache the main camera's Cinemachine component and avoid redundant viewport and mask visibility writes. No measured performance improvement is claimed.
- F9 reports save-room mask counts, menu viewport and black-bar state. Visual changes confirmed on Windows at 3440x1440; other aspect ratios remain unverified for this release.

## 0.8.7

- The camera aspect limit is now always on (16:9 for the game's camera logic on wider screens) and the "Camera aspect limit" switch and `LimitCameraLogicAspect` setting are removed. `CameraLogicAspect` still controls the cap.
- The gameplay camera is now also found when the game switches it to physical camera properties; previously the limit could silently never apply on 32:9, so the camera stopped following the player.
- F9 lists `physical=` and `tracked=` for each Cinemachine brain to help diagnose camera issues.

## 0.8.6

- Fixed history controller glyphs disappearing after entering a level: retain the descriptor, sprites and textures, and reload invalid cached descriptors. Confirmed with an Xbox controller on Windows.
- Cached resolved glyphs, controller shortcut controls and history row transforms. History headers and mod settings footers skip unchanged UI work; missing sprites still retry. All features and UI remain available.
- F9 now reports recent Unity frame intervals (average, median, p95, p99, maximum and counts above 25/50 ms), separated into gameplay/game menus, history and mod settings. Focus gaps, mode transitions and diagnostic dumps are excluded; loading/startup remain included. These are Update intervals, not GPU presentation timings.
- Diagnostic dumps no longer count toward plugin Update or per-feature CPU timings. Added tests for the frame sampler (28 tests total).
- Tested at 3440x1440 on Windows: history and settings each averaged about 6.06 ms per frame, p99 below 6.86 ms. Real dialogue capture and voice replay remain unverified in this test session; Steam Deck, other aspect ratios and non-Xbox glyphs remain untested.

## 0.8.5

- **Keep mouse always hidden** (`Input.HideMouseAlways`, off by default; also in the mod menu): the cursor stays hidden instead of returning on movement; works on its own, without hide-on-idle. It still shows in the history window and the mod menu.

## 0.8.4

- **Controller button sprites in the history window** are repaired. 0.8.3 loaded only the matching glyph descriptor, which could leave the shoulder and face-button sprites empty so only the D-pad showed; it now falls back to loading the full glyph folder and logs when it has to.
- **Mod menu**: sliders (bigger-HUD size, HUD margins, auto-advance timing, mouse idle time), a two-step "Reset to defaults", and mouse support (hover, click, wheel).
- **History** can be scrolled and selected with the mouse; clicking the selected card replays its voice.
- **Optional history across sessions** (`Dialogue.SaveHistoryToDisk`, off by default): text and speaker names only, in `BepInEx/BelmontsCurseUltrawide_history.txt`.
- **Menu positions** the mod has recorded are re-read if the game moves them in the meantime, instead of being restored to a stale position.
- The log now says when the dialogue-skip hook is switched on or off, and where a line without a speaker label (a speech bubble) lives.
- Unit tests for the pure helpers (`tests/Logic.Tests`, `dotnet test`).
- Hotkey controls are looked up only when the keyboard or a key setting changes, and a dialogue-history entry whose label has been destroyed is dropped instead of raising an error on every poll.

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
