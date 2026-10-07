using UnityEngine;

namespace Reclamation.Blight
{
    // Opt-in policy for the isolated crowd lab. No production Company/player dependencies.
    public sealed class CrowdSquadCommands
    {
        public enum Command { Move, Hold, Attack }
        public sealed class Squad
        {
            public Vector3 anchor, destination;
            public Command command = Command.Hold;
            public int count;
            public CrowdNavigation.Field route;
        }
        public readonly Squad[] Squads;
        private readonly CrowdBattleSimulation simulation;
        private readonly int[] squadOf, slotOf;
        public CrowdSquadCommands(CrowdBattleSimulation simulation)
        {
            this.simulation = simulation;
            Squads = new Squad[(simulation.FriendlyCount + 31) / 32];
            squadOf = new int[simulation.Units.Length];
            slotOf = new int[squadOf.Length];
            for (int s = 0; s < Squads.Length; s++)
                Squads[s] = new Squad { anchor = new Vector3((s % 4 - 1.5f) * 11, 0, -27 + s / 4 * 9) };
            int friendly = 0, enemy = 0;
            for (int i = 0; i < squadOf.Length; i++)
            {
                var u = simulation.Units[i]; squadOf[i] = -1;
                if (u.team == 0)
                {
                    int s = friendly / 32; squadOf[i] = s; slotOf[i] = friendly++ % 32;
                    Squads[s].count++; u.position = Squads[s].anchor + Offset(slotOf[i]);
                }
                else { u.position = new Vector3((enemy % 40 - 19.5f) * 1.15f, 0, 8 + enemy / 40 * 1.15f); enemy++; }
                u.home = u.position;
            }
            foreach (var s in Squads) s.destination = s.anchor;
        }
        public static Vector3 Offset(int slot) => new Vector3((slot % 8 - 3.5f) * 1.15f, 0, (slot / 8 - 1.5f) * 1.15f);
        public int SquadOf(int unit) => squadOf[unit];
        public Vector3 Slot(int unit) => Squads[squadOf[unit]].anchor + Offset(slotOf[unit]);
        public void Issue(int squad, Command command, Vector3 destination)
        {
            var s = Squads[squad]; s.command = command;
            destination.y = 0; destination.x = Mathf.Clamp(destination.x, -34, 34); destination.z = Mathf.Clamp(destination.z, -34, 34);
            s.destination = command == Command.Hold ? s.anchor : destination;
            if(simulation.Navigation!=null && command!=Command.Hold){s.destination=simulation.Navigation.Snap(s.destination);s.route=simulation.Navigation.Build(s.destination);}
        }
        public void Tick(float dt)
        {
            foreach (var s in Squads)
                if (s.command != Command.Hold)
                {
                    Vector3 next=simulation.Navigation!=null?simulation.Navigation.Waypoint(s.route,s.anchor,s.destination):s.destination;
                    s.anchor=Vector3.MoveTowards(s.anchor,next,1.6f*dt);
                }
        }
        // True consumes movement/attack decisions; committed actions are checked by the caller first.
        public bool Control(int index, float dt)
        {
            var u = simulation.Units[index]; int squad = squadOf[index];
            if (squad < 0) return false;
            var s = Squads[squad];
            bool nearby = u.target >= 0 && simulation.Units[u.target].fighter.Alive &&
                (simulation.TargetPosition(index) - u.position).sqrMagnitude < 1.9f * 1.9f;
            if (s.command == Command.Hold) return !nearby; // Defend in place, never chase.
            if (s.command == Command.Attack && u.target >= 0 && simulation.Units[u.target].fighter.Alive &&
                (simulation.TargetPosition(index) - Slot(index)).sqrMagnitude < 36) return false;
            simulation.MoveTowards(index, Slot(index), dt);
            return true;
        }
        public bool CanInvestigate(int index,Vector3 point)
        {
            int squad=squadOf[index];return squad<0 || (Squads[squad].command==Command.Attack && (point-Slot(index)).sqrMagnitude<36);
        }
        public bool MayChase(int index) => squadOf[index] < 0 || Squads[squadOf[index]].command == Command.Attack;
    }
}
