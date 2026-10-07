# Sidekick player integration

## Scope and behavior

Thomas selected PF_SampleFace for the first player integration. The project-owned `Assets/Reclamation/Resources/Player/SidekickPlayer.prefab` is a variant of that complete vendor character; the source prefab is untouched. Human-Custom.sk remains a design configuration for a later custom-character bake.

The dedicated CombatDemo scene and newly created combat sandboxes use the live SidekickDuelBridge. Companions retain their existing presentation. LimbDamage retains its specialized articulated rig. Existing saved working scenes are not regenerated.

The bridge reads the existing player action/visual state, maps the humanoid skeleton, disables independent avatar scripts/colliders/root motion, and hides the original player's renderers. The generated weapon remains a cosmetic proxy. Damage, reach, hit tests, actor movement, timing, stamina, input and camera behavior remain owned by the existing combat code.

This integration adds clean replacement across scenario changes/reset, restores the original renderers on disable, freezes the live pose on pause, disables rehearsal-only unrelated-preview hiding in the live demo, and suppresses the rehearsal status overlay there. A bounded hand-reach adjustment improves support contact; mirrored palm frames, forearm twist sharing, a lower guard, and outward/forward elbow targets correct the reversed grip and the torso intersection identified in user review. Sword strike targets keep the support hand ahead of the chest; a phase-driven duck/lean supplies a dodge pose without changing the actor's displacement or immunity. No morning-review component is added to the live scene.

## Files

- Added: `Assets/Reclamation/Editor/SidekickPlayerSetup.cs`, `Assets/Reclamation/Resources/Player/SidekickPlayer.prefab`, `Assets/Reclamation/Tests/PlayMode/SidekickPlayerIntegrationTests.cs`, and their Unity metadata.
- Updated: `SidekickDuelBridge.cs`, `SidekickGroundedMotion.cs`, `BlightCombatLabBuilder.cs`, `CombatDemoBuild.cs`, `CombatDemoSmoke.cs`, and `Assets/Scenes/CombatDemo.unity`.
- Updated documentation: this report, PROJECT_MAP.md, COMBAT_ARCHITECTURE.md, WINDOWS_DEMO.md, WINDOWS_DEMO_README.txt.

## Validation

- Full baseline: 116/116 EditMode and 209/209 PlayMode, zero failures, skips or inconclusive results; both Unity processes exited 0. Command: `Tools/Run-UnityTests.ps1 -IncludeThirdParty -ResultsDirectory <outputs/sidekick-player/full-baseline>`. Evidence: `full-baseline/20260927-214740-1b0feb43`.
- After the user-requested hand/arm correction: 6/6 related PlayMode tests passed (the four integration tests and two existing rehearsal tests), zero failures/skips, Unity exit 0. Command uses `-runTests -testPlatform PlayMode -testFilter Reclamation.Tests.Sidekick`; evidence `clearance-03.xml` and `.log`. The full 325-test run preceded this final presentation-only correction; it was not rerun afterward.
- Windows build-02 succeeded (Unity 6000.3.24f1, x64 Development). Existing package/shader warnings remain (485 reported by BuildReport); no vendor/package upgrades were made.
- Packaged-player smoke passed all 11 scenarios, including Sidekick installation, player ownership, LimbDamage fallback, reset, pause, material support and Company planning/recall; executable exited 0. Command: `Tools/Test-WindowsDemo.ps1 -BuildDirectory <build-02> -ResultsDirectory <smoke-01>`. Its Squad image was visually inspected.
- Final package: `Reclamation-Sidekick-Player-Windows-x64.zip`, built from build-02. Earlier build-01 and earlier pose captures are superseded. The original pre-Sidekick Windows demo ZIP remains untouched.

Evidence is under this Codex task's `outputs/sidekick-player`. Incidental Unity changes to three URP assets and ProjectSettings were restored to the exact starting bytes. Newly generated Sidekick database copies were archived and removed only after confirming they were absent at sprint entry. `preservation-audit.json` records the final changed/added files; core combat, earlier companion/Company changes, vendor originals and existing settings were preserved.

Four new integration tests cover player-only replacement/reset/disable/fallback, action-state preservation and weapon grips across light/heavy Sword/Spear/Axe attacks, pause and physics-driver isolation, and eight review poses without changing live combat state. The review-pose test also samples both forearms against a conservative torso core in idle, light/heavy contact and block; an index-knuckle orientation assertion rejects the original reversed right-hand grip. Existing combat regression suites remain the mechanical baseline. Grip distances measure attachment targets, not visual polish.

Pose captures use the actual Unity camera and wait for a frame so skinned geometry consumes the current pose. Idle, walk, light, heavy, block, dodge, hit and defeat are captured, including close-up front and side views. User feedback correctly rejected the earlier wrist/arm poses; final evidence is poses-clearance-03. The core-volume test is not mesh collision detection and cannot certify every animation frame or armor intersection. Packaged smoke renders the game camera offscreen; it does not certify HUD layout, physical input, motion quality or subjective combat feel.

## Hands-on check

Extract the full new Windows ZIP and launch `Play Demo.cmd`. Start with Duel, then Squad and Hulk. Walk, sprint and strafe; light/heavy attack; block; dodge in several directions; switch sword/spear/axe with X; pause and reset. Check that the character and weapon remain connected, feet and body motion read clearly, the original player is hidden, companions remain visible, and familiar attack/dodge timing feels unchanged. Switch to LimbDamage and back to confirm the specialized fallback. Send a recording with the scenario and action if there is clipping, sliding or an awkward pose.

In the editor, open `Assets/Scenes/CombatDemo.unity`, or use `Reclamation > Testing > Create Combat Sandbox`. `Reclamation > Art > Prepare Sidekick Player Demo` refreshes the saved demo wiring with normal unsaved-scene handling. Save and close the editor before batch tests/builds.

## Limitations and next scope

This is an initial procedural player presentation pass, not an authored animation set. Weapons still use simple generated geometry; fine finger contact, armor intersections, gait and transitions need human motion review. No companion avatar replacement, custom Human-Custom bake, combat retuning or release optimization is included. The prefab variant depends on the installed vendor source; do not delete that source. This remains a Development build.

Review the new player in motion before expanding to a custom character or companion visuals. Keep any subsequent animation polish scoped to presentation, with the existing combat contracts and regression suites retained.
