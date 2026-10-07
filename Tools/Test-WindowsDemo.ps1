[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$BuildDirectory, [string]$ResultsDirectory)
$ErrorActionPreference = 'Stop'
$BuildDirectory = (Resolve-Path -LiteralPath $BuildDirectory).Path
$exe = Join-Path $BuildDirectory 'ReclamationCombatDemo.exe'
if (!(Test-Path -LiteralPath $exe)) { throw "Missing $exe" }
if (!$ResultsDirectory) { $ResultsDirectory = Join-Path $BuildDirectory ('Smoke-' + (Get-Date -Format 'yyyyMMdd-HHmmss')) }
$ResultsDirectory = [IO.Path]::GetFullPath($ResultsDirectory)
if (Test-Path -LiteralPath $ResultsDirectory) { throw 'Choose a new results directory.' }
New-Item -ItemType Directory -Path $ResultsDirectory | Out-Null
$log = Join-Path $ResultsDirectory 'player.log'
$capture = Join-Path $ResultsDirectory 'squad.png'
foreach ($value in @($log,$capture)) { if ($value -match '["\r\n]') { throw 'Invalid path characters.' } }
$arguments = '-screen-fullscreen 0 -screen-width 1440 -screen-height 960 --combat-demo-smoke --combat-demo-capture "{0}" -logFile "{1}"' -f $capture,$log
$process = Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $BuildDirectory -PassThru -WindowStyle Hidden
if (!$process.WaitForExit(120000)) { $process.Kill(); throw "Smoke timed out; inspect $log" }
$process.WaitForExit()
$text = if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log -Raw } else { '' }
$scenarios = @([regex]::Matches($text, 'COMBAT_DEMO_SCENARIO_OK: (\w+)') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
$passed = $process.ExitCode -eq 0 -and $text.Contains('COMBAT_DEMO_SMOKE_PASSED') -and $scenarios.Count -eq 11 -and (Test-Path -LiteralPath $capture)
[ordered]@{ Passed=$passed; ExitCode=$process.ExitCode; Scenarios=$scenarios; Screenshot=$capture; Log=$log } | ConvertTo-Json | Set-Content (Join-Path $ResultsDirectory 'summary.json')
if (!$passed) { throw "Packaged-player smoke failed; inspect $ResultsDirectory" }
Write-Output "Packaged-player smoke passed: $ResultsDirectory"
