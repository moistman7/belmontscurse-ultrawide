<#
.SYNOPSIS
  Builds and verifies the four release ZIPs (GitHub plain/AllInOne, Nexus plain/AllInOne) for a new version.

.DESCRIPTION
  Derives each package from the previous release's ZIPs (they carry the bundled BepInEx), swaps in the new plugin DLL,
  README and (GitHub only) the Steam Deck scripts, then writes the ZIPs with Unix permissions (scripts executable, forward
  slashes: .NET's ZipFile on Windows loses both), writes .sha256 files, re-extracts every ZIP and checks it byte for byte.
  Nexus packages never contain steamdeck/ scripts or launchers. Publishing is NOT done here.

.EXAMPLE
  .\tools\release.ps1 -Version 0.8.23 -Dll .\bin\Release\BelmontsCurseUltrawide.dll -BaseDir ..\..\outputs\release-0.8.22 -OutDir ..\..\outputs\release-0.8.23
#>
param(
	[Parameter(Mandatory)][string]$Version,
	[Parameter(Mandatory)][string]$Dll,
	[Parameter(Mandatory)][string]$BaseDir,
	[Parameter(Mandatory)][string]$OutDir,
	[string]$RepoDir = '',
	[string]$Python = 'C:\Users\aids7\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
)
$ErrorActionPreference = 'Stop'
# ($PSScriptRoot is empty inside param() defaults in Windows PowerShell 5.1, so the default is resolved here.)
if (-not $RepoDir) { $RepoDir = Split-Path $PSScriptRoot -Parent }
$enc = New-Object Text.UTF8Encoding($false)
$nl = "`r`n"
$Dll = (Resolve-Path $Dll).Path; $BaseDir = (Resolve-Path $BaseDir).Path; $RepoDir = (Resolve-Path $RepoDir).Path
[void](New-Item -ItemType Directory -Force $OutDir); $OutDir = (Resolve-Path $OutDir).Path
$work = Join-Path ([IO.Path]::GetTempPath()) ("uw-release-" + [guid]::NewGuid().ToString('N')); [void](New-Item -ItemType Directory $work)

# the DLL must report the version we are packaging
$bytes = [IO.File]::ReadAllBytes($Dll); $found = $false
foreach ($o in 0, 1) { $s = [Text.Encoding]::Unicode.GetString($bytes, $o, $bytes.Length - $o - (($bytes.Length - $o) % 2)); if ($s -match "Ultrawide $([regex]::Escape($Version)) loaded") { $found = $true; break } }
if (-not $found) { throw "The DLL does not report version $Version." }

# repo README (GitHub) and its version block
$ghReadme = [IO.File]::ReadAllText((Join-Path $RepoDir 'README.txt'))
$start = $ghReadme.IndexOf("VERSION $Version"); if ($start -lt 0) { throw "README.txt has no 'VERSION $Version' block." }
$end = $ghReadme.IndexOf('-------------', $start)
$block = $ghReadme.Substring($start, $end - $start).TrimEnd("`r", "`n")

# the base packages: which version were they?
$baseZip = Get-ChildItem $BaseDir -Filter 'BelmontsCurseUltrawide-v*.zip' | Where-Object { $_.Name -notmatch 'nexus|AllInOne' } | Select-Object -First 1
if (-not $baseZip) { throw "No base ZIPs found in $BaseDir." }
$baseVersion = [regex]::Match($baseZip.Name, 'v(\d+\.\d+\.\d+)').Groups[1].Value

function Extract($zip, $dest) { Expand-Archive $zip $dest -Force }
$sets = @(
	@{ Key = 'plain';       Base = "BelmontsCurseUltrawide-v$baseVersion.zip";                 Out = "BelmontsCurseUltrawide-v$Version.zip";                 GitHub = $true  },
	@{ Key = 'aio';         Base = "BelmontsCurseUltrawide-AllInOne-v$baseVersion.zip";        Out = "BelmontsCurseUltrawide-AllInOne-v$Version.zip";        GitHub = $true  },
	@{ Key = 'nexus-plain'; Base = "BelmontsCurseUltrawide-v$baseVersion-nexus.zip";           Out = "BelmontsCurseUltrawide-v$Version-nexus.zip";           GitHub = $false },
	@{ Key = 'nexus-aio';   Base = "BelmontsCurseUltrawide-AllInOne-v$baseVersion-nexus.zip";  Out = "BelmontsCurseUltrawide-AllInOne-v$Version-nexus.zip";  GitHub = $false }
)

# Nexus README = the previous Nexus README with the new version block and loaded-line
$nexusBase = Join-Path $work 'nexus-readme-src'; Extract (Join-Path $BaseDir $sets[2].Base) $nexusBase
$nexusReadme = [IO.File]::ReadAllText((Join-Path $nexusBase 'README.txt')) -replace "`r?`n", $nl
$ns = $nexusReadme.IndexOf("VERSION $baseVersion"); $ne = $nexusReadme.IndexOf('-------------', $ns)
if ($ns -lt 0 -or $ne -lt 0) { throw "The base Nexus README has no 'VERSION $baseVersion' block." }
$nexusReadme = $nexusReadme.Substring(0, $ns) + $block + $nl + $nexusReadme.Substring($ne)
if ($nexusReadme.IndexOf("Ultrawide $baseVersion loaded") -lt 0) { throw 'The base Nexus README has no loaded-line to update.' }
$nexusReadme = $nexusReadme.Replace("Ultrawide $baseVersion loaded", "Ultrawide $Version loaded")

# the helper that zips with Unix permissions
$mkzip = Join-Path $work 'mkzip.py'
$py = @'
import sys, os, zipfile, stat, time
src, dst = sys.argv[1], sys.argv[2]
files = []
for base, dirs, names in os.walk(src):
    for n in names:
        full = os.path.join(base, n)
        files.append((os.path.relpath(full, src).replace(os.sep, '/'), full))
files.sort()
with zipfile.ZipFile(dst, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    for name, full in files:
        zi = zipfile.ZipInfo(name, date_time=time.localtime(os.path.getmtime(full))[:6])
        zi.create_system = 3
        executable = name.endswith('.sh') or name.endswith('.desktop')
        zi.external_attr = ((stat.S_IFREG | (0o755 if executable else 0o644)) << 16)
        zi.compress_type = zipfile.ZIP_DEFLATED
        with open(full, 'rb') as f:
            z.writestr(zi, f.read())
'@
[IO.File]::WriteAllText($mkzip, $py, $enc)

$want = (Get-FileHash $Dll).Hash
$problems = 0
foreach ($set in $sets) {
	$stage = Join-Path $work $set.Key; Extract (Join-Path $BaseDir $set.Base) $stage
	Copy-Item $Dll (Join-Path $stage 'BepInEx\plugins\BelmontsCurseUltrawide.dll') -Force
	$readme = if ($set.GitHub) { $ghReadme } else { $nexusReadme }
	[IO.File]::WriteAllText((Join-Path $stage 'README.txt'), $readme, $enc)
	if ($set.GitHub) { foreach ($f in 'install.sh', 'uninstall.sh', 'set-launch-option.awk') { Copy-Item (Join-Path $RepoDir "steamdeck\$f") (Join-Path $stage "steamdeck\$f") -Force } }
	$zip = Join-Path $OutDir $set.Out; if (Test-Path $zip) { [IO.File]::Delete($zip) }
	& $Python -I $mkzip $stage $zip
	$hash = (Get-FileHash $zip).Hash.ToLower()
	[IO.File]::WriteAllText("$zip.sha256", "$hash *$($set.Out)`n", $enc)

	# verify: re-extract and compare every file with the staging tree
	$check = Join-Path $work ("verify-" + $set.Key); Extract $zip $check
	$bad = 0; $n = 0
	Get-ChildItem $stage -Recurse -File | ForEach-Object { $rel = $_.FullName.Substring($stage.Length); $n++; if (-not (Test-Path "$check$rel") -or (Get-FileHash $_.FullName).Hash -ne (Get-FileHash "$check$rel").Hash) { $bad++ } }
	$extra = (Get-ChildItem $check -Recurse -File).Count - $n
	$dllOk = (Get-FileHash (Join-Path $check 'BepInEx\plugins\BelmontsCurseUltrawide.dll')).Hash -eq $want
	$scripts = @(Get-ChildItem $check -Recurse | Where-Object { $_.FullName -match 'steamdeck|\.sh$|\.desktop$|\.awk$' }).Count
	$readmeOk = [bool](Select-String -Path (Join-Path $check 'README.txt') -Pattern "VERSION $([regex]::Escape($Version))" -Quiet)
	$entries = & $Python -I -c "import zipfile,sys; z=zipfile.ZipFile(sys.argv[1]); bad=[i.filename for i in z.infolist() if '\\' in i.filename or i.filename.startswith('./')]; ex=[i.filename for i in z.infolist() if i.filename.endswith(('.sh','.desktop')) and (i.external_attr>>16)&0o111==0]; print(len(bad), len(ex))" $zip
	$badNames, $nonExec = $entries -split ' '
	$ok = ($bad -eq 0 -and $extra -eq 0 -and $dllOk -and $readmeOk -and $badNames -eq '0' -and $nonExec -eq '0' -and ($set.GitHub -or $scripts -eq 0) -and (-not $set.GitHub -or $scripts -gt 0))
	if (-not $ok) { $problems++ }
	$state = if ($ok) { 'OK  ' } else { 'FAIL' }
	Write-Host ("{0} {1}  {2:N0} bytes  files={3} mismatches={4} extra={5} dll={6} readme={7} badNames={8} nonExecScripts={9} scriptEntries={10}" -f $state, $set.Out, (Get-Item $zip).Length, $n, $bad, $extra, $dllOk, $readmeOk, $badNames, $nonExec, $scripts)
}
[IO.Directory]::Delete($work, $true)
if ($problems) { Write-Host "$problems package(s) failed verification." -ForegroundColor Red; exit 1 }
Write-Host "All four packages built and verified in $OutDir" -ForegroundColor Green
