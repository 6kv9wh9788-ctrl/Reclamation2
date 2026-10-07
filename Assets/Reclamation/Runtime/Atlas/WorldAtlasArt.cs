using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Atlas
{
    public sealed partial class WorldAtlasDemo
    {
        private AtlasArtKit art;
        private readonly List<VillagerVisual> villagers=new List<VillagerVisual>();
        private float villageTime;
        public int ActiveVillagerCount { get; private set; }
        public int SceneryInstanceCount=>art==null?0:art.StaticInstances;
        private sealed class VillagerVisual { public Transform root,torso,head,leftArm,rightArm,leftElbow,rightElbow,leftLeg,rightLeg,basket,load; public Transform[] props; public AtlasSyntyVisual synty; }
        private void BuildVillagePeople()
        {
            villagers.Clear();villageTime=0;ActiveVillagerCount=0;
            for(int i=0;i<18;i++)
            {
                int variant=i%3;var v=new VillagerVisual();v.root=new GameObject("Resident / "+(variant==0?"field worker":variant==1?"woodcutter / miner":"merchant")).transform;v.root.SetParent(world,false);
                v.torso=new GameObject("Upper body").transform;v.torso.SetParent(v.root,false);v.torso.localPosition=Vector3.up*.85f;art.Object("Body"+variant,v.torso,Vector3.down*.85f);v.head=art.Object("Head"+variant,v.torso,new Vector3(0,.88f,0));
                v.leftArm=art.Object("UpperArm"+variant,v.torso,new Vector3(-.35f,.54f,0));v.rightArm=art.Object("UpperArm"+variant,v.torso,new Vector3(.35f,.54f,0));
                v.leftElbow=art.Object("Forearm"+variant,v.leftArm,new Vector3(0,-.3f,0));v.rightElbow=art.Object("Forearm"+variant,v.rightArm,new Vector3(0,-.3f,0));
                v.props=new Transform[5];string[] names={"Hoe","Axe","Pick","Mug","Bread"};
                for(int prop=0;prop<names.Length;prop++){v.props[prop]=art.Object(names[prop],v.rightElbow,new Vector3(0,-.27f,.08f));v.props[prop].gameObject.SetActive(false);}
                v.leftLeg=art.Object("Leg",v.root,new Vector3(-.135f,.68f,0));v.rightLeg=art.Object("Leg",v.root,new Vector3(.135f,.68f,0));
                v.load=art.Object(i%6==1?"LogBundle":"OreLoad",v.root,new Vector3(0,1,.42f));v.load.gameObject.SetActive(false);v.basket=art.Object("Basket",v.root,new Vector3(0,1,.42f));v.root.localScale=Vector3.one*(.94f+(i%4)*.035f);v.root.gameObject.SetActive(false);villagers.Add(v);
                if(SyntyRoleTrial&&i<3)
                {
                    v.root.gameObject.SetActive(true);
                    var original=new List<Renderer>();foreach(var renderer in v.root.GetComponentsInChildren<Renderer>(true))
                    {
                        bool prop=renderer.transform.IsChildOf(v.basket)||renderer.transform.IsChildOf(v.load);
                        foreach(var tool in v.props)prop|=renderer.transform.IsChildOf(tool);
                        if(!prop)original.Add(renderer);
                    }
                    var actor=new GameObject("Synty resident");actor.transform.SetParent(v.root,false);
                    v.synty=actor.AddComponent<AtlasSyntyVisual>();v.synty.Build(null,false,(SyntyRole)((int)SyntyRole.Farmer+i),i);v.synty.HideOriginal(original);
                    v.root.gameObject.SetActive(false);
                }
            }
        }
        private void UpdateVillagePeople(float dt)
        {
            villageTime+=dt*clockRate;ActiveVillagerCount=0;
            var site=Model.Nearest(new Vector2(hero.position.x,hero.position.z));
            var layout=LayoutFor(site.id);
            bool inhabited=developedVillages.Contains(site.id)&&(site.owner==-2||site.owner>=0&&Model.factions[site.owner].family==0);
            bool near=Vector2.Distance(site.point,new Vector2(hero.position.x,hero.position.z))<185;
            for(int i=0;i<villagers.Count;i++)
            {
                var v=villagers[i];var activity=AtlasVillageRoutine.Activity(clockHours,i);bool active=near&&inhabited&&activity!=VillageActivity.Sleeping;
                v.root.gameObject.SetActive(active);if(!active)continue;ActiveVillagerCount++;
                Vector2 local=AtlasVillageRoutine.Position(clockHours,i,layout),p=site.point+local;
                v.root.position=new Vector3(p.x,Surface(p),p.y);
                var motion=AtlasVillageRoutine.PresentationMotion(clockHours,i);bool walking=motion==VillageMotion.Walking||motion==VillageMotion.Carrying;
                Vector2 next=AtlasVillageRoutine.Position(clockHours+.001f,i,layout);Vector2 direction=walking?next-local:activity==VillageActivity.Working?(i%3==2?layout.Rotate(i/3<3?Vector2.down:Vector2.up):layout.WorkFacing(i)):activity==VillageActivity.Socializing?layout.SocialFocus(i)-local:layout.Rotate(i%2==0?Vector2.left:Vector2.right);
                if(direction.sqrMagnitude>.000001f){Quaternion facing=Quaternion.LookRotation(new Vector3(direction.x,0,direction.y));v.root.rotation=dt==0?facing:Quaternion.Slerp(v.root.rotation,facing,Mathf.Min(1,dt*clockRate*6));}
                PoseVillager(v,motion,villageTime*(.88f+(i*17%11)*.025f)+i*1.73f,walking,i);
                if(v.synty)
                {
                    v.synty.SampleCivilian(v.torso,v.leftArm,v.leftElbow,v.rightArm,v.rightElbow,walking,dt);
                    foreach(var prop in v.props)if(prop.gameObject.activeSelf)prop.position=v.synty.RightPalm;
                }

            }
        }
        private static void PoseVillager(VillagerVisual v,VillageMotion motion,float t,bool walking,int resident)
        {
            float swing=walking?Mathf.Sin(t*7)*23:0;
            v.leftLeg.localRotation=Quaternion.Euler(swing,0,0);v.rightLeg.localRotation=Quaternion.Euler(-swing,0,0);
            float left=-swing*.7f,right=swing*.7f,leftElbow=-8,rightElbow=-8,lean=0;int prop=-1;
            float cycle=Mathf.Repeat(t*.36f,1),beat=(1-Mathf.Cos(cycle*Mathf.PI*2))*.5f;
            bool carry=motion==VillageMotion.Carrying||walking&&resident%3==2;v.basket.gameObject.SetActive(carry&&resident%3!=1);v.load.gameObject.SetActive(carry&&resident%3==1);
            if(carry){left=right=-35;leftElbow=rightElbow=-65;}
            switch(motion)
            {
                case VillageMotion.Farming:prop=0;right=-5-beat*30;rightElbow=-10;left=-25;lean=8+beat*8;break;
                case VillageMotion.Chopping:case VillageMotion.Mining:
                    prop=motion==VillageMotion.Chopping?1:2;right=-30-beat*100;rightElbow=-25;left=-25-beat*35;leftElbow=-35;lean=beat*5;break;
                case VillageMotion.Trading:
                    float gesture=Mathf.Sin(t*2);right=-40-gesture*18;rightElbow=-65;left=-15+gesture*10;leftElbow=-35;break;
                case VillageMotion.Drinking:case VillageMotion.Eating:
                    prop=motion==VillageMotion.Drinking?3:4;right=-20-beat*80;rightElbow=-55+beat*10;left=-15;leftElbow=-30;lean=motion==VillageMotion.Drinking?-beat*5:beat*4;break;
            }
            v.leftArm.localRotation=Quaternion.Euler(left,0,8);v.rightArm.localRotation=Quaternion.Euler(right,motion==VillageMotion.Drinking||motion==VillageMotion.Eating?-beat*35:0,-8);
            v.leftElbow.localRotation=Quaternion.Euler(leftElbow,0,0);v.rightElbow.localRotation=Quaternion.Euler(rightElbow,0,0);
            v.torso.localRotation=Quaternion.Euler(lean,0,0);
            v.head.localRotation=Quaternion.Euler(lean*.3f,walking?0:Mathf.Sin(t*.8f)*8,0);
            for(int i=0;i<v.props.Length;i++){v.props[i].gameObject.SetActive(i==prop);v.props[i].localRotation=Quaternion.Euler(i<3?180:-(lean+right+rightElbow)-(motion==VillageMotion.Drinking?beat*35:0),0,0);}
        }
        public void VisitVillage()
        {
            var site=Model.sites[selected];Vector2 p=site.point+LayoutFor(site.id).Rotate(new Vector2(0,-35));
            hero.position=new Vector3(p.x,Surface(p),p.y);yaw=LayoutFor(site.id).Angle;pitch=18;zoom=12;mapOpen=false;UpdateCamera(true);
            message="Village main street. Town hall straight ahead; tavern and market by the square. T advances three hours for review.";
            UpdateVillagePeople(0);UpdateDaylight();
        }
        public void VisitTownHall()
        {
            if(!developedVillages.Contains(selected)){message="This site has no developed town hall yet.";return;}
            var site=Model.sites[selected];Vector2 p=site.point+LayoutFor(site.id).Rotate(new Vector2(0,28));
            hero.position=new Vector3(p.x,Surface(p),p.y);yaw=LayoutFor(site.id).Angle;pitch=18;zoom=8;mapOpen=false;UpdateCamera(true);UpdateDaylight();
            message="Town hall: walk through the open doorway. Civic occupation is a future objective; entering does not capture the settlement.";
        }
    }
}
