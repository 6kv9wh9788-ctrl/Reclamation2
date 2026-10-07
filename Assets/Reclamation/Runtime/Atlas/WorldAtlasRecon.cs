using UnityEngine;
namespace Reclamation.Atlas
{
    public sealed partial class WorldAtlasDemo
    {
        public AtlasKnowledge Knowledge { get; private set; }
        private Texture2D fogTexture, rankStar;
        private Color32[] fogPixels;
        private float reconTimer, reportAge;
        private bool hasScoutReport, reportDelivered;
        private Vector2 reportedContact;
        public bool HasScoutMapReport=>hasScoutReport;
        public Vector2 ReportedScoutPosition=>reportedContact;
        private readonly System.Collections.Generic.List<Light> torchLights=new System.Collections.Generic.List<Light>();
        public int ActiveTorchLights {get;private set;}
        public int LitGuardTorches {get;private set;}
        private void BuildRecon()
        {
            Knowledge=new AtlasKnowledge(Model);hasScoutReport=reportDelivered=false;reportAge=reconTimer=0;
            fogPixels=new Color32[AtlasKnowledge.Resolution*AtlasKnowledge.Resolution];
            fogTexture=new Texture2D(AtlasKnowledge.Resolution,AtlasKnowledge.Resolution,TextureFormat.RGBA32,false){name="Scout knowledge",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};owned.Add(fogTexture);
            rankStar=new Texture2D(32,32,TextureFormat.RGBA32,false);owned.Add(rankStar);
            var starPoints=new Vector2[10];for(int i=0;i<10;i++){float a=i*Mathf.PI/5+Mathf.PI/2;starPoints[i]=new Vector2(16+Mathf.Cos(a)*(i%2==0?15:6),16+Mathf.Sin(a)*(i%2==0?15:6));}
            for(int y=0;y<32;y++)for(int x=0;x<32;x++){bool inside=false;for(int i=0,j=9;i<10;j=i++)if((starPoints[i].y>y)!=(starPoints[j].y>y)&&x<(starPoints[j].x-starPoints[i].x)*(y-starPoints[i].y)/(starPoints[j].y-starPoints[i].y)+starPoints[i].x)inside=!inside;rankStar.SetPixel(x,y,inside?new Color(1,.8f,.23f):Color.clear);}rankStar.Apply();
            torchLights.Clear();for(int i=0;i<4;i++){var go=new GameObject("Budgeted watch torch light");go.transform.SetParent(world,false);var l=go.AddComponent<Light>();l.type=LightType.Point;l.range=7;l.intensity=2.4f;l.color=new Color(1,.55f,.18f);l.shadows=LightShadows.None;l.enabled=false;torchLights.Add(l);}
        }
        public void RefreshKnowledge()
        {
            if(Knowledge==null)return;
            Knowledge.BeginObservation();Knowledge.Reveal(new Vector2(hero.position.x,hero.position.z),90);
            if(guardSite>=0&&Knowledge.Allied(Model.sites[guardSite].owner))foreach(var g in guards)if(g.root.gameObject.activeSelf&&AtlasGuardAlive(g.root))Knowledge.Reveal(new Vector2(g.root.position.x,g.root.position.z),35);
            Knowledge.FinishObservation(fogPixels);fogTexture.SetPixels32(fogPixels);fogTexture.Apply(false);
        }
        private void UpdateRecon(float seconds)
        {
            if(Knowledge==null)return;
            reconTimer-=seconds;if(reconTimer<=0){RefreshKnowledge();reconTimer=.25f;}
            if(!paused)reportAge+=seconds*clockRate;
            if(!scoutEncounter.Known)hasScoutReport=reportDelivered=false;
            bool seen=false;
            if(scoutEncounter.Active){seen=ScoutSight(hero.position,ScoutContactPosition);if(guardSite>=0&&Knowledge.Allied(Model.sites[guardSite].owner))foreach(var g in guards)if(g.root.gameObject.activeSelf&&ScoutSight(g.root.position,ScoutContactPosition)){seen=true;break;}}
            // Reports reach the map after the existing command reporting delay.
            if(scoutEncounter.Known&&ScoutPhase!=ScoutEncounterPhase.Reporting&&(seen||!reportDelivered))
            {reportedContact=new Vector2(ScoutContactPosition.x,ScoutContactPosition.z);hasScoutReport=reportDelivered=true;reportAge=0;Knowledge.Reveal(reportedContact,25);}
            if(!scoutEncounter.Active||reportAge>60)hasScoutReport=false;
            foreach(var scout in hostileScouts)if(scout)foreach(var renderer in scout.GetComponentsInChildren<Renderer>())renderer.enabled=seen;
            UpdateWatchTorches();
        }
        private void UpdateWatchTorches()
        {
            bool night=clockHours>=19||clockHours<6;LitGuardTorches=ActiveTorchLights=0;
            foreach(var g in guards){bool lit=night&&g.root.gameObject.activeSelf&&AtlasGuardAlive(g.root);g.rig.SetTorch(lit);if(lit)LitGuardTorches++;}
            foreach(var light in torchLights)light.enabled=false;
            if(!night||!view)return;
            // Select four nearby bearers, not one realtime light for every soldier.
            var nearest=new System.Collections.Generic.List<GuardVisual>();foreach(var g in guards)if(g.root.gameObject.activeSelf&&AtlasGuardAlive(g.root))nearest.Add(g);
            nearest.Sort((a,b)=>(a.root.position-view.transform.position).sqrMagnitude.CompareTo((b.root.position-view.transform.position).sqrMagnitude));
            for(int i=0;i<Mathf.Min(4,nearest.Count);i++){if(Vector3.Distance(nearest[i].root.position,view.transform.position)>45)break;torchLights[i].transform.position=nearest[i].rig.FlamePosition;torchLights[i].enabled=true;ActiveTorchLights++;}
        }
        private int KnownHoldings(int faction){int count=0;foreach(var site in Model.sites)if(Knowledge.ReportedOwner(site.id)==faction)count++;return count;}
        private void DrawMinimap()
        {
            const float side=236,range=400;
            Rect frame=new Rect(Width-268,Height-422,252,284),r=new Rect(frame.x+8,frame.y+29,side,side);
            Panel(frame);GUI.Label(new Rect(frame.x+12,frame.y+5,220,22),hasScoutReport?"SCOUT MAP / report "+reportAge.ToString("0")+"s old / N":"SCOUT MAP                         N ^",small);
            Vector2 center=new Vector2(hero.position.x,hero.position.z);float half=Model.Extent*.5f;
            center.x=Mathf.Clamp(center.x,-half+range/2,half-range/2);center.y=Mathf.Clamp(center.y,-half+range/2,half-range/2);
            Rect uv=new Rect((center.x-range/2)/Model.Extent+.5f,(center.y-range/2)/Model.Extent+.5f,range/Model.Extent,range/Model.Extent);
            GUI.DrawTextureWithTexCoords(r,mapTexture,uv);GUI.DrawTextureWithTexCoords(r,fogTexture,uv);
            Vector2 P(Vector2 p)=>new Vector2(r.center.x+(p.x-center.x)*side/range,r.center.y-(p.y-center.y)*side/range);
            foreach(var s in Model.sites)if(Knowledge.ReportedOwner(s.id)!=AtlasKnowledge.UnknownOwner&&r.Contains(P(s.point)))
            {
                if(developedVillages.Contains(s.id))
                {
                    var layout=LayoutFor(s.id);
                    // Reported roads and work sites give the local map useful landmarks.
                    for(int road=0;road<3;road++)
                    {
                        var points=layout.WorkRoad(road);
                        for(int segment=1;segment<points.Length;segment++)
                        {
                            Vector2 a=s.point+points[segment-1],b=s.point+points[segment];int samples=Mathf.CeilToInt(Vector2.Distance(a,b)/4);
                            for(int j=0;j<=samples;j++){var at=Vector2.Lerp(a,b,j/(float)Mathf.Max(1,samples));if(Knowledge.Explored(at)&&r.Contains(P(at)))Dot(P(at),1,new Color(.68f,.57f,.36f));}
                        }
                    }
                }
                foreach(var d in s.deposits){Vector2 at=ResourceDisplayPoint(s,d);if(Knowledge.Explored(at)&&r.Contains(P(at)))Dot(P(at),3,d.kind==AtlasResource.Wood?new Color(.25f,.65f,.27f):d.kind==AtlasResource.Food?new Color(.87f,.72f,.32f):new Color(.67f,.7f,.72f));}
                Dot(P(s.point),4,OwnerColor(Knowledge.ReportedOwner(s.id)));
            }
            if(guardSite>=0&&Knowledge.Allied(Model.sites[guardSite].owner))for(int i=0;i<guards.Count;i++){var p=guards[i].root.position;var at=P(new Vector2(p.x,p.z));if(guards[i].root.gameObject.activeSelf&&AtlasGuardAlive(guards[i].root)&&r.Contains(at))Dot(at,i==0?4:guards[i].member==0?3:2,ResponseGuideHighlighted&&guards[i].platoon==ResponseGuidePlatoon?ResponseColor:i==0?new Color(1,.8f,.25f):guards[i].member==0?new Color(.8f,.87f,1):new Color(.5f,.8f,.65f));}
            if(defenseMode)foreach(var a in battleActors)if(a.enemy&&a.fighter.Alive)
            {
                Vector2 position=new Vector2(a.root.position.x,a.root.position.z);
                if(Knowledge.Visible(position)&&r.Contains(P(position)))Dot(P(position),3,new Color(1,.3f,.14f));
            }
            if(hasScoutReport&&r.Contains(P(reportedContact)))Dot(P(reportedContact),5,new Color(1,.3f,.14f));
            var player=P(new Vector2(hero.position.x,hero.position.z));Dot(player,4,Color.cyan);
            var forward=new Vector2(hero.forward.x,-hero.forward.z);Dot(player+forward*8,2,Color.cyan);
            if(ResponseGuideHighlighted)
            {
                var leader=GuardPosition(1+ResponseGuidePlatoon*6);
                DrawResponseMinimap(r,P(new Vector2(leader.x,leader.z)));
            }
            GUI.Label(new Rect(frame.x+10,frame.yMax-18,235,20),ResponseGuideHighlighted?"Amber P"+(ResponseGuidePlatoon+1)+": "+ResponseGuideName+" / "+ResponseGuideStage:hasScoutReport?"Contact report / "+reportAge.ToString("0")+"s old":"400 m / dark: unknown / dim: explored",new GUIStyle(small){fontSize=11});
        }
        private void DrawCommanderBadge()
        {
            if(guardSite<0||guards.Count==0||!guards[0].root.gameObject.activeSelf)return;
            // Hide badges behind HUD panels or an earlier badge instead of drawing partial labels.
            var occupied = new System.Collections.Generic.List<Rect>
            {
                new Rect(16,16,Width-32,60), new Rect(Width-330,90,314,74),
                new Rect(Width-273,Height-427,273,294), new Rect(0,Height-128,Width,128), ScoutHudRect
            };
            if(commanderPanel) occupied.Add(CommandHudRect);
            if(debug) occupied.Add(DiagnosticHudRect);
            for(int leader=0;leader<5;leader++)
            {
                int index=leader==0?0:1+(leader-1)*6;
                var target=guards[index].root.position+Vector3.up*2.3f;
                if(Vector3.Distance(hero.position,target)>55||Physics.Linecast(view.transform.position,target,ObstacleMask))continue;
                var p=view.WorldToScreenPoint(target);if(p.z<=0)continue;
                float x=p.x/UiScale,y=(Screen.height-p.y)/UiScale;
                float distance=Vector3.Distance(hero.position,target);
                bool response=ResponseGuideHighlighted && leader-1==ResponseGuidePlatoon;
                bool detail=response || distance<12 || (Mathf.Abs(x-Width*.5f)<70 && Mathf.Abs(y-Height*.5f)<100);
                var badge=detail?new Rect(x-65,y-43,130,43):new Rect(x-24,y-22,48,22);
                if(badge.xMin<0||badge.xMax>Width||badge.yMin<0||badge.yMax>Height)continue;
                bool overlaps=false;foreach(var panel in occupied)if(panel.Overlaps(badge)){overlaps=true;break;}
                if(overlaps)continue;
                occupied.Add(badge);Panel(badge);if(response)ResponseOutline(badge);
                float top=badge.y;
                if(index==0)
                {
                    GUI.DrawTexture(new Rect(x-17,top,14,14),rankStar);
                    GUI.DrawTexture(new Rect(x+3,top,14,14),rankStar);
                }
                else GUI.DrawTexture(new Rect(badge.x+3,top+3,14,14),rankStar);
                var centered=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,wordWrap=false,fontSize=11};
                if(detail)
                {
                    GUI.Label(new Rect(x-57,top+13,114,17),index==0?CommanderName:ActiveCompany.Platoons[(index-1)/6].Name,centered);
                    GUI.Label(new Rect(x-57,top+28,114,14),index==0?"COMPANY":response?"P"+(ResponseGuidePlatoon+1)+" / "+ResponseGuideStage.ToUpperInvariant():"PLATOON "+((index-1)/6+1),new GUIStyle(centered){fontSize=9});
                }
                else if(index>0)GUI.Label(new Rect(x-4,top,27,22),"P"+((index-1)/6+1),centered);
            }
        }
    }
}

