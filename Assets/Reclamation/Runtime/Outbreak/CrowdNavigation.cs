using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Blight
{
    // Shared destination fields on a static one-metre grid. Lab-only, no NavMesh or production terrain changes.
    public sealed class CrowdNavigation
    {
        public enum Layout { Wall, Gap, Bridge }
        public const int Size = 101;
        public readonly Layout Scenario;
        public int FieldsBuilt {get;private set;}
        public double PlanningMilliseconds {get;private set;}
        public readonly Rect[] Obstacles;
        private readonly bool[] walkable = new bool[Size * Size];
        private readonly int[] queue = new int[Size * Size];
        public sealed class Field { public readonly int[] distance = new int[Size * Size]; public Vector3 goal; }
        public CrowdNavigation(Layout scenario)
        {
            Scenario = scenario;
            Obstacles = scenario == Layout.Wall ? new[]{new Rect(-15,-3,30,6)} :
                new[]{new Rect(-51,-3,scenario == Layout.Gap ? 49 : 48,6), new Rect(scenario == Layout.Gap ? 2 : 3,-3,51,6)};
            for(int z=0;z<Size;z++)for(int x=0;x<Size;x++)walkable[z*Size+x]=Clear(new Vector3(x-50,0,z-50));
        }
        public bool Clear(Vector3 p)
        {
            if(Mathf.Abs(p.x)>48 || Mathf.Abs(p.z)>48)return false;
            foreach(var r in Obstacles)if(p.x>r.xMin-.45f && p.x<r.xMax+.45f && p.z>r.yMin-.45f && p.z<r.yMax+.45f)return false;
            return true;
        }
        private static int Cell(Vector3 p) => Mathf.Clamp(Mathf.RoundToInt(p.z)+50,0,100)*Size+Mathf.Clamp(Mathf.RoundToInt(p.x)+50,0,100);
        private static Vector3 Point(int i) => new Vector3(i%Size-50,0,i/Size-50);
        public Vector3 Snap(Vector3 p)
        {
            if(Clear(p))return new Vector3(p.x,0,p.z);
            float best=float.MaxValue;int choice=0;
            for(int i=0;i<walkable.Length;i++)if(walkable[i]){float d=(Point(i)-p).sqrMagnitude;if(d<best){best=d;choice=i;}}
            return Point(choice);
        }
        public bool Visible(Vector3 a,Vector3 b)
        {
            int steps=Mathf.CeilToInt(Vector3.Distance(a,b)*3);
            for(int i=1;i<=steps;i++)if(!Clear(Vector3.Lerp(a,b,i/(float)steps)))return false;
            return true;
        }
        public Field Build(Vector3 destination)
        {
            long stamp=System.Diagnostics.Stopwatch.GetTimestamp();
            var f=new Field{goal=Snap(destination)};
            for(int i=0;i<f.distance.Length;i++)f.distance[i]=-1;
            int start=Cell(f.goal);if(!walkable[start])start=Cell(Snap(Point(start)));
            int read=0,write=0;queue[write++]=start;f.distance[start]=0;
            while(read<write)
            {
                int c=queue[read++],x=c%Size,z=c/Size;
                for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                {
                    int nx=x+dx,nz=z+dz;if(nx<0 || nx>=Size || nz<0 || nz>=Size || (dx==0 && dz==0))continue;
                    int n=nz*Size+nx;if(!walkable[n] || f.distance[n]>=0 || !walkable[z*Size+nx] || !walkable[nz*Size+x])continue;
                    f.distance[n]=f.distance[c]+1;queue[write++]=n;
                }
            }
            FieldsBuilt++;PlanningMilliseconds+=(System.Diagnostics.Stopwatch.GetTimestamp()-stamp)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            return f;
        }
        public Vector3 Waypoint(Field field,Vector3 position,Vector3 final)
        {
            final=Clear(final)?final:field.goal;
            if(Visible(position,final))return final;
            int c=Cell(position),best=field.distance[c];Vector3 next=Point(c);
            // Look ahead a few cells to avoid following every grid corner.
            for(int dz=-3;dz<=3;dz++)for(int dx=-3;dx<=3;dx++)
            {
                int x=c%Size+dx,z=c/Size+dz;if(x<0 || x>=Size || z<0 || z>=Size)continue;
                int n=z*Size+x,d=field.distance[n];if(d<0 || (best>=0 && d>=best))continue;
                Vector3 p=Point(n);if(Visible(position,p)){best=d;next=p;}
            }
            return next;
        }
        public Vector3 Constrain(Vector3 from,Vector3 step)
        {
            if(Clear(from+step))return step;
            Vector3 x=new Vector3(step.x,0,0),z=new Vector3(0,0,step.z);
            if(Clear(from+x))return x;
            return Clear(from+z)?z:Vector3.zero;
        }
    }
}
