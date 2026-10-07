# Isolated squad-command prototype

## Scope and controls

The battlefield lab now has eight friendly squads of 32 against 1,024 enemies. Launch with --squads (the supplied launcher does this). Click a squad button or All squads, select Move/Attack, then right-click terrain outside the UI. Hold stops travel and permits in-place defense. Existing committed strikes finish before a new decision. All-squad commands offset destinations into a four-column, two-row layout. Squad rows use eight columns and four ranks at 1.15 m spacing; anchors march at 1.6 m/s and individual catch-up is capped at 2.2 m/s.

Move follows assigned positions and does not initiate attacks. Attack marches toward the objective and permits engagement within 6 m of each assigned slot. Hold never chases. Commands are a separate opt-in policy over the crowd harness, not production Company AI. The old stress workload is retained when the policy is absent. Terrain/equipment/shadow controls remain available. Respawns preserve population for performance testing.

## Changed files

- Assets/Reclamation/Runtime/Outbreak/CrowdSquadCommands.cs (new): squad membership, formation slots, commands, anchor movement, engagement policy.
- Assets/Reclamation/Runtime/Outbreak/CrowdBattleSimulation.cs: optional command hooks and spatially bounded local avoidance for squad mode. Original-mode movement remains intact.
- Assets/Reclamation/Runtime/Outbreak/BattleCrowdPerformanceLab.cs: command panel, Input System right-click destinations, projected squad-anchor labels, standalone suite and exported squadCommands flag. Squad mode locks the unrelated population/workload buttons; Return to stress lab restores them.
- Assets/Reclamation/Tests/PlayMode/CrowdSquadCommandTests.cs (new): membership/spacing, arrival, selected-squad isolation, Hold, engagement, commitment, Move versus Hold near an enemy.
- Docs/SQUAD_COMMAND_LAB.md (new): this report. New scripts retain Unity-generated meta files.

## Acceptance and limitations

Automated arrival check moves all eight squads 8 m across clear ground: every unit must finish within 0.2 m of its assigned slot after 10 simulated seconds and retain greater than 0.65 m pairwise spacing. Hold is tested while an enemy remains 5 m away; the friendly stays fixed. Attack starts a nearby strike, and a replacement Move order cannot move that fighter during commitment. Per-squad commands leave another squad's anchor unchanged.

Local avoidance is approximate and bounded to 48 nearby candidates per moving unit. It is not a global route planner or a guarantee against congestion in crossing streams or deliberately overlapping squad goals. No collision with terrain props, slope logic, formation wheeling, drag selection, casualties/victory condition, strategic enemy orders or production Company integration is included. Terrain height remains cosmetic. Squad labels mark commanded formation anchors, which can lead the actual soldiers during fighting. Equipment remains visual only. Shared player/DuelFighter rules, attacks, dodge and approved body assets are unchanged.

Human playtesting of mouse commands and perceived responsiveness is still needed; automated simulation and rendered captures do not establish that subjective result. Try Move into open ground, Hold, one-squad Attack, then All-squad Attack. The prototype is suitable for this feedback, not a finished army controller.

## Reproduce

Unity 6000.3.24f1. Focused test filter: Reclamation.Tests.CrowdSquadCommandTests;Reclamation.Tests.BattleCrowdPerformanceTests;Reclamation.Tests.CrowdBattlefieldTests. Run with -batchmode -runTests -testPlatform PlayMode, graphics enabled and no -quit, supplying -projectPath, -testResults and -logFile.

Build: set RECLAMATION_CROWD_BUILD to a new executable path; run Unity with -batchmode -quit -executeMethod Reclamation.Editor.BattleCrowdPerformanceBuilder.BuildWindows. The global scene list is unchanged.

Benchmark: ReclamationSquadLab.exe --squad-suite --captures --warmup 20 --seconds 10 --output <folder> -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -logFile <log>. Repeat at 2560x1440. Four cases per resolution: original stress, squad attack, squad attack, original stress. The mode flag and raw frame CSV are exported. These workloads use different deployments/combat densities; their difference is not isolated command overhead.

## Preservation

The pre-turn inventory covered 1,685 existing nonignored files. Only the two intended crowd runtime files changed; 1,683 stayed byte-identical and none were missing. Unity-generated changes to settings and vendor database copies were restored/archived. No commits or package upgrades.

## Standalone validation

The non-development Windows x64 build succeeded (227,147,169 bytes; 485 shader/package warnings). Eight final standalone cases completed, with exit code 0 at both resolutions and no exceptions in the player logs. JSON flags, population, actual resolution and non-development status were checked. Frame counts, mean and p95 were independently recomputed from CSV.

All measured windows had zero simulation backlog and all eight cases reported focus at measurement end. Warmup stalls reached 0.678 dropped seconds; startup/first-use hitches still need investigation. No midrange hardware was tested. The screenshots at both resolutions show the command panel, squad-anchor labels and active engagement; physical input has not been automated.

# Squad-command benchmark results

All layers enabled; 1,280 units. Non-development D3D12 player, uncapped. Two trials per workload and resolution; reversed second order. 20-second warmup + 10-second measurement per case.

Hardware: Intel(R) Core(TM) Ultra 9 275HX / NVIDIA GeForce RTX 5070 Ti Laptop GPU.

| Resolution | Workload | Avg FPS | 1% low FPS | CPU simulation ms/frame |
|---|---|---:|---:|---:|
| 1080p | Original stress | 254.1-344.3 | 98.4-139.9 | 0.182-0.247 |
| 1080p | Squad attack | 265.6-374.9 | 121.9-179.3 | 0.229-0.297 |
| 1440p | Original stress | 218.6-264.1 | 91.8-94.3 | 0.232-0.282 |
| 1440p | Squad attack | 181.1-186.5 | 75.7-75.8 | 0.400-0.444 |

This is a comparison of two workloads, not isolated command overhead: squad mode starts the sides in separate formations while the original stress workload starts interleaved. Combat density, active fighters, geometry visibility and LOD differ. CPU simulation time is averaged over rendered frames (many have no 60 Hz tick). Do not interpret a higher FPS as an algorithmic speedup.

Zero measured simulation backlog in all eight cases. Focus at measurement end: 8/8. Maximum warmup dropped time: 0.678 seconds. Focus flags only describe the end of each measurement.

Results do not establish the target midrange-PC or complete production battle performance.

Final focused PlayMode result: 13 passed, 0 failed, 0 skipped; 2.089 seconds test time, Unity exit 0. Includes six command behavior tests plus all seven existing crowd/battlefield tests. The full production test suite was not rerun; protected production source was verified byte-identical.
