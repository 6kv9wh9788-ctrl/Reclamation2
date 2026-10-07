# North-road raid: playable defense event

Approved after the delegated-defense playtest, 2026-09-28. This is one opt-in encounter in the existing Founding scenario, testing the player's personal combat alongside Mara's defense policy. It does not introduce another strategy camera, higher echelons, siege or training.

## Player flow

After the storehouse and watchpost are complete, return to the council with everyone rested and clear of combat. Order Defend town. Save a checkpoint if desired, then open Mara / defense and choose Prepare for a north-road raid. Twenty simulated seconds later three raiders enter along the north road and advance toward the village. The observation perimeter includes 16 m around the village center and 10 m around a built watchpost, still requiring a living soldier within 11 m and line of sight. This includes threats encountered by watchpost guards before they reach the village center. Mara reacts through local observations; event creation does not force her alert or assignments. The player can fight alongside the platoon using unchanged combat controls.

When all three raiders die, the event is marked complete. Friendly health/casualties are retained. Rest at home and save to preserve the result. One raid runs per checkpoint timeline; reload a pre-raid save to replay. No automatic save, free healing, resource reward or recurring waves. Pause freezes preparation and battle. Reset creates a fresh unsaved village.

## Implementation and checkpoint compatibility

`BlightFoundingRaid.cs` owns prerequisites, countdown, the temporary three-actor group and completion. The existing `ControlEnemy` combat is unchanged; each new raider's idle destination/leash is set to the village so it advances rather than guarding the supply cache. The event update runs from the existing Founding update outside actor iteration.

Dead event actors are retired on completion and references cleared, preserving the original 12-actor checkpoint roster. A new optional `raidCompleted` boolean in the version-1 DTO defaults to false for old saves. Completion is only valid with a developed village. Current builds retain older Village Life/Delegated Defense checkpoints; older builds do not understand the new completion flag.

Saving is disabled while preparing or fighting. Loading a validated old checkpoint during the encounter replaces the live raid and restores the original roster. No mid-raid serialization is claimed. Defeat remains the player's defeat; buildings/civilians have no damage model, and Mara's death does not create a replacement commander.

HUD objective and optional Developer diagnostics show raid state. The commander panel includes the contextual start button and requirements. The existing Council save/load controls remain in place.

## Tests and packaged check

`FoundingRaidTests` verifies prerequisites, pause, duplicate-start rejection, reset, a natural AI battle and observed patrol recall, active-save rejection, loading during a raid, and completion save/load. It runs alongside DelegatedDefenseTests, VillageLifeTests, FoundingVillageTests and BlightCombatFeelTests using Unity 6000.3.24f1 graphics-enabled PlayMode.

The opt-in standalone switch `--raid-smoke --output <directory>` prepares a developed village, then runs actual raid AI and public player movement/attack APIs without scripted damage to the event enemies. It captures preparation/response/outcome and verifies saving/loading the result. Setup clears the original supply raiders and grants development; it is not a natural complete founding playthrough or physical keyboard/mouse certification. All smoke files are isolated from the user's checkpoint.

Build entry remains `Reclamation.Editor.FoundingVillageBuild.BuildWindows` with `RECLAMATION_FOUNDING_BUILD` pointing to a new Windows x64 executable. See the delivered report for exact validation results and preservation audit.

## Manual acceptance

Does the approach give enough time to prepare? Is Mara's patrol recall and reserve response understandable? Can the player participate without issuing individual duties? Does the HUD clearly communicate preparation, contact and outcome? Record with Developer diagnostics expanded when useful. This encounter is a small behavior test, not a target-hardware or large-army performance certification.
