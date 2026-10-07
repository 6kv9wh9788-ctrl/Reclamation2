# Atlas guard presentation

The atlas guards use separate shared pose banks (`AtlasGuardPoses` and
`AtlasGuardOutfits`). The original 33-pose libraries used by the player and scouts
remain unchanged. Five readiness levels each contain the same 33 idle/walk/run
samples. Meshes, equipment, torch socket and head details use matching indices.
No finger articulation, per-guard skeleton, or per-frame mesh deformation is added.

Posted, patrolling and returning guards lower their sword and shield. The responding
reserve blends toward its existing ready pose over half a simulated second, and
lowers its equipment again after response ends. Readiness uses five baked steps;
this is not continuous skeletal blending. The shared bake increases asset size.

Root positions and logical headings remain owned by formation simulation. A child
presentation transform turns toward that heading at up to 180 degrees per simulated
second, including time acceleration. It initializes immediately on settlement
activation and freezes when paused. It does not alter steering, report timing,
avoidance, defensive slots, movement speeds, or combat.

The compact HUD keeps settlement and company information at the right edge and
the scout event at the upper left. A shorter world header frees vertical space.
F3 toggles one clearly labeled lower-left DEBUG / RECORDING AID panel containing
local metrics, platoon phases and the last six reports. C opens/closes the company
roster independently. Commander badges respect the revised panel bounds.

Validation: `AtlasPresentationTests` checks gradual turning, readiness transitions,
pause, return to relaxed posture, and matching shared body/equipment pose banks.
Run it with the existing atlas formation/command/visibility/scout/guard/wildlife/
village/map suites. The delivery report records actual run results.

Player combat, hero/scout pose banks, formation rules and the 25-person company
size are unchanged. This is not a large-army performance certification.
