# Delegated town defense

Approved follow-up to Village Life, 2026-09-28. A Defend town objective authorizes Mara to organize defense. Routine patrolling belongs inside that objective; the player does not have to issue each subordinate duty. Training/experience remain a later milestone.

## Policy

Mara reviews local defense every 0.5 simulated seconds. Living soldiers above 35 health are fit for duty; the commander is excluded from the seven-soldier allocation. Two fit soldiers hold fixed posts (or fewer when short-handed). With Patrol Doctrine and at least five fit soldiers, two more patrol and the remainder form a reserve. Before doctrine or when understaffed, preserve the posts and reserve. A watchpost changes garrison positions. Wounded soldiers remain in reserve and the commander inspects the approaches.

Duty assignments rotate every 60 simulated seconds using the existing saved patrol clock and stable roster order. This prototype demonstrates reassignment, not fatigue or a day/night schedule. Existing committed actions finish before soldiers move to their next assignment.

A living soldier must observe an enemy within 11 m and line of sight, with the enemy within 16 m of the village center or within 10 m of a built watchpost, to trigger a defensive alert. The garrison holds, patrol is recalled, and remaining fit soldiers respond toward the last observed contact. Eight seconds without a new observation restores routine duties. Status says WATCH ACTIVE, not that all enemies are known to be absent. Accompany/Scout/explicit Patrol remain player overrides; a fallen commander cannot accept orders. Observations here use the existing Founding range/LOS rules, not the separate crowd hearing simulation.

## Presentation

The normal HUD separates compact health/stamina, the main objective, and defense status. Reports appear briefly rather than permanently filling the screen. Mara / defense opens a plan with allocation, rationale, objectives and recent reports. Council contains economy/research/construction and save/load. Controls provides the keyboard reference. Panels do not pause; their rectangles exclude combat mouse clicks. Existing combat key bindings remain unchanged.

At the user's request, an optional collapsible **Developer diagnostics** box remains available for recordings. It starts closed, is explicitly labeled as a recording aid, and shows simulation time, duty cycle, current order, alert state, assignment counts, research/build progress and checkpoint status. It is separate from the gameplay panels. Diagnostic enemy totals are debug data, not information supplied to Mara's policy.

## Implementation

- `BlightDelegatedDefense.cs`: allocation, rotations, local observations, response goals, status and recent report history.
- `BlightVillageHud.cs`: Founding-only HUD, panels, diagnostics and load confirmation.
- `BlightFoundingVillage.cs`: scenario lifecycle and assignment dispatch to the new policy.
- `BlightVillageLife.cs`: keeps civilian routines; replaces permanent world labels with the cleaner HUD.
- `BlightVillagePersistence.cs`: refreshes the derived plan after loading; checkpoint schema remains version 1.
- `DelegatedDefenseSmoke.cs` / `FoundingVillageDemo.cs`: explicitly opt-in standalone smoke/screenshots.
- `DelegatedDefenseTests.cs`: automated allocation/rotation, threat recall/resumption and understaffing/override checks.

Runtime files are under `Assets/Reclamation/Runtime/Outbreak`; tests are under `Assets/Reclamation/Tests/PlayMode`. Player combat/shared movement code and existing scenes are unchanged. Version-1 Village Life saves remain compatible. Allocation derives from saved clock/health/research/order; transient observation alerts and UI panels are rebuilt on load.

## Validation commands

Use Unity 6000.3.24f1, graphics enabled, editor closed. Run PlayMode with filter `Reclamation.Tests.DelegatedDefenseTests;Reclamation.Tests.VillageLifeTests;Reclamation.Tests.FoundingVillageTests;Reclamation.Tests.BlightCombatFeelTests`. Use `-runTests -testPlatform PlayMode -testResults <xml> -logFile <log>` without `-quit`.

Build via `Reclamation.Editor.FoundingVillageBuild.BuildWindows`, setting `RECLAMATION_FOUNDING_BUILD` to a new executable path. Add `-batchmode -quit` for the build. Standalone smoke: `ReclamationDelegatedDefense.exe --defense-smoke --output <isolated-directory>`. It uses prepared state and an injected local threat for deterministic checks; it does not simulate a natural complete keyboard/mouse playthrough. Existing `--village-life-smoke` remains available to validate the original development/persistence loop. See the delivered report for exact counts and screenshots.

## Acceptance and limits

One Defend objective establishes duties; doctrine starts patrols automatically; observed threats recall them; shortages preserve essential posts; the player can inspect the reason without reading diagnostics. User playtest should confirm this behavior feels like delegation and the normal HUD is easier to read. Training, tactical research, hierarchy, replacement recruitment, fatigue, and full settlement simulation remain deferred. This is not a new large-army or target-hardware performance certification.
