# Atlas response guide

The upper-left encounter card identifies the company-assigned responding platoon
by its persistent commander name and platoon number. It reads the reserve assignment
after report delivery, not the reporting patrol or nearest guard. Before delegation,
the existing unseen/reporting cards remain in use.

The card displays Travelling, Deploying, Holding, Returning, then Watch resumed.
Deploying and Holding come from the actual formation phase, not the support count.
The outcome remains distinct: an escaped contact is never called secured. The G
hint explicitly visits the CONTACT; it does not teleport to or follow the commander.

The active response uses an amber square on the minimap, an amber outlined commander
badge and company-roster row. Map labels show the platoon number, and the legend
names its commander and state. Ordinary patrols retain normal markers. A response
leader outside the minimap is clamped to its edge and explicitly labeled off map.
World badges retain existing occlusion, range and HUD-overlap rules.

Highlighting persists through the completed encounter's return, then clears.
A presentation-only completion latch prevents a later routine watch rotation from
reactivating an old response highlight. A new encounter or map load resets the
guide. A different local company never inherits the previous company's marker.

`AtlasResponseGuide.cs` owns the read-only view of command/formation state and its
UI helpers. `AtlasScoutResponse.cs`, `AtlasSettlementGuard.cs` and `WorldAtlasRecon.cs`
draw the card, roster and markers. No orders, perception, fog reveal, movement,
timing, combat, player controls or troop counts are changed.

Existing AtlasPlatoonTests and AtlasFormationTests now also verify report-delivery
gating, reporter/responder distinction, named identity, actual travel/line/return
states, completion and map-reset clearing across the three presets.

Manual review: R starts scouts, C shows the roster, G visits contact, and F3 shows
diagnostics. Follow the amber reserve instead of the reporting farm patrol; let
the encounter finish and watch its guide change through return and completion.
