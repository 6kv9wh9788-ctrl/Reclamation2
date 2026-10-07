# Tests, labs and build workflow

Use Unity **6000.3.24f1**, as pinned by ProjectVersion.txt. The installed Windows editor is `C:\Program Files\Unity\Hub\Editor\6000.3.24f1\Editor\Unity.exe`. Test Framework is 1.6.0. A valid local Unity license, resolved packages and graphics driver are required. A newer installed editor is not permission to migrate this project.

## Repeatable baseline

Save and close this project's Unity editor, then run from the repository root:

```powershell
powershell -NoProfile -File .\Tools\Run-UnityTests.ps1
```

Optional parameters: `-TestPlatform EditMode` or `PlayMode`; `-UnityPath` for another installation of the **same** editor version; `-ResultsDirectory` for artifact storage; `-IncludeThirdParty` to remove the Reclamation assembly filter. The default scopes to the game's two test assemblies. SQLite has separate vendor EditMode tests, so do not describe the default as all installed vendor/package tests.

Results go into a unique timestamped directory under ignored `Logs/AgentTests`: XML, editor logs, and summary.json. The runner waits for each editor to exit before launching the next mode. A launch return code alone is not proof of tests running. XML must contain executed tests; inspect failing test messages and report skips/inconclusive results. If startup stalls, inspect the log/license/package state; do not delete Library or kill an unrelated editor as an automatic repair.

## Equivalent explicit command (PowerShell)

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.24f1\Editor\Unity.exe'
$project = 'C:\Users\thoma\Reclamation2'
$results = Join-Path $project ('Logs\AgentTests\manual-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $results -Force | Out-Null
$arguments = '-batchmode -projectPath "{0}" -runTests -testPlatform EditMode -assemblyNames Reclamation.EditModeTests -testResults "{1}\EditMode-results.xml" -logFile "{1}\EditMode.log"' -f $project, $results
$process = Start-Process -FilePath $unity -ArgumentList $arguments -PassThru -WindowStyle Hidden
$process.WaitForExit()
$process.ExitCode
```

For PlayMode, change the platform to `PlayMode`, assembly to `Reclamation.PlayModeTests`, and output filenames accordingly. Execute sequentially, never two Unity processes on this project simultaneously. Do **not** add `-quit`: the installed Test Framework SettingsBuilder explicitly warns it prevents command-line test execution. Do not add `-nographics` to the reference baseline: rendering/character tests need the graphics path. Tests run in Editor PlayMode, not a built player.

## Interactive alternative and lab validation

In Unity: **Window > General > Test Runner**, select EditMode then PlayMode, and run the Reclamation assemblies. Save/export the results. Historical documents' test counts are not current acceptance totals. Legacy NavMesh tests can intentionally show a blank Game view.

For a future gameplay change, use **Reclamation > Testing > Create Combat Sandbox** in a disposable/new scene (save existing work first). Compare Duel, Weapons and Hulk against the unchanged baseline: light/heavy commitment, hits/misses, dodge direction/cost/distance, block/guard break, sprint, target lock, camera comfort settings, pause/focus loss and reset. Then exercise Squad orders, Gateway/Horde tactical fallback, Company and Village Defense. Record scenario, weapon, order, settings and observed result. Do not replace saved labs to run automated tests.

## Windows combat demo

The Windows x64 development demo is now built and validated independently of the Editor test suites. See [WINDOWS_DEMO.md](WINDOWS_DEMO.md) for build and packaged-player smoke commands. `Tools/Build-WindowsDemo.ps1` explicitly builds `Assets/Scenes/CombatDemo.unity`; it does not use or change the global SampleScene build list.

## Build status at onboarding (historical)

No project-owned `BuildPipeline.BuildPlayer` method or CI player-build entry point was found in `Assets/Reclamation`. EditorBuildSettings currently enables only `Assets/Scenes/SampleScene.unity`. Test execution compiles the project but does not validate a distributable combat game.

A player build first needs an agreed target platform and saved entry scene/build profile. Use **File > Build Profiles** in Unity 6 to review those choices. Do not silently add combat scenes, change target/render settings, or advertise a made-up `-executeMethod` build command. Adding a deterministic player-build entry point is a separate infrastructure task once those choices are settled.
