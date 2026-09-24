using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class CompanionArtPlayTests
    {
        private GameObject root;
        private BlightCombatLab lab;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Companion art tests"); lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = false; lab.AutomaticSimulation = false; lab.CombatAudioEnabled = false;
            yield return null; lab.SelectScenario(BlightScenario.Squad);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { Object.Destroy(root); yield return null; }
        private Transform Actor(string name)
        {
            foreach (Transform child in root.transform)
                if (child.gameObject.activeSelf && child.name == name) return child;
            return null;
        }
        private BlightHeroVisual Visual(string name) => Actor(name).GetComponentInChildren<BlightHeroVisual>();
        private Material Cloth(BlightHeroVisual visual) => visual.transform.Find("TorsoCloth").GetComponent<Renderer>().sharedMaterial;

        [UnityTest] public IEnumerator CompanionsHaveDistinctGearAndIsolatedMaterials()
        {
            var hero = Visual("Company fighter"); var mara = Visual("Mara - swordswoman"); var bren = Visual("Bren - spearman");
            Assert.That(mara.Look, Is.EqualTo(BlightHumanLook.Mara));
            Assert.That(bren.Look, Is.EqualTo(BlightHumanLook.Bren));
            Assert.That(mara.Rig.HeavyArmour, Is.False); Assert.That(mara.Rig.LongHair, Is.True);
            Assert.That(mara.Rig.SoftFace, Is.EqualTo(.75f));
            Assert.That(bren.Rig.HeavyArmour, Is.True); Assert.That(bren.Rig.LongHair, Is.False);
            Assert.That(mara.Weapon, Is.EqualTo(BlightWeapon.Sword)); Assert.That(bren.Weapon, Is.EqualTo(BlightWeapon.Spear));
            Assert.That(Cloth(mara), Is.Not.SameAs(Cloth(hero))); Assert.That(Cloth(mara), Is.Not.SameAs(Cloth(bren)));
            Assert.That(Vector4.Distance(Cloth(mara).color, new Color(.16f, .36f, .25f)), Is.LessThan(.0001f));
            Assert.That(Vector4.Distance(Cloth(bren).color, new Color(.39f, .30f, .15f)), Is.LessThan(.0001f));
            Color heroColor = Cloth(hero).color, brenColor = Cloth(bren).color;
            mara.Equip(BlightWeapon.Sword);
            Assert.That(Cloth(hero).color, Is.EqualTo(heroColor)); Assert.That(Cloth(bren).color, Is.EqualTo(brenColor));
            foreach (var visual in new[] {mara, bren})
            {
                Assert.That(visual.Rig.PhysicsBodyCount, Is.Zero);
                Assert.That(visual.GetComponentsInChildren<Collider>(true).Length, Is.Zero);
                Assert.That(visual.transform.parent.Find("Torso").gameObject.activeSelf, Is.False);
                Assert.That(visual.Rig.Bone("WeaponSocket_R").Find("Hero spear").gameObject.activeSelf,
                    Is.EqualTo(visual == bren));
            }
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator LiveCompanionAttackUsesItsOwnWeaponPoseAndPauses()
        {
            foreach (string name in new[] {"Mara - swordswoman", "Bren - spearman"})
            {
                var visual = Visual(name); DuelFighter fighter = lab.GetFighter(Actor(name));
                Assert.That(fighter.Attack(BlightEquipment.Weapon(visual.Weapon, false)), Is.True);
            }
            lab.Simulate(.1f);
            var mara = Visual("Mara - swordswoman"); var bren = Visual("Bren - spearman");
            Assert.That(mara.Rig.CurrentClip, Is.EqualTo("LightAttack")); Assert.That(bren.Rig.CurrentClip, Is.EqualTo("LightAttack"));
            Assert.That(mara.Rig.SpearGrip, Is.False); Assert.That(bren.Rig.SpearGrip, Is.True);
            Assert.That(mara.Rig.GripError, Is.LessThan(.005f)); Assert.That(bren.Rig.GripError, Is.LessThan(.005f));
            Vector3 hand = bren.Rig.Bone("Hand_R").position; Quaternion rotation = bren.Rig.Bone("Hand_R").rotation;
            lab.SetPaused(true); lab.Simulate(.2f); yield return null; yield return null;
            Assert.That(bren.Rig.Bone("Hand_R").position, Is.EqualTo(hand));
            Assert.That(Quaternion.Angle(bren.Rig.Bone("Hand_R").rotation, rotation), Is.LessThan(.001f));
        }

        [UnityTest] public IEnumerator AttackGuardAndInjuryPosesKeepGearAttachedAndStatsUnchanged()
        {
            foreach (string name in new[] {"Mara - swordswoman", "Bren - spearman"})
            {
                var visual = Visual(name);
                var fighter = new DuelFighter(); fighter.Attack(BlightEquipment.Weapon(visual.Weapon, true));
                for (int i = 0; i < 150; i++)
                {
                    float health = fighter.Health, stamina = fighter.Stamina, remaining = fighter.Remaining;
                    visual.Pose(fighter, false, i / 60f);
                    Assert.That(visual.Rig.GripError, Is.LessThan(.005f), name + " " + i);
                    Assert.That(fighter.Health, Is.EqualTo(health)); Assert.That(fighter.Stamina, Is.EqualTo(stamina));
                    Assert.That(fighter.Remaining, Is.EqualTo(remaining)); fighter.Advance(1f / 60f);
                }
                fighter = new DuelFighter { Blocking = true }; visual.Pose(fighter, false, 0);
                Assert.That(visual.Rig.CurrentClip, Is.EqualTo("Block")); Assert.That(visual.Rig.GripError, Is.LessThan(.005f));
                fighter.Blocking = false; fighter.Dodge(); visual.Pose(fighter, false, 0);
                Assert.That(visual.Rig.CurrentClip, Is.EqualTo("Dodge"));
                fighter = new DuelFighter(); fighter.Receive(20, false, true); visual.Pose(fighter, false, 0);
                Assert.That(visual.Rig.CurrentClip, Is.EqualTo("Stagger")); Assert.That(visual.Rig.Expression, Is.EqualTo("Hurt"));
                fighter.Receive(1000, false, false); visual.Pose(fighter, false, 0);
                Assert.That(visual.Rig.CurrentClip, Is.EqualTo("Death"));
                Assert.That(Actor(name).localScale, Is.EqualTo(Vector3.one));
            }
            lab.ResetFight(); yield return null;
            Assert.That(Visual("Mara - swordswoman").Look, Is.EqualTo(BlightHumanLook.Mara));
            Assert.That(Visual("Bren - spearman").Weapon, Is.EqualTo(BlightWeapon.Spear));
            Assert.That(root.GetComponentsInChildren<BlightHeroVisual>().Length, Is.EqualTo(3));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
