using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        private readonly List<Actor> foundingRaid = new List<Actor>();
        private float raidCountdown;
        private bool raidPreparing;
        public bool FoundingRaidCompleted { get; private set; }
        public bool FoundingRaidActive => raidPreparing || foundingRaid.Count > 0;
        public int FoundingRaidRemaining
        {
            get { int count = 0; foreach (var a in foundingRaid) if (a.fighter.Alive) count++; return count; }
        }
        public string FoundingRaidStatus => FoundingRaidCompleted ? "Raid repelled" : raidPreparing ? "Raiders arrive in " + Mathf.CeilToInt(raidCountdown) + "s" : foundingRaid.Count > 0 ? "Defend the village - " + FoundingRaidRemaining + " raiders remain" : "Prepare for a north-road raid";
        public bool CanBeginFoundingRaid => Founding && !paused && !FoundingRaidActive && !FoundingRaidCompleted &&
            Village.Storehouse && Village.Watchpost && CanSaveVillage && foundingCommander.fighter.Alive &&
            CommanderDirective == FoundingDirective.DefendVillage && LivingEnemies == 0;
        public bool BeginFoundingRaid()
        {
            if (!CanBeginFoundingRaid || Campaign!=null&&!startingCampaign) return false;
            raidPreparing = true; raidCountdown = 20;
            CommanderReport = "Mara: Raiders are approaching from the north. Twenty seconds to prepare. I will hold the defense plan.";
            return true;
        }
        private void ResetFoundingRaid()
        {
            foundingRaid.Clear(); raidPreparing = false; raidCountdown = 0; FoundingRaidCompleted = false;
        }
        private void UpdateFoundingRaid(float dt)
        {
            TrackDefenseOperation(dt);
            if (raidPreparing)
            {
                raidCountdown = Mathf.Max(0, raidCountdown - dt);
                if (raidCountdown > 0) return;
                raidPreparing = false;
                for (int i = 0; i < 3; i++)
                {
                    var a = CreateActor("Village raider " + (i + 1), new Vector3((i - 1) * 2.5f, 0, 9 + i), true);
                    // Existing enemy combat remains unchanged. Its idle destination/leash is now the village.
                    a.spawn = VillageCenter + Vector3.forward * 3; foundingRaid.Add(a);
                }
                CommanderReport = "Raiders are on the north road. Hold the village or meet them alongside your soldiers.";
            }
            if (foundingRaid.Count == 0 || FoundingRaidRemaining > 0) return;
            // Retain the original checkpoint roster. Raid casualties do not become duplicate saved actors.
            foreach (var a in actors) if (a.target != null && foundingRaid.Contains(a.target)) a.target = null;
            if (selectedEnemy != null && foundingRaid.Contains(selectedEnemy)) selectedEnemy = null;
            foreach (var a in foundingRaid) { actors.Remove(a); a.root.gameObject.SetActive(false); Destroy(a.root.gameObject); }
            foundingRaid.Clear(); FoundingRaidCompleted = true;
            CompleteDefenseOperation();
            if(Campaign==null)CommanderReport = "Raid repelled. " + LivingCompanions + "/8 of Mara's platoon survive. Return to the council and save after everyone rests.";
        }
    }
}
