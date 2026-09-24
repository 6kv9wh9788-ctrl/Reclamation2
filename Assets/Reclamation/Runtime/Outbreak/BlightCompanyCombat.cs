using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        private void ControlCompanySoldier(Actor actor, float dt)
        {
            CommandPlatoon p = company[actor.platoon];
            if (!actor.fighter.CanAct)
            { actor.intent = actor.fighter.Action == DuelAction.Windup ? "Committed strike" : "Evading / recovering"; return; }
            bool retreat = p.phase == CompanyPhase.Returning;
            bool escort = p.order == CompanyOrder.Escort && p.phase != CompanyPhase.Completed && !retreat;
            Vector3 center = escort ? player.root.position - player.root.forward * (2 + actor.platoon) + player.root.right * (actor.platoon - 1) * 2.5f : p.goal;
            Vector3 home = ClampCompanionGoal(center + CompanyFormationOffset(actor));
            actor.anchor = home;
            actor.fighter.Blocking = false;
            Actor incoming = Incoming(actor);
            if (DefendCompanion(actor, incoming, dt, true) || !actor.fighter.CanAct) return;
            actor.defense.recovering = BlightDefenseRules.Recovering(actor.defense.recovering,
                actor.fighter.Health / actor.fighter.MaximumHealth, actor.fighter.Stamina);
            if (retreat)
            {
                actor.target = null; actor.intent = "Returning: " + p.commander.Name;
                MoveCompanion(actor, home, actor.fighter.Blocking ? 2.2f : 3.8f, dt, .3f, incoming == null); return;
            }
            int nearbyEnemies = 0; Actor nearest = null; float distance = float.PositiveInfinity;
            foreach (Actor enemy in actors)
                if (enemy.enemy && VisibleTo(actor, enemy, 8))
                {
                    nearbyEnemies++; float next = Vector3.Distance(actor.root.position, enemy.root.position);
                    if (next < distance) { distance = next; nearest = enemy; }
                }
            float reach = BlightEquipment.Weapon(actor.weapon, false).Reach;
            bool assault = p.order == CompanyOrder.Assault && p.phase != CompanyPhase.Staging && p.phase != CompanyPhase.Completed;
            bool opportunity = p.order == CompanyOrder.Scout && p.phase != CompanyPhase.Completed &&
                p.commander.Risk != CommanderRisk.Cautious && nearbyEnemies == 1 && nearest != null && !nearest.hulk &&
                distance < 4 && actor.fighter.Health >= actor.fighter.MaximumHealth * .7f && actor.fighter.Stamina >= 55 && CompanyLiving(actor.platoon) == CompanyCapacity(actor.platoon);
            bool localDefense = VillageDefense && p.order == CompanyOrder.Defend && nearest != null &&
                distance <= 5 && Vector3.Distance(nearest.root.position,p.goal) <= 6;
            bool canPursue = (assault || opportunity || localDefense) && (!actor.defense.recovering || p.noFail && actor.fighter.Stamina >= 45);
            if (nearest != null && !canPursue && (distance > reach - .1f || Vector3.Distance(nearest.root.position, home) > 4)) nearest = null;
            actor.target = nearest;
            Vector3 goal = home;
            if (canPursue && nearest != null)
            {
                Vector3 axis = actor.root.position - nearest.root.position; axis.y = 0;
                if (axis.sqrMagnitude < .01f) axis = Vector3.back;
                goal = nearest.root.position + axis.normalized * (reach - .35f);
            }
            else if (actor.defense.recovering && nearbyEnemies > 0)
            {
                // Stay close to the platoon rather than retreating alone across the map.
                Vector3 away = nearest != null ? actor.root.position - nearest.root.position : camp - actor.root.position;
                away.y = 0; goal = home + Vector3.ClampMagnitude(away, 2);
            }
            goal = ClampCompanionGoal(goal);
            actor.intent = actor.defense.recovering ? "Recovering near platoon" : opportunity ? "Isolated opportunity" :
                p.phase == CompanyPhase.Staging ? "Staging: " + p.commander.Name : p.order.ToString() + ": " + p.commander.Name;
            bool inStrikeRange = nearest != null && Vector3.Distance(actor.root.position, nearest.root.position) <= reach &&
                TerrainSight(actor.root.position, nearest.root.position);
            if (!inStrikeRange && (nearest != null || Vector3.Distance(actor.root.position, goal) > .5f))
            {
                MoveCompanion(actor, goal, actor.fighter.Blocking ? 2.2f : 3.4f, dt, nearest != null ? .08f : .3f, nearest == null && incoming == null);
                if (nearest != null) Face(actor, nearest.root.position - actor.root.position);
                return;
            }
            if (nearest == null) { if (!actor.fighter.Blocking) Face(actor, Vector3.forward); return; }
            Face(actor, nearest.root.position - actor.root.position);
            if (!DuelFighter.InReach(actor.root.position, actor.root.forward, nearest.root.position, reach, 60) ||
                actor.fighter.Blocking || incoming != null || actor.delay > 0 || actor.fighter.Stamina < BlightEquipment.Weapon(actor.weapon, false).Cost + 25 ||
                actor.defense.recovering && !p.noFail && nearest.fighter.Action != DuelAction.Recovery) return;
            bool heavy = !actor.defense.recovering && actor.attacks % 3 == 2 && actor.fighter.Stamina >= BlightEquipment.Weapon(actor.weapon, true).Cost + 25;
            if (StartAttack(actor, heavy)) { actor.attacks++; actor.delay = 1.1f; actor.intent = "Strike: " + p.commander.Name; }
        }
    }
}
