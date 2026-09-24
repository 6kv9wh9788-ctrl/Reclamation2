using UnityEngine;

namespace Reclamation.Blight
{
    public enum BlightScenario { Duel, Squad, Hulk, Weapons, Patrol, LimbDamage, Outpost, Skirmish, Horde, Gateway, Company }
    public enum SquadOrder { Follow, Hold, Assault, Withdraw }
    public enum PatrolStage { Outbound, CacheAvailable, Returning, Complete }

    public sealed class BlightPatrol
    {
        public PatrolStage Stage { get; private set; }
        public bool SpearRecovered { get; private set; }
        public void ObserveEnemies(int alive)
        {
            if (alive == 0 && Stage == PatrolStage.Outbound) Stage = PatrolStage.CacheAvailable;
        }
        public bool TryRecoverCache(float distance, bool playerAlive)
        {
            if (Stage != PatrolStage.CacheAvailable || !playerAlive ||
                !(distance >= 0 && distance <= 2.6f)) return false;
            SpearRecovered = true; Stage = PatrolStage.Returning; return true;
        }
        public bool TryComplete(float campDistance, bool threatened, bool playerAlive)
        {
            if (Stage != PatrolStage.Returning || !playerAlive || threatened ||
                !(campDistance >= 0 && campDistance <= 4)) return false;
            Stage = PatrolStage.Complete; return true;
        }
        public static bool CanRecover(float campDistance, bool threatened)
            => campDistance >= 0 && campDistance <= 4 && !threatened;
    }

    public static class BlightSquadRules
    {
        public static bool CanEngage(SquadOrder order, float distanceFromAnchor)
        {
            if (order == SquadOrder.Withdraw) return false;
            return distanceFromAnchor >= 0 && distanceFromAnchor <=
                (order == SquadOrder.Hold ? 3 : order == SquadOrder.Assault ? 14 : 7);
        }
        public static Vector3 Formation(Vector3 anchor, Vector3 forward, int slot)
        {
            forward.y = 0; forward = forward.normalized;
            if (forward.sqrMagnitude < 0.1f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            return anchor - forward * 1.8f + right * (slot == 0 ? -1.8f : 1.8f);
        }
    }
}
