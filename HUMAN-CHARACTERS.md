# Simple human characters

Apply this patch after Patient Zero. In Unity, open your existing lab scene,
exit Play Mode, and choose **Reclamation > Upgrade Open Scene to Human Characters**.
Save the scene and press Play. Repeating the command does not duplicate models.
Undo can remove the upgrade. Newly generated labs also use the human visuals.

These are original block-style placeholders with clothes, hair, eyes, and
procedural arm/leg swings. No asset downloads or packages are required.
The figures appear in the Scene view as well as during Play Mode.

Navigation, root colliders, infection logic, and reservations are unchanged.
Child geometry has no colliders and uses the lab's excluded navigation layer.
Latent cases retain their healthy appearance. Symptomatic heads become yellow;
turned heads become green and arms extend forward. Clothing retains identity
colors; use the existing status panel to inspect isolation. Neutralized actors
still disappear. Animation freezes with the neighborhood clock.

The upgrade changes only the open scene and does not overwrite it automatically.
Save it to retain the change. Generated materials are reusable assets.

## Local verification

Unity compilation and visual verification must be performed in the editor.
Run the existing 30 EditMode and 15 PlayMode tests. In Patient Zero, check walking,
pause/resume, 4x/12x speed, symptoms, turning, isolation, and neutralization.
Check that feet meet the ground and visuals do not obstruct movement.
These simple figures are not rigged Humanoid assets or final production art;
the per-part renderers are suitable for the small labs, not city-scale crowds.
