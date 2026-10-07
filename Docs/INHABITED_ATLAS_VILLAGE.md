# Inhabited atlas village

Current workplace follow-up: see [Atlas village hinterland](ATLAS_HINTERLAND.md) for larger fields, forest/quarry districts and work-carry-return sequences. Earlier details below describe the preceding village milestone.

This slice adds everyday schedules, collision, an enterable civic building and seeded layout variation to the separate WorldAtlas scene. The two approved map dimensions, terrain/resource model, player walk/sprint speeds and existing combat demos remain unchanged. Broader architecture, weather, wildlife, rare creatures and procedural-world direction is recorded in `WORLD_LIFE_DIRECTION.md`.

## Playtest

Launch the standalone build and walk the village main street. Houses block passage. Hold RMB to orbit near a wall: the camera retracts instead of moving through it. Click **Town hall**, then walk through the open doorway. Benches, a council table and civic crest furnish the interior; the roof cuts away while the character is inside. Exit through the doorway. Ordinary homes, the tavern and storehouse have exterior collision but no interiors. Hall entry does not change ownership or award victory.

The clock starts at 09:00; 1x means a 24-minute day. T advances three game hours for review, and the time button cycles 1x/10x/60x. Esc pauses the clock and civilian animation. Dawn/day/dusk/night change the sun, ambient light, sky/fog and nearby lanterns. Weather is not implemented yet.

Eighteen residents are assigned homes and roles at each initially developed human settlement or neutral enclave. A bounded local visual pool renders only the closest village within 150 m. Analytic schedule sampling reconstructs the appropriate location for distant residents without ticking thousands of navigation agents. This is not a production, needs, household or population simulation. Newly claimed empty survey sites do not automatically become developed villages.

Typical schedule: 06–07 breakfast at home; 07–08 commute; 08–17 work at a field, woodcutting spot, quarry or market; 17–18 travel to evening destinations; 18–21:30 social time; 21:30–22 return home; 22–06 sleep. Departures are reproducibly staggered over about 36 game minutes; outbound/evening travel takes roughly 24–51 game minutes depending on the resident. Small lane offsets and differing animation phases reduce synchronized processions. Residents use farming, chopping, mining, trading, eating and drinking poses, with simple elbow/torso articulation and mitten hands. Six farm, three chop, three mine, and six demonstrate trading gestures. Breakfast uses food; evening destinations show mugs or food. Work trees and rocks are local presentation props; no resources are depleted or produced. Sleeping residents are hidden at their home entrances. Accelerated time speeds their travel as well as the clock. Future autonomous jobs and threat responses must not be inferred from these routines.

## Procedural variation

`AtlasVillageLayout` derives stable per-site seeds from the current map preset and site ID, independent of combat and strategy randomness. Market-street, village-green and crossroads plans vary orientation, street width, plot spacing, cottage appearance and six/eight-house starting counts. This is a constrained first layout generator, not arbitrary free-form town generation. City/village preview plots and fortress placements also receive deterministic variation and skip obstructed placements. The current terrain and deposit positions are unchanged. F3 displays the selected site's layout seed and plan name.

Building placement, civilian destinations and travel routes consume the same layout. A route-clearance test samples all residents throughout the day at the initially populated human/neutral sites of both map presets. The field access route skirts outer plots in rotated layouts. This sampled check is not an exhaustive proof for arbitrary future seeds or building additions.

## Architecture and boundaries

- `AtlasVillageLayout.cs`: deterministic site plans and route anchors.
- `AtlasVillageRoutine.cs`: activity and position sampling by hour/resident/layout.
- `WorldAtlasVillage.cs`: layout construction, building boundaries, town halls, light cycle, movement/camera collision.
- `AtlasVillageArt.cs`: paths, gardens/hedges, hall roof/furnishings, lanterns, signs and baskets, sharing `AtlasArtKit` geometry/material ownership.
- `WorldAtlasArt.cs`: bounded civilian visual pool, role poses, held tools/food/mugs and village/hall survey shortcuts.
- `WorldAtlasVillageSmoke.cs`: packaged checks/captures for schedules, hall entry/exit, wall blocking and day phases.

Collision uses project-local generated BoxColliders on the built-in Ignore Raycast layer with an explicit mask. No project layer/input settings or combat controllers are changed. Player survey movement uses a swept capsule with wall sliding; the survey camera uses sphere casts and immediate retraction, with eased extension. Terrain remains the atlas height/Walkable model. Static scenery remains shared/instanced. Trees and resource decorations are not all collision obstacles, and villagers do not physically separate from one another or the player yet. Future procedural layouts must continue to validate doors, routes, spacing and camera behavior.

Run the focused PlayMode filter `Reclamation.Tests.AtlasVillageTests;Reclamation.Tests.WorldAtlasTests`, retaining graphics and omitting `-quit`. Build with the existing WorldAtlasBuild entry point; run the executable with `--village-smoke --output <folder>` for the new checks or `--atlas-smoke` for the atlas workflow. See the delivery report for exact results and known limits. None of these checks establish midrange-PC FPS targets.

## Activity and village polish follow-up

Market awnings and posts now obstruct the survey camera. Generated plots have a wider bounded spacing variation, yard props, grass clumps and slightly varied path widths. This is still a constrained three-family generator, not organic town growth. Activity tools are pooled on the 18 local residents and enabled only when appropriate. Pause freezes both motion and clock. Tests cover visible tool selection, animated arm movement, pause, commute continuity, route clearance and overhead awning obstruction. The packaged village smoke captures three poses of each of the six activities on both maps.
