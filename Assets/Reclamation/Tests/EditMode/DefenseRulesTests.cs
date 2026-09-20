using NUnit.Framework;
using Reclamation.Outbreak;

namespace Reclamation.Tests
{
    public sealed class DefenseRulesTests
    {
        [Test] public void OrdinaryCannotDamageMasonryOrSteel()
        {
            Assert.That(DefenseRules.Damage(ZombieClass.Ordinary, DefenseMaterial.Masonry), Is.Zero);
            Assert.That(DefenseRules.Damage(ZombieClass.Ordinary, DefenseMaterial.Steel), Is.Zero);
        }
        [Test] public void BruteDamagesMasonryButNotSteel()
        {
            Assert.That(DefenseRules.Damage(ZombieClass.Brute, DefenseMaterial.Masonry), Is.GreaterThan(0));
            Assert.That(DefenseRules.Damage(ZombieClass.Brute, DefenseMaterial.Steel), Is.Zero);
        }
        [Test] public void WoodIsVulnerableAndBruteIsStronger()
        {
            Assert.That(DefenseRules.Damage(ZombieClass.Ordinary, DefenseMaterial.Wood), Is.GreaterThan(0));
            Assert.That(DefenseRules.Damage(ZombieClass.Brute, DefenseMaterial.Wood),
                Is.GreaterThan(DefenseRules.Damage(ZombieClass.Ordinary, DefenseMaterial.Wood)));
        }
        [Test] public void FutureSiegeProfileCanDamageSteel()
        {
            Assert.That(DefenseRules.Damage(ZombieClass.Siege, DefenseMaterial.Steel), Is.GreaterThan(0));
            Assert.That(DefenseRules.Damage(ZombieClass.Ordinary, DefenseMaterial.ReinforcedWood),
                Is.LessThan(DefenseRules.Damage(ZombieClass.Ordinary, DefenseMaterial.Wood)));
        }
    }
}
