# Reclamation combat and character design principles

Standing direction agreed with Tom, 23 September 2026. Apply this checklist to
future character, animation, combat, controls and encounter iterations.

## Creative intent

A stylized supernatural-blight action RPG, centered on a player hero fighting
alongside autonomous companions under broad orders. Combat must feel weighty,
responsive and consequential, with readable telegraphs and useful equipment
choices. Base building supports that experience. Design for eventual larger
battles without sacrificing the clarity of a small duel.

## Character readability

- Distinct silhouettes and consistent shape language: bulk communicates strength
  and resilience; lean shapes suggest speed and agility. Equipment, posture and
  gait should distinguish roles before names or health bars become readable.
- Grounded animation: anticipation, contact, follow-through, planted feet,
  attached hand grips and purposeful transitions. Convey acceleration and mass
  without adding sluggish input handling or silently changing combat timings.
- Keep modular armour, weapons, faces, hair and future body/gender options
  compatible with the rig. Judge silhouettes at actual gameplay camera distance.
- Do not use colour alone to identify role, faction or attack type.

## Predictable mechanics and ergonomic controls

- Document speed, reach, damage, stamina costs, dodge distance and recovery.
  Visual weapon reach, telegraphs and gameplay contact should agree.
- Put frequent actions within easy reach. Keep camera, block, attack, dodge and
  sprint from fighting over the same input. Later remapping/controller support
  must preserve these roles and update the prompts shown on screen.
- Acknowledge accepted inputs promptly; distinguish buffering, insufficient
  stamina and committed actions. Feedback must not imply that a rejected attack
  actually started.
- Teach through behaviour, telegraphs, poses, contextual cues and consistent
  outcomes. Use text for optional detail rather than making it the only teacher.

## Impact loop

1. Anticipation: readable windup, planted stance and low-intensity audio/visual
   cues. Subtle camera framing can support major attacks without fighting manual
   camera input or obscuring other threats.
2. Contact: confirm a real resolved hit, block or guard break immediately. Align
   sound, visual reaction and optional camera pulse to the same impact event.
   Particle position should come from actual contact data where available.
3. Decay: let the attack follow through and effects settle promptly. Use smooth,
   fast shake decay and appropriate recovery or stagger animations.

Impact feedback and gameplay interruption are separate. A light hit can visibly
recoil without cancelling a Hulk's committed attack. Stagger, guard breaks and
knockback must follow explicit balance rules, not happen simply because an
impact effect played. Do not stun-lock enemies with cosmetic feedback.

## Hit-stop direction for a future pass

Explore short, configurable pauses scoped to attacker/victim presentation before
using a global simulation pause in squad battles. A nominal 3–10 frames at 60 Hz
is roughly 0.05–0.167 seconds: tune in seconds, not rendered frame counts.
Begin at the short end; stronger impacts may justify more emphasis. Validate
input buffering, shared animation/strike timing, repeated hits, pause/resume,
death and reset. A presentation freeze must not make gameplay damage occur at a
visibly incorrect moment. Do not globally alter Unity timeScale for routine hits.

## Direction, particles, audio and haptics

- Directional recoil should match the strike's trajectory and impact side.
  Physical knockback changes spacing and needs collision/navigation handling;
  cosmetic bone reactions should not move authoritative weapon contact geometry.
- Contextual bursts: sparks for metal/guard, muted organic effects for flesh,
  distinctive debris for blighted armour. Respect reduced-gore preferences.
- Generic range/cone attacks do not yet provide exact weapon-surface intersection.
  Do not present an estimated effect position as true collider contact. Extend
  impact event data before claiming precise contact-localized bursts.
- Separate sound profiles for swings, blocks, flesh hits, armour, guard breaks
  and heavy impacts. Scale intensity meaningfully; cap concurrent voices.
- When controller support is added, use optional impact-scaled vibration with
  reliable stop/reset on pause, focus loss, disconnect and scene teardown.

## Accessibility and large-battle limits

- Independent shake, bob, flash and eventual haptic controls; keep essential
  telegraphs readable with these effects disabled. Reduced-gore option remains.
- Every meaningful action needs readable feedback, not maximum-intensity effects.
  Reserve the strongest emphasis for significant hits, guard breaks and kills.
- Prioritize the player's nearby combat for camera/audio emphasis. Distant or
  off-screen events must not shake the camera or drown out imminent threats.
- Pool/budget future particles, debris and voices. Verify a duel, the squad
  mission and larger encounters separately; rendering benchmarks do not prove
  full battle simulation performance.

## Current combat-feel iteration

Implemented for playtesting: RMB camera orbit, nearby lock-on framing, Ctrl block,
Shift sprint, shared stamina, player block/hit camera pulses, optional gentle
movement bob, thrall heavy-attack evasion, Hulk bracing, cosmetic head recoil,
and separate six/twelve-hostile scenarios.

Current movement tuning:

| Mechanic | Value |
|---|---|
| Walk / block movement | 3.8 / 1.8 m/s |
| Sprint | 6.2 m/s, 18 stamina/s |
| Post-sprint stamina recovery delay | 0.7 s |
| Restart after sprint exhaustion | Release Shift and recover to 20 stamina |
| Dodge | 25 stamina, 0.32 s at 8 m/s; up to 2.56 m before obstruction |
| Stamina capacity | 100; shared by movement and combat |
| Camera bob | Default peak about 0.018 m; adjustable/off |

Weapon-specific costs, timing, reach and damage remain in the equipment panel
and BlightEquipment/BlightLootCatalog. Jumping is not currently implemented.

Not implemented in this iteration: hit-stop, directional full-body recoil or
physical knockback, contact particles, a new layered audio pass, haptics,
remapping UI, locomotion acceleration/transition overhaul. These require
separate implementation and validation; this document sets their direction.

## Review checklist for each iteration

- Can the player recognize the role and attack at gameplay distance?
- Does the first visible response follow input promptly, with clear commitment?
- Do grips, feet, weapon edge and impact direction agree through the action?
- Is the result clear: miss, evade, block, hit, armoured hit, stagger or defeat?
- Do effects peak at contact and settle before they obscure the next tell?
- Do the same rules remain understandable with camera motion/audio disabled?
- Are stamina, range, timing and equipment tradeoffs predictable?
- Does a mixed encounter stay readable and within effect/performance budgets?
- Do pause, focus loss, reset and scenario switching clean up transient effects?
- Record observed playtest results separately from design targets and unrun tests.
