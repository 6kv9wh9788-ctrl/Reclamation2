# Weapon identity and equipment sprint

Requires the combat-feedback update. Use the existing Blight Combat Lab: no new scene.

- B opens/closes equipment comparison. It opens automatically in Weapons.
- Click Equip or press X to cycle sword, spear, axe. Five metres of space is required.
- Combat remains live with the panel open. Esc pauses; equipping is disabled while paused.
- Limb damage cycles sword/axe. The spear is excluded until it has a thrust contact implementation.
- Patrol retains its original sword/spear reward progression; axe is a lab loadout for now.

| Weapon | Light/heavy damage | Stamina | Windup seconds | Recovery seconds | Reach metres |
| --- | --- | --- | --- | --- | --- |
| Sword | 18 / 32 | 16 / 30 | 0.24 / 0.65 | 0.48 / 0.85 | 2.3 / 2.65 |
| Spear | 20 / 36 | 21 / 36 | 0.38 / 0.85 | 0.65 / 1.05 | 3.4 / 3.8 |
| Axe | 24 / 44 | 24 / 42 | 0.50 / 1.00 | 0.85 / 1.30 | 2.1 / 2.3 |

Sword and spear stats, dodge timing and stamina regeneration are unchanged.
Axe heavy hits stagger for 0.95 seconds (normally 0.65), but still respect blocks,
dodges and Hulk poise. Armour still requires two heavy hits during the same windup.

In Limb damage, hits follow the actual blade or axe head, not the reach cone above.
The axe handle cannot sever limbs. Axe integrity damage is 30/55 versus sword 18/35.
One heavy axe cut severs an arm; two sever the same leg. Approach to about 1.8m for
practice. At very close range, the axe head can pass beyond the target; spacing matters.

## After-work test list

1. Run both Unity test suites, including BlightWeaponTests and BlightWeaponPlayTests.
2. Weapons: compare all three at the starting position using B. Confirm models change.
3. Fight with each: sword should feel quick, spear should reward distance and alignment,
   axe should hit hard but leave a longer opening. Check X does not cancel attacks/dodges.
4. Limb damage: enable stationary practice with T; equip axe before approaching.
   Arm aim + E should sever with one contact. Reset, equip axe, use Z for legs:
   one heavy should cause a limp, a second should force crawling.
5. Hulk: axe should respect the first armoured heavy impact, not instantly interrupt it.
6. Confirm misses do no damage, reset restores sword/intact limbs, and Patrol still
   requires its cache to unlock the spear. Check B's panel does not pass mouse clicks
   through into attacks.

This sprint adds a selectable loadout and comparison UI. Inventory, random loot,
rarity, armour equipment and persistence are future work. Companion commands are unchanged.
Prototype axe visuals reuse the current attack animation; final weapon animations remain future work.

Local validation covers patch application, contact geometry and unchanged baseline
values. Unity compilation, test execution and gameplay tuning require the editor.
