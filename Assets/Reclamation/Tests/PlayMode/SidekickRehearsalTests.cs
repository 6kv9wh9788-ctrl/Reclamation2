using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class SidekickRehearsalTests
    {
        private GameObject root;
        private BlightCombatLab lab;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Sidekick rehearsal regression");
            lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = false; lab.AutomaticSimulation = false; lab.CombatAudioEnabled = false;
            yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup() { Object.Destroy(root); yield return null; }
        [UnityTest] public IEnumerator RehearsalUsesAttackPosesWithoutSpendingLiveHealthOrStamina()
        {
            float health = lab.PlayerFighter.Health, stamina = lab.PlayerFighter.Stamina;
            Vector3 position = lab.SidekickPlayerRoot.position;
            foreach (BlightWeapon weapon in new[] { BlightWeapon.Sword, BlightWeapon.Spear, BlightWeapon.Axe })
                foreach (bool heavy in new[] { false, true })
                {
                    var scratch = new DuelFighter(); Assert.That(scratch.Attack(BlightEquipment.Weapon(weapon, heavy)), Is.True);
                    int impacts = 0;
                    for (int i = 0; i < 420; i++)
                    {
                        if (scratch.Advance(1f / 120)) impacts++;
                        lab.SidekickRehearse(scratch, weapon, false, i / 120f);
                        if (i == 1) Assert.That(lab.SidekickAnimationSource.Rig.CurrentClip, Is.EqualTo(heavy ? "HeavyAttack" : "LightAttack"));
                    }
                    Assert.That(impacts, Is.EqualTo(1));
                    Assert.That(lab.PlayerFighter.Health, Is.EqualTo(health));
                    Assert.That(lab.PlayerFighter.Stamina, Is.EqualTo(stamina));
                    Assert.That(lab.PlayerFighter.Action, Is.EqualTo(DuelAction.Ready));
                    Assert.That(lab.SidekickPlayerRoot.position, Is.EqualTo(position));
                    yield return null;
                }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ResetDiscardsTheRehearsalPoseAndSuppliesNewHero()
        {
            var previous = lab.SidekickAnimationSource;
            var scratch = new DuelFighter(); scratch.Receive(1000, true, true);
            lab.SidekickRehearse(scratch, BlightWeapon.Axe, false, 0);
            Assert.That(previous.Rig.CurrentClip, Is.EqualTo("Death"));
            lab.ResetFight(); yield return null;
            Assert.That(lab.SidekickAnimationSource, Is.Not.Null);
            Assert.That(lab.SidekickAnimationSource == previous, Is.False);
            Assert.That(lab.PlayerFighter.Alive, Is.True);
            Assert.That(lab.SidekickAnimationSource.Weapon, Is.EqualTo(BlightWeapon.Sword));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
