# Companion AI 2.0 — ordinary squad recovery

Implemented for playtesting on 2026-09-25 following the onboarding sprint. Scope: Mara and Bren in **Squad and Hulk**. No company/tactical redesign or player combat retuning.

## Behavior

Ordinary companions now enter recovery at <=35% health or <30 stamina. Stamina recovery remains active until at least 65, avoiding repeated switching near the entry threshold. Decisions refresh every 0.35 simulated seconds when the companion is ready to act. A wound does not cancel an already committed strike, dodge, recovery or stagger.

On recovery entry, the companion chooses one nearby position up to 2.1 m away from the current eligible threat, constrained by the current order's area. It does not choose a new farther-away position every frame. Shared terrain routing/body separation still apply; unreachable routes report **Route blocked** without teleporting. There is no added healing or immunity and no automatic squad-wide Withdraw order.

Defense still takes priority and still uses the existing paid dodge/guard rules. At the recovery position, a wounded companion may use a light attack only against an in-range enemy whose recovery has at least the companion's windup plus 0.1 s remaining, with >=65 stamina, no incoming threat and its attack delay complete. It does not pursue that opening. Explicit Withdraw overrides recovery and retains existing commitment and return behavior. Pause/reset and target death are covered.

Spacing, weapon-specific flanking and Hulk defenses already passed the new characterization cases before runtime edits. They were retained. Ordinary companions in Patrol/Outpost/Skirmish, tactical Gateway/Horde and company soldiers retain their existing policy paths. The new intent messages appear through the existing companion labels; no UI code was changed.

## Source changes

- `Assets/Reclamation/Runtime/Outbreak/BlightCompanionCombat.cs`: two scenario-gated hooks and two private ordinary-squad recovery methods. Shared defense, escape selection, movement, targeting and normal attack policy are unchanged.
- `Assets/Reclamation/Tests/PlayMode/BlightCompanionPolicyTests.cs` and `.meta`: twelve new PlayMode methods. Hulk methods loop across Sweep/Smash, both sides, and clear/blocked/exhausted conditions. Durable stationary targets and injected low terrain barriers are fixture-only state; production combat rules are not bypassed by the runtime change.
- `Assets/Reclamation/Tests/PlayMode/BlightCompanyFieldTests.cs`: failure-only position/intent/phase diagnostics; original assertion, timeout and scenario remain unchanged.
- `Docs/COMPANION_AI_2_SPRINT.md`: status updated and linked to this result.
- `Docs/COMBAT_ARCHITECTURE.md`: current ordinary-squad recovery behavior and scope added.
- This result document.

## Test evidence

| Run | Passed | Failed | Interpretation |
| --- | ---: | ---: | --- |
| Before runtime changes: characterization + company failure | 5 | 5 | Four new recovery requirements correctly failed; existing company failure reproduced |
| First implementation: companion, player-feel, adaptive-defense and Gateway checks | 37 | 0 | New recovery behavior and adjacent policies passed |
| Final focused companion suite | 12 | 0 | Includes late-opening refusal and clear-space exhaustion cases |
| Full EditMode, including vendor SQLite | 116 | 0 | No skips/inconclusive; Unity exit 0 |
| Full PlayMode | 203 | 1 | Only the pre-existing company-staging failure; no skips/inconclusive; Unity exit 2 |

No new regression was detected relative to the onboarding baseline. The final companion fixture passed all 12 tests in the full suite as well. Full PlayMode test execution took 352.76 seconds (excluding editor startup). No skips occurred in any run. XML and editor logs are retained in this task's `outputs/companion-sprint` and copied into ignored project `Logs/AgentTests/Companion-AI-2-2026-09-25` after validation. The complete run uses `Tools/Run-UnityTests.ps1 -IncludeThirdParty`. Focused runs use `-testPlatform PlayMode -assemblyNames Reclamation.PlayModeTests -testFilter Reclamation.Tests.BlightCompanionPolicyTests` with separate XML/log paths, graphics enabled, and no `-quit`.

Acceptance exercised: 3-second spacing settlement and 10 seconds of real attacks; >1 m companion separation and at least 0.35 m additional spear range; actual Hulk impact crossings with paid dodges or legal guard/guard break; recovery hysteresis and bounded movement; rejection of late openings; commitment, pause, reset, target death and all three engagement leashes; three identical 8-second decision/position/stamina traces at 1/60-second steps. This is deterministic fixture coverage, not a guarantee across all arbitrary encounters or frame rates.

## Company failure at original sprint handoff

Follow-up: the separately authorized Company staging repair now passes the original test and the full automated suites. See [COMPANY_STAGING_REPAIR.md](COMPANY_STAGING_REPAIR.md). The following records the earlier triage evidence.

`ExpandedAssaultTraversesBridgeAndReleasesBothStagingGroups` remains a separate baseline defect. The new diagnostics showed Mara at approximately (-0.865, 0, 6.134), while Bren was **Route blocked** at (0.092, 0, -16.113). Reserve soldiers Tess and Oren were near (-0.682, 0, -15.826) and (0.919, 0, -15.878). The flank reached approximately (-26.9, 0, 21.7) / (-25.2, 0, 21.7). Bren stalls well south of the river/bridge at z=-5 to -2.

The positions and shared body-separation code point to reserve-body obstruction/local steering as the next investigation target, rather than insufficient bridge travel time. That cause is an inference, not a completed repair. The main/flank staging gate correctly stays closed while Bren has not arrived. No company runtime code, shared movement, test timeout or assertion was changed. A separate repair should reproduce the reserve bottleneck and validate company/player collision regressions before changing those shared helpers.

## Manual playtest report and acceptance checklist

Tom subsequently reviewed the sandbox and reported: "It worked just fine, just ran a test." Record this as a successful user-reported manual playtest. The reviewed recording also showed Squad running and Bren displaying "Recovering stamina." The report does not enumerate every acceptance case, so it is not evidence that every item below was individually checked. No standalone player build has been verified. The Company staging failure was subsequently repaired and validated separately; see COMPANY_STAGING_REPAIR.md. This manual playtest preceded that repair.

1. Save existing scene work. Use **Reclamation > Testing > Create Combat Sandbox** to create a fresh unsaved lab, press Play, and choose **Squad** or **Hulk**.
2. Issue Follow/Hold/Assault. Watch Mara/Bren fight while maintaining their different weapon ranges. When hurt or exhausted, check that they make a short, bounded reposition and that the intent label explains it. There should be no healing or endless retreat.
3. During enemy tells, watch legal guard/dodge responses. Issue Withdraw during a committed attack: the action must finish before returning, with no new offensive pursuit.
4. Compare Duel/Weapons: light/heavy attacks, directional dodge, blocking, sprint, targeting and camera should feel unchanged. Pause/resume and reset should clear transient behavior.

Do not treat the automated pass as subjective playtest approval. Company staging remains separately tracked even if the ordinary companion review is approved.

## Final preservation audit

SHA-256 verification confirmed 1612 of 1616 pre-existing nonignored files unchanged. Only the two intended code/test files and two documentation updates above changed. Player combat/camera/weapon source, existing character work, scenes, materials and packages were preserved. Unity's test-induced analytics-symbol removal was restored to the exact pre-sprint settings bytes. Shared companion targeting/defense/escape/movement helpers were additionally compared directly and remained unchanged. No commit or push was made.
