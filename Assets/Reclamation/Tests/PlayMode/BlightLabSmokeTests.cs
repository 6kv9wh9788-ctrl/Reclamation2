using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class BlightLabSmokeTests
    {
        private GameObject root;
        private BlightCombatLab lab;

        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Test blight lab");
            lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = false; lab.AutomaticSimulation = false;
            yield return null;
            lab.SetPaused(false);
        }

        [UnityTearDown] public IEnumerator Cleanup()
        { if (root != null) Object.Destroy(root); yield return null; }

        private Transform Actor(string name)
        {
            foreach (Transform child in root.transform)
                if (child.gameObject.activeSelf && child.name == name) return child;
            return null;
        }

        private void Advance(float seconds)
        { for (int i = 0; i < Mathf.CeilToInt(seconds / 0.05f); i++) lab.Simulate(0.05f); }

        [UnityTest] public IEnumerator ArenaStartsAndResetRestoresActors()
        {
            Assert.That(Actor("Company fighter"), Is.Not.Null);
            Assert.That(Actor("Blighted Thrall"), Is.Not.Null);
            Assert.That(root.GetComponentInChildren<Camera>(), Is.Not.Null);
            lab.PlayerFighter.Receive(35, false, false);
            Actor("Company fighter").position = Vector3.right * 8;
            lab.ResetFight();
            Assert.That(Actor("Company fighter").position, Is.EqualTo(new Vector3(0, 0, -4)));
            Assert.That(lab.PlayerFighter.Health, Is.EqualTo(100));
            Assert.That(lab.LivingEnemies, Is.EqualTo(1));
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator EveryScenarioResetsWithoutLeakingActorsOrLoot()
        {
            int[] enemies = { 1, 3, 1, 1, 3, 1 };
            int[] companions = { 0, 2, 2, 0, 2, 0 };
            for (int i = 0; i < 6; i++)
            {
                lab.SelectScenario((BlightScenario)i);
                yield return null;
                Assert.That(lab.LivingEnemies, Is.EqualTo(enemies[i]));
                Assert.That(lab.LivingCompanions, Is.EqualTo(companions[i]));
                Assert.That(lab.PlayerWeapon, Is.EqualTo(BlightWeapon.Sword));
                Assert.That(lab.Patrol.SpearRecovered, Is.False);
                Assert.That(root.GetComponentsInChildren<Camera>().Length, Is.EqualTo(1));
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator PauseFreezesDamageTimersAndMovement()
        {
            lab.PlayerFighter.Attack(false);
            float remaining = lab.PlayerFighter.Remaining;
            Vector3 enemyPosition = Actor("Blighted Thrall").position;
            lab.SetPaused(true); Advance(2);
            Assert.That(lab.PlayerFighter.Remaining, Is.EqualTo(remaining));
            Assert.That(Actor("Blighted Thrall").position, Is.EqualTo(enemyPosition));
            lab.SetPaused(false); lab.Simulate(0.1f);
            Assert.That(lab.PlayerFighter.Remaining, Is.LessThan(remaining));
            yield return null;
        }

        [UnityTest] public IEnumerator WeaponSwapRequiresSpaceAndCannotCancelWindup()
        {
            lab.SelectScenario(BlightScenario.Weapons);
            Assert.That(lab.TryEquip(BlightWeapon.Spear), Is.True);
            lab.PlayerFighter.Attack(BlightEquipment.Weapon(BlightWeapon.Spear, true));
            Assert.That(lab.TryEquip(BlightWeapon.Sword), Is.False);
            lab.ResetFight();
            Actor("Company fighter").position = Actor("Blighted Thrall").position + Vector3.back * 2;
            Assert.That(lab.TryEquip(BlightWeapon.Spear), Is.False);
            yield return null;
        }

        [UnityTest] public IEnumerator HoldAndWithdrawPreserveOrderAndDoNotChase()
        {
            lab.SelectScenario(BlightScenario.Patrol);
            Transform ally = Actor("Mara - swordswoman");
            lab.GiveOrder(SquadOrder.Hold);
            Vector3 anchor = ally.position;
            Actor("Company fighter").position += Vector3.right * 6;
            Advance(2);
            Assert.That(Vector3.Distance(ally.position, anchor), Is.LessThan(0.1f));
            ally.position = Vector3.zero;
            float before = Vector3.Distance(ally.position, lab.CampPosition);
            lab.GiveOrder(SquadOrder.Withdraw); Advance(1);
            Assert.That(lab.Order, Is.EqualTo(SquadOrder.Withdraw));
            Assert.That(Vector3.Distance(ally.position, lab.CampPosition), Is.LessThan(before));
            Assert.That(lab.GetFighter(ally).Action, Is.Not.EqualTo(DuelAction.Windup));
            yield return null;
        }

        [UnityTest] public IEnumerator PatrolRequiresClearCacheThenReturnAndHealsOnlyAtCamp()
        {
            lab.SelectScenario(BlightScenario.Patrol);
            Assert.That(lab.TryEquip(BlightWeapon.Spear), Is.False);
            Assert.That(lab.Interact(), Is.False);
            lab.PlayerFighter.Receive(40, false, false);
            float wounded = lab.PlayerFighter.Health;
            lab.Simulate(0.2f);
            Assert.That(lab.PlayerFighter.Health, Is.GreaterThan(wounded), "A safe camp heals.");
            Transform player = Actor("Company fighter");
            player.position = new Vector3(12, 0, -10);
            wounded = lab.PlayerFighter.Health;
            lab.Simulate(0.2f);
            Assert.That(lab.PlayerFighter.Health, Is.EqualTo(wounded), "The field does not heal.");

            foreach (string name in new[] { "Road thrall 1", "Road thrall 2", "Cache guardian" })
                lab.GetFighter(Actor(name)).Receive(1000, false, false);
            lab.Simulate(0.05f);
            Assert.That(lab.Patrol.Stage, Is.EqualTo(PatrolStage.CacheAvailable));
            Assert.That(lab.Interact(), Is.False, "Clearing enemies alone does not collect the cache.");
            player.position = lab.CachePosition;
            Assert.That(lab.Interact(), Is.True);
            Assert.That(lab.PlayerWeapon, Is.EqualTo(BlightWeapon.Spear));
            Assert.That(lab.Interact(), Is.False, "Reward cannot be duplicated.");
            Assert.That(lab.Patrol.Stage, Is.EqualTo(PatrolStage.Returning));
            player.position = lab.CampPosition; lab.Simulate(0.05f);
            Assert.That(lab.Patrol.Stage, Is.EqualTo(PatrolStage.Complete));
            lab.ResetFight();
            Assert.That(lab.Patrol.Stage, Is.EqualTo(PatrolStage.Outbound));
            Assert.That(lab.Patrol.SpearRecovered, Is.False);
            Assert.That(lab.LivingEnemies, Is.EqualTo(3));
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator HulkSweepHitsMultipleFightersOnlyOnce()
        {
            lab.SelectScenario(BlightScenario.Hulk);
            Transform hulk = Actor("Blightbound Hulk");
            hulk.position = Vector3.zero; hulk.rotation = Quaternion.identity;
            string[] names = { "Company fighter", "Mara - swordswoman", "Bren - spearman" };
            for (int i = 0; i < names.Length; i++)
            {
                Transform actor = Actor(names[i]);
                actor.position = new Vector3((i - 1) * 1.1f, 0, 2.4f);
                // Committed actions cannot instantly cancel into a dodge.
                var committed = BlightEquipment.Weapon(BlightWeapon.Sword, true);
                committed.Windup = 10;
                lab.GetFighter(actor).Attack(committed);
            }
            DuelFighter enemy = lab.GetFighter(hulk);
            var sweep = BlightEquipment.Enemy(BlightEnemy.Hulk, 0);
            enemy.Attack(sweep); enemy.Advance(sweep.Windup - 0.01f);
            lab.Simulate(0.02f);
            foreach (string name in names)
                Assert.That(lab.GetFighter(Actor(name)).Health, Is.EqualTo(76), name);
            lab.Simulate(0.2f);
            foreach (string name in names)
                Assert.That(lab.GetFighter(Actor(name)).Health, Is.EqualTo(76), "No repeated overlap damage: " + name);
            yield return null;
        }

        [UnityTest] public IEnumerator NearbyHostilesPreventCampHealing()
        {
            lab.SelectScenario(BlightScenario.Patrol);
            lab.PlayerFighter.Receive(40, false, false);
            Actor("Road thrall 1").position = lab.CampPosition + Vector3.forward * 6;
            lab.Simulate(0.1f);
            Assert.That(lab.PlayerFighter.Health, Is.EqualTo(60));
            yield return null;
        }
    }
}
