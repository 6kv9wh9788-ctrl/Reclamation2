using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class BlightGatewayPlayTests
    {
        private GameObject root; private BlightCombatLab lab;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Gateway tests"); lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = false; lab.AutomaticSimulation = false; lab.CombatAudioEnabled = false;
            yield return null; lab.SelectScenario(BlightScenario.Gateway);
        }
        [UnityTearDown] public IEnumerator Cleanup() { Object.Destroy(root); yield return null; }
        private Transform Actor(string name)
        {
            foreach (Transform child in root.transform) if (child.name == name && child.gameObject.activeSelf) return child;
            Assert.Fail("Missing " + name); return null;
        }
        private void QuietEnemies()
        {
            foreach (Transform child in root.transform)
                if (child.gameObject.activeSelf && child.name.StartsWith("Formation")) lab.GetFighter(child).Interrupt(60);
        }
        private void Advance(float seconds) { for (int i = 0; i < Mathf.CeilToInt(seconds * 60); i++) lab.Simulate(1f / 60); }
        [UnityTest] public IEnumerator GatewayAndOpenGroundMatchForcesAndStartingFormation()
        {
            float health = lab.GetFighter(Actor("Formation Hulk")).Health;
            Vector3 mara = Actor("Mara - swordswoman").position;
            Assert.That(lab.LivingEnemies, Is.EqualTo(12)); Assert.That(lab.ActiveEnemyAttackLimit, Is.EqualTo(4));
            Assert.That(lab.TerrainSight(new Vector3(5, 0, -2), new Vector3(5, 0, 2)), Is.False);
            lab.SelectScenario(BlightScenario.Horde); yield return null;
            Assert.That(lab.LivingEnemies, Is.EqualTo(12)); Assert.That(lab.ActiveEnemyAttackLimit, Is.EqualTo(4));
            Assert.That(lab.GetFighter(Actor("Formation Hulk")).Health, Is.EqualTo(health));
            Assert.That(Actor("Mara - swordswoman").position, Is.EqualTo(mara)); Assert.That(lab.Order, Is.EqualTo(SquadOrder.Hold));
            Assert.That(lab.TerrainSight(new Vector3(5, 0, -2), new Vector3(5, 0, 2)), Is.True);
        }
        [UnityTest] public IEnumerator PlayerMovementAndMeleeCannotPassThroughSolidWall()
        {
            QuietEnemies(); Transform hero = Actor("Company fighter"), enemy = Actor("Formation thrall 1");
            hero.position = new Vector3(5, 0, -1.15f); enemy.position = new Vector3(5, 0, 1.15f);
            lab.SetPlayerLocomotion(Vector3.forward, true, false); Advance(.5f);
            Assert.That(hero.position.z, Is.LessThan(-1));
            lab.SetPlayerLocomotion(Vector3.zero, false, false);
            hero.rotation = Quaternion.identity; Assert.That(lab.RequestPlayerAttack(true), Is.True); Advance(1);
            Assert.That(lab.GetFighter(enemy).Health, Is.EqualTo(100));
            yield return null;
        }
        [UnityTest] public IEnumerator HoldDoesNotChaseAndFallbackRegroupsAtSecondLine()
        {
            QuietEnemies(); Transform mara = Actor("Mara - swordswoman"), bren = Actor("Bren - spearman");
            Vector3 start = mara.position; Actor("Company fighter").position = new Vector3(13.5f, 0, 5);
            Advance(2); Assert.That(Vector3.Distance(start, mara.position), Is.LessThan(.4f));
            lab.GiveOrder(SquadOrder.Withdraw); Advance(5);
            Assert.That(lab.FallingBack, Is.False); Assert.That(lab.Order, Is.EqualTo(SquadOrder.Hold));
            Assert.That(Vector3.Distance(mara.position, lab.FallbackPosition), Is.LessThan(1.2f));
            Assert.That(Vector3.Distance(bren.position, lab.FallbackPosition), Is.LessThan(1.2f));
            yield return null;
        }
        [UnityTest] public IEnumerator PauseResetAndScenarioSwitchClearTacticalState()
        {
            QuietEnemies(); lab.GiveOrder(SquadOrder.Withdraw); lab.SetPaused(true);
            Vector3 before = Actor("Mara - swordswoman").position; Advance(1);
            Assert.That(Actor("Mara - swordswoman").position, Is.EqualTo(before));
            lab.ResetFight(); Assert.That(lab.FallingBack, Is.False); Assert.That(lab.Order, Is.EqualTo(SquadOrder.Hold));
            lab.SelectScenario(BlightScenario.Duel); yield return null;
            Assert.That(root.transform.Find("Gateway tactics"), Is.Null); Assert.That(lab.TacticalScenario, Is.False);
            Assert.That(lab.TerrainPassable(new Vector3(5, 0, -2), new Vector3(5, 0, 2), .8f), Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
