using System;
using System.IO;
using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        public string VillageSavePath
        {
            get
            {
                string[] args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "--village-checkpoint");
                return i >= 0 && i + 1 < args.Length ? Path.GetFullPath(args[i + 1]) : Path.Combine(Application.persistentDataPath, "FoundingVillage", "village-v1.json");
            }
        }
        public string VillageSaveStatus { get; private set; } = "Save at the council before quitting. No automatic overwrites.";
        private bool confirmVillageLoad;
        public bool CanSaveVillage
        {
            get
            {
                if (FoundingRaidActive || !AtFoundingCouncil || player.fighter.Stamina < 99.9f) return false;
                foreach (var a in actors)
                {
                    if (a.fighter.Alive && (!a.fighter.CanAct || a.fighter.Stamina < 99.9f)) return false;
                    if (a.enemy && a.fighter.Alive)
                        foreach (var friend in actors) if (!friend.enemy && friend.fighter.Alive && Vector3.Distance(a.root.position, friend.root.position) < 10) return false;
                }
                return true;
            }
        }
        public bool SaveVillage(string path = null)
        {
            if (!CanSaveVillage) { VillageSaveStatus = "Finish any active raid, then return to the council and let everyone rest before saving."; return false; }
            try
            {
                var save = new FoundingSave { version = 1, hasCampaign = Campaign!=null, campaign = Campaign, raidCompleted = FoundingRaidCompleted, village = Village.ToSnapshot(), fighters = new FoundingSave.Fighter[actors.Count], workers = new Vector3[2], carrying = (bool[])workerCarrying.Clone(), gathering = (float[])workerGathering.Clone(), directive = (int)CommanderDirective, scoutReturning = scoutReturning, scoutReport = ScoutReportReceived, report = CommanderReport, clock = patrolClock, simulationTime = simulationTime };
                for (int i = 0; i < actors.Count; i++)
                {
                    var a = actors[i]; save.fighters[i] = new FoundingSave.Fighter { name = a.root.name, health = a.fighter.Health, position = a.root.position, forward = a.root.forward, weapon = (int)a.weapon };
                }
                for (int i = 0; i < 2; i++) save.workers[i] = foundingWorkers[i].position;
                save.Write(path ?? VillageSavePath); VillageSaveStatus = "Village saved. This checkpoint will resume next launch."; return true;
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            { VillageSaveStatus = "Could not save: " + ex.Message; return false; }
        }
        public bool LoadVillage(string path = null)
        {
            if (!Founding) return false;
            FoundingSave save; bool backup;
            try
            {
                save = FoundingSave.Read(path ?? VillageSavePath, out backup);
                // Validate against the actual scenario before changing any live state.
                for (int i = 0; i < save.fighters.Length; i++)
                    if (save.fighters[i].name != actors[i].root.name || !terrain.Clear(save.fighters[i].position, save.fighters[i].position, actors[i].radius)) throw new InvalidDataException("Save roster or position does not match this village.");
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            { VillageSaveStatus = "Load failed: " + ex.Message; return false; }
            SelectScenario(BlightScenario.Founding);
            Village = FoundingVillage.FromSnapshot(save.village); CommanderDirective = (FoundingDirective)save.directive;
            scoutReturning = save.scoutReturning; ScoutReportReceived = save.scoutReport; CommanderReport = save.report;
            Campaign=save.campaign;campaignRaid=false;
            FoundingRaidCompleted = save.raidCompleted; patrolClock = save.clock; simulationTime = save.simulationTime;
            for (int i = 0; i < actors.Count; i++)
            {
                var a = actors[i]; var state = save.fighters[i];
                a.fighter.Receive(100 - state.health, false, false); Equip(a, (BlightWeapon)state.weapon);
                a.root.position = state.position; a.root.rotation = Quaternion.LookRotation(state.forward); Pose(a);
                if(Campaign!=null&&a.enemy&&!a.fighter.Alive)a.root.gameObject.SetActive(false);
            }
            for (int i = 0; i < 2; i++) { foundingWorkers[i].position = save.workers[i]; workerCarrying[i] = save.carrying[i]; workerGathering[i] = save.gathering[i]; workerBaskets[i].gameObject.SetActive(workerCarrying[i]); }
            cacheMarker.gameObject.SetActive(!Village.SuppliesCarried && !Village.SuppliesDelivered);
            RefreshFoundingBuildings(); RefreshMaraAssignments(); ReviewDefense(); ResetCombatCamera();
            VillageSaveStatus = backup ? "Loaded the previous valid backup; the main save was unreadable." : "Village checkpoint loaded.";
            return true;
        }
    }
}
