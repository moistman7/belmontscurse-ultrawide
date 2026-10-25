<#
.SYNOPSIS
  Launches Castlevania: Belmont's Curse with a given plugin DLL and checks that it loads and runs.

.DESCRIPTION
  Installs the DLL, starts the game through Steam, waits for the plugin to load, optionally drives the menus to load
  the existing save and press the dump key, then checks for crashes, plugin errors and the expected log/dump lines.
  Closes the game afterwards. Exit code 0 = PASS, 1 = FAIL. Never presses E / interacts with the world; it only loads
  the save (Resume) and presses F9. Use -Gameplay only when you are not using the PC: it takes over the keyboard.

  This is the check that was skipped before the 0.8.17 crash. Run it before publishing any build.

.EXAMPLE
  .\tools\smoke-test.ps1 -Dll .\bin\Release\BelmontsCurseUltrawide.dll -Gameplay
#>
param(
	[Parameter(Mandatory)][string]$Dll,
	[string]$GameDir = "D:\SteamLibrary\steamapps\common\Castlevania Belmont's Curse",
	[int]$AppId = 4231820,
	[int]$HoldSeconds = 45,
	[switch]$Gameplay,
	[switch]$RestoreAfter
)
$ErrorActionPreference = 'Stop'
$failures = New-Object System.Collections.Generic.List[string]
function Fail($m) { $script:failures.Add($m); Write-Host "FAIL: $m" -ForegroundColor Red }
function Ok($m) { Write-Host "ok:   $m" -ForegroundColor Green }
function GameProcs { Get-Process | Where-Object { $_.Path -and $_.Path -like "$GameDir*" } }

$Dll = (Resolve-Path $Dll).Path
$plugins = Join-Path $GameDir 'BepInEx\plugins\BelmontsCurseUltrawide.dll'
$log = Join-Path $GameDir 'BepInEx\LogOutput.log'
$dump = Join-Path $GameDir 'BepInEx\BelmontsCurseUltrawide_dump.txt'
if (GameProcs) { throw 'The game is already running. Close it first (this test needs a clean launch).' }
if (-not (Test-Path $plugins)) { throw "Plugin folder not found: $plugins" }

# The version the DLL reports: read it from the BepInPlugin attribute via its loaded-line string.
$bytes = [IO.File]::ReadAllBytes($Dll)
$expect = $null
foreach ($offset in 0, 1) {
	$s = [Text.Encoding]::Unicode.GetString($bytes, $offset, $bytes.Length - $offset - (($bytes.Length - $offset) % 2))
	$m = [regex]::Match($s, 'Ultrawide (0\.\d+\.\d+) loaded')
	if ($m.Success) { $expect = $m.Groups[1].Value; break }
}
if (-not $expect) { throw 'Could not find the version string in the DLL.' }
Write-Host "Testing build $expect ($((Get-FileHash $Dll).Hash.Substring(0,12)))"

$backup = Join-Path ([IO.Path]::GetTempPath()) ("uw-plugin-backup-" + [guid]::NewGuid().ToString('N') + '.dll')
Copy-Item $plugins $backup -Force
Copy-Item $Dll $plugins -Force

$started = Get-Date
# The previous session's log stays on disk until the game rewrites it, so only a log written after launch counts.
function LogHas($pattern) { (Test-Path $log) -and ((Get-Item $log).LastWriteTime -ge $started) -and (Select-String -Path $log -Pattern $pattern -Quiet) }
try {
	Start-Process "steam://rungameid/$AppId"
	# 1. the plugin loads
	$loaded = $false
	for ($i = 0; $i -lt 40; $i++) {
		Start-Sleep 3
		if (LogHas "Ultrawide $([regex]::Escape($expect)) loaded") { $loaded = $true; break }
		if (Get-WinEvent -FilterHashtable @{LogName='Application';Id=1000;StartTime=$started} -ErrorAction SilentlyContinue | Where-Object { $_.Message -match 'Belmont' }) { break }
	}
	if ($loaded) { Ok "plugin $expect loaded" } else { Fail "plugin $expect did not load" }

	# 2. optionally load the save and take a dump
	if ($loaded -and $Gameplay) {
		Add-Type -AssemblyName Microsoft.VisualBasic
		Add-Type -TypeDefinition 'using System;using System.Runtime.InteropServices;public static class SmokeKeys{[DllImport("user32.dll")]static extern void keybd_event(byte vk,byte scan,uint flags,UIntPtr extra);[DllImport("user32.dll")]static extern uint MapVirtualKey(uint code,uint type);public static void Tap(byte vk,int holdMs){byte sc=(byte)MapVirtualKey(vk,0);keybd_event(vk,sc,0,UIntPtr.Zero);System.Threading.Thread.Sleep(holdMs);keybd_event(vk,sc,2,UIntPtr.Zero);}}'
		function WaitLog($pattern, $seconds) {
			for ($i = 0; $i -lt $seconds; $i += 2) { if (LogHas $pattern) { return $true }; Start-Sleep 2 }
			return $false
		}
		# The title screen accepts input once the start-menu intros have been handled (the plugin logs them).
		if (-not (WaitLog 'Photosensitivity' 60)) { Fail 'start menu never appeared' } else {
			Start-Sleep 12
			$p = GameProcs | Select-Object -First 1
			if ($p) {
				[Microsoft.VisualBasic.Interaction]::AppActivate($p.Id)
				Start-Sleep 2; [SmokeKeys]::Tap(0x0D,200)      # title screen: Start
				Start-Sleep 8; [SmokeKeys]::Tap(0x0D,200)      # main menu: Resume (loads the existing save)
				# Gameplay shows up in the log (level map camera / save-room mask) once the save has loaded.
				if (WaitLog "LevelMapCamera|Hid save-room" 60) {
					Ok 'reached gameplay'
					Start-Sleep 8
					[SmokeKeys]::Tap(0x78,200)                  # F9: diagnostic dump
					Start-Sleep 4
				} else { Fail 'did not reach gameplay (menus or key input did not respond)' }
			}
		}
	} else {
		Start-Sleep $HoldSeconds
	}

	# 3. crash?
	$crash = Get-WinEvent -FilterHashtable @{LogName='Application';Id=1000;StartTime=$started} -ErrorAction SilentlyContinue | Where-Object { $_.Message -match 'Belmont' }
	if ($crash) { Fail "the game crashed at $($crash[0].TimeCreated.ToString('T'))" } elseif (GameProcs) { Ok 'game still running, no crash event' } else { Fail 'the game exited on its own' }

	# 4. log checks
	if ((Test-Path $log) -and (Get-Item $log).LastWriteTime -ge $started) {
		$text = Get-Content $log
		if ($text -match 'solver hooks=2/2') { Ok 'camera solver hooks installed (2/2)' } else { Fail 'solver hooks not 2/2' }
		$bad = $text | Where-Object { $_ -match '^\[(Error|Fatal)' -or $_ -match 'Exception' -or $_ -match 'switched off' -or $_ -match 'kept failing' }
		if ($bad) { Fail ("errors in the log: " + (($bad | Select-Object -First 3) -join ' | ')) } else { Ok 'no errors, exceptions or switched-off features in the log' }
	}

	# 5. dump checks (gameplay mode)
	if ($Gameplay -and $loaded) {
		if ((Test-Path $dump) -and (Get-Item $dump).LastWriteTime -gt $started) {
			$d = Get-Content $dump
			if ($d -match "=== DUMP $([regex]::Escape($expect))") { Ok "dump written by $expect" } else { Fail 'dump has the wrong version' }
			$hooks = $d | Select-String 'Camera timing hooks: solver=2/2 runs=(\d+)' | Select-Object -First 1
			if ($hooks -and [int]$hooks.Matches[0].Groups[1].Value -gt 0) { Ok "solver hook ran $($hooks.Matches[0].Groups[1].Value) times" } else { Fail 'solver hook never ran (not in a level?)' }
			$cam = $d | Select-String 'begin=(\d+) swappedEnd=(\d+)' | Select-Object -First 1
			if ($cam -and $cam.Matches[0].Groups[1].Value -eq $cam.Matches[0].Groups[2].Value -and [int]$cam.Matches[0].Groups[1].Value -gt 0) { Ok 'render begin/end callbacks paired' } else { Fail 'render callbacks not paired or never ran' }
			if ($d -match 'Disabled feature') { Fail 'a feature was switched off' }
			$summary = $d | Select-String 'gameplay / game menus:|Timing: plugin Update|Hitch log:' | ForEach-Object { $_.Line.Trim() }
			$summary | ForEach-Object { Write-Host "      $_" }
		} else { Fail 'no new dump was written (F9 did not reach the game?)' }
	}
}
finally {
	$procs = GameProcs
	if ($procs) {
		$procs | ForEach-Object { [void]$_.CloseMainWindow() }
		Start-Sleep 8
		$procs = GameProcs
		if ($procs) { $procs | Stop-Process -Force }
	}
	if ($RestoreAfter) { Copy-Item $backup $plugins -Force; Write-Host 'original plugin restored' }
	Remove-Item $backup -Force -ErrorAction SilentlyContinue
}
if ($failures.Count -eq 0) { Write-Host "PASS ($expect)" -ForegroundColor Green; exit 0 }
Write-Host "FAIL ($expect): $($failures.Count) problem(s)" -ForegroundColor Red; exit 1