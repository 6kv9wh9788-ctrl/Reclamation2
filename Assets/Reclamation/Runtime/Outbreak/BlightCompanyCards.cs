using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        private Rect CompanyCardsRect => new Rect(16, 130, 335, 368);
        private bool companyDetails;
        public bool CompanyReserveHeld => Scenario == BlightScenario.Company && company.Count == 3 &&
            company[2].assigned && company[2].order == CompanyOrder.Defend && company[2].site == (VillageDefense ? CompanySite.Fortress : CompanySite.Gate) &&
            company[2].phase != CompanyPhase.Returning && CompanyLiving(2) > 0;
        public bool ReleaseCompanyReserve() => CompanyReserveHeld && GiveCompanyOrder(2, VillageDefense ? CompanyOrder.Defend : CompanyOrder.Assault, VillageDefense ? CompanySite.Gate : CompanySite.Fortress);
        public string CompanyActivity(int index)
        {
            CommandPlatoon p = company[index];
            if (CompanyLiving(index) == 0) return "Lost";
            if (p.phase == CompanyPhase.Returning) return "Withdrawing";
            bool moving = false, fighting = false, recovering = false, engaged = false, blocked = false;
            foreach (Actor a in p.members) if (a.fighter.Alive)
            {
                moving |= a.moving; blocked |= a.navigationStall >= 1.5f;
                fighting |= a.fighter.Action == DuelAction.Windup || a.fighter.Action == DuelAction.Recovery;
                recovering |= a.defense.recovering;
                engaged |= a.target != null && a.target.fighter.Alive;
            }
            if (fighting) return "Attacking";
            if (engaged) return recovering ? "Engaged / recovering" : "Engaging";
            if (blocked) return "Route blocked";
            if (p.phase == CompanyPhase.Staging) return CompanyAt(p, p.goal, 2) ? "Awaiting partner" : "Staging";
            if (recovering) return "Recovering";
            if (moving) return "Moving";
            if (p.phase == CompanyPhase.Completed) return "Task complete";
            return "Holding";
        }
        private string CompanyMission(int index)
        {
            CommandPlatoon p = company[index];
            return !p.assigned || index == 2 && CompanyReserveHeld ? "Reserve" : p.order.ToString();
        }
        private Color CompanyStatusColor(int index)
        {
            if (CompanyLiving(index) == 0) return new Color(.65f,.3f,.3f);
            if (company[index].phase == CompanyPhase.Returning) return new Color(1,.65f,.25f);
            return selectedPlatoon == index ? new Color(.4f,.85f,.9f) : new Color(.65f,.72f,.72f);
        }
        private void MissionIcon(Rect r, CompanyOrder order, bool reserve, Color ink)
        {
            Vector2 a = new Vector2(r.x + 6, r.y + 6), b = new Vector2(r.xMax - 6, r.yMax - 6);
            Vector2 mid = r.center;
            if (reserve)
            {
                Fill(new Rect(a.x,a.y,6,b.y-a.y),ink); Fill(new Rect(b.x-6,a.y,6,b.y-a.y),ink);
            }
            else if (order == CompanyOrder.Assault)
            { MapLine(a,b,ink); MapLine(new Vector2(a.x,b.y),new Vector2(b.x,a.y),ink); }
            else if (order == CompanyOrder.Defend)
            {
                MapLine(a,new Vector2(b.x,a.y),ink); MapLine(a,new Vector2(a.x,mid.y),ink);
                MapLine(new Vector2(b.x,a.y),new Vector2(b.x,mid.y),ink);
                MapLine(new Vector2(a.x,mid.y),new Vector2(mid.x,b.y),ink); MapLine(new Vector2(b.x,mid.y),new Vector2(mid.x,b.y),ink);
            }
            else if (order == CompanyOrder.Scout)
            {
                Vector2 center = mid - Vector2.one * 3;
                for (int i = 0; i < 12; i++)
                {
                    float t = i * Mathf.PI / 6, next = (i+1) * Mathf.PI / 6;
                    MapLine(center+new Vector2(Mathf.Cos(t),Mathf.Sin(t))*8,center+new Vector2(Mathf.Cos(next),Mathf.Sin(next))*8,ink);
                }
                MapLine(center+Vector2.one*6,b,ink);
            }
            else
            {
                bool back = order == CompanyOrder.Withdraw;
                Vector2 tip = new Vector2(back ? a.x : b.x,mid.y), tail = new Vector2(back ? b.x : a.x,mid.y);
                MapLine(tail,tip,ink); float dx = back ? 7 : -7;
                MapLine(tip,tip+new Vector2(dx,-7),ink); MapLine(tip,tip+new Vector2(dx,7),ink);
                if (!back) Fill(new Rect(a.x-2,a.y,4,b.y-a.y),ink);
            }
        }
        private void DrawCompanyCards()
        {
            for (int i = 0; i < company.Count; i++)
            {
                CommandPlatoon p = company[i]; Rect card = new Rect(16,130+i*108,335,102);
                Color ink = CompanyStatusColor(i);
                Fill(card,new Color(.055f,.075f,.085f,.98f));
                if (GUI.Button(card,GUIContent.none,GUIStyle.none)) selectedPlatoon = i;
                Fill(new Rect(card.x,card.y,4,card.height),ink);
                MissionIcon(new Rect(card.x+9,card.y+9,36,36),p.order,CompanyMission(i)=="Reserve",ink);
                GUI.Label(new Rect(card.x+52,card.y+5,220,23),(i+1)+"  "+p.commander.Name+"  |  "+CompanyMission(i),label);
                GUI.Label(new Rect(card.x+278,card.y+5,52,23),CompanyLiving(i)+"/"+CompanyCapacity(i),label);
                GUI.Label(new Rect(card.x+52,card.y+29,270,22),CompanyActivity(i),label);
                float health=0, stamina=0; int alive=0;
                foreach (Actor a in p.members) if (a.fighter.Alive) { health+=a.fighter.Health/a.fighter.MaximumHealth; stamina+=a.fighter.Stamina; alive++; }
                Meter(new Rect(card.x+12,card.y+57,150,12),alive>0 ? health/alive : 0,1,new Color(.25f,.6f,.43f),"");
                Meter(new Rect(card.x+172,card.y+57,150,12),alive>0 ? stamina/alive : 0,100,new Color(.3f,.65f,.8f),"");
                string location = !p.assigned ? (VillageDefense ? "Square" : "Camp") : (p.phase == CompanyPhase.Returning || p.order == CompanyOrder.Withdraw) ? (VillageDefense ? "Square" : "Camp") : CompanySiteName(p.site);
                GUI.Label(new Rect(card.x+12,card.y+74,310,22),location+"  |  "+(p.noFail ? "NO FAIL" : i==2 && CompanyReserveHeld ? "HOLD UNTIL RELEASED" : "Discretionary"),small);
            }
            GUI.Label(new Rect(24,456,320,20),"Bars: health / stamina   |   Select for detail",small);
            GUI.Label(new Rect(24,478,320,20),"Last-known hostiles: "+KnownHostileCount+"  |  Intel on map [G]",small);
        }
    }
}
