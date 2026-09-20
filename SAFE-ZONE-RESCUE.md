# Safe-zone rescue checkpoint

Apply this patch after the pursuit-tuning patch. To upgrade the existing
PatientZeroLab, exit Play Mode and choose **Reclamation > Add Safe Zone to Open
Outbreak Scene**. Save the scene before pressing Play. The installer is
repeatable and does not duplicate the zone. Newly generated outbreak scenes
include it automatically.

## Play loop

The blue refuge has four spaces. The yellow quarantine area has two spaces.
Use **Evacuate** beside a civilian in the outbreak panel. Orders reserve space
immediately, but the person must physically reach the destination and can still
be chased or infected on the way. Use **Cancel** to withdraw an active order.

People who appear healthy are sent to the refuge. Visible symptoms divert an
arrival to quarantine. Latent infections remain indistinguishable from healthy
people and can therefore enter the refuge. If a sheltered latent case turns,
the refuge reports a persistent **BREACH** and releases every shelter occupant
back into the active simulation at the refuge entrance.
A quarantined person who turns remains isolated and non-contagious until the
player neutralizes them.

If a sheltered resident develops visible symptoms, the panel presents a manual
**Quarantine** action. A successful transfer prevents their later turn from
breaching the refuge. The action fails when both quarantine spaces are occupied.

Sheltered and quarantined civilians are excluded from outdoor zombie targeting.
The quarantine has separate capacity; filling it can leave a symptomatic
evacuee waiting outside. A person changing from latent to symptomatic while en
route is redirected on the next route update. The panel displays shelter,
quarantine, and en-route totals.

The pads represent an abstract secured perimeter. This checkpoint does not yet
simulate gates, guards, wall durability, food consumption, beds, or admission
staff. Evacuation is an individual command rather than a drag-selection or
district-wide order. A breach remains latched until Play Mode restarts.

## Verification

Run 35 EditMode and 35 PlayMode tests. Seven new PlayMode tests cover capacity
reservation, symptom screening, a latent breach, quarantine containment,
post-admission transfer, physical travel, and cancellation. Then test these scenarios at 1x:

1. Evacuate four apparently healthy residents immediately.
2. Wait for symptoms, then evacuate the visible case into quarantine.
3. Admit the visitor before symptoms and watch for a refuge breach.
4. Fill the refuge and verify another healthy evacuation request is rejected.
5. Cancel an en-route order and issue it to someone else.

The patch is statically checked. Unity compilation, navigation, and gameplay
behavior remain local verification gates.
