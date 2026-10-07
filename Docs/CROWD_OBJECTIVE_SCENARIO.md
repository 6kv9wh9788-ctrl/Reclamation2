# Secure the relay tactical scenario

2026-09-28. A small integrated battle in the isolated crowd lab combines existing squad commands, wall routing, perception, and local alerts around one objective. Production player combat, Company AI, action timings and weapon tuning are untouched.

## Play and acceptance

Extract the complete Windows ZIP and run `Open Relay Battle.cmd`. Select Squad 1 or 2, choose Move or Attack, then right-click a ground destination. Hold defends in place. Existing action commitment still applies. The selected squad's observer has a yellow vision cone and awareness label.

Two squads of 32 friendlies face 32 defenders. The wall blocks soldier vision and forces movement around its ends. The relay is the blue circle beyond it. Capture requires at least four living friendlies, no living enemy inside, and ten consecutive seconds. A contested or understrength zone resets progress. Losing every friendly is defeat; the three-minute limit is timeout. Terminal outcomes stop simulation. Restart creates a fresh battle and clears casualty, capture, timer, perception and alert state.

Suggested first route: Attack around opposite ends of the wall, then converge on the relay. The objective supports several approaches; this prototype does not claim to establish balanced difficulty or prove that flanking is always superior. Enemies use existing local perception and combat behavior, rather than a new strategic controller.

Manual acceptance: confirm right-click destination selection, Move/Hold authority under reports, readable capture state, terminal result and restart. Assess whether the routes provide useful choices. Automated preview scripts issue commands through the same squad API but do not validate physical mouse clicks.

## Architecture and changes

- Added `CrowdObjectiveScenario.cs`: isolated deployment and round state, 64/32 population, capture, casualties and terminal conditions.
- Added `BattleCrowdObjective.cs`: partial lab presentation, objective boundary, concise controls, observer status, restart and standalone preview workflow.
- Updated `BattleCrowdPerformanceLab.cs`: opt-in `--objective` and `--objective-preview` paths, scenario ticking/UI, hidden fallen models, and scenario-specific HUD input exclusion. Existing benchmark entry points remain available.
- Updated `CrowdBattleSimulation.cs`: optional explicit friendly population and opt-out respawns. Defaults preserve existing stress population and respawn policy. Fallen non-respawning units are excluded from the spatial index and no longer act.
- Added `CrowdObjectiveTests.cs`: capture population/duration/contesting, timeout and new-round state, elimination, legacy respawns, and a complete two-flank victory through the real simulation.

Existing communication, perception, navigation, squad command, DuelFighter and player sources were not edited. New C# files retain generated Unity meta pairs. No scene, asset, project-setting or package changes are intended.

## Reproduction

Unity version: 6000.3.24f1. Close this project's editor before batch execution. Use graphics-enabled `-batchmode -runTests -testPlatform PlayMode` without `-quit`, selecting these fixtures via `-testFilter`:

`Reclamation.Tests.CrowdObjectiveTests;Reclamation.Tests.CrowdCommunicationTests;Reclamation.Tests.CrowdPerceptionTests;Reclamation.Tests.CrowdNavigationTests;Reclamation.Tests.CrowdSquadCommandTests;Reclamation.Tests.BattleCrowdPerformanceTests;Reclamation.Tests.CrowdBattlefieldTests`

Build with the existing `Reclamation.Editor.BattleCrowdPerformanceBuilder.BuildWindows` entry point, setting `RECLAMATION_CROWD_BUILD` to a new executable output path. This uses the existing crowd scene explicitly and preserves the global build list. Launch with `--objective` for play, or `--objective-preview --output <directory>` for an automated round with start/contact/result captures and round-result.txt.

## Limits

This 96-unit battle is a functional playtest, not a new 1,280-unit performance measurement. Prior performance results do not establish midrange-PC performance. Commander view shows all enemies; soldier decisions still use their own senses. No fog of war, recorded voice audio, casualty art, rewards, new animations, or strategic enemy coordination is included. Fallen bodies disappear. The existing bounded local communication and navigation limitations remain. Full production tests were not rerun.

Validation evidence is stored in this task's `outputs/crowd-objective` directory, with preservation inventory and scripts under `work/crowd-objective`.

## Validation results

- Focused PlayMode suite after casualty handling: **38 passed, 0 failed, 0 skipped/inconclusive**, 16.136 seconds of test execution, Unity exit 0. Five new objective tests plus 33 existing crowd tests. Later presentation-only changes freeze pose time at the result and move overlapping squad labels into the HUD; standalone captures validate those changes.
- The automated two-flank round reached Secured in the built player. Start, contact and terminal-state images were inspected. No error/exception was reported in the preview log. Physical mouse interaction and subjective tactical usefulness remain manual playtests.
- The non-development Windows x64 build succeeds. It retains 486 build warnings, including existing package shader warnings; the build is not warning-free.
- A pre-work SHA-256 inventory covered 1,705 existing nonignored files. After restoring incidental Unity settings/vendor output, 1,703 remain byte-identical; only `BattleCrowdPerformanceLab.cs` and `CrowdBattleSimulation.cs` differ. No existing files are missing. New runtime/test files and their meta pairs are listed above; this document is added as `Docs/CROWD_OBJECTIVE_SCENARIO.md`.

