# Living settlement: hunger and autonomous meals

This checkpoint begins the original living-settlement milestone without adding another generated lab. It extends the consolidated Systems Validation Lab with **Hunger and meals** and also adds the same behavior to regenerated hauling and shelter labs.

## Rules

- Hunger is bounded from 0–100 and advances deterministically with simulation time.
- At 70 hunger, eating outranks ordinary hauling and construction work.
- A survivor reserves a serving before travelling, preventing two survivors from consuming the same meal.
- Eating takes two simulation seconds and relieves 65 hunger.
- Cancelled, disabled, or unreachable meal jobs release their reservation without consuming food.
- Carried wood is retained while eating; construction reservations are released so another worker can continue.
- After eating, the survivor automatically returns to settlement work.

Food is deliberately stored separately from the existing wood counter. Farming, food hauling/production, sleep, energy, morale, medicine, and a general multi-job utility scorer remain future living-settlement increments.

## Verify

1. Outside Play Mode, choose **Reclamation > Validation > Create or Replace Systems Validation Lab**.
2. Enter Play Mode and select **Hunger and meals**.
3. Avery-equivalent survivor 1 begins above the urgent threshold and should reserve and eat first.
4. The other survivors should continue hauling until their hunger becomes urgent.
5. Confirm the overlay shows hunger, remaining food, reservations, current state, and decision explanation.
6. Confirm an eater resumes hauling after the meal.
7. Run both EditMode and PlayMode test suites.

New tests cover deterministic hunger growth and relief, exclusive and released meal reservations, actual navigation to food, exact consumption, and resumption of hauling.
