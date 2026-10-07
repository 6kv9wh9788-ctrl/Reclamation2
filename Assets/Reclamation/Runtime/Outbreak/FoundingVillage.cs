using System;

namespace Reclamation.Blight
{
    // Checkpointable vertical slice: civilian development never upgrades military facilities.
    public sealed class FoundingVillage
    {
        public int Food { get; private set; }
        public bool DeliverFood() { if (!Storehouse || Food >= 20) return false; Food++; return true; }
        public int RemoveFood(int requested) { int removed=Math.Min(Food,Math.Max(0,requested)); Food-=removed;return removed; }
        [Serializable] public sealed class Snapshot
        {
            public bool carried, delivered, workCrews, patrolDoctrine, building, storehouse, watchpost;
            public int timber, insight, research, food;
            public float researchSeconds, buildSeconds;
        }
        public Snapshot ToSnapshot() => new Snapshot { carried=SuppliesCarried, delivered=SuppliesDelivered, timber=Timber, insight=Insight, workCrews=WorkCrews, patrolDoctrine=PatrolDoctrine, research=(int)Research, researchSeconds=ResearchSeconds, building=BuildingStorehouse, buildSeconds=BuildSeconds, storehouse=Storehouse, watchpost=Watchpost, food=Food };
        public static FoundingVillage FromSnapshot(Snapshot s)
        {
            if (s == null || s.research < 0 || s.research > 2 || float.IsNaN(s.researchSeconds) || s.researchSeconds < 0 || s.researchSeconds > 15 || float.IsNaN(s.buildSeconds) || s.buildSeconds < 0 || s.buildSeconds > 20 || s.food < 0 || s.food > 20 || s.carried && s.delivered || (s.storehouse || s.building) && !s.workCrews || s.storehouse && s.building || s.watchpost && !s.patrolDoctrine || s.food > 0 && !s.storehouse || s.storehouse && s.buildSeconds != 20 || s.building && s.buildSeconds >= 20 || s.research == 1 && s.workCrews || s.research == 2 && s.patrolDoctrine)
                throw new System.IO.InvalidDataException("Invalid village progress.");
            int spent = (s.workCrews ? 1 : 0) + (s.patrolDoctrine ? 1 : 0) + (s.research != 0 ? 1 : 0);
            if (s.timber != (s.delivered ? 6 : 0) - (s.building || s.storehouse ? 4 : 0) - (s.watchpost ? 2 : 0) || s.insight != (s.delivered ? 2 : 0) - spent || s.timber < 0 || s.insight < 0)
                throw new System.IO.InvalidDataException("Invalid village resource ledger.");
            return new FoundingVillage { SuppliesCarried=s.carried, SuppliesDelivered=s.delivered, Timber=s.timber, Insight=s.insight, WorkCrews=s.workCrews, PatrolDoctrine=s.patrolDoctrine, Research=(Technology)s.research, ResearchSeconds=s.researchSeconds, BuildingStorehouse=s.building, BuildSeconds=s.buildSeconds, Storehouse=s.storehouse, Watchpost=s.watchpost, Food=s.food };
        }
        public enum Technology { None, WorkCrews, PatrolDoctrine }
        public bool SuppliesCarried { get; private set; }
        public bool SuppliesDelivered { get; private set; }
        public int Timber { get; private set; }
        public int Insight { get; private set; }
        public bool WorkCrews { get; private set; }
        public bool PatrolDoctrine { get; private set; }
        public Technology Research { get; private set; }
        public float ResearchSeconds { get; private set; }
        public bool BuildingStorehouse { get; private set; }
        public float BuildSeconds { get; private set; }
        public bool Storehouse { get; private set; }
        public bool Watchpost { get; private set; }
        public string CivilianStage => "Village";
        public string MilitaryStage => Watchpost ? "Outpost" : "No permanent outpost";
        public bool RecoverSupplies()
        {
            if (SuppliesCarried || SuppliesDelivered) return false;
            SuppliesCarried = true; return true;
        }
        public bool DeliverSupplies()
        {
            if (!SuppliesCarried || SuppliesDelivered) return false;
            SuppliesCarried = false; SuppliesDelivered = true; Timber += 6; Insight += 2; return true;
        }
        public bool StartResearch(Technology technology)
        {
            if (technology != Technology.WorkCrews && technology != Technology.PatrolDoctrine) return false;
            if (Research != Technology.None || Insight < 1 || (technology == Technology.WorkCrews ? WorkCrews : PatrolDoctrine)) return false;
            Insight--; Research = technology; ResearchSeconds = 0; return true;
        }
        public bool StartStorehouse()
        {
            if (!WorkCrews || Timber < 4 || Storehouse || BuildingStorehouse) return false;
            Timber -= 4; BuildingStorehouse = true; BuildSeconds = 0; return true;
        }
        public bool BuildWatchpost()
        {
            if (!PatrolDoctrine || Timber < 2 || Watchpost) return false;
            Timber -= 2; Watchpost = true; return true;
        }
        public void Tick(float dt)
        {
            if (!(dt > 0) || float.IsInfinity(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            if (Research != Technology.None)
            {
                ResearchSeconds += dt;
                if (ResearchSeconds >= 15)
                {
                    if (Research == Technology.WorkCrews) WorkCrews = true; else PatrolDoctrine = true;
                    Research = Technology.None; ResearchSeconds = 15;
                }
            }
            if (BuildingStorehouse)
            {
                BuildSeconds += dt;
                if (BuildSeconds >= 20) { Storehouse = true; BuildingStorehouse = false; BuildSeconds = 20; }
            }
        }
    }
}
