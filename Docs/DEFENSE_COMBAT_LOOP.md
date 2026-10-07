# Village defense combat loop

This is an opt-in extension of the Founding combat village. The world atlas and
its separate 25-guard simulation remain unchanged. Launch with `--defense-loop`
and an explicit `--village-checkpoint` path to isolate the campaign. The delivered
launcher uses `%LOCALAPPDATA%/ReclamationDefenseLoop/checkpoint.json`.

## Playable loop

The declared scenario starts developed: Mara, seven soldiers, two provisioners,
a storehouse, watchpost and ten food. It skips the earlier supply-fetch sequence
to focus playtesting on combat, intent and progression. Two authored three-raider
operations are available. No recurring raid generator is claimed.

At the council choose Hold village or Intercept road. Twenty seconds of warning
precede the raid. Interception deploys Mara and mobile troops to the north road;
two fixed guards remain (three with Watch captain). Hold retains the existing
observed-contact response. Conflicting directives are rejected during operations.
Committed actions, collision, defense, attacks and damage use existing combat.

Five cumulative seconds of live hostile presence within six metres of the village
center breaches stores. Repelling a breached raid loses up to five food. Food
production still works normally. Buildings and civilians have no damage model.

Completion awards 100 player XP, plus 50 for protected stores and 25 for spending
at least three seconds within ten metres and line of sight of living raiders.
Mara receives 100 plus the protection bonus only if she survives. This is a small
objective/proximity contribution rule, not a full damage/assist scoring system.
There are no kill-count rewards. Rewards occur once per completed operation.

## Progression and recovery

One player choice and one commander choice unlock at 100 XP. The second operation
requires both choices; after two completions this slice ends. Choices are permanent
within a checkpoint timeline, with no full tree or respec yet.

- Field dressing: once per operation, one food restores up to 20 health and about
  39 stamina through existing recovery to the most wounded ready living ally
  within 3 m. Both must be at least 8 m from all living enemies. No revival or
  committed-action cancellation.
- Forward rally: while within 12 m of living Mara, place the reserve objective
  at the player's position in the central approach corridor (x +/-6, z -10..7).
  Fixed garrison is unaffected; the order is repeatable during that operation.
- Watch captain: three fixed guards rather than two, reducing mobile strength.
- Careful relief: reserve wounded soldiers at <=55 health instead of <=35. It
  grants no healing and trades fighting strength for earlier withdrawal.

Paid recovery at the council consumes two food and restores living fighters using
existing recovery. It requires the safe checkpoint gate and is unavailable during
a raid. Casualties remain dead. Saving without recovery preserves wounds.

## Persistence and implementation

`DefenseCampaign` is the bounded checkpoint DTO with validation. `FoundingSave`
retains version 1 and adds an explicit `hasCampaign` marker because Unity's inline
serialization can materialize an empty object for null nested data. Ordinary and
older saves ignore absent campaign data. Campaign saves require a developed village
and consistent completed-raid state. No mid-raid save is supported.

`BlightDefenseCampaign` owns mission state, objective tracking, rewards, perks and
scoped assignment overrides. `BlightFoundingRaid` supplies the existing event actor
lifecycle. `BlightDefenseCampaignHud` provides the Operation / perks panel. The
existing UI exclusion test covers its expanded rectangle. Reset starts a fresh
unsaved scenario; normal startup resumes the explicitly selected checkpoint.

Mara's death prevents a follow-up operation; there is no replacement commander.
Reload an earlier checkpoint or restart to try again. No automatic save occurs.

## Verification

Run `DefenseCampaignTests` alongside FoundingRaid, DelegatedDefense, VillageLife,
FoundingVillage and BlightCombatFeel PlayMode fixtures. See delivery REPORT.md for
actual counts and prior failed attempts. The new tests cover bounded rewards and
save/load, protected posts, pause, both perk types, paid recovery, casualties,
breached stores and invalid progression.

`--campaign-smoke --output <folder>` fights two Hold operations through actual
combat AI and public player movement/attack APIs, chooses perks and saves/loads.
Add `--intercept` for interception and the other perk pair. A separate process
with `--campaign-resume-smoke --village-checkpoint <file>` verifies saved progression.
Smoke setup uses a declared developed scenario; event enemies receive only actual
combat damage. Screenshots are evidence of layout/state, not physical input or
subjective combat-feel certification. The legacy `--raid-smoke` remains separate.

Not included: atlas combat integration, large armies, full skill trees, multiplayer,
generalized faction AI, building destruction, new combat timings or FPS certification.
