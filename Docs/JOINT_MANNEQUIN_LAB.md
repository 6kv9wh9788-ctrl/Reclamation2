# Joint mannequin experiment

This isolated lab explores Thomas's proposal to build a character as separate body parts around a joint hierarchy. It leaves the working Sidekick player, CombatDemo scene, combat rules, and existing Modular Character Workshop unchanged.

## Delivered study

- A new 43-joint hierarchy: pelvis, waist, chest, neck/head, shoulders/elbows/wrists, finger segments/thumbs, hips/knees/ankles.
- Project-owned procedural loft meshes for torso, pelvis, upper/lower arms, palms, head, thighs and shins; simple feet and fingers; exposed joint caps. No vendor meshes are used.
- Rigid separate body pieces make joint positions and intersections visible. This is an articulated mannequin, not the proposed final deforming body/cloth system. Joint seams, simple hands, and placeholder feet/head are deliberate first-pass limits.
- Relaxed, walk, guard, light attack, heavy attack, block and dodge inspection motions. Arm chains are posed first and the inspection sword follows the wrist. The free arm stays independent; no two-handed grip solver is added.
- Walking uses the existing `ModularHumanRig.SolveGrip` helper for planted/swing foot targets. That helper and the old modular rig are unchanged.
- Orbit/zoom, turntable, upper-body framing, joint highlighting, slow playback and a phase scrubber.

These motions run in place on a normalized lab timeline. They are not combat simulation and do not change or certify player attack timing. No armor/clothing, body swaps, asset export, Humanoid Avatar, retargeting, production transitions, crowd optimization or live-player replacement is included.

## Open and inspect

Extract the entire standalone ZIP and run `Open Mannequin Lab.cmd`. Or open `Assets/Scenes/JointMannequinLab.unity` and press Play in Unity 6000.3.24f1. The editor menu is **Reclamation > Art > Open Joint Mannequin Lab**; it respects unsaved-scene handling.

Start with Relaxed and inspect the silhouette from front/side/back. Check shoulder width, arm length and hands at the sides. Select each motion, pause it, and drag the pose phase through the whole cycle. Watch upper arms, elbows and forearms for torso intersections; watch feet in Walk/Dodge. The sword toggle shows that the prop follows the hand. Use joint highlighting to examine the attachment chain.

Approve or correct body proportions and motion before adding continuous skin or a first clothing/armor set. A later comparison with Sidekick should use equivalent combat events; this lab does not yet provide an in-game comparison.

## Files and rebuild

New runtime: `Assets/Reclamation/Runtime/Outbreak/JointMannequin.cs`, `JointMannequinLab.cs`.
New editor setup: `Assets/Reclamation/Editor/JointMannequinLabBuilder.cs`.
New tests: `Assets/Reclamation/Tests/PlayMode/JointMannequinTests.cs`.
New scene: `Assets/Scenes/JointMannequinLab.unity`. Each Unity asset has its own metadata. No existing scene is rewritten and the global build scene list is unchanged.

For a Windows build, set `RECLAMATION_MANNEQUIN_BUILD` to a new absolute EXE path, close this project's editor, then launch Unity with `-batchmode -quit -projectPath <project> -executeMethod Reclamation.Editor.JointMannequinLabBuilder.BuildWindows -logFile <log>`. Only the mannequin scene is supplied to BuildPipeline.

## Validation

- Final targeted Unity PlayMode run: **5 passed, 0 failed, 0 skipped/inconclusive**, Unity exit 0. Includes 3 JointMannequinTests plus 2 existing ModularHumanPlayTests. Filter: `Reclamation.Tests.JointMannequinTests;Reclamation.Tests.ModularHumanPlayTests`. Captures enabled with `RECLAMATION_MANNEQUIN_CAPTURES`. Evidence: `outputs/mannequin/tests-02.xml`, `tests-02.log`, and `poses-02` (21 actual Unity renders).
- Windows x64 Development build-02 succeeded, Unity exit 0, 293,902,924 bytes reported. The existing project/package shader warning class remains (485 BuildReport warnings). No package changes were made.
- Packaged executable smoke passed all 7 poses and exited 0 (`smoke-summary.json`, `smoke.log`). This verifies initialization, not physical input or subjective motion quality.
- Only this isolated addition and the existing modular tests were run; the full combat baseline was not rerun. Existing combat sources and assets were verified unchanged by the preservation audit.
- Final deliverable: `Reclamation-Joint-Mannequin-Lab-Windows-x64.zip`, from build-02. Build-01 and poses-01/poses-final are intermediate artifacts.
- Unity's incidental edits to ProjectSettings and three URP assets were reviewed and restored byte-for-byte. Newly generated vendor database copies were archived/removed after inventory and content checks. `preservation-audit.json` lists the new files and verifies all preexisting inventoried files remain unchanged.

The new tests sample 121 points in each of seven motions for stable joint offsets/limb lengths, finite transforms, fixed weapon attachment, and conservative torso clearance for both upper arms and forearms. They also verify the hierarchy, absence of a combat lab/physics-driven mannequin, and repeatable pose sampling. Render captures cover front, side and three-quarter views. The clearance volume is a conservative geometric check, not full mesh collision detection or certification of natural motion. Offscreen renders do not validate the IMGUI controls or physical mouse input.

The prototype uses many separate renderers to keep the body parts inspectable. It has not been profiled or optimized for crowds. The next stage needs visual feedback, followed by a deforming joint/skin experiment and one equipment slot before a full wardrobe or character creator.
