# Crowd spacing and boundary escapes

Apply after the camera zoom fix. Open your existing Patient Zero scene and press
Play. No scene regeneration or upgrade command is required. The outbreak actors
configure their avoidance radius and priority at startup.

## Behavior changes

- Human avoidance radius is at least 0.45 m, with high-quality avoidance and varied
  priorities. This reduces bunching; it is soft local avoidance, not hard physics.
- Turned actors stop about 0.95 m from their prey instead of aiming at its center.
  This remains within the existing 1.4 m contact-transmission radius.
- Civilians evaluate 24 possible escape destinations, including lateral routes
  along edges. Only complete NavMesh paths qualify. The search samples path
  segments to avoid crossing too close to a pursuer, considers all active nearby
  outbreak threats, and prefers destinations with boundary and crowd clearance.
- Fleeing starts within 7 m; returning to routines requires 9 m of separation.
  This reduces rapidly switching between flight and normal routines.
- Exposed but not yet symptomatic civilians still flee and look healthy.
- Paths are refreshed periodically instead of every rendered frame. Refresh
  intervals, turning speed, and acceleration account for simulation speed.
- Pursuers discard stale paths when prey disappears, becomes isolated, turns,
  or cannot be reached. Their next target remains the nearest eligible person.
- The outbreak panel displays movement status, including cornered actors.

A cornered person is not guaranteed an escape: where no safer complete route is
found, they stop and retry. This is a local escape search, not a city-scale planner.
Very narrow spaces and large/high-speed crowds still need further tuning. Contact
transmission, infection timing, isolation, and campaign outcomes are unchanged.

## Verify in Unity

Run 30 EditMode and 24 PlayMode tests. Six new PlayMode tests cover movement along
an edge, corner routing, escaping while latently infected, pause/isolation, pursuit
standoff and target removal, and choosing a route around two pursuers.

In PatientZeroLab, follow a civilian during a pursuit near the map edge. Look for
sideways movement instead of repeated attempts to move off the map. Test at 1x
first, then 4x and 12x. Pause to inspect spacing. Neutralize a followed threat and
check that healthy civilians resume their routines when the area is safe.

The patch is statically checked; Unity compilation and runtime results must be
verified in the installed editor. It changes no saved scene geometry.
