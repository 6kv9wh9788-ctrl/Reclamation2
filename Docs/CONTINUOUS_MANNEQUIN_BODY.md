# Continuous mannequin body experiment

This extends the approved Joint Mannequin Lab with a continuous skinned body and rounded, shorter feet. The original articulated body remains available with the **Continuous body** checkbox. Both displays use the same 43 joints, joint positions and seven inspection motions. No live player, combat timing, Sidekick prefab, armor or clothing is replaced.

## Open and review

Extract the entire standalone ZIP and run **Open Body Lab.cmd**. In Unity 6000.3.24f1, use **Reclamation > Art > Open Joint Mannequin Lab**, then Play. Compare both body styles in Relaxed, Guard, Heavy attack, Block, Walk and Dodge. Pause and scrub the phase; orbit to inspect shoulder, elbow, hip and knee bends from several angles. Check the foot profile and floor contact.

The first offscreen captures were contaminated by an unrelated Sidekick character in the editor's open scene. The capture tests now restrict their camera to layer 30 and assign only their disposable lab objects to that layer. Use poses-final, not poses-01, for review. The saved standalone lab scene contains only the isolated lab root.

## Implementation

- `JointMannequinSkin.cs`: project-owned implicit surface meshing, shared-edge welding, normalized four-bone weights and a SkinnedMeshRenderer. Head, torso, limbs and digits form one connected closed mesh. This component does not pose the skeleton.
- `JointMannequinSkinBaker.cs`: generates `Resources/Player/ContinuousMannequinBody.asset` in an editor preview scene. Use **Reclamation > Art > Rebuild Continuous Mannequin Skin** after changing the generator or skeleton. The lab loads this mesh; procedural generation remains a fallback.
- `JointMannequinLab.cs`: body comparison control and packaged smoke check.
- `JointMannequin.cs`: cleanup now supports edit-mode baking; hierarchy and pose code are unchanged.
- `JointMannequinSkinTests.cs`: topology, weights, deformation bounds, pose-preserving display switch and rendered comparisons. Existing mannequin capture tests also isolate their camera.

The current body has 60,538 vertices and 121,072 triangles after the leg-proportion refinement. Generation takes about nine seconds on this machine; baking avoids that cost at startup. It is a deformation study, not a production topology or crowd-performance solution.

## Rebuild and verification

Close the project editor before launching a separate batch process. Use Unity 6000.3.24f1 with `-batchmode -quit -projectPath "C:\Users\thoma\Reclamation2" -executeMethod Reclamation.Editor.JointMannequinSkinBaker.Bake -logFile <bake.log>` to rebuild the mesh.

Run PlayMode tests with `-batchmode -projectPath "C:\Users\thoma\Reclamation2" -runTests -testPlatform PlayMode -testFilter "Reclamation.Tests.JointMannequinSkinTests;Reclamation.Tests.JointMannequinTests;Reclamation.Tests.ModularHumanPlayTests" -testResults <results.xml> -logFile <tests.log>`. Do not add `-quit` or `-nographics` to this rendered test run. Set `RECLAMATION_BODY_CAPTURES` to an output directory for the comparison PNGs.

Set `RECLAMATION_MANNEQUIN_BUILD` to a new absolute executable path, then invoke `Reclamation.Editor.JointMannequinLabBuilder.BuildWindows` with batchmode/quit. Run the resulting executable with `--mannequin-smoke -logFile <smoke.log>` to exercise all seven poses and the display switch.

## Visual limits and next decision

The feet have rounded heels and tapered toes instead of cubes. Raised-arm poses still show shoulder/armpit pinching, and the hands and face remain simple. Passing closed-surface and bounded-deformation checks does not prove natural anatomy or absence of self-intersection. Offscreen captures do not validate the interactive HUD or physical mouse input. Review this body before adding an equipment slot; refine shoulder weights/topology first if that pinching is unacceptable.

## Verified run - 2026-09-28

- Focused PlayMode: 8 passed, 0 failed, 0 skipped (20.07 seconds). Includes three new skin tests, three mannequin tests and two existing modular-human tests. This was not a full combat-suite rerun.
- Windows x64 build succeeded; Unity reported 485 warnings, including package shader warnings. See build-01/build.log for details.
- Packaged executable exited 0: BODY_SKIN_OK, all seven MANNEQUIN_POSE_OK markers, and MANNEQUIN_SMOKE_PASSED. Cached mesh setup rounded to 0.00 seconds; this excludes player launch/assembly loading.
- ZIP integrity passed: 317 entries, 99,014,283 bytes. Prior mannequin and Sidekick packages were retained.
- Preservation audit: 1,649 original files inventoried, 1,646 unchanged, three intended existing-file changes, no missing files. JointMannequin changes are cleanup-only; all pose code is unchanged. Incidental Unity settings and generated vendor database copies were restored/archived after verification.
- Evidence in the workspace: outputs/continuous-body/tests-final.xml, poses-final, packaged-smoke.log, preservation-audit.json and package-result.json.


## Leg proportion refinement - 2026-09-28

Following the user's 34.8-second recording and feedback that the legs were too thin for the upper body, increased mid-thigh radius from .076 to .092 m (21%) and calf radius from .056 to .072 m (29%). The upper-thigh and knee transitions increased modestly; ankle radius, feet, upper body, joint positions, bind pose and motion code are unchanged. Only the skin generator, its baked mesh and this document changed.

Rebaked with JointMannequinSkinBaker.Bake, then ran the three JointMannequinSkinTests: 3 passed, 0 failed, 0 skipped in 20.15 seconds. These exercise surface connectivity/closure, normalized weights, pose-preserving body switches and bounded deformation across seven poses. Reviewed rendered relaxed and crouched views. The earlier shoulder pinching and simple anatomy limitations remain. The full combat suite was not rerun for this isolated geometry revision.

Evidence and the new standalone ZIP are in the task workspace under outputs/body-proportions; the prior continuous-body package remains available for comparison.

The revised Windows build succeeded (485 shader warnings). Packaged smoke exited 0 with all seven pose markers and MANNEQUIN_SMOKE_PASSED. ZIP integrity passed (99,107,357 bytes). Final audit: 1,658 files inventoried, 1,655 unchanged, only the three files named above changed, none missing or added.


## Running and shoulder refinement - 2026-09-28

Added Run as the eighth enum value, preserving previous serialized values. The in-place cycle alternates a 38% support period with lifted foot recovery, a short flight interval, bent opposing arms, torso lean/twist and pelvis bounce. Run defaults unarmed and plays at 1.4 cycles/second with the default half-speed slider. The phase slider still addresses one complete cycle. There is no actor travel/root motion, animation clip export, transition controller or gameplay integration.

The skin now blends chest-to-arm across a wider anatomical region and uses two intermediate shoulder transforms, each following half of the corresponding shoulder rotation. These 45 skinning transforms comprise the original 43 articulation joints plus two cosmetic helpers; existing joint parents, positions, body proportions and attack pose curves remain unchanged. PoseSampled updates the helpers synchronously, including scrubbing and automated mesh baking. Regenerate the cached mesh after changes to this binding setup.

Changed files: JointMannequin.cs (Run and pose notification), JointMannequinLab.cs (eighth button, unarmed selection and run playback rate), JointMannequinSkin.cs (shoulder weights/helpers), ContinuousMannequinBody.asset (rebaked weights), JointMannequinTests.cs (run loop/support regression), JointMannequinSkinTests.cs (run capture), and this report. No live player or combat files changed.

The new run regression verifies seam continuity, alternating left/right support, a flight interval, floor clearance of ankle targets and the unarmed default. Existing tests also sample eight poses for stable limb lengths, torso clearance, closed connected skin, normalized weights and finite bounded deformation. Those checks do not certify anatomical realism, sole collision or physical user input.

Final verification: 7 focused PlayMode tests passed, 0 failed, 0 skipped (21.89 seconds). Windows build succeeded with 485 shader warnings; packaged smoke exited 0 with eight pose markers including Run. ZIP CRC passed (99,118,061 bytes). All 1,658 preexisting files retained; only the seven scoped files listed above changed after incidental settings/vendor restoration. Captures in outputs/body-run/poses-02 show better raised-shoulder volume, with a remaining crease at the deepest armpit bend. This is refinement, not final anatomical validation. The full combat suite and interactive mouse controls were not rerun.
