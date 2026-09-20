# Perimeter defense checkpoint

Based on the saved safe-zone checkpoint (4a6d994). Existing scenes are not overwritten.

## Try it

1. Open PatientZeroLab outside Play Mode.
2. Choose Reclamation > Add Buildable Perimeter to Open Refuge. Save the scene.
3. Optionally choose Reclamation > Scenario > Visitor Becomes Brute, then save.
4. Enter Play Mode, evacuate healthy survivors into the refuge, and use Build next (4).
5. Build all six sections, then close the gate after arrivals clear its footprint.
6. Watch a zombie attack a reachable section when its prey is behind the perimeter.
7. Repair damaged standing sections, or rebuild destroyed ones. Open the gate for arrivals.

The gate is built first and starts OPEN. Unbuilt/destroyed sections and an open gate appear as flat strips. A closed intact ring provides physical protection, not shelter immunity. Quarantine still uses the earlier abstract isolation rules.

## Prototype balance

- Separate defense supply: 36 wood. Six fixed slots cost 4 each (24 total).
- Repair costs 2 and restores a standing section to full health. No free-form placement or connection to the hauling economy yet.
- A sheltered survivor must reach the inside work point and work for 5 simulation seconds. Nearby reachable zombies interrupt work. Cancellation refunds reserved wood once.
- Sections have 120 health. One ordinary zombie destroys wood in 30 simulation seconds; a brute takes 10, after reaching its attack point.
- Ordinary zombies cannot damage masonry or steel. Brutes can damage masonry slowly, not steel. Reinforced wood and future siege damage are defined in DefenseRules; the build interface currently supplies wood only.
- Brutes are an explicit pre-play Visitor scenario choice, not a response to player construction. They have a larger visual silhouette; combat abilities come in a separate checkpoint.
- Broken sections disable their collider and navigation obstacle. Zombies prefer reachable survivors and can enter the new gap after navigation updates.
- Gate closure and construction wait for an occupied footprint to clear.

## Verification

Run both EditMode and PlayMode suites in Unity Test Runner. Added tests cover material immunity, time-scaled damage, gate states, destroyed-wall navigation, blocked contact, construction/repair accounting, cancellation refunds, and paused siege behavior.

Unity is not available in the patch-authoring environment; these runtime tests must be run in your editor. Stop/restart Play Mode to reset supplies and sections.
