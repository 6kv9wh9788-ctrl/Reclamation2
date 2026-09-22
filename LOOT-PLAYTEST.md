# Recoverable gear sprint

Requires the weapon-equipment update. Open the existing Blight Combat Lab and choose
**Weapons**. This scenario now tests the complete fight -> collect -> compare -> equip
-> fight loop. Other scenarios keep their existing progression and combat rules.

## Controls and flow

1. Defeat the thrall. A gold marker appears at its position. You can still move after victory.
2. Approach within 2.6 metres and press **F**. Gear enters your collection; it does not auto-equip.
3. **B** opens equipment. Choose **Recovered gear** to compare each item's light/heavy
   damage, stamina cost and total attack time against your currently equipped weapon.
   Standard weapons remain available on their own tab. Equip requires five metres of space.
4. Press **N** after collecting the drop. A new thrall spawns, your gear is retained,
   and health/stamina are restored. The next drop is the next variant in the list below.
5. **R** or changing scenarios starts fresh and clears recovered gear. This is a session
   prototype; it does not save inventory when you leave the scene or restart Unity.

| Drop order | Weapon | Advantage | Cost |
| --- | --- | --- | --- |
| 1 | Duelist's Sword | 20% shorter windup/recovery; about 15% lower stamina cost | About 15% lower damage |
| 2 | Warden's Spear | About 20% higher damage | About 20% higher stamina cost; 15% longer windup/recovery |
| 3 | Executioner's Axe | Heavy-hit stagger 1.2s instead of 0.95s | 25% longer recovery |

Damage and stamina modifiers round to whole numbers. Drops repeat after the third
encounter; duplicate pickups clear the marker without adding another copy. Weapons
retain their base models, reach, block/dodge rules and Hulk armour interaction.
This pass does not add random loot, sell/salvage, armour, or limb-mode variants.

## Test checklist

- Run EditMode and PlayMode tests, including BlightLootTests and BlightLootPlayTests.
- Complete three encounters using N between them. Confirm all three variants are collected.
- Compare and equip gear. Confirm attacks feel like the displayed tradeoffs; X returns
  to cycling standard weapons, while the Recovered gear tab retains your items.
- Confirm a miss creates no loot; a defeated enemy creates only one drop.
- Try F from too far away, while paused, and after collecting; no extra item should appear.
- Try N before winning or before collecting; it should not start another encounter.
- Try equipping during an attack or near a living enemy; the swap should fail.
- Check both equipment tabs for clipping/click-through. The scenario selector and
  equipment panel close each other to avoid overlapping menus.
- Verify R clears inventory/drop markers and restores the standard sword. Patrol's
  spear cache remains unchanged.

Local checks cover patch application, unchanged baseline combat rules and package
contents. Unity compilation, test execution and visual/play validation remain editor tasks.
