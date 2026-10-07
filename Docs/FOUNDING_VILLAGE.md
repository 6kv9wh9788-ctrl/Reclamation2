# First village: player-led kingdom slice

This document records the original First Village milestone. For the subsequent commander duties, civilian provisioning and persistent checkpoints, see [Village life](VILLAGE_LIFE.md). Historical validation and limitations below refer to that original build.

Built for the direction confirmed on 2026-09-28. The player directly controls one character and gives orders to Mara, a physical platoon commander. This replaces the RTS-style interaction for this slice while preserving the crowd labs as experiments.

## Implemented loop

Start in a small village with Mara and seven soldiers, a reduced test platoon. Within 12 m, issue Accompany, Defend village, or Scout road. Mara assigns two soldiers to scout while others hold village posts. Scouts report a count observed near the north road using range and line of sight; the report is a timestamped observation, not a live hostile position stream. Accompany uses the commander as the soldiers' formation reference. Soldiers can engage near their assignment and use the existing defense/action rules. A fallen commander cannot accept orders; survivors fall back to village assignments.

The player can fight the three road raiders with the existing movement, attack, dodge, sprint and guard controls. Once the road is clear, F at the gold cache recovers supplies. F back at the village circle delivers six timber and two insight, once only. The player must physically recover and return them; a commander cannot complete those interactions remotely.

At the council bench, research costs one insight and takes 15 simulated seconds. Work crews unlocks assigning two civilian builders to a 20-second, four-timber storehouse project. Patrol doctrine unlocks the Peacekeep directive and a two-timber watchpost. Peacekeep assigns two soldiers a local patrol route while the remainder guard posts. Research and construction respect pause and stop if the player is defeated.

Civilian state stays **Village + storehouse**. Military development separately becomes **Outpost** when the watchpost is founded. No Town promotion is implied. `Docs/PLAYER_LED_KINGDOM_DIRECTION.md` records the intended separate civilian and military ladders, higher command hierarchy, multiple holdings and research philosophy.

## Files

New runtime files:
- `FoundingVillage.cs`: session-only supply ledger, research prerequisites/costs/timers, independent projects.
- `BlightFoundingVillage.cs`: deployment, commander assignments and reports, interaction gates, simple village/civilian visuals and player HUD.
- `FoundingVillageDemo.cs`: opt-in packaged smoke sequence and shader references.

New editor/test/scene files:
- `FoundingVillageBuild.cs`: explicit new FoundingVillage scene and non-development Windows build; preserves existing scenes/global build list.
- `FoundingVillageTests.cs`: five focused tests covering supply duplication gates, technology/project independence, scouting/proximity/reset, player interaction/pause/construction, and unlocked patrols/commander loss.
- `Assets/Scenes/FoundingVillage.unity`, plus the generated Unity meta pairs.

Existing files changed:
- `BlightPatrolRules.cs`: appends Founding without renumbering existing scenarios.
- `BlightCombatLab.cs`: opt-in lifecycle, interaction, order and update hooks; founding-only enemy acquisition range/leash. The ControlPlayer method and combat rules are unchanged.
- `BlightCompanionCombat.cs`: dispatches only Founding soldiers into the new assignment policy.
- `BlightCombatPresentation.cs`: founding-only HUD and mouse exclusion.
- `AGENTS.md`: links and summarizes the approved player-led direction to prevent future scope drift.

## Validation

Unity 6000.3.24f1, graphics-enabled PlayMode tests: **37 passed, 0 failed, 0 skipped/inconclusive**, 161.056 seconds, editor exit 0. The selection included FoundingVillageTests, BlightCombatFeelTests, BlightLabSmokeTests, BlightCompanionPlayTests, BlightCompanyPlayTests and BlightVillageDefenseTests. This is a targeted regression run, not the entire repository baseline.

After the UI/building/council-collision refinements, a second run passed all **15 selected tests**, 30.887 seconds, editor exit 0: five Founding tests and all ten BlightCombatFeelTests. The two runs cover **38 distinct passing tests**, with no failures, skips or inconclusive results. A source comparison confirms ControlPlayer is text-identical to its pre-work version.

The packaged smoke drives the public locomotion/attack APIs for combat; it moves the player transform to the cache/council for the interaction checks. It does not certify physical keyboard/mouse input or a natural complete player journey. Inspect the screenshots and `smoke-result.txt` under `outputs/founding-village/preview-final` for final packaged evidence. Start, scouting and developed-village images are inspected for readability, character view and project visibility.

Build entry: `Reclamation.Editor.FoundingVillageBuild.BuildWindows`, with `RECLAMATION_FOUNDING_BUILD` set to a new executable path. Smoke: `ReclamationFirstVillage.exe --founding-smoke --output <directory>`. Normal play requires no switch. The ZIP supplies `Open First Village.cmd` and a concise README.

## Limits and next playtest

This is a greybox gameplay slice. Buildings and civilian figures are placeholders. It uses the existing combat presentation rather than adding a new character-art integration. Civilians have simple assigned visual work, not a resource-production economy or needs simulation. Watchpost construction is immediate after purchase; civilian construction is timed. Peacekeeping currently means a patrol route, not policing incidents or political simulation. There is no save system: restart/exit discards progress.

Only one commander and one village are implemented here. Army/Battalion/Company hierarchy for this mode, multiple holdings, final unit sizes, intermediate settlement tiers, research balance, and persistence remain future work. The separate production Company and crowd labs were not migrated into a unified system. Founding soldiers use the existing combat range/line-of-sight rules; the crowd lab's hearing and local-alert systems have not been ported here. This build is not a new 1,280-unit or midrange-PC performance certification.

The next acceptance gate is a manual playthrough: does direct character control plus commander intent feel right; are scout reports useful; do research and development create the intended sense of improving a home settlement? Prefer that feedback before another scale or system expansion.

## Preservation

The pre-work inventory contains 1,712 existing nonignored files. The intended existing-file changes are only AGENTS.md and the four runtime hook files listed above; all other inventoried content is preserved. Incidental Unity settings/vendor startup changes are restored from the pre-work snapshot. New scene and script meta pairs are retained. Previous demo artifacts remain available. No commits, resets, upgrades or pushes were performed.

## Final packaged result

Non-development Windows x64 build succeeded, 227,210,949 reported bytes, 485 warnings (including existing package shader warnings). The build is not warning-free. Final smoke exited 0 with FOUNDING_SMOKE_PASSED: player alive, zero remaining hostiles, storehouse complete, military Outpost founded, and both technologies unlocked. Final start/scouting/developed images were inspected; the player stands clear of the council bench. Final preservation audit: 1,707 of 1,712 existing files unchanged, five intended changes, no missing files. The full report lists remaining manual input and gameplay validation.

