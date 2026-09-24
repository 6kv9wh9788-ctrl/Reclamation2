using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class BlightOutpostPlayTests
    {
        private GameObject root;
        private BlightCombatLab lab;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Outpost tests"); lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = false; lab.AutomaticSimulation = false; lab.CombatAudioEnabled = false;
            yield return null; lab.SelectScenario(BlightScenario.Outpost);
        }
        [UnityTearDown] public IEnumerator Cleanup() { Object.Destroy(root); yield return null; }
        private Transform Actor(string name)
        {
            foreach (Transform child in root.transform)
                if (child.name == name && child.gameObject.activeSelf) return child;
            Assert.Fail("Missing active actor: " + name); return null;
        }
        private Transform Hero => Actor("Company fighter");
        private Transform Mara => Actor("Mara - swordswoman");
        private Transform Bren => Actor("Bren - spearman");
        private void Rally(Vector3 point)
        {
            Hero.position = point; Mara.position = point + Vector3.left * 2; Bren.position = point + Vector3.right * 2;
        }
        private void ClearWave()
        {
            foreach (Transform child in root.transform)
            {
                if (!child.gameObject.activeSelf || child == Hero || child == Mara || child == Bren) continue;
                DuelFighter fighter = lab.GetFighter(child);
                if (fighter != null) fighter.Receive(10000, false, false);
            }
            lab.Simulate(.02f);
        }
        private void Begin(bool east = false)
        {
            Rally(new Vector3(east ? 8 : -8, 0, -12)); Assert.That(lab.Interact(), Is.True);
            Assert.That(lab.MissionStage, Is.EqualTo(OutpostStage.Patrol));
            Assert.That(lab.LivingEnemies, Is.EqualTo(east ? 3 : 2));
        }
        private void ReachReward()
        {
            Begin(); ClearWave();
            Assert.That(lab.MissionStage, Is.EqualTo(OutpostStage.Regroup));
            Rally(new Vector3(0, 0, -3)); Assert.That(lab.Interact(), Is.True);
            Assert.That(lab.MissionStage, Is.EqualTo(OutpostStage.Stronghold));
            Assert.That(lab.LivingEnemies, Is.EqualTo(3));
            ClearWave(); Assert.That(lab.MissionStage, Is.EqualTo(OutpostStage.Cache));
            Assert.That(lab.Interact(), Is.False, "Cache requires proximity.");
            Rally(lab.CachePosition); Assert.That(lab.Interact(), Is.True);
        }

        [UnityTest] public IEnumerator ConnectedMissionRetainsRewardAndRequiresSurvivorsAtCamp()
        {
            ReachReward();
            Assert.That(lab.OwnedLootCount, Is.EqualTo(1));
            Assert.That(lab.EquippedLoot.HasValue, Is.False, "Collection must not auto-equip.");
            Assert.That(lab.TryEquipLoot(BlightLoot.WardensSpear), Is.True);
            AttackSpec reward = BlightLootCatalog.Attack(BlightLoot.WardensSpear, false);
            Assert.That(lab.PlayerAttack(false).Damage, Is.EqualTo(reward.Damage));
            Assert.That(lab.Interact(), Is.True);
            Assert.That(lab.MissionStage, Is.EqualTo(OutpostStage.Rearguard));
            Assert.That(lab.LivingEnemies, Is.EqualTo(3));
            Assert.That(lab.Interact(), Is.False, "Repeated departure cannot duplicate enemies.");
            Assert.That(lab.LivingEnemies, Is.EqualTo(3));
            ClearWave(); Assert.That(lab.MissionStage, Is.EqualTo(OutpostStage.Returning));
            Hero.position = lab.CampPosition; lab.Simulate(.02f);
            Assert.That(lab.MissionStage, Is.EqualTo(OutpostStage.Returning));
            Rally(lab.CampPosition); lab.Simulate(.02f);
            Assert.That(lab.MissionStage, Is.EqualTo(OutpostStage.Complete));
            Assert.That(lab.MissionSurvivors, Is.EqualTo(3)); Assert.That(lab.MissionKills, Is.EqualTo(8));
            Assert.That(lab.EquippedLoot, Is.EqualTo(BlightLoot.WardensSpear));
            Assert.That(lab.RequestPlayerAttack(false), Is.False);
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator ApproachNeedsRallyAndPausePreventsProgressOrSpawns()
        {
            Assert.That(lab.LivingEnemies, Is.Zero);
            Assert.That(lab.Interact(), Is.False);
            Hero.position = new Vector3(8, 0, -12);
            Assert.That(lab.Interact(), Is.False, "Companions still at camp.");
            Assert.That(lab.MissionRoute, Is.EqualTo(OutpostRoute.None));
            Rally(Hero.position); lab.SetPaused(true);
            lab.Simulate(.2f); Assert.That(lab.MissionSeconds, Is.Zero);
            Assert.That(lab.Interact(), Is.False); Assert.That(lab.LivingEnemies, Is.Zero);
            lab.SetPaused(false); Assert.That(lab.Interact(), Is.True);
            Assert.That(lab.MissionRoute, Is.EqualTo(OutpostRoute.East));
            Assert.That(lab.LivingEnemies, Is.EqualTo(3));
            Assert.That(lab.Interact(), Is.False); Assert.That(lab.LivingEnemies, Is.EqualTo(3));
            ClearWave(); Assert.That(lab.MissionStage, Is.EqualTo(OutpostStage.Regroup));
            Assert.That(lab.Interact(), Is.False, "Rally point requires proximity.");
            yield return null;
        }

        [UnityTest] public IEnumerator CasualtiesDoNotBlockCompletionAndResetClearsMissionState()
        {
            ReachReward(); lab.GetFighter(Mara).Receive(10000, false, false);
            Assert.That(lab.Interact(), Is.True); ClearWave();
            Rally(lab.CampPosition); lab.Simulate(.02f);
            Assert.That(lab.MissionStage, Is.EqualTo(OutpostStage.Complete));
            Assert.That(lab.MissionSurvivors, Is.EqualTo(2));
            Assert.That(lab.GetFighter(Mara).Alive, Is.False, "Camp cannot resurrect casualties.");
            lab.ResetFight();
            Assert.That(lab.MissionStage, Is.EqualTo(OutpostStage.Approach));
            Assert.That(lab.MissionRoute, Is.EqualTo(OutpostRoute.None));
            Assert.That(lab.MissionSeconds, Is.Zero); Assert.That(lab.MissionKills, Is.Zero);
            Assert.That(lab.OwnedLootCount, Is.Zero); Assert.That(lab.LivingEnemies, Is.Zero);
            Assert.That(lab.LivingCompanions, Is.EqualTo(2));
            lab.SelectScenario(BlightScenario.Duel); yield return null;
            Assert.That(root.transform.Find("Outpost mission scenery"), Is.Null);
            Assert.That(lab.LivingEnemies, Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator DefeatStopsMissionAndRewardCannotBeClaimedEarly()
        {
            Begin(); Hero.position = lab.CachePosition;
            Assert.That(lab.Interact(), Is.False); Assert.That(lab.OwnedLootCount, Is.Zero);
            Assert.That(lab.TryEquipLoot(BlightLoot.WardensSpear), Is.False);
            lab.PlayerFighter.Receive(10000, false, false);
            float elapsed = lab.MissionSeconds; lab.Simulate(.25f);
            Assert.That(lab.MissionSeconds, Is.EqualTo(elapsed)); Assert.That(lab.Interact(), Is.False);
            lab.ResetFight(); Assert.That(lab.PlayerFighter.Alive, Is.True);
            yield return null;
        }

        [UnityTest] public IEnumerator ClearIntervalsKeepSimulationRunningAndGearPreparationDoesNotStartReturn()
        {
            ReachReward();
            Assert.That(lab.MissionStage, Is.EqualTo(OutpostStage.PrepareReturn));
            Assert.That(lab.PlayerFighter.Dodge(), Is.True);
            for (int i = 0; i < 30; i++) lab.Simulate(1f / 60f);
            Assert.That(lab.PlayerFighter.CanAct, Is.True);
            Assert.That(lab.MissionStage, Is.EqualTo(OutpostStage.PrepareReturn));
            Assert.That(lab.LivingEnemies, Is.Zero);
            lab.SetPaused(true);
            Assert.That(lab.TryEquipLoot(BlightLoot.WardensSpear), Is.False);
            Assert.That(lab.Interact(), Is.False);
            yield return null;
        }
    }
}
