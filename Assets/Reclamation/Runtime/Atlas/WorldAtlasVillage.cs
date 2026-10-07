using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Atlas
{
    public sealed partial class WorldAtlasDemo
    {
        private const int ObstacleMask=1<<2;
        private readonly HashSet<int> developedVillages=new HashSet<int>();
        private readonly List<BoxCollider> hallRoofColliders=new List<BoxCollider>();
        private readonly List<Renderer> hallRoofs=new List<Renderer>();
        private readonly List<Vector3> hallCenters=new List<Vector3>();
        private readonly List<Quaternion> hallRotations=new List<Quaternion>();
        private readonly Dictionary<int,AtlasVillageLayout> villageLayouts=new Dictionary<int,AtlasVillageLayout>();
        public AtlasVillageLayout LayoutFor(int site){if(!villageLayouts.TryGetValue(site,out var layout)){layout=new AtlasVillageLayout((Model.Size==AtlasSize.Small?193:811)*7919+site*104729);layout.FitHinterland(Model,Model.sites[site]);villageLayouts[site]=layout;}return layout;}
        private readonly List<Light> lanterns=new List<Light>();
        private Light daylight,moonlight;
        private float clockHours=9,clockRate=1;
        private int day=1;
        public float Hour=>clockHours;
        public bool SurveyPaused {get=>paused;set=>paused=value;}
        public void TickVillage(float seconds){if(!paused){AdvanceVillageClock(seconds);UpdateVillagePeople(seconds);UpdateSettlementGuards(seconds);TickScoutEncounter(seconds);TickWildlife(seconds);}}
        public bool IsInsideTownHall { get; private set; }
        public bool AutomaticClock { get; set; }=true;
        public Vector3 CameraPosition=>view.transform.position;
        public string RoutineSummary=>clockHours<6||clockHours>=22?"Residents sleeping":clockHours<8?"Breakfast / travelling to work":clockHours<17?"Fields / woodcutting / quarry / market":clockHours<21.5f?"Tavern / evening market":"Returning home";
        public void SetHour(float value){clockHours=Mathf.Repeat(value,24);UpdateVillagePeople(0);UpdateSettlementGuards(0);UpdateDaylight();UpdateWatchTorches();}
        private void AdvanceVillageClock(float seconds)
        {
            if(AutomaticClock){clockHours+=seconds*clockRate/60;while(clockHours>=24){clockHours-=24;day++;}}
            UpdateDaylight();
        }
        private BoxCollider Obstacle(string name,Vector3 center,Vector3 size,Quaternion rotation=default,Transform parent=null)
        {
            var go=new GameObject(name+" / solid boundary");go.layer=2;go.transform.SetParent(parent?parent:world,false);go.transform.position=center;go.transform.rotation=rotation==default?Quaternion.identity:rotation;
            var box=go.AddComponent<BoxCollider>();box.size=size;return box;
        }
        private void PlaceSolid(string name,Vector2 position,float yaw=0)
        {
            Vector3 ground=new Vector3(position.x,Surface(position),position.y);art.Place(name,ground,yaw);
            Vector3 size=name=="Tower"?new Vector3(6.8f,9.4f,6.8f):name=="Well"?new Vector3(3.4f,4.4f,3):name=="Market"?new Vector3(5.4f,1.6f,2.4f):new Vector3(8.3f,name=="Cottage2"||name=="Storehouse"||name=="Barracks"?8.4f:6.8f,7.3f);
            var rotation=Quaternion.Euler(0,yaw,0);
            Obstacle(name,ground+Vector3.up*size.y*.5f,size,rotation);
            if(name=="Market")
            {
                Obstacle("Market awning",ground+Vector3.up*3.6f,new Vector3(5.7f,.18f,4.1f),rotation*Quaternion.Euler(12,0,0));
                foreach(float x in new[]{-2.5f,2.5f})foreach(float z in new[]{-1.6f,1.6f})Obstacle("Market post",ground+rotation*new Vector3(x,1.8f,z),new Vector3(.18f,3.6f,.18f),rotation);
            }
        }
        private void BuildLane(Vector2 a,Vector2 b,float width,Material material)
        {
            float length=Vector2.Distance(a,b);
            if(length<.01f)return;
            SurfacePatch((a+b)*.5f,width,length+.12f,Mathf.Atan2(b.x-a.x,b.y-a.y)*Mathf.Rad2Deg,new Color(.44f,.35f,.23f),.16f);
        }

        private void BuildInhabitedSite(AtlasSite site,Material path)
        {
            developedVillages.Add(site.id);var center=site.point;var layout=LayoutFor(site.id);
            Vector2 P(float x,float z)=>center+layout.Rotate(new Vector2(x,z));
            BuildLane(P(0,layout.HouseCount>6?-52:-38),P(0,33),layout.Style==1?6:4,path);
            BuildLane(P(-35,0),P(35,0),3,path);
            for(int i=0;i<layout.HouseCount;i++)
            {
                Vector2 home=center+layout.House(i);float facing=(i%2==0?-90:90)+layout.Angle;
                PlaceSolid(i==0?"Storehouse":"Cottage"+((i+layout.Seed)%3),home,facing);
                if(i<6)BuildLane(center+layout.Road(i),center+layout.Door(i),2,path);
                art.Place("Garden",new Vector3(home.x,Surface(home),home.y)+Quaternion.Euler(0,layout.Angle,0)*new Vector3(0,0,6),layout.Angle);
            }
            PlaceSolid("Cottage2",center+layout.Tavern,layout.Angle-90);
            art.Place("TavernSign",Ground(P(-14.5f,27),1),layout.Angle);
            PlaceSolid("Market",center+layout.Market,layout.Angle);PlaceSolid("Well",P(-7,-3));
            BuildLane(P(-13,22),P(13,22),3,path);
            BuildHinterland(site,path);BuildGuardBuildings(site,path);
            for(int i=0;i<layout.HouseCount;i++)
            {
                Vector2 home=center+layout.House(i);Vector2 rear=layout.Rotate(new Vector2(i%2==0?-6:6,-2));
                art.Place("Yard",Ground(home+rear),layout.Angle+i*37);
                for(int j=0;j<4;j++){Vector2 grass=home+layout.Rotate(new Vector2(-3+j*2,5.1f));art.Place("GrassEdge",Ground(grass),i*41+j*67,Vector3.one*(.7f+(i+j)%3*.25f));}
            }
            BuildTownHall(center+layout.Hall,layout.Angle);
            foreach(var point in new[]{new Vector2(-4,-16),new Vector2(4,15),new Vector2(-4,30),new Vector2(-13,28),new Vector2(14,28)})BuildLantern(center+layout.Rotate(point));
        }
        private void BuildLantern(Vector2 p)
        {
            art.Place("Lantern",Ground(p));var glow=new GameObject("Village lantern light").AddComponent<Light>();glow.transform.SetParent(world,false);glow.transform.position=Ground(p,3.1f);glow.type=LightType.Point;glow.color=new Color(1,.66f,.29f);glow.range=18;glow.intensity=3;glow.shadows=LightShadows.None;lanterns.Add(glow);
        }
        private void BuildTownHall(Vector2 p,float angle)
        {
            Vector3 ground=new Vector3(p.x,Surface(p),p.y);hallCenters.Add(ground);var rotation=Quaternion.Euler(0,angle,0);hallRotations.Add(rotation);
            var plaster=Mat(new Color(.78f,.73f,.58f));var wood=Mat(new Color(.29f,.17f,.1f));var stone=Mat(new Color(.43f,.45f,.42f));
            void Wall(string name,Vector3 offset,Vector3 size,Material material)
            {var shape=Shape(name,PrimitiveType.Cube,ground+rotation*offset,size,material);shape.rotation=rotation;Obstacle(name,ground+rotation*offset,size,rotation);}
            Shape("Town hall floor",PrimitiveType.Cube,ground+Vector3.up*.015f,new Vector3(14,.03f,10),wood).rotation=rotation;
            Wall("Town hall west wall",new Vector3(-7,2.5f,0),new Vector3(.45f,5,10),plaster);
            Wall("Town hall east wall",new Vector3(7,2.5f,0),new Vector3(.45f,5,10),plaster);
            Wall("Town hall back wall",new Vector3(0,2.5f,5),new Vector3(14,5,.45f),plaster);
            Wall("Doorway left",new Vector3(-4.3f,2.5f,-5),new Vector3(5.4f,5,.45f),plaster);
            Wall("Doorway right",new Vector3(4.3f,2.5f,-5),new Vector3(5.4f,5,.45f),plaster);
            Wall("Doorway lintel",new Vector3(0,4.1f,-5),new Vector3(3.2f,1.8f,.45f),plaster);
            foreach(float x in new[]{-1.65f,1.65f})Shape("Door jamb",PrimitiveType.Cube,ground+rotation*new Vector3(x,1.6f,-5.3f),new Vector3(.2f,3.2f,.2f),wood).rotation=rotation;
            foreach(float x in new[]{-6.9f,6.9f})Shape("Hall timber post",PrimitiveType.Cube,ground+rotation*new Vector3(x,2.5f,-5.25f),new Vector3(.22f,5,.22f),wood).rotation=rotation;
            foreach(float y in new[]{3.3f,4.9f})Shape("Hall crossbeam",PrimitiveType.Cube,ground+rotation*new Vector3(0,y,-5.26f),new Vector3(14,.18f,.2f),wood).rotation=rotation;
            foreach(float x in new[]{-4.5f,4.5f})
            {
                Shape("Hall window frame",PrimitiveType.Cube,ground+rotation*new Vector3(x,2,-5.26f),new Vector3(1.65f,1.8f,.14f),wood).rotation=rotation;
                Shape("Hall window",PrimitiveType.Cube,ground+rotation*new Vector3(x,2,-5.35f),new Vector3(1.4f,1.55f,.05f),stone).rotation=rotation;
                Shape("Hall window mullion",PrimitiveType.Cube,ground+rotation*new Vector3(x,2,-5.4f),new Vector3(.1f,1.6f,.06f),wood).rotation=rotation;
            }
            var roof=art.Object("HallRoof",world,ground+Vector3.up*5);roof.rotation=rotation;hallRoofs.Add(roof.GetComponent<Renderer>());hallRoofColliders.Add(Obstacle("Town hall roof",ground+Vector3.up*6.75f,new Vector3(15,3.5f,11),rotation));
            foreach(float x in new[]{-4f,4f})
            {
                art.Place("Bench",ground+rotation*new Vector3(x,0,0),angle);Obstacle("Council bench",ground+rotation*new Vector3(x,.5f,0),new Vector3(1.2f,1,3),rotation);
            }
            art.Place("CouncilTable",ground+rotation*new Vector3(0,0,2.7f),angle);Obstacle("Council table",ground+rotation*new Vector3(0,.65f,2.7f),new Vector3(3,1.3f,1.3f),rotation);
            Shape("Council rug",PrimitiveType.Cube,ground+rotation*new Vector3(0,.04f,.2f),new Vector3(3.5f,.025f,4),Mat(new Color(.28f,.35f,.43f))).rotation=rotation;
            Shape("Civic crest",PrimitiveType.Cube,ground+rotation*new Vector3(0,3,4.73f),new Vector3(1.5f,2,.08f),Mat(new Color(.62f,.38f,.16f))).rotation=rotation;
            art.Place("HallSign",ground+rotation*new Vector3(-2.7f,2.1f,-5.4f),angle);
            BuildLantern(p+new Vector2((rotation*new Vector3(-5,0,1)).x,(rotation*new Vector3(-5,0,1)).z));BuildLantern(p+new Vector2((rotation*new Vector3(5,0,1)).x,(rotation*new Vector3(5,0,1)).z));
        }
        private void UpdateDaylight()
        {
            if(!daylight)return;
            float altitude=Mathf.Sin((clockHours-6)/24*Mathf.PI*2);float brightness=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.12f,.3f,altitude));
            daylight.transform.rotation=Quaternion.Euler((clockHours-6)*15, -35,0);daylight.intensity=1.3f*brightness;daylight.color=Color.Lerp(new Color(1,.59f,.33f),new Color(1,.96f,.86f),Mathf.Clamp01(altitude*3));
            moonlight.intensity=.16f*(1-brightness);RenderSettings.ambientLight=Color.Lerp(new Color(.16f,.2f,.29f),new Color(.55f,.6f,.65f),brightness);
            Color sky=Color.Lerp(new Color(.035f,.06f,.12f),new Color(.65f,.77f,.79f),brightness);view.backgroundColor=sky;RenderSettings.fogColor=sky;
            foreach(var light in lanterns)light.enabled=brightness<.65f&&(light.transform.position-hero.position).sqrMagnitude<110*110;
            IsInsideTownHall=false;
            for(int i=0;i<hallCenters.Count;i++){Vector3 delta=Quaternion.Inverse(hallRotations[i])*(hero.position-hallCenters[i]);bool inside=Mathf.Abs(delta.x)<6.8f&&Mathf.Abs(delta.z)<4.8f;hallRoofs[i].enabled=!inside;hallRoofColliders[i].enabled=!inside;IsInsideTownHall|=inside;}
        }
        private bool BlocksResidentRoute(int siteId,Vector2 point)
        {
            if(!developedVillages.Contains(siteId))return false;
            Vector2 local=point-Model.sites[siteId].point;var layout=LayoutFor(siteId);
            bool Near(Vector2 a,Vector2 b)
            {
                Vector2 delta=b-a;float t=delta.sqrMagnitude>.001f?Mathf.Clamp01(Vector2.Dot(local-a,delta)/delta.sqrMagnitude):0;
                return Vector2.Distance(local,a+delta*t)<7;
            }
            for(int i=0;i<18;i++)
            {
                if(Near(layout.Door(i),layout.Road(i))||Near(layout.Road(i),Vector2.zero)||Near(Vector2.zero,layout.SocialRoad(i))||Near(layout.SocialRoad(i),layout.Social(i)))return true;
                Vector2[] road=layout.WorkRoad(i);for(int j=1;j<road.Length;j++)if(Near(road[j-1],road[j]))return true;
            }
            return false;
        }
        public bool IsSolidAt(Vector3 ground)
        {return Physics.CheckCapsule(ground+Vector3.up*.4f,ground+Vector3.up*1.4f,.34f,ObstacleMask,QueryTriggerInteraction.Ignore);}
        public Vector3 ResolveVillageMove(Vector3 from,Vector3 displacement)
        {
            Vector3 position=from;
            for(int iteration=0;iteration<3&&displacement.sqrMagnitude>.000001f;iteration++)
            {
                float distance=displacement.magnitude;Vector3 direction=displacement/distance;
                if(!Physics.CapsuleCast(position+Vector3.up*.4f,position+Vector3.up*1.4f,.34f,direction,out var hit,distance+.025f,ObstacleMask,QueryTriggerInteraction.Ignore)){position+=displacement;break;}
                float travel=Mathf.Max(0,hit.distance-.025f);position+=direction*travel;displacement=Vector3.ProjectOnPlane(displacement-direction*travel,hit.normal);displacement.y=0;
            }
            return position;
        }
        public Vector3 ResolveVillageCamera(Vector3 target,Vector3 desired)
        {
            Vector3 delta=desired-target;
            if(delta.magnitude>.01f&&Physics.SphereCast(target,.22f,delta.normalized,out var hit,delta.magnitude,ObstacleMask,QueryTriggerInteraction.Ignore))return target+delta.normalized*Mathf.Max(.35f,hit.distance-.12f);
            return desired;
        }
    }
}
