using UnityEngine;

namespace Reclamation.Atlas
{
    public enum VillageActivity { Sleeping, Breakfast, GoingToWork, Working, GoingOut, Socializing, GoingHome }

    // Pure schedule sampling also represents distant residents without running navigation agents.
    public enum VillageMotion { Resting, Walking, Farming, Trading, Chopping, Mining, Drinking, Eating, Sleeping, Carrying }

    public static class AtlasVillageRoutine
    {
        public static float DepartureOffset(int resident) => ((resident*37+11)%101)/101f*.6f;
        public static float TravelDuration(int resident) => .4f+((resident*29+7)%97)/97f*.45f;
        public static VillageMotion Motion(float hour,int resident)
        {
            switch(Activity(hour,resident))
            {
                case VillageActivity.Sleeping:return VillageMotion.Sleeping;
                case VillageActivity.Breakfast:return VillageMotion.Eating;
                case VillageActivity.GoingToWork:case VillageActivity.GoingOut:case VillageActivity.GoingHome:return VillageMotion.Walking;
                case VillageActivity.Working:return resident%3==0?VillageMotion.Farming:resident%3==2?VillageMotion.Trading:resident%6==1?VillageMotion.Chopping:VillageMotion.Mining;
                case VillageActivity.Socializing:return resident%2==0?VillageMotion.Drinking:VillageMotion.Eating;
                default:return VillageMotion.Resting;
            }
        }
        public static float WorkPhase(float hour,int resident)=>Mathf.Repeat((Mathf.Repeat(hour,24)-DepartureOffset(resident)-7-TravelDuration(resident))*60,140+resident%5*7)/(140+resident%5*7);
        public static VillageMotion PresentationMotion(float hour,int resident)
        {
            if(Activity(hour,resident)!=VillageActivity.Working||resident%3==2)return Motion(hour,resident);
            float phase=WorkPhase(hour,resident);
            if(phase>=.51f&&phase<.71f)return VillageMotion.Carrying;
            if(phase>=.2f&&phase<.26f||phase>=.76f)return VillageMotion.Walking;
            if(phase>=.46f&&phase<.51f||phase>=.71f&&phase<.76f)return VillageMotion.Resting;
            return Motion(hour,resident);
        }
        private static Vector2 WorkPosition(float hour,int resident,AtlasVillageLayout layout)
        {
            if(resident%3==2)return layout.Job(resident);
            float phase=WorkPhase(hour,resident);Vector2 first=layout.Job(resident),second=layout.WorkPoint(resident,2),collection=layout.Collection(resident);
            if(phase<.2f)return first;
            if(phase<.26f)return Vector2.Lerp(first,second,(phase-.2f)/.06f);
            if(phase<.51f)return second;
            if(phase<.71f)return Along(layout.SupplyRoad(resident,second),(phase-.51f)/.2f,resident);
            if(phase<.76f)return collection;
            return Vector2.Lerp(collection,first,(phase-.76f)/.24f);
        }
        public static VillageActivity Activity(float hour,int resident)
        {
            float h=Mathf.Repeat(hour-DepartureOffset(resident),24);
            if(h<6||h>=22)return VillageActivity.Sleeping;
            if(h<7)return VillageActivity.Breakfast;
            if(h<7+TravelDuration(resident))return VillageActivity.GoingToWork;
            if(h<17)return VillageActivity.Working;
            if(h<17+TravelDuration(resident))return VillageActivity.GoingOut;
            if(h<21.5f)return VillageActivity.Socializing;
            return VillageActivity.GoingHome;
        }
        private static Vector2 Along(Vector2[] points,float fraction,int resident)
        {
            float length=0;for(int i=1;i<points.Length;i++)length+=Vector2.Distance(points[i-1],points[i]);
            float left=Mathf.Clamp01(fraction)*length;
            for(int i=1;i<points.Length;i++){float segment=Vector2.Distance(points[i-1],points[i]);if(left<=segment){var delta=(points[i]-points[i-1]).normalized;float lane=((resident*13%7)-3)*.12f*Mathf.Sin(Mathf.PI*Mathf.Clamp01(fraction));return Vector2.Lerp(points[i-1],points[i],segment>0?left/segment:0)+new Vector2(-delta.y,delta.x)*lane;}left-=segment;}
            return points[points.Length-1];
        }
        private static readonly AtlasVillageLayout DefaultLayout=new AtlasVillageLayout(0);
        public static Vector2 Position(float hour,int resident,AtlasVillageLayout layout=null)
        {
            layout=layout??DefaultLayout;float h=Mathf.Repeat(hour-DepartureOffset(resident),24);var home=layout.Door(resident);var work=layout.WorkRoad(resident);var social=layout.Social(resident);
            switch(Activity(hour,resident))
            {
                case VillageActivity.GoingToWork:
                    var outward=new Vector2[work.Length+2];outward[0]=home;outward[1]=layout.Road(resident);work.CopyTo(outward,2);return Along(outward,(h-7)/TravelDuration(resident),resident);
                case VillageActivity.Working:return WorkPosition(hour,resident,layout);
                case VillageActivity.GoingOut:
                    var evening=new Vector2[work.Length+3];evening[0]=WorkPosition(17+DepartureOffset(resident),resident,layout);for(int i=0;i<work.Length;i++)evening[i+1]=work[work.Length-1-i];evening[work.Length+1]=layout.SocialRoad(resident);evening[work.Length+2]=social;return Along(evening,(h-17)/TravelDuration(resident),resident);
                case VillageActivity.Socializing:return social;
                case VillageActivity.GoingHome:return Along(new[]{social,layout.SocialRoad(resident),Vector2.zero,layout.Road(resident),home},(h-21.5f)*2,resident);
                default:return home;
            }
        }
    }
}
