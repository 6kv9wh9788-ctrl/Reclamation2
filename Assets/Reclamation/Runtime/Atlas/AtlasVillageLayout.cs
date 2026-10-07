using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Atlas
{
    // Independent streams keep cosmetic/layout changes from altering combat or strategic randomness.
    public sealed class AtlasVillageLayout
    {
        public readonly int Seed, Style, HouseCount;
        public readonly float Angle;
        public readonly string Name;
        private readonly Vector2[] houses=new Vector2[8];
        public AtlasVillageLayout(int seed)
        {
            Seed=seed;var random=new System.Random(seed);Style=random.Next(3);Angle=random.Next(4)*90;HouseCount=Style==2?8:6;
            Name=new[]{"Market street","Village green","Crossroads"}[Style];
            float width=Style==1?23:Style==2?17:19;
            for(int i=0;i<8;i++)houses[i]=new Vector2((i%2==0?-1:1)*(width+(float)random.NextDouble()*4),new[]{-27f,-12f,8f,-43f}[i/2]+((float)random.NextDouble()-.5f)*4);
        }
        public Vector2 Rotate(Vector2 p){Vector3 v=Quaternion.Euler(0,Angle,0)*new Vector3(p.x,0,p.y);return new Vector2(v.x,v.z);}
        public Vector2 House(int i)=>Rotate(houses[i%HouseCount]);
        public Vector2 Door(int i){Vector2 p=houses[i%6];p.x+=p.x<0?5:-5;p.y+=(i/6-1)*.8f;return Rotate(p);}
        public Vector2 Road(int i)=>Rotate(new Vector2(0,houses[i%6].y));
        public Vector2 Hall=>Rotate(new Vector2(0,38));
        public Vector2 Tavern=>Rotate(new Vector2(-19,25));
        public Vector2 Market=>Rotate(new Vector2(19,25));
        public float HinterlandAngle { get; private set; }
        public Vector2 Hinterland(Vector2 p){Vector3 v=Quaternion.Euler(0,HinterlandAngle,0)*new Vector3(p.x,0,p.y);return new Vector2(v.x,v.z);}
        public Vector2 FieldSize(int i)=>new Vector2(22+(Seed+i*17)%7,22+(Seed/7+i*13)%5);
        public float FieldEdge(int field,float along)=>1+.09f*Mathf.Sin(along*3.1f+(Seed%17+field))+.05f*along;
        public Vector2 Field(int i)=>Hinterland(new Vector2(i==1?88:78,(i-1)*32));
        public Vector2 Collection(int resident)=>Hinterland(resident%3==0?new Vector2(59,(resident/3%3-1)*32):resident%6==1?new Vector2(80,-55):new Vector2(82,50));
        public Vector2 CollectionGate(int resident)=>Hinterland(new Vector2(58,resident%3==0?(resident/3%3-1)*32:resident%6==1?-55:50));
        public void FitHinterland(WorldAtlasModel model,AtlasSite site)
        {
            float best=float.MaxValue,bestAngle=0;
            for(int direction=0;direction<8;direction++)
            {
                HinterlandAngle=(direction+(Seed%8))*45;float score=0;
                for(int field=0;field<3;field++)
                {
                    Vector2 middle=site.point+Field(field);float height=model.Height(middle.x,middle.y);
                    for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
                    {
                        Vector2 point=middle+Hinterland(new Vector2(x*12,z*12));
                        score+=Mathf.Abs(model.Height(point.x,point.y)-height);
                        if(!model.Walkable(point)||model.Nearest(point).id!=site.id)score+=10000;
                    }
                }
                foreach(int resident in new[]{0,1,3,4,6,7,9,10,12,13,15,16})
                {
                    var road=WorkRoad(resident);for(int j=1;j<road.Length;j++)for(int step=0;step<=10;step++)
                    {var point=site.point+Vector2.Lerp(road[j-1],road[j],step/10f);if(!model.Walkable(point)||model.Nearest(point).id!=site.id)score+=10000;}
                }
                if(score<best){best=score;bestAngle=HinterlandAngle;}
            }
            HinterlandAngle=bestAngle;
        }
        public Vector2 Job(int resident)
        {
            int role=resident%3,index=resident/3;
            if(role==0)return Field(index%3)+Hinterland(new Vector2(index<3?-5:5,-5));
            if(role==1)return Hinterland(index%2==0?new Vector2(97,-62+index/2*7):new Vector2(99,43+index/2*6));
            return Market+Rotate(new Vector2(-2+index%3*2,-3-index/3*1.6f));
        }
        public Vector2 WorkFacing(int resident)=>resident%3==0?Hinterland(Vector2.up):Hinterland(Vector2.right);
        public Vector2 WorkTarget(int resident)=>Job(resident)+WorkFacing(resident)*1.05f;
        public Vector2 WorkPoint(int resident,int row)=>Job(resident)+(resident%3==0?Hinterland(Vector2.up*(row%3)*3):Hinterland(Vector2.down*(row%3)*.4f));
        public Vector2[] SupplyRoad(int resident,Vector2 work)
        {
            Vector2 anchor=Collection(resident);return new[]{work,Job(resident),anchor};
        }
        public bool ReservedLand(Vector2 offset)
        {
            Vector3 local=Quaternion.Euler(0,-HinterlandAngle,0)*new Vector3(offset.x,0,offset.y);
            return local.x>48&&local.x<130&&local.z>-88&&local.z<78;
        }
        public Vector2 SocialFocus(int i)
        {
            int group=i/2/3;return Rotate(new Vector2((i%2==0?-10:11)+(group%2==0?-.5f:1),22+group*4));
        }
        public Vector2 Social(int i)
        {
            float angle=(i/2%3)*Mathf.PI*2/3+Seed%7*.09f;
            return SocialFocus(i)+Rotate(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(1.05f+(i%4)*.12f));
        }
        public Vector2 SocialRoad(int i)=>Rotate(new Vector2(0,22+i/2/3*4));
        public Vector2[] WorkRoad(int resident)
        {
            Vector2 job=Job(resident);
            if(resident%3==2)return new[]{Vector2.zero,Rotate(new Vector2(0,21)),job};
            var points=new List<Vector2>{Vector2.zero,Rotate(new Vector2(0,-58))};
            Vector2 entry=points[1],exit=Hinterland(new Vector2(62,0));
            float a=Mathf.Atan2(entry.y,entry.x)*Mathf.Rad2Deg,b=Mathf.Atan2(exit.y,exit.x)*Mathf.Rad2Deg,delta=Mathf.DeltaAngle(a,b);
            int steps=Mathf.Max(1,Mathf.CeilToInt(Mathf.Abs(delta)/15));
            for(int i=0;i<=steps;i++){float angle=(a+delta*i/steps)*Mathf.Deg2Rad;points.Add(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*62);}
            points.Add(Hinterland(new Vector2(58,0)));points.Add(CollectionGate(resident));points.Add(Collection(resident));points.Add(job);return points.ToArray();
        }
    }
}
