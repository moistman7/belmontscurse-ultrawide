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


# 3. Steam launch option (needed so Proton loads BepInEx). Done automatically when possible.
OPT_SET=0
set_launch_option() {
    local lib appid cfg cur
    lib="$(dirname "$(dirname "$(dirname "$GAME")")")"
    appid="$(grep -l "\"installdir\"[[:space:]]*\"$GAME_DIR_NAME\"" "$lib"/steamapps/appmanifest_*.acf 2>/dev/null | head -n1 | sed 's/.*appmanifest_\([0-9]*\)\.acf/\1/')"
    [ -n "$appid" ] || return 1
    for cfg in "$HOME"/.local/share/Steam/userdata/*/config/localconfig.vdf "$HOME"/.steam/steam/userdata/*/config/localconfig.vdf \
               "$HOME"/.var/app/com.valvesoftware.Steam/.local/share/Steam/userdata/*/config/localconfig.vdf; do
        [ -f "$cfg" ] && break
        cfg=""
    done
    [ -n "$cfg" ] || return 1

    if pgrep -x steam >/dev/null 2>&1 || pgrep -x steamwebhelper >/dev/null 2>&1; then
        echo
        echo "Steam has to be closed to change the game's launch option."
        read -r -p "Close Steam now? [Y/n] " ans || ans=n
        case "${ans:-Y}" in [nN]*) return 1 ;; esac
        steam -shutdown >/dev/null 2>&1 || pkill -x steam || true
        for _ in $(seq 1 30); do pgrep -x steam >/dev/null 2>&1 || break; sleep 1; done
        pgrep -x steam >/dev/null 2>&1 && return 1
        sleep 2
    fi

    cur="$(awk -v appid="$appid" -v query=1 -f "$HERE/set-launch-option.awk" "$cfg" 2>&1 >/dev/null || true)"
    case "$cur" in
        *winhttp=n*) OPT_SET=1; return 0 ;;                       # already set
        ?*) echo "The game already has launch options ($cur); not overwriting them."; return 1 ;;
    esac

    cp -f "$cfg" "$cfg.bak-belmonts-curse"
    if awk -v appid="$appid" -v opt="$LAUNCH_OPT" -f "$HERE/set-launch-option.awk" "$cfg" > "$TMP/localconfig.vdf" \
        && [ -s "$TMP/localconfig.vdf" ]; then
        cp -f "$TMP/localconfig.vdf" "$cfg"
        echo "Launch option set (backup: $cfg.bak-belmonts-curse)."
        OPT_SET=1
        return 0
    fi
    return 1
}
if [ -f "$HERE/set-launch-option.awk" ]; then
    set_launch_option || true
else
    echo "(set-launch-option.awk not found next to the script; setting the launch option manually.)"
fi

echo
echo "=============================================="
echo " Installed."
echo "=============================================="
if [ "$OPT_SET" = 1 ]; then
    echo "Steam launch option: set automatically. Start Steam and launch the game."
else
    cat <<MSG
One manual step left. In Steam:
  Castlevania: Belmont's Curse > Properties (gear icon) > General > Launch Options
  paste exactly:

      $LAUNCH_OPT
MSG
fi
cat <<MSG

The FIRST launch can sit on a black screen for several minutes while BepInEx sets itself up
under Proton. Don't close it. Later launches are quick.

Check it worked: $GAME/BepInEx/LogOutput.log should mention "Belmont's Curse Ultrawide".
To uninstall: delete BelmontsCurseUltrawide.dll from BepInEx/plugins and clear the launch option.
MSG
