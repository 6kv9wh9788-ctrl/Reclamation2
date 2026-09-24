using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        private sealed class CommandPlatoon
        {
            public CompanyCommander commander;
            public CompanyOrder order;
            public CompanySite site;
            public CompanyPhase phase;
            public readonly List<Actor> members = new List<Actor>();
            public Vector3 goal;
            public float nextThink, pressure, serviceTime;
            public bool objectiveVisited, assigned, noFail;
            public string reason = "Defending camp";
        }
        private readonly List<CommandPlatoon> company = new List<CommandPlatoon>();
        private readonly List<CompanyContact> companyContacts = new List<CompanyContact>();
        private readonly List<string> companyJournal = new List<string>();
        private Transform companyScenery, companySelectionMarker;
        private int selectedPlatoon;
        private CompanySite selectedCompanySite = CompanySite.Gate;
        private bool companyPlan, companyReportOpen;
        private string companyReport = "";
        public int KnownHostileCount { get { int n = 0; foreach (CompanyContact c in companyContacts) if (c.KnownAlive) n++; return n; } }
        public int CompanyPlatoonCount => company.Count;
        public bool CompanyPlanActive => companyPlan;
        public bool CompanyReportOpen => companyReportOpen;
        public string CompanyReport => companyReport;
        public CompanyContact GetCompanyContact(string name) => companyContacts.Find(c => c.Name == name);
        public CompanyCommander GetCommander(int index) => index >= 0 && index < company.Count ? company[index].commander : null;
        public CompanyOrder GetCompanyOrder(int index) => company[index].order;
        public CompanyPhase GetCompanyPhase(int index) => company[index].phase;
        public bool CompanyNoFail(int index) => company[index].noFail;
        public string GetCompanyReason(int index) => company[index].reason;
        public int CompanyLiving(int index) { int n = 0; foreach (Actor a in company[index].members) if (a.fighter.Alive) n++; return n; }
        public static Vector3 CompanyDestination(CompanySite site)
            => site == CompanySite.West ? new Vector3(-11, 0, 5) : site == CompanySite.East ? new Vector3(11, 0, 5) :
                site == CompanySite.Fortress ? new Vector3(0, 0, 13) : new Vector3(0, 0, -7);

        private void ClearCompany()
        {
            ClearCommandMap(); ClearCompanyFog(); ClearCompanyField(); ClearVillageDefense(); companyDetails = false;
            if (companyScenery != null) { companyScenery.gameObject.SetActive(false); Destroy(companyScenery.gameObject); }
            companyScenery = companySelectionMarker = null; company.Clear(); companyContacts.Clear(); companyJournal.Clear();
            companyPlan = companyReportOpen = false; selectedPlatoon = 0; selectedCompanySite = CompanySite.Gate; companyReport = "";
        }
        private void BuildCompany()
        {
            companyScenery = Pivot(transform, "Company operation", Vector3.zero);
            companySelectionMarker = Marker("Selected platoon", new Color(.2f, .75f, .95f), 2.2f);
            companySelectionMarker.SetParent(companyScenery, true);
            string[] leaders = { "Mara", "Ivo", "Tess" };
            for (int i = 0; i < 3; i++)
                company.Add(new CommandPlatoon { commander = new CompanyCommander(leaders[i], (CommanderRisk)i),
                    order = CompanyOrder.Defend, site = CompanySite.Gate, phase = CompanyPhase.Holding,
                    goal = camp + new Vector3((i - 1) * 4, 0, 2) });
            foreach (Actor a in actors)
                if (!a.enemy && a != player) { a.platoon = 0; company[0].members.Add(a); }
            string[] names = { "Ivo - swordsman", "Nessa - spearfighter", "Tess - swordswoman", "Oren - spearman" };
            for (int i = 0; i < 4; i++)
            {
                int group = 1 + i / 2, slot = i % 2;
                Actor a = CreateActor(names[i], camp + new Vector3((group - 1) * 4 + (slot == 0 ? -.8f : .8f), 0, -1), false);
                a.platoon = group; a.slot = slot; Equip(a, slot == 0 ? BlightWeapon.Sword : BlightWeapon.Spear);
                InstallHumanVisual(a, (i == 0 || i == 3) ? BlightHumanLook.Bren : BlightHumanLook.Mara);
                company[group].members.Add(a);
            }
            if (VillageDefense) { BuildVillageDefense(); return; }
            CreateActor("West roaming thrall", new Vector3(-10, 0, 11) * CompanyScale, true);
            CreateActor("East roaming thrall", new Vector3(10, 0, 11) * CompanyScale, true);
            CreateActor("Fortress defender west", new Vector3(-3, 0, 13) * CompanyScale, true);
            CreateActor("Fortress defender east", new Vector3(3, 0, 13) * CompanyScale, true);
            CreateActor("Fortress Hulk", new Vector3(0, 0, 16) * CompanyScale, true, true);
            foreach (Rect unscaledWall in new[] { new Rect(-9, 8.4f, 7, 1.2f), new Rect(2, 8.4f, 7, 1.2f) })
            {
                Rect wall = new Rect(unscaledWall.position * CompanyScale, unscaledWall.size * CompanyScale);
                terrain.Add(wall);
                Part(companyScenery, "Fortress wall", new Vector3(wall.center.x, 1.2f, wall.center.y),
                    new Vector3(wall.width, 2.4f, wall.height), Material(new Color(.31f, .29f, .34f)));
            }
            for (int i = 0; i < 4; i++)
            {
                Transform marker = Marker(((CompanySite)i).ToString(), new Color(.45f, .55f, .7f), 1.6f);
                marker.SetParent(companyScenery, true); marker.position = CompanySitePosition((CompanySite)i);
            }
            if (ExpandedCompany) BuildCompanyField();
            companySelectionMarker.position = CompanyCenter(company[0]);
            CompanyLog("Ready: choose platoon, location and mission. Scout first.");
        }
        private void CompanyLog(string message)
        {
            companyJournal.Add(Mathf.FloorToInt(simulationTime / 60) + ":" + Mathf.FloorToInt(simulationTime % 60).ToString("00") + "  " + message);
            if (companyJournal.Count > 8) companyJournal.RemoveAt(0);
        }
        public bool GiveCompanyOrder(int index, CompanyOrder order, CompanySite site, bool noFail = false)
        {
            if (Scenario != BlightScenario.Company || paused || player == null || !player.fighter.Alive ||
                index < 0 || index >= company.Count || (int)order < 0 || (int)order > 4 || (int)site < 0 || (int)site > 3 || CompanyLiving(index) == 0) return false;
            if (companyPlan && index < 2) CancelCompanyPlan("Coordinated plan cancelled by a new order.");
            AssignCompanyOrder(index, order, site, noFail);
            CompanyLog(company[index].commander.Name + ": " + order + " " + (order == CompanyOrder.Withdraw ? (VillageDefense ? "to square" : "to camp") : CompanySiteName(site)));
            return true;
        }
        private void AssignCompanyOrder(int index, CompanyOrder order, CompanySite site, bool noFail = false)
        {
            CommandPlatoon p = company[index]; p.order = order; p.site = site;
            p.noFail = order == CompanyOrder.Assault && noFail;
            p.phase = order == CompanyOrder.Withdraw ? CompanyPhase.Returning : CompanyPhase.Moving;
            p.goal = order == CompanyOrder.Withdraw ? CompanyCampSlot(index) : CompanySitePosition(site);
            p.reason = order == CompanyOrder.Scout ? "Survey and return; avoid unfavorable combat" : order.ToString();
            p.objectiveVisited = false; p.assigned = true; p.nextThink = p.pressure = p.serviceTime = 0;
            foreach (Actor a in p.members) { a.target = null; a.routeUntil = 0; a.navigationStall = 0; a.defense = new DefenseMemory(); }
        }
        private Vector3 CompanyCampSlot(int index) => camp + new Vector3((index - 1) * (VillageDefense ? 8 : 3), 0, 0);
        public void RecallCompany()
        {
            if (Scenario != BlightScenario.Company || paused || player == null || !player.fighter.Alive) return;
            CancelCompanyPlan("Company recalled.");
            for (int i = 0; i < company.Count; i++) if (CompanyLiving(i) > 0) GiveCompanyOrder(i, CompanyOrder.Withdraw, CompanySite.Gate);
        }
        public bool PlanCompanyAssault(bool noFail = false)
        {
            if (VillageDefense) return DeployVillageDefense();
            if (Scenario != BlightScenario.Company || paused || !player.fighter.Alive || CompanyLiving(0) == 0 || CompanyLiving(1) == 0) return false;
            AssignCompanyOrder(0, CompanyOrder.Assault, CompanySite.Fortress, noFail);
            AssignCompanyOrder(1, CompanyOrder.Assault, CompanySite.Fortress, noFail);
            company[0].phase = company[1].phase = CompanyPhase.Staging;
            company[0].goal = CompanyAssaultRally(0); company[1].goal = CompanyAssaultRally(1);
            company[0].reason = "Main approach: wait for flank"; company[1].reason = "West flank: moving into position";
            if (CompanyLiving(2) > 0) AssignCompanyOrder(2, CompanyOrder.Defend, CompanySite.Gate);
            companyPlan = true; CompanyLog("Plan: Mara main / Ivo west flank / Tess reserve. Main waits. " + (noFail ? "NO FAIL: automatic withdrawal disabled." : "Discretionary withdrawal enabled.")); return true;
        }
        private void HandleCompanyPlatoonLost()
        {
            if (!companyPlan) return;
            bool committed = company[0].noFail || company[1].noFail;
            if (!committed) { CancelCompanyPlan("Assault coordination lost: a platoon has fallen."); return; }
            companyPlan = false;
            for (int i = 0; i < 2; i++) if (CompanyLiving(i) > 0)
            {
                company[i].phase = CompanyPhase.Fighting;
                company[i].goal = CompanySitePosition(CompanySite.Fortress);
                company[i].reason = "NO FAIL: partner lost; continuing assault";
            }
            CompanyLog("NO FAIL assault continues after platoon loss. Recall remains available.");
        }
        private void CancelCompanyPlan(string reason)
        {
            if (!companyPlan) return;
            companyPlan = false;
            // Never release a waiting main force into a lone assault when its partner aborts.
            for (int i = 0; i < 2; i++)
                if (company[i].phase == CompanyPhase.Staging)
                { company[i].noFail = false; company[i].order = CompanyOrder.Defend; company[i].phase = CompanyPhase.Holding; company[i].reason = "Plan cancelled: holding rally"; }
            CompanyLog(reason);
        }
        private bool CompanyAt(CommandPlatoon p, Vector3 point, float range)
        {
            bool any = false;
            foreach (Actor a in p.members) if (a.fighter.Alive) { any = true; Vector3 expected = VillageDefense && p.order == CompanyOrder.Defend ? point + CompanyFormationOffset(a) : point; if (Vector3.Distance(a.root.position, expected) > range + (VillageDefense && p.order != CompanyOrder.Defend ? 1.3f : 0)) return false; }
            return any;
        }
        private Vector3 CompanyCenter(CommandPlatoon p)
        {
            Vector3 sum = Vector3.zero; int count = 0;
            foreach (Actor a in p.members) if (a.fighter.Alive) { sum += a.root.position; count++; }
            return count == 0 ? p.goal : sum / count;
        }
        private void ObserveCompanyContacts()
        {
            foreach (Actor enemy in actors)
            {
                if (!enemy.enemy) continue;
                bool seen = false;
                foreach (Actor human in actors)
                    if (!human.enemy && human.fighter.Alive && Vector3.Distance(human.root.position, enemy.root.position) <= 8 && TerrainSight(human.root.position, enemy.root.position)) { seen = true; break; }
                if (!seen) continue;
                CompanyContact known = companyContacts.Find(c => c.Name == enemy.root.name);
                if (known == null)
                {
                    known = new CompanyContact(enemy.root.name); companyContacts.Add(known);
                    CompanyLog("Contact: " + enemy.root.name + ". Position recorded.");
                }
                else if (known.KnownAlive && !enemy.fighter.Alive) CompanyLog("Confirmed defeated: " + enemy.root.name);
                known.Observe(enemy.root.position, simulationTime, enemy.fighter.Alive);
            }
        }
        private void UpdateCompany(float dt)
        {
            UpdateVillageDefense(dt);
            UpdateCompanyField(dt);
            if (simulationTime >= nextFogObservation) RefreshCompanyFog();
            ObserveCompanyContacts();
            companySelectionMarker.gameObject.SetActive(CompanyLiving(selectedPlatoon) > 0);
            companySelectionMarker.position = CompanyCenter(company[selectedPlatoon]);
            for (int i = 0; i < company.Count; i++)
            {
                CommandPlatoon p = company[i];
                if (CompanyLiving(i) == 0)
                { if (p.phase != CompanyPhase.Lost) { p.phase = CompanyPhase.Lost; p.reason = "Platoon lost"; CompanyLog(p.commander.Name + " platoon lost."); HandleCompanyPlatoonLost(); } continue; }
                if (simulationTime >= p.nextThink)
                {
                    p.nextThink = simulationTime + p.commander.AssessmentInterval;
                    AssessCompanyRisk(i);
                }
                if (p.phase == CompanyPhase.Returning && CompanyAt(p, CompanyCampSlot(i), 2))
                {
                    if (p.order == CompanyOrder.Scout && p.objectiveVisited) CreditCompany(p);
                    p.phase = CompanyPhase.Completed; p.goal = CompanyCampSlot(i); p.reason = "Returned; awaiting orders";
                    CompanyLog(p.commander.Name + " returned with " + CompanyLiving(i) + "/" + CompanyCapacity(i) + " soldiers.");
                }
                else if (p.order == CompanyOrder.Scout && p.phase != CompanyPhase.Returning && p.phase != CompanyPhase.Completed && CompanyAt(p, CompanySitePosition(p.site), 2))
                {
                    p.objectiveVisited = true; p.phase = CompanyPhase.Returning; p.goal = CompanyCampSlot(i); p.reason = "Survey complete: returning with report";
                    CompanyLog(p.commander.Name + " surveyed " + CompanySiteName(p.site) + "; returning.");
                }
                else if (p.order == CompanyOrder.Defend && p.phase != CompanyPhase.Returning && p.phase != CompanyPhase.Completed)
                {
                    if (CompanyAt(p, p.goal, 3)) { p.phase = CompanyPhase.Holding; p.serviceTime += dt; if (p.assigned && p.serviceTime >= 30 && Vector3.Distance(p.goal, CompanySitePosition(p.site)) < 1) CreditCompany(p); }
                }
                else if (p.order == CompanyOrder.Escort && p.phase != CompanyPhase.Returning && p.phase != CompanyPhase.Completed &&
                    Vector3.Distance(player.root.position, CompanySitePosition(p.site)) <= 2 && CompanyAt(p, player.root.position, 4))
                { CreditCompany(p); p.phase = CompanyPhase.Completed; p.goal = CompanySitePosition(p.site); p.reason = "Escort destination reached: defending"; }
                else if (p.order == CompanyOrder.Assault && p.phase != CompanyPhase.Staging && p.phase != CompanyPhase.Returning && p.phase != CompanyPhase.Completed && CompanyAt(p, p.goal, 3))
                {
                    bool contested = false;
                    foreach (Actor e in actors) if (e.enemy && e.fighter.Alive && Vector3.Distance(e.root.position, p.goal) < 7) { contested = true; break; }
                    if (!contested) { CreditCompany(p); p.phase = CompanyPhase.Completed; p.reason = "Objective secured: holding"; CompanyLog(p.commander.Name + " secured " + CompanySiteName(p.site)); }
                }
                // Recovery only at safe camp, never on a field retreat.
                foreach (Actor a in p.members)
                    if (!VillageDefense && a.fighter.Alive && Vector3.Distance(a.root.position, camp) <= 6 && !CampThreatened()) a.fighter.Recover(dt);
            }
            if (companyPlan && CompanyAt(company[0], company[0].goal, 2) && CompanyAt(company[1], company[1].goal, 2))
            {
                companyPlan = false;
                for (int i = 0; i < 2; i++) { company[i].phase = CompanyPhase.Fighting; company[i].goal = CompanySitePosition(CompanySite.Fortress); company[i].reason = "Flank ready: assault released"; }
                CompanyLog("Flank ready. Main and flank platoons begin the assault; reserve stays at Gate.");
            }
        }
        private void CreditCompany(CommandPlatoon p)
        {
            if (p.commander.Credit(p.order, p.site)) CompanyLog(p.commander.Name + " earned 10 practice points from " + p.order + ".");
        }
        private void AssessCompanyRisk(int index)
        {
            CommandPlatoon p = company[index];
            if (p.noFail && p.order == CompanyOrder.Assault) { p.pressure = 0; return; }
            if (p.phase == CompanyPhase.Returning || p.phase == CompanyPhase.Completed && Vector3.Distance(CompanyCenter(p), camp) <= 6) return;
            float threat = 0, support = 0, health = 1;
            foreach (Actor member in p.members) if (member.fighter.Alive) health = Mathf.Min(health, member.fighter.Health / member.fighter.MaximumHealth);
            foreach (Actor other in actors)
            {
                if (!other.fighter.Alive) continue;
                bool visible = false;
                foreach (Actor member in p.members)
                    if (member.fighter.Alive && (member == other || VisibleTo(member, other, 7))) { visible = true; break; }
                if (!visible) continue;
                if (other.enemy) threat += other.hulk ? 2 : 1;
                else support += other.fighter.Stamina >= 25 && other.fighter.Health / other.fighter.MaximumHealth > .35f ? 1 : .5f;
            }
            if (VillageDefense)
            {
                float sum=0; int alive=0;
                foreach (Actor member in p.members) if (member.fighter.Alive) { sum+=member.fighter.Health/member.fighter.MaximumHealth; alive++; }
                health=alive>0 ? sum/alive : 0;
            }
            bool danger = p.commander.ShouldWithdraw(threat, support, health) || p.order == CompanyOrder.Scout && threat >= 2;
            p.pressure = danger ? p.pressure + p.commander.AssessmentInterval : 0;
            if (p.pressure < 1.2f) return;
            CancelCompanyPlan(p.commander.Name + " reports excessive risk; coordinated plan cancelled.");
            p.phase = CompanyPhase.Returning; p.goal = CompanyCampSlot(index); p.reason = "Risk too high: returning to camp";
            CompanyLog(p.commander.Name + " withdraws: " + (health < p.commander.RetreatHealth ? "serious casualties" : "observed threats exceed tolerance"));
        }
        public bool ReviewCompanyOperation()
        {
            if (Scenario != BlightScenario.Company || paused || !player.fighter.CanAct || Vector3.Distance(player.root.position, camp) > 4) return false;
            for (int i = 0; i < company.Count; i++) if (CompanyLiving(i) > 0 && !CompanyAt(company[i], CompanyCampSlot(i), 3))
            { Say("Recall surviving platoons first (4), then review at camp with F."); return false; }
            if (CampThreatened()) return false;
            var report = new StringBuilder("OPERATION REVIEW\n\n");
            if (VillageDefense) report.Append(VillageObjective()).Append("\n\n");
            report.Append("Survivors: ").Append(LivingCompanions).Append("/").Append(CompanyInitialStrength).Append(" companions\n");
            report.Append("Recorded contacts: ").Append(companyContacts.Count).Append(" | Last known alive: ").Append(KnownHostileCount).Append("\n\n");
            for (int i = 0; i < company.Count; i++) report.Append(company[i].commander.Name).Append(" | ").Append(company[i].commander.Risk)
                .Append(" | ").Append(CompanyLiving(i)).Append("/").Append(CompanyCapacity(i)).Append(" returned | Practice ").Append(company[i].commander.Experience).Append("\n").Append(company[i].reason).Append("\n\n");
            report.Append("Practice is retained during this Play session. R starts a fresh operation and clears it.");
            companyReport = report.ToString(); companyReportOpen = true; SetPaused(true); return true;
        }
    }
}
