using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Blight
{
    public enum CompanyOrder { Defend, Scout, Assault, Escort, Withdraw }
    public enum CompanySite { Gate, West, East, Fortress }
    public enum CommanderRisk { Cautious, Balanced, Bold }
    public enum CompanyPhase { Moving, Holding, Scouting, Returning, Staging, Fighting, Completed, Lost }

    public sealed class CompanyCommander
    {
        private readonly HashSet<int> achievements = new HashSet<int>();
        public string Name { get; }
        public CommanderRisk Risk { get; }
        public int Experience { get; private set; }
        public int Judgment => Mathf.Min(5, Experience / 20);
        public float AssessmentInterval => Mathf.Lerp(.75f, .35f, Judgment / 5f);
        public float PressureTolerance => Risk == CommanderRisk.Cautious ? 1.1f : Risk == CommanderRisk.Bold ? 2 : 1.5f;
        public float RetreatHealth => .22f + Judgment * .016f;
        public CompanyCommander(string name, CommanderRisk risk) { Name = name; Risk = risk; }
        public bool Credit(CompanyOrder order, CompanySite site)
        {
            if ((int)order < 0 || (int)order > 4 || (int)site < 0 || (int)site > 3 || order == CompanyOrder.Withdraw) return false;
            if (!achievements.Add((int)order * 4 + (int)site)) return false;
            Experience += 10; return true;
        }
        public bool ShouldWithdraw(float observedThreat, float nearbySupport, float lowestHealth)
            => lowestHealth < RetreatHealth || observedThreat >= 2 && observedThreat > Mathf.Max(1, nearbySupport) * PressureTolerance;
    }

    public sealed class CompanyContact
    {
        public string Name { get; }
        public Vector3 Position { get; private set; }
        public float LastSeen { get; private set; }
        public bool KnownAlive { get; private set; }
        public CompanyContact(string name) { Name = name; }
        public void Observe(Vector3 position, float now, bool alive)
        { Position = position; LastSeen = now; KnownAlive = alive; }
    }
}
