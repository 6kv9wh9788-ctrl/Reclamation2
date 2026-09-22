# Modular human foundation — beginner playtest guide

This update adds an isolated **Character Workshop** to test the structure of future
characters. It can be installed independently of the earlier character-art update. It is not the finished
high-detail fighter from the concept sheet, and does not replace the live combat rig yet.

## Open it

Let Unity compile, run EditMode and PlayMode tests, then choose:
**Reclamation > Art > Open Modular Character Workshop**. Save your existing scene if
prompted, then press Play. The workshop opens as a separate unsaved scene.

## Things to try

1. **Body** switches between broad and athletic builds. These are initial masculine/
   feminine proportion studies, not gender restrictions; faces, hair and gear are independent.
2. Drag the **Face** slider from angular to softer/narrower jaw and chin. Use **Face**
   framing to inspect it. This is one shape control, not a finished face creator.
3. Switch short/long hair, light/heavy armour, sword/axe. Try every combination.
   Heavy armour hides its covered torso cloth to avoid internal clipping.
4. Play Idle, Walk, Run, attacks, Dodge, Block, Stagger, Death and Crawl. Both builds
   use the same animation names and hierarchy. Right-drag or turntable orbits the camera;
   scroll zooms. **Body**, **Face** and **Hands** are quick close-up buttons.
5. Enable **Two-hand grip test** and try both weapons. This positions the left arm
   toward a weapon-specific grip point without stretching the arm. Unreachable poses
   clamp to arm length; grip orientation and posing still require artist refinement.
6. Enable **Jaw and eye motion test**. This checks that those controls deform visible
   geometry. It is not lip sync or a facial-expression system.
7. Press **Test ragdoll** and watch it collapse onto the floor. **Recover** restores
   the pose and animation; it does not yet play a get-up animation. Repeat several
   times and check for flying limbs, persistent jitter or disappearing equipment.

## What is underneath

- Shared 57-bone skeleton: root, pelvis, spine, chest, neck, head, jaw, eyes, clavicles,
  arms, hands, 30 finger bones, legs, feet, toes and two weapon sockets.
- Actual skin weights with torso, elbow/knee transitions and jaw/foot blends, using
  Unity SkinnedMeshRenderer. Armour stays rigid where appropriate.
- Independent equipment mesh groups and body coverage rule; changing items preserves
  the skeleton. The prototype has two complete armour loadouts, not per-slot inventory UI.
- Eleven rigidbodies, capsule colliders and constrained joints for a ragdoll prototype.
  Internal collision pairs are ignored for initial stability. Weapons have no physics colliders yet.
- Mesh groups retain limb-region labels for future severing. Migrating the combat
  dismemberment implementation to this skinned rig, caps and detached skin baking is
  still future work. The existing ten-bone combat characters continue to handle that test.

## Limits and next work

This is a technical foundation, using revised prototype geometry. It does not deliver
the detailed concept face, sculpted armour finish, painted textures, soft joint seams,
cloth simulation, LODs, foot planting, production animation transitions or a tuned
combat ragdoll. Both GLBs contain the common clips and a SoftFace shape key. Unity uses
generic clips for this test; an imported Humanoid Avatar/Animator migration is not done.

No Blender is needed for the workshop. The package also includes animated GLBs and
reproducible Python source for editing outside Unity. Local checks cover generated
geometry, skin weights, skeleton compatibility, animation structure, Python syntax
and patch application. Unity compile, skinning, physics and the new test runs need
your editor. Please send the first Console error and a screenshot if something fails.
