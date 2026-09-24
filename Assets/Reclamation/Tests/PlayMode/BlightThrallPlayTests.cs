using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class BlightThrallPlayTests
    {
        private GameObject root;
        private BlightCombatLab lab;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Thrall art tests"); lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = false; lab.AutomaticSimulation = false; lab.CombatAudioEnabled = false;
            yield return null; lab.SetPaused(false);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { Object.Destroy(root); yield return null; }

        [UnityTest] public IEnumerator ScenariosInstallThrallsWithoutReplacingTheHulk()
        {
            int[] expected = {1, 3, 0, 1, 2, 0};
            for (int i = 0; i < 6; i++)
            {
                lab.SelectScenario((BlightScenario)i); yield return null;
                var thralls = root.GetComponentsInChildren<BlightThrallVisual>();
                Assert.That(thralls.Length, Is.EqualTo(expected[i]));
                foreach (var thrall in thralls)
                {
                    Assert.That(thrall.Rig.PhysicsBodyCount, Is.Zero);
                    Assert.That(thrall.transform.Find("HookedClaw_Index_R"), Is.Not.Null);
                }
            }
            Assert.That(ModularHumanRig.LoadThrall(), Is.Not.SameAs(ModularHumanRig.Load(false)));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator PauseFreezesClawPoseAndResetRestoresEnemy()
        {
            var thrall = root.GetComponentInChildren<BlightThrallVisual>();
            DuelFighter fighter = lab.GetFighter(thrall.transform.parent);
            fighter.Attack(BlightEquipment.Enemy(BlightEnemy.Thrall, 0)); lab.Simulate(.1f);
            Quaternion hand = thrall.Rig.Bone("Hand_R").rotation;
            Vector3 head = thrall.Rig.Bone("Head").position;
            float remaining = fighter.Remaining;
            lab.SetPaused(true); lab.Simulate(.2f); yield return null; yield return null;
            Assert.That(Quaternion.Angle(hand, thrall.Rig.Bone("Hand_R").rotation), Is.LessThan(.001f));
            Assert.That(thrall.Rig.Bone("Head").position, Is.EqualTo(head));
            Assert.That(fighter.Remaining, Is.EqualTo(remaining));
            lab.ResetFight(); yield return null;
            Assert.That(root.GetComponentsInChildren<BlightThrallVisual>().Length, Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator ClawsAndGrowthsDetachWithTheirArmAndBiteUsesJaw()
        {
            lab.SelectScenario(BlightScenario.LimbDamage); lab.LimbPractice = true;
            Transform player = null, enemy = null;
            foreach (Transform child in root.transform)
            {
                if (!child.gameObject.activeSelf) continue;
                if (child.name == "Company fighter") player = child;
                if (child.name == "Blighted Thrall") enemy = child;
            }
            player.position = Vector3.zero; enemy.position = Vector3.forward * 1.8f; lab.Simulate(.02f);
            for (int swing = 0; swing < 2; swing++)
            {
                Assert.That(lab.RequestPlayerAttack(true), Is.True);
                for (int i = 0; i < 120; i++) lab.Simulate(1f / 60f);
            }
            var visual = enemy.GetComponentInChildren<RefinedLimbVisual>();
            Assert.That(lab.GetLimbs(enemy).Missing(BodyRegion.RightArm), Is.True);
            Assert.That(visual.transform.Find("HookedClaw_Index_R").GetComponent<Renderer>().enabled, Is.False);
            Transform debris = root.transform.Find("Severed RightArm");
            Assert.That(debris.Find("HookedClaw_Index_R"), Is.Not.Null);
            Assert.That(debris.Find("ShoulderGrowth_R0"), Is.Not.Null);
            var limbs = new BlightLimbs(); limbs.Damage(BodyRegion.RightArm, 100, true); limbs.Damage(BodyRegion.LeftArm, 100, true);
            var biting = new DuelFighter(); biting.Attack(limbs.Attack(0)); biting.Advance(.6f);
            visual.Rig.BindPose(); BlightThrallVisual.ShapePose(visual.Rig, biting, limbs);
            Assert.That(visual.Rig.Expression, Is.EqualTo("Shout"));
            Assert.That(Quaternion.Angle(visual.Rig.Bone("Jaw").localRotation, Quaternion.identity), Is.GreaterThan(10));
            lab.ResetFight(); yield return null;
            Assert.That(root.transform.Find("Severed RightArm"), Is.Null);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
