# Battle crowd performance lab

Primary target: 1,280 active characters (256 friendly / 1,024 enemy) at 1080p and 60 FPS on a future, explicitly selected midrange reference PC. Secondary: 1440p/60; stretch: 120-144 FPS. This task establishes a repeatable workload and an initial local baseline, not final-game certification.

## What was built

A separate saved BattleCrowdPerformance scene and non-development Windows player. The original crowd test, body lab, live Sidekick player and production combat code remain unchanged.

The new render path uses 370-vertex near and 206-vertex far rigid-piece characters, fixed fists, and shared baked animation meshes. There are 16 run and 16 attack poses per detail tier plus an idle/guard pose, 66 meshes total. Near animation uses 16 phases and far animation eight; there is no per-unit GameObject, Animator, skeleton or finger update. Characters are grouped by team, detail, action and pose for instanced drawing. All units are submitted; Unity performs batch visibility handling. Distance detail changes at 65 metres from a fixed camera. Force near detail is available for comparison. This deliberately rough representation is not a production replacement for the approved body mesh.

Counts: 64, 256, 640 and 1,280. Team assignment is one friendly per five units, rounded up for counts not divisible by five; the maximum is exactly 256/1,024.

Render + animation mode has no simulation updates. Active combat stress uses the unchanged DuelFighter and BlightEquipment rules for action commitment, stamina, damage and recovery. A separate deterministic harness performs staggered 10 Hz target searches in spatial cells, 60 Hz action/movement updates, local separation and hit resolution. Defeated units respawn to sustain population. Friendlies respond to Advance/Hold/Retreat outside timed measurement. There is no production Company tactical policy, terrain routing, individual physics, equipment variety, ragdolls, effects, audio or full gameplay UI. Shadows are off. The two modes measure this workload, not a complete game battle.

## Measurement

VSync is disabled and the frame cap removed for the isolated process. The default measurement is 5 seconds warmup and 20 seconds sampling per count/mode. JSON reports record actual resolution, hardware, graphics API, quality, build type, end-of-run focus state, average FPS, p95/p99 frame time, 1% low FPS, CPU simulation and CPU submission time, memory, detail counts and draw submissions. Frame intervals come from a monotonic stopwatch. They are not GPU timings; CPU submission excludes GPU execution. One-percent-low FPS is the reciprocal of the mean slowest 1% frame time. CSV retains every sampled frame.

Simulation processes up to eight fixed steps per rendered frame. Any remaining backlog is explicitly counted as dropped simulation seconds; nonzero drops invalidate claims of sustained real-time simulation throughput. Combat counters include warmup. Memory values are Unity allocated memory and managed heap, not process working set or GPU VRAM.

Keep the standalone player visible and use consistent power, resolution and background-app conditions. Run repeated full-length trials on the selected midrange PC. A faster laptop result is only a development baseline. Use frame-time tails and simulation backlog, not average FPS alone, to assess the 16.67 / 8.33 / 6.94 ms budgets.

## Files and commands

- Runtime: CrowdAnimationLibrary.cs, CrowdBattleSimulation.cs, BattleCrowdPerformanceLab.cs.
- Editor: BattleCrowdPerformanceBuilder.cs. Menu: Reclamation > Testing > Open Battle Crowd Performance Lab.
- Tests: BattleCrowdPerformanceTests.cs.
- Resources/CrowdPerformance/AnimationLibrary.asset and Scenes/BattleCrowdPerformance.unity, with metadata.

Bake once using `Reclamation.Editor.BattleCrowdPerformanceBuilder.Bake` in Unity 6000.3.24f1 with `-batchmode -quit`. The method refuses to overwrite an existing library. Build with `Reclamation.Editor.BattleCrowdPerformanceBuilder.BuildWindows` and RECLAMATION_CROWD_BUILD set to a new executable path. The builder passes its scene explicitly and does not modify the global scene list.

Tests: `-batchmode -projectPath C:\Users\thoma\Reclamation2 -runTests -testPlatform PlayMode -testFilter Reclamation.Tests.BattleCrowdPerformanceTests -testResults <xml> -logFile <log>` (retain graphics; no -quit). Set RECLAMATION_CROWD_CAPTURES for a render capture.

Player: `ReclamationCrowdLab.exe --crowd-suite --output <directory> --warmup 5 --seconds 20 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -logFile <log>`. Change width/height for 1440p. Optional `--count 1280` restricts the suite to the target population; `--captures` saves target-population screenshots after measurement. Launchers are included in the ZIP.

The five focused tests verify the maximum team split, deterministic damage and population maintenance, order behavior, percentile calculation, asset budgets and a rendered instanced crowd. They do not certify final battle AI or visual quality.

## Verified delivery - 2026-09-28

Five focused tests passed, zero failed or skipped (4.33 seconds). Final non-development Windows build succeeded with 485 package shader warnings. Sixteen initial cases (two resolutions, four populations, two workloads) and four longer final-build target-population cases completed; all standalone processes exited 0. Actual resolutions and 256/1024 team counts were verified. No simulation time was dropped. Frame counts, means and p95 values were independently recomputed from CSV.

Final 20-second active-combat samples after five-second warmup: 1080p 381.5 average FPS / 114.5 1% low / 5.04 ms p95; 1440p 337.0 average FPS / 93.6 1% low / 6.25 ms p95. Hardware: Core Ultra 9 275HX / RTX 5070 Ti Laptop GPU. Results varied from the earlier short trials (253.2 and 250.7 average FPS respectively); do not attribute this uncontrolled difference to the small UI-only revision or extrapolate it to the intended reference PC. Full results are in MEASUREMENTS.md and verified-summary.json under outputs/crowd-performance in this task workspace.

Final rendered player captures were inspected. The Results folder control replaces a clipped long path, and the near-detail toggle no longer overlaps a count button. Physical mouse interaction was not automated. These results do not validate the full production battle, stable 120/144 FPS, or midrange-PC performance. Next: select a reference PC, repeat the suite under controlled conditions, then add representative terrain, production AI/routing, equipment and shadows one layer at a time.

Preservation audit: all 1,658 preexisting inventoried files are byte-identical. Only the new benchmark runtime/editor/tests, baked library, scene and this document were added. Incidental Unity settings changes and staged vendor database copies were restored or archived after verification. No commits were created.
