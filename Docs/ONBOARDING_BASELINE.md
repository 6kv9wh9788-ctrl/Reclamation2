# Onboarding baseline — 2026-09-25

## Identity and scope

Active project: `C:\Users\thoma\Reclamation2`. Inspected HEAD: `f962c0b3f2cd73256e534249bb69a97b64d65d84` (`added some stuff`). Unity 6000.3.24f1. This baseline includes the existing working-copy edits and untracked Sidekick code/tests; it is not a pristine HEAD baseline. No gameplay implementation or Companion AI 2.0 feature work was authorized for this sprint.

On entry, existing changes included BlightCombatFeedback.cs, BlightCombatPresentation.cs, four Synty HumanSpecies materials, QualitySettings.asset, and untracked Sidekick editor/runtime files, tests, Human-Custom.sk and test_sidekick.unity (plus meta files). These are the user's work and must be preserved. A SHA-256 inventory of 1,609 pre-existing tracked/untracked nonignored files was captured before tests.

## Baseline execution

| Run | Passed | Failed | Skipped / inconclusive | Test duration | Unity exit |
| --- | ---: | ---: | --- | --- | --- |
| Reclamation EditMode | 115 | 0 | 0 / 0 | 1.83 s | 0 |
| Reclamation PlayMode | 191 | 1 | 0 / 0 | 339.46 s | 2 |
| Isolated company failure recheck | 0 | 1 | 0 / 0 | 20.10 s | 2 |
| EditMode including vendor tests | 116 | 0 | 0 / 0 | 1.91 s | 0 |

The baseline is **not fully green**. `Reclamation.Tests.BlightCompanyFieldTests.ExpandedAssaultTraversesBridgeAndReleasesBothStagingGroups` failed at `Assets/Reclamation/Tests/PlayMode/BlightCompanyFieldTests.cs:115` in both the full suite and isolated rerun: `CompanyPlanActive` remained true after 120 simulated seconds. The fixture removes hostiles, plans a no-fail expanded assault and expects main/flank staging to finish. This is a reproducible existing-working-copy failure, not an onboarding gameplay regression. Exact route/coordination root cause is not yet established. Investigate `PlanCompanyAssault`, staging completion/CompanyAt, `ControlCompanySoldier`, and route/body avoidance in a separately scoped repair; do not increase the timeout or weaken the assertion to hide it.

Passing PlayMode fixtures include BlightCombatFeelTests (10), BlightCompanionPlayTests (6), BlightAdaptiveDefenseTests (6), BlightGatewayPlayTests (4), BlightHulkPlayTests (4), BlightWeaponPlayTests (3), CompanionArtPlayTests (3), and SidekickRehearsalTests (2). All 115 Reclamation EditMode cases passed, including shared rule coverage.

The installed runner correctly rejected the failing suite (Unity exit 2 plus failed XML). Focused reproduction used `-testPlatform PlayMode -assemblyNames Reclamation.PlayModeTests -testFilter Reclamation.Tests.BlightCompanyFieldTests.ExpandedAssaultTraversesBridgeAndReleasesBothStagingGroups`, with separate XML/log paths. Full command shapes are in TESTING.md. Test times above exclude editor startup/import/compilation.

Evidence is retained under ignored `Logs/AgentTests/Onboarding-2026-09-25` in the project and in this onboarding task's output bundle. The first EditMode run produced exit-0 confirmation in its editor log; the subsequent runner executions captured process exit codes directly.

Tests execute in Unity Editor, not a standalone game. No manual playtest or performance benchmark was performed, so passing automation must not be described as certification of subjective combat feel or shipping-player readiness.

## Onboarding changes

Added `AGENTS.md`, `Docs/PROJECT_MAP.md`, `Docs/COMBAT_ARCHITECTURE.md`, `Docs/TESTING.md`, this baseline report, `Docs/COMPANION_AI_2_SPRINT.md`, and `Tools/Run-UnityTests.ps1`. Updated only the opening notice of `Docs/GETTING_STARTED.md` to point to current guidance while retaining its historical content. No C# source, tests, scenes, materials, packages, or gameplay tuning were intentionally changed.

The runner pins its default editor from ProjectVersion.txt, runs modes sequentially, retains graphics, writes unique XML/log artifacts and rejects missing/empty/failed/inconclusive results. The installed Test Framework source confirms these CLI arguments and explicitly warns against `-quit` with `-runTests`.

## Risks and technical debt found

- **Broad shared combat surface:** BlightCombatLab spans 21 partial files (3,659 lines) but remains one class/private Actor state. Companion defense and movement are called by ordinary, tactical and company policies; shared changes can affect player movement/collision or approved combat. No broad refactor is included here.
- **Documentation drift:** older GETTING_STARTED and BLIGHT-COMBAT notes list obsolete counts, controls, menus and missing-feature claims. Current code uses Ctrl block/RMB orbit, includes axe/loot/limb/company/tactical systems and eleven scenarios. Use the new map and source-backed architecture notes.
- **Build gap:** only SampleScene is enabled in EditorBuildSettings; no project-owned scripted player build was found. Select a target and saved combat entry scene before implementing a build pipeline. Editor tests do not validate a deliverable game.
- **Unity startup mutation:** opening for tests removed `SENTIS_ANALYTICS_ENABLED` from Standalone scripting symbols in ProjectSettings.asset. The pre-run setting was restored with a byte-identical hash check. Future runners should inspect their diffs; package/editor startup can write project files despite no gameplay edits.
- **Existing character work is uncommitted:** the executed compilation/tests include those files. A clean clone of HEAD is a different baseline. No cleanup/commit of that work was attempted.
- **Prototype-scale navigation:** BlightTerrain creates a visibility graph/arrays when routing; Actor loops scan other actors for targets/separation. This is a scaling risk to profile, not a measured performance failure. The art crowd benchmark does not establish combat simulation throughput.
- **Test seams and manual limits:** many Blight tests use fixed manual simulation and named procedural objects, while legacy NavMesh tests depend on real frames/time. This is useful regression coverage but does not prove real input-device, animation perception, controller support, arbitrary frame stalls or packaged-player behavior.
- **Multiple companion policies:** low-health recovery/cover/fallback already exist in tactical/company paths. An ordinary-companion improvement must retain those policies and avoid adding a second incompatible system.

See `COMPANION_AI_2_SPRINT.md` for the proposed narrow follow-up and measurable acceptance. It has not begun.
