using UnityEngine;

namespace Reclamation.Blight
{
    // Gameplay perception: position snapshots, never a hidden target's live transform.
    public sealed class CrowdPerception
    {
        public const float SightRange = 12, HearingRange = 10, MemorySeconds = 4;
        public sealed class Awareness
        {
            public int visibleTarget = -1;
            public Vector3 knownPosition;
            public float expires;
            public bool heard, reported;
            public float observedAt=-1;
            public long lastNoise;
        }
        private struct Noise { public Vector3 position; public float expires; public int source; public long serial; }
        public readonly Awareness[] States;
        private readonly CrowdBattleSimulation simulation;
        private readonly Noise[] noises = new Noise[128];
        private long serial;
        public long SightChecks, HearingChecks, SightAcquisitions, HeardEvents;
        public float Time {get;private set;}
        public CrowdPerception(CrowdBattleSimulation simulation)
        {
            this.simulation=simulation;States=new Awareness[simulation.Units.Length];
            for(int i=0;i<States.Length;i++)States[i]=new Awareness();
        }
        public void Tick(float dt) { Time+=dt; }
        public void Forget(int index) { States[index]=new Awareness {lastNoise=serial}; }
        public bool Unobstructed(Vector3 from,Vector3 to) => simulation.Navigation==null || simulation.Navigation.Scenario==CrowdNavigation.Layout.Bridge || simulation.Navigation.Visible(from,to);
        public bool CanSee(int observer,int target)
        {
            SightChecks++;
            var a=simulation.Units[observer];var b=simulation.Units[target];
            Vector3 delta=b.position-a.position;float distance=delta.magnitude;
            if(!b.fighter.Alive || a.team==b.team || distance>SightRange)return false;
            if(distance>.001f && Vector3.Dot(a.forward,delta/distance)<.5f)return false;
            return Unobstructed(a.position,b.position);
        }
        public void Emit(int source,Vector3 position)
        {
            serial++;noises[(int)(serial%noises.Length)]=new Noise {source=source,position=position,expires=Time+.5f,serial=serial};
        }
        public void Sense(int observer,int visibleTarget)
        {
            var state=States[observer];state.visibleTarget=visibleTarget;
            if(visibleTarget>=0)
            {
                SightAcquisitions++;state.knownPosition=simulation.Units[visibleTarget].position;
                state.expires=Time+MemorySeconds;state.heard=false;state.reported=false;state.observedAt=Time;state.lastNoise=serial;return;
            }
            // Keep an unexpired last-seen location in preference to anonymous sounds.
            if(state.expires>Time && !state.heard){state.lastNoise=serial;return;}
            var position=simulation.Units[observer].position;float closest=float.MaxValue;int choice=-1;
            for(int i=0;i<noises.Length;i++)
            {
                var noise=noises[i];if(noise.serial<=state.lastNoise || noise.expires<=Time || noise.source==observer)continue;
                HearingChecks++;float distance=(noise.position-position).sqrMagnitude;
                if(distance>HearingRange*HearingRange || distance>=closest)continue;
                // Simple muffling through solid map barriers, not physical acoustics.
                if(!Unobstructed(position,noise.position) && distance>25)continue;
                closest=distance;choice=i;
            }
            state.lastNoise=serial;
            if(choice>=0){state.knownPosition=noises[choice].position;state.expires=Time+2;state.heard=true;state.reported=false;HeardEvents++;}
        }
        public bool ReceiveReport(int receiver,Vector3 position,float observedAt)
        {
            var state=States[receiver];
            if(state.visibleTarget>=0 || (state.expires>Time && !state.heard && !state.reported))return false;
            if(state.reported && observedAt<=state.observedAt)return false;
            state.knownPosition=position;state.observedAt=observedAt;state.expires=observedAt+3;
            state.heard=false;state.reported=true;return true;
        }
        public bool TryKnownPosition(int observer,out Vector3 position)
        {
            var state=States[observer];position=state.knownPosition;return state.expires>Time;
        }
    }
}
