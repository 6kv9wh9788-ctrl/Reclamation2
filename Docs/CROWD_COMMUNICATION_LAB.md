# Local squad communication lab

Implemented 2026-09-28 in the isolated crowd lab. Soldiers can pass fresh visual sightings to nearby allies as expiring position snapshots. Player combat and production Company AI are unchanged.

## Behavior and architecture

- `CrowdCommunication.cs` coordinates fresh-sight broadcasts, one per second per sender. Received reports cannot be relayed. Respawn resets the cooldown.
- `CrowdBattleSimulation.cs` finds recipients through existing spatial cells: 12 m open range, 6 m across solid cover, at most 192 candidates and 32 recipients per broadcast. Friendly recipients must belong to the sender's squad; enemies use a bounded local same-team group. Water does not block communication.
- `CrowdPerception.cs` stores report positions for three seconds without an enemy identity or live target reference. Own visual memory takes priority; reports take priority over anonymous noise. A fresh visual check is still required to start an attack.
- `BattleCrowdPerformanceLab.cs` supplies alert controls, the scout demonstration, counters, standalone launch modes, and comparative benchmark output.
- Move and Hold retain their existing authority. Attack units may investigate within their existing leash. The existing command implementation is unchanged.

Reports are simulated events; this milestone adds no recorded voice audio or physical acoustics. Dense crowds can exceed bounded recipient searches. There is no report relay, strategic communication network, transmission latency model, or enemy squad organization.

## Validation

Unity 6000.3.24f1, graphics-enabled PlayMode batch run: **33 passed, 0 failed, 0 skipped**, 16.735 seconds of test execution; editor exit 0. This covers six new communication tests plus 27 existing perception, navigation, command, battlefield, and crowd-performance tests. The complete production test suite was not rerun.

New tests cover position-only reports and expiry, inability to relay, faction/squad/range isolation, cooldown and fresh-sight requirements, own visual-memory priority, Move/Hold authority, and solid-cover versus water transmission.

Focused test command (close this project's editor first; do not add `-quit`):

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.24f1\Editor\Unity.exe' -batchmode -projectPath 'C:\Users\thoma\Reclamation2' -runTests -testPlatform PlayMode -testFilter 'Reclamation.Tests.CrowdCommunicationTests;Reclamation.Tests.CrowdPerceptionTests;Reclamation.Tests.CrowdNavigationTests;Reclamation.Tests.CrowdSquadCommandTests;Reclamation.Tests.BattleCrowdPerformanceTests;Reclamation.Tests.CrowdBattlefieldTests' -testResults "$PWD\communication-tests.xml" -logFile "$PWD\communication-tests.log"
```

Non-development Windows x64 standalone build succeeded, exit 0, 227,162,657 bytes reported by the builder, with 485 warnings. Build warnings remain technical debt; this is not a warning-free build. The existing `Reclamation.Editor.BattleCrowdPerformanceBuilder.BuildWindows` entry point was used with `RECLAMATION_CROWD_BUILD` pointing to the output executable.

The standalone scout preview exited 0. Its captured image was visually inspected: the selected ally behind the wall displays ALLY REPORT and a reported-location line; the UI is readable. Physical mouse interaction remains a user playtest.

## Playtest

Extract the full ZIP and run **Open Communication Lab.cmd**. Click **Scout report demo**. The scout outside the wall sees an enemy and reports to the observed ally behind cover. Look for **ALLY REPORT** and the yellow location line. The ally can turn while holding but should not chase. Reset the demonstration to repeat it; combat continues after setup.

Try Move, Hold, and Attack, then compare Alerts ON/OFF (resets the lab). Alerts ON also enables senses; disabling senses disables alerts. Existing Wall/Gap/Bridge controls remain available.

The two Compare launchers run 1,280 units with perception enabled, identical bridge deployment and attack destinations, equipment and shadows enabled, flat ground. Each resolution runs two trials per alert setting in reversed order, with 20 seconds warmup and 10 seconds measured. Keep the player focused. Counters include warmup. Measured performance and qualifications follow below.

## Preservation and files

The pre-work inventory contains 1,700 existing nonignored files. After restoring incidental Unity-generated settings/vendor changes, 1,697 are byte-identical and only the three intended runtime files differ: `BattleCrowdPerformanceLab.cs`, `CrowdBattleSimulation.cs`, and `CrowdPerception.cs`. No existing files are missing. New code consists of `CrowdCommunication.cs` and `CrowdCommunicationTests.cs`, each with its Unity meta file. This report is added as `Docs/CROWD_COMMUNICATION_LAB.md`.

Player control, DuelFighter rules, production Company AI, existing squad command source, scenes, and project settings are unchanged. Existing local edits and earlier demo artifacts were preserved.

Evidence is retained in the workspace under `outputs/crowd-communication`: test XML/log, build output, preview image, benchmark JSON/CSV/captures, verified results and package verification. The pre-work snapshot and audit tools are under `work/crowd-communication`.

## Limits and next validation

These are isolated prototype workloads on a Core Ultra 9 275HX / RTX 5070 Ti Laptop GPU. They do not establish 60 FPS on the intended midrange desktop or a full production battle with all game systems. Alerts change behavior and combat density, so the off/on comparison does not isolate messaging overhead. Retain the existing navigation and perception limitations. The next useful validation is the interactive scout/order playtest, followed by a benchmark on a selected reference PC before expanding scope.

# Local-alert benchmark results

Shadows and equipment enabled, flat ground; 1,280 units. Non-development D3D12 player, uncapped. Two trials per workload and resolution; reversed second order. 20-second warmup + 10-second measurement per case.

Hardware: Intel(R) Core(TM) Ultra 9 275HX / NVIDIA GeForce RTX 5070 Ti Laptop GPU.

| Resolution | Workload | Avg FPS | 1% low FPS | CPU simulation ms/frame |
|---|---|---:|---:|---:|
| 1080p | Alerts OFF | 209.3-298.8 | 85.6-132.5 | 0.575-0.641 |
| 1080p | Alerts ON | 267.2-327.4 | 105.5-157.1 | 0.574-0.699 |
| 1440p | Alerts OFF | 263.6-280.2 | 94.9-111.7 | 0.544-0.587 |
| 1440p | Alerts ON | 283.3-286.0 | 101.9-116.9 | 0.614-0.628 |

Both workloads use the same bridge map, squad deployment, starting facing and attack destinations. Perception is enabled in both workloads. Local alerts can change decisions and therefore trajectories and combat density; this is a workload comparison, not an isolated messaging microbenchmark. CPU simulation time is averaged over rendered frames (many have no 60 Hz tick).

Zero measured simulation backlog in all eight cases. Focus at measurement end: 8/8. Maximum warmup dropped time: 0.459 seconds. Focus flags only describe the end of each measurement.

Results do not establish the target midrange-PC or complete production battle performance.

Planning field totals per alerts-on case (initialization + orders): 17 fields / 7.91 ms, 17 fields / 6.80 ms, 17 fields / 6.72 ms, 17 fields / 4.88 ms. These are separate from per-frame simulation time.

