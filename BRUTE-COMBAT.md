# Brute combat and survivor spacing

Apply after the outbreak-pacing checkpoint. Existing combat-enabled scenes gain the behavior without rebuilding. Brute health remains 180; this patch changes its actions and vulnerability windows.

## Try the lab

Outside Play Mode choose **Reclamation > Combat > Create Three Civilians vs Brute Lab**. This saves a new uniquely named scene, preserving your older labs and Patient Zero. The scenario contains three ordinary civilians (orange shirts) and one brute. Threats are recognized immediately, as in the other combat labs.

Play at the default **0.5x**. Follow the brute or zoom in after collapsing the status panel. Watch for:

1. The first hit staggering the brute, with later hits still causing damage without constantly resetting that stagger.
2. The brute planting its feet, spreading its arms, and winding its torso back before sweeping.
3. Survivors stepping outside its reach, or being shoved away if caught.
4. A visible bent-over recovery pose, giving survivors an opening to counterattack.
5. Teammates still being able to interrupt a bite, even during stagger resistance.

Restart Play to reset. Use Hold here / Disengage to compare standing your ground with withdrawing. Then revisit Patient Zero with the Visitor set to Brute. Early containment remains a valid outcome; neither side is guaranteed to win this lab.

## Rules in this checkpoint

| Mechanic | Initial tuning |
| --- | --- |
| Stagger resistance | After being staggered by a hit, a brute resists further hit staggers for 2.5 simulation seconds; damage is unchanged |
| Rescue exception | An active bite always remains interruptible by a successful teammate strike/shove; victims can still break free |
| Sweep trigger | At least two reachable, unobstructed humans within 2.4 metres, and the sweep is ready |
| Sweep warning | 1.2 simulation seconds; facing locks at the start, so it does not track a dodging target |
| Sweep coverage | 2.4-metre radius, 220-degree forward arc; the rear is outside the hit area |
| Sweep impact | 10 health and 25 combat stamina damage, followed by up to 2 metres of outward displacement over 0.45 simulation seconds |
| Sweep infection | None: infection still requires a landed bite |
| Recovery opening | 1.7 simulation seconds, with 25% extra incoming damage; hits do not shorten this recovery |
| Repeat timing | 6 simulation seconds from the start of a completed sweep; an interrupted windup can be retried after stagger |

All timings use the same paused/scaled simulation clock. Sweeps require clear contact lines, and knockback clips to navigable ground instead of warping through walls. A blocked survivor may travel less than two metres. This is NavMesh displacement, not a ragdoll or physics impulse.

The first unguarded strike can interrupt a sweep. The brief resistance afterward lets the brute attempt another clearly telegraphed sweep instead of remaining permanently stunned. Resistance never reduces incoming damage or protects a bite from rescue.

## Survivor behavior

- Ready survivors who see a sweep attempt a stamina-costing dodge beyond its reach. Those already outside the forward danger area wait for the opening instead of immediately charging back in.
- A committed attack, grab, or recovery is not cancelled for a free dodge. Exhaustion, congestion, or a wall can prevent escape.
- Crowded survivors seek separate nearby melee positions, checking both occupied space and teammates' dodge/reposition destinations. Normal repositioning respects the Hold radius; emergency evasion may move beyond it.
- Position choice penalizes standing close to additional zombies. This is local spacing, not a full formation/flanking planner.
- The existing preference for rescuing a grabbed teammate remains; spacing never delays an immediately reachable bite interruption.

Sweeps can down an uninfected survivor at zero health. The status now says Downed; needs rescue. Healing/carrying downed survivors is not implemented yet; restart the lab to reset. A downed survivor can still be bitten and progress through infection.

## Verification

Run both EditMode and PlayMode suites. Added tests cover the forward/rear arc, windup, pause, multiple hits without infection, locked facing, dodge destination spacing, repeated-stagger resistance with a bite-rescue exception, the recovery damage opening, wall-bounded knockback, and melee spacing.

Static checks and clean patch application are performed in the authoring environment. Unity is not available there: runtime tests, animation readability, and win/loss balance still require editor verification.

This checkpoint does not add XP, levels, weapons, new character assets, or a guaranteed brute victory. Those remain separate progression and presentation work.
