# Settlement companies and supporting buildings

The atlas now presents a peacetime company at initially developed human settlements and neutral enclaves. Its local pool contains one commander and 24 soldiers, organized into four six-person duty sections: fixed approach posts, town patrol, outer farm patrol, and reserve. This is a demo company size, not the final Army/Battalion/Company/Platoon organization or a 128-soldier battle simulation.

The standing intent is Defend settlement. Every six game hours the sections rotate duties. Soldiers retrace their route to the shared street before taking the next assignment. Posts and patrols remain active at night; civilians retain their work/social/sleep schedule. Pause freezes movement and animation. C visits the commander and opens a compact duty report; C again closes it. H still cycles workplaces. The commander wears heavier armor and stands by a standard near the square.

`AtlasCompanySchedule` uses the existing `CompanyCommander` identity/risk model but adds no combat decision logic to it. Atlas simulation stays separate from `BlightCombatLab`, Founding Village, its research and checkpoint saves. There are no atlas enemies, threat sensing, combat, training experience, player-issued company orders, casualties, recruitment or supply consumption in this pass. Defense coverage is a peacetime schedule, not an adaptive tactical defense model. Patrol doctrine is assumed for this visual demonstration, not granted in existing saves.

Only the nearest inhabited settlement within 190 m animates its company. Other settlements' companies are represented by their standing schedule, not continuously ticked. Revisiting reconstructs duty positions; continuous offscreen journeys and soldier persistence are future work. Troops have no crowd collision and may overlap during relief. Routes are shared with settlement roads and sampled against terrain/building boundaries in tests. Arbitrary future maps and construction require revalidation.

## Buildings and civilian/military crossover

The town has a watch house, modest barracks, and shared supply building. The fortress footprint preview adds core command, barracks, supply and guard facilities around a marked drill/muster space when plots are unobstructed. Inspect that preview on an unclaimed site for a clear layout; it is not a constructed fortress or functioning economy.

| Function | Civilian emphasis | Military emphasis |
| --- | --- | --- |
| Leadership | Hall, courthouse, palace | Command post, headquarters |
| Housing | Homes, inn | Barracks, officers' quarters |
| Food | Farms, mill, bakery, market | Mess hall, kitchens, protected stores |
| Logistics | Granary, warehouse, trade depot | Quartermaster, supply and ammunition stores |
| Craft | Smithy, carpenter, guild workshops | Armorer, repair and siege workshops |
| Security | Watch house, gatehouse, small garrison | Walls, towers, substantial garrison |
| Health | Herbalist, infirmary | Infirmary, field hospital |
| Knowledge | School, library, guildhall | Drill yard, range, military academy |
| Transport | Stables, wagon yard, docks | Remount stables, supply yard |
| Community | Tavern, shrine, square | Canteen, chapel, muster square |

These are design categories, not implemented construction/research trees. Small settlements can combine functions in one structure. Civilian and military progression remain separate.

## Terrain presentation

Fields now have seeded widths/depths and uneven side boundaries while retaining traversable worker anchors. Quarry rubble uses an irregular terrain-following apron, partially buried rock formations and scattered scree. Paths are raised slightly above soil to avoid coincident ground surfaces. Scenery uses small spatial instance batches instead of large cross-map batches; check the eight-faction medium preset as well as twelve factions when reviewing tree visibility.

## Validation

Run the PlayMode filter `Reclamation.Tests.AtlasGuardTests;Reclamation.Tests.AtlasVillageTests;Reclamation.Tests.WorldAtlasTests`, with graphics and without `-quit`. Added checks cover six soldiers per duty, full daily rotation, no teleport on relief, pause/night coverage, and route clearance on small/4, medium/8, and medium/12 presets. Packaged `--village-smoke` captures those three configurations, including commander views. The existing `--atlas-smoke` exercises map/claim/strategy/footprint behavior. Exact counts, visual findings and build warnings belong in the delivery report. These checks do not certify the target midrange-PC FPS or large-army scale.

## Opt-in scout encounter
See [Farm scout response](ATLAS_SCOUT_RESPONSE.md) for the subsequent observation, reporting and reserve-deterrence demonstration. It adds an optional encounter to the peacetime schedule described above; it does not add melee combat or integrate existing saved progression.
