[CmdletBinding()]
param([string]$OutputDirectory, [string]$UnityPath)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$versionLine = Get-Content (Join-Path $project 'ProjectSettings/ProjectVersion.txt') | Where-Object { $_ -match '^m_EditorVersion: ' } | Select-Object -First 1
$version = ($versionLine -split ': ', 2)[1].Trim()
if (!$UnityPath) { $UnityPath = Join-Path $env:ProgramFiles "Unity/Hub/Editor/$version/Editor/Unity.exe" }
if (!(Test-Path -LiteralPath $UnityPath)) { throw "Unity $version is not installed." }
$lockPath = Join-Path $project 'Temp/UnityLockfile'
if (Test-Path -LiteralPath $lockPath) {
    try { $lockProbe = [IO.File]::Open($lockPath, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None); $lockProbe.Dispose() }
    catch { throw 'Save and close this project in Unity before building.' }
}
if (!$OutputDirectory) { $OutputDirectory = Join-Path $project ('Builds/WindowsCombatDemo/' + (Get-Date -Format 'yyyyMMdd-HHmmss')) }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Choose a new output directory to avoid stale build files.' }
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$exe = Join-Path $OutputDirectory 'ReclamationCombatDemo.exe'
$log = Join-Path $OutputDirectory 'build.log'
foreach ($value in @($project,$exe,$log)) { if ($value -match '["\r\n]') { throw 'Invalid path characters.' } }
$arguments = '-batchmode -quit -projectPath "{0}" -executeMethod Reclamation.Editor.CombatDemoBuild.BuildWindows -combatBuildPath "{1}" -logFile "{2}"' -f $project,$exe,$log
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -PassThru -WindowStyle Hidden
$process.WaitForExit()
if ($process.ExitCode -ne 0 -or !(Test-Path $exe) -or !(Test-Path (Join-Path $OutputDirectory 'BUILD-RESULT.txt'))) {
    throw "Build failed (Unity exit $($process.ExitCode)). Inspect $log"
}
@'
@echo off
cd /d "%~dp0"
start "Reclamation Combat Demo" "ReclamationCombatDemo.exe" -screen-fullscreen 0 -screen-width 1440 -screen-height 960
'@ | Set-Content (Join-Path $OutputDirectory 'Play Demo.cmd') -Encoding ascii
$guide = Join-Path $project 'Docs/WINDOWS_DEMO_README.txt'
if (Test-Path -LiteralPath $guide) { Copy-Item -LiteralPath $guide -Destination (Join-Path $OutputDirectory 'README.txt') }
Write-Output "Build succeeded: $exe"
