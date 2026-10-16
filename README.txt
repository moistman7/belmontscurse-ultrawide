CASTLEVANIA: BELMONT'S CURSE - ULTRAWIDE & STEAM DECK FIX
==========================================================

An unofficial community mod. It contains no game files. Use at your own risk and keep a backup
of your saves.

WHAT IT DOES
------------
- Makes the game fill ultrawide monitors (21:9, 32:9 and wider) with no side bars.
- Keeps the camera framing rooms and bosses the way it does on 16:9.
- Scales menus and the HUD correctly, and removes cutscene black bars.
- Fixes menus that are zoomed in / cut off on 16:10 screens (the Steam Deck's own screen).
- Skips the startup logos.

Everything is automatic: no launch options on Windows, no settings to change.


STEAM DECK (and other Linux / Proton)
-------------------------------------
The Deck's own screen is 16:10. The mod fixes the zoomed-in menus there, and it also works when
you dock the Deck to an ultrawide monitor.

1. Switch to Desktop Mode (hold the power button > Switch to Desktop).
2. Extract this zip (right-click > Extract). Keep the folders together.
3. Open the "steamdeck" folder and double-click "Install on Steam Deck".
   (If the Deck asks, choose to run / Execute it.)
   It finds the game (including on an SD card), installs BepInEx and the mod, and sets the Steam
   launch option for you. If Steam is open, it asks to close it first. You need an internet
   connection for the first install.
4. Go back to Gaming Mode and start the game.

The FIRST launch can sit on a black screen for several minutes while BepInEx sets itself up.
Don't close the game; later launches are quick.

Updating: extract the new zip and run "Install on Steam Deck" again.

If the installer can't set the launch option (or you prefer to), do it by hand: in Steam open the
game's Properties > General > Launch Options and paste exactly:

    WINEDLLOVERRIDES="winhttp=n,b" %command%

Prefer a terminal? Run:  ./steamdeck/install.sh
If it can't find the game:  ./steamdeck/install.sh "/path/to/Castlevania Belmont's Curse"

Steam Deck notes:
- The F6 / F7 / F10 keys need a keyboard. Without one, change the settings file instead (see
  SETTINGS below).
- Check it worked: BepInEx/LogOutput.log in the game folder should contain
  "Belmont's Curse Ultrawide".
- To uninstall: delete BepInEx/plugins/BelmontsCurseUltrawide.dll and clear the game's launch
  option in Steam.


WINDOWS
-------
1. Close the game. Install BepInEx 6 bleeding edge, build be.788, "Unity IL2CPP x64" (NOT
   BepInEx 5) into the game folder (where "Castlevania Belmont's Curse.exe" is):
   https://builds.bepinex.dev/projects/bepinex_be
   Run the game once; the first launch can sit on a black screen for a few minutes.
2. Extract this zip into the game folder (the BepInEx/plugins folder gets the mod DLL). You can
   ignore the "steamdeck" folder on Windows.
3. Launch the game. BepInEx/LogOutput.log should contain "Belmont's Curse Ultrawide 0.5.1 loaded".

To uninstall: delete BepInEx/plugins/BelmontsCurseUltrawide.dll.


KEYS (keyboard)
---------------
F10   HUD in the centered 16:9 area, or at the screen edges (remembered)
F6    Bigger HUD on / off (remembered)
F7    Write a diagnostic dump to BepInEx/BelmontsCurseUltrawide_dump.txt


SETTINGS
--------
BepInEx/config/local.belmontscurse.ultrawide.cfg is created on the first launch. Edit it with the
game closed. The defaults are meant to just work. The useful ones:

  UI.CenterHUD            true = HUD in the centered 16:9 area (same as F10)
  UI.BigHUD               true = bigger HUD (same as F6)
  UI.HUDScale             how much bigger BigHUD makes it (1.1 to 1.6 is sensible)
  UI.FixNarrowScreens     fixes menus on 16:10 screens such as the Steam Deck (default true)
  General.SkipIntros      false keeps the startup logos
  General.StartupFpsCap   60 by default; still getting a black screen at launch? Try 30
  Resolution.Width/Height 0 = your screen's native resolution


TROUBLESHOOTING
---------------
- Black screen on the very first launch: wait a few minutes (BepInEx setup).
- Occasional black screen at launch afterwards: set General.StartupFpsCap = 30.
- Nothing changed in game: check that "winhttp.dll" is next to the game's .exe and that
  BepInEx/LogOutput.log exists and mentions "Belmont's Curse Ultrawide". On Steam Deck also
  check the launch option above is set.
- On 32:9 you may see a soft lit circle with darkness around it at the screen sides, outside the
  room you're in. That's the game's own room-edge darkening; the mod leaves it as the game draws it.
- Settings got messed up: close the game and delete local.belmontscurse.ultrawide.cfg. A fresh
  one with the defaults is created on the next launch.


REPORTING A PROBLEM
-------------------
Press F7 in game while the problem is on screen, then send BepInEx/BelmontsCurseUltrawide_dump.txt,
BepInEx/LogOutput.log and a screenshot, and say which resolution / device you use. These files
stay on your machine; nothing is transmitted.

  https://github.com/moistman7/belmontscurse-ultrawide

Licence: MIT.
