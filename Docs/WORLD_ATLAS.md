# World atlas: map scale and factions

Current follow-up: [Inhabited atlas village](INHABITED_ATLAS_VILLAGE.md) adds seeded layouts, building/camera collision, an enterable town hall, schedules and day/night. Its collision and routine descriptions supersede the earlier art-pass limits below. See [World-life direction](WORLD_LIFE_DIRECTION.md) for approved subsequent milestones.

Approved direction, 2026-09-28: a directly controlled player character leads a faction in a large fantasy world. Initially support a design ceiling of 8-12 human/bot faction slots, affinity-based alliances and conquest, multiple civilian settlements and independent military facilities. Each faction should have room for an army of at least 128. This document distinguishes the current survey prototype from the eventual integrated game.

## Two examples

| Preset | Dimensions | Area | Slots offered | Potential sites |
| --- | --- | --- | --- | --- |
| Greenward Valley, small forest valley | 1,600 x 1,600 m | 2.56 sq km | 2 or 4 | 36 |
| Sundered Coast, medium mountainous coast | 3,200 x 3,200 m | 10.24 sq km | 8 or 12 | 80 |

These are candidate dimensions, not final balance. Walking straight across at 3.8 m/s would take about 7 and 14 minutes respectively, before obstacles/detours. The atlas provides explicit survey travel so comparisons do not require long walks. The valley has a winding river, three crossings and forest patches; the coast has western mountain ridges and an eastern shoreline. Surface arches mark two cavern candidates; no underground map is implemented. Islands, boats and other biomes remain future examples.

Every clearing has nearby food, wood and stone markers, within 55 m. Roughly one third have gold and one seventh niter; those rarer deposits create different expansion incentives. The displayed number is relative richness, not an inventory or harvesting rate. Resources are visible markers/model data, not a functioning gathering economy. Potential settlement clearings are predefined on a grid for comparison; placement anywhere on arbitrary terrain is not implemented. Connectivity, fairness and travel times need further playtesting before these become competitive maps.

## Player and factions

The explorer uses the existing modular character art with independent survey movement. WASD, Shift and RMB remain familiar; mouse wheel zoom supports close exploration and footprint comparisons. The combat demos and their player controllers are untouched. There is no combat or networking in this scene.

Slot zero is the player. Other slots have human, blighted or undead identities. Human rivals initially remain neutral; cross-family rivals are hostile. Two neutral enclave sites and two hostile lair sites are independent of the playable slots. They are currently occupation markers; no trading, monster AI or diplomacy interaction exists for them.

The optional, paused-by-default **abstract faction simulation** takes one turn every ten seconds, or one turn when requested. Bots prefer nearby unclaimed land and richer site types, join same-family affinity alliances from turn three, and contest enemy holdings after expansion options narrow. A simple strength/holding-support calculation resolves those contests. Bots can win dominance over faction-held territory; neutral and lair sites are excluded from that victory condition. This is deterministic prototype logic with no negotiation, fog of war, physical army travel, route-aware conquest or combat simulation. It is not the finished faction intelligence.

The player can travel to a free clearing and press F to claim it. This has no economy cost in the survey. Village, city and fortress footprint previews compare approximate 50 m, 116 m and 86 m layouts; they are not settlement upgrade systems. Occupied ownership cannot be overwritten by the player's claim action. Switching map/slot presets resets the survey, and no atlas save system is provided. Existing village/combat saves are never read or written.

## Army scale

Each faction home includes 128 instanced static figures. The 12-slot preset therefore displays up to 1,536 figures. These show formation footprint at human scale; they are not skinned, animated or individually simulated combatants. Abstract strategy strength may grow beyond 128 while the scale yard remains fixed at 128. No new claim of 60/120/144 FPS on a midrange PC is made. Full-HD and 1440p are intended display targets; final hardware benchmarking remains separate.

## Architecture

New independent namespace `Reclamation.Atlas`, under `Assets/Reclamation/Runtime/Atlas`:

- `WorldAtlasModel.cs`: deterministic terrain/site/resource model, ownership, faction relations and abstract strategy.
- `WorldAtlasScene.cs`: terrain mesh, water, instanced woods/scale formations, resource and settlement markers, modular player art and atlas texture.
- `WorldAtlasDemo.cs`: survey movement, camera, travel, claiming, footprint preview and isolated smoke.
- `WorldAtlasHud.cs`: on-foot HUD, map selection, faction presets, resource information and collapsible developer diagnostics.

`WorldAtlasBuild.cs` creates `Assets/Scenes/WorldAtlas.unity` only if absent and builds it explicitly. It preserves all existing scenes and the global scene list. `RECLAMATION_ATLAS_BUILD` must identify a new Windows executable path. Runtime assembly dependencies are unchanged.

New `WorldAtlasTests` contains four preset cases plus tests for claims/bounds, alliance/expansion, abstract bot victory, and scene switching/travel/claiming. Run graphics-enabled Unity 6000.3.24f1 with `-batchmode -runTests -testPlatform PlayMode -testFilter Reclamation.Tests.WorldAtlasTests -testResults <xml> -logFile <log>`, without `-quit`.

Build with `-batchmode -quit -executeMethod Reclamation.Editor.WorldAtlasBuild.BuildWindows`. Packaged verification: `ReclamationWorldAtlas.exe --atlas-smoke --output <isolated-directory>`. It captures both map views, ground views, city footprints and strategy progression. See the delivered report for exact results and preservation counts.

## Limits and acceptance

The scene is a geography/scale/faction exploration prototype. Cave candidates and army figures remain placeholders; the first village art pass adds modeled exterior buildings, resource props and local cosmetic villagers. Buildings and trees do not have blocking colliders; walking is bounded by terrain/water/slope checks. Water transport, tunnels, integrated command hierarchy, combat, real-player slots, networking, production, research, migration and persistent campaigns are future work.

For this review, judge whether small already feels large enough; whether medium supports many kingdoms without feeling crowded; whether the rare resources and terrain suggest different settlement choices; and whether the player can understand faction relationships. Next, choose one region for integrating the proven village/commander/combat loop before scaling to a full networked conquest game.

## Village art pass (2026-09-28)

Thomas approved the atlas scale and requested recognizable buildings, resource models and villagers. The map dimensions, terrain, movement, resource distribution and faction rules remain unchanged.

`AtlasArtKit.cs` builds shared meshes for three timber/plaster cottage variants, a storehouse, striped produce stall, stone well and crenellated stone tower. Roofs have ridges and courses; cottages have framing, doors, windows, chimneys and steps. Resources now appear as fenced crop rows, cut log stacks with a stump, irregular stone outcrops, gold-bearing outcrops and pale niter-bearing outcrops. Village/city/fortress footprint previews use these building models. Hostile settlements retain a provisional shared building vocabulary with a dark lair landmark; faction-specific architecture remains future art work.

Static placements use shared geometry and material-based GPU instancing. Preview buildings reuse the same meshes. `WorldAtlasArt.cs` owns a pool of 18 local villager visuals with three outfits/head variants, simple mitten hands, walking leg/arm motion and idle head turns. The pool is shown only within 150 m of the nearest occupied human settlement or neutral enclave. It is cosmetic: the count is not a population model, and routes do not implement jobs, hauling, production, combat or persistence. Esc freezes these routines. No physics bodies, finger bones or resource simulation were added. This is not proof of target-hardware performance.

Use **Village close-up** in the bottom bar to inspect the selected site at human scale. It adjusts the survey camera and travel position; normal exploration controls and speeds are unchanged. F3 diagnostics include the active local villager count. Existing combat presentation and all non-atlas gameplay files are preserved. Exterior building/prop collisions and interiors remain unimplemented, matching the survey's previous visual-marker boundary.

The focused eight-test atlas suite additionally verifies scenery creation, the bounded villager pool across map switches, and absence at empty sites. Packaged smoke captures both villages and crop fields in addition to the prior atlas, ground, footprint and strategy views. See the delivered art-pass report for executed results.
## Scout map and night watch (current)
See [Atlas scout map and night watch](ATLAS_SCOUT_MAP.md) for knowledge-based minimap/full-atlas fog, reported contacts, optimized prototype soldiers, commander identification, night torches and current validation commands. Earlier art-pass limitations above are historical; subsequent inhabited-village and guard slices added building collision, interiors and scheduled work.


## Wildlife and layered outfits
See [Wildlife and layered outfits](ATLAS_WILDLIFE_OUTFITS.md) for the matte material pass, shared equipment poses, bounded rabbit/deer/bird habitats, J review controls and validation. Wildlife remains cosmetic and does not change combat or resource production.


Current command hierarchy: see [Atlas company and platoons](ATLAS_PLATOONS.md). The local company now contains four named platoon commanders, each with five soldiers, under its company commander (25 total). This replaces the earlier four duty sections; headcount and the separate combat systems are unchanged.

Current movement follow-up: see [Atlas platoon movement](ATLAS_FORMATIONS.md) for road columns, patrol spacing, defensive lines and compact commander badges.
