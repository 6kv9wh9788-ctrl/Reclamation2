using System.Collections;
using NUnit.Framework;
using Reclamation.Atlas;
using UnityEngine;
using UnityEngine.TestTools;
namespace Reclamation.Tests
{
    public sealed class AtlasWildlifeTests
    {
        [UnityTest] public IEnumerator HabitatsStayBoundedSafeAndReproducibleAcrossPresets()
        {
            var go=new GameObject("Wildlife test");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try
            {
                demo.AutomaticClock=false;
                foreach(var config in new[]{(AtlasSize.Small,4),(AtlasSize.Medium,8),(AtlasSize.Medium,12)})
                {
                    demo.LoadMap(config.Item1,config.Item2);demo.VisitCommander();demo.TickVillage(0);
                    Assert.That(demo.ActiveWildlifeCount,Is.EqualTo(18));var positions=new Vector3[18];
                    for(int i=0;i<18;i++){positions[i]=demo.WildlifePosition(i);Assert.That(demo.IsSolidAt(positions[i]),Is.False);}
                    demo.LoadMap(config.Item1,config.Item2);demo.VisitCommander();demo.TickVillage(0);
                    for(int i=0;i<18;i++)Assert.That(demo.WildlifePosition(i),Is.EqualTo(positions[i]),"Same seed must place the same habitat population");
                    for(int step=0;step<100;step++){demo.TickVillage(.1f);for(int i=0;i<18;i++){var p=demo.WildlifePosition(i);Assert.That(demo.Model.Walkable(new Vector2(p.x,p.z)),Is.True);Assert.That(demo.IsSolidAt(p),Is.False);}}
                    demo.SurveyPaused=true;var before=demo.WildlifePosition(6);demo.TickVillage(20);Assert.That(demo.WildlifePosition(6),Is.EqualTo(before));demo.SurveyPaused=false;
                }
            }
            finally{Object.Destroy(go);}yield return null;
        }
        [UnityTest] public IEnumerator PeopleCauseFlightAndDistantPopulationsDeactivate()
        {
            var go=new GameObject("Wildlife reaction test");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try
            {
                demo.AutomaticClock=false;demo.VisitCommander();demo.TickVillage(0);
                var hero=go.transform.Find("Atlas survey world/Player explorer");
                hero.position=demo.WildlifePosition(6)+Vector3.back*2;
                float before=Vector3.Distance(hero.position,demo.WildlifePosition(6));demo.TickVillage(.1f);
                Assert.That(demo.WildlifeState(6),Is.EqualTo(AtlasAnimalState.Fleeing));Assert.That(Vector3.Distance(hero.position,demo.WildlifePosition(6)),Is.GreaterThan(before));
                hero.position=Vector3.one*100000;demo.TickVillage(.1f);Assert.That(demo.ActiveWildlifeCount,Is.Zero);
            }
            finally{Object.Destroy(go);}yield return null;
        }
        [UnityTest] public IEnumerator ClothAndSkinAreMatteAndOutfitsFollowAllPoses()
        {
            var go=new GameObject("Outfit integration");var visual=go.AddComponent<AtlasCharacterVisual>();visual.Build(3);
            try
            {
                var body=go.transform.Find("Prototype body").GetComponent<Renderer>();Assert.That(body.sharedMaterials[0].GetFloat("_Smoothness"),Is.LessThan(.1f));Assert.That(body.sharedMaterials[1].GetFloat("_Metallic"),Is.Zero);
                foreach(string clip in new[]{"Idle","Walk","Run"}){visual.Play(clip);yield return null;var mesh=go.transform.Find("Padded tunic belt pouch and straps").GetComponent<MeshFilter>().sharedMesh;Assert.That(mesh,Is.Not.Null);Assert.That(mesh.vertexCount,Is.GreaterThan(100));}
                Assert.That(go.transform.Find("Helmet breastplate shield and sword").GetComponent<Renderer>().sharedMaterials.Length,Is.EqualTo(4));
            }
            finally{Object.Destroy(go);}yield return null;
        }
    }
}
