# Combat direction and first integrated map slice

Confirmed direction, 29 September 2026: a single embodied player fights alongside
troops whose training, survival and role in the kingdom matter. The large maps,
settlements and company hierarchy provide the context for combat, rather than
remaining a separate sightseeing demo.

## Seven goals

1. Guide each exchange: choose an attack direction, judge reach and place a block.
2. Movement contributes to a blow: closing speed and contact quality matter;
   eventual mounted combat rewards lining up the pass, distance and swing.
3. Weapons demand distinct habits: spear reach, sword speed, shields and two-handed
   axe commitment create readable tradeoffs, including in cramped spaces.
4. Decisive, restrained feedback: clear contact sounds, interruptions and falls;
   avoid effects that obscure what connected and why.
5. Player mastery grows alongside character progression. Recognizing openings and
   managing distance should remain valuable after acquiring perks.
6. Battles produce unscripted reversals: lost mounts, broken shields, pursuit and
   refuge among friendly infantry should arise from interacting systems.
7. Victory changes the campaign: wounds, casualties, trained troops, prisoners,
   supplies and loot connect a fight to the larger game.

These are design targets, not a claim that every listed system exists today.
Directional attack/block selection, velocity/contact damage, mounted combat,
breakable shields, prisoners and recruitment costs require future implementation.

## Current map integration

Launch the existing WorldAtlas scene with `--atlas-defense`. It retains the small
forest valley (1.6 km, four faction slots) and medium mountainous coast (3.2 km,
eight slots), procedural layouts, resources, villagers, wildlife, clock and scout
map. The player fights in the actual home settlement, with the atlas characters.

One local company contains a company commander and four six-person platoons.
Each platoon includes its leader. During an operation the current reserve platoon
responds; other platoons hold their current locations and defend nearby contacts.
This is a local tactical adapter, not full Army/Battalion command or all-map war.

At the command post choose interception or store defense. A 20-second warning
precedes six raiders approaching the south road. Interception deploys the reserve
forward; defense holds nearer the stores. Five cumulative seconds of raider
presence near the stores costs up to five food. Civilians and buildings do not
yet take combat damage.

Two operations comprise the slice. Completion gives 100 XP, with 50 for protected
stores and 25 player proximity/contribution XP. The commander must survive to earn
command XP. One choice each unlocks at 100 XP; both choices are required before
the second operation. No full perk tree is claimed.

- Player field dressing: once per operation, one food restores up to 20 health
  and about 39 stamina to a ready wounded ally within 3 m, away from enemies.
- Player forward rally: redirect the reserve to the player's position within
  the bounded village approach. Requires a surviving company commander.
- Watch captain: two reserve troops reinforce the stores, reducing the forward
  response. Existing posted platoons keep their duties.
- Careful relief: wounded soldiers withdraw at 55 rather than 35 health.

Recover at the command post for two food; dead soldiers remain dead. Saving
requires no active operation, a living player at command, and recovered stamina.
Checkpoints store this map's defense progression, food and all 26 participants'
health (player plus 25 troops). They are not a save of every world-system state.
Other settlements, abstract territory turns and the clock are not persisted.
Map switching starts or resumes that map's own checkpoint and can discard unsaved
defense progress. No mid-battle travel or save is available from the defense UI.

## Implementation boundary

`WorldAtlasDefense` adapts unchanged `DuelFighter` and `BlightEquipment` to atlas
movement, collision, AI decisions and visuals. Existing combat-lab source and its
movement/camera remain unchanged. Atlas movement uses its own building capsule
sweeps and terrain heights; this is not identical navigation to the combat lab.
Damage resolves once at windup completion using shared reach, facing, blocking,
dodge and stamina rules. The first atlas integration equips swords only.

`AtlasCombatPoses` and `AtlasCombatOutfits` are new baked pose banks derived from
the approved joint mannequin. The original walking and guard libraries remain
unchanged. Presentation does not control damage. Defeat currently uses a simple
sideways collapse; sound and contact polish need a dedicated pass.

The prior Founding combat lab remains available. Its defense campaign source is
retained as a separate tested prototype; the delivered map build uses the atlas
adapter and map-specific checkpoint rather than loading the old lab scene.

## Next combat acceptance gate

After playtesting this map connection, develop directional combat in a controlled
duel within the same world: at least three distinguishable attack directions,
corresponding defensible directions, clear windup/impact/recovery signals, and
no action-cancel or free-dodge regression. Validate the rules with deterministic
tests and player recordings before adding momentum or mounts. Measure attack
readability, missed-hit causes and survival against one then two opponents.

Later gates should compare spear/sword/axe tactics, then add constrained momentum
and contact quality, then shield durability and mounts, then campaign losses,
prisoners and loot. Preserve a reproducible test scenario for each mechanic.

## Synty character and role trial (29 September 2026)

Launch with `--atlas-defense --atlas-synty` to replace the player and first raider
in each operation with the existing project-owned `Player/SidekickPlayer` prefab.
Both maps support this opt-in presentation trial. Add `--atlas-synty-roles` to
replace the company commander, all four platoon commanders, one soldier per
platoon, all six raiders, and three pooled residents (farmer, woodworker/miner,
merchant). This is 19 Synty actors at peak, including the player. Remaining
soldiers and residents retain their lightweight models. The role flag implies
`--atlas-synty`. Without either flag, the prior atlas
presentation remains active. Use a separate `--defense-save-directory` for art
playtests to preserve earlier checkpoints.

`AtlasSyntyVisual` reads the same fighter state and owns only the avatar pose and
cosmetic sword/shield. It disables prefab animation/root-motion and physics
drivers, uses a fixed hand grip and outward elbow hints, and hides the replaced
prototype renderers. The sample has a saw prosthetic instead of a left hand:
instance-owned mesh copies remove that region, and shipped Synty knight forearm
and hand modules bind to its skeleton. Fog visibility also applies to the Synty raider. The old
`SidekickDuelBridge` and vendor assets remain unchanged. The shield represents
the existing frontal block; it does not introduce durability or directional
blocking. The animations are procedural, not a finished authored animation set.

This uses the sample character/outfit, not a verified match to the original
concept images, which must still be recovered. `AtlasSyntyRoles` assembles shipped
knight and plain-clothing modules on the existing skeleton: gold company armor,
closed helmets and pauldrons for platoon officers, lighter soldier variants,
three mixed-armor raider silhouettes and unarmed civilian outfits. Unhelmeted
roles still share the sample face. Civilian poses follow existing work/social
schedules and tools; guard torches follow the new left hand at night. No command
hierarchy, economy, equipment statistics or civilian schedule rules change.
The starter civilian modules are provisional, not the final historical wardrobe.

The player contains 23,073 allocated skinned vertices (including unused vertices
in the trimmed source mesh); role modules add renderers and skinning cost. This
bounded rollout does not certify the 256-friendly / 1,024-hostile performance
target. Mesh compaction, material/mesh batching, animation/rendering LOD and target
hardware profiling remain gates before wider rollout.

Run `AtlasSyntyTests` and `AtlasSyntyRoleTests` with `AtlasDefenseTests` and the existing combat/atlas
regression fixtures. `Reclamation.Editor.AtlasSyntyReview.Capture` uses the output
directory in `RECLAMATION_SYNTY_REVIEW` for disposable-scene pose renders.
`--atlas-defense --atlas-synty --atlas-synty-capture --output <folder>` captures
staged poses in the built forest map; these are presentation references, not
combat outcomes. Use `--atlas-defense-smoke --atlas-synty` separately for the
actual two-operation combat playthrough on both maps.
Use `--atlas-synty-roles` in place of `--atlas-synty` for the expanded role demo.
`Reclamation.Editor.AtlasSyntyReview.CaptureRoles` renders the role wardrobe in a
disposable scene using the same output environment variable.

### Company presentation pass (30 September 2026)

`--atlas-defense --atlas-synty-company` implies the role trial and replaces all
25 company members: one company commander, four platoon commanders and twenty
soldiers. With the player, six raiders and three pooled residents this is 35
Synty actors at peak. The earlier `--atlas-synty-roles` mode remains available
for comparison. Civilian coverage is still three residents, not the whole town.

Officers use their existing helmet plumes, armor and star/name UI; the misplaced
primitive head marker is removed. `SyntyReadiness` is presentation-only: peaceful
patrol lowers the arms and carries the sword in a simple hip scabbard, the raid
warning raises an alert stance, and an active engagement raises combat readiness.
Attacking, blocking, dodging, staggering and defeat take precedence immediately.
No draw delay, damage change or cancellation rule is introduced. Transitions
pause with the existing simulation. Night torches retain their hand attachment.

The company mode starts with compact status and Operations closed. B opens the
existing full controls; F3 still toggles the clearly labeled diagnostic box.
Victory no longer automatically opens Operations in this mode. Input exclusion
matches the visible compact panels, including the minimap and debug box. Camera
distance, orbit, movement and collision tuning are unchanged.

Run `AtlasSyntyPresentationTests` together with both earlier Synty fixtures,
`AtlasDefenseTests` and `BlightCombatFeelTests`. Built-player smoke and resume
use `--atlas-synty-company`. The `--atlas-synty-profile --atlas-defense` diagnostic
compares the prior role roster with the full company on both maps in the same
build: 120 warm-up frames and 600 sampled frames per case, uncapped/vsync off,
fixed visible lineup and pose updates. It records hardware, resolution, median,
mean and p95 frame intervals plus allocated vertices and renderer counts.
This is a local presentation diagnostic, not a combat or target-hardware benchmark.

### Village sword-and-shield practice (30 September 2026)

`--atlas-duel` starts a separate practice exchange on the existing village road.
It implies the Synty company presentation and disables checkpoint auto-resume.
1 selects one opponent, 2 selects two, G selects a stationary guarding opponent,
and R restarts the current drill. The practice HUD can switch between the same
forest and coast maps. Campaign orders/saves are absent from this mode; exchanges
do not complete defense operations or award XP. Company members do not attack or
receive practice strikes. Enemy and player damage, stamina, hit geometry, movement,
block, dodge and action timing still use the existing shared rules. The shield
drill intentionally suppresses opponent attacks so contact can be inspected.

`AtlasSyntyExchange` defines presentation weights for preparation, cut, follow-
through and return. Stationary committed strikes transfer the hips/chest above
fixed foot targets rather than shifting those targets at impact. This is not
terrain-aware foot IK. `ResolveAtlasHit` forwards the already resolved result to
the target visual: Blocked produces short shield recoil, Hit/Guard broken produce
bounded torso recoil. These cosmetic timers cannot interrupt or advance fighters
and freeze when simulation pauses. Existing attack timing and damage are unchanged.

Run `AtlasSyntyExchangeTests` alongside the previous three Synty fixtures,
`AtlasDefenseTests` and `BlightCombatFeelTests`. `--atlas-duel-smoke --output <dir>`
runs actual blocked strikes and one-/two-opponent exchanges on both maps, with
ordinary damage and checks for zero campaign progression. Separately run the
existing company combat/resume smoke to detect unintended campaign regressions.
Practice permits defeat and restart; no invulnerability is introduced.

### Duel readability follow-up

The Synty combat guard now lowers the hips above staggered support feet, turns
the chest slightly and brings the shield closer to the body. Resolved contacts
produce immediate, decaying recoil; actual blocks also trigger a short cosmetic
rebound on the attacking sword. Clean hits do not trigger that rebound. Neither
reaction changes fighter state, root movement, damage, stamina or timing.

Practice starts with floating Hit/Blocked labels hidden; F4 or the HUD button
toggles them for comparison. Health and stamina use a compact corner panel with
meters, and the empty screen beside that panel remains available for input.
F3 retains the developer diagnostics. Judge the exchange with labels hidden,
then enable them to compare the visual result with the resolved outcome.
This is still procedural posing with reach/cone hit resolution, not weapon-mesh
collision or terrain-aware foot planting. Exact contact at maximum reach and
opponent spacing remain limitations rather than silently retuned combat rules.

### Two-opponent practice readability

The two-opponent drill gives living opponents separate approach lanes, 32 degrees
either side of the initial practice heading. Lanes follow player translation but
do not swap when the camera or target lock turns. The existing obstacle/body
movement resolver still controls travel. Only Ready actors seek lanes; committed
attacks keep their position/facing and existing shared windup/recovery rules.
Attack starts have a minimum 0.32-second separation, while windups may overlap:
both opponents remain threats rather than taking exclusive turns. This changes
practice AI positioning and initiation cadence, not the player's timing or damage.

The policy applies only while both opponents are alive in the two-opponent drill.
One-opponent practice, the stationary shield drill and company battle AI retain
their previous policies. Restart clears the spacing timer and heading. This is a
bounded playtest before considering wider melee AI rollout; obstacles, sustained
player movement and arbitrary camera angles can still obscure combat silhouettes.

### Practice combat locomotion

Only the practice player and raiders enable `AtlasSyntyVisual.CombatMotion`.
The gait advances with travelled distance, with shorter lateral/backward steps
and separate foot lanes. Bounded foot yaw and hip lag soften Ready-state turns;
small torso balance, hip sway and breathing keep the guard active. Attack poses
retain their existing preparation/contact/recovery and fixed stationary support
targets. Cosmetic motion never rotates or translates the actor root or changes
fighter state. Zero simulation delta freezes these pose timers. Company soldiers,
civilians and campaign combat keep the prior presentation pending approval.

The practice smoke captures real blocking strafe/backstep inputs at two step
phases from front and side views on both maps. This remains procedural animation,
not world-anchored foot IK or a finished authored locomotion set. Instant root
facing and moving-to-attack transitions remain possible visual limitations.
