using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Blight
{
    // Isolated workload harness using production DuelFighter rules, not Company AI.
    public sealed class CrowdBattleSimulation
    {
        public enum Order { Advance, Hold, Retreat }
        public sealed class Unit
        {
            public Vector3 position, home, forward=Vector3.forward;
            public DuelFighter fighter=new DuelFighter();
            public int target=-1, team;
            public bool moving;
        }
        public readonly Unit[] Units;
        public Order FriendlyOrder;
        public bool RespawnEnabled=true;
        public CrowdSquadCommands Squads {get;private set;}
        public void EnableSquads() { Squads = new CrowdSquadCommands(this); }
        public CrowdNavigation Navigation {get;private set;}
        private CrowdNavigation.Field enemyRoute;
        private Vector3[] routeGoals,routePoints;
        public void EnableNavigation(CrowdNavigation.Layout layout)
        {
            if(Squads==null)EnableSquads();
            Navigation=new CrowdNavigation(layout);enemyRoute=Navigation.Build(new Vector3(0,0,-25));
            routeGoals=new Vector3[Units.Length];routePoints=new Vector3[Units.Length];
            foreach(var squad in Squads.Squads)squad.route=Navigation.Build(squad.destination);
        }
        public CrowdCommunication Communication {get;private set;}
        public void EnableCommunication() { if(Perception==null)EnablePerception();Communication=new CrowdCommunication(this); }
        public void LocalAllies(int source,List<int> recipients,CrowdCommunication metrics)
        {
            recipients.Clear();var sender=Units[source];var key=Cell(sender.position);int visited=0;
            for(int x=-3;x<=3 && visited<192 && recipients.Count<32;x++)for(int z=-3;z<=3 && visited<192 && recipients.Count<32;z++)
                if(cells.TryGetValue(key+new Vector2Int(x,z),out var list))foreach(int other in list)
                {
                    visited++;metrics.CandidateChecks++;var unit=Units[other];
                    if(other!=source && unit.team==sender.team && unit.fighter.Alive &&
                       (sender.team!=0 || Squads==null || Squads.SquadOf(source)==Squads.SquadOf(other)))
                    {
                        float distance=(unit.position-sender.position).sqrMagnitude;
                        if(distance<=144 && (distance<=36 || Perception.Unobstructed(sender.position,unit.position)))recipients.Add(other);
                    }
                    if(visited>=192 || recipients.Count>=32)break;
                }
        }
        public CrowdPerception Perception {get;private set;}
        public void EnablePerception()
        {
            Perception=new CrowdPerception(this);
            foreach(var unit in Units)unit.forward=unit.team==0?Vector3.forward:Vector3.back;
        }
        public Vector3 TargetPosition(int observer) => Perception!=null?Perception.States[observer].knownPosition:Units[Units[observer].target].position;
        public long Hits, Attacks, Respawns, CandidateChecks, Steps;
        public int FriendlyCount { get; }
        private readonly Dictionary<Vector2Int,List<int>> cells=new Dictionary<Vector2Int,List<int>>();
        private readonly List<List<int>> lists=new List<List<int>>();
        private readonly int[] impacts;
        private int impactCount;
        private const float CellSize=4;
        public CrowdBattleSimulation(int count, int friendlyCount=-1)
        {
            if(count<1 || count>1280) throw new ArgumentOutOfRangeException(nameof(count));
            if(friendlyCount < -1 || friendlyCount > count)throw new ArgumentOutOfRangeException(nameof(friendlyCount));
            Units=new Unit[count]; FriendlyCount=friendlyCount<0?(count+4)/5:friendlyCount; impacts=new int[count];
            int columns=Mathf.CeilToInt(Mathf.Sqrt(count));
            for(int i=0;i<count;i++)
            {
                Vector3 home=new Vector3((i%columns-(columns-1)*.5f)*1.8f,0,(i/columns-(columns-1)*.5f)*1.8f);
                Units[i]=new Unit {position=home,home=home,team=friendlyCount<0?(i%5==0?0:1):(i<friendlyCount?0:1)};
            }
        }
        private static Vector2Int Cell(Vector3 p)=>new Vector2Int(Mathf.FloorToInt(p.x/CellSize),Mathf.FloorToInt(p.z/CellSize));
        private void Index()
        {
            foreach(var list in lists) list.Clear(); cells.Clear(); int used=0;
            for(int i=0;i<Units.Length;i++)
            {
                if(!RespawnEnabled && !Units[i].fighter.Alive)continue;
                Vector2Int key=Cell(Units[i].position);
                if(!cells.TryGetValue(key,out var list))
                {
                    if(used==lists.Count) lists.Add(new List<int>(32));
                    list=lists[used++]; cells.Add(key,list);
                }
                list.Add(i);
            }
        }
        public void MoveTowards(int index,Vector3 goal,float dt)
        {
            Unit u=Units[index];
            if(Navigation!=null)
            {
                if((Steps+index)%6==0 || (routeGoals[index]-goal).sqrMagnitude>1 || routePoints[index]==Vector3.zero)
                {
                    var field=u.team==0?Squads.Squads[Squads.SquadOf(index)].route:enemyRoute;
                    routePoints[index]=Navigation.Waypoint(field,u.position,goal);routeGoals[index]=goal;
                }
                goal=routePoints[index];
            }
            Vector3 delta=goal-u.position;delta.y=0;
            if(delta.sqrMagnitude<.01f)return;
            Vector3 move=delta.normalized;Vector2Int key=Cell(u.position);int visited=0;
            for(int x=-1;x<=1 && visited<48;x++)for(int z=-1;z<=1 && visited<48;z++)
                if(cells.TryGetValue(key+new Vector2Int(x,z),out var list))foreach(int other in list)
                {
                    if(other==index)continue;visited++;
                    Vector3 away=u.position-Units[other].position;float d=away.sqrMagnitude;
                    if(d<.65f*.65f)
                        move+=d>.0001f?away*(.5f/d):Vector3.right*(index<other?-1:1);
                    if(visited>=48)break;
                }
            Vector3 step=Vector3.ClampMagnitude(move,1)*Mathf.Min(2.2f*dt,delta.magnitude);
            if(Navigation!=null)step=Navigation.Constrain(u.position,step);
            u.position+=step;u.forward=delta.normalized;u.moving=step.sqrMagnitude>.000001f;
        }
        public void Step(float dt)
        {
            if(!(dt>0) || dt>1f/30f) throw new ArgumentOutOfRangeException(nameof(dt));
            Index(); impactCount=0;if(Perception!=null)Perception.Tick(dt);
            if(Squads!=null) Squads.Tick(dt);
            for(int i=0;i<Units.Length;i++)
            {
                Unit u=Units[i];u.moving=false;
                if(!u.fighter.Alive && !RespawnEnabled)continue;
                if(!u.fighter.Alive) {u.fighter=new DuelFighter();u.position=u.home;u.target=-1;if(Perception!=null)Perception.Forget(i);if(Communication!=null)Communication.Forget(i);Respawns++;}
                if(u.fighter.Advance(dt)) impacts[impactCount++]=i;
                // Decisions staggered at 10 Hz while action state advances at 60 Hz.
                if((Steps+i)%6==0)
                {
                    float best=float.MaxValue;int target=-1, visited=0;Vector2Int key=Cell(u.position);
                    int radius=Perception==null?2:3;
                    for(int x=-radius;x<=radius && visited<96;x++) for(int z=-radius;z<=radius && visited<96;z++)
                        if(cells.TryGetValue(key+new Vector2Int(x,z),out var list)) foreach(int other in list)
                        {
                            CandidateChecks++; if(Units[other].team==u.team || !Units[other].fighter.Alive) continue;
                            float d=(Units[other].position-u.position).sqrMagnitude;
                            if(d<best && (Perception==null || Perception.CanSee(i,other))){best=d;target=other;} visited++;
                            if(visited>=96) break;
                        }
                    u.target=target;if(Perception!=null)Perception.Sense(i,target);if(Communication!=null && target>=0)Communication.ReportSight(i);
                }
                if(!u.fighter.CanAct) continue;
                if(Perception!=null && u.target<0 && Perception.TryKnownPosition(i,out Vector3 known))
                {
                    bool move=Squads==null?!(u.team==0 && FriendlyOrder==Order.Hold):Squads.CanInvestigate(i,known);
                    bool commandedMove=u.team==0 && Squads!=null && Squads.Squads[Squads.SquadOf(i)].command==CrowdSquadCommands.Command.Move;
                    if(!commandedMove)
                    {
                        Vector3 look=known-u.position;if(look.sqrMagnitude>.01f)u.forward=look.normalized;
                        if(move){MoveTowards(i,known,dt);continue;}
                    }
                }
                if(Squads!=null && Squads.Control(i,dt)) continue;
                if(u.team==0 && FriendlyOrder==Order.Retreat)
                {u.forward=Vector3.back;u.position+=u.forward*(2.2f*dt);u.position.z=Mathf.Max(-40,u.position.z);u.moving=true;continue;}
                if(u.target<0 || !Units[u.target].fighter.Alive) continue;
                Vector3 delta=TargetPosition(i)-u.position;delta.y=0;float distance=delta.magnitude;
                if(distance>.01f) u.forward=delta/distance;
                if(distance<1.9f && (Perception==null || Perception.CanSee(i,u.target)) && (Navigation==null || Navigation.Visible(u.position,Units[u.target].position)))
                {
                    if(u.fighter.Attack(BlightEquipment.Weapon(BlightWeapon.Sword,(u.fighter.AttackSequence+i)%4==3))) {Attacks++;if(Perception!=null)Perception.Emit(i,u.position);}
                }
                else if(Squads!=null ? Squads.MayChase(i) : !(u.team==0 && FriendlyOrder==Order.Hold))
                {
                    if(Squads!=null) MoveTowards(i,TargetPosition(i),dt);
                    else
                    {
                    Vector3 move=u.forward;
                    Vector2Int key=Cell(u.position);int contacts=0;
                    for(int x=-1;x<=1 && contacts<24;x++) for(int z=-1;z<=1 && contacts<24;z++)
                        if(cells.TryGetValue(key+new Vector2Int(x,z),out var list)) foreach(int other in list)
                        {
                            if(other==i) continue;Vector3 away=u.position-Units[other].position;float d=away.sqrMagnitude;
                            if(d>.0001f && d<.65f*.65f) {move+=away*(.5f/d);contacts++;} if(contacts>=24)break;
                        }
                    u.position+=Vector3.ClampMagnitude(move,1)*(2.2f*dt);u.moving=true;
                    }
                }
            }
            for(int j=0;j<impactCount;j++)
            {
                Unit u=Units[impacts[j]];if(!u.fighter.Alive || u.target<0)continue;
                Unit target=Units[u.target];
                if(target.fighter.Alive && (Navigation==null || Navigation.Visible(u.position,target.position)) && DuelFighter.InReach(u.position,u.forward,target.position,u.fighter.Strike.Reach,u.fighter.Strike.HalfAngle))
                {target.fighter.ReceiveAttack(u.fighter.Strike,Vector3.Dot(target.forward,-u.forward)>0);Hits++;}
            }
            Steps++;
        }
    }
}
