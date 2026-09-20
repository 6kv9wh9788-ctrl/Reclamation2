# Autonomous combat: first playable slice

Apply after the perimeter-defense patch. Existing saved scenes are not modified automatically.

## Start with the isolated labs

Outside Play Mode choose a scene from Reclamation > Combat:

- Create Civilian vs One Lab
- Create Veteran vs Three Lab
- Create Protect Civilian Lab (two veterans, one civilian, five zombies)

Each creates a uniquely named saved scene, preserving older labs. Press Play at **1x** initially; collapse the panel for a clearer view and use camera Follow/zoom. Blue shirts identify veterans, orange civilians, green zombies. Restart Play to reset. New labs have no hidden patient-zero seed: their starting zombies are explicitly authored.

Scroll down in the outbreak panel for combat health/stamina and orders:

- **Hold here:** captures the current location. The survivor engages threats within four metres of that anchor and returns to it; emergency exhaustion retreat can leave the area.
- **Disengage:** breaks contact using the existing escape planner, then waits for another order when nearby threats clear. It does not cancel a grab, attack windup, or recovery instantly.
- **Self defense:** handles nearby threats, allowing normal routines/evacuation when clear. This is the default when enabling combat in the existing outbreak scene.

The protection lab tests positioning and autonomous bite interruption. A dedicated Protect-person order, squad formations, and command-click movement are not implemented in this slice.

## What is implemented

- Human strike, crowd shove, movement-based dodge, recovery, and combat stamina.
- Zombie lunge, grab, telegraphed bite, and stagger. Teammate strikes/shoves interrupt a bite; capable survivors can break free if they have enough stamina.
- Infection occurs at successful bite contact, **not zombie proximity**, when combat is enabled. Existing symptomatic-human disease transmission is unchanged.
- Strength affects damage and grab escape; dexterity windup; agility dodge cost/speed; speed closing movement; intelligence threat awareness; endurance stamina capacity.
- Civilian and veteran presets share 100 health. Ordinary zombies have 80 health, brutes 180. Veterans have faster attacks, stronger strikes, more stamina, and faster grab escape—not invulnerability.
- Bites inflict 25 health damage and use the existing infection timeline. At zero health a human is downed in place while infection progresses; death/ragdolls, healing, and rescue-carrying are future work.
- Visible procedural poses on the current block humans: strike, shove/lunge, stagger, dodge, struggle, and exaggerated bite lean. These are prototype animations, not imported skeletal clips or a finished animation blend system.
- Attacks require proximity and an unobstructed navigation line; a complete detour around a wall is not permission to hit through it.
- A single simulation clock controls actions. Pause freezes action progress; camera controls remain usable. Inspect at 1x first; 4x/12x are time-compression checks, not cinematic viewing speeds.

Combat stamina and the earlier movement sprint charge remain separate for now. Experience accumulation, levelling, perks, weapons, concealment, cover tactics, and advanced squad coordination are deliberately deferred until the core encounters are tuned. The veteran-vs-three result is a tuning target, not a measured guarantee.

## Enable in Patient Zero after testing

Open the saved PatientZeroLab outside Play Mode, choose Reclamation > Combat > Enable Combat in Open Outbreak Scene, and save. The installer adds CombatDirector plus Combatant components without replacing the scene. Attributes can be adjusted on each Combatant in the Inspector before Play.

Do not manually add only a CombatDirector: every population member needs a Combatant to participate. Use the installer. Disable CombatDirector to return to the old proximity-infection behavior. Construction workers yield to combat; healthy sheltered workers in Self defense can still build when no nearby threat needs attention.

## Verification and playtest checklist

Run both Unity Test Runner suites. Added combat tests cover preset differences, pre-contact infection timing, bite interruption, grabber removal/isolation, zero-time pause, order changes while grabbed, hold radius, disengagement, wall blocking, and stamina accounting. Perimeter tests are included in the preceding checkpoint.

Unity and a C# compiler are not installed in the patch-authoring environment. Runtime test results and visual animation quality must be verified in the editor; no automated runtime pass is claimed.

For each lab observe:

1. Can you identify windup, impact, recovery, grab, and bite without reading the panel?
2. Does Hold avoid runaway chasing? Does Disengage result in actual escape attempts?
3. Can a teammate visibly interrupt a grab before infection?
4. Does the veteran usually handle three ordinary zombies at 1x without becoming unbeatable when surrounded?
5. Does pause freeze a bite halfway through? Do fights still behave sensibly at 4x and 12x?

Report outcome, survivor health/infection, speed setting, and any stuck/unnatural transitions. This checkpoint establishes the mechanics; combat balance needs that feedback.
