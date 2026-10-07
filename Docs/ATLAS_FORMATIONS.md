# Atlas platoon movement

Each local platoon now follows a shared route guide and a bounded trail rather than six independent route cursors. Membership, company orders, report delay, gate coverage and six-hour watch assignments remain the platoon milestone's contract. This remains the separate, 25-person atlas demonstration.

## Formation behavior

- **Posted:** six distinct gate or reserve positions. Passing troops yield around posted guards.
- **Patrol:** members follow the commander's route with 2.7 m trail offsets and alternating 1.05 m lateral offsets. Town patrol uses a loop with separate sides; farm patrol follows the existing hinterland roads.
- **Column:** the responding platoon follows one road trail with 2.1 m offsets. The guide advances at up to 1.2 m/s; members walk at up to 1.35 m/s and the guide waits if a member lags more than 7 m behind its assigned slot.
- **Deploying / Line:** at the farm approach, five troops spread across the approach at 2.2 m intervals. The commander stands 2.2 m behind the center. They face the reported approach and do not chase withdrawing scouts.
- **Returning:** after the incident, members reform around the shared trail and retrace the road. The platoon then resumes its current watch assignment. Relief also uses road travel instead of teleporting.

Movement uses simulation substeps no larger than 0.1 seconds, including accelerated time. Local avoidance attempts lateral yielding when guards would approach within 0.95 m. Building and terrain checks reject blocked moves. This is constrained road following and local avoidance, not a general navigation planner or an exhaustive collision solver. It does not add physical separation against villagers, wildlife or the player. New maps or obstacle types need additional route validation.

## Presentation and playtest

Use **C** to view the company and its four platoons. **R** starts the scout encounter; **G** visits the approach after the report. Observe a marching column, deployment into a line, then return to town after deterrence. Let it finish autonomously or use the existing **E** challenge. **T** advances the watch, and **Escape** pauses. The existing time button supports 1x/10x/60x review.

Overhead badges are compact rank/platoon markers at distance. Names expand within 12 m or when the badge is near the center of the view. These are camera proximity/focus rules, not a new unit-selection control. Badges avoid HUD panels and other badges. The previous constant 100 HP placeholder has been removed from these badges; no health/combat model has been added.

With the C panel open, **F3** shows the explicitly labeled **DEBUG / COMMAND AND FORMATION** panel. It includes each platoon's current movement phase and the last six received reports. Close diagnostics for ordinary play.

## Code and checks

- `AtlasFormation.cs`: shared path/trail, slot selection, deployment, road retracing, bounded movement and local yielding.
- `AtlasSettlementGuard.cs`: active company pooling and integration with formation updates; rendering consumes resolved positions.
- `AtlasScoutResponse.cs`: starts the reserve march and return on company orders and encounter completion.
- `WorldAtlasRecon.cs`: compact and focused commander badges.
- `AtlasFormationTests.cs`: movement bounds, building clearance, spacing, line facing, autonomous completion, return to reserve posts, pause and accelerated patrol movement.
- `WorldAtlasFormationSmoke.cs`: standalone captures of compact badges, road column, defensive line, return and final posts on small/4, medium/8 and medium/12 presets. Run with `--formation-smoke --output <folder>`.

Run the focused PlayMode suite with `Reclamation.Tests.AtlasFormationTests` plus the existing AtlasPlatoon, AtlasWildlife, AtlasVisibility, AtlasScout, AtlasGuard, AtlasVillage and WorldAtlas test fixtures. Retain graphics and omit `-quit` for tests. The delivery report gives exact results and distinguishes earlier attempts from final verification.

No changes to player attack/dodge, combat lab navigation, existing character mesh libraries, maps, wildlife, recruitment or troop counts. The speed limits here apply only to atlas guards. Army/Battalion command and integrated skirmishes remain future work. No midrange-PC FPS certification is claimed.

Arrival details: the return begins with a regroup into the new travel direction. At home, rear posts fill before front posts, preventing early arrivals from blocking the last member. A committed relief or return finishes before a newer watch assignment is consumed.

Watch handover reporting: a guard still completing the old farm watch remains a valid eyewitness even if its platoon has received a new assignment. The normal report delivery delay still applies. A regression test exercises rapid 12:00 -> 23:00 -> 09:00 changes before starting scouts.

Farm coverage during relief: the outgoing farm patrol keeps patrolling until the newly assigned farm commander reaches the approach. This prevents rapid time changes from leaving the fields unobserved while both platoons are in transit.
