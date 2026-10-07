using UnityEngine;
using Reclamation.Blight;

namespace Reclamation.Atlas
{
    public sealed partial class AtlasSyntyVisual
    {
        public bool CombatMotion { get; set; }
        private Vector3 travelDirection=Vector3.forward;
        private float breathingPhase,footTurn,lastHeading,turnStep;
        private bool motionInitialized;
        private float SampleCombatMotion(Vector3 displacement,bool moving,bool neutral,float delta)
        {
            if(!motionInitialized||displacement.sqrMagnitude>1)
            {
                motionInitialized=true;lastHeading=transform.eulerAngles.y;
                footTurn=turnStep=0;travelDirection=Vector3.forward;
            }
            if(delta<=0)return 0;
            float heading=transform.eulerAngles.y;
            float turn=Mathf.DeltaAngle(lastHeading,heading);lastHeading=heading;
            footTurn=neutral?Mathf.MoveTowards(Mathf.Clamp(footTurn-turn,-28,28),0,delta*95):0;
            turnStep=Mathf.MoveTowards(turnStep,neutral?Mathf.Clamp01(Mathf.Abs(footTurn)/18):0,delta*8);
            if(neutral)breathingPhase=Mathf.Repeat(breathingPhase+delta*.38f,1);
            if(moving&&displacement.sqrMagnitude>.000001f)
                travelDirection=Vector3.Lerp(travelDirection,transform.InverseTransformDirection(displacement.normalized),1-Mathf.Exp(-delta*14));
            // Distance-based cadence: blocked travel does not play a full-speed walk.
            return neutral?Mathf.Min(displacement.magnitude/1.6f,delta*2.8f):0;
        }
        private float contactTime,contactDuration;
        private bool shieldContact;
        // Resolved contact reacts immediately, then settles; no delayed recoil peak.
        public float ContactRecoil=>contactDuration>0?Mathf.Sqrt(Mathf.Clamp01(contactTime/contactDuration)):0;
        private float weaponReboundTime;
        public float WeaponRebound=>Mathf.Clamp01(weaponReboundTime/.18f);
        public void ReceiveWeaponContact(string outcome)
        {
            if(outcome=="Blocked")weaponReboundTime=.18f;
        }
        public string LastContact {get;private set;}
        public void ReceiveContact(string outcome)
        {
            if(outcome!="Blocked"&&outcome!="Hit"&&outcome!="Guard broken")return;
            LastContact=outcome;shieldContact=outcome=="Blocked";contactDuration=shieldContact?.24f:.30f;contactTime=contactDuration;
        }
        // A deliberate preparation, fast cut, follow-through, then return to guard.
        // Recovery remains the existing fighter recovery; this never advances it.
        public static void ExchangeWeights(DuelAction action,float progress,out float load,out float hit,out float follow)
        {
            float p=Mathf.Clamp01(progress);load=hit=follow=0;
            if(action==DuelAction.Windup)
            {
                float cut=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.68f,1,p));
                load=Mathf.SmoothStep(0,1,p/.52f)*(1-cut);hit=cut;
            }
            else if(action==DuelAction.Recovery)
            {
                hit=1-Mathf.SmoothStep(0,1,p/.32f);
                follow=Mathf.SmoothStep(0,1,p/.22f)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.28f,1,p)));
            }
        }
    }
}
