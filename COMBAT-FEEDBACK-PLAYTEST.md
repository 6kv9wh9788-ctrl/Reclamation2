# Combat feedback pass

Install on top of the limb-models update. Uses the same six scenarios and scenes.

Changes:
- Brief per-character hit tint; small head recoil for articulated models.
- Distinct HIT, BLOCK, DODGE, INTERRUPTED, ARMOURED, GUARD BROKEN and LIMB LOST cues.
- Color and text cues above enemies for windup, sweep, smash, recovery and stagger.
- Quiet generated prototype hit/block/break sounds; M toggles audio, including while paused.
- Pause freezes feedback and pauses sound. Reset clears feedback and preserves mute.

## Test checklist

1. Run EditMode and PlayMode suites, including BlightFeedbackTests.
2. Duel: hit, block and dodge. Cues should match the outcome; misses should not sound like hits.
3. Hulk: check SWEEP versus SMASH and the OPENING cue after impact. The first heavy
   hit during an armoured windup must say ARMOURED; an actual windup cancellation
   must say INTERRUPTED. A heavy hit outside a windup is just HIT.
4. Limb damage: use T for stationary practice, then two heavy arm cuts. Look for
   head recoil and LIMB LOST. Check that existing limb targeting still feels the same.
5. Press M to mute/unmute. Pause during a hit, resume, then reset or change scenario.
   No stale flashes or old sound should carry into the next encounter.
6. Squad: make sure simultaneous impacts remain readable and volume is comfortable.

Attack/dodge timings, damage, stamina, hit volumes, enemy decisions and companion
commands are unchanged. Cosmetic recoil never moves a sword or limb hit region.
Sounds are temporary synthesized placeholders; no external assets are required.
Sound events are rate-limited during crowded fights. Visual feedback can be disabled
through CombatFeedbackEnabled for automated comparison; it is on by default.

Validation: patch application and source checks are performed outside Unity.
Unity compilation, audiovisual quality and the included test runs still need
validation in your editor. Final impact cues freeze with the encounter end screen.
