# Forward attacks and crowd revision 2

Requires the character-review update already installed. Extract this folder into Downloads.
Save scenes and stop Play mode, then run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "$HOME\Downloads\Reclamation2-forward-crowd\Apply-Update.ps1" -ProjectRoot "C:\Users\thoma\Reclamation2"
```

Let Unity compile, then run EditMode and PlayMode tests. No scene changes, commit,
push, Git reset, or live combat changes are included.

## Forward-cut review

Open Reclamation > Art > Open Modular Character Workshop, then Play.
Enable Two-hand grip and quarter-speed playback. Select LightAttack and HeavyAttack,
replaying each when it finishes. Check sword and axe, both body builds, and front/side views.

The weapon should now lift, cut forward and downward through the space in front of
the hero, then recover. A green rectangle marks an opponent's space in front.
It is a direction guide only: there is no target collision or damage in this workshop.
The cutting-edge highlight and off-hand error readout remain available.
Check fingers, wrist orientation, shoulder/chest clipping and whether the cut feels
aimed at an opponent. Reach checks do not guarantee the surface geometry looks right.
Light and heavy currently share this corrected upper-body path with their existing
different timing/body animation; distinct polished choreography is still future work.

## Repeat the crowd benchmark

Open Reclamation > Art > Open Character Crowd Benchmark, then Play.
Start at 16 and work up to 128. Let each run finish without camera/editor interaction.
Repeat at 2560x1440, VSync 0 and frame cap -1 to compare with your prior result:

**128 humans: 20.5 average FPS, p95 57.3 ms, Unity Editor.**

Keep quality, Game-window sizing, Scene-view visibility, laptop power mode and recording
conditions consistent. The console now identifies Crowd revision 2 and reports skinned
renderer count, material slots and shared appearance variants. At 128, expect 128
skinned renderers. Material slots are not measured draw calls. Send the complete console
result and any visual problems; an FPS improvement has not been measured here.

Changes: visible parts combined into one skinned mesh per actor, matching appearances
share meshes/materials, unused vertices and facial expression targets omitted from the
crowd representation, and constant walk channels initialized once. Face identity remains
independent per actor. Shared assets release when their last user is destroyed.
The workshop retains separate editable parts and all expressions.

Visible triangles and the count/arrangement of walkers are retained. This is still a
walk/render test without AI, navigation, combat, IK or ragdolls, not a final battle-capacity
test. Standalone measurements and CPU/GPU profiling are still needed before attributing
the remaining cost or choosing the next optimization. No mesh LOD system was added.

## Validation

Offline checks cover sampled pose reach, forward/downward weapon movement, source
geometry counts and patch application/reversal. New Unity tests cover forward attacks,
shared meshes, independent per-character face settings and animation phases, triangle
preservation, appearance switching and asset lifetime. Existing grip tests still apply.

Unity is unavailable here: compilation, Unity test results, final visual correctness
and performance gains require your local test. This remains prototype art under our
standing concept-sheet brief, not the final detailed character model.
