using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class BlightWeaponPlayTests
    {
        private GameObject root;
        private BlightCombatLab lab;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Weapon tests"); lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = false; lab.AutomaticSimulation = false; lab.CombatAudioEnabled = false;
            yield return null; lab.SelectScenario(BlightScenario.Weapons);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { Object.Destroy(root); yield return null; }
        private Transform Actor(string name)
        {
            foreach (Transform child in root.transform)
                if (child.gameObject.activeSelf && child.name == name) return child;
            return null;
        }
        private void Advance(float seconds)
        { for (int i = 0; i < Mathf.CeilToInt(seconds * 60); i++) lab.Simulate(1f / 60f); }

        [UnityTest] public IEnumerator SwapUsesSpacingAndCannotCancelAnAttackOrDodge()
        {
            Assert.That(lab.TryEquip(BlightWeapon.Axe), Is.True);
            Assert.That(lab.RequestPlayerAttack(true), Is.True);
            Assert.That(lab.TryEquip(BlightWeapon.Sword), Is.False);
            Assert.That(lab.PlayerFighter.Strike.Damage, Is.EqualTo(44));
            lab.ResetFight(); lab.PlayerFighter.Dodge();
            Assert.That(lab.TryEquip(BlightWeapon.Axe), Is.False);
            lab.ResetFight();
            Actor("Company fighter").position = Actor("Blighted Thrall").position + Vector3.back * 2;
            Assert.That(lab.TryEquip(BlightWeapon.Axe), Is.False);
            Assert.That(lab.TryEquip((BlightWeapon)99), Is.False);
            yield return null;
        }

        [UnityTest] public IEnumerator AxeHeadContactSeversOnceAndResetRestoresSword()
        {
            lab.SelectScenario(BlightScenario.LimbDamage); lab.LimbPractice = true;
            Assert.That(lab.TryEquip(BlightWeapon.Spear), Is.False);
            Assert.That(lab.TryEquip(BlightWeapon.Axe), Is.True);
            Transform enemy = Actor("Blighted Thrall");
            Actor("Company fighter").position = Vector3.zero; enemy.position = Vector3.forward * 1.8f;
            lab.Simulate(0.02f); Assert.That(lab.RequestPlayerAttack(true), Is.True); Advance(3);
            Assert.That(lab.GetLimbs(enemy).Missing(BodyRegion.RightArm), Is.True);
            Assert.That(lab.GetFighter(enemy).Health, Is.EqualTo(146.8f).Within(0.01f));
            Assert.That(lab.FeedbackEventCount, Is.EqualTo(1));
            lab.ResetFight();
            Assert.That(lab.PlayerWeapon, Is.EqualTo(BlightWeapon.Sword));
            Assert.That(lab.GetLimbs(Actor("Blighted Thrall")).Missing(BodyRegion.RightArm), Is.False);
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator PatrolKeepsItsOriginalRewardGate()
        {
            lab.SelectScenario(BlightScenario.Patrol);
            Assert.That(lab.TryEquip(BlightWeapon.Axe), Is.False);
            Assert.That(lab.TryEquip(BlightWeapon.Spear), Is.False);
            Assert.That(lab.PlayerWeapon, Is.EqualTo(BlightWeapon.Sword));
            yield return null;
        }
    }
}
