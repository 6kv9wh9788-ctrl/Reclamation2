using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class RefinedLimbPlayTests
    {
        private GameObject root;
        private BlightCombatLab lab;
        private Transform Enemy => Actor("Blighted Thrall");
        private RefinedLimbVisual EnemyVisual => Enemy.GetComponentInChildren<RefinedLimbVisual>();
        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Refined limb tests"); lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = false; lab.AutomaticSimulation = false; lab.CombatAudioEnabled = false;
            yield return null; Arrange();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { Object.Destroy(root); yield return null; }
        private Transform Actor(string name)
        {
            foreach (Transform child in root.transform)
                if (child.gameObject.activeSelf && child.name == name) return child;
            return null;
        }
        private void Arrange()
        {
            lab.SelectScenario(BlightScenario.LimbDamage); lab.LimbPractice = true;
            Actor("Company fighter").position = Vector3.zero;
            Enemy.position = Vector3.forward * 1.8f;
            lab.Simulate(.02f);
        }
        private void Advance(float seconds)
        { for (int i = 0; i < Mathf.CeilToInt(seconds * 60); i++) lab.Simulate(1f / 60f); }
        private void Heavy()
        { Assert.That(lab.RequestPlayerAttack(true), Is.True); Advance(2); }
        private int Visible(BodyRegion region)
        {
            int count = 0;
            foreach (SkinnedMeshRenderer renderer in EnemyVisual.Rig.RegionRenderers(region.ToString()))
                if (renderer.enabled && renderer.gameObject.activeInHierarchy) count++;
            return count;
        }

        [UnityTest] public IEnumerator WoundSeverAndResetPreserveLiveSkeleton()
        {
            Assert.That(Visible(BodyRegion.RightArm), Is.GreaterThan(0));
            Assert.That(EnemyVisual.Rig.PhysicsBodyCount, Is.Zero);
            Transform bone = EnemyVisual.Rig.Bone("UpperArm_R");
            Transform parent = bone.parent;
            Heavy();
            Assert.That(lab.GetLimbs(Enemy).Missing(BodyRegion.RightArm), Is.False);
            Assert.That(Enemy.Find("Articulated body/RightArm/Wound RightArm").gameObject.activeSelf, Is.True);
            Heavy();
            Assert.That(lab.GetLimbs(Enemy).Missing(BodyRegion.RightArm), Is.True);
            Assert.That(Visible(BodyRegion.RightArm), Is.Zero);
            Assert.That(Visible(BodyRegion.LeftArm), Is.GreaterThan(0));
            Assert.That(bone.parent, Is.EqualTo(parent));
            Assert.That(Enemy.Find("Articulated body/Sever cap RightArm").gameObject.activeSelf, Is.True);
            Transform debris = root.transform.Find("Severed RightArm");
            Assert.That(debris, Is.Not.Null);
            Assert.That(debris.GetComponentsInChildren<MeshFilter>(true).Length, Is.GreaterThan(0));
            Mesh snapshot = debris.GetComponentsInChildren<MeshFilter>(true)[0].sharedMesh;
            Assert.That(debris.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length, Is.Zero);
            Assert.That(debris.gameObject.activeSelf, Is.False, "Reduced gore hides detached pieces.");
            float health = lab.GetFighter(Enemy).Health;
            lab.SetPaused(true); lab.LowGore = false;
            Assert.That(debris.gameObject.activeSelf, Is.True);
            Assert.That(lab.GetFighter(Enemy).Health, Is.EqualTo(health));
            Vector3 position = debris.position; lab.Simulate(.2f); yield return null;
            Assert.That(debris.position, Is.EqualTo(position));
            lab.SetPaused(false); Advance(9); yield return null;
            Assert.That(debris == null, Is.True);
            Assert.That(snapshot == null, Is.True, "Expired debris releases its baked meshes.");
            Assert.That(bone != null && bone.parent == parent, Is.True);
            Advance(1); lab.ResetFight(); yield return null;
            Assert.That(Visible(BodyRegion.RightArm), Is.GreaterThan(0));
            Assert.That(root.transform.Find("Severed RightArm"), Is.Null);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator LowCutRemovesBootAndTrouserRegionAndLeavesCrawlerDangerous()
        {
            lab.SetLimbAim(SwingHeight.Legs); Heavy();
            Assert.That(lab.GetLimbs(Enemy).Limping, Is.True);
            Assert.That(Visible(BodyRegion.RightLeg), Is.GreaterThan(0));
            Heavy();
            Assert.That(lab.GetLimbs(Enemy).Crawling, Is.True);
            Assert.That(Visible(BodyRegion.RightLeg), Is.Zero);
            Assert.That(Visible(BodyRegion.LeftLeg), Is.GreaterThan(0));
            Assert.That(EnemyVisual.Rig.Bone("Pelvis").position.y, Is.EqualTo(.3f).Within(.01f));
            float health = lab.PlayerFighter.Health; lab.LimbPractice = false; Advance(7);
            Assert.That(lab.PlayerFighter.Health, Is.LessThan(health));
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator BothHandsFollowTheContactWeaponAtEveryAimHeight()
        {
            foreach (BlightWeapon weapon in new[] {BlightWeapon.Sword, BlightWeapon.Axe})
                foreach (SwingHeight height in new[] {SwingHeight.Arms, SwingHeight.Torso, SwingHeight.Legs})
                {
                    Arrange(); Enemy.position = Vector3.forward * 12;
                    Assert.That(lab.TryEquip(weapon), Is.True); lab.SetLimbAim(height);
                    Assert.That(lab.RequestPlayerAttack(true), Is.True);
                    var visual = Actor("Company fighter").GetComponentInChildren<RefinedLimbVisual>();
                    for (int i = 0; i < 160; i++)
                    {
                        lab.Simulate(1f / 60f);
                        Assert.That(visual.Rig.GripError, Is.LessThan(.005f), weapon + " " + height + " " + i);
                    }
                    yield return null;
                }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
