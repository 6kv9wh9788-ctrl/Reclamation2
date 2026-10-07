using UnityEngine;

namespace Reclamation.Atlas
{
    public sealed partial class WorldAtlasDemo
    {
        private GUIStyle heading,body,small,button,subheading;
        private Rect CommandHudRect => new Rect(Width-330,174,314,197);
        private Rect ScoutHudRect => new Rect(16,90,365,103);
        private Rect DiagnosticHudRect => new Rect(16,Height-429,415,291);
        private void Styles()
        {
            if(heading!=null)return;
            heading=new GUIStyle(GUI.skin.label){fontSize=25,fontStyle=FontStyle.Bold};heading.normal.textColor=new Color(.92f,.9f,.8f);
            subheading=new GUIStyle(heading){fontSize=18};
            body=new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true};body.normal.textColor=new Color(.85f,.88f,.85f);
            small=new GUIStyle(body){fontSize=13};button=new GUIStyle(GUI.skin.button){fontSize=14};
        }
        private void Panel(Rect r){Color old=GUI.color;GUI.color=new Color(.08f,.12f,.13f,.96f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        private void OnGUI()
        {
            if(Model==null||wildlifePhotoView)return;Styles();var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(Vector3.one*UiScale);
            if(defenseMode){DrawAtlasDefenseHud();GUI.matrix=old;return;}
            Panel(new Rect(16,16,Width-32,60));
            GUI.Label(new Rect(30,21,640,26),Model.Name,subheading);
            GUI.Label(new Rect(30,48,Width-420,22),(Model.Extent/1000).ToString("0.0")+" km across  /  "+Model.factions.Count+" factions  /  "+Model.sites.Count+" settlement sites",small);
            GUI.Label(new Rect(Width-310,23,280,44),"DAY "+day+"  /  "+((int)clockHours).ToString("00")+":"+((int)(clockHours%1*60)).ToString("00")+"  /  "+clockRate+"x\n"+RoutineSummary,small);
            if(mapOpen)DrawAtlas();else {DrawNearby();DrawMinimap();DrawCommanderBadge();}DrawCommanderPanel();DrawScoutPanel();
            float y=Height-69;Panel(new Rect(16,y-8,Width-32,63));
            if(GUI.Button(new Rect(28,y,150,30),mapOpen?"M  Close atlas":"M  Open atlas",button))mapOpen=!mapOpen;
            if(GUI.Button(new Rect(188,y,180,30),"Small forest valley",button))LoadMap(AtlasSize.Small,4);
            if(GUI.Button(new Rect(378,y,200,30),"Medium mountain coast",button))LoadMap(AtlasSize.Medium,8);
            if(GUI.Button(new Rect(588,y,130,30),"Return home",button))Travel(Model.factions[0].capital);
            if(GUI.Button(new Rect(728,y,145,30),"Village close-up",button))VisitVillage();
            if(GUI.Button(new Rect(884,y,90,30),"Time "+clockRate+"x",button))clockRate=clockRate==1?10:clockRate==10?60:1;
            if(GUI.Button(new Rect(984,y,110,30),"Town hall",button))VisitTownHall();
            if(GUI.Button(new Rect(Width-282,y,254,30),debug?"Hide developer diagnostics":"Show developer diagnostics",button))debug=!debug;
            GUI.Label(new Rect(30,y+34,Width-60,20),"WASD explore / Shift sprint / RMB look / Wheel zoom / F claim nearby clearing / J wildlife / M atlas / Esc pause",small);
            if(debug&&!mapOpen)
            {
                Rect r=DiagnosticHudRect;Panel(r);
                GUI.Label(new Rect(r.x+12,r.y+7,r.width-24,22),"DEBUG / RECORDING AID  ·  F3 hides",subheading);
                GUI.Label(new Rect(r.x+12,r.y+34,r.width-24,62),"Frame interval (not a benchmark): "+frameMs.ToString("0.0")+" ms  |  Company "+ActiveGuardCount+"/25\nPosition "+hero.position.ToString("F1")+"  |  Villagers "+ActiveVillagerCount+"/18\nSeed "+LayoutFor(selected).Seed+" / "+LayoutFor(selected).Name,small);
                if(guardSite>=0)
                {
                    GUI.Label(new Rect(r.x+12,r.y+103,r.width-24,39),"P1 "+FormationPhase(0)+" / P2 "+FormationPhase(1)+"\nP3 "+FormationPhase(2)+" / P4 "+FormationPhase(3),small);
                    int row=0;foreach(string report in ActiveCompany.Reports)
                        GUI.Label(new Rect(r.x+12,r.y+147+row++*22,r.width-24,22),report,new GUIStyle(small){fontSize=11,wordWrap=false});
                }

            }
            if(paused){Panel(new Rect(Width/2-130,Height/2-35,260,70));GUI.Label(new Rect(Width/2-105,Height/2-18,220,40),"PAUSED - Esc",heading);}
            GUI.matrix=old;
        }
        private Vector2 MapPoint(Rect r,Vector2 p)=>new Vector2(r.x+(p.x/Model.Extent+.5f)*r.width,r.y+(.5f-p.y/Model.Extent)*r.height);
        private void Dot(Vector2 p,float radius,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(p.x-radius,p.y-radius,radius*2,radius*2),Texture2D.whiteTexture);GUI.color=old;}
        private void DrawAtlas()
        {
            float side=Mathf.Min(Height-250,Width*.53f);Rect map=new Rect(20,125,side,side);GUI.DrawTexture(map,mapTexture,ScaleMode.StretchToFill);GUI.DrawTexture(map,fogTexture,ScaleMode.StretchToFill);
            foreach(var s in Model.sites)
            {
                int knownOwner=Knowledge.ReportedOwner(s.id);if(knownOwner==AtlasKnowledge.UnknownOwner)continue;
                var p=MapPoint(map,s.point);var c=OwnerColor(knownOwner);
                if(knownOwner>=0)Dot(p,s.capital?12:7,new Color(c.r,c.g,c.b,.45f));Dot(p,s.id==selected?7:s.capital?5:3,s.id==selected?Color.white:c);
                if(s.capital)GUI.Label(new Rect(p.x+7,p.y-12,70,22),"F"+knownOwner,small);
                if(knownOwner==-2||knownOwner==-3)GUI.Label(new Rect(p.x+5,p.y-13,60,22),knownOwner==-2?"Trade":"Lair",small);
            }
            var chosen=Model.sites[selected];foreach(var d in chosen.deposits)if(Knowledge.Explored(ResourceDisplayPoint(chosen,d)))Dot(MapPoint(map,ResourceDisplayPoint(chosen,d)),4,d.kind==AtlasResource.Gold?Color.yellow:d.kind==AtlasResource.Niter?Color.cyan:d.kind==AtlasResource.Wood?Color.green:Color.white);
            if(hasScoutReport)Dot(MapPoint(map,reportedContact),5,new Color(1,.3f,.14f));
            Dot(MapPoint(map,new Vector2(hero.position.x,hero.position.z)),5,new Color(.25f,.8f,1));
            if(Model.Size==AtlasSize.Small)foreach(float z in new[]{-Model.Extent*.25f,0,Model.Extent*.25f}){var p=MapPoint(map,new Vector2(Model.RiverX(z),z));if(Knowledge.Explored(new Vector2(Model.RiverX(z),z)))GUI.Label(new Rect(p.x,p.y,65,22),"Ford",small);}
            foreach(int id in new[]{Model.sites.Count/3,Model.sites.Count*2/3}){var mark=MapPoint(map,Model.sites[id].point+Vector2.up*65);if(Knowledge.Explored(Model.sites[id].point+Vector2.up*65))GUI.Label(new Rect(mark.x,mark.y,24,22),"C",small);}
            if(Event.current.type==EventType.MouseDown&&Event.current.button==0&&map.Contains(Event.current.mousePosition)&&!new Rect(map.x+5,map.y+5,250,68).Contains(Event.current.mousePosition))
            {
                var p=new Vector2((Event.current.mousePosition.x-map.x)/map.width-.5f,.5f-(Event.current.mousePosition.y-map.y)/map.height)*Model.Extent;
                int candidate=Model.Nearest(p).id;if(Knowledge.ReportedOwner(candidate)!=AtlasKnowledge.UnknownOwner)selected=candidate;Event.current.Use();
            }
            GUI.Label(new Rect(map.x,map.yMax+8,map.width,38),"North up / Cyan: you / White: selected\nF: faction home / Trade: neutral / Lair: hostile / C: cavern candidate",small);
            DrawAtlasDetails(map,chosen);
            float controls=map.y+10;int a=Model.Size==AtlasSize.Small?2:8,b=Model.Size==AtlasSize.Small?4:12;
            if(GUI.Button(new Rect(map.x+10,controls,115,27),a+" factions",button)){LoadMap(Model.Size,a);mapOpen=true;}
            if(GUI.Button(new Rect(map.x+135,controls,115,27),b+" factions",button)){LoadMap(Model.Size,b);mapOpen=true;}
            if(GUI.Button(new Rect(map.x+10,controls+32,240,27),showArmies?"Hide 128-soldier scale figures":"Show 128-soldier scale figures",button))showArmies=!showArmies;
        }
        private void DrawAtlasDetails(Rect map,AtlasSite chosen)
        {
            float x=map.xMax+20,w=Width-x-20,y=125;Panel(new Rect(x,y,w,map.height));x+=16;w-=32;
            int knownOwner=Knowledge.ReportedOwner(chosen.id);if(knownOwner==AtlasKnowledge.UnknownOwner){GUI.Label(new Rect(x,y+10,w,60),"UNEXPLORED / Scout this region first",body);return;}
            GUI.Label(new Rect(x,y+10,w,32),chosen.name,heading);y+=47;
            string owner=knownOwner>=0?Model.factions[knownOwner].name+" ("+Model.factions[knownOwner].culture+")":Model.Relation(0,knownOwner);
            GUI.Label(new Rect(x,y,w,38),owner+" / "+Model.Relation(0,knownOwner)+"\n"+Vector2.Distance(new Vector2(hero.position.x,hero.position.z),chosen.point).ToString("0")+" m away; "+chosen.elevation.ToString("0")+" m elevation",small);y+=44;
            string resource="Nearby: ";foreach(var d in chosen.deposits)if(Knowledge.Explored(ResourceDisplayPoint(chosen,d)))resource+=d.kind+" "+d.richness+"   ";GUI.Label(new Rect(x,y,w,40),resource,body);y+=43;
            if(GUI.Button(new Rect(x,y,w*.47f,30),"Survey travel here",button)){Travel(selected);mapOpen=false;}
            if(GUI.Button(new Rect(x+w*.50f,y,w*.5f,30),"Claim when nearby (F)",button))ClaimNearby();y+=37;
            for(int i=0;i<3;i++)if(GUI.Button(new Rect(x+i*w/3,y,w/3-5,27),new[]{"Village footprint","City footprint","Fortress footprint"}[i],button)){Travel(selected);PreviewFootprint(i);mapOpen=false;}y+=35;
            GUI.Label(new Rect(x,y,w,25),"FACTIONS / culture / relationship / holdings",body);y+=28;
            int rows=(Model.factions.Count+1)/2;
            for(int i=0;i<Model.factions.Count;i++)
            {
                if(KnownHoldings(i)==0)continue;var f=Model.factions[i];float column=x+(i/rows)*(w/2),line=y+(i%rows)*28;
                Dot(new Vector2(column+5,line+9),5,f.color);
                GUI.Label(new Rect(column+18,line,w/2-20,26),"F"+i+" "+f.name+" / "+f.culture+" / "+Model.Relation(0,i)+" / "+KnownHoldings(i)+" reported sites",small);
            }
            y+=rows*28+8;
            if(GUI.Button(new Rect(x,y,w*.55f,29),simulation?"Pause abstract faction simulation":"Run abstract faction simulation",button))simulation=!simulation;
            if(GUI.Button(new Rect(x+w*.58f,y,w*.42f,29),"Advance one turn",button))Model.AdvanceStrategy();y+=35;
            GUI.Label(new Rect(x,y,w,48),"Optional territory model: bots expand, form affinity alliances and contest sites. Army battles are not simulated. Turn "+Model.Turns+(Model.Winner>=0?" / alliance "+Model.Winner+" controls faction territory":""),small);y+=51;
            GUI.Label(new Rect(x,y,w,32),"Map ownership is last reported. Dark = unknown; dim = explored. Enemy movements require a sighting.",small);
        }
        private void DrawNearby()
        {
            var near=Model.Nearest(new Vector2(hero.position.x,hero.position.z));float d=Vector2.Distance(near.point,new Vector2(hero.position.x,hero.position.z));
            float x=Width-330;Panel(new Rect(x,90,314,74));
            GUI.Label(new Rect(x+12,96,290,25),Knowledge.ReportedOwner(near.id)==AtlasKnowledge.UnknownOwner?"Uncharted region":near.name,subheading);
            string relation=Knowledge.ReportedOwner(near.id)==AtlasKnowledge.UnknownOwner?"No report":Model.Relation(0,Knowledge.ReportedOwner(near.id));
            GUI.Label(new Rect(x+12,122,290,37),relation+" / "+d.ToString("0")+" m\n"+(d<65&&near.owner==-1?"F: establish camp":IsInsideTownHall?"Town hall / doorway to exit":"C: commander / H: workplaces"),small);
            Panel(new Rect(16,Height-125,Width-32,39));GUI.Label(new Rect(30,Height-119,Width-60,32),message,small);
        }
    }
}

