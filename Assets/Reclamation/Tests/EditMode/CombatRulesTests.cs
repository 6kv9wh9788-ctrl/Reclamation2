using NUnit.Framework;
using Reclamation.Outbreak;
using UnityEngine;

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

        [Test] public void SweepCoversFrontAndSidesButHasARearOpening()
        {
            Assert.That(CombatDirector.InSweepArc(Vector3.zero, Vector3.forward, Vector3.forward * 2), Is.True);
            Assert.That(CombatDirector.InSweepArc(Vector3.zero, Vector3.forward, Vector3.right * 2), Is.True);
            Assert.That(CombatDirector.InSweepArc(Vector3.zero, Vector3.forward, Vector3.back * 2), Is.False);
            Assert.That(CombatDirector.InSweepArc(Vector3.zero, Vector3.forward, Vector3.forward * 3), Is.False);
        }

        [Test] public void SweepArcUsesItsLockedFacingAndIgnoresActorHeight()
        {
            Assert.That(CombatDirector.InSweepArc(Vector3.zero, Vector3.right, new Vector3(2, 1, 0)), Is.True);
            Assert.That(CombatDirector.InSweepArc(Vector3.zero, Vector3.right, Vector3.left * 2), Is.False);
        }

        [Test] public void CombatHelpersDoNotCollideWithUnityStartMessage()
        {
            Assert.That(typeof(CombatDirector).GetMethod("Start",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic), Is.Null);
        }

        [Test] public void ProgressionRanksAndCapsAreStableAtBoundaries()
        {
            Assert.That(ProgressionRules.Rank(99), Is.EqualTo(SurvivorRank.Civilian));
            Assert.That(ProgressionRules.Rank(100), Is.EqualTo(SurvivorRank.Trained));
            Assert.That(ProgressionRules.Rank(300), Is.EqualTo(SurvivorRank.Veteran));
            CombatAttributes veteran = ProgressionRules.Attributes(300);
            Assert.That(veteran.strength, Is.EqualTo(12));
            Assert.That(veteran.dexterity, Is.EqualTo(9));
            CombatAttributes extreme = ProgressionRules.Attributes(int.MaxValue);
            Assert.That(extreme.strength, Is.LessThanOrEqualTo(12));
            Assert.That(extreme.agility, Is.LessThanOrEqualTo(9));
        }

        [Test] public void ProgressionImprovesTechniqueWithoutAddingHumanHealth()
        {
            CombatAttributes civilian = ProgressionRules.Attributes(0);
            CombatAttributes trained = ProgressionRules.Attributes(100);
            CombatAttributes veteran = ProgressionRules.Attributes(300);
            Assert.That(trained.Windup, Is.LessThan(civilian.Windup));
            Assert.That(veteran.Damage, Is.GreaterThan(trained.Damage));
            Assert.That(veteran.MaximumStamina, Is.GreaterThan(civilian.MaximumStamina));
        }
    }
}
