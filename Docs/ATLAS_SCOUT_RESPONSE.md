# Farm scout response encounter

This opt-in atlas encounter connects the local company's peacetime duty schedule to a small observed threat. Press R near your initially inhabited town to introduce three hostile scouts near the farm approach. Nothing starts automatically, and only one encounter runs at a time. No combat or saved progress is changed.

The farm patrol must come within 22 m and have an unobstructed eye-level line of sight. Generated building colliders and sampled rendered terrain can block observation. Vegetation is not fully collidable and does not conceal these scouts. The player can physically see the scout models without the commander receiving an automatic report.

After a sighting, a three-simulation-second reporting delay precedes reserve dispatch. This is an abstract communication delay, not a rendered messenger or integration with the communication lab. The six reserve soldiers return along their current route to the shared street, then follow the existing outer road to the farm approach. Fixed posts hold; duty rotation is suspended during the encounter. Town and farm patrols continue their assigned routes.

G becomes available after a sighting as an explicit survey shortcut to the reported location. The player can also walk there normally. E challenges the scouts only within 14 m and with at least three reserve soldiers within 10 m of the contact. Supported challenges cause the scouts to withdraw. Without the player, sustained reserve presence also deters them. The commander orders no pursuit, and ordinary duty routes resume after withdrawal. Unobserved scouts or an unsuccessful prolonged response leave without a confirmed defense success.

The event uses simulation time scaled by the atlas time button. At 1x, each simulation second advances one game minute. T changes the day clock but does not instantly complete report, travel, support or withdrawal timers. Esc pauses the event and its actor animation. Map changes reset it. R can replay a finished event. During an active event the existing bounded company pool stays assigned to the encounter site, even if the explorer walks away; this prevents survey travel from silently cancelling the defense.

## Limits

This is deterrence, not melee combat. Scouts do not attack or damage soldiers, villagers, buildings or resources. There are no casualties, rewards, loot, research, recruitment, save persistence or territorial changes. Hostile scout identity is a demonstration role, not a persistent strategic faction war. There is no civilian evacuation or crowd separation. Soldier patrol/duty logic remains separate from the combat and Founding scenarios. The existing attack/dodge contract and command hierarchy prototypes are untouched.

The local encounter has finite observation/response timeouts. Scout spawn/withdrawal lanes are checked against terrain and building obstruction; unsuitable future layouts reject the encounter instead of spawning on blocked ground. Terrain sight uses sampled height tests, not a complete visibility solver, and all future maps/building placements need validation. The three scouts use simple existing modular rigs. This is not evidence of target-hardware performance or large-army battle scale.

## Source and validation

`AtlasScoutResponse.cs` contains the state machine, line-of-sight sampling, opt-in controls, local actors, response routing, compact encounter card and standalone smoke capture. `AtlasSettlementGuard.cs` holds the current watch and pins the pool to the encounter site while active; `WorldAtlasVillage.cs` ticks the encounter after guard movement. Atlas input, HUD and map lifecycle supply the remaining hooks.

Run PlayMode filter `Reclamation.Tests.AtlasScoutTests;Reclamation.Tests.AtlasGuardTests;Reclamation.Tests.AtlasVillageTests;Reclamation.Tests.WorldAtlasTests` with graphics and no `-quit`. Run the Windows executable with `--scout-smoke --output <folder>` to exercise and capture sighting, report, reserve arrival, player challenge and withdrawal on small/4, medium/8 and medium/12. Existing village and atlas smokes remain available. Exact results belong in the delivery report.
