using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class BlightLootPlayTests
    {
        private GameObject root;
        private BlightCombatLab lab;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Loot tests"); lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = false; lab.AutomaticSimulation = false; lab.CombatAudioEnabled = false;
            yield return null; lab.SelectScenario(BlightScenario.Weapons);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { Object.Destroy(root); yield return null; }
        private Transform Actor(string name)
        {
            foreach (Transform child in root.transform)
                if (child.name == name && child.gameObject.activeSelf) return child;
            return null;
        }
        private void DefeatEnemy()
        {
            lab.GetFighter(Actor("Blighted Thrall")).Receive(1000, false, false);
            lab.Simulate(0.02f);
        }
        private void Collect()
        {
            Actor("Company fighter").position = Actor("Blighted Thrall").position;
            Assert.That(lab.Interact(), Is.True);
        }

        [UnityTest] public IEnumerator LastEnemyDropsOnceAndCollectionRequiresRangeAndDoesNotAutoEquip()
        {
            DefeatEnemy(); lab.Simulate(0.2f);
            Assert.That(lab.GroundLootCount, Is.EqualTo(1));
            Assert.That(lab.Interact(), Is.False, "Too far away.");
            Assert.That(lab.StartNextLootEncounter(), Is.False, "Do not discard an uncollected drop.");
            Collect(); Assert.That(lab.Interact(), Is.False);
            Assert.That(lab.OwnedLootCount, Is.EqualTo(1));
            Assert.That(lab.GroundLootCount, Is.Zero);
            Assert.That(lab.EquippedLoot.HasValue, Is.False);
            Assert.That(lab.PlayerFighter.Dodge(), Is.True);
            lab.Simulate(0.1f);
            Assert.That(lab.PlayerFighter.Remaining, Is.LessThan(0.32f), "Clear encounters must still simulate.");
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator ThreeEncountersRecoverAllVariantsAndResetClearsThem()
        {
            for (int i = 0; i < 4; i++)
            {
                DefeatEnemy(); Collect();
                Assert.That(lab.OwnedLootCount, Is.EqualTo(Mathf.Min(i + 1, 3)));
                if (i < 3) Assert.That(lab.StartNextLootEncounter(), Is.True);
            }
            Assert.That(lab.TryEquipLoot(BlightLoot.ExecutionersAxe), Is.True);
            Assert.That(lab.StartNextLootEncounter(), Is.True);
            Assert.That(lab.EquippedLoot, Is.EqualTo(BlightLoot.ExecutionersAxe));
            Assert.That(lab.PlayerWeapon, Is.EqualTo(BlightWeapon.Axe));
            Assert.That(lab.LivingEnemies, Is.EqualTo(1));
            lab.ResetFight();
            Assert.That(lab.OwnedLootCount, Is.Zero); Assert.That(lab.GroundLootCount, Is.Zero);
            Assert.That(lab.EquippedLoot.HasValue, Is.False);
            Assert.That(lab.PlayerWeapon, Is.EqualTo(BlightWeapon.Sword));
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator RecoveredStatsReachActualAttackAndCannotSwapMidWindup()
        {
            Assert.That(lab.TryEquipLoot(BlightLoot.DuelistSword), Is.False);
            DefeatEnemy(); Collect(); Assert.That(lab.TryEquipLoot(BlightLoot.DuelistSword), Is.True);
            Assert.That(lab.StartNextLootEncounter(), Is.True);
            Assert.That(lab.RequestPlayerAttack(true), Is.True);
            AttackSpec expected = BlightLootCatalog.Attack(BlightLoot.DuelistSword, true);
            Assert.That(lab.PlayerFighter.Strike.Damage, Is.EqualTo(expected.Damage));
            Assert.That(lab.PlayerFighter.Strike.Windup, Is.EqualTo(expected.Windup));
            Assert.That(lab.PlayerFighter.Stamina, Is.EqualTo(100 - expected.Cost));
            Assert.That(lab.TryEquip(BlightWeapon.Axe), Is.False);
            Assert.That(lab.TryEquipLoot(BlightLoot.DuelistSword), Is.False);
            yield return null;
        }

        [UnityTest] public IEnumerator PauseAndScenarioSwitchProtectLootState()
        {
            DefeatEnemy();
            Actor("Company fighter").position = Actor("Blighted Thrall").position;
            lab.SetPaused(true); Assert.That(lab.Interact(), Is.False);
            Assert.That(lab.GroundLootCount, Is.EqualTo(1));
            lab.SetPaused(false); Collect(); lab.SetPaused(true);
            Assert.That(lab.TryEquipLoot(BlightLoot.DuelistSword), Is.False);
            Assert.That(lab.StartNextLootEncounter(), Is.False);
            lab.SelectScenario(BlightScenario.Patrol);
            Assert.That(lab.OwnedLootCount, Is.Zero); Assert.That(lab.GroundLootCount, Is.Zero);
            Assert.That(lab.Interact(), Is.False); Assert.That(lab.Patrol.SpearRecovered, Is.False);
            yield return null;
        }
    }
}
