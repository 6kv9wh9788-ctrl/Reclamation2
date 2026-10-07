# Village life and checkpoints

Follow-up to the approved First Village slice, 2026-09-28. The player still controls one character and gives intent to Mara. Civilian Village and military Outpost remain separate development tracks. Shared player combat, attack/dodge timing and movement rules are unchanged.

## Commander and civilians

Mara is presented as a cautious veteran. Every two simulated seconds she reviews the platoon's fitness. The first two living soldiers above 35 health take detached scout, patrol or watchpost duties; wounded soldiers remain in reserve. The player's directive is retained when the assigned pair changes. In Defend Village, Mara inspects the approaches and assigns the fit pair to an established watchpost. If she dies, survivors hold village posts and new orders are rejected. This is a small deterministic duty policy, not a personality/relationship simulation.

Elin and Oren visibly work at the storehouse construction site with tools. The structure rises as its existing construction timer progresses. After completion, workers walk to the garden, gather provisions for three seconds, carry visible baskets to the storehouse, and increment stored food on arrival. Storage caps at 20. Nearby threats interrupt provisioning. This is the first observable production loop; food consumption, needs, workforce allocation and a full economy are not implemented. Construction remains timer-driven and is not gated on worker arrival.

## Saving and loading

Use **Save village** at the council while the player and living fighters are rested and clear of nearby combat. A successful save resumes automatically on the next normal launch. **Load saved** pauses and asks before replacing unsaved play. **R** starts a fresh unsaved session and leaves the saved checkpoint intact. There is no automatic overwrite on exit.

Default file: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Reclamation2\FoundingVillage\village-v1.json`. The path derives from Unity's `Application.persistentDataPath`; company/product changes affect it. A `.bak` file preserves the previous valid checkpoint. Writes use a flushed temporary file and replacement; malformed main saves fall back to that backup. Invalid data is rejected before resetting live play. This is version 1 with no older-save migration.

Saved: supplies in transit/delivered, timber/insight/food, technologies and active research, partial construction and completed facilities, directive/scout report, simulation clocks, fighter health/casualties/positions/facing/weapons, worker positions/cargo/gathering progress. Resume occurs at a rested checkpoint: transient attacks, defense memory and camera state are not serialized. There is no offline advancement. Previous First Village builds had no persistent session to import.

## Code map

- `Runtime/Outbreak/FoundingVillage.cs`: checkpointable ledger and bounded food storage.
- `Runtime/Outbreak/FoundingSave.cs`: versioned DTO, validation, durable replacement and backup recovery.
- `Runtime/Outbreak/BlightVillagePersistence.cs`: safe checkpoint gate, capture/restore, confirmation UI.
- `Runtime/Outbreak/BlightVillageLife.cs`: fitness review, visible civilian work and provisioning.
- `Runtime/Outbreak/BlightFoundingVillage.cs`: integrates duties, construction visuals and HUD into the existing Founding scenario.
- `Runtime/Outbreak/FoundingVillageDemo.cs` and `VillageLifeSmoke.cs`: startup resume and opt-in isolated packaged checks.
- `Tests/PlayMode/VillageLifeTests.cs`: five behavioral tests for round-trip progress/casualties, corruption, unsafe save rejection, worker output/pause, and reassigned scouting.

All paths above are relative to `Assets/Reclamation`. The existing FoundingVillage scene and build entry point are reused; other scenarios and global scene configuration are preserved.

## Repeatable validation

Close the editor before running a separate Unity process. Use Unity 6000.3.24f1 with graphics enabled:

```powershell
& $unity -batchmode -projectPath $project -runTests -testPlatform PlayMode -testFilter 'Reclamation.Tests.VillageLifeTests;Reclamation.Tests.FoundingVillageTests;Reclamation.Tests.BlightCombatFeelTests' -testResults $xml -logFile $log
```

Do not add `-quit` to the test runner. Check exit code and XML counts, including skips. For a new Windows build set `RECLAMATION_FOUNDING_BUILD` to a new executable path, then use `-batchmode -quit -executeMethod Reclamation.Editor.FoundingVillageBuild.BuildWindows`.

Packaged persistence checks use an isolated checkpoint, never the normal user save:

```powershell
& $exe --village-life-smoke --output $evidence -logFile $smokeLog
& $exe --village-resume-smoke --village-checkpoint "$evidence/test-village.json" -logFile $resumeLog
```

The first check clears raiders as setup, uses public interaction/state APIs, saves during construction, loads, finishes development, observes food deliveries and saves again. The second starts a fresh process and verifies automatic resume. These checks do not certify physical keyboard/mouse input or a natural combat playthrough. See the delivered report for exact results and visual evidence.

## Manual acceptance

Play normally, finish a storehouse and watch workers deliver baskets. Save at the council, quit, and reopen: resources, buildings and casualties should persist. Give Mara Scout Road and review her report; after Patrol Doctrine, try Peacekeep and Defend Village with the watchpost. Confirm personal attack/dodge still feels as approved. Buildings and civilian art remain placeholders; this is not a new large-army or midrange-PC performance certification.
