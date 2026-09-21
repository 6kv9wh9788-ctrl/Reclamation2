using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;

namespace Reclamation.Tests
{
    public sealed class BlightPatrolTests
    {
        [Test] public void CacheRequiresClearanceProximityAndALivingPlayer()
        {
            var p = new BlightPatrol();
            Assert.That(p.TryRecoverCache(0, true), Is.False);
            p.ObserveEnemies(1);
            Assert.That(p.Stage, Is.EqualTo(PatrolStage.Outbound));
            p.ObserveEnemies(0);
            Assert.That(p.TryRecoverCache(4, true), Is.False);
            Assert.That(p.TryRecoverCache(0, false), Is.False);
            Assert.That(p.TryRecoverCache(float.NaN, true), Is.False);
            Assert.That(p.TryRecoverCache(2, true), Is.True);
            Assert.That(p.SpearRecovered, Is.True);
            Assert.That(p.TryRecoverCache(0, true), Is.False, "Reward can be collected only once.");
        }

        [Test] public void CompletionRequiresRewardAndSafeReturn()
        {
            var p = new BlightPatrol();
            Assert.That(p.TryComplete(0, false, true), Is.False);
            p.ObserveEnemies(0); p.TryRecoverCache(0, true);
            Assert.That(p.TryComplete(12, false, true), Is.False);
            Assert.That(p.TryComplete(0, true, true), Is.False);
            Assert.That(p.TryComplete(0, false, false), Is.False);
            Assert.That(p.TryComplete(3, false, true), Is.True);
            Assert.That(p.Stage, Is.EqualTo(PatrolStage.Complete));
            Assert.That(new BlightPatrol().SpearRecovered, Is.False, "A fresh patrol cannot inherit loot.");
        }

        [Test] public void RecoveryAndOrdersRespectTheirBoundaries()
        {
            Assert.That(BlightPatrol.CanRecover(3, false), Is.True);
            Assert.That(BlightPatrol.CanRecover(3, true), Is.False);
            Assert.That(BlightPatrol.CanRecover(8, false), Is.False);
            Assert.That(BlightSquadRules.CanEngage(SquadOrder.Withdraw, 1), Is.False);
            Assert.That(BlightSquadRules.CanEngage(SquadOrder.Hold, 4), Is.False);
            Assert.That(BlightSquadRules.CanEngage(SquadOrder.Hold, 2), Is.True);
            Assert.That(BlightSquadRules.CanEngage(SquadOrder.Assault, 15), Is.False);
            Assert.That(BlightSquadRules.Formation(Vector3.zero, Vector3.forward, 0).x, Is.LessThan(0));
            Assert.That(BlightSquadRules.Formation(Vector3.zero, Vector3.forward, 1).x, Is.GreaterThan(0));
        }
    }
}
