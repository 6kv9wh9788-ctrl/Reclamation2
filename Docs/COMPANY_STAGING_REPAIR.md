# Company staging repair

## Problem and repair

The expanded Company assault could wait forever for Bren even after Mara and the flank reached their rally points. A fresh reproduction placed Bren at approximately (0.092, 0, -16.113), behind held reserve soldiers at (-0.682, 0, -15.826) and (0.919, 0, -15.878). The stall was south of the bridge. Independent avoidance directions around the two reserve bodies cancelled, leaving Bren pressed against a gap too narrow to walk through.

Company soldiers travelling to staging without a combat target now choose a consistent passing side around friendly bodies, based on their formation slot. The Company caller explicitly enables this behavior; other MoveCompanion calls retain the previous calculation. Village Defense is excluded. Existing body separation, terrain checks, speed, defense, attack commitment, orders, and staging arrival conditions remain in force. No teleport, reserve relocation, timeout increase, or weakened assertion is used.

## Changes

- `Assets/Reclamation/Runtime/Outbreak/BlightCompanyCombat.cs`: enables consistent passing only during Company staging transit.
- `Assets/Reclamation/Runtime/Outbreak/BlightCompanionCombat.cs`: optional staging-transit flag and consistent friendly-body avoidance direction.
- `Assets/Reclamation/Tests/PlayMode/BlightCompanyFieldTests.cs`: a regression reconstructs the reserve bottleneck for both formation slots, checks progress, walking step size, body clearance, stationary reserves, and the reserve order. The original expanded-assault test remains unchanged from the pre-repair working copy.
- Sprint/architecture documentation records the repair and Tom's successful preceding companion playtest. That playtest predates this repair and does not count as manual validation of Company staging.

## Validation

- Original expanded-assault test before repair: 0 passed, 1 failed; Unity exit 2. The same previously recorded Bren/reserve positions reproduced.
- New reserve-bottleneck regression before repair: 0 passed, 1 failed on lack of progress. XML was written, then Unity crashed in Sidekick's native SQLite/editor initialization during shutdown. This run is failure evidence, not a successful process run.
- Company field suite after repair: 9 passed, 0 failed, 0 skipped/inconclusive; Unity exit 0.
- Full EditMode including vendor tests: 116 passed, 0 failed, 0 skipped/inconclusive; Unity exit 0.
- Full PlayMode: 205 passed, 0 failed, 0 skipped, 0 inconclusive; Unity exit 0.

Full validation command: `Tools/Run-UnityTests.ps1 -IncludeThirdParty` with a task-specific results directory. Focused runs use `-batchmode -runTests -testPlatform PlayMode -assemblyNames Reclamation.PlayModeTests -testFilter Reclamation.Tests.BlightCompanyFieldTests`, retaining graphics and omitting `-quit`.

Evidence is retained in the Codex task's `outputs/company-repair` directory: `before.xml`, `regression-before.xml`, `company-after.xml`, their editor logs, and `full-baseline/20260927-205058-65c27c76/`.

## Limits and manual check

This fixes the reproduced Company staging bottleneck, not general crowd pathfinding. The Sidekick SQLite shutdown crash is a separate observed tooling risk; vendor code was not changed. Tom subsequently tested the compact Company scenario on video, then reported success after the expanded-region follow-up. This records a successful user-reported post-repair playtest. The expanded run was not independently observed on video. No standalone build was performed during the staging repair.

Optional visual confirmation: create the combat sandbox, select Company and the expanded region, then plan an assault. Watch both attacking platoons reach staging and release together while Tess's reserve remains at Gate. Live enemies can change outcomes; the deterministic regression removes hostiles to isolate navigation.

The full automated baseline is green. Unity's test-induced analytics-define edit was restored byte-for-byte. The original Company test, including its timeout and assertions, is byte-identical to the pre-repair working copy. The final SHA-256 audit is retained beside this report. No commit or push requested or performed.
