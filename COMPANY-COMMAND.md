# Company command: prototype and direction

## Standing vision

The player fights alongside people who develop skills, judgment and shared
history. Commanders act on imperfect information, delegate tasks and weigh
risk. Preparation, reconnaissance, coordination, reserves and preservation of
experienced personnel should affect outcomes. Good AI does not mean perfect
choices or guaranteed victories.

Platoon tasks discussed so far are examples, not an exhaustive order catalog.
The eventual hierarchy is player intent -> battalion allocation -> company
plan -> platoon mission -> soldier action. Rank should change responsibility
and resources, not turn the game into a collection of disconnected order menus.

## This playable iteration

Company Command Lab uses three two-person platoon stand-ins plus the player.
These are deliberately small units for testing decisions, NOT representative
military platoon sizes or a claim of battalion-scale battle performance.

- Mara/Bren: cautious commander preference.
- Ivo/Nessa: balanced preference.
- Tess/Oren: bold preference.

All use existing modular models, sword/spear combat and local survival rules.
They have independent assignments. A selected-platoon ground ring, roster,
commander status and report panel expose their actions.

| Mission | Current behavior | Completion or transition |
|---|---|---|
| Defend | Move to a named point and protect the nearby area | Standing duty; one practice award after 30 seconds on station |
| Scout | Survey a named point, observe nearby contacts, avoid unfavorable combat | Return to camp after survey; credit on return |
| Assault | Move toward and engage defenders of a named point | Secure its nearby area and hold |
| Escort | Accompany the player toward the selected named destination | Reach that destination with the player and defend it |
| Withdraw | Return the surviving platoon to its camp slots | Await further orders; no practice farming from recall |

The optional company assault template assigns a main approach, a west flank and
a reserve. The main force waits until both assault platoons reach staging points.
An overridden order, lost platoon or excessive-risk withdrawal cancels staging
coordination and leaves the other waiting unit holding rather than charging alone.
This is one authored planning template, not a general strategy generator.

Contacts update only when a living friendly observer is within 8 metres with
unobstructed terrain sight. Reports show last-seen location and age; unseen
movements or deaths do not automatically update them. Observations are shared
instantly across this prototype. There is no communications delay, visual fog
of war, stealth system or terrain exploration map yet.

Risk decisions use nearby observed threats, friendly support and platoon health.
A visible Hulk is estimated as more dangerous than a thrall. Scouts prioritize
returning intelligence over difficult fights; balanced/bold scouts may engage
an isolated nearby thrall under favorable personal conditions. Soldier fatigue
and injury still constrain actions and movement. Retreats do not cancel committed
attacks, and the player cannot assume that a bold commander ignores casualties.

Distinct completed duty/location combinations grant 10 practice points once
per commander per run. Every 20 points improves assessment cadence and makes
serious wounds trigger withdrawal slightly earlier, up to five increments.
There is no damage bonus, learned strategy model, personality convergence or
campaign persistence. R and scenario changes start a new operation and clear
these records. These modest rules demonstrate progression rather than complete it.

F at camp opens an operation review once surviving platoons have returned. It
records casualties, known contacts, current reasons and practice earned. Lost
soldiers remain lost until a fresh operation. Safe camp recovery helps survivors.

## Order contract for the eventual system

Every delegated mission should identify:

1. Purpose and objective: what result matters, and why.
2. Assigned units, attachments and boundaries.
3. Priority, time constraints and completion conditions.
4. Engagement policy, pursuit limits and acceptable risk.
5. Reporting requirements and the authority to change the plan.
6. Contingencies: fallback points, relief, reinforcement and evacuation triggers.

Use sensible defaults. Players should be able to issue a quick order and adjust
its details only when useful. Show the commander's interpretation before the
player has to infer it from a disaster.

## Candidate task families

Defend/secure; patrol/screen; reconnaissance/observation; assault/seize; raid/
ambush; escort/protect; reserve/reinforce; withdraw/delay; casualty recovery;
resupply/rest; engineering/fortification; containment; training/integration.
Some require systems that do not exist yet. They are design candidates, not
implemented command buttons.

## Next development gates

1. Validate independent orders and local judgment in the small command lab.
2. Improve plan explanations, map-based assignment and constrained area defense.
3. Add persistent, versioned personnel records and experience from responsibility.
4. Add ranged roles and logistics so preparation changes available plans.
5. Expand to company task decomposition, coordination and relief/reserve rules.
6. Only then expand unit counts, battalion delegation and large-battle performance.

At every gate: test a duel, a squad fight and the command layer separately.
Record actual frame timings at representative battle loads. Walk/render-only
benchmarks do not establish army-scale AI, navigation, physics or combat capacity.
