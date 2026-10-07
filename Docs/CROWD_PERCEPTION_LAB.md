# Isolated crowd perception prototype

## Behavior

Each soldier has gameplay vision (120-degree cone, 12 m range, static-map line of sight through open air, including across water), hearing (10 m open / 5 m across blocked sight lines), a four-second last-seen memory and a two-second heard-location memory. Perception is optional and confined to the crowd lab. Units do not render through eye cameras or use microphone input.

Nearest visible enemies are selected from the existing spatial index, using staggered 10 Hz decisions and a bounded 96-opponent candidate search. A decision stores a position snapshot. Pursuit uses this snapshot rather than a hidden target's live position. New attacks require a fresh visibility check; resolved strikes retain physical range/cone/obstacle checks. The underlying DuelFighter rules and player behavior are unchanged.

Accepted attack swings emit sound events with source-position snapshots. Listeners hear anonymous locations, not target identities. Hearing ignores the listener's own event and does not override an unexpired visual memory. A 128-entry ring bounds work/storage; sound events remain available for 0.5 seconds, then expire. Heavy bursts may overwrite older sounds. These are gameplay events, not audible sound effects, footsteps, voice communications or physical acoustics.

A ready soldier can turn toward remembered information. Hold does not chase; Move continues to its formation destination. Friendly Attack investigations remain within 6 m of the assigned slot. Enemies can investigate using the existing navigation policy. Memory expiration ends investigation, and respawn clears awareness. Squad information sharing is not implemented.

## User controls

Open Perception Lab.cmd launches a wall layout with senses enabled. Existing Wall/Gap/Bridge and squad command buttons remain available. Senses ON/OFF resets the lab. Observe friendly/enemy switches between two example soldiers. Yellow geometry displays a 12 m vision cone and the current known-location line; the status shows UNAWARE, HEARD noise, LAST SEEN memory or SEES enemy. Overlays are hidden during timed comparisons.

Turn Noise click ON, then right-click within hearing distance behind the observer. A soldier on Hold should turn but stay in place. Turn Noise click OFF to resume right-click movement/attack commands. Then compare approaching enemies from the front versus behind cover. Physical mouse interaction and subjective responsiveness remain a user playtest; screenshots and simulation tests are not a substitute.

## Changed files

- New Runtime/Outbreak/CrowdPerception.cs: awareness snapshots, cone/range/map visibility, bounded noise events, hearing and expiry.
- Runtime/Outbreak/CrowdBattleSimulation.cs: optional target filtering, snapshot pursuit, noise emission, memory investigation, respawn cleanup and fresh attack visibility checks.
- Runtime/Outbreak/CrowdSquadCommands.cs: uses perceived positions and preserves command priority during investigation.
- Runtime/Outbreak/BattleCrowdPerformanceLab.cs: optional perception mode, observer/noise controls, overlays, comparison/preview entry points and exported counters. Corrected the old generic report limitation text to distinguish prototype navigation from production terrain routing.
- New Tests/PlayMode/CrowdPerceptionTests.cs and Docs/CROWD_PERCEPTION_LAB.md. Source paths above are under Assets/Reclamation; new scripts include generated meta files.

## Tests and reproduction

27 focused tests passed, zero failed/skipped, Unity exit 0: eight perception tests and all 19 existing crowd/command/navigation cases. The final run includes the strengthened actual death/respawn check and a river regression: water blocks travel, but permits vision and hearing. Tests cover front/behind/out-of-cone/out-of-range sight, wall occlusion, immutable last-seen snapshots and expiry, sound behind a unit without granting a target, hearing range/muffling, river transparency, sound prompting a Hold unit to turn and see an enemy, Move priority and respawn memory reset.

Use Unity 6000.3.24f1 with -batchmode -runTests -testPlatform PlayMode and filter Reclamation.Tests.CrowdPerceptionTests;Reclamation.Tests.CrowdNavigationTests;Reclamation.Tests.CrowdSquadCommandTests;Reclamation.Tests.BattleCrowdPerformanceTests;Reclamation.Tests.CrowdBattlefieldTests. Retain graphics; supply -projectPath, -testResults and -logFile; do not use -quit. The full production suite was not rerun.

Build using RECLAMATION_CROWD_BUILD and Reclamation.Editor.BattleCrowdPerformanceBuilder.BuildWindows with -batchmode -quit. The global scene list is unchanged. Interactive --perception; preview --perception-preview; comparison --perception-suite --captures --warmup 20 --seconds 10 --output <folder>. Set screen width/height explicitly. Both workloads use the same bridge map, starting facing and squad orders, with only awareness enabled/disabled. Two trials per workload, reversed second order.

Exported sightAcquisitions counts successful visible observation samples, not unique enemies or first-ever sightings. HearingChecks counts eligible event checks; heardEvents counts accepted listener updates. Counters include warmup. Gameplay differences change combat density and routing, so frame-time differences are not isolated sensor timings.

## Limits

No lighting/stealth, head/eye articulation, vertical sight, body occlusion, dynamic occluders, footstep acoustics, information sharing or production Company integration. Static map knowledge remains available for navigation. Candidate and sound limits trade completeness for bounded work. New orders do not cancel existing committed actions. Memory and sound parameters are initial lab tuning, not approved final combat balance. Midrange reference hardware still needs testing.

## Preservation

The pre-turn inventory covered 1,695 existing nonignored files. Only the three intended crowd runtime files changed; 1,692 remained byte-identical and none were missing. Known Unity-generated settings and vendor database copies were restored/archived. No saved scenes, player controls, shared combat rules, package versions or approved character assets were changed. No commits or pushes.

Final focused test execution time was 18.048 seconds (editor startup excluded). Preliminary measurements before the river visibility correction are excluded from the final results.

## Final player validation

The final non-development Windows build succeeded: 227,158,849 bytes, with 485 shader/package warnings. Both final benchmark players and the final observer preview exited 0. No exceptions were found in the final benchmark logs. The final-build observer capture was inspected: the complete cone is visible, text does not overlap the noise controls, and the observer reports HEARD noise after the injected event. Physical mouse input was not automated.

All eight final cases were verified from JSON and raw CSV: population, resolution, flags, frame counts, mean and p95. Measured fixed-step totals matched sampled wall time within 40 ms. Perception-on cases had positive sight checks and heard-event counters, and all cases produced combat. Startup/warmup dropped time reached 0.410 seconds; no measured-window backlog occurred. Midrange hardware still needs testing.

# Perception benchmark results

Shadows and equipment enabled, flat ground; 1,280 units. Non-development D3D12 player, uncapped. Two trials per workload and resolution; reversed second order. 20-second warmup + 10-second measurement per case.

Hardware: Intel(R) Core(TM) Ultra 9 275HX / NVIDIA GeForce RTX 5070 Ti Laptop GPU.

| Resolution | Workload | Avg FPS | 1% low FPS | CPU simulation ms/frame |
|---|---|---:|---:|---:|
| 1080p | Perception OFF | 269.9-271.5 | 128.3-129.9 | 0.342-0.367 |
| 1080p | Perception ON | 264.7-265.8 | 128.6-130.7 | 0.538-0.541 |
| 1440p | Perception OFF | 229.8-231.2 | 114.4-120.5 | 0.396-0.410 |
| 1440p | Perception ON | 218.4-219.4 | 113.0-114.0 | 0.638-0.658 |

Both workloads use the same bridge map, squad deployment, starting facing and attack destinations. Perception changes detection and therefore trajectories and combat density. The difference is a workload comparison, not an isolated sensor microbenchmark. CPU simulation time is averaged over rendered frames (many have no 60 Hz tick).

Zero measured simulation backlog in all eight cases. Focus at measurement end: 8/8. Maximum warmup dropped time: 0.410 seconds. Focus flags only describe the end of each measurement.

Results do not establish the target midrange-PC or complete production battle performance.

Planning field totals per perception-on case (initialization + orders): 17 fields / 5.06 ms, 17 fields / 5.75 ms, 17 fields / 4.72 ms, 17 fields / 4.63 ms. These are separate from per-frame simulation time.
