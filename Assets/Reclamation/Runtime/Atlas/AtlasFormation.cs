using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Atlas
{
    public enum AtlasFormationPhase { Posted, Patrol, Column, Deploying, Line, Returning }

    public sealed partial class WorldAtlasDemo
    {
        private sealed class PlatoonMarch
        {
            public readonly List<Vector2> trail = new List<Vector2>();
            public readonly List<Vector2> path = new List<Vector2>();
            public Vector2 guide;
            public int next;
            public bool response, returning, regroup;
            public AtlasGuardDuty duty;
            public AtlasFormationPhase phase;
        }
        private readonly PlatoonMarch[] marches = new PlatoonMarch[4];
        public AtlasFormationPhase FormationPhase(int platoon) => marches[platoon].phase;
        public string FormationDiagnostic(int platoon)
        {
            var m=marches[platoon];string text=$"{m.phase} next {m.next}/{m.path.Count}, guide {m.guide}, regroup {m.regroup}";
            for(int i=0;i<6;i++)text+=$" | {i}: {guards[1+platoon*6+i].local} trail {MarchSlot(m,i,false)}";
            return text;
        }
        public Vector3 GuardFacing(int index) => guards[index].root.forward;

        private List<Vector2> FormationRoute(AtlasVillageLayout layout, AtlasGuardDuty duty)
        {
            // Centerline route; individual slots are derived from the same travelled trail.
            var points = new List<Vector2>(AtlasCompanySchedule.Route(layout, duty, 0));
            if (duty == AtlasGuardDuty.TownPatrol)
                points = new List<Vector2> { Vector2.zero, layout.Rotate(new Vector2(-3,-30)), layout.Rotate(new Vector2(3,-30)), layout.Rotate(new Vector2(3,20)), layout.Rotate(new Vector2(-3,20)), Vector2.zero };
            return points;
        }
        private void InitializeMarches(AtlasVillageLayout layout)
        {
            for(int p=0;p<4;p++)
            {
                var m = new PlatoonMarch { duty=ActiveCompany.Platoons[p].Duty };
                marches[p]=m;m.path.AddRange(FormationRoute(layout,m.duty));m.next=1;m.guide=m.path[0];m.trail.Add(m.guide);
                bool posted=m.duty==AtlasGuardDuty.Gate||m.duty==AtlasGuardDuty.Reserve;
                AdvanceGuide(m,posted?1000:m.duty==AtlasGuardDuty.FarmPatrol?65:48);
                m.phase=posted?AtlasFormationPhase.Posted:AtlasFormationPhase.Patrol;
                for(int member=0;member<6;member++)
                {
                    var g=guards[1+p*6+member];g.duty=m.duty;
                    g.local=posted?AtlasCompanySchedule.Route(layout,m.duty,member)[2]:MarchSlot(m,member,true);
                }
            }
        }
        private void AdvanceGuide(PlatoonMarch m,float distance)
        {
            while(distance>0 && m.next<m.path.Count)
            {
                float step=Mathf.Min(.2f,distance),left=Vector2.Distance(m.guide,m.path[m.next]);
                if(left<.001f){m.next++;continue;}
                step=Mathf.Min(step,left);m.guide=Vector2.MoveTowards(m.guide,m.path[m.next],step);distance-=step;
                if(Vector2.Distance(m.trail[m.trail.Count-1],m.guide)>=.19f||left<=step+.001f)m.trail.Add(m.guide);
                if(m.trail.Count>160)m.trail.RemoveAt(0);
                if(left<=step+.001f)m.next++;
            }
        }
        private Vector2 MarchSlot(PlatoonMarch m,int member,bool patrol)
        {
            float behind=member*(patrol?2.7f:2.1f);Vector2 at=m.guide,forward=Vector2.up;
            for(int i=m.trail.Count-1;i>=0;i--)
            {
                var prior=m.trail[i];float length=Vector2.Distance(at,prior);
                if(length<.0001f)continue;
                forward=(at-prior)/length;
                if(behind<=length){at=Vector2.Lerp(at,prior,behind/length);behind=0;break;}
                behind-=length;at=prior;
            }
            if(behind>0)at-=forward*behind;
            if(patrol&&member>0)at+=new Vector2(-forward.y,forward.x)*(member%2==0?1:-1)*1.05f;
            return at;
        }
        private void RetaskMarch(int platoon,AtlasVillageLayout layout,bool response)
        {
            var m=marches[platoon];var route=FormationRoute(layout,ActiveCompany.Platoons[platoon].Duty);
            // Retrace the current road before taking the next assignment. Keep the trail so the tail follows continuously.
            var retrace=new List<Vector2>();
            for(int i=Mathf.Min(m.next-1,m.path.Count-1);i>=0;i--)retrace.Add(m.path[i]);
            m.path.Clear();m.path.Add(m.guide);m.path.AddRange(retrace);
            if(response)
            {
                var approach=layout.CollectionGate(1);
                if(Vector2.Distance(m.guide,approach)<30&&FormationClear(m.guide,approach))
                {
                    m.path.Clear();m.path.Add(m.guide);m.path.Add(approach);
                }
                else
                {
                    var road=new List<Vector2>(layout.WorkRoad(0));road.RemoveRange(road.Count-3,3);
                    m.path.AddRange(road);m.path.Add(approach);
                }
            }
            else
            {
                if(m.response)
                {
                    m.path.Clear();m.path.Add(m.guide);
                    var road=new List<Vector2>(layout.WorkRoad(0));road.RemoveRange(road.Count-3,3);road.Add(layout.CollectionGate(1));
                    for(int i=road.Count-1;i>=0;i--)m.path.Add(road[i]);
                }
                m.path.AddRange(route);
            }
            m.regroup=false;
            if(m.response && !response)
            {
                // First reform behind the commander in the return direction. Reusing the
                // outward trail at a 180-degree turn folds a column back through itself.
                Vector2 heading=Vector2.zero;
                for(int i=1;i<m.path.Count;i++)if(Vector2.Distance(m.guide,m.path[i])>.1f){heading=(m.path[i]-m.guide).normalized;break;}
                if(heading.sqrMagnitude>.5f&&FormationClear(m.guide,m.guide-heading*11))
                {
                    m.trail.Clear();m.trail.Add(m.guide-heading*14);m.trail.Add(m.guide);m.regroup=true;
                }
            }
            m.next=1;m.duty=ActiveCompany.Platoons[platoon].Duty;m.returning=!response;m.response=response;
            m.phase=response?AtlasFormationPhase.Column:AtlasFormationPhase.Returning;
            for(int i=0;i<6;i++)guards[1+platoon*6+i].duty=m.duty;
        }
        private Vector2 DefensiveSlot(AtlasVillageLayout layout,int member)
        {
            // Five troops across the approach, commander one pace behind the center.
            return layout.CollectionGate(1)+layout.Hinterland(member==0?new Vector2(-2.2f,0):new Vector2(0,(member-3)*2.2f));
        }
        private bool FormationClear(Vector2 from,Vector2 to)
        {
            var site=Model.sites[guardSite];Vector2 a=site.point+from,b=site.point+to;
            if(!Model.Walkable(b))return false;
            var start=new Vector3(a.x,Surface(a)+.8f,a.y);var end=new Vector3(b.x,Surface(b)+.8f,b.y);
            return !Physics.CheckSphere(end,.3f,ObstacleMask,QueryTriggerInteraction.Ignore)&&!Physics.Linecast(start,end,ObstacleMask,QueryTriggerInteraction.Ignore);
        }
        private void MoveFormationMember(int index,Vector2 goal,float dt)
        {
            var g=guards[index];Vector2 delta=goal-g.local;
            float distance=Mathf.Min(delta.magnitude,1.35f*dt);if(distance<=0)return;
            Vector2 forward=delta.normalized;
            // Validate each complete candidate against every guard. A later avoidance choice
            // must not invalidate clearance from an earlier neighbor.
            for(int attempt=0;attempt<7;attempt++)
            {
                float angle=(attempt==0?0:(attempt+1)/2*45*(attempt%2==1?1:-1))*Mathf.Deg2Rad;
                Vector2 direction=new Vector2(forward.x*Mathf.Cos(angle)-forward.y*Mathf.Sin(angle),forward.x*Mathf.Sin(angle)+forward.y*Mathf.Cos(angle));
                Vector2 candidate=g.local+direction*distance;
                bool clear=true;
                for(int other=0;other<guards.Count;other++)
                {
                    if(other==index)continue;
                    float old=Vector2.Distance(g.local,guards[other].local),next=Vector2.Distance(candidate,guards[other].local);
                    if(next<.95f&&next<old-.00001f){clear=false;break;}
                }
                if(clear&&FormationClear(g.local,candidate)){g.local=candidate;return;}
            }
        }

        private bool FarmReliefReady(int outgoing,AtlasVillageLayout layout)
        {
            for(int p=0;p<4;p++)
                if(p!=outgoing&&ActiveCompany.Platoons[p].Duty==AtlasGuardDuty.FarmPatrol&&marches[p].duty==AtlasGuardDuty.FarmPatrol
                    &&Vector2.Distance(guards[1+p*6].local,layout.CollectionGate(1))<18)return true;
            return false;
        }

        private void TickMarches(AtlasVillageLayout layout,float dt)
        {
            for(int p=0;p<4;p++)
            {
                var m=marches[p];var duty=ActiveCompany.Platoons[p].Duty;
                // Finish a committed relief/return route before consuming a newer watch.
                if(duty!=m.duty&&!m.returning&&(m.duty!=AtlasGuardDuty.FarmPatrol||FarmReliefReady(p,layout)))RetaskMarch(p,layout,false);
                duty=m.duty;
                bool finished=m.next>=m.path.Count;
                bool patrol=!m.response&&!m.returning&&(duty==AtlasGuardDuty.TownPatrol||duty==AtlasGuardDuty.FarmPatrol);
                if(finished&&m.returning&&(duty==AtlasGuardDuty.TownPatrol||duty==AtlasGuardDuty.FarmPatrol))
                {
                    m.returning=false;patrol=true;
                }
                if(finished&&patrol)
                {
                    m.path.Clear();m.path.AddRange(FormationRoute(layout,duty));m.next=1;
                    finished=false;
                }
                if(!finished)
                {
                    float lag=0;for(int i=0;i<6;i++)lag=Mathf.Max(lag,Vector2.Distance(guards[1+p*6+i].local,MarchSlot(m,i,patrol)));
                    if(m.regroup&&lag<.6f)m.regroup=false;
                    if(!m.regroup&&lag<7)AdvanceGuide(m,dt*1.2f);
                    finished=m.next>=m.path.Count;
                }
                m.phase=m.response?(finished?AtlasFormationPhase.Deploying:AtlasFormationPhase.Column):m.returning?AtlasFormationPhase.Returning:patrol?AtlasFormationPhase.Patrol:AtlasFormationPhase.Posted;
                bool rearPosted=true;
                if(finished&&!m.response&&!patrol)
                    for(int i=3;i<6;i++)rearPosted&=Vector2.Distance(guards[1+p*6+i].local,AtlasCompanySchedule.Route(layout,duty,i)[2])<.35f;
                bool arrived=true;
                for(int member=0;member<6;member++)
                {
                    var g=guards[1+p*6+member];Vector2 goal;
                    if(finished&&m.response)goal=DefensiveSlot(layout,member);
                    else if(finished&&!patrol)
                    {
                        goal=AtlasCompanySchedule.Route(layout,duty,member)[2];
                        // Fill the rear posts first, then the front. Otherwise the final
                        // arriving soldier can be fenced out by already parked comrades.
                        if(!rearPosted&&member<3)goal+=layout.Rotate(new Vector2(0,duty==AtlasGuardDuty.Reserve?-4:5));
                    }
                    else goal=MarchSlot(m,member,patrol);
                    if(!FormationClear(g.local,goal)&&!finished)goal=MarchSlot(m,member,false);
                    // Static posts never drift; moving platoons yield around them.
                    if(Vector2.Distance(g.local,goal)>.025f)MoveFormationMember(1+p*6+member,goal,dt);
                    arrived&=Vector2.Distance(g.local,goal)<.35f;
                }
                if(finished&&arrived)
                {
                    if(m.response)m.phase=AtlasFormationPhase.Line;
                    else if(m.returning){m.returning=false;m.phase=duty==AtlasGuardDuty.Gate||duty==AtlasGuardDuty.Reserve?AtlasFormationPhase.Posted:AtlasFormationPhase.Patrol;}
                }
            }
        }
    }
}
