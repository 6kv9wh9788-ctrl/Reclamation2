using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Reclamation.Tests
{
    public sealed class SidekickPlayerIntegrationTests
    {
        private GameObject root;
        private BlightCombatLab lab;
        private SidekickDuelBridge bridge;

        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Sidekick player integration");
            lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = lab.AutomaticSimulation = lab.CombatAudioEnabled = false;
            bridge = root.AddComponent<SidekickDuelBridge>();
            bridge.lab = lab;
            bridge.characterPrefab = Resources.Load<GameObject>("Player/SidekickPlayer");
            bridge.hideUnrelatedPreviews = bridge.showStatus = false;
            Assert.That(bridge.characterPrefab, Is.Not.Null);
            yield return null;
            bridge.RefreshPresentation();
            Assert.That(bridge.Ready, Is.True);
        }
        [UnityTearDown] public IEnumerator Cleanup() { Object.Destroy(root); yield return null; }

        [UnityTest] public IEnumerator PlayerOnlyReplacementSurvivesScenariosResetAndDisable()
        {
            var unrelated = new GameObject("Unrelated skinned renderer").AddComponent<SkinnedMeshRenderer>();
            unrelated.transform.SetParent(root.transform);
            foreach (BlightScenario scenario in new[] { BlightScenario.Duel, BlightScenario.Squad, BlightScenario.Hulk })
            {
                lab.SelectScenario(scenario);
                bridge.RefreshPresentation();
                yield return null;
                Assert.That(bridge.Ready, Is.True);
                Assert.That(bridge.ModelRoot.parent, Is.EqualTo(lab.SidekickPlayerRoot));
                Assert.That(root.GetComponentsInChildren<Transform>().Count(t => t.name == "Sidekick playable hero"), Is.EqualTo(1));
                Assert.That(lab.SidekickAnimationSource.GetComponentsInChildren<Renderer>().All(r => !r.enabled), Is.True);
                foreach (BlightHeroVisual hero in root.GetComponentsInChildren<BlightHeroVisual>())
                    if (hero != lab.SidekickAnimationSource)
                        Assert.That(hero.GetComponentsInChildren<Renderer>().Any(r => r.enabled), Is.True, "Companions keep their presentation.");
                Assert.That(unrelated.enabled, Is.True);
                lab.ResetFight(); bridge.RefreshPresentation(); yield return null;
                Assert.That(bridge.Ready, Is.True);
            }
            bridge.enabled = false;
            yield return null;
            Assert.That(bridge.ModelRoot, Is.Null);
            Assert.That(lab.SidekickAnimationSource.GetComponentsInChildren<Renderer>().Any(r => r.enabled), Is.True);
            bridge.enabled = true; bridge.RefreshPresentation();
            Assert.That(bridge.Ready, Is.True);
            lab.SelectScenario(BlightScenario.LimbDamage); bridge.RefreshPresentation(); yield return null;
            Assert.That(bridge.Ready, Is.False, "Limb contact lab keeps its authoritative articulated rig.");
            lab.SelectScenario(BlightScenario.Duel); bridge.RefreshPresentation();
            Assert.That(bridge.Ready, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator LiveActionsKeepTimingStaminaAndWeaponGrip()
        {
            foreach (BlightWeapon kind in new[] { BlightWeapon.Sword, BlightWeapon.Spear, BlightWeapon.Axe })
                foreach (bool heavy in new[] { false, true })
                {
                    lab.ResetFight(); bridge.RefreshPresentation();
                    Assert.That(lab.TryEquip(kind), Is.True);
                    DuelFighter fighter = lab.PlayerFighter;
                    AttackSpec spec = BlightEquipment.Weapon(kind, heavy);
                    Assert.That(fighter.Attack(spec), Is.True);
                    for (int step = 0; step < 90; step++)
                    {
                        lab.Simulate(1f / 60f);
                        DuelAction action = fighter.Action;
                        float remaining = fighter.Remaining, stamina = fighter.Stamina, health = fighter.Health;
                        Vector3 position = lab.SidekickPlayerRoot.position;
                        bridge.RefreshPresentation();
                        Assert.That(fighter.Action, Is.EqualTo(action));
                        Assert.That(fighter.Remaining, Is.EqualTo(remaining));
                        Assert.That(fighter.Stamina, Is.EqualTo(stamina));
                        Assert.That(fighter.Health, Is.EqualTo(health));
                        Assert.That(lab.SidekickPlayerRoot.position, Is.EqualTo(position));
                        Assert.That(bridge.RightGripError, Is.LessThan(.005f));
                        Animator avatar = bridge.ModelRoot.GetComponentInChildren<Animator>();
                        Vector3 knuckles = avatar.GetBoneTransform(HumanBodyBones.RightIndexProximal).position - avatar.GetBoneTransform(HumanBodyBones.RightLittleProximal).position;
                        Assert.That(Vector3.Dot(knuckles.normalized, bridge.WeaponRoot.forward), Is.GreaterThan(.8f), "Index side of the grip faces the blade, not the pommel.");
                        if (bridge.SupportGripActive) Assert.That(bridge.LeftGripError, Is.LessThan(.08f), kind + " support hand");
                        Assert.That(bridge.WeaponRoot.IsChildOf(bridge.ModelRoot), Is.True);
                    }
                    yield return null;
                }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator PausedBridgeFreezesAndAvatarCannotDrivePhysics()
        {
            lab.SetPlayerLocomotion(Vector3.forward, false, false);
            lab.Simulate(.1f); bridge.RefreshPresentation();
            lab.SetPaused(true);
            Transform[] bones = bridge.ModelRoot.GetComponentsInChildren<Transform>();
            Vector3[] positions = bones.Select(t => t.localPosition).ToArray();
            Quaternion[] rotations = bones.Select(t => t.localRotation).ToArray();
            for (int i = 0; i < 4; i++) { yield return null; bridge.RefreshPresentation(); }
            for (int i = 0; i < bones.Length; i++)
            {
                Assert.That(bones[i].localPosition, Is.EqualTo(positions[i]));
                Assert.That(bones[i].localRotation, Is.EqualTo(rotations[i]));
            }
            Assert.That(bridge.ModelRoot.GetComponentsInChildren<Collider>(true).All(c => !c.enabled), Is.True);
            Assert.That(bridge.ModelRoot.GetComponentsInChildren<Rigidbody>(true).All(b => b.isKinematic), Is.True);
            Animator animator = bridge.ModelRoot.GetComponentInChildren<Animator>();
            Assert.That(animator.applyRootMotion, Is.False);
            Assert.That(animator.enabled, Is.False);
            lab.SetPaused(false);
            Assert.That(lab.PlayerFighter.Dodge(), Is.True);
            float stamina = lab.PlayerFighter.Stamina;
            bridge.RefreshPresentation();
            Assert.That(lab.PlayerFighter.Stamina, Is.EqualTo(stamina));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator ReviewPosesRenderWithoutChangingLiveCombat()
        {
            string directory = Environment.GetEnvironmentVariable("RECLAMATION_SIDEKICK_CAPTURE_DIR");
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            float health = lab.PlayerFighter.Health, stamina = lab.PlayerFighter.Stamina;
            foreach (string pose in new[] { "Idle", "Walk", "Light", "Heavy", "Block", "Dodge", "Hit", "Defeat" })
            {
                var fighter = new DuelFighter();
                if (pose == "Light" || pose == "Heavy") { fighter.Attack(BlightEquipment.Weapon(BlightWeapon.Sword, pose == "Heavy")); fighter.Advance(fighter.Remaining + .01f); }
                if (pose == "Block") fighter.Blocking = true;
                if (pose == "Dodge") { fighter.Dodge(); fighter.Advance(.16f); }
                if (pose == "Hit") fighter.Receive(12, true, true);
                if (pose == "Defeat") fighter.Receive(1000, true, true);
                bridge.ReviewFighter = fighter; bridge.ReviewWeapon = BlightWeapon.Sword;
                bridge.ReviewWalking = pose == "Walk"; bridge.ReviewDelta = 1f / 60f;
                for (int i = 0; i < 15; i++) { lab.SidekickRehearse(fighter, BlightWeapon.Sword, bridge.ReviewWalking, i / 60f); bridge.RefreshPresentation(); }
                Assert.That(lab.PlayerFighter.Health, Is.EqualTo(health));
                if (pose == "Idle" || pose == "Light" || pose == "Heavy" || pose == "Block") AssertArmClearance(pose);
                Assert.That(lab.PlayerFighter.Stamina, Is.EqualTo(stamina));
                yield return null; // Let the skinned mesh consume this pose before offscreen rendering.
                if (!string.IsNullOrEmpty(directory)) { Capture(Path.Combine(directory, pose + ".png")); Capture(Path.Combine(directory, pose + "-hands.png"), true); Capture(Path.Combine(directory, pose + "-front.png"), true, 1); Capture(Path.Combine(directory, pose + "-side.png"), true, 2); }
                yield return null;
            }
            bridge.ReviewFighter = null;
            LogAssert.NoUnexpectedReceived();
        }

        private void AssertArmClearance(string pose)
        {
            Animator avatar = bridge.ModelRoot.GetComponentInChildren<Animator>();
            Vector3 left = avatar.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
            Vector3 right = avatar.GetBoneTransform(HumanBodyBones.RightUpperArm).position;
            Vector3 side = right - left; side.y = 0; side.Normalize();
            Vector3 forward = Vector3.Cross(side, Vector3.up);
            Vector3 center = (left + right) * .5f;
            // A conservative torso core, not a claim of mesh-level collision detection.
            // Elbows and forearms must not cut through this volume below the shoulders.
            for (int arm = 0; arm < 2; arm++)
            {
                Vector3 elbow = avatar.GetBoneTransform(arm == 0 ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm).position;
                Vector3 wrist = avatar.GetBoneTransform(arm == 0 ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand).position;
                for (int sample = 0; sample <= 4; sample++)
                {
                    Vector3 point = Vector3.Lerp(elbow, wrist, sample / 4f) - center;
                    float lateral = Vector3.Dot(point, side), depth = Vector3.Dot(point, forward);
                    Assert.That(lateral * lateral + depth * depth, Is.GreaterThan(.18f * .18f), pose + " arm " + arm + " enters torso core at sample " + sample);
                }
            }
        }
        private void Capture(string path, bool closeup = false, int angle = 0)
        {
            if (angle == 1)
            {
                Animator avatar = bridge.ModelRoot.GetComponentInChildren<Animator>();
                var ids = new[] { HumanBodyBones.Hips, HumanBodyBones.Chest, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand };
                File.WriteAllLines(Path.ChangeExtension(path, ".bones.txt"), ids.Select(id => id + " " + bridge.ModelRoot.InverseTransformPoint(avatar.GetBoneTransform(id).position).ToString("F4")));
            }
            var cameraObject = new GameObject("Sidekick inspection camera");
            var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
            camera.transform.position = lab.SidekickPlayerRoot.position + new Vector3(2.5f, 1.8f, 3.4f);
            camera.transform.LookAt(lab.SidekickPlayerRoot.position + Vector3.up);
            if (closeup) { camera.transform.position = lab.SidekickPlayerRoot.position + new Vector3(1.25f, 1.65f, 1.9f); camera.transform.LookAt(lab.SidekickPlayerRoot.position + new Vector3(0, 1.25f, .3f)); }
            if (angle != 0) { camera.transform.position = lab.SidekickPlayerRoot.position + (angle == 1 ? new Vector3(0, 1.4f, 2.5f) : new Vector3(2.5f, 1.4f, 0)); camera.transform.LookAt(lab.SidekickPlayerRoot.position + new Vector3(0, 1.2f, .15f)); }
            camera.fieldOfView = closeup ? 25 : 35;
            var target = new RenderTexture(900, 900, 24);
            var image = new Texture2D(900, 900, TextureFormat.RGB24, false);
            RenderTexture old = RenderTexture.active;
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 900, 900), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally { RenderTexture.active = old; target.Release(); Object.Destroy(target); Object.Destroy(image); Object.Destroy(cameraObject); }
        }
    }
}
