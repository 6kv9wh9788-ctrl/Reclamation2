# Reclamation — Blight playtest batch

## Creative direction
Combat-first dark-fantasy action RPG: lead a surviving company into lands consumed
by supernatural blight. Third-person, telegraphed melee combat; autonomous squad
orders; meaningful equipment; reclaimed territory. Settlement logistics support
expeditions. The blight's origin remains undecided. The new lab has no automatic
bite-to-zombie conversion. Legacy outbreak/settlement labs remain available.

## Install and open
The Reclamation2-blight-playtest-batch.zip download includes an installer, a full
patch (including the original duel), and an incremental patch for projects that
already received Reclamation2-blight-combat.patch.

Exit Play Mode. Extract the ZIP, then run Apply-Update.ps1 with your project path.
The installer checks the patch before applying it, selects the matching baseline,
and stops if existing local edits conflict. It does not commit, push, rebuild old
scenes, or discard changes. The earlier selection-cue patch is not a dependency.

Let Unity compile, then choose:
**Reclamation > Combat > Create Blight Combat Lab**
Press Play. The environment and actors are created at runtime, so the edit-time
scene initially contains only the lab component. Save the new scene if desired.
An existing saved Blight Combat Lab scene also uses the updated code.

The **Choose scenario** button opens all five scenarios. R resets the current one.
Switching scenarios resets health, stamina, orders, target, equipment, and loot.
Each starts fresh; there is no campaign save data yet.

## Controls
| Input | Action |
| --- | --- |
| WASD | Move relative to camera |
| Left mouse | Light attack |
| E | Heavy attack |
| Hold right mouse | Block frontal attacks |
| Space + direction | Dodge; backward if stationary |
| Tab | Toggle target-facing lock |
| Q | Cycle marked enemy |
| Middle mouse drag | Orbit camera |
| 1 / 2 / 3 / 4 | Follow / Hold / Assault target / Withdraw |
| X or weapon button | Swap sword/spear when ready and at least 5 m from hostiles |
| F | Recover the cleared patrol cache at close range |
| Escape | Pause/resume |
| R | Fresh reset of current scenario |
| H | Show/hide control help |

Click the Game view for keyboard focus. Losing application focus pauses the lab.
The camera can orbit while paused. Attacks don't fire through HUD buttons.
Lock-on controls facing, not camera orbit; orbit manually to keep the target visible.

## Suggested playtest order (about 20–30 minutes)
| Scenario | Setup | What to check |
| --- | --- | --- |
| Duel | Sword fighter vs one thrall | Windup readability, hit distance, block costs, timed dodge, recovery |
| Squad | Player + Mara + Bren vs three thralls | All four orders, focus target with Q then 3, survival and crowd spacing |
| Hulk | Player + two companions vs one Hulk | Broad sweep vs narrow smash, flank openings, two-heavy-hit interruption |
| Weapons | Solo vs one thrall; both weapons available | X to compare sword and spear reach, arc, pace, and stamina cost |
| Patrol | Company starts at a blue camp; road encounter and cache to north | Full objective sequence, spear reward, withdrawal, return and healing |

### Squad orders
- Follow: companions remain near you and defend against nearby enemies.
- Hold: each companion holds the position they occupied when ordered; engages only
  within three metres of it. Move the squad first if you want a different position.
- Assault: focus the marked enemy (Q cycles it); after it falls, engage nearby
  threats. The squad will not pursue targets more than 14 m from the player.
- Withdraw: move toward the camp location without beginning new attacks. Existing
  windups/recovery/staggers finish first. Companions can block/dodge while retreating.
  In non-patrol scenarios the rally is the southern starting area (z = -8).

Mara uses a sword; Bren uses a spear. Both use the same stamina, action commitment,
damage, block, and dodge rules as the player. They are not invulnerable. Downed
companions remain down until a scenario reset; this batch does not include revival.
A small enemy attack-slot budget limits simultaneous windups/recoveries (default 2).

### Hulk
260 health, bigger footprint, slower movement. Alternates:
- Sweep: wide arc, can hit multiple company members once each. Back away or dodge.
- Smash: narrow forward arc, longer reach and higher damage. Sidestep or dodge.

The Hulk commits its facing during a windup. One heavy hit damages it but does not
cancel that windup; two heavy hits within the same windup break its poise. Ordinary
thralls are interrupted by one heavy hit. Light hits do not stun-lock either.

### Equipment
| Weapon | Light reach | Light cost | Light windup | Arc |
| --- | --- | --- | --- | --- |
| Sword | 2.3 m | 16 stamina | 0.24 s | Broad |
| Spear | 3.4 m | 21 stamina | 0.38 s | Narrow |

Heavy attacks are slower and more expensive. The spear trades speed and stamina
efficiency for reach; it isn't a straight damage upgrade. Only the Hulk sweep
hits multiple targets. Other strikes hit the nearest hostile in the attack arc.
Melee currently resolves one directional range check at impact, not a swept blade.

### Patrol
1. Start at the blue camp in the south. Both companions follow.
2. Walk north along the road. Defeat two road thralls and the Hulk cache guardian.
3. Approach the gold cache after all three are down; press F within 2.6 m.
4. Recover and auto-equip the company spear. The reward can be taken only once.
5. Head south to camp, optionally use 4 (Withdraw) for companions.
6. Returning alive within the blue camp completes the patrol.
   Surviving company members recover health/stamina while ready and inside camp.
   Recovery is disabled if any living hostile is within 10 m of camp.

Going home early does not complete the mission. The spear is locked for the player
until the cache is recovered, even though Bren already carries his own spear.
Patrol loot is session-only. R or a scenario change clears it.
Victory in other scenarios freezes that encounter; in Patrol you can keep moving
after defeating enemies to collect the reward and return.

## Automated checks
Run all Unity EditMode and PlayMode tests.
- BlightDuelTests: commitment, hit-once timer, facing/range, blocking, dodge,
  guard break, interruption, weapon tradeoffs, Hulk poise, recovery, invalid damage.
- BlightPatrolTests: proximity/clearance/reward gates, safe return, order boundaries.
- BlightLabSmokeTests: spawn/reset, all scenarios, pause, swap restrictions,
  hold/withdraw, patrol completion, camp healing, one-hit-per-victim Hulk sweep.

The development environment has no Unity editor or C# compiler. C# syntax parsing,
patch application, file reconstruction, and packaging checks were run there.
These are not a substitute for a Unity compile or executed Unity tests.

## Tuning
Select the lab object before Play to adjust enemy windup scale, enemy damage scale,
player damage scale, and maximum enemy attackers in the Inspector. Defaults are
provisional. Weapon timing/reach/cost profiles live in BlightEquipment.
Report which scenario, weapon, and order you used with any failure or balance issue.
Useful feedback: can you read the windup, distinguish sweep/smash, retain your target,
withdraw reliably, and complete the patrol without a reset?

## Scope and next milestones
This is a flat, bounded greybox with procedural models/poses and a simple overhead
windup meter. There are no production animations, audio telegraphs, weapon trails,
obstacle navigation, controller support, equipment inventory, armor system,
revival, persistent loot, open world, or large armies yet.
The shared Blight combat rules now serve player, companions, and enemies; legacy
combat remains separate for regression comparison.
Next: tune from playtests, improve animation/hit feel, then armor and richer loot,
navigation around real obstacles, rescue, and a persistent reclaimed outpost.
