using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        private Vector2 companySoldierScroll;
        private Rect CompanyPanel => new Rect(UiWidth - 310, ScenarioRect.yMax + 8, 294, 452);
        private Rect CompanyReportRect => new Rect(UiWidth / 2 - 310, 180, 620, 430);
        private bool CompanyUiContains(Vector2 point) => Scenario == BlightScenario.Company &&
            (CompanyMapOpen || new Rect(UiWidth - 310, UiHeight - 42, 294, 30).Contains(point) || CompanyCardsRect.Contains(point) || CompanyPanel.Contains(point) || companyReportOpen && CompanyReportRect.Contains(point));

        private void DrawCompanyCommands()
        {
            GUI.Box(CommandRect, GUIContent.none); float y = CommandRect.y + 6;
            bool old = GUI.enabled; GUI.enabled = old && !paused && player.fighter.Alive;
            for (int i = 0; i < 3; i++)
                if (GUI.Button(new Rect(24 + i * 153, y, 147, 26), (selectedPlatoon == i ? "> " : "") + (i + 1) + " " + company[i].commander.Name, button)) selectedPlatoon = i;
            if (GUI.Button(new Rect(490, y, 140, 26), "Recall all [4]", button)) RecallCompany();
            if (GUI.Button(new Rect(638, y, 146, 26), VillageDefense ? (!VillageStarted ? "Start waves" : "Deploy defense") : "Plan assault", button)) { if (VillageDefense && !VillageStarted) StartVillageDefense(); else PlanCompanyAssault(); }
            y += 32;
            for (int i = 0; i < 5; i++)
                if (GUI.Button(new Rect(24 + i * 153, y, 147, 26), ((CompanyOrder)i).ToString(), button)) GiveCompanyOrder(selectedPlatoon, (CompanyOrder)i, selectedCompanySite);
            GUI.enabled = old;
            if (help) GUI.Label(new Rect(24, y + 31, 755, 75),
                "G tactical map: pause, preview and queue orders. 1–3 selects platoons.\n" +
                "WASD | RMB camera | Ctrl block | Shift sprint | LMB/E attack | Space dodge\n" +
                "4 recall all | F at camp: review | Esc pause | R fresh operation | B gear | H help", small);
        }
        private void DrawCompanyMarkers()
        {
            if (Scenario != BlightScenario.Company) return;
            WorldMarker(camp + Vector3.up, VillageDefense ? "SQUARE — fallback [4], review [F]" : "CAMP — recall all [4], review [F]");
            for (int i = 0; i < 4; i++) WorldMarker(CompanySitePosition((CompanySite)i) + Vector3.up, CompanySiteName((CompanySite)i));
        }
        private void DrawCompanyPanels()
        {
            if (Scenario != BlightScenario.Company) return;
            if (!VillageDefense && !CompanyMapOpen && !companyReportOpen && GUI.Button(new Rect(UiWidth - 310, UiHeight - 42, 294, 30), ExpandedCompany ? "Region: expanded (reset to compact)" : "Region: compact (reset to expanded)", button)) SetExpandedCompany(!ExpandedCompany);
            if (!scenarioMenu && !equipmentMenu)
            {
                Rect panel = CompanyPanel; GUI.Box(panel, GUIContent.none);
                if (GUI.Button(new Rect(panel.x + 8, panel.y + 5, 278, 24), "Tactical map & planning [G]", button)) OpenCompanyMap();
                bool old = GUI.enabled; GUI.enabled = old && !paused && player.fighter.Alive;
                for (int i = 0; i < 4; i++)
                    if (GUI.Button(new Rect(panel.x + 8 + i % 2 * 140, panel.y + 34 + i / 2 * 28, 134, 25),
                        (selectedCompanySite == (CompanySite)i ? "> " : "") + CompanySiteName((CompanySite)i), button)) selectedCompanySite = (CompanySite)i;
                GUI.enabled = old;
                CommandPlatoon p = company[selectedPlatoon];
                GUI.Label(new Rect(panel.x+8,panel.y+100,278,26),p.commander.Name+" | "+CompanyActivity(selectedPlatoon),label);
                GUI.Label(new Rect(panel.x+8,panel.y+128,278,44),p.commander.Risk+" | Practice "+p.commander.Experience+"\n"+p.reason,small);
                companySoldierScroll = GUI.BeginScrollView(new Rect(panel.x+8,panel.y+178,278,58),companySoldierScroll,new Rect(0,0,258,p.members.Count*28));
                for (int i=0;i<p.members.Count;i++)
                {
                    Actor a=p.members[i];
                    GUI.Label(new Rect(0,i*28,258,26),a.root.name.Split(' ')[0]+": "+(a.fighter.Alive ? a.intent : "Down"),small);
                }
                GUI.EndScrollView();
                bool enabled=GUI.enabled; GUI.enabled=enabled && !paused && CompanyReserveHeld;
                if (GUI.Button(new Rect(panel.x+8,panel.y+241,278,28),VillageDefense ? "Reinforce main gate" : "Release reserve to fortress",button)) ReleaseCompanyReserve();
                GUI.enabled=enabled;
                if (GUI.Button(new Rect(panel.x+8,panel.y+277,278,26),companyDetails ? "Hide decision history" : "Show decision history",button)) companyDetails=!companyDetails;
                if (companyDetails)
                    for (int i=Mathf.Max(0,companyJournal.Count-3),row=0;i<companyJournal.Count;i++,row++)
                        GUI.Label(new Rect(panel.x+8,panel.y+309+row*43,278,41),companyJournal[i],small);
                else GUI.Label(new Rect(panel.x+8,panel.y+314,278,60),VillageDefense ? "Reserve holds the square.\n4: fallback all platoons to square." : "Reserve: hold until released.\nScouting reports and supply intel: G.",small);
            }
            if (!equipmentMenu && !scenarioMenu && !CompanyMapOpen) DrawCompanyCards();
            if (companyReportOpen)
            {
                GUI.Box(CompanyReportRect, GUIContent.none);
                GUI.Label(new Rect(CompanyReportRect.x + 16, CompanyReportRect.y + 12, 588, 365), companyReport, label);
                if (GUI.Button(new Rect(CompanyReportRect.x + 200, CompanyReportRect.y + 385, 220, 30), "Resume operation", button))
                { companyReportOpen = false; SetPaused(false); }
            }
        }
    }
}
