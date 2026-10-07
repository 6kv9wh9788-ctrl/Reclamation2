using UnityEngine;

namespace Reclamation.Blight
{
    // Round rules for the isolated lab; no production actor or Company changes.
    public sealed class CrowdObjectiveScenario
    {
        public enum State { Running, Secured, Defeated, TimedOut }
        public readonly CrowdBattleSimulation Simulation;
        public readonly Vector3 Center = new Vector3(0, 0, 12);
        public const float Radius = 7, CaptureSeconds = 10, TimeLimit = 180;
        public State Outcome { get; private set; }
        public float Elapsed { get; private set; }
        public float Progress { get; private set; }
        public int FriendlyAlive { get; private set; }
        public int EnemyAlive { get; private set; }
        public int FriendlyInside { get; private set; }
        public int EnemyInside { get; private set; }

        public CrowdObjectiveScenario()
        {
            Simulation = new CrowdBattleSimulation(96, 64) { RespawnEnabled = false };
            Simulation.EnableSquads();
            Simulation.EnableNavigation(CrowdNavigation.Layout.Wall);
            Simulation.EnablePerception();
            Simulation.EnableCommunication();
            for (int s = 0; s < 2; s++)
            {
                var squad = Simulation.Squads.Squads[s];
                squad.anchor = new Vector3(s == 0 ? -12 : 12, 0, -12);
                Simulation.Squads.Issue(s, CrowdSquadCommands.Command.Hold, squad.anchor);
            }
            for (int i = 0; i < Simulation.Units.Length; i++)
            {
                var unit = Simulation.Units[i];
                if (unit.team == 0) unit.position = Simulation.Squads.Slot(i);
                else
                {
                    int n = i - 64;
                    unit.position = n < 16
                        ? new Vector3((n < 8 ? -17 : 17) + (n % 4 - 1.5f) * 1.1f, 0, 5 + (n % 8 / 4) * 1.2f)
                        : Center + new Vector3(((n - 16) % 4 - 1.5f) * 1.2f, 0, ((n - 16) / 4 - 1.5f) * 1.2f);
                }
                unit.home = unit.position;
            }
            Count();
        }
        private void Count()
        {
            FriendlyAlive = EnemyAlive = FriendlyInside = EnemyInside = 0;
            foreach (var unit in Simulation.Units)
            {
                if (!unit.fighter.Alive) continue;
                bool inside = (unit.position - Center).sqrMagnitude <= Radius * Radius;
                if (unit.team == 0) { FriendlyAlive++; if (inside) FriendlyInside++; }
                else { EnemyAlive++; if (inside) EnemyInside++; }
            }
        }
        public void Tick(float dt)
        {
            if (Outcome != State.Running) return;
            Count(); Elapsed += dt;
            if (FriendlyAlive == 0) { Outcome = State.Defeated; return; }
            // Ten consecutive uncontested seconds; entering enemies reset progress.
            Progress = FriendlyInside >= 4 && EnemyInside == 0 ? Progress + dt : 0;
            if (Progress >= CaptureSeconds) { Progress = CaptureSeconds; Outcome = State.Secured; }
            else if (Elapsed >= TimeLimit) Outcome = State.TimedOut;
        }
    }
}
