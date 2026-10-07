# Atlas company and platoons

The inhabited atlas company now has four persistent, named platoon commands. For this demonstration each platoon contains one commander and five soldiers: 24 platoon personnel plus the company commander, 25 in total. This reduced strength is not the final platoon size. The existing combat lab and Founding command systems remain separate.

## Behavior

The company interprets its standing **Defend settlement** intent as four concurrent duties: gate posts, town patrol, farm patrol and reserve. The same named platoons rotate through these duties every six game hours. Routes, movement speed, coverage and watch timing remain the previously approved demonstration behavior.

The farm platoon's observations go through its commander. The existing three-simulation-second report delay must complete before the company delegates **Secure approach** to the reserve platoon. That platoon's six members, including its commander, follow the validated response route. Other platoons retain their assignments and relief is suspended during the encounter. The reserve commander reports once when at least three members are in supporting range and again when the incident ends. The company retains the last six received reports. The response can deter the scouts autonomously, or support the player's existing challenge. It does not add combat or damage.

Platoon identities survive duty changes and local visual pooling within the loaded map. Reloading the map starts a fresh command model and report history. This is not save-game persistence. Reports and orders are a bounded deterministic state machine, not a general planning AI, radio propagation model or simulated messenger system. A platoon member's observation is available to its commander without a separate internal communication delay; the existing company report delay remains enforced by the encounter.

## Presentation and playtest

1. Press **C** near the home settlement to visit its company commander and show the four-platoon roster. Each row names a commander and its current delegated duty. C also closes the panel.
2. Company commanders have a red crest and two rank stars. Platoon commanders have pale crests and one star, a name and a platoon-number badge. The minimap uses gold for the company commander, pale markers for platoon commanders and green for troops. Badge health remains the explicitly labeled 100 HP demonstration placeholder.
3. Press **R** to start the optional scout encounter. Wait for the farm patrol's report, then watch the reserve platoon receive Secure approach. **G** visits the reported contact; **E** challenges when nearby with support. Alternatively let the response finish without intervening.
4. Press **F3** while the C roster is open to show the clearly labeled **DEBUG / COMPANY REPORT HISTORY**. It records the reporting commander, in-position report and outcome. Hide it with F3 when not recording diagnostics.
5. After the incident, press **T** to review a later watch. Names and platoon identities stay consistent as duties change; soldiers walk through relief instead of teleporting. Escape pauses the simulation.

## Code and validation

- `AtlasPlatoonCommand.cs`: persistent platoon identities, delegated orders, report gating and bounded company history; extends `AtlasCompanySchedule`.
- `AtlasSettlementGuard.cs`: company watch delegation, platoon membership, movement consuming the assigned duty, and roster UI.
- `AtlasScoutResponse.cs`: observation-to-report-to-order integration and outcome reporting.
- `AtlasCharacterVisual.cs` / `WorldAtlasRecon.cs`: crests, rank badges and minimap markers.
- `AtlasPlatoonTests.cs`: stable leaders, duty coverage, report gating, exclusive response, pause, leader movement, automatic deterrence, watch resumption and reset across all three UI map presets.
- `WorldAtlasPlatoonSmoke.cs`: packaged validation and captures, invoked with `--platoon-smoke --output <absolute folder>`.

Run PlayMode with filter `Reclamation.Tests.AtlasPlatoonTests;Reclamation.Tests.AtlasWildlifeTests;Reclamation.Tests.AtlasVisibilityTests;Reclamation.Tests.AtlasScoutTests;Reclamation.Tests.AtlasGuardTests;Reclamation.Tests.AtlasVillageTests;Reclamation.Tests.WorldAtlasTests`. Retain graphics and omit `-quit` for tests. Build using `Reclamation.Editor.WorldAtlasBuild.BuildWindows` with a fresh `RECLAMATION_ATLAS_BUILD` path.

No Army/Battalion implementation, recruitment, casualties, succession, arbitrary platoon orders, larger troop counts, final formation tactics or performance-target certification is included. The player attack/dodge systems, original character mesh libraries, map terrain and approved wildlife behavior are unchanged.

Formation follow-up: [Atlas platoon movement](ATLAS_FORMATIONS.md) supersedes the earlier movement and badge details: shared road columns, spaced patrols, defensive line deployment, return to watch and compact rank badges. F3 now includes formation phases under DEBUG / COMMAND AND FORMATION.
