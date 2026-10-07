using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        private readonly bool[] workerCarrying = new bool[2];
        private readonly float[] workerGathering = new float[2];
        private readonly Transform[] workerBaskets = new Transform[2], workerTools = new Transform[2];
        private int dutyFirst = 1, dutySecond = 2;
        private float nextMaraReview;
        public string MaraDutyStatus { get; private set; }
        public int CivilianFood => Village == null ? 0 : Village.Food;
        private bool DutyPair(Actor a) => a.slot == dutyFirst || a.slot == dutySecond;
        private void BuildVillageLife()
        {
            confirmVillageLoad = false; VillageSaveStatus = "Unsaved session. Save at the council before quitting."; dutyFirst = 1; dutySecond = 2; nextMaraReview = 0;
            for (int i = 0; i < 2; i++)
            {
                workerCarrying[i] = false; workerGathering[i] = 0;
                foundingWorkers[i].name = i == 0 ? "Elin - builder and provisioner" : "Oren - builder and provisioner";
                workerBaskets[i] = Part(foundingWorkers[i], "Supply basket", new Vector3(0, 1, .4f), new Vector3(.48f, .32f, .3f), Material(new Color(.43f, .25f, .1f)));
                workerBaskets[i].gameObject.SetActive(false);
                workerTools[i] = Pivot(foundingWorkers[i], "Work tool", new Vector3(.4f, 1.2f, .2f));
                Part(workerTools[i], "Hammer handle", Vector3.up * .12f, new Vector3(.07f, .4f, .07f), Material(new Color(.3f, .2f, .12f)));
                Part(workerTools[i], "Hammer head", Vector3.up * .32f, new Vector3(.26f, .12f, .13f), Material(Color.gray));
                workerTools[i].gameObject.SetActive(false);
                for (int side = -1; side <= 1; side += 2)
                    Part(foundingWorkers[i], "Work arm", new Vector3(side * .3f, 1.25f, .15f), new Vector3(.17f, .5f, .18f), Material(new Color(.65f, .49f, .35f)));
            }
            Part(foundingScenery, "Provision garden", new Vector3(-5.5f, .03f, -4.5f), new Vector3(3, .06f, 3), Material(new Color(.24f, .4f, .19f)));
            RefreshMaraAssignments();
        }
        private void RefreshMaraAssignments()
        {
            dutyFirst = dutySecond = -1;
            int wounded = 0;
            foreach (var soldier in foundingSoldiers)
            {
                if (!soldier.fighter.Alive) continue;
                if (soldier.fighter.Health <= SoldierReliefHealth) { wounded++; continue; }
                if (soldier == foundingCommander) continue;
                if (dutyFirst < 0) dutyFirst = soldier.slot; else if (dutySecond < 0) dutySecond = soldier.slot;
            }
            MaraDutyStatus = !foundingCommander.fighter.Alive ? "Command vacant: survivors hold village posts." :
                "Mara | Cautious veteran | " + CommanderDirective + " | " + (dutyFirst < 0 ? 0 : dutySecond < 0 ? 1 : 2) + " fit for detached duty | " + wounded + " wounded in reserve";
        }
        private void UpdateVillageLife(float dt)
        {
            if (simulationTime >= nextMaraReview) { RefreshMaraAssignments(); nextMaraReview = simulationTime + 2; }
            for (int i = 0; i < 2; i++)
            {
                Vector3 worksite = new Vector3(6.5f, 0, -7 + i * 1.3f);
                Vector3 home = new Vector3(-4 - i * 1.2f, 0, -14);
                Vector3 field = new Vector3(-5.5f + i * 1.4f, 0, -4.5f);
                bool safe = true;
                foreach (var a in actors) if (a.enemy && a.fighter.Alive && Vector3.Distance(a.root.position, foundingWorkers[i].position) < 10) safe = false;
                Vector3 destination = Village.BuildingStorehouse ? worksite : Village.Storehouse && Village.Food < 20 && safe ? workerCarrying[i] ? worksite : field : home;
                Vector3 delta = destination - foundingWorkers[i].position;
                if (delta.sqrMagnitude > .05f) foundingWorkers[i].rotation = Quaternion.LookRotation(delta);
                foundingWorkers[i].position = Vector3.MoveTowards(foundingWorkers[i].position, destination, 2 * dt);
                bool arrived = Vector3.Distance(foundingWorkers[i].position, destination) < .2f;
                workerTools[i].gameObject.SetActive(Village.BuildingStorehouse && arrived);
                workerTools[i].localRotation = Quaternion.Euler(Mathf.Sin(simulationTime * 7 + i) * 35, 0, 0);
                if (Village.Storehouse && Village.Food < 20 && safe && arrived)
                {
                    if (workerCarrying[i]) { if (Village.DeliverFood()) workerCarrying[i] = false; }
                    else { workerGathering[i] += dt; if (workerGathering[i] >= 3) { workerGathering[i] = 0; workerCarrying[i] = true; } }
                }
                workerBaskets[i].gameObject.SetActive(workerCarrying[i]);
            }
        }
        private void RefreshFoundingBuildings()
        {
            foundingRoof.gameObject.SetActive(Village.Storehouse); foundingPlatform.gameObject.SetActive(Village.Watchpost);
            if (Village.Storehouse || Village.BuildingStorehouse)
            { float height = Village.Storehouse ? 3.2f : .24f + 2.96f * Village.BuildSeconds / 20; foundingStorehouse.localPosition = new Vector3(10, height / 2, -7); foundingStorehouse.localScale = new Vector3(4, height, 4); }
            if (Village.Watchpost) { foundingWatchpost.localPosition = new Vector3(8, 2, 0); foundingWatchpost.localScale = new Vector3(2, 4, 2); }
        }
    }
}
