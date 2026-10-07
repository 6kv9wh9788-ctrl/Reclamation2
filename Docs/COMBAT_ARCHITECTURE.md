# Current combat architecture and protected contract

Source inspection, 2026-09-25. This describes current code, not a proposal to change behavior.

## Simulation ownership

`BlightCombatLab.Start` builds the arena then selects a scenario. Reset destroys/deactivates prior actors and clears feature state. `Update` reads keyboard/mouse input and passes unscaled delta time to `Simulate`. Public test switches disable input and automatic ticking. `Simulate` caps each request to 0.25 seconds and subdivides it into steps no larger than 1/60 second; pause/end guards apply. Large elapsed-time requests therefore do not simulate their full duration.

Each Tick captures limb poses, advances transient state/timers and company logic, controls player and AI, moves dodges, advances all fighters, resolves impacts, updates mission/fallback state, poses actors, resolves limb sweeps, then updates loot/feedback/camera. Changing this order can change combat outcomes.

`DuelFighter` is a non-MonoBehaviour state model (using Unity math) shared by hero, companions and enemies. States: Ready, Windup, Recovery, Dodge, Stagger, Defeated. `Attack` validates/stores AttackSpec, spends stamina and starts windup. `Advance` returns an impact signal once when windup becomes recovery. `CanAct` prevents an already committed fighter from starting a fresh action. `ReceiveAttack` handles block/guard break/dodge/poise/stagger. Presentation is not the damage authority.

`ResolveStrike` checks hostility, life, range/cone, and terrain line of sight, then hits the nearest eligible target, or each eligible victim for a multi-target attack. Frontal guard uses a 70-degree comparison. Limb-rig player attacks bypass this generic resolution and use `BlightLimbCombat` sweep/contact logic; do not describe every attack as collider contact or every attack as a simple cone. Player attack profiles may be modified by equipped loot.

## Existing companion policies

`ControlCompanion` dispatches company soldiers first, then tactical companions, then regular squad behavior. These are different policies over shared Actor state, not separate physics controllers.

- Regular squad: eligible targets respect visibility and order anchors (Hold 3 m, Follow 7 m, Assault 14 m). Assault prefers the marked target; otherwise eligible targets remain sticky. Two weapon-dependent flank stations use opposite 40-degree lanes. Attacks retain 25 stamina for defense and defer if a visible threat will land before the planned commitment ends.
- Ordinary **Squad/Hulk recovery** (Companion AI 2.0): <=35% health or <30 stamina enters a bounded, latched recovery position; stamina hysteresis exits at >=65. Ready decisions refresh every .35 s. Wounded fighters only take nearby light-attack openings with adequate enemy recovery time and stamina. Explicit Withdraw wins; no healing, action cancellation, or automatic global retreat. Other regular scenarios keep their prior policy. See `COMPANION_AI_2_RESULTS.md` for automated validation and the subsequent user-reported successful playtest.
- Shared defense: `Incoming` picks the soonest visible hostile windup in expanded reach/cone. Companions guard lighter strikes and seek obstacle/body-clear dodge directions for heavy strikes. A dodge still pays the shared cost. Defense cannot cancel committed attacks.
- Gateway/Horde tactical Hold: adaptive defense evaluates position and recovery at 0.35-second intervals; low health (<=35%) or stamina (<30, recovery hysteresis to 65) affects behavior. Teammate cover and pressure-based fallback already exist. Hold at all costs disables automatic fallback, not defensive actions.
- Company soldiers: follow platoon phase/goal, commander risk and order, local defense, and recovery near the platoon. This path also shares threat detection, defense and movement. Company staging transit without a combat target explicitly requests a consistent passing side around friendly bodies, avoiding cancellation between reserve-body avoidance directions. Other movement calls and Village Defense retain their previous behavior; see COMPANY_STAGING_REPAIR.md.
- `MoveCompanion` applies route planning and local avoidance. `RouteGoal` caches waypoints against custom `BlightTerrain`; shared `Move` separates live bodies, clamps bounds and sweeps static terrain. Changes can alter the player's obstruction/dodge behavior too. This is not the legacy NavMesh worker system.

## Frozen player mechanics

| Mechanic | Current base behavior |
| --- | --- |
| Input | WASD camera-relative movement; LMB light; E heavy; Space directional dodge (backward if stationary); Ctrl block; Shift sprint; RMB camera orbit; Tab lock; Q target |
| Dodge | 25 stamina, 0.32 s, 8 m/s; immune to Receive damage while in Dodge; obstruction can shorten travel |
| Movement | Walk 3.8, block 1.8, sprint 6.2 m/s |
| Sprint | 18 stamina/s, 0.7 s recovery delay; release Shift and regain >=20 stamina after exhaustion |
| Ready recovery | 24 stamina/s, or 7 while blocking, except sprint recovery delay |
| Sword light/heavy | cost 16/30; windup .24/.65 s; recovery .48/.85 s; reach 2.3/2.65 m; damage 18/32 |
| Spear light/heavy | cost 21/36; windup .38/.85 s; recovery .65/1.05 s; reach 3.4/3.8 m; damage 20/36 |
| Axe light/heavy | cost 24/42; windup .5/1 s; recovery .85/1.3 s; reach 2.1/2.3 m; damage 24/44 |
| Guard | Frontal ready block costs 24 light / 42 heavy; insufficient stamina causes damage and .9 s guard-break stagger |
| Hulk poise | Two heavy hits in one armored windup interrupt; a single heavy does not cancel it |

These are base profiles; scenario scales, limb rules and loot remain applicable. Preserve player action selection (dodge takes priority), input rejection during commitment, facing, hit-once behavior, and camera/pause/reset semantics. Automated tests help detect mechanical regression but cannot certify subjective feel: retain a manual Duel/Weapons/Hulk before/after comparison for future changes.

## Test anchors

EditMode: BlightDuelTests, BlightSprintTests, BlightWeaponTests, BlightPatrolTests, BlightDefenseTests, BlightTerrainTests and BlightExpandedNavigationTests.

PlayMode: BlightCombatFeelTests, BlightLabSmokeTests, BlightCompanionPlayTests, BlightHulkPlayTests, BlightGatewayPlayTests, BlightAdaptiveDefenseTests, BlightCompanyPlayTests/FieldTests, BlightVillageDefenseTests, plus limb/loot/hero/weapon and Sidekick coverage. Tests generally construct disposable lab GameObjects, advance fixed steps and destroy them in teardown; legacy integration tests also exercise real NavMesh components.

## Live Sidekick player presentation

The CombatDemo scene and newly created combat sandbox include SidekickDuelBridge with a project-owned PF_SampleFace prefab variant. The bridge hides only the original player renderers, disables the avatar's independent animation/physics drivers, and reads the existing player visual/action state. Bone posing and a generated cosmetic weapon do not move the actor root or resolve damage. Companions retain their original visuals; LimbDamage retains its specialized rig because it has no compatible animation source. Scenario rebuilds reinstall the model, disabling the bridge restores original renderers, and pause freezes the last live pose. Rehearsal remains an explicit separate mode. See SIDEKICK_PLAYER_INTEGRATION.md.
