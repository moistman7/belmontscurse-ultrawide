#!/usr/bin/env bash
# Belmont's Curse Ultrawide - Steam Deck / Linux (Proton) installer.
# Installs BepInEx 6 (be.788, Windows x64 IL2CPP) and the plugin into the game folder.
# Usage: ./install.sh [path-to-game-folder]     (run from Desktop Mode, in a terminal)
set -euo pipefail

GAME_EXE="Castlevania Belmont's Curse.exe"
GAME_DIR_NAME="Castlevania Belmont's Curse"
BEPINEX_URL='https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788%2B5b766a3.zip'
REPO='moistman7/belmontscurse-ultrawide'
LAUNCH_OPT='WINEDLLOVERRIDES="winhttp=n,b" %command%'
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

die() { echo "ERROR: $*" >&2; exit 1; }
need() { command -v "$1" >/dev/null 2>&1 || die "'$1' is required but not installed."; }
need curl; need unzip

find_game() {
    local roots=("$HOME/.local/share/Steam" "$HOME/.steam/steam" "$HOME/.var/app/com.valvesoftware.Steam/.local/share/Steam")
    local libs=() r vdf p
    for r in "${roots[@]}"; do
        [ -d "$r/steamapps" ] || continue
        libs+=("$r")
        vdf="$r/steamapps/libraryfolders.vdf"
        if [ -f "$vdf" ]; then
            # extra libraries (e.g. the SD card) are listed as "path" "/run/media/..."
            while IFS= read -r p; do libs+=("$p"); done < <(sed -n 's/^[[:space:]]*"path"[[:space:]]*"\(.*\)"/\1/p' "$vdf")
        fi
    done
    for r in "${libs[@]}"; do
        if [ -f "$r/steamapps/common/$GAME_DIR_NAME/$GAME_EXE" ]; then
            echo "$r/steamapps/common/$GAME_DIR_NAME"; return 0
        fi
    done
    return 1
}

GAME="${1:-}"
if [ -z "$GAME" ]; then
    GAME="$(find_game)" || die "Could not find the game. Install it in Steam first, or pass the game folder: ./install.sh \"/path/to/$GAME_DIR_NAME\""
fi
[ -f "$GAME/$GAME_EXE" ] || die "'$GAME_EXE' not found in: $GAME"
echo "Game folder: $GAME"

TMP="$(mktemp -d)"; trap 'rm -rf "$TMP"' EXIT

# 1. BepInEx (the Windows build; Proton runs the game as a Windows program)
if [ -f "$GAME/winhttp.dll" ] && [ -d "$GAME/BepInEx/core" ]; then
    echo "BepInEx already installed, skipping."
else
    echo "Downloading BepInEx 6 be.788 ..."
    curl -fL --progress-bar -o "$TMP/bepinex.zip" "$BEPINEX_URL"
    unzip -qo "$TMP/bepinex.zip" -d "$GAME"
fi
mkdir -p "$GAME/BepInEx/plugins"

# 2. The plugin: use the DLL shipped next to this script, otherwise fetch the latest release
DLL="$(find "$HERE" "$HERE/.." -maxdepth 4 -name BelmontsCurseUltrawide.dll 2>/dev/null | head -n1 || true)"
if [ -z "$DLL" ]; then
    echo "Fetching the latest plugin release ..."
    ZIP_URL="$(curl -fsSL "https://api.github.com/repos/$REPO/releases/latest" | sed -n 's/.*"browser_download_url": *"\([^"]*\.zip\)".*/\1/p' | head -n1)"
    [ -n "$ZIP_URL" ] || die "Could not find a release zip. Download it from https://github.com/$REPO/releases and run this script from the extracted folder."
    curl -fL --progress-bar -o "$TMP/plugin.zip" "$ZIP_URL"
    unzip -qo "$TMP/plugin.zip" -d "$TMP/plugin"
    DLL="$(find "$TMP/plugin" -name BelmontsCurseUltrawide.dll | head -n1)"
    [ -n "$DLL" ] || die "BelmontsCurseUltrawide.dll not found in the release zip."
fi
cp -f "$DLL" "$GAME/BepInEx/plugins/BelmontsCurseUltrawide.dll"
echo "Plugin installed."

cat <<MSG

Almost done. One manual step (Steam can't be changed safely from a script):

  In Steam (Gaming or Desktop Mode):
    Castlevania: Belmont's Curse > Properties (gear icon) > General > Launch Options
    paste exactly:

        $LAUNCH_OPT

Then launch the game. The FIRST launch can sit on a black screen for several minutes while
BepInEx sets itself up under Proton. Don't close it. Later launches are quick.

Check it worked: $GAME/BepInEx/LogOutput.log should mention "Belmont's Curse Ultrawide".
To uninstall: delete BelmontsCurseUltrawide.dll from BepInEx/plugins and clear the launch option.
MSG
