using System.Collections;
using NUnit.Framework;
using Reclamation.Neighborhood;
using Reclamation.Outbreak;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class OutbreakAgentTests
    {
        private GameObject go;
        private OutbreakAgent agent;

        [SetUp] public void Setup()
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.AddComponent<CivilianRoutine>();
            agent = go.AddComponent<OutbreakAgent>();
            agent.Configure("Test civilian");
        }

        [TearDown] public void Cleanup()
        {
            if (go != null) Object.DestroyImmediate(go);
        }

        [UnityTest] public IEnumerator LatentInfectionIsNotPubliclyRevealed()
        {
            yield return null;
            Assert.That(agent.Expose(0, 10, 5), Is.True);
            Assert.That(agent.State, Is.EqualTo(InfectionState.Exposed));
            Assert.That(agent.PublicStatus, Is.EqualTo("appears healthy"));
            Assert.That(agent.Infected, Is.True);
            Assert.That(agent.Contagious, Is.False);
        }

        [UnityTest] public IEnumerator IsolationBlocksSusceptibilityAndContagion()
        {
            yield return null;
            agent.SetIsolated(true);
            Assert.That(agent.Isolated, Is.True);
            Assert.That(agent.Infectable, Is.False);
            Assert.That(agent.PublicStatus, Is.EqualTo("isolated"));
            agent.SetIsolated(false);
            Assert.That(agent.Infectable, Is.True);
            agent.Expose(0, 1, 1);
            agent.Simulate(1);
            Assert.That(agent.Contagious, Is.True);
            agent.SetIsolated(true);
            Assert.That(agent.Contagious, Is.False);
        }

        [UnityTest] public IEnumerator TurnedAgentCanBeNeutralizedExactlyOnce()
        {
            yield return null;
            agent.Expose(0, 1, 1);
            agent.Simulate(2);
            Assert.That(agent.State, Is.EqualTo(InfectionState.Turned));
            Assert.That(agent.Neutralize(), Is.True);
            Assert.That(agent.State, Is.EqualTo(InfectionState.Neutralized));
            Assert.That(agent.gameObject.activeSelf, Is.False);
            Assert.That(agent.Neutralize(), Is.False);
        }

        [UnityTest] public IEnumerator PauseCommandIsSafeWithoutABakedNavMesh()
        {
            yield return null;
            Assert.DoesNotThrow(() => agent.SetSimulationPaused(true));
            Assert.DoesNotThrow(() => agent.SetSimulationPaused(false));
        }
    }
}
