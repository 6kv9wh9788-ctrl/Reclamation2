# Squad terrain-navigation prototype

## Scope

Adds selectable Wall, Gap and Bridge layouts to the isolated crowd lab. Each scenario button resets the deployment. Normal squad and stress modes remain available. The player, DuelFighter rules, production Company behavior and existing art are untouched.

The wall is 30 m wide and 6 m deep; troops detour around its ends. The gap is 4 m wide between barrier sections. The river is 6 m deep with a 6 m-wide bridge; river banks are logically impassable. Navigation uses flat ground to match the displayed boundaries. Navigation mode suppresses rolling terrain; equipment and shadows are retained. Primitive geometry is intentional prototype art.

## Implementation

CrowdNavigation.cs (new) builds a static 101 x 101 one-metre grid over the arena, inflating obstacle rectangles by 0.45 m. Eight-neighbor breadth-first fields prohibit diagonal corner cutting. Each squad shares a destination field, rebuilt on an order; there is one shared enemy fallback field toward the friendly side. Grid lookahead chooses a visible lower-distance cell. Units cache waypoints with staggered 10 Hz refresh, while motion and static-obstacle constraints run at 60 Hz. Direct visible routes bypass grid following. Once troops clear a bottleneck, they resume distinct formation slots. Clicks inside obstacles snap to walkable ground.

CrowdSquadCommands.cs routes squad anchors and builds new fields on Move/Attack; Hold remains stationary. CrowdBattleSimulation.cs applies opt-in waypoint steering and obstacle-constrained movement, and prevents attacks through blocked sight lines. BattleCrowdPerformanceLab.cs adds obstacle visuals, scenario buttons, command-line launches, and navigation metadata. These are the only three preexisting source files changed. CrowdNavigationTests.cs and this document are new; new scripts include Unity meta files.

## Acceptance tests

Nineteen focused PlayMode tests passed, zero failed/skipped, Unity exit 0. Includes the prior 13 crowd/command tests and six navigation cases:
- A 32-person squad traverses each of Wall, Gap and Bridge, never enters blocked space, reforms within 0.4 m of its slots within 150 simulated seconds, and retains greater than 0.65 m pairwise spacing.
- All 256 friendlies traverse the bridge, with all 1,280 units present in simulation and enemies moved aside to isolate traffic behavior. Every friendly remains in walkable space and reaches within 0.4 m of its slot within 180 simulated seconds.
- Hold stops routing and a replacement Move reaches the new destination.
- An obstacle click snaps to a valid destination and the wall blocks sight lines.

Enemy interference is excluded from the arrival tests. Active-combat player benchmarks test a different congested workload. Automated success does not guarantee arbitrary opposing traffic or formation overlap will be deadlock-free.

## Reproduce

Unity 6000.3.24f1, graphics enabled. Test filter: Reclamation.Tests.CrowdNavigationTests;Reclamation.Tests.CrowdSquadCommandTests;Reclamation.Tests.BattleCrowdPerformanceTests;Reclamation.Tests.CrowdBattlefieldTests. Use -batchmode -runTests -testPlatform PlayMode, with -projectPath, -testResults and -logFile; do not add -quit.

Build with RECLAMATION_CROWD_BUILD pointing to a new executable and -batchmode -quit -executeMethod Reclamation.Editor.BattleCrowdPerformanceBuilder.BuildWindows. No scene regeneration or global build-list change.

Interactive: ReclamationNavigationLab.exe --navigation. Benchmark: --navigation-suite --captures --warmup 20 --seconds 10 with --output <folder> and -screen-width / -screen-height. Two trials of open-ground squads and bridge navigation, second order reversed. Shadows/equipment enabled; terrain off in both. JSON/CSV exports include actual resolution, navigation flag, field count and cumulative field-planning time.

## Limits and next validation

Static authored layouts only; no dynamic obstacle updates, general terrain import, slopes, global traffic reservations or production Company integration. Individual routes use one-metre resolution and approximate avoidance. Destination formations that overlap obstacles or each other may not fully settle; choose open ground beyond the passage. The shared enemy fallback is not strategic AI. Respawns maintain load. Squad labels track anchors, which can get ahead of delayed soldiers. Physical mouse interaction and subjective feel remain a user playtest. Test opposing traffic, repeated reversals and stopping inside bottlenecks before broadening navigation scope.

Planning totals include initialization plus all issued orders; they are not a single-order latency. Reported simulation milliseconds are per rendered frame, not per fixed tick. Frame timing is not GPU timing. Reference midrange hardware remains untested.

## Build and preservation

Non-development Windows x64 build succeeded: 227,152,993 bytes and 485 shader/package warnings. Test execution time was 16.807 seconds, excluding editor startup. The full production suite was not rerun; protected source was verified byte-identical.

The pre-turn inventory contained 1,690 existing nonignored files. Only the three intended crowd-lab runtime files changed; 1,687 remained byte-identical, with none missing. Known Unity-generated settings and vendor database copies were restored/archived. No changes to saved scenes, shared rule files, packages or player controls; no commit or push.

## Standalone measurements

Both resolution runs exited 0, completing eight cases. Exported population, flags and resolution were verified; frame counts, means and p95 were independently recomputed from CSV. Player logs contained no exceptions. Captures at both resolutions were inspected. Five of eight cases reported focus at measurement end, so timing is provisional. Warmup stalls are real and separately recorded.

# Navigation benchmark results

Shadows and equipment enabled, flat ground; 1,280 units. Non-development D3D12 player, uncapped. Two trials per workload and resolution; reversed second order. 20-second warmup + 10-second measurement per case.

Hardware: Intel(R) Core(TM) Ultra 9 275HX / NVIDIA GeForce RTX 5070 Ti Laptop GPU.

| Resolution | Workload | Avg FPS | 1% low FPS | CPU simulation ms/frame |
|---|---|---:|---:|---:|
| 1080p | Open-ground squads | 362.9-393.4 | 112.8-170.2 | 0.205-0.217 |
| 1080p | Bridge routing | 227.6-382.0 | 85.2-148.4 | 0.262-0.402 |
| 1440p | Open-ground squads | 254.7-301.6 | 85.9-145.3 | 0.247-0.317 |
| 1440p | Bridge routing | 236.5-291.9 | 84.5-97.5 | 0.321-0.400 |

Both workloads start in the same squad deployment and receive the same attack destinations. Bridge routing adds an impassable river and route fields; resulting trajectories, congestion and combat density differ, so the difference is not an isolated pathfinding microbenchmark. CPU simulation time is averaged over rendered frames (many have no 60 Hz tick). Do not interpret a higher FPS as an algorithmic speedup.

Zero measured simulation backlog in all eight cases. Focus at measurement end: 5/8. Maximum warmup dropped time: 0.545 seconds. Focus flags only describe the end of each measurement.

Results do not establish the target midrange-PC or complete production battle performance.

Planning field totals per navigation case (initialization + orders): 17 fields / 5.54 ms, 17 fields / 5.81 ms, 17 fields / 6.12 ms, 17 fields / 4.64 ms. These are separate from per-frame simulation time.
