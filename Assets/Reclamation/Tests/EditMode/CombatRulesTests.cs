using NUnit.Framework;
using Reclamation.Outbreak;

namespace Reclamation.Tests
{
    public sealed class CombatRulesTests
    {
        [Test] public void VeteranHasTechniqueAndStaminaAdvantagesNotExtraHumanHealth()
        {
            var civilian = CombatAttributes.Civilian(); var veteran = CombatAttributes.Veteran();
            Assert.That(veteran.Damage, Is.GreaterThan(civilian.Damage));
            Assert.That(veteran.Windup, Is.LessThan(civilian.Windup));
            Assert.That(veteran.MaximumStamina, Is.GreaterThan(civilian.MaximumStamina));
            Assert.That(veteran.DodgeCost, Is.LessThan(civilian.DodgeCost));
        }
        [Test] public void VeteranCanBreakGrabBeforeBiteButCivilianNeedsHelp()
        {
            Assert.That(CombatAttributes.Veteran().EscapeTime, Is.LessThan(1.2f));
            Assert.That(CombatAttributes.Civilian().EscapeTime, Is.GreaterThan(1.2f));
        }
        [Test] public void PresetStrikeCountsDifferentiateOrdinaryZombieFights()
        {
            Assert.That(CombatAttributes.Veteran().Damage * 2, Is.GreaterThanOrEqualTo(80));
            Assert.That(CombatAttributes.Civilian().Damage * 3, Is.LessThan(80));
            Assert.That(CombatAttributes.Civilian().Damage * 4, Is.GreaterThanOrEqualTo(80));
        }
    }
}
