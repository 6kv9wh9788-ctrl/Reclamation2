# Articulated models and limb combat prototype

Install on top of the five-scenario Blight playtest batch. Open the existing
Blight Combat Lab and choose **Limb damage — articulated models**. No new scene
or asset import is needed. Models are generated from Unity primitives, with
shoulders, elbows, hips and knees. These are gameplay prototypes, not final art.

## After-work checklist

1. Let Unity compile, then run all EditMode and PlayMode tests. New coverage is
   `BlightLimbTests` and `BlightLimbPlayTests`; the scenario reset test now covers six scenarios.
2. Choose Limb damage. Press **T** for a stationary opponent; approach to roughly
   two metres. **Z** cycles arm, leg and torso swing heights. **LMB** is light;
   **E** is heavy. The selected height locks when an attack begins.
3. Aim at arms and land two heavy cuts on the same arm. It should disappear,
   cancel the enemy's current attack, and remove heavy attacks. Remaining-arm
   attacks still work. Approach from the other side to target its other arm;
   losing both arms leaves a short-range bite.
4. Reset with **R**, enable stationary practice again, select legs and land two
   heavy cuts on the same leg. The first should cause a limp; the second forces
   crawling. Turn practice off: the creature approaches slowly and can still
   attack at close range. Standing arm-height swings can miss a crawler: aim low.
5. **G** toggles reduced gore (default on). Reduced gore hides detached parts and
   uses dark caps. Turning it off shows detached prototype limbs and muted red
   caps, with no blood spray. Damage and enemy behavior must remain identical.
6. Check a swing beyond reach misses, each swing damages only once, pause freezes
   combat, and reset restores limbs and health. Debris expires after eight seconds
   of active simulation; reset clears it immediately.
7. Revisit Duel, Squad, Hulk, Weapons and Patrol to confirm their previous behavior.

## Scope and known limits

- The new models and limb rules are isolated to this sixth scenario. The original
  five scenarios, including Hulk telegraphs, retain their models and combat tuning.
- Player sword damage in this scenario follows swept blade contact against body
  regions during the first 0.22 seconds of recovery. Windup, overall recovery,
  stamina costs and dodge duration are unchanged. This deliberately tests a new
  contact window before adopting it across the game.
- A limb hit deals 30% normal health damage and 18/35 light/heavy integrity damage.
  The test thrall has 160 health so limb outcomes can be inspected. Arms have 50
  integrity; legs have 60. Torso hits deal normal health damage.
- Sword only; no player dismemberment, head severing, imported meshes or final
  animation assets. Enemy attacks still use the existing directional reach check.
- Practice mode prevents new enemy decisions; an already committed action finishes.
  Reset returns to live combat, arm aim, intact limbs, and preserves gore preference.
- Companion command polish remains deferred to its own sprint.

## Validation boundary

The package includes regression tests for Unity. Local syntax, contact-geometry,
and patch-application checks do not substitute for Unity compilation or the
EditMode/PlayMode runs. Please report the first Console exception and failing
test name if either test suite fails.
