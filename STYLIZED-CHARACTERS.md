# First stylized character set

Original, generated Company Fighter and Blighted Thrall meshes. The art direction is
faceted cloth/metal/leather, clear silhouettes, warm accent colours and ochre blight
growths. These are editable first-pass assets, not final reference-image-quality art.

## In the existing combat lab

Choose **Limb damage**. Both actors now use the new mesh parts on the existing combat
joint hierarchy. Attack/dodge timing, sword/axe hit positions, health and limb rules
are unchanged. The lab's existing procedural poses drive the new meshes, so timing
stays linked to gameplay. The standalone animation library does not replace that logic.

Arm and leg parts follow their existing joints when severed; the existing wound caps
and G reduced-gore option remain in use. Other five scenarios keep their current art.
No imported character package, Blender installation or glTF Unity importer is required.

## Animation preview

Choose **Reclamation > Art > Preview Stylized Characters**, then press Play. This opens
a separate, unsaved preview scene after Unity's normal save prompt. Buttons play:
Idle, Walk, Run, LightAttack, HeavyAttack, Dodge, Block, Stagger, Death, Crawl.

The same ten clips per character are embedded in the supplied GLBs. They are basic
keyframed motion studies using rigid segment weights, not polished mocap. Locomotion
is in place; the Dodge clip includes a small local body offset. Head/hand acting,
foot contact, transitions, skin deformation and final weapon animation need refinement.

## Blender files

The downloaded package includes Models/CompanyFighter.glb and Models/BlightedThrall.glb.
Import through Blender's glTF importer to inspect meshes, materials, armature and actions.
Each character has ten bones. Segments are closed meshes with rigid skin weights.
The human GLB includes a sword prop; live Unity combat keeps its gameplay weapon instead.

Optionally run Convert-Blender.ps1 from the package folder to create .blend and FBX
copies using your installed Blender. The converter runs in a fresh background process;
do not execute the Python script inside a Blender session with unsaved work.
Blender conversion has not been executed in the authoring environment.

## Playtest checklist

1. Let Unity compile. Run EditMode/PlayMode tests, including StylizedCharacterArtTests
   and StylizedCharacterPlayTests. Check Console for shader errors or pink meshes.
2. In the art preview, inspect each of the ten clips and replay single attacks.
3. In Limb damage, inspect the human's hands on sword and axe during arm/leg swings.
4. Check T stationary practice, Z aiming, blocking, dodging, heavy interrupts and death.
5. Sever arms and legs; verify visible mesh parts detach, reduced gore hides debris,
   the crawler still moves/attacks, and R restores the complete model.
6. Repeat the existing combat tests. Report any mismatch between visible contact and damage.

## Scope and validation

Company Fighter: 3,676 triangles including preview sword; Thrall: 3,326 triangles.
The supplied render/GIF comes from actual generated geometry and motion data. It is a
software preview; Unity lighting, camera and live combat poses will differ.
The URP shader supplies flat light bands and dark grazing-angle edges. It is not a
full screen-space outline/environment rendering system. Materials use palette colours,
without painted texture maps or UV detail. Mesh groups are unoptimized; LODs, draw-call
reduction, soft skinning, hands and face refinement remain future art work.

Local validation: GLB structure/accessors, weights, bind matrices, mesh normals/indices,
animation times/quaternions, Python syntax, preview rendering, and patch application.
Unity compilation, shader compilation and Unity test execution require your editor.
