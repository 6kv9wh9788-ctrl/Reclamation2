using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class BlightHeroPlayTests
    {
        private GameObject root;
        private BlightCombatLab lab;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Hero combat tests");
            lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = false; lab.AutomaticSimulation = false; lab.CombatAudioEnabled = false;
            yield return null; lab.SetPaused(false);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { Object.Destroy(root); yield return null; }
        private Transform Actor(string name)
        {
            foreach (Transform child in root.transform)
                if (child.gameObject.activeSelf && child.name == name) return child;
            return null;
        }
        private BlightHeroVisual Hero => Actor("Company fighter").GetComponentInChildren<BlightHeroVisual>();

        [UnityTest] public IEnumerator ScenariosUseExpectedHumansAndPreserveLimbContactRig()
        {
            for (int i = 0; i < 6; i++)
            {
                lab.SelectScenario((BlightScenario)i); yield return null;
                int expected = i == 5 ? 0 : i == 1 || i == 2 || i == 4 ? 3 : 1;
                Assert.That(root.GetComponentsInChildren<BlightHeroVisual>().Length, Is.EqualTo(expected));
                if (i == 5)
                {
                    Assert.That(lab.GetLimbs(Actor("Blighted Thrall")), Is.Not.Null);
                    Assert.That(root.GetComponentsInChildren<RefinedLimbVisual>().Length, Is.EqualTo(2));
                }
                else
                {
                    Assert.That(Hero.Rig.PhysicsBodyCount, Is.Zero);
                    Assert.That(Hero.GetComponentsInChildren<Collider>(true).Length, Is.Zero);
                    Assert.That(Actor("Company fighter").Find("Torso").gameObject.activeSelf, Is.False);
                }
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator HeroAttackPreservesDamageAndPauseFreezesPose()
        {
            lab.SelectScenario(BlightScenario.Weapons);
            Transform enemy = Actor("Blighted Thrall");
            Actor("Company fighter").position = Vector3.zero; enemy.position = Vector3.forward * 1.7f;
            AttackSpec waiting = BlightEquipment.Weapon(BlightWeapon.Sword, true); waiting.Windup = 100;
            lab.GetFighter(enemy).Attack(waiting);
            Assert.That(lab.RequestPlayerAttack(false), Is.True);
            lab.Simulate(.1f);
            Assert.That(Hero.Rig.CurrentClip, Is.EqualTo("LightAttack"));
            Transform socket = Hero.Rig.Bone("WeaponSocket_R");
            Vector3 position = socket.position; Quaternion rotation = socket.rotation;
            float remaining = lab.PlayerFighter.Remaining;
            lab.SetPaused(true); lab.Simulate(.2f); yield return null; yield return null;
            Assert.That(Vector3.Distance(socket.position, position), Is.LessThan(.00001f));
            Assert.That(Quaternion.Angle(socket.rotation, rotation), Is.LessThan(.001f));
            Assert.That(lab.PlayerFighter.Remaining, Is.EqualTo(remaining));
            lab.SetPaused(false);
            for (int i = 0; i < 20; i++) lab.Simulate(.02f);
            Assert.That(lab.GetFighter(enemy).Health, Is.EqualTo(82).Within(.01f));
            Assert.That(lab.FeedbackEventCount, Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator EquipmentUsesCorrectMeshAndHandsStayAttached()
        {
            lab.SelectScenario(BlightScenario.Weapons);
            foreach (BlightWeapon weapon in new[] {BlightWeapon.Sword, BlightWeapon.Axe, BlightWeapon.Spear})
            {
                Assert.That(lab.TryEquip(weapon), Is.True);
                Assert.That(Hero.Weapon, Is.EqualTo(weapon));
                Transform spear = Hero.Rig.Bone("WeaponSocket_R").Find("Hero spear");
                Assert.That(spear.gameObject.activeSelf, Is.EqualTo(weapon == BlightWeapon.Spear));
                foreach (bool heavy in new[] {false, true})
                {
                    var fighter = new DuelFighter(); fighter.Attack(BlightEquipment.Weapon(weapon, heavy));
                    for (int step = 0; step < 180; step++)
                    {
                        Hero.Pose(fighter, false, step / 60f);
                        Assert.That(Hero.Rig.GripError, Is.LessThan(.005f), weapon + " " + heavy + " " + step);
                        fighter.Advance(1f / 60f);
                    }
                }
            }
            lab.ResetFight(); yield return null;
            Assert.That(Hero.Weapon, Is.EqualTo(BlightWeapon.Sword));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator DefenseInjuryAndDeathUseTheirOwnPoses()
        {
            var fighter = new DuelFighter { Blocking = true };
            Hero.Pose(fighter, false, 0);
            Assert.That(Hero.Rig.CurrentClip, Is.EqualTo("Block"));
            Assert.That(Hero.Rig.GripError, Is.LessThan(.005f));
            fighter.Blocking = false; fighter.Dodge(); Hero.Pose(fighter, false, 0);
            Assert.That(Hero.Rig.CurrentClip, Is.EqualTo("Dodge"));
            fighter = new DuelFighter(); fighter.Receive(10, false, true); Hero.Pose(fighter, false, 0);
            Assert.That(Hero.Rig.CurrentClip, Is.EqualTo("Stagger"));
            Assert.That(Hero.Rig.Expression, Is.EqualTo("Hurt"));
            fighter.Receive(1000, false, false); Hero.Pose(fighter, false, 0);
            Assert.That(Hero.Rig.CurrentClip, Is.EqualTo("Death"));
            Assert.That(Actor("Company fighter").localScale, Is.EqualTo(Vector3.one));
            yield return null;
        }
    }
}
