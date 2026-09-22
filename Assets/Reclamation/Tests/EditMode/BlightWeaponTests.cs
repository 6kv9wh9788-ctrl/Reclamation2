using NUnit.Framework;
using Reclamation.Blight;

namespace Reclamation.Tests
{
    public sealed class BlightWeaponTests
    {
        [Test] public void AxeTradesSpeedStaminaAndReachForDamage()
        {
            foreach (bool heavy in new[] { false, true })
            {
                AttackSpec sword = BlightEquipment.Weapon(BlightWeapon.Sword, heavy);
                AttackSpec axe = BlightEquipment.Weapon(BlightWeapon.Axe, heavy);
                AttackSpec spear = BlightEquipment.Weapon(BlightWeapon.Spear, heavy);
                Assert.That(axe.Windup, Is.GreaterThan(sword.Windup));
                Assert.That(axe.Recovery, Is.GreaterThan(sword.Recovery));
                Assert.That(axe.Cost, Is.GreaterThan(sword.Cost));
                Assert.That(axe.Damage, Is.GreaterThan(sword.Damage));
                Assert.That(axe.Reach, Is.LessThan(sword.Reach));
                Assert.That(spear.Reach, Is.GreaterThan(sword.Reach));
                Assert.That(spear.HalfAngle, Is.LessThan(sword.HalfAngle));
            }
        }

        [Test] public void AxeHeavyHasLongerStaggerButDoesNotBypassHulkPoise()
        {
            AttackSpec axe = BlightEquipment.Weapon(BlightWeapon.Axe, true);
            var target = new DuelFighter(); target.ReceiveAttack(axe, false);
            Assert.That(target.Remaining, Is.EqualTo(0.95f));
            var hulk = new DuelFighter(260, true);
            hulk.Attack(BlightEquipment.Enemy(BlightEnemy.Hulk, 0));
            Assert.That(hulk.ReceiveAttack(axe, false), Is.EqualTo("Armoured windup"));
            Assert.That(hulk.Action, Is.EqualTo(DuelAction.Windup));
            hulk.ReceiveAttack(axe, false);
            Assert.That(hulk.Action, Is.EqualTo(DuelAction.Stagger));
            Assert.That(hulk.Remaining, Is.EqualTo(0.95f));
        }

        [Test] public void AxeDoesNotStaggerThroughBlockOrDodge()
        {
            AttackSpec axe = BlightEquipment.Weapon(BlightWeapon.Axe, true);
            var target = new DuelFighter { Blocking = true };
            Assert.That(target.ReceiveAttack(axe, true), Is.EqualTo("Blocked"));
            Assert.That(target.Health, Is.EqualTo(100));
            target.Blocking = false; target.Dodge();
            Assert.That(target.ReceiveAttack(axe, false), Is.EqualTo("Dodged"));
            Assert.That(target.Health, Is.EqualTo(100));
        }

        [Test] public void AxeSeversArmWithOneHeavyButLegRequiresTwo()
        {
            var limbs = new BlightLimbs(); float damage = BlightEquipment.LimbDamage(BlightWeapon.Axe, true);
            Assert.That(limbs.Damage(BodyRegion.RightArm, damage, true), Is.True);
            Assert.That(limbs.Damage(BodyRegion.RightLeg, damage, true), Is.False);
            Assert.That(limbs.Limping, Is.True);
            Assert.That(limbs.Damage(BodyRegion.RightLeg, damage, true), Is.True);
        }
    }
}
