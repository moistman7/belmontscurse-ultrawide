# Changelog

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
