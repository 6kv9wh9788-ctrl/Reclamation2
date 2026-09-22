# Character review update

Requires the modular character workshop update already installed and tested.
Extract this ZIP into Downloads as `Reclamation2-character-review`.
Close Play mode, save scenes, and run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "$HOME\Downloads\Reclamation2-character-review\Apply-Update.ps1" -ProjectRoot "C:\Users\thoma\Reclamation2"
```

The installer checks the complete patch before applying. It does not reset files,
commit, push, or change your scenes. If the check fails, share the output.
Let Unity compile and run both EditMode and PlayMode tests.

## Workshop checklist

Open **Reclamation > Art > Open Modular Character Workshop**, then Play.

1. Two-hand grip defaults on. Try both body builds, both weapons and both armour options.
2. Select LightAttack and HeavyAttack. Enable quarter speed and cutting-edge highlight.
   Replay the attack after it finishes. Check both hands, wrists, shoulders and chest
   from front and side. The contact error should stay near zero. This measures the
   attachment points, not finger-surface or armour intersection; report visible clipping.
3. Confirm the sword's edge leads the cut and the axe reads as a bearded axe.
   Two-hand mode is a new upper-body review pose; switching it off retains the old
   one-hand clips for comparison. New poses are not yet wired into live combat.
4. Frame Face and cycle Neutral, Determined, Angry, Hurt, Shout and Smile.
   Check that the expression is understandable without reading the label.
   Try the face-identity slider and both hairstyles. These are prototype shapes,
   not the final concept-quality head. No biting animation is included yet.
5. Toggle ragdoll and recover. The existing exaggerated physics remains available
   in the workshop; no random gameplay Easter egg was added.

## Crowd baseline checklist

Stop Play, open **Reclamation > Art > Open Character Crowd Benchmark**, then Play.
Start at 16; try 32, 64, then 128 only while the editor remains responsive.
Each run builds incrementally, warms up for 5 seconds and samples for 20 seconds.
Clear/cancel stops setup or sampling and removes the characters.

Keep resolution and quality settings consistent, keep the Game window focused,
and avoid interacting during a measurement. Record the displayed average FPS and
95th-percentile frame interval. Export frame samples if useful; the location is
printed in Console. Results and machine/display information are also logged there.
The CSV is raw per-frame intervals, not GPU timings; editor/VSync/frame caps affect it.

This is a **human mesh and walk-animation baseline**, not a battle benchmark.
There is no AI, navigation, combat, horde simulation, ragdoll, facial animation or IK.
It uses shared parsed source data, but currently builds per-character meshes,
materials and a single walk clip. It has no mesh LODs or production crowd renderer.
128 is a conservative first measurement cap, not a supported final battle capacity.
A standalone-player test with combat and navigation is a later required milestone.

## Included and verified

Updated JSON/GLB model data, generator source, expression comparison PNG, installer,
and Unity regression tests. GLBs contain geometry, facial targets and the original
ten clips. The coordinated grip is implemented by the Unity runtime and is not
baked into GLB clips. Source tools require Python, numpy, Pillow and scipy.

Offline checks cover mesh data, bind poses, skin weights, animation structure,
expression arrays and target reach. The patch is checked forward and backward.
Unity is unavailable in the build environment: compilation, PlayMode/EditMode tests,
actual hand appearance and performance numbers remain unverified until you run them.

The standing art target remains the approved stylized fantasy concept: one polished
hero first, actual mesh previews, modular equipment and bodies, and honest limits.
This patch addresses review mechanics and legibility; it is not the finished hero.
