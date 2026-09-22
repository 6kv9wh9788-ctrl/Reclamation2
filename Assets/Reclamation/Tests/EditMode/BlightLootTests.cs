using NUnit.Framework;
using Reclamation.Blight;

namespace Reclamation.Tests
{
    public sealed class BlightLootTests
    {
        [Test] public void InventoryRejectsDuplicatesAndUnknownItems()
        {
            var inventory = new BlightInventory();
            Assert.That(inventory.Collect(BlightLoot.DuelistSword), Is.True);
            Assert.That(inventory.Collect(BlightLoot.DuelistSword), Is.False);
            Assert.That(inventory.Collect((BlightLoot)99), Is.False);
            Assert.That(inventory.Count, Is.EqualTo(1));
        }

        [Test] public void VariantsHaveAdvantagesAndCostsWithoutChangingBaseWeapons()
        {
            foreach (bool heavy in new[] { false, true })
            {
                AttackSpec sword = BlightEquipment.Weapon(BlightWeapon.Sword, heavy);
                AttackSpec duelist = BlightLootCatalog.Attack(BlightLoot.DuelistSword, heavy);
                Assert.That(duelist.Damage, Is.LessThan(sword.Damage));
                Assert.That(duelist.Cost, Is.LessThan(sword.Cost));
                Assert.That(duelist.Windup + duelist.Recovery, Is.LessThan(sword.Windup + sword.Recovery));
                AttackSpec spear = BlightEquipment.Weapon(BlightWeapon.Spear, heavy);
                AttackSpec warden = BlightLootCatalog.Attack(BlightLoot.WardensSpear, heavy);
                Assert.That(warden.Damage, Is.GreaterThan(spear.Damage));
                Assert.That(warden.Cost, Is.GreaterThan(spear.Cost));
                Assert.That(warden.Windup + warden.Recovery, Is.GreaterThan(spear.Windup + spear.Recovery));
                AttackSpec axe = BlightEquipment.Weapon(BlightWeapon.Axe, heavy);
                AttackSpec executioner = BlightLootCatalog.Attack(BlightLoot.ExecutionersAxe, heavy);
                Assert.That(executioner.Recovery, Is.GreaterThan(axe.Recovery));
                Assert.That(executioner.Reach, Is.EqualTo(axe.Reach));
                if (heavy) Assert.That(executioner.Stagger, Is.GreaterThan(axe.Stagger));
            }
        }
    }
}
