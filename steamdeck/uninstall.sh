#!/usr/bin/env bash
# Belmont's Curse Ultrawide - Steam Deck / Linux (Proton) uninstaller.
# Removes the plugin, and (if nothing else uses it) BepInEx and the Steam launch option.
# Usage: ./uninstall.sh [path-to-game-folder]
set -euo pipefail

GAME_EXE="Castlevania Belmont's Curse.exe"
GAME_DIR_NAME="Castlevania Belmont's Curse"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

die() { echo "ERROR: $*" >&2; exit 1; }

find_game() {
    local roots=("$HOME/.local/share/Steam" "$HOME/.steam/steam" "$HOME/.var/app/com.valvesoftware.Steam/.local/share/Steam")
    local libs=() r vdf p
    for r in "${roots[@]}"; do
        [ -d "$r/steamapps" ] || continue
        libs+=("$r")
        vdf="$r/steamapps/libraryfolders.vdf"
        if [ -f "$vdf" ]; then
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
    GAME="$(find_game)" || die "Could not find the game. Pass the game folder: ./uninstall.sh \"/path/to/$GAME_DIR_NAME\""
fi
[ -f "$GAME/$GAME_EXE" ] || die "'$GAME_EXE' not found in: $GAME"
echo "Game folder: $GAME"

# 1. The mod
rm -f "$GAME/BepInEx/plugins/BelmontsCurseUltrawide.dll" \
      "$GAME/BepInEx/config/local.belmontscurse.ultrawide.cfg" \
      "$GAME/BepInEx/BelmontsCurseUltrawide_dump.txt"
echo "Mod removed."

# 2. BepInEx, only if no other plugin needs it
OTHERS="$(find "$GAME/BepInEx/plugins" -type f \( -name '*.dll' -o -name '*.zip' \) 2>/dev/null | grep -v 'BelmontsCurseUltrawide' || true)"
REMOVED_BEPINEX=0
if [ -n "$OTHERS" ]; then
    echo "Keeping BepInEx and the Steam launch option: other plugins are installed."
else
    read -r -p "Also remove BepInEx and the Steam launch option? [Y/n] " ans || ans=n
    case "${ans:-Y}" in
        [nN]*) echo "Keeping BepInEx." ;;
        *)
            rm -rf "$GAME/BepInEx" "$GAME/dotnet" "$GAME/winhttp.dll" "$GAME/doorstop_config.ini" "$GAME/.doorstop_version"
            echo "BepInEx removed."
            REMOVED_BEPINEX=1
            ;;
    esac
fi

# 3. Steam launch option
clear_launch_option() {
    local lib appid cfg
    lib="$(dirname "$(dirname "$(dirname "$GAME")")")"
    appid="$(grep -l "\"installdir\"[[:space:]]*\"$GAME_DIR_NAME\"" "$lib"/steamapps/appmanifest_*.acf 2>/dev/null | head -n1 | sed 's/.*appmanifest_\([0-9]*\)\.acf/\1/')"
    [ -n "$appid" ] || return 1
    for cfg in "$HOME"/.local/share/Steam/userdata/*/config/localconfig.vdf "$HOME"/.steam/steam/userdata/*/config/localconfig.vdf \
               "$HOME"/.var/app/com.valvesoftware.Steam/.local/share/Steam/userdata/*/config/localconfig.vdf; do
        [ -f "$cfg" ] && break
        cfg=""
    done
    [ -n "$cfg" ] || return 1
    [ -f "$HERE/set-launch-option.awk" ] || return 1

    if pgrep -x steam >/dev/null 2>&1 || pgrep -x steamwebhelper >/dev/null 2>&1; then
        echo "Steam has to be closed to change the game's launch option."
        read -r -p "Close Steam now? [Y/n] " ans || ans=n
        case "${ans:-Y}" in [nN]*) return 1 ;; esac
        steam -shutdown >/dev/null 2>&1 || pkill -x steam || true
        for _ in $(seq 1 30); do pgrep -x steam >/dev/null 2>&1 || break; sleep 1; done
        pgrep -x steam >/dev/null 2>&1 && return 1
        sleep 2
    fi
    local tmp; tmp="$(mktemp)"
    # Keep the first backup (the config from before this mod touched it); do not overwrite it.
    [ -e "$cfg.bak-belmonts-curse" ] || cp -f "$cfg" "$cfg.bak-belmonts-curse"
    if awk -v appid="$appid" -v remove=1 -f "$HERE/set-launch-option.awk" "$cfg" > "$tmp" && [ -s "$tmp" ]; then
        cp -f "$tmp" "$cfg"; rm -f "$tmp"
        echo "Launch option cleared (backup: $cfg.bak-belmonts-curse)."
        return 0
    fi
    rm -f "$tmp"; return 1
}
if [ "$REMOVED_BEPINEX" = 1 ]; then
    clear_launch_option || echo "Could not clear the launch option automatically. In Steam: game Properties > General > Launch Options, delete it."
fi

echo
echo "Done. Start Steam again if it was closed."
