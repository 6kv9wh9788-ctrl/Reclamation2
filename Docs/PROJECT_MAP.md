# Project map

Inspected 2026-09-25 in `C:\Users\thoma\Reclamation2`. Unity 6000.3.24f1; Test Framework 1.6.0; Input System 1.20.0; URP 17.3.0; AI Navigation 2.0.14. The authoritative pins are ProjectVersion.txt, manifest.json and packages-lock.json.

| Location | Responsibility / entry points |
| --- | --- |
| `Assets/Reclamation/Runtime/Outbreak/BlightCombatLab.cs` | Current Blight lifecycle, Actor state, player/enemy control, simulation, strikes, movement; feature partials share this class |
| `.../BlightDuelRules.cs` | DuelFighter, AttackSpec, BlightEquipment; shared combat contract |
| `.../BlightCompanionCombat.cs` | Regular companion targeting, orders, defense, flanking, movement |
| `.../BlightGateway.cs`, `BlightAdaptiveDefense.cs`, `BlightTerrain.cs` | Tactical lines, recovery/cover/fallback, obstacle routing |
| `.../BlightCompany*.cs`, `BlightCommandMap.cs`, `BlightVillageDefense.cs` | Platoons, commanders, strategic sites, fog, map/UI, waves |
| `.../BlightCombatPresentation.cs`, `BlightCombatFeedback.cs`, `BlightCombatCamera.cs` | Arena/UI/pose, impact presentation/audio, camera |
| `.../BlightHeroVisual.cs`, `BlightThrallVisual.cs`, `BlightHulkVisual.cs` | Procedural character presentation |
| `.../BlightLimb*.cs`, `RefinedLimbVisual.cs`, `BlightLoot*.cs`, `BlightEquipmentPanel.cs` | Limb contact/rules, loot and equipment |
| `.../BlightOutpostMission.cs`, `BlightPatrolRules.cs` | Mission progression and basic squad rules |
| `.../Sidekick*.cs`, `BlightSidekickAccess.cs` | Current local character/rehearsal integration; preserve uncommitted work |
| `.../Combatant.cs`, `CombatDirector.cs`, `CombatEncounter.cs`, `OutbreakAgent.cs` | Separate legacy combat/outbreak simulation, not the Blight fighter model |
| `Runtime/AI`, `Simulation`, `Prototype` | Reservations/scoring, resources, hauling/construction/needs/settlement |
| `Runtime/Neighborhood` | Day clock, civilian routines, legacy camera and displays |
| `Assets/Reclamation/Editor` | Reclamation menus, scene builders, art tools; builders can replace scenes |
| `Assets/Reclamation/Tests/EditMode`, `Tests/PlayMode` | Rule tests and lifecycle/simulation integration tests |
| `Assets/Scenes` | Saved legacy labs and SampleScene; see lab inventory below |
| `Assets/new_combat.unity`, `Assets/test_sidekick.unity` | Existing working scenes; do not regenerate or replace during onboarding |
| `Assets/Synty` | Vendor Sidekick assets/code, SQLite plugin and its separate Editor tests |
| `Docs`, root `*.md` | Current onboarding plus historical design/playtest instructions |
| `Tools/Run-UnityTests.ps1` | Repeatable Windows baseline runner |

Paths abbreviated with `.../` above are relative to `Assets/Reclamation/Runtime/Outbreak`.

## Assembly boundaries

`Reclamation.Runtime` references Unity.InputSystem. `Reclamation.Editor` is Editor-only and references Runtime plus Unity.AI.Navigation. `Reclamation.EditModeTests` is Editor-only; `Reclamation.PlayModeTests` runs in PlayMode; both reference Runtime and TestAssemblies. Vendor Sidekick and SQLite have their own assemblies. Scripts outside these assembly-definition trees also compile into Unity's default assemblies.

## Current labs and entry menus

- `Reclamation > Testing > Create Combat Sandbox`: creates a new unsaved scene containing BlightCombatLab; runtime generates the arena. Scenarios: Duel, Squad, Hulk, Weapons, Patrol, LimbDamage, Outpost, Skirmish, Horde, Gateway, Company.
- `Reclamation > Play > Create Outpost Mission`, `Create Company Command Demo`, `Create Village Defense Demo`: fresh unsaved scenes with scenario settings. They ask about unsaved scene changes; do not invoke merely to inspect code.
- `Reclamation > Testing > Open Sidekick Character Test` / `Open Sidekick Hero Duel`: local in-progress art/test tools. `Open Character Crowd Benchmark` is a rendering lab, not proof of battle performance.
- `Reclamation > Art > Open Modular Character Workshop`: modular art inspection.
- Saved legacy scenes: `AutonomousHaulingLab`, `ShelterConstructionLab`, `LivingNeighborhoodLab`, `PatientZeroLab`, `SystemsValidationLab`. Relevant creation/upgrade menus are now under `Reclamation > Legacy`.

Current Windows demo tooling: `Assets/Reclamation/Editor/CombatDemoBuild.cs`, `Assets/Scenes/CombatDemo.unity`, `Tools/Build-WindowsDemo.ps1`, and `Tools/Test-WindowsDemo.ps1`. See `Docs/WINDOWS_DEMO.md`. The new builder passes its scene explicitly and leaves the global scene list unchanged.

The global configured player build list includes only `Assets/Scenes/SampleScene.unity`. A current combat demo build needs a deliberately chosen saved scene/build profile; do not assume the open editor scene is the release entry point.

Player presentation: `Editor/SidekickPlayerSetup.cs` prepares the project-owned `Assets/Reclamation/Resources/Player/SidekickPlayer.prefab` variant of PF_SampleFace and wires the combat sandbox/demo. `SidekickDuelBridge.cs` and `SidekickGroundedMotion.cs` consume combat state and animate only the player. `SidekickPlayerIntegrationTests.cs` checks lifecycle, pause, action-state preservation and grips. See `Docs/SIDEKICK_PLAYER_INTEGRATION.md` for validation and limitations.
