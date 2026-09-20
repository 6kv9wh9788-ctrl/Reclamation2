# Slower pacing and the Patient Zero opening

Apply after the autonomous-combat checkpoint. Existing scenes do not need rebuilding.

## Pacing

Every new Play session starts at **0.5x**. Both neighborhood and outbreak panels now offer 0.25x, 0.5x, 1x, 4x, and 12x; the labels display fractions correctly. The shared clock scales disease progression, navigation speeds, combat action times, stamina, and perimeter work/damage. Camera movement remains independent.

The original 1x remains available for direct comparison. No extra disease-clock slowdown is included yet.

## Awareness in Patient Zero

CombatDirector now defaults to Require Witness. Existing combat-enabled Patient Zero scenes pick up this new field automatically. On that component in the Inspector, unchecking Require Witness restores immediately recognized enemies for comparison.

- **Unaware:** normal routines; survivors do not read another person's hidden infection state to select attacks.
- **Suspicious:** visible symptoms, a turned appearance, or observable withdrawal within eight metres and a clear navigation sight line cause suspicion. Survivors keep distance within 2.5 metres, without attacking a sick human.
- **Alerted:** a zombie lunge alerts its intended victim and witnesses within eight metres who have a clear sight line. Someone arriving during the lunge/bite can also recognize the danger. Recognition persists for that survivor for the rest of the Play session.
- There is no instant map-wide alert, radio system, or automatic relay between witnesses. Hold and disengage orders still apply. A survivor can defend against the first lunge before a bite lands.

Sight uses a straight navigation ray plus the current perimeter contact blocker. This is a prototype for ground-level obstacle occlusion, not a full visual perception/stealth system.

The existing diagnostic infection counters and event log still expose simulation state to the player; this checkpoint changes NPC knowledge, not the entire interface into a fog-of-war view.

## Withdrawal

An infected human begins seeking solitude after 55% of incubation has elapsed, and can continue withdrawing while symptomatic. The behavior chooses reachable ground with fewer nearby people instead of teleporting or selecting an inaccessible hiding place. It checks alternatives every three simulation seconds and rests when its current location is quiet.

Movement labels show Seeking somewhere quiet / Keeping to themselves. Visible symptoms retain their existing presentation. Isolation and evacuation take priority, and explicit combat orders/active fighting can interrupt the withdrawal. This behavior applies only while local-awareness combat is enabled. Turning ends withdrawal and uses the existing zombie pursuit behavior.

## Compatibility and scope

CombatEncounter disables the witness requirement at startup, including on previously saved Civilian vs One, Veteran vs Three, and Protect Civilian labs. Those labs still start with known enemies, but use the new slower default speed. Existing routine tests explicitly select 1x to preserve their original real-time deadlines.

Brute health, attack damage, and stagger behavior are unchanged. A lone brute can still lose to several alerted survivors. Stagger resistance, a sweeping shove, stalker AI, and other post-turn tactical behavior are the next tuning layer, not part of this patch.

## Test

1. Run EditMode and PlayMode suites. Eight new PlayMode tests cover recognition, local/occluded witnesses, symptoms, withdrawal, evacuation, fractional clock pacing, and saved-lab compatibility.
2. Open your combat-enabled PatientZeroLab and Play at the default 0.5x. No reinstall is needed.
3. Follow the visitor through incubation. Look for withdrawal before visible symptoms.
4. Observe the first lunge: nearby witnesses should react, while distant/occluded survivors remain unaware or merely suspicious.
5. Try 0.25x and pause; compare with 1x. Open an existing combat lab and confirm it still begins as a fight.

Unity is unavailable in the patch-authoring environment. Static checks and patch application are verified there; the new Unity tests and scenario behavior need editor verification.
