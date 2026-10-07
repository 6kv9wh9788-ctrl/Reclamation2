# Reclamation: player-led kingdom direction

Confirmed with Thomas on 2026-09-28. This direction supersedes treating the crowd command demos as the main game loop.

## Player and command

The player directly controls one character. Personal combat, exploration, relationships and operations influence a growing kingdom. Military orders pass through commanders, rather than requiring the player to move individual squads through an RTS camera.

The intended hierarchy is **Army -> Battalion -> Company -> Platoon**. Higher levels become relevant as the kingdom grows. Commanders interpret objectives, assign subordinate responsibilities, act within available forces and knowledge, and report outcomes or decisions needing the player's attention. Defense, offense, scouting and peacekeeping are distinct responsibilities. Orders should preserve action commitment and the approved player combat feel.

Start small: one village and a small platoon with a named commander. The founding prototype represents that formation with a commander and seven soldiers; this is a reduced test formation, not a decision about final unit sizes.

## Separate development paths

- Civilian: **Village -> Town -> City -> Metropolis**.
- Military: **Outpost -> Fortress -> Citadel**.

Intermediate stages remain to be designed. Military facilities are a separate specialization, not mandatory civilian settlement tiers. A kingdom can contain multiple settlements with different civilian and military development levels. The player expands through play; the eventual game includes multiple holdings, not just one endlessly upgraded settlement.

## Research

Research should unlock ways to employ soldiers and civilians and improve their effectiveness. Examples include new duties, training, logistics, construction methods, and coordination capabilities. Avoid making every discovery a generic damage percentage. Its gameplay effect must be observable.

The first prototype implements two small examples:

- Work crews: unlock assigning two civilian builders to a storehouse project.
- Patrol doctrine: unlock a pair's peacekeeping route and construction of a military watchpost, the first outpost facility.

These examples demonstrate role unlocks; they do not constitute the final technology tree or its balance. Later efficacy improvements should be measured through task outcomes while preserving the player's approved combat timings.

## Current playable boundary

The Founding scenario restores the third-person player experience and introduces one commander, a recover-and-return supply operation, research and two visible projects. Civilian development stays at Village with a new storehouse; establishing the watchpost separately changes the military track to Outpost. There is no automatic Town promotion.

Not implemented here: Army/Battalion/Company delegation for this mode, multiple holdings, a full production economy, population growth, political peacekeeping, research trees beyond two prototypes, intermediate settlement stages, or a unified large-army simulation. The prior Company system remains a separate preserved prototype. Crowd rendering/perception/communication labs remain technical experiments; their existence is not proof that they have all been integrated into this scenario.

The approved Village Life follow-up adds named civilian workers with visible provision deliveries, Mara's fitness-based duty assignments, and manual checkpoints that resume on launch. See [Village life and checkpoints](VILLAGE_LIFE.md) for implementation boundaries and validation. These additions do not expand the command hierarchy or settlement tiers.

## Next acceptance gate

Before expanding to a second holding, Thomas should be able to play the founding loop and recognize his intended game: control his character, entrust an objective to Mara, see subordinate roles change, return supplies personally, and see research produce a concrete village improvement. Review the player's sense of agency, commander autonomy, reporting clarity, and whether the military/civilian distinction is clear. Choose final formation sizes, intermediate development stages and research pacing after that review. Also verify that saving, quitting and relaunching preserves the founding village and its people.

## Delegated defense follow-up

Defend town now includes garrison, reserve, and doctrine-unlocked rotating patrols. Mara recalls patrols for locally observed threats and prioritizes posts when understaffed. The player-facing HUD separates status from commander/council details; optional developer diagnostics remain available for recordings. See [Delegated defense](DELEGATED_DEFENSE.md). Training and experience remain deferred.

## Playable defense event

The north-road raid is one opt-in encounter after developing the village. It tests Mara's defense response alongside the directly controlled player. Existing combat remains unchanged; completion and friendly casualties can be saved. See [Founding raid](FOUNDING_RAID.md). Siege/building damage, recurring waves and rewards are not implemented in this step.

## Map scale and faction direction

The approved world direction adds 8-12 eventual human/bot faction slots, each centered on its player character and command hierarchy. Factions pursue expansion, conquest and affinity alliances. Even small maps should support several civilian settlements and separate fortresses per faction, with armies of at least 128 each. The first separate survey examples are a 1.6 km forest valley and a 3.2 km mountainous coast, with food/wood/stone near clearings and rarer gold/niter deposits. See [World atlas](WORLD_ATLAS.md) for the exploration prototype and explicit limits. Its abstract strategy and static army yards are not integrated combat or networking.
