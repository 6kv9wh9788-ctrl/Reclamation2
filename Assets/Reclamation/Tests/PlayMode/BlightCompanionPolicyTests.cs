using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class BlightCompanionPolicyTests
    {
        private GameObject root;
        private BlightCombatLab lab;
        private Transform Hero => Actor("Company fighter");
        private Transform Mara => Actor("Mara - swordswoman");
        private Transform Bren => Actor("Bren - spearman");

        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Ordinary companion policy tests");
            lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = lab.AutomaticSimulation = lab.CombatAudioEnabled = false;
            yield return null;
            PrepareSquad();
        }

        [UnityTearDown] public IEnumerator Cleanup()
        { Object.Destroy(root); yield return null; }

        private Transform Actor(string name)
        {
            foreach (Transform t in root.transform)
                if (t.gameObject.activeSelf && t.name == name) return t;
            Assert.Fail("Missing actor " + name); return null;
        }

        private void Advance(float seconds)
        { for (int i = 0; i < Mathf.CeilToInt(seconds * 60); i++) lab.Simulate(1f / 60); }

        // Test fixture only: durable stationary targets let real attacks run for
        // ten seconds without ending the encounter. Production fighter rules stay intact.
        private DuelFighter DurableTarget(Transform target)
        {
            var actors = (IEnumerable)typeof(BlightCombatLab).GetField("actors", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(lab);
            foreach (object actor in actors)
                if ((Transform)actor.GetType().GetField("root").GetValue(actor) == target)
                {
                    var fighter = new DuelFighter(10000);
                    actor.GetType().GetField("fighter").SetValue(actor, fighter);
                    fighter.Interrupt(1000); return fighter;
                }
            Assert.Fail("Missing fighter"); return null;
        }

        private BlightTerrain Terrain => (BlightTerrain)typeof(BlightCombatLab)
            .GetField("terrain", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(lab);

        private void PrepareSquad()
        {
            lab.SelectScenario(BlightScenario.Squad);
            Hero.position = Vector3.zero;
            Mara.position = new Vector3(-2, 0, -1); Bren.position = new Vector3(2, 0, -1);
            for (int i = 1; i <= 3; i++)
            {
                Transform enemy = Actor("Thrall " + i);
                enemy.position = new Vector3(12, 0, 14 + i);
                DurableTarget(enemy);
            }
        }

        private static void Exhaust(DuelFighter fighter)
        {
            for (int i = 0; i < 4; i++)
            { Assert.That(fighter.Dodge(), Is.True); fighter.Advance(.4f); }
        }

        [UnityTest] public IEnumerator WeaponSpacingSettlesAndSurvivesTenSecondsOfRealAttacks()
        {
            Transform target = Actor("Thrall 1"); target.position = Vector3.forward * 4;
            lab.GiveOrder(SquadOrder.Assault); Advance(3);
            Assert.That(Vector3.Distance(Bren.position, target.position),
                Is.GreaterThan(Vector3.Distance(Mara.position, target.position) + .35f));
            int attacks = lab.GetFighter(Mara).AttackSequence + lab.GetFighter(Bren).AttackSequence;
            for (int step = 0; step < 600; step++)
            {
                lab.Simulate(1f / 60);
                Assert.That(Vector3.Distance(Mara.position, Bren.position), Is.GreaterThan(1));
                Assert.That(Vector3.Distance(Mara.position, Hero.position), Is.GreaterThanOrEqualTo(.81f));
                Assert.That(Vector3.Distance(Bren.position, Hero.position), Is.GreaterThanOrEqualTo(.81f));
                if (step % 60 == 0) yield return null;
            }
            Assert.That(lab.GetFighter(Mara).AttackSequence + lab.GetFighter(Bren).AttackSequence, Is.GreaterThan(attacks));
            Assert.That(lab.GetFighter(target).Health, Is.LessThan(10000));
        }

        [UnityTest] public IEnumerator WoundedCompanionYieldsWithoutHealingOrChangingTheOrder()
        {
            Actor("Thrall 1").position = new Vector3(-2, 0, 1.2f);
            lab.GiveOrder(SquadOrder.Hold);
            Vector3 start = Mara.position;
            DuelFighter fighter = lab.GetFighter(Mara); fighter.Receive(70, false, false);
            Advance(.35f);
            Assert.That(lab.CompanionRecovering(Mara), Is.True);
            Advance(1);
            Assert.That(Mara.position.z, Is.LessThan(start.z - .5f));
            Advance(3);
            Assert.That(fighter.Health, Is.EqualTo(30));
            Assert.That(fighter.AttackSequence, Is.Zero, "A staggered threat is not a recovery opening.");
            Assert.That(Vector3.Distance(Mara.position, start), Is.LessThanOrEqualTo(3));
            Assert.That(lab.Order, Is.EqualTo(SquadOrder.Hold));
            Assert.That(lab.GetCompanionIntent(Mara), Does.Contain("Wounded"));
            yield return null;
        }

        [UnityTest] public IEnumerator ExhaustionUsesHysteresisAndRecoversWithoutOscillating()
        {
            DuelFighter fighter = lab.GetFighter(Mara); Exhaust(fighter);
            lab.Simulate(1f / 60); Assert.That(lab.CompanionRecovering(Mara), Is.True);
            Advance(1.5f);
            Assert.That(fighter.Stamina, Is.InRange(30, 64));
            Assert.That(lab.CompanionRecovering(Mara), Is.True, "Keep recovering past entry threshold.");
            Advance(1.6f);
            Assert.That(fighter.Stamina, Is.GreaterThanOrEqualTo(65));
            Assert.That(lab.CompanionRecovering(Mara), Is.False);
            yield return null;
        }

        [UnityTest] public IEnumerator RecoveryHonorsCommitmentPauseResetAndWithdraw()
        {
            DuelFighter fighter = lab.GetFighter(Mara);
            var strike = BlightEquipment.Weapon(BlightWeapon.Sword, true); strike.Windup = 2;
            Assert.That(fighter.Attack(strike), Is.True); fighter.Receive(70, false, false);
            Vector3 start = Mara.position; Advance(.5f);
            Assert.That(Mara.position, Is.EqualTo(start)); Assert.That(fighter.Action, Is.EqualTo(DuelAction.Windup));
            lab.SetPaused(true); float remaining = fighter.Remaining;
            Advance(1); Assert.That(fighter.Remaining, Is.EqualTo(remaining)); Assert.That(Mara.position, Is.EqualTo(start));
            lab.SetPaused(false); Advance(3);
            Assert.That(lab.CompanionRecovering(Mara), Is.True);
            lab.GiveOrder(SquadOrder.Withdraw); float distance = Vector3.Distance(Mara.position, lab.CampPosition);
            int attacks = fighter.AttackSequence; Advance(2);
            Assert.That(fighter.AttackSequence, Is.EqualTo(attacks)); Assert.That(lab.GetCompanionTarget(Mara), Is.Null);
            Assert.That(Vector3.Distance(Mara.position, lab.CampPosition), Is.LessThan(distance - 2));
            lab.ResetFight(); yield return null;
            Assert.That(lab.CompanionRecovering(Mara), Is.False); Assert.That(lab.Order, Is.EqualTo(SquadOrder.Follow));
        }

        private void PrepareHulk(float side, bool blocked, bool exhausted)
        {
            lab.SelectScenario(BlightScenario.Hulk);
            Hero.position = new Vector3(10, 0, -12); Bren.position = new Vector3(-10, 0, -12);
            Mara.position = Vector3.zero;
            Transform enemy = Actor("Blightbound Hulk"); enemy.position = new Vector3(side * .3f, 0, 2);
            enemy.rotation = Quaternion.LookRotation(Mara.position - enemy.position);
            lab.GiveOrder(SquadOrder.Hold);
            if (exhausted) Exhaust(lab.GetFighter(Mara));
            if (blocked)
            {
                // Low barriers block movement but leave the heavy tell visible.
                Terrain.Add(new Rect(-1, -.85f, 2, .2f), false);
                Terrain.Add(new Rect(-1, .65f, 2, .2f), false);
                Terrain.Add(new Rect(-.85f, -1, .2f, 2), false);
                Terrain.Add(new Rect(.65f, -1, .2f, 2), false);
            }
        }

        [UnityTest] public IEnumerator HulkSweepAndSmashResolveAgainstPaidDodgesFromBothSides()
        {
            foreach (int sequence in new[] { 0, 1 }) foreach (float side in new[] { -1f, 1f })
            {
                PrepareHulk(side, false, false); yield return null;
                DuelFighter friend = lab.GetFighter(Mara), foe = lab.GetFighter(Actor("Blightbound Hulk"));
                var strike = BlightEquipment.Enemy(BlightEnemy.Hulk, sequence);
                Assert.That(foe.Attack(strike), Is.True); foe.Advance(strike.Windup - .2f);
                lab.Simulate(1f / 60);
                Assert.That(friend.Action, Is.EqualTo(DuelAction.Dodge)); Assert.That(friend.Stamina, Is.EqualTo(75).Within(.01f));
                Advance(.22f);
                Assert.That(foe.Action, Is.EqualTo(DuelAction.Recovery), "Cross the actual impact boundary.");
                Assert.That(friend.Health, Is.EqualTo(100));
            }
        }

        [UnityTest] public IEnumerator BlockedOrExhaustedHulkDefenseNeverGetsAFreeDodge()
        {
            foreach (int sequence in new[] { 0, 1 }) foreach (int constraint in new[] { 0, 1, 2 })
                foreach (float side in new[] { -1f, 1f })
                {
                    bool blocked = constraint != 1, exhausted = constraint != 0;
                    PrepareHulk(side, blocked, exhausted); yield return null;
                    DuelFighter friend = lab.GetFighter(Mara), foe = lab.GetFighter(Actor("Blightbound Hulk"));
                    var strike = BlightEquipment.Enemy(BlightEnemy.Hulk, sequence);
                    Assert.That(foe.Attack(strike), Is.True); foe.Advance(strike.Windup - .12f);
                    for (int i = 0; i < 10; i++)
                    {
                        lab.Simulate(1f / 60); Assert.That(friend.Action, Is.Not.EqualTo(DuelAction.Dodge));
                        if (blocked)
                        {
                            Assert.That(Mathf.Abs(Mara.position.x), Is.LessThan(.3f));
                            Assert.That(Mathf.Abs(Mara.position.z), Is.LessThan(.3f));
                        }
                    }
                    Assert.That(foe.Action, Is.EqualTo(DuelAction.Recovery));
                    Assert.That(friend.Health, exhausted ? Is.LessThan(100) : Is.EqualTo(100));
                    if (!exhausted) Assert.That(friend.Stamina, Is.LessThan(65), "Blocking spends stamina at impact.");
                }
        }

        [UnityTest] public IEnumerator HulkTellCannotCancelACompanionsCommittedAttack()
        {
            PrepareHulk(1, false, false);
            DuelFighter friend = lab.GetFighter(Mara), foe = lab.GetFighter(Actor("Blightbound Hulk"));
            Assert.That(friend.Attack(true), Is.True);
            var strike = BlightEquipment.Enemy(BlightEnemy.Hulk, 0);
            Assert.That(foe.Attack(strike), Is.True); foe.Advance(strike.Windup - .2f);
            Vector3 start = Mara.position; Advance(.1f);
            Assert.That(friend.Action, Is.EqualTo(DuelAction.Windup)); Assert.That(Mara.position, Is.EqualTo(start));
            yield return null;
        }

        [UnityTest] public IEnumerator UnreachableRecoveryReportsBlockedWithoutTeleporting()
        {
            PrepareHulk(1, true, false); yield return null;
            lab.GetFighter(Actor("Blightbound Hulk")).Interrupt(100);
            lab.GetFighter(Mara).Receive(70, false, false);
            Advance(2);
            Assert.That(lab.CompanionRecovering(Mara), Is.True);
            Assert.That(Mara.position.magnitude, Is.LessThan(.3f));
            Assert.That(lab.GetCompanionIntent(Mara), Does.Contain("blocked"));
        }

        [UnityTest] public IEnumerator RecoveryAndAttackDecisionsReplayDeterministically()
        {
            var reference = new List<string>();
            for (int run = 0; run < 3; run++)
            {
                PrepareSquad(); yield return null;
                Actor("Thrall 1").position = Vector3.forward * 4; lab.GiveOrder(SquadOrder.Assault);
                lab.GetFighter(Mara).Receive(70, false, false); Exhaust(lab.GetFighter(Bren));
                var trace = new List<string>();
                for (int step = 0; step < 480; step++)
                {
                    lab.Simulate(1f / 60);
                    if (step % 15 != 0) continue;
                    foreach (Transform actor in new[] { Mara, Bren })
                    {
                        var fighter = lab.GetFighter(actor);
                        trace.Add(actor.position.ToString("F4") + ":" + fighter.Action + ":" + fighter.AttackSequence + ":" +
                            fighter.Stamina.ToString("F4") + ":" + lab.CompanionRecovering(actor) + ":" + lab.GetCompanionIntent(actor));
                    }
                }
                if (run == 0) reference.AddRange(trace); else CollectionAssert.AreEqual(reference, trace);
            }
        }

        [UnityTest] public IEnumerator WoundedCompanionOnlyTakesAnInRangeRecoveryOpening()
        {
            Transform target = Actor("Thrall 1"); target.position = new Vector3(-2, 0, 1.2f);
            lab.GiveOrder(SquadOrder.Hold);
            DuelFighter friend = lab.GetFighter(Mara), foe = lab.GetFighter(target);
            friend.Receive(70, false, false); Advance(2);
            Assert.That(friend.AttackSequence, Is.Zero);
            Vector3 pocket = Mara.position;
            // Bring the threat into the existing pocket; the companion must not pursue it.
            target.position = pocket + Vector3.forward * 1.7f;
            foe.Advance(1001);
            var strike = BlightEquipment.Enemy(BlightEnemy.Thrall, 0);
            Assert.That(foe.Attack(strike), Is.True); foe.Advance(strike.Windup);
            lab.Simulate(1f / 60);
            Assert.That(friend.Action, Is.EqualTo(DuelAction.Windup));
            Assert.That(friend.Heavy, Is.False); Assert.That(friend.Stamina, Is.EqualTo(84).Within(.01f));
            Assert.That(friend.Health, Is.EqualTo(30)); Assert.That(Mara.position, Is.EqualTo(pocket));
            Assert.That(lab.GetCompanionIntent(Mara), Is.EqualTo("Defensive opening"));
            yield return null;
        }

        [UnityTest] public IEnumerator RecoveryRespectsEachOrderLeashAndTargetDeath()
        {
            foreach (SquadOrder order in new[] { SquadOrder.Follow, SquadOrder.Hold, SquadOrder.Assault })
            {
                PrepareSquad(); yield return null;
                Transform enemy = Actor("Thrall 1"); enemy.position = new Vector3(-2, 0, 1.2f);
                lab.GiveOrder(order); Vector3 anchor = order == SquadOrder.Hold ? Mara.position : Hero.position;
                float leash = order == SquadOrder.Hold ? 3 : order == SquadOrder.Follow ? 7 : 14;
                lab.GetFighter(Mara).Receive(70, false, false);
                for (int step = 0; step < 240; step++)
                {
                    if (step == 120) lab.GetFighter(enemy).Receive(20000, false, false);
                    lab.Simulate(1f / 60);
                    Assert.That(Vector3.Distance(Mara.position, anchor), Is.LessThanOrEqualTo(leash));
                }
                Assert.That(lab.GetCompanionTarget(Mara), Is.Null);
                Assert.That(lab.Order, Is.EqualTo(order)); Assert.That(lab.GetFighter(Mara).Health, Is.EqualTo(30));
            }
        }

        [UnityTest] public IEnumerator WoundedCompanionRejectsAnOpeningThatEndsBeforeItsWindup()
        {
            Transform target = Actor("Thrall 1"); target.position = new Vector3(-2, 0, 1.2f);
            lab.GiveOrder(SquadOrder.Hold);
            DuelFighter friend = lab.GetFighter(Mara), foe = lab.GetFighter(target);
            friend.Receive(70, false, false); Advance(2);
            target.position = Mara.position + Vector3.forward * 1.7f;
            foe.Advance(1001);
            var strike = BlightEquipment.Enemy(BlightEnemy.Thrall, 0);
            Assert.That(foe.Attack(strike), Is.True); foe.Advance(strike.Windup); foe.Advance(strike.Recovery - .02f);
            lab.Simulate(1f / 60);
            Assert.That(friend.AttackSequence, Is.Zero); Assert.That(friend.Action, Is.EqualTo(DuelAction.Ready));
            Assert.That(friend.Stamina, Is.EqualTo(100));
            yield return null;
        }
    }
}
