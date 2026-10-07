# Atlas scout map and night watch

This atlas-only presentation slice adds a north-up, 400 m minimap and knowledge-based full atlas. It integrates an optimized version of the morning joint-built character prototype for the explorer, company and hostile scouts. The combat lab, Founding scenario, strategic model and approved player combat rules are unchanged.

## Knowledge and reports

`AtlasKnowledge` owns a 128 x 128 visibility/exploration grid and last-reported settlement owners. Cell territory is assigned to the nearest settlement. Owned/allied territory is shared automatically; the player reveals a 90 m radius and the local friendly company reveals 35 m around guards. Geography discovery uses these radii, not per-cell raycasts. Sight of hostile scouts still uses the existing 22 m terrain/building occlusion checks. Tree foliage does not block that sight test.

Unknown ground is dark. Explored ground dims when observation is lost. Unknown settlements cannot be selected for UI survey travel; their owners/resources are not exposed. Reported owners do not change while unseen. Public `Travel` remains an unrestricted automation/developer seam. Map reload clears knowledge. Alliances share territory; losing an alliance retains explored geography but removes shared visibility.

`WorldAtlasRecon` refreshes cartographic knowledge at 4 Hz. Orange hostile markers arrive after the existing patrol-to-commander reporting delay, retain the last observed position, and expire after 60 simulation seconds unseen. Expired reports cannot reappear without a new observation. Encounter completion clears the marker. The map shows friendly soldiers and a gold commander marker, discovered work sites and work roads. The full atlas uses the same knowledge and contact report.

This is cartographic fog plus scout-renderer/territorial-banner/army-marker filtering. It does not darken physical third-person terrain, hide all environment geometry, or implement a full strategic reconnaissance network. Reports, ownership and exploration are session-local, with no save/load persistence.

## Character presentation and torches

`AtlasCharacterBaker.Bake` creates the new `Resources/AtlasCharacters.asset` using the existing `JointMannequin` poses and crowd-body proportions. It does not overwrite the full-detail mannequin or the crowd benchmark assets. The atlas library has 33 shared poses: 16 walk, 16 run and a guard idle. Each body has 370 vertices, separate cloth/skin/boot submeshes, fixed fists and baked hand/head sockets. Equipment and facial/torch detail are additional geometry. No soldier needs a runtime skeleton, Animator or movable fingers.

`AtlasCharacterVisual` varies height/build, skin, cloth, metal finish, beards and helmet plumes deterministically. The commander uses gold kit and a red crest. Overhead two-star rank, name and health bar are distance/occlusion filtered. **Health is a full 100 HP presentation placeholder**; the atlas encounter remains deterrence-only and does not deal damage. Villagers retain their separate animated worker models, and optional 128-soldier scale markers remain static proxies.

Between 19:00 and 06:00, all active company members carry a visible left-hand torch. Four nearest bearers within 45 m of the camera receive pooled, unshadowed point lights. Distant torch flames remain visible without additional lights. Pause freezes character pose/flicker. Cosmetic variation does not change movement speed, routes, duty assignments, perception ranges or combat statistics.

The company is still a bounded 25-character pool. This integration is not a 1,280-unit benchmark, and its extra renderers/materials must be profiled before scaling up.

## Validation and controls

Run focused PlayMode filters `Reclamation.Tests.AtlasVisibilityTests;Reclamation.Tests.AtlasScoutTests;Reclamation.Tests.AtlasGuardTests;Reclamation.Tests.AtlasVillageTests;Reclamation.Tests.WorldAtlasTests` using the Unity commands in `Docs/TESTING.md`. `AtlasVisibilityTests` covers unknown ownership, exploration memory, alliance sharing/loss, stale contact non-tracking/expiry, optimized character integration, torch light limits and reload reset.

Build with `Reclamation.Editor.WorldAtlasBuild.BuildWindows` and a new `RECLAMATION_ATLAS_BUILD` path. A prepared checkout already contains the baked library; do not call the baker again without preserving it for a deliberate rebuild.

Standalone `--visibility-smoke --output <folder>` captures commander, fog, night torches and reported contacts on small/4, medium/8 and medium/12. Existing `--scout-smoke`, `--village-smoke` and `--atlas-smoke` remain available. Smoke launches run in background so alt-tab does not stall automation. Normal play keeps its existing focus behavior.

Manual review: C visits the commander, M opens the atlas, T advances three hours, R starts the scout encounter, G visits a reported approach and E challenges with reserve support. WASD/Shift move, RMB orbits, wheel zooms, Esc pauses and F3 toggles clearly labeled developer diagnostics.
