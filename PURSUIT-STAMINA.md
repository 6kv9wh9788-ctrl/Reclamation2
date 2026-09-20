# Pursuit stamina checkpoint

Apply after the navigation-height fix. Open the saved PatientZeroLab and press
Play; no scene regeneration is needed. This is a first balance pass, not a
promise that every zombie catches every civilian.

## Current tuning (second balance pass)

| Setting | Human | Zombie |
| --- | --- | --- |
| Sustained escape/pursuit speed | 2.8 m/s | 3.6 m/s |
| Sprint/burst speed | 5.0 m/s | 6.2 m/s |
| Full burst duration | 4 simulation seconds | 3 simulation seconds |
| Recovery from empty | 12 simulation seconds | 8 simulation seconds |
| Automatic activation distance | 5 m from threat | 3 m from prey |

The previous infinite human escape speed was 3.8 m/s, faster than a zombie's
3.3 m/s. These settings give tired humans a disadvantage while retaining their
longer burst and the existing directional escape search.

A burst needs a full charge to start. It drains only during movement on a route.
Leaving the activation range, stopping, isolation, symptoms, or resuming a routine
ends the burst; partial charges must also refill completely before restarting.
Recovery occurs while jogging or resting. Pausing freezes both use and recovery.
An actor that turns starts with the zombie profile's full charge. Exposed humans
keep the human profile until transformation, avoiding a stamina-based infection
reveal. Neutralized actors no longer advance stamina.

Simulation seconds scale with 1x / 4x / 12x, just like movement. They are not the
clock's displayed game minutes. Human routines retain their existing walking speed.
The panel shows Ready, Sprint/Burst, or Recovering, with charge percentage.
Tuning values are centralized at the top of OutbreakAgent.cs for this prototype.

There is no dodge invulnerability, random evasion roll, melee damage, or guaranteed
catch in this update. Existing contact-exposure rules still govern infection.

## Verify

Run 35 EditMode and 28 PlayMode tests. Five new stamina tests cover exhaustion,
full recharge before restart, interruption, invalid/paused time, and equivalent
elapsed simulation time. Two component integration tests cover speed changes,
paused stamina, and zombie exhaustion. Unity execution remains a local editor gate.

Watch a pursuit at 1x. Both actors should occasionally sprint, then return to their
sustained speeds. Pause midway and confirm percentages stay fixed. Try 4x and 12x;
the same bursts finish faster in real time. Use a fresh Play session for comparisons
so prior infections, positions, and fatigue do not bias the result.

## Why the second balance pass changes zombies

The original sprinting zombie was only 0.2 m/s faster than a sprinting human,
and its two-second burst could begin five metres away. It could gain little
ground before running out of charge. The revised profile reserves the burst
until three metres, gains 1.2 m/s against a sprinting human, and lasts three
seconds. Sustained speed rises to 3.6 m/s and recovery drops to eight seconds.
Human movement and the 1.5-second contact exposure requirement are unchanged.

As a simplified straight-line estimate, closing from 3 m to the 1.4 m contact
radius takes about 1.33 seconds at that relative burst speed, leaving about
1.67 seconds of the burst for contact. This excludes acceleration, navigation,
avoidance, stopping distance, and human fatigue; it is a tuning rationale,
not a measured Unity outcome or guarantee of infection.

Apply this tuning patch after the initial pursuit-stamina update. No scene
regeneration is needed. Start a fresh Play session and compare pursuits at 1x.
