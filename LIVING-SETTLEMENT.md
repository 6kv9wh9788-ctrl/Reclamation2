# Living settlement: autonomous needs, farming, medicine, and morale

This checkpoint begins the original living-settlement milestone without adding another generated scene. It extends the consolidated Systems Validation Lab with **Hunger and meals** and **Energy and sleep**, and adds the same behavior to regenerated hauling and shelter labs.

## Rules

- Hunger is bounded from 0–100 and advances deterministically with simulation time.
- At 70 hunger, eating outranks ordinary hauling and construction work.
- A survivor reserves a serving before travelling, preventing two survivors from consuming the same meal.
- Eating takes two simulation seconds and relieves 65 hunger.
- Cancelled, disabled, or unreachable meal jobs release their reservation without consuming food.
- Carried wood is retained while eating; construction reservations are released so another worker can continue.
- After eating, the survivor automatically returns to settlement work.
- Energy is bounded from 0–100. It drains while awake and triggers an urgent sleep need at 30.
- Beds are exclusively reserved before travel, so two survivors cannot claim the same sleeping space.
- Sleeping restores energy to 85 before the survivor releases the bed and resumes work.
- When hunger and fatigue are both urgent, the more severe normalized need is attempted first. If its resource is unavailable, the survivor tries the other need.
- Interrupted sleep releases the bed reservation. Carried wood remains with the survivor.

Food is deliberately stored separately from the existing wood counter. Farm plots grow from 0–100%, become exclusively reservable at maturity, and yield a bounded batch of servings after a two-second harvest. Survivors carry harvested food separately from wood, deliver it to the shared food store, and keep it if travel is interrupted. Meals consume that same produced inventory.

Combat damage now records persistent physical injury on human combatants. At 25 injury, treatment outranks ordinary settlement work. A survivor exclusively reserves one finite medical supply, travels to storage, completes three seconds of treatment, relieves 65 injury, and restores combat health up to the human maximum. Cancellation releases the supply without consuming it. Medicine treats trauma only: it does not reveal or cure latent infection.

Morale is bounded from 0–100. Hunger, exhaustion, untreated injury, bites, brute sweeps, and seeing an ally fall apply pressure; defeating a threat raises group morale. At 35 morale, survivors seek a limited-capacity community gathering spot and recover to 65 before resuming work. Panicked survivors at 20 or below flee immediate combat threats instead of initiating attacks. Low morale slows decision cadence and hands-on work, while confident survivors at 80 or above receive a small work-rate bonus. Morale never overrides urgent treatment, hunger, or sleep. A general multi-job utility scorer remains a future increment.

The settlement overlay now provides five broad player focuses. **Balanced** favors food, then construction/defense, then stockpiling. **Food** makes mature crops the leading ordinary job and authorizes meals at 55 hunger. **Build/Defense** prioritizes funded construction and material delivery. **Supplies** fills storage before optional projects. **Recovery** authorizes treatment at 10 injury, sleep at 55 energy, and recreation at 55 morale. Emergency thresholds remain autonomous under every focus. Changing focus interrupts only ordinary work, releases its reservation, retains carried resources and construction progress, and causes a fresh decision; it never cancels an active meal, treatment, sleep, or morale-recovery action.

## Verify

1. Outside Play Mode, choose **Reclamation > Validation > Create or Replace Systems Validation Lab**.
2. Enter Play Mode and select **Hunger and meals**.
3. Avery-equivalent survivor 1 begins above the urgent threshold and should reserve and eat first.
4. The other survivors should continue hauling until their hunger becomes urgent.
5. Confirm the overlay shows hunger, remaining food, reservations, current state, and decision explanation.
6. Confirm an eater resumes hauling after the meal.
7. Run both EditMode and PlayMode test suites.

For sleep, select **[Settlement] Energy and sleep**. Two exhausted survivors should reserve separate beds, recover, release those beds, and resume hauling while the rested third survivor continues working. The combat panel is hidden in both settlement scenarios so the settlement decision overlay remains readable.

For production, select **[Settlement] Farming and food supply**. The store begins empty. Survivors should reserve the mature green plot, harvest three servings, visibly carry them to storage, and allow the hungry survivor to eat one. The second plot matures shortly afterward, proving regrowth and repeated supply. The overlay reports each plot's growth/reservation state plus separate wood and food cargo.

For medical recovery, select **[Settlement] Injury and medicine**. Two injured survivors should reserve the two available supplies, treat themselves, and return to work while the healthy survivor continues hauling. The overhead indicators distinguish critical injury, seeking medicine, active treatment, and untreated injury.

For psychology, select **[Settlement] Morale and recreation**. The panicked and low-morale survivors should reserve the gathering spot's two spaces, recover to steady morale, release both reservations, and resume work. The confident survivor continues working with a small productivity bonus. Combat validation also confirms that a panicked survivor flees instead of attacking and that a landed bite causes an immediate morale loss.

For player direction, select **[Settlement] Player work priorities**. Use the five focus buttons in the left overlay. Food should claim mature plots; Build/Defense should release those plots and supply the funded blueprint; Supplies should gather loose wood into storage; Recovery should make the partially hungry, tired, injured, and discouraged survivor address those conditions early. Balanced restores the default autonomous ranking.

Tests cover deterministic hunger, energy, crop growth, trauma, morale pressure, policy rankings/thresholds, exclusive reservations, actual navigation, exact production/consumption, retained cargo, safe policy replanning, combat-to-injury/morale propagation, panic behavior, and resumption of settlement work.
