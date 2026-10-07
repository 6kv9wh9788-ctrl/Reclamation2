using System.Collections.Generic;
using UnityEngine;
namespace Reclamation.Atlas
{
    public enum AtlasAnimalKind { Rabbit, Deer, Bird }
    public enum AtlasAnimalState { Feeding, Wandering, Resting, Fleeing }
    public sealed partial class WorldAtlasDemo
    {
        private sealed class Animal
        {
            public Transform root,head; public readonly List<Transform> limbs=new List<Transform>();
            public AtlasAnimalKind kind;public AtlasAnimalState state;public Vector2 home,point,target;
            public System.Random random;public float phase,timer,flight;public int id;
        }
        private readonly List<Animal> animals=new List<Animal>();
        private readonly List<(Vector2 a,Vector2 b)> wildlifeRoads=new List<(Vector2,Vector2)>();
        private int wildlifeSite=-1,wildlifeReview;
        public int ActiveWildlifeCount {get;private set;}
        public int WildlifeFleeCount {get;private set;}
        public Vector3 WildlifePosition(int index)=>animals[index].root.position;
        public AtlasAnimalState WildlifeState(int index)=>animals[index].state;
        public AtlasAnimalKind WildlifeKind(int index)=>animals[index].kind;
        private void BuildWildlife()
        {
            animals.Clear();wildlifeSite=-1;wildlifeReview=0;ActiveWildlifeCount=0;
            for(int i=0;i<18;i++)
            {
                var a=new Animal{id=i,kind=i<6?AtlasAnimalKind.Rabbit:i<10?AtlasAnimalKind.Deer:AtlasAnimalKind.Bird};
                a.root=new GameObject(a.kind+" / habitat wildlife "+i).transform;a.root.SetParent(world,false);
                art.Object(a.kind+"Body",a.root,Vector3.zero);
                if(a.kind!=AtlasAnimalKind.Bird)
                {
                    a.head=art.Object(a.kind+"Head",a.root,a.kind==AtlasAnimalKind.Deer?new Vector3(0,1.1f,.38f):new Vector3(0,.37f,.23f));
                    if(a.kind==AtlasAnimalKind.Deer&&i%2==0)art.Object("Antlers",a.head,Vector3.zero);
                    for(int leg=0;leg<4;leg++)a.limbs.Add(art.Object(a.kind+"Leg",a.root,a.kind==AtlasAnimalKind.Deer?new Vector3(leg%2==0?-.19f:.19f,.76f,leg<2?.37f:-.38f):new Vector3(leg%2==0?-.12f:.12f,.085f,leg<2?.16f:-.16f)));
                }
                else for(int side=0;side<2;side++){var wing=art.Object("BirdWing",a.root,new Vector3(side==0?-.07f:.07f,.17f,0));wing.localScale=new Vector3(side==0?-1:1,1,1);a.limbs.Add(wing);}
                a.root.gameObject.SetActive(false);animals.Add(a);
            }
        }
        private bool WildlifeClear(Vector2 p,int site)
        {
            if(!Model.Walkable(p)||IsSolidAt(HinterlandGround(p)))return false;
            var local=p-Model.sites[site].point;var layout=LayoutFor(site);
            // Keep animals out of the inhabited center and away from busy work routes.
            if(local.magnitude<65)return false;
            if(developedVillages.Contains(site))
            {
                for(int field=0;field<3;field++){var offset=p-Model.sites[site].point-layout.Field(field);var aligned=Quaternion.Euler(0,-layout.HinterlandAngle,0)*new Vector3(offset.x,0,offset.y);var size=layout.FieldSize(field);if(Mathf.Abs(aligned.x)<size.x*.5f+2&&Mathf.Abs(aligned.z)<size.y*.5f+2)return false;}
                var h=Quaternion.Euler(0,-layout.HinterlandAngle,0)*new Vector3(local.x,0,local.y);
                if(Mathf.Pow((h.x-115)/18,2)+Mathf.Pow((h.z+57)/31,2)<1.1f)return false;
                if(new Vector2(h.x-106,h.z-49.5f).sqrMagnitude<19*19)return false;
            }
            foreach(var road in wildlifeRoads){Vector2 delta=road.b-road.a;float t=delta.sqrMagnitude>.001f?Mathf.Clamp01(Vector2.Dot(p-road.a,delta)/delta.sqrMagnitude):0;if(Vector2.Distance(p,road.a+delta*t)<3)return false;}
            return true;
        }
        private bool WildlifeLane(Vector2 a,Vector2 b,int site)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.7f));
            for(int i=1;i<=steps;i++)if(!WildlifeClear(Vector2.Lerp(a,b,i/(float)steps),site))return false;
            return true;
        }
        private Vector2 Habitat(int site,AtlasAnimalKind kind)
        {
            var s=Model.sites[site];var layout=LayoutFor(site);
            return s.point+layout.Hinterland(kind==AtlasAnimalKind.Deer?new Vector2(98,-93):kind==AtlasAnimalKind.Rabbit?new Vector2(115,0):new Vector2(115,20));
        }
        private void PlaceWildlife(int site)
        {
            wildlifeSite=site;wildlifeRoads.Clear();
            if(developedVillages.Contains(site))for(int i=0;i<18;i++){var route=LayoutFor(site).WorkRoad(i);for(int j=1;j<route.Length;j++)wildlifeRoads.Add((Model.sites[site].point+route[j-1],Model.sites[site].point+route[j]));}
            foreach(var a in animals)
            {
                a.random=new System.Random(LayoutFor(site).Seed^((a.id+1)*8353));a.phase=(float)a.random.NextDouble()*6;a.timer=2+(float)a.random.NextDouble()*5;a.flight=0;
                var anchor=Habitat(site,a.kind);bool found=false;
                for(int attempt=0;attempt<160;attempt++)
                {
                    float angle=(float)a.random.NextDouble()*Mathf.PI*2,rad=3+(float)a.random.NextDouble()*18;
                    var p=anchor+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*rad;
                    if(!WildlifeClear(p,site))continue;
                    bool crowded=false;foreach(var other in animals)if(other.id<a.id&&other.root.gameObject.activeSelf&&Vector2.Distance(other.point,p)<1.6f)crowded=true;
                    if(crowded)continue;a.home=a.point=a.target=p;found=true;break;
                }
                a.state=AtlasAnimalState.Feeding;a.root.gameObject.SetActive(found);if(found)PoseAnimal(a,0,false);
            }
        }
        private void TickWildlife(float seconds)
        {
            if(paused||Model==null)return;
            var near=Model.Nearest(new Vector2(hero.position.x,hero.position.z));
            bool active=Vector2.Distance(near.point,new Vector2(hero.position.x,hero.position.z))<260;
            if(!active){foreach(var a in animals)a.root.gameObject.SetActive(false);wildlifeSite=-1;ActiveWildlifeCount=WildlifeFleeCount=0;return;}
            if(wildlifeSite!=near.id)PlaceWildlife(near.id);
            ActiveWildlifeCount=WildlifeFleeCount=0;
            float duration=Mathf.Min(Mathf.Max(0,seconds)*clockRate,1f);int steps=Mathf.Max(1,Mathf.CeilToInt(duration/.1f));float dt=duration/steps;
            foreach(var a in animals)
            {
                if(!a.root.gameObject.activeSelf)continue;ActiveWildlifeCount++;
                bool moved=false;
                for(int step=0;step<steps;step++)
                {
                    Vector2 threat=new Vector2(hero.position.x,hero.position.z);float closest=Vector2.Distance(threat,a.point);
                    foreach(var g in guards)if(g.root.gameObject.activeSelf){var p=new Vector2(g.root.position.x,g.root.position.z);float d=Vector2.Distance(p,a.point);if(d<closest){closest=d;threat=p;}}
                    foreach(var v in villagers)if(v.root.gameObject.activeSelf){var p=new Vector2(v.root.position.x,v.root.position.z);float d=Vector2.Distance(p,a.point);if(d<closest){closest=d;threat=p;}}
                    foreach(var scout in hostileScouts)if(scout&&scout.gameObject.activeSelf){var p=new Vector2(scout.transform.position.x,scout.transform.position.z);float d=Vector2.Distance(p,a.point);if(d<closest){closest=d;threat=p;}}
                    float fear=a.kind==AtlasAnimalKind.Deer?15:7;
                    bool night=clockHours<6||clockHours>=20;
                    if(closest<fear)
                    {
                        a.state=AtlasAnimalState.Fleeing;a.timer=3;
                        Vector2 away=(a.point-threat).normalized;if(away.sqrMagnitude<.01f)away=Vector2.right;
                        a.target=a.point+away*10;
                    }
                    else if(a.timer<=0)
                    {
                        a.state=night?AtlasAnimalState.Resting:(a.random.Next(3)==0?AtlasAnimalState.Feeding:AtlasAnimalState.Wandering);
                        a.timer=3+(float)a.random.NextDouble()*6;
                        float angle=(float)a.random.NextDouble()*Mathf.PI*2;
                        a.target=a.home+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(3+(float)a.random.NextDouble()*14);
                    }
                    if(night&&a.state!=AtlasAnimalState.Fleeing)a.state=AtlasAnimalState.Resting;
                    a.timer-=dt;
                    bool moving=a.state==AtlasAnimalState.Fleeing||a.state==AtlasAnimalState.Wandering;
                    if(moving&&dt>0)
                    {
                        float speed=a.state==AtlasAnimalState.Fleeing?(a.kind==AtlasAnimalKind.Deer?5:a.kind==AtlasAnimalKind.Bird?6:3.8f):a.kind==AtlasAnimalKind.Deer?.7f:.4f;
                        Vector2 next=Vector2.MoveTowards(a.point,a.target,speed*dt);
                        if(WildlifeLane(a.point,next,wildlifeSite))
                        {Vector2 direction=next-a.point;if(direction.sqrMagnitude>.000001f){a.root.rotation=Quaternion.LookRotation(new Vector3(direction.x,0,direction.y));moved=true;}a.point=next;}
                        else {a.timer=0;a.target=a.point;a.state=AtlasAnimalState.Feeding;}
                        if(Vector2.Distance(a.point,a.target)<.1f){a.state=AtlasAnimalState.Feeding;a.timer=2;}
                    }
                    a.phase+=dt*(a.state==AtlasAnimalState.Fleeing?13:5);
                    a.flight=Mathf.MoveTowards(a.flight,a.kind==AtlasAnimalKind.Bird&&a.state==AtlasAnimalState.Fleeing?2.8f:0,dt*2);
                }
                if(a.state==AtlasAnimalState.Fleeing)WildlifeFleeCount++;
                PoseAnimal(a,duration,moved);
            }
        }
        private void PoseAnimal(Animal a,float seconds,bool moving)
        {
            float hop=a.kind==AtlasAnimalKind.Rabbit&&moving?Mathf.Abs(Mathf.Sin(a.phase))*.13f:0;
            a.root.position=HinterlandGround(a.point)+Vector3.up*(hop+a.flight);
            if(a.head){bool feeding=a.state==AtlasAnimalState.Feeding;a.head.localPosition=a.kind==AtlasAnimalKind.Deer?new Vector3(0,feeding?.85f:1.1f,.38f):new Vector3(0,feeding?.22f:.37f,.23f);a.head.localRotation=Quaternion.Euler(feeding?(a.kind==AtlasAnimalKind.Deer?130:30)+Mathf.Sin(a.phase)*5:0,0,0);}
            for(int i=0;i<a.limbs.Count;i++)a.limbs[i].localRotation=a.kind==AtlasAnimalKind.Bird?(a.flight>.05f?Quaternion.Euler(0,0,(i==0?-1:1)*Mathf.Sin(a.phase*2)*55):Quaternion.Euler(0,i==0?-90:90,0)):Quaternion.Euler(moving?Mathf.Sin(a.phase+(i%3==0?0:Mathf.PI))*25:0,0,0);
        }
        public void VisitWildlife()
        {
            if(paused)return;
            if(wildlifeSite<0)TickWildlife(0);
            var kind=(AtlasAnimalKind)(wildlifeReview++%3);Animal chosen=animals.Find(a=>a.kind==kind&&a.root.gameObject.activeSelf);
            if(chosen==null){message="No safe habitat nearby. Visit a village and try J again.";return;}
            Vector2 p=chosen.point-Vector2.up*(kind==AtlasAnimalKind.Deer?20:11);
            if(!Model.Walkable(p)||IsSolidAt(HinterlandGround(p))){message="Wildlife viewpoint obstructed. Approach on foot.";return;}
            hero.position=HinterlandGround(p);yaw=0;pitch=17;zoom=kind==AtlasAnimalKind.Deer?12:8;mapOpen=false;UpdateCamera(true);
            message="Wildlife: "+kind+" habitat. Approach slowly, then walk closer to see them flee. J cycles habitats.";
        }
    }
}


