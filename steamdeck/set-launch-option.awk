# Sets "LaunchOptions" for one app in Steam's localconfig.vdf.
# Usage: awk -v appid=123 -v opt='...' -f set-launch-option.awk localconfig.vdf > out
function esc(s) { gsub("\\\\", "\\\\\\\\", s); gsub("\"", "\\\"", s); return s }
function ind(n,   s) { s = ""; while (n-- > 0) s = s "\t"; return s }
{
    t = $0; sub(/^[ \t]+/, "", t); sub(/[ \t\r]+$/, "", t)

    if (t == "{") {
        depth++
        if (pendapps) { inapps = 1; appsdepth = depth; pendapps = 0 }
        else if (pendapp) { inapp = 1; appdepth = depth; pendapp = 0 }
        print; next
    }
    if (t == "}") {
        if (inapp && depth == appdepth) {
            if (!done) { print ind(depth) "\"LaunchOptions\"\t\t\"" esc(opt) "\""; done = 1 }
            inapp = 0
        } else if (inapps && depth == appsdepth) {
            if (!done) {
                print ind(depth) "\"" appid "\""
                print ind(depth) "{"
                print ind(depth + 1) "\"LaunchOptions\"\t\t\"" esc(opt) "\""
                print ind(depth) "}"
                done = 1
            }
            inapps = 0
        }
        depth--; print; next
    }
    if (t == "\"Apps\"" && !seenapps) { seenapps = 1; pendapps = 1; print; next }
    if (inapps && !inapp && depth == appsdepth && t == "\"" appid "\"") { pendapp = 1; print; next }
    if (inapp && depth == appdepth && t ~ /^"LaunchOptions"/ && query) {
        v = t; sub(/^"LaunchOptions"[ \t]*"/, "", v); sub(/"$/, "", v); print v > "/dev/stderr"; print; next
    }
    if (inapp && depth == appdepth && t ~ /^"LaunchOptions"/) {
        print ind(depth) "\"LaunchOptions\"\t\t\"" esc(opt) "\""; done = 1; next
    }
    print
}
END { if (!done && !query) exit 3 }
