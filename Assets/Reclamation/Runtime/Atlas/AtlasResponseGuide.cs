using UnityEngine;

namespace Reclamation.Atlas
{
    // Presentation reads the assigned reserve, never the reporting patrol or nearest soldier.
    public sealed partial class WorldAtlasDemo
    {
        private bool responseGuideComplete;
        private static readonly Color ResponseColor = new Color(1, .69f, .20f);
        public int ResponseGuidePlatoon => guardSite >= 0 && guardSite == scoutSite &&
            ActiveCompany.ReportDelivered ? ActiveCompany.RespondingPlatoon : -1;
        public string ResponseGuideName => ResponseGuidePlatoon < 0 ? "" : ActiveCompany.Platoons[ResponseGuidePlatoon].Name;
        public string ResponseGuideStage
        {
            get
            {
                int p = ResponseGuidePlatoon;
                if (p < 0) return "";
                if (responseGuideComplete) return "Watch resumed";
                var phase = FormationPhase(p);
                if (!scoutEncounter.Active) return phase == AtlasFormationPhase.Returning ? "Returning" : "Watch resumed";
                if (phase == AtlasFormationPhase.Line) return "Holding";
                if (phase == AtlasFormationPhase.Deploying) return "Deploying";
                return "Travelling";
            }
        }
        public bool ResponseGuideHighlighted => ResponseGuidePlatoon >= 0 && ResponseGuideStage != "Watch resumed";
        private void UpdateResponseGuide()
        {
            int p = ResponseGuidePlatoon;
            if (p >= 0 && !scoutEncounter.Active && FormationPhase(p) != AtlasFormationPhase.Returning)
                responseGuideComplete = true;
        }
        private void ResponseOutline(Rect r)
        {
            Color before = GUI.color; GUI.color = ResponseColor;
            GUI.DrawTexture(new Rect(r.x,r.y,r.width,2),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x,r.yMax-2,r.width,2),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x,r.y,2,r.height),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax-2,r.y,2,r.height),Texture2D.whiteTexture);
            GUI.color = before;
        }
        private void DrawResponseMinimap(Rect map, Vector2 position)
        {
            bool outside = !map.Contains(position);
            position.x = Mathf.Clamp(position.x,map.x+9,map.xMax-9);
            position.y = Mathf.Clamp(position.y,map.y+9,map.yMax-9);
            ResponseOutline(new Rect(position.x-7,position.y-7,14,14));
            string label = "P"+(ResponseGuidePlatoon+1)+(outside ? " / off map" : "");
            Rect text = new Rect(Mathf.Clamp(position.x+10,map.x,map.xMax-94),Mathf.Clamp(position.y-21,map.y,map.yMax-20),94,20);
            Panel(text);
            var style = new GUIStyle(small){fontSize=11,fontStyle=FontStyle.Bold};style.normal.textColor=ResponseColor;
            GUI.Label(text,label,style);
        }
        private void DrawResponseCard()
        {
            int p = ResponseGuidePlatoon; if(p<0) return;
            Rect r = ScoutHudRect; Panel(r);
            var accent = new GUIStyle(subheading);accent.normal.textColor=ResponseColor;
            GUI.Label(new Rect(r.x+12,r.y+7,r.width-24,25),"P"+(p+1)+" / "+ResponseGuideName+" / "+ResponseGuideStage.ToUpperInvariant(),accent);
            string detail;
            if(ResponseGuideStage == "Travelling") detail="Responding platoon / amber square on scout map";
            else if(ResponseGuideStage == "Deploying") detail="Forming the defensive line at the approach";
            else if(ResponseGuideStage == "Holding") detail=ScoutPhase==ScoutEncounterPhase.Withdrawing?"Scouts withdrawing / hold position, no pursuit":"Defensive line ready / watching the approach";
            else if(ResponseGuideStage == "Returning") detail=ScoutPhase==ScoutEncounterPhase.Secured?"Approach secured / returning to assigned watch":"Contact lost / returning to assigned watch";
            else detail=ScoutPhase==ScoutEncounterPhase.Secured?"Approach secured / routine watch resumed":"Contact lost / routine watch resumed";
            GUI.Label(new Rect(r.x+12,r.y+37,r.width-24,36),detail,small);
            string hint = scoutEncounter.Active ? "G: visit CONTACT / E: challenge with support" : "R: replay scout encounter";
            GUI.Label(new Rect(r.x+12,r.y+77,r.width-24,22),hint,small);
        }
    }
}
