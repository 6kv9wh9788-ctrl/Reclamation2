# Atlas wildlife and layered outfits

This slice is restricted to the atlas. It preserves the original character pose library, combat/Founding systems, strategic model, movement contract, scenes and settings.

## Materials and outfits

`AtlasCharacterVisual.Mat` explicitly sets nonmetal surfaces to metallic 0 and smoothness 0.04, skin to 0.12 smoothness, leather armor to 0.08, and metal to 0.55 metallic / 0.22 smoothness. Existing torch flames keep their unlit treatment. There are no project-wide shader or lighting changes.

`AtlasOutfitBaker.Bake` creates `Resources/AtlasOutfits.asset` without modifying `AtlasCharacters.asset`. Its 33 shared poses separate helmet, breastplate, shield and sword materials, correct the carrying shield angle and add 528 vertices per outfit for padded split hems, a belt/buckle, pouch, shoulder pieces, boot cuffs and chest straps. Character bodies remain 370 vertices; facial, torch and other equipment meshes are additional geometry. Watch variants use leather/plate finishes and faction-colored cloth/shields; the commander retains gold kit and a crest. These are presentation variants, not new combat roles, armor statistics or inventory items.

Existing bake tools reject overwriting an existing library. Preserve assets before any deliberate rebake. Runtime soldiers still use shared baked poses and fixed hands, with no added per-character skeletal animation.

## Wildlife

`AtlasWildlifeArt` adds shared procedural rabbit, deer and bird meshes to `AtlasArtKit`. `WorldAtlasWildlife` owns a maximum of 18 visuals: six rabbits, four deer and eight birds. Deer include antlered and antlerless variants. Legs, grazing heads, rabbit hops and bird wings animate with simple transforms.

The nearest site's seeded hinterland defines grass/field-edge and woodland-edge candidate areas. A deterministic, separate random stream chooses placements and routines. Placement checks terrain walkability, solid buildings, crop footprints, dense work-forest/quarry footprints, spacing and cached work-road exclusion corridors. Movement samples the same constraints along each step. Inhabited centers within 65 m are excluded. The pool activates while the player is within 260 m of a site's center; changing regions recreates its seeded population. These are local ambient populations, not persistent animal entities spread across the whole map.

Animals feed, wander and rest. They flee from the closest nearby player, guard, civilian or encounter scout, including at night. Deer react within 15 m; rabbits/birds within 7 m. Birds rise into a short flight and land after the scare. Night rest is 20:00-06:00. Esc freezes positions and animation; clock acceleration is applied with bounded substeps and at most one simulation second per update, avoiding a catch-up spiral after a long stall.

Wildlife uses no new realtime lights, finger/skeletal rigs, physics bodies, resource production or damage systems. It does not change human patrol routes. Tree foliage and decorative props without existing collision are not navigation obstacles. Animals can stop at an excluded lane; this pass does not add full pathfinding. Grazing is cosmetic and consumes no crops. Hunting, predation, reproduction, sound, seasonal migration and rare fantasy encounters remain future work.

## Controls and validation

J cycles nearby rabbit/deer/bird review viewpoints; ordinary walking also discovers them. C visits the commander for outfit inspection; T changes time; F3 diagnostics show wildlife and flee counts. All existing atlas controls remain.

Focused PlayMode filters: `Reclamation.Tests.AtlasWildlifeTests;Reclamation.Tests.AtlasVisibilityTests;Reclamation.Tests.AtlasScoutTests;Reclamation.Tests.AtlasGuardTests;Reclamation.Tests.AtlasVillageTests;Reclamation.Tests.WorldAtlasTests`. Wildlife tests exercise reproducible placement on small/4, medium/8 and medium/12; terrain/building safety; fleeing; pause; distance deactivation; material categories; and all outfit pose families.

Build with the existing `WorldAtlasBuild.BuildWindows` method and a new `RECLAMATION_ATLAS_BUILD` destination. Standalone `--wildlife-smoke --output <folder>` captures outfits, close views of each species, reaction checks and night equipment on all three presets. Close animal captures use a temporary automation camera/player-hide setup; normal gameplay does not hide the player. Existing visibility/scout/village/atlas smoke modes remain supported. Smoke launches continue in background when alt-tabbed.

This bounded local pass does not establish performance at the eventual 256 friendly / 1,024 hostile target. Profile the added renderers/materials before scaling up.

