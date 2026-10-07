# Reclamation / Blight: agent guide

## Start here

This Unity project is `Reclamation2`; current action combat uses the `Reclamation.Blight` namespace. Read `Docs/PROJECT_MAP.md`, `Docs/COMBAT_ARCHITECTURE.md`, `Docs/TESTING.md`, and `Docs/ONBOARDING_BASELINE.md` before changing systems. `COMBAT-DESIGN.md` records creative direction. Older root playtest notes and `Docs/GETTING_STARTED.md` describe historical milestones: verify controls, menus, and counts against current code.

Use Unity **6000.3.24f1** from `ProjectSettings/ProjectVersion.txt`. Do not upgrade Unity, packages, rendering, input configuration, or asset serialization as incidental cleanup. Check `git status --short` and the diff first. Preserve all existing local edits and untracked files; never reset, clean, regenerate, or overwrite them to obtain a green baseline. Do not commit or push unless requested.

## Architecture and conventions

- Own game code lives in `Assets/Reclamation/Runtime`; editor tools in `Assets/Reclamation/Editor`; tests in `Assets/Reclamation/Tests/{EditMode,PlayMode}`. Runtime code must not depend on UnityEditor.
- `BlightCombatLab` is one partial MonoBehaviour spread across feature files, sharing a private Actor model. A file split is not a runtime isolation boundary. Trace callers before changing shared methods.
- `DuelFighter` / `BlightEquipment` own shared actions, timing, stamina, damage and weapon profiles. Presentation consumes resolved outcomes; cosmetic effects must not change damage or action timing.
- Blight uses custom `BlightTerrain` routing/body separation. Legacy hauling/neighborhood systems use separate models and NavMesh behavior. Do not silently combine them.
- Match neighboring C#: four-space indentation, namespace blocks, PascalCase types/public members, camelCase fields and locals, private serialized configuration, sealed classes where established. Preserve serialized field names and enum numeric values. Avoid unrelated formatting rewrites.
- Follow existing test seams (`InputEnabled = false`, `AutomaticSimulation = false`, `Simulate(1f / 60f)`) for deterministic combat checks. Test observable behavior; do not make tests pass by weakening assertions.
- Preserve Unity `.meta` files/GUIDs and asset-reference pairs. Do not edit generated `.csproj`/`.slnx`, Library, Temp, or PackageCache. Treat `Assets/Synty` as vendor content; no incidental edits.

## Protected behavior: requires explicit feature scope to change

The player attack/dodge feel is approved and is frozen for onboarding and companion work. Preserve:

- Player input roles, attack acceptance, action commitment, light/heavy timings, recovery, facing/lock, hit-once semantics, hit geometry, weapon reach/damage/costs, and existing loot modifiers.
- Dodge cost **25**, duration **0.32 s**, speed **8 m/s** (up to **2.56 m** before obstruction), directional selection and dodge damage immunity. No free dodges, cancellation shortcuts, or timing changes.
- Walk/block/sprint speeds **3.8 / 1.8 / 6.2 m/s**, stamina capacity **100**, sprint cost **18/s**, recovery delay **0.7 s**, release-and-20-stamina exhaustion recovery.
- Guard direction/costs, guard break, ordinary heavy interruption and Hulk two-heavy-hit windup poise. Visual recoil must not add gameplay interruption.
- Shared `Simulate`/`Tick` order and substeps, `Move` collision/routing behavior, camera motion comfort settings, pause/focus loss/reset cleanup, HUD input exclusion, audio toggles, and existing Sidekick presentation/rehearsal behavior.
- Existing squad order/leash and target rules; committed actions finish before new orders act. Preserve tactical fallback, Hold at all costs, company commands/fog, patrol rewards, loot, limb damage, outpost/village behavior, and legacy resource conservation/reservation/infection tests unless explicitly in scope.

Do not use a companion feature as a reason to retune `BlightDuelRules.cs`, `ControlPlayer`, player equipment, shared movement, camera, scenes, materials, or project settings. If an intended change needs those, explain its concrete impact and obtain expanded scope first. Do not start Companion AI 2.0 during onboarding.

## Validation and handoff

From the repository root on Windows:

```powershell
powershell -NoProfile -File .\Tools\Run-UnityTests.ps1
# Focused modes:
powershell -NoProfile -File .\Tools\Run-UnityTests.ps1 -TestPlatform EditMode
powershell -NoProfile -File .\Tools\Run-UnityTests.ps1 -TestPlatform PlayMode
```

Close this project's editor before batch tests. The runner uses separate sequential Unity processes, retains graphics, and writes timestamped XML/logs under ignored `Logs/AgentTests`. It fails on missing/empty XML, failed/inconclusive tests, or a nonzero editor exit. Skips must be reported. See `Docs/TESTING.md` for raw commands, vendor inclusion, GUI fallback, and build limitations. Build the Windows x64 combat demo with `Tools/Build-WindowsDemo.ps1` and validate the executable with `Tools/Test-WindowsDemo.ps1 -BuildDirectory <folder>`. The dedicated CombatDemo scene is passed explicitly; the global SampleScene build list stays unchanged. See Docs/WINDOWS_DEMO.md.

Report changed files, executed commands and exact pass/fail/skip counts, pre-existing failures separately from regressions, remaining manual playtests, and risks. Never label a compile-only run or an empty XML result a passing baseline. Inspect the final diff for unintended scene/asset/settings changes.

## Product direction (confirmed 2026-09-28)

Read `Docs/PLAYER_LED_KINGDOM_DIRECTION.md` before proposing the next milestone. The core game follows one directly controlled character who issues intent through Army, Battalion, Company and Platoon commanders. Crowd RTS-style labs are supporting experiments, not the main player experience. Civilian progression (Village -> Town -> City -> Metropolis) and military specialization (Outpost -> Fortress -> Citadel) are separate. Research should enable roles and useful capabilities for soldiers and civilians. Start with the Founding village slice and its acceptance gate; do not infer approval to implement the full hierarchy or settlement tree.

Procedural-world direction: read Docs/WORLD_LIFE_DIRECTION.md and Docs/INHABITED_ATLAS_VILLAGE.md. Generated settlements must vary through reproducible seeds and validated layouts/routes; preserve approved player combat responsiveness. The inhabited atlas slice is separate from the combat/commander demos.


## Combat in the world (confirmed 2026-09-29)

Read `Docs/COMBAT_DIRECTION.md` for the seven approved combat goals and the current
map integration boundary. `WorldAtlasDemo --atlas-defense` connects a local company
and two defense operations to the forest valley and mountainous coast. Its adapter
reuses unchanged `DuelFighter` rules with atlas navigation and map-specific saves.
Directional attacks/blocks, momentum damage, mounts, shield breaking and full perk
trees remain future work; do not describe these goals as existing capabilities.
Preserve the original guard/character pose libraries; combat uses additional banks.
Run `AtlasDefenseTests` alongside existing atlas and protected combat-feel fixtures.
