using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        private readonly List<Actor> defenseFit = new List<Actor>();
        private float alertUntil, nextDefenseReview;
        private Vector3 lastDefenseContact;
        private bool defenseAlert;
        public bool DefenseAlert => defenseAlert;
        public int DefenseGarrison { get; private set; }
        public int DefensePatrol { get; private set; }
        public int DefenseReserve { get; private set; }
        public string DefenseStatus { get; private set; }
        public string DefenseReason { get; private set; }
        private readonly List<string> defenseJournal = new List<string>();
        private string lastVillageReport;
        private float reportUntil;
        private void ResetDefense()
        {
            alertUntil = nextDefenseReview = 0; defenseAlert = false;
            defenseJournal.Clear(); lastVillageReport = null; villagePanel = 0; villageDebug = false;
            ReviewDefense();
        }
        private void ReviewDefense()
        {
            defenseFit.Clear();
            foreach (var a in foundingSoldiers)
                if (a != foundingCommander && a.fighter.Alive && a.fighter.Health > SoldierReliefHealth) defenseFit.Add(a);
            // A duty rotation is derived from the saved clock; loading does not reroll assignments.
            int rotate = defenseFit.Count == 0 ? 0 : ((int)(patrolClock / 60) * 2) % defenseFit.Count;
            for (int i = 0; i < rotate; i++) { var first = defenseFit[0]; defenseFit.RemoveAt(0); defenseFit.Add(first); }
            bool delegated = CommanderDirective == FoundingDirective.DefendVillage && foundingCommander.fighter.Alive;
            bool wasAlert = defenseAlert;
            if (delegated)
                foreach (var enemy in actors)
                {
                    if (!enemy.enemy || !enemy.fighter.Alive || (Vector3.Distance(enemy.root.position, VillageCenter) > 16 && (!Village.Watchpost || Vector3.Distance(enemy.root.position, new Vector3(8, 0, 0)) > 10))) continue;
                    foreach (var observer in foundingSoldiers)
                        if (observer.fighter.Alive && Vector3.Distance(observer.root.position, enemy.root.position) < 11 && TerrainSight(observer.root.position, enemy.root.position))
                        { lastDefenseContact = enemy.root.position; alertUntil = patrolClock + 8; break; }
                }
            defenseAlert = delegated && patrolClock < alertUntil;
            DefenseGarrison = delegated ? Mathf.Min(Campaign!=null&&Campaign.commanderPerk==(int)CaptainPerk.WatchCaptain?3:2, defenseFit.Count) : 0;
            DefensePatrol = delegated && !campaignRaid && !defenseAlert && Village.PatrolDoctrine && defenseFit.Count >= 5 ? 2 : 0;
            DefenseReserve = delegated ? defenseFit.Count - DefenseGarrison - DefensePatrol : 0;
            DefenseStatus = !foundingCommander.fighter.Alive ? "COMMAND VACANT" : !delegated ? "DETACHED MISSION" : defenseAlert ? "THREAT OBSERVED" : defenseFit.Count < 5 ? "UNDERSTAFFED" : "WATCH ACTIVE";
            DefenseReason = !delegated ? "Town defense resumes when you give Mara the Defend town objective." : defenseAlert ? "Patrol recalled. Garrison holds the approaches; reserve responds to the last observed contact." : defenseFit.Count < 5 ? "Too few fit soldiers for a patrol and reserve. Keep the approaches covered first." : !Village.PatrolDoctrine ? "Garrison and reserve established. Patrol doctrine unlocks scheduled patrols." : "Two on fixed posts, two patrolling, the remainder in reserve. Duties rotate every 60 seconds.";
            if(Campaign!=null)DefenseReason="Fixed posts: "+DefenseGarrison+". "+(campaignRaid?"Reserve executing "+((DefenseApproach)Campaign.approach)+". ":"Routine watch. ")+"Soldiers at or below "+SoldierReliefHealth+" health recover in reserve.";
            if (defenseAlert != wasAlert) CommanderReport = defenseAlert ? "Mara: Threat observed. Recalling patrol; reserve responding. Garrison holds." : "Mara: No recent contact. Resuming the defense schedule.";
        }
        public string DefenseDuty(Transform root)
        {
            foreach (var a in foundingSoldiers) if (a.root == root) return DefenseDuty(a);
            return "Unknown";
        }
        private string DefenseDuty(Actor a)
        {
            if (!a.fighter.Alive) return "Fallen";
            if (a.fighter.Health <= SoldierReliefHealth) return "Wounded reserve";
            if (a == foundingCommander) return "Command / inspection";
            if (!foundingCommander.fighter.Alive) return "Hold post";
            if (CommanderDirective != FoundingDirective.DefendVillage) return DutyPair(a) ? "Detached duty" : "Support";
            int rank = defenseFit.IndexOf(a);
            return rank < 0 ? "Reserve" : rank < DefenseGarrison ? "Garrison" : rank < DefenseGarrison + DefensePatrol ? "Patrol" : defenseAlert ? "Responding" : "Reserve";
        }
        private Vector3 DelegatedDefenseGoal(Actor a)
        {
            int rank = defenseFit.IndexOf(a);
            if(CampaignGoal(a,rank,out var missionGoal))return missionGoal;
            if (a == foundingCommander) return defenseAlert ? VillageCenter + Vector3.forward * 5 : VillageCenter + new Vector3(Mathf.Sin(patrolClock * .15f) * 3, 0, 3);
            if (rank < DefenseGarrison) return Village.Watchpost ? new Vector3(5.5f, 0, -rank*3) : new Vector3(rank == 0 ? -3 : 3, 0, -8);
            if (rank < DefenseGarrison + DefensePatrol)
                return new Vector3(Mathf.Sin(patrolClock * .12f + (rank % 2) * Mathf.PI) * 6, 0, -7 + Mathf.Cos(patrolClock * .12f) * 3);
            if (defenseAlert) return lastDefenseContact + Vector3.right * ((rank % 3 - 1) * 1.4f);
            return VillageCenter + new Vector3((rank % 3 - 1) * 1.7f, 0, -3);
        }
        private void UpdateDefense()
        {
            if (patrolClock >= nextDefenseReview) { ReviewDefense(); nextDefenseReview = patrolClock + .5f; }
            if (CommanderReport != lastVillageReport)
            {
                lastVillageReport = CommanderReport; reportUntil = patrolClock + 7;
                defenseJournal.Insert(0, patrolClock.ToString("0") + "s  " + CommanderReport);
                if (defenseJournal.Count > 5) defenseJournal.RemoveAt(5);
            }
        }
    }
}
