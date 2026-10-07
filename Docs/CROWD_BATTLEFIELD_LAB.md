# Battlefield visual-cost benchmark

Extends the existing crowd lab with independently switchable terrain, shadows and equipment. The approved character/body labs and production combat rules are unchanged. This measures the incremental cost of simple battlefield visuals over the same isolated combat stress workload, not a complete game.

## Added layers

- **Terrain:** a 100 x 100 m, 10,201-vertex rolling ground mesh, 64 rocks, and 32 trees (trunk/canopy drawn separately). Deterministic placement around the arena perimeter keeps obstacles out of combat movement lanes. Character render roots follow the height function. Combat remains an XZ-plane simulation: no slope physics, foot IK, obstacle routing or production terrain integration.
- **Shadows:** real soft directional shadows from characters, equipment and props onto the ground. A separate copy of the PC render-pipeline asset sets a 2048 shadow map, two cascades and 110 m distance. Existing render settings/assets are not rewritten. The lab restores the previous pipeline on teardown. Shadow quality otherwise follows the copied PC settings.
- **Equipment:** shared baked helmets, torso armor, shields and sword/spear silhouettes aligned with each animation frame. 86 additional near-detail vertices / 50 far-detail vertices per unit, submitted as one extra instanced material group per occupied body group. These are representative low-detail shapes, not finished assets or changes to weapon reach/damage. Hands remain fixed.

The body remains 370 near / 206 far vertices. The optional layers do not replace the approved high-detail body. Instanced submission counts exclude extra GPU shadow passes and ordinary terrain rendering; they are not hardware draw-call counters. Shadows still have a real render cost even when CPU submission time is low.

## Controlled comparison

`--layer-suite` runs 1,280-unit active combat (256 friendly / 1,024 enemy) with baseline, terrain-only, shadows-only, equipment-only and combined presets. The second trial reverses preset order. Each case resets the same simulation, camera and workload. Reports include profile, trial, layer flags, geometry budgets, shadow configuration, actual resolution, frame-time percentiles, 1% lows, CPU simulation/submission time, memory and backlog. JSON and per-frame CSV remain available.

The normal launch opens with all layers on. Switch layers before a timed run; controls lock during measurement. Existing `--crowd-suite` still runs the original population ladder with all added layers off. No frame cap/VSync is applied in the isolated player. Frame intervals are not isolated GPU timings.

Use repeated trials on the intended midrange PC before accepting the 1080p/60 target. This development laptop is Core Ultra 9 275HX / RTX 5070 Ti Laptop GPU. Thermal/power/background-app variation prevents treating a single run or small difference as a causal performance conclusion.

## Files and validation

Modified runtime: `BattleCrowdPerformanceLab.cs` for switches, suite orchestration, root height, equipment submissions and report metadata. Added `CrowdBattlefieldAssets.cs`, `CrowdBattlefieldEnvironment.cs`, `CrowdBattlefieldBuilder.cs`, `CrowdBattlefieldTests.cs`, and the baked `Resources/CrowdPerformance/BattlefieldAssets.asset` with its subassets. No changes to CrowdBattleSimulation, DuelFighter, player control, existing body meshes or poses.

Bake with `Reclamation.Editor.CrowdBattlefieldBuilder.Bake` in Unity 6000.3.24f1 batchmode/quit. It refuses to overwrite an existing bake. Build using the existing `Reclamation.Editor.BattleCrowdPerformanceBuilder.BuildWindows`, with RECLAMATION_CROWD_BUILD pointing to a new executable. The global build scene list is unchanged.

Focused tests: `Reclamation.Tests.CrowdBattlefieldTests;Reclamation.Tests.BattleCrowdPerformanceTests` in PlayMode, graphics enabled, no -quit. They verify layer changes preserve combat state, pipeline restoration, shared gear budgets, actual shadow darkening, and the original crowd correctness checks. Offscreen image checks and player screenshots do not validate physical mouse interaction.

Standalone comparison: `ReclamationBattlefieldLab.exe --layer-suite --captures --repeats 2 --warmup 5 --seconds 20 --output <directory> -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -logFile <log>`. Repeat at 2560x1440. The included comparison launchers use these default timing/repetition values.

## Verified results (2026-09-28)

Seven focused PlayMode tests passed, with zero failures/skips. The final non-development Windows build succeeded; Unity emitted 485 shader/package warnings. Both final standalone suites exited successfully, completing 20 cases total. Independent CSV checks verified frame counts, means, p95 and fixed-step throughput.

With all visual layers enabled, two trials measured 306.0-310.7 average FPS at 1080p (121.6-133.5 FPS 1% lows), and 204.6-220.8 average FPS at 1440p (82.4-107.1 FPS 1% lows). Mean frame time increased by 22.5% and 51.6% respectively against the fresh same-build baseline. Individual-layer comparisons were noisy; see MEASUREMENTS.md and raw JSON/CSV for the complete comparison. These are provisional laptop measurements, not proof of the midrange-PC target.

All measured windows had zero simulation backlog. Two warmup windows dropped simulation time, up to 0.391 seconds. Warmup backlog is exported separately and boundary frames are excluded from measured samples. Startup/first-use hitches remain a profiling task. Combat counters include warmup.

Focus was true at measurement end for 10/20 cases (1440p); the 1080p cases reported false. The lab runs in the background, but OS scheduling/focus remains an uncontrolled variable. Repeat foreground runs on the selected reference PC before making performance commitments.

Preservation audit: of 1,674 preexisting nonignored files, only BattleCrowdPerformanceLab.cs changed; the other 1,673 remained byte-identical, with none missing. New layer code, assets, tests and this document were added. Generated changes to existing assets were restored.

## Try the package

Extract Reclamation-Battlefield-Crowd-Lab-Windows-x64.zip and run Open Battlefield Lab.cmd. Terrain, Shadows and Equipment can be toggled independently before a timed run. Comparison 1080p.cmd and Comparison 1440p.cmd run longer repeatable suites (5-second warmup, 20-second measurements, two trials per preset). Keep the player focused during comparisons.
