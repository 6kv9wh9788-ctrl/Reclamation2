using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class BlightHulkPlayTests
    {
        private GameObject root;
        private BlightCombatLab lab;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Hulk presentation tests"); lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = false; lab.AutomaticSimulation = false; lab.CombatAudioEnabled = false;
            yield return null; lab.SelectScenario(BlightScenario.Hulk);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { Object.Destroy(root); yield return null; }
        private BlightHulkVisual Hulk => root.GetComponentInChildren<BlightHulkVisual>();

        [UnityTest] public IEnumerator HulkAndGuardianUseOneNewBodyAndResetCleanly()
        {
            Assert.That(Hulk, Is.Not.Null);
            Assert.That(Hulk.Rig.PhysicsBodyCount, Is.Zero);
            Assert.That(Hulk.transform.parent.localScale, Is.EqualTo(Vector3.one * 1.65f));
            Assert.That(Hulk.transform.parent.Find("Torso").gameObject.activeSelf, Is.False);
            Assert.That(Hulk.transform.Find("HulkShoulderPlate_R2"), Is.Not.Null);
            lab.SelectScenario(BlightScenario.Patrol); yield return null;
            Assert.That(root.GetComponentsInChildren<BlightHulkVisual>().Length, Is.EqualTo(1));
            Assert.That(Hulk.transform.parent.name, Is.EqualTo("Cache guardian"));
            Assert.That(root.GetComponentsInChildren<BlightThrallVisual>().Length, Is.EqualTo(2));
            lab.ResetFight(); yield return null;
            Assert.That(root.GetComponentsInChildren<BlightHulkVisual>().Length, Is.EqualTo(1));
            lab.SelectScenario(BlightScenario.Duel); yield return null;
            Assert.That(root.GetComponentsInChildren<BlightHulkVisual>().Length, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator SmashRaisesBothFistsAndMeetsImpactWithoutAPoseJump()
        {
            var fighter = new DuelFighter(260, true); var spec = BlightEquipment.Enemy(BlightEnemy.Hulk, 1);
            fighter.Attack(spec); fighter.Advance(spec.Windup * .8f); Hulk.Pose(fighter, false, 0);
            foreach (string side in new[] {"R", "L"})
                Assert.That(Hulk.transform.InverseTransformPoint(Hulk.Rig.Bone("Hand_" + side).position).y, Is.GreaterThan(1.9f));
            Vector3 foot = Hulk.Rig.Bone("Foot_R").position;
            fighter.Advance(fighter.Remaining - .001f); Hulk.Pose(fighter, false, 0);
            Vector3 before = Hulk.Rig.Bone("Hand_R").position;
            fighter.Advance(.002f); Hulk.Pose(fighter, false, 0);
            Assert.That(fighter.Action, Is.EqualTo(DuelAction.Recovery));
            Assert.That(Vector3.Distance(before, Hulk.Rig.Bone("Hand_R").position), Is.LessThan(.02f));
            Assert.That(Vector3.Distance(foot, Hulk.Rig.Bone("Foot_R").position), Is.LessThan(.01f));
            Assert.That(Hulk.transform.InverseTransformPoint(Hulk.Rig.Bone("Hand_R").position).y, Is.LessThan(.4f));
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator AttackTargetsStayReachableAndPresentationDoesNotAdvanceCombat()
        {
            foreach (int sequence in new[] {0, 1})
            {
                var fighter = new DuelFighter(260, true); fighter.Attack(BlightEquipment.Enemy(BlightEnemy.Hulk, sequence));
                Vector3 rootPosition = Hulk.transform.parent.position;
                for (int i = 0; i < 210; i++)
                {
                    float remaining = fighter.Remaining, health = fighter.Health, stamina = fighter.Stamina;
                    Hulk.Pose(fighter, false, 0);
                    Assert.That(Hulk.HandTargetError, Is.LessThan(.005f), sequence + " phase " + i);
                    Assert.That(fighter.Remaining, Is.EqualTo(remaining));
                    Assert.That(fighter.Health, Is.EqualTo(health));
                    Assert.That(fighter.Stamina, Is.EqualTo(stamina));
                    Assert.That(Hulk.transform.parent.position, Is.EqualTo(rootPosition));
                    fighter.Advance(1f / 60f);
                }
            }
            yield return null;
        }

        [UnityTest] public IEnumerator PauseFreezesSweepAndDefeatKeepsBodyScale()
        {
            DuelFighter fighter = lab.GetFighter(Hulk.transform.parent);
            fighter.Attack(BlightEquipment.Enemy(BlightEnemy.Hulk, 0)); lab.Simulate(.2f);
            Vector3 hand = Hulk.Rig.Bone("Hand_R").position;
            lab.SetPaused(true); lab.Simulate(.2f); yield return null; yield return null;
            Assert.That(Hulk.Rig.Bone("Hand_R").position, Is.EqualTo(hand));
            fighter.Receive(1000, false, false); Hulk.Pose(fighter, false, 0);
            Assert.That(Hulk.Rig.CurrentClip, Is.EqualTo("Death"));
            Assert.That(Hulk.transform.parent.localScale, Is.EqualTo(Vector3.one * 1.65f));
            lab.ResetFight(); yield return null;
            Assert.That(lab.GetFighter(Hulk.transform.parent).Health, Is.EqualTo(260));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
