[CmdletBinding()]
param(
    [ValidateSet('All', 'EditMode', 'PlayMode')]
    [string]$TestPlatform = 'All',
    [string]$ProjectPath = (Split-Path -Parent $PSScriptRoot),
    [string]$UnityPath,
    [string]$ResultsDirectory,
    [switch]$IncludeThirdParty
)

$ErrorActionPreference = 'Stop'
$ProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
$versionFile = Join-Path $ProjectPath 'ProjectSettings\ProjectVersion.txt'
$versionLine = Get-Content -LiteralPath $versionFile | Where-Object { $_ -match '^m_EditorVersion: ' } | Select-Object -First 1
if (-not $versionLine) { throw "No Unity version in $versionFile" }
$version = ($versionLine -split ': ', 2)[1].Trim()
if (-not $UnityPath) { $UnityPath = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$version\Editor\Unity.exe" }
if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) { throw "Install Unity $version or supply -UnityPath for that version." }
$UnityPath = (Resolve-Path -LiteralPath $UnityPath).Path
if (-not $ResultsDirectory) { $ResultsDirectory = Join-Path $ProjectPath 'Logs\AgentTests' }
# A unique directory prevents stale results from being mistaken for this run.
$runId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8)
$runDirectory = Join-Path ([System.IO.Path]::GetFullPath($ResultsDirectory)) $runId
New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null

function Quote-Argument([string]$Value) {
    if ($Value.Contains('"') -or $Value.Contains("`n") -or $Value.Contains("`r")) { throw 'Invalid command-line path.' }
    return '"' + $Value.TrimEnd('\') + '"'
}

$modes = if ($TestPlatform -eq 'All') { @('EditMode', 'PlayMode') } else { @($TestPlatform) }
$summaries = @()
foreach ($mode in $modes) {
    $resultFile = Join-Path $runDirectory "$mode-results.xml"
    $logFile = Join-Path $runDirectory "$mode.log"
    $arguments = @('-batchmode', '-projectPath', (Quote-Argument $ProjectPath), '-runTests', '-testPlatform', $mode,
        '-testResults', (Quote-Argument $resultFile), '-logFile', (Quote-Argument $logFile))
    if (-not $IncludeThirdParty) { $arguments += @('-assemblyNames', "Reclamation.${mode}Tests") }
    Write-Host "Running $mode with Unity $version. Results: $runDirectory"
    # Keep graphics enabled: rendering/character tests are part of this project.
    # Do not add -quit: the Test Framework exits Unity when testing completes.
    $process = Start-Process -FilePath $UnityPath -ArgumentList ($arguments -join ' ') -PassThru -WindowStyle Hidden
    $process.WaitForExit()
    $exitCode = $process.ExitCode
    $summary = [ordered]@{ Platform = $mode; ExitCode = $exitCode; Result = 'NoResults'; Total = 0; Passed = 0; Failed = 0; Skipped = 0; Inconclusive = 0; Xml = $resultFile; Log = $logFile; Successful = $false }
    if (Test-Path -LiteralPath $resultFile) {
        try {
            [xml]$document = Get-Content -LiteralPath $resultFile -Raw
            $run = $document.'test-run'
            if (-not $run) { throw 'Missing NUnit test-run element.' }
            $summary.Result = [string]$run.result
            $summary.Total = [int]$run.total
            $summary.Passed = [int]$run.passed
            $summary.Failed = [int]$run.failed
            $summary.Skipped = [int]$run.skipped
            $summary.Inconclusive = [int]$run.inconclusive
            $summary.Successful = ($exitCode -eq 0 -and $summary.Total -gt 0 -and $summary.Passed -gt 0 -and $summary.Failed -eq 0 -and $summary.Inconclusive -eq 0 -and $summary.Result -eq 'Passed')
        } catch { $summary.Result = 'InvalidResults'; Write-Warning $_.Exception.Message }
    }
    $summaries += [pscustomobject]$summary
    [pscustomobject]$summary | Format-List | Out-Host
}
$summaries | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $runDirectory 'summary.json') -Encoding UTF8
if (@($summaries | Where-Object { -not $_.Successful }).Count -gt 0) { throw "Unity baseline did not pass. Inspect $runDirectory" }
