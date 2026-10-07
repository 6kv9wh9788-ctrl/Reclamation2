using System.Collections;
using NUnit.Framework;
using Reclamation.Atlas;
using UnityEngine;
using UnityEngine.TestTools;
namespace Reclamation.Tests
{
    public sealed class AtlasVisibilityTests
    {
        [Test] public void UnknownEnemyExplorationAndLastReportedOwnership()
        {
            var model=new WorldAtlasModel(AtlasSize.Small,4);var k=new AtlasKnowledge(model);var pixels=new Color32[128*128];
            var enemy=model.sites[model.factions[1].capital];
            k.BeginObservation();k.FinishObservation(pixels);
            Assert.That(k.Visible(enemy.point),Is.False);Assert.That(k.ReportedOwner(enemy.id),Is.EqualTo(AtlasKnowledge.UnknownOwner));
            k.Reveal(enemy.point,90);k.FinishObservation(pixels);Assert.That(k.ReportedOwner(enemy.id),Is.EqualTo(1));
            enemy.owner=2;k.BeginObservation();k.FinishObservation(pixels);
            Assert.That(k.Visible(enemy.point),Is.False);Assert.That(k.Explored(enemy.point),Is.True);Assert.That(k.ReportedOwner(enemy.id),Is.EqualTo(1),"Hidden ownership must not update intelligence");
            k.Reveal(enemy.point,90);k.FinishObservation(pixels);Assert.That(k.ReportedOwner(enemy.id),Is.EqualTo(2));
        }
        [Test] public void AllianceSharesTerritoryAndLossReturnsToExplored()
        {
            var model=new WorldAtlasModel(AtlasSize.Medium,8);var k=new AtlasKnowledge(model);var pixels=new Color32[128*128];var ally=model.sites[model.factions[1].capital];
            model.factions[1].alliance=model.factions[0].alliance;k.BeginObservation();k.FinishObservation(pixels);Assert.That(k.Visible(ally.point),Is.True);
            model.factions[1].alliance=99;k.BeginObservation();k.FinishObservation(pixels);Assert.That(k.Visible(ally.point),Is.False);Assert.That(k.Explored(ally.point),Is.True);
            var fresh=new AtlasKnowledge(model);fresh.BeginObservation();fresh.FinishObservation(pixels);Assert.That(fresh.Explored(ally.point),Is.False);
        }
        [UnityTest] public IEnumerator LostContactDoesNotTrackOrResurrectWithoutObservation()
        {
            var go=new GameObject("Contact memory test");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try{
                demo.AutomaticClock=false;demo.VisitCommander();Assert.That(demo.BeginScoutEncounter(),Is.True);
                const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                var encounter=(AtlasScoutEncounter)typeof(WorldAtlasDemo).GetField("scoutEncounter",flags).GetValue(demo);
                encounter.Tick(1,true,0,false);encounter.Tick(3,false,0,false);
                demo.Travel(demo.Model.factions[1].capital);
                foreach(var b in go.GetComponentsInChildren<AtlasCharacterVisual>())if(b.name.StartsWith("Company"))b.gameObject.SetActive(false);
                var update=typeof(WorldAtlasDemo).GetMethod("UpdateRecon",flags);
                update.Invoke(demo,new object[]{0f});Assert.That(demo.HasScoutMapReport,Is.True);
                Vector2 reported=demo.ReportedScoutPosition;
                var position=typeof(WorldAtlasDemo).GetField("scoutLocal",flags);position.SetValue(demo,(Vector2)position.GetValue(demo)+Vector2.one*80);
                update.Invoke(demo,new object[]{10f});Assert.That(demo.ReportedScoutPosition,Is.EqualTo(reported),"Hidden movement must not update map contact");
                update.Invoke(demo,new object[]{51f});Assert.That(demo.HasScoutMapReport,Is.False);
                update.Invoke(demo,new object[]{0f});Assert.That(demo.HasScoutMapReport,Is.False,"Expiry must not create a new report");
            }finally{Object.Destroy(go);}yield return null;
        }
        [UnityTest] public IEnumerator PrototypeBodiesNightTorchesAndMapReset()
        {
            var go=new GameObject("Visibility integration");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try{
                demo.AutomaticClock=false;demo.VisitCommander();demo.SetHour(23);yield return null;yield return null;
                Assert.That(demo.LitGuardTorches,Is.EqualTo(25));Assert.That(demo.ActiveTorchLights,Is.InRange(1,4));
                var bodies=go.GetComponentsInChildren<AtlasCharacterVisual>();Assert.That(bodies.Length,Is.EqualTo(26));
                var variants=new System.Collections.Generic.HashSet<int>();foreach(var b in bodies){Assert.That(b.BodyVertices,Is.LessThan(1000));variants.Add(b.Variant);}Assert.That(variants.Count,Is.GreaterThan(10));
                demo.SetHour(12);Assert.That(demo.LitGuardTorches,Is.Zero);Assert.That(demo.ActiveTorchLights,Is.Zero);
                demo.RefreshKnowledge();Assert.That(demo.Knowledge.Visible(new Vector2(demo.PlayerPosition.x,demo.PlayerPosition.z)),Is.True);
                Assert.That(demo.BeginScoutEncounter(),Is.True);yield return null;Assert.That(demo.HasScoutMapReport,Is.False,"Unreported scouts must not appear on minimap");
                demo.LoadMap(AtlasSize.Medium,8);yield return null;Assert.That(demo.HasScoutMapReport,Is.False);Assert.That(demo.Knowledge.ReportedOwner(demo.Model.factions[1].capital),Is.EqualTo(AtlasKnowledge.UnknownOwner));
            }finally{Object.Destroy(go);}yield return null;
        }
    }
}
