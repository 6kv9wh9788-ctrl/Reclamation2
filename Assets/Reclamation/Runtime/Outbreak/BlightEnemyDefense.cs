using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        public int ActiveEnemyAttackLimit => VillageDefense ? 8 : (Scenario == BlightScenario.Horde || Scenario == BlightScenario.Gateway) ? 4 :
            Scenario == BlightScenario.Skirmish ? 3 : maximumEnemyAttackers;

        private void BuildLargerEncounter()
        {
            int count = (Scenario == BlightScenario.Horde || Scenario == BlightScenario.Gateway) ? 12 : 6;
            for (int i = 0; i < count; i++)
                CreateActor(i == count - 1 ? "Formation Hulk" : "Formation thrall " + (i + 1),
                    new Vector3(-6 + (i % 4) * 4, 0, 6 + (i / 4) * 4), true, i == count - 1);
        }

        private bool ControlEnemyDefense(Actor actor, float dt)
        {
            if (actor.limbs != null) return false; // Keep the contact/dismemberment lab reproducible.
            Actor threat = null;
            foreach (Actor human in actors)
            {
                if (human.enemy || !human.fighter.Alive || human.fighter.Action != DuelAction.Windup || !human.fighter.Heavy) continue;
                if (!TerrainSight(human.root.position, actor.root.position)) continue;
                AttackSpec strike = human.fighter.Strike;
                if (!DuelFighter.InReach(human.root.position, human.root.forward, actor.root.position, strike.Reach + .6f, strike.HalfAngle + 5)) continue;
                if (Vector3.Angle(actor.root.forward, human.root.position - actor.root.position) > 100) continue;
                if (threat == null || human.fighter.Remaining < threat.fighter.Remaining) threat = human;
            }
            if (threat == null)
            {
                actor.fighter.Blocking = false; actor.observedThreat = null; actor.reactionTime = 0;
                return false;
            }
            // A raised guard persists only for the observed attack, not the cooldown.
            if (actor.fighter.Blocking && actor.observedThreat == threat && actor.observedAttack == threat.fighter.AttackSequence)
            { Face(actor, threat.root.position - actor.root.position); return true; }
            actor.fighter.Blocking = false;
            if (actor.defenseCooldown > 0) return false;
            if (actor.observedThreat != threat || actor.observedAttack != threat.fighter.AttackSequence)
            {
                actor.observedThreat = threat; actor.observedAttack = threat.fighter.AttackSequence; actor.reactionTime = 0;
            }
            // Deterministic variation: ordinary thralls attempt one in three heavy tells.
            // Uses attacks already visible in the world, never pending player inputs.
            if ((actor.defenseSeed + threat.fighter.AttackSequence) % (actor.hulk ? 2 : 3) != 0) return false;
            actor.reactionTime += dt;
            float delay = .22f + (actor.defenseSeed % 3) * .04f;
            if (actor.reactionTime < delay) return true;
            Face(actor, threat.root.position - actor.root.position);
            if (actor.hulk)
            {
                if (actor.fighter.Stamina < 42) return false;
                actor.fighter.Blocking = true; actor.defenseCooldown = 3.5f; return true;
            }
            if (threat.fighter.Remaining > .25f) return true;
            Vector3 side = Vector3.Cross(Vector3.up, actor.root.position - threat.root.position).normalized;
            if (actor.defenseSeed % 2 == 0) side = -side;
            if (actor.fighter.Stamina >= 25 && TryEscapeDirection(actor, threat, side, false, out Vector3 escape) && actor.fighter.Dodge())
            {
                actor.dodgeDirection = escape; actor.defenseCooldown = 4; return true;
            }
            actor.defenseCooldown = 1; return false;
        }

        private bool RepositionWaitingEnemy(Actor actor, Actor target, AttackSpec spec, float dt)
        {
            if (Scenario != BlightScenario.Skirmish && Scenario != BlightScenario.Horde && Scenario != BlightScenario.Gateway) return false;
            if (EnemyAttackSlots() < ActiveEnemyAttackLimit) return false;
            Vector3 axis = target.root.position - actor.spawn; axis.y = 0;
            if (axis.sqrMagnitude < .1f) axis = Vector3.back;
            float angle = (actor.defenseSeed % 5 - 2) * 32;
            Vector3 station = target.root.position - Quaternion.Euler(0, angle, 0) * axis.normalized * (spec.Reach + 1.3f);
            MoveToward(actor, station, actor.hulk ? 1.8f : 2.2f, dt, .4f);
            Face(actor, target.root.position - actor.root.position); return true;
        }
    }
}
