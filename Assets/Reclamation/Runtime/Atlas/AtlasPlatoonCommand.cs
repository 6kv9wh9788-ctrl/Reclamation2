using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Atlas
{
    public enum AtlasPlatoonOrder { Routine, SecureApproach }

    public sealed class AtlasPlatoonCommand
    {
        public int Id { get; }
        public string Name { get; }
        public int LeaderGuardIndex => 1 + Id * 6;
        public AtlasGuardDuty Duty { get; internal set; }
        public AtlasPlatoonOrder Order { get; internal set; }
        public string Report { get; internal set; } = "Watch assigned";
        public int ReportsSent { get; internal set; }
        internal AtlasPlatoonCommand(int id, string name) { Id = id; Name = name; }
    }

    // Company intent delegates to persistent platoon identities; visuals consume their orders.
    public sealed partial class AtlasCompanySchedule
    {
        private readonly AtlasPlatoonCommand[] platoons = new AtlasPlatoonCommand[4];
        private readonly Queue<string> reports = new Queue<string>();
        private int watch = -1;
        public IReadOnlyList<AtlasPlatoonCommand> Platoons => platoons;
        public IEnumerable<string> Reports => reports;
        public int ReportingPlatoon { get; private set; } = -1;
        public int RespondingPlatoon { get; private set; } = -1;
        public bool ReportDelivered { get; private set; }
        public string LatestReport { get; private set; } = "No contact reports";
        private void InitializePlatoons(int seed)
        {
            string[] names = { "Alden", "Bryn", "Cerys", "Darin", "Elian", "Farah", "Gareth", "Hana" };
            for (int i = 0; i < 4; i++) platoons[i] = new AtlasPlatoonCommand(i, names[((seed & int.MaxValue) % 8 + i) % 8]);
        }
        public void UpdateWatch(float hour)
        {
            int next = Mathf.FloorToInt(Mathf.Repeat(hour, 24) / 6);
            if (watch == next) return;
            watch = next;
            foreach (var p in platoons) p.Duty = Duty(hour, p.Id * 6);
        }
        public void BeginContact()
        {
            ReportingPlatoon = RespondingPlatoon = -1; ReportDelivered = false;
            foreach (var p in platoons) { p.Order = AtlasPlatoonOrder.Routine; p.Report = "Watch assigned"; }
        }
        public void ObserveContact(int platoon, bool completingFarmWatch = false)
        {
            if (ReportingPlatoon >= 0 || platoon < 0 || platoon >= 4 || (platoons[platoon].Duty != AtlasGuardDuty.FarmPatrol && !completingFarmWatch)) return;
            // An eyewitness completing the previous farm watch remains a valid source during relief.
            ReportingPlatoon = platoon;
            platoons[platoon].Report = "Contact report en route";
        }
        private void Receive(AtlasPlatoonCommand p, string report)
        {
            p.Report = report; p.ReportsSent++;
            LatestReport = "P" + (p.Id + 1) + " / " + p.Name + ": " + report;
            reports.Enqueue(LatestReport); while (reports.Count > 6) reports.Dequeue();
        }
        public bool DelegateResponse()
        {
            if (ReportingPlatoon < 0 || ReportDelivered) return false;
            ReportDelivered = true;
            Receive(platoons[ReportingPlatoon], "Three scouts at the farm approach");
            foreach (var p in platoons) if (p.Duty == AtlasGuardDuty.Reserve)
            {
                RespondingPlatoon = p.Id; p.Order = AtlasPlatoonOrder.SecureApproach; p.Report = "Acknowledged / moving to approach";
                return true;
            }
            return false;
        }
        public void ReportSupport(int support)
        {
            if (RespondingPlatoon < 0 || support < 3) return;
            var p = platoons[RespondingPlatoon];
            if (p.Report == "Acknowledged / moving to approach") Receive(p, "In position / holding approach");
        }
        public void CompleteContact(bool secured)
        {
            if (RespondingPlatoon >= 0) Receive(platoons[RespondingPlatoon], secured ? "Approach secured / returning to watch" : "Contact lost / returning to watch");
            foreach (var p in platoons) p.Order = AtlasPlatoonOrder.Routine;
        }
    }
}
