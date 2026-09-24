using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class BlightCombatFeelTests
    {
        private GameObject root;
        private BlightCombatLab lab;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Combat feel tests"); lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = false; lab.AutomaticSimulation = false; lab.CombatAudioEnabled = false;
            yield return null; lab.SelectScenario(BlightScenario.Outpost);
        }
        [UnityTearDown] public IEnumerator Cleanup() { Object.Destroy(root); yield return null; }
        private Transform Actor(string name)
        {
            foreach (Transform child in root.transform)
                if (child.name == name && child.gameObject.activeSelf) return child;
            Assert.Fail("Missing actor " + name); return null;
        }
        private Transform Hero => Actor("Company fighter");
        private void Advance(float seconds)
        { for (int i = 0; i < Mathf.CeilToInt(seconds * 60); i++) lab.Simulate(1f / 60); }

        [UnityTest] public IEnumerator SprintMovesFasterUsesStaminaAndPauseResetClearIt()
        {
            Hero.position = new Vector3(10, 0, -12);
            Vector3 start = Hero.position;
            lab.SetPlayerLocomotion(Vector3.forward, false, false); Advance(.5f);
            float walk = Vector3.Distance(start, Hero.position);
            lab.ResetFight(); Hero.position = start;
            lab.SetPlayerLocomotion(Vector3.forward, true, false); Advance(.5f);
            Assert.That(Vector3.Distance(start, Hero.position), Is.GreaterThan(walk * 1.5f));
            Assert.That(lab.PlayerFighter.Stamina, Is.EqualTo(91).Within(.1f)); Assert.That(lab.IsSprinting, Is.True);
            lab.SetPaused(true); float stamina = lab.PlayerFighter.Stamina; Vector3 stopped = Hero.position;
            Advance(.5f); Assert.That(Hero.position, Is.EqualTo(stopped)); Assert.That(lab.PlayerFighter.Stamina, Is.EqualTo(stamina));
            Assert.That(lab.IsSprinting, Is.False);
            lab.ResetFight(); Assert.That(lab.PlayerFighter.Stamina, Is.EqualTo(100)); Assert.That(lab.IsSprinting, Is.False);
            yield return null;
        }

        [UnityTest] public IEnumerator GuardAndAttackPreventSprinting()
        {
            Hero.position = new Vector3(10, 0, -12);
            lab.SetPlayerLocomotion(Vector3.forward, true, true); Advance(.1f);
            Assert.That(lab.IsSprinting, Is.False); Assert.That(lab.PlayerFighter.Stamina, Is.EqualTo(100));
            lab.SetPlayerLocomotion(Vector3.forward, true, false); Assert.That(lab.RequestPlayerAttack(true), Is.True);
            Vector3 committed = Hero.position; Advance(.1f);
            Assert.That(Hero.position, Is.EqualTo(committed)); Assert.That(lab.IsSprinting, Is.False);
            yield return null;
        }

        [UnityTest] public IEnumerator LockFollowsTargetManualOrbitOverridesAndPauseFreezesCamera()
        {
            lab.SelectScenario(BlightScenario.Duel);
            Actor("Blighted Thrall").position = Hero.position + Vector3.right * 8;
            lab.GetFighter(Actor("Blighted Thrall")).Interrupt(10);
            Advance(.5f); Assert.That(lab.CameraYaw, Is.GreaterThan(75));
            lab.SetCameraOrbit(new Vector2(-400, 0), true); float manual = lab.CameraYaw;
            Advance(.2f); Assert.That(lab.CameraYaw, Is.EqualTo(manual).Within(.001f));
            lab.SetCameraOrbit(Vector2.zero, false); Advance(.4f);
            Assert.That(lab.CameraYaw, Is.GreaterThan(manual + 30));
            lab.SetPaused(true); float paused = lab.CameraYaw;
            lab.SetCameraOrbit(new Vector2(100, 100), true); Advance(.5f);
            Assert.That(lab.CameraYaw, Is.EqualTo(paused));
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator RealBlockTriggersShakeAndComfortToggleRemovesMotion()
        {
            lab.SelectScenario(BlightScenario.Duel);
            Transform enemy = Actor("Blighted Thrall"); Hero.position = Vector3.zero;
            enemy.position = Vector3.forward * 2; enemy.rotation = Quaternion.LookRotation(Vector3.back);
            lab.SetPlayerLocomotion(Vector3.zero, false, true);
            var strike = BlightEquipment.Enemy(BlightEnemy.Thrall, 0);
            DuelFighter f = lab.GetFighter(enemy); Assert.That(f.Attack(strike), Is.True);
            f.Advance(strike.Windup - .01f); lab.Simulate(.02f);
            Assert.That(lab.LastCombatImpact, Is.EqualTo(CombatImpact.Block));
            Assert.That(lab.PlayerFighter.Health, Is.EqualTo(100)); Assert.That(lab.CameraImpactStrength, Is.GreaterThan(0));
            lab.CameraMotionEnabled = false; yield return null;
            Assert.That(lab.CameraImpactStrength, Is.Zero); Assert.That(lab.CameraMotionOffset, Is.EqualTo(Vector3.zero));
            lab.ResetFight(); Assert.That(lab.CameraImpactStrength, Is.Zero);
        }

        [UnityTest] public IEnumerator BobIsSmallAndSettlesWhenMovementStops()
        {
            Hero.position = new Vector3(10, 0, -12);
            lab.SetPlayerLocomotion(Vector3.forward, false, false); Advance(.4f); yield return null;
            Assert.That(lab.CameraMotionOffset.magnitude, Is.GreaterThan(.0001f));
            Assert.That(lab.CameraMotionOffset.magnitude, Is.LessThan(.025f));
            lab.SetPlayerLocomotion(Vector3.zero, false, false); Advance(.3f); yield return null;
            Assert.That(lab.CameraMotionOffset.magnitude, Is.LessThan(.00001f));
        }

        [UnityTest] public IEnumerator ThrallEvadesSomeHeavyTellsAfterDelayAndSpendsStamina()
        {
            lab.SelectScenario(BlightScenario.Duel);
            Transform enemy = Actor("Blighted Thrall"); DuelFighter foe = lab.GetFighter(enemy);
            // The first attack misses at distance. The second visible heavy is this thrall's evade choice.
            Hero.position = new Vector3(0, 0, -20); enemy.position = new Vector3(0, 0, 15);
            Assert.That(lab.PlayerFighter.Attack(true), Is.True); lab.PlayerFighter.Advance(.7f); lab.PlayerFighter.Advance(1);
            Hero.position = Vector3.zero; enemy.position = Vector3.forward * 2;
            enemy.rotation = Quaternion.LookRotation(Vector3.back);
            Assert.That(lab.RequestPlayerAttack(true), Is.True);
            lab.Simulate(.1f); Assert.That(foe.Action, Is.EqualTo(DuelAction.Ready), "No instant dodge.");
            Advance(.32f); Assert.That(foe.Action, Is.EqualTo(DuelAction.Dodge));
            Assert.That(foe.Stamina, Is.EqualTo(75).Within(.01f));
            Advance(.3f); Assert.That(foe.Health, Is.EqualTo(100));
            yield return null;
        }

        [UnityTest] public IEnumerator CommittedEnemyCannotCancelIntoDefense()
        {
            lab.SelectScenario(BlightScenario.Duel);
            Transform enemy = Actor("Blighted Thrall"); Hero.position = Vector3.zero; enemy.position = Vector3.forward * 2;
            DuelFighter foe = lab.GetFighter(enemy); var attack = BlightEquipment.Enemy(BlightEnemy.Thrall, 0); attack.Windup = 3;
            Assert.That(foe.Attack(attack), Is.True); Assert.That(lab.RequestPlayerAttack(true), Is.True);
            Advance(.3f); Assert.That(foe.Action, Is.EqualTo(DuelAction.Windup));
            yield return null;
        }

        [UnityTest] public IEnumerator HulkBracesAfterTellAndGuardCostsStamina()
        {
            lab.SelectScenario(BlightScenario.Hulk);
            Transform hulk = Actor("Blightbound Hulk"); DuelFighter foe = lab.GetFighter(hulk);
            Hero.position = Vector3.zero; hulk.position = Vector3.forward * 2;
            hulk.rotation = Quaternion.LookRotation(Vector3.back);
            Actor("Mara - swordswoman").position = new Vector3(-12, 0, -15);
            Actor("Bren - spearman").position = new Vector3(12, 0, -15);
            lab.GiveOrder(SquadOrder.Hold);
            Assert.That(lab.RequestPlayerAttack(true), Is.True);
            Advance(.1f); Assert.That(foe.Blocking, Is.False);
            Advance(.25f); Assert.That(foe.Blocking, Is.True);
            Advance(.32f); Assert.That(foe.Health, Is.EqualTo(260));
            Assert.That(foe.Stamina, Is.LessThan(65));
            Assert.That(foe.Action, Is.Not.EqualTo(DuelAction.Dodge));
            yield return null;
        }

        [UnityTest] public IEnumerator HordeLimitsSimultaneousAttacksDuringLiveSimulation()
        {
            lab.SelectScenario(BlightScenario.Horde);
            int largest = 0;
            for (int tick = 0; tick < 360; tick++)
            {
                lab.Simulate(1f / 60);
                int committed = 0;
                foreach (Transform child in root.transform)
                {
                    if (!child.gameObject.activeSelf || !child.name.StartsWith("Formation")) continue;
                    DuelFighter fighter = lab.GetFighter(child);
                    if (fighter != null && fighter.Alive &&
                        (fighter.Action == DuelAction.Windup || fighter.Action == DuelAction.Recovery)) committed++;
                }
                Assert.That(committed, Is.LessThanOrEqualTo(4)); largest = Mathf.Max(largest, committed);
            }
            Assert.That(largest, Is.GreaterThan(0), "The encounter must actually engage.");
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator LargerEncountersSpawnCorrectCountsAndResetWithoutDuplicates()
        {
            foreach (BlightScenario scenario in new[] { BlightScenario.Skirmish, BlightScenario.Horde })
            {
                lab.SelectScenario(scenario); yield return null;
                int expected = scenario == BlightScenario.Horde ? 12 : 6;
                Assert.That(lab.LivingEnemies, Is.EqualTo(expected)); Assert.That(lab.LivingCompanions, Is.EqualTo(2));
                Assert.That(lab.ActiveEnemyAttackLimit, Is.EqualTo(scenario == BlightScenario.Horde ? 4 : 3));
                Assert.That(root.GetComponentsInChildren<BlightHulkVisual>().Length, Is.EqualTo(1));
                Assert.That(root.GetComponentsInChildren<BlightThrallVisual>().Length, Is.EqualTo(expected - 1));
                lab.ResetFight(); yield return null; Assert.That(lab.LivingEnemies, Is.EqualTo(expected));
            }
            lab.SelectScenario(BlightScenario.Outpost); yield return null;
            Assert.That(lab.LivingEnemies, Is.Zero); Assert.That(root.GetComponentsInChildren<BlightHulkVisual>().Length, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
