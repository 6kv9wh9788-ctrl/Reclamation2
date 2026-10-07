using System.Collections;
using System.Linq;
using NUnit.Framework;
using Reclamation.Atlas;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class WorldAtlasTests
    {
        [TestCase(AtlasSize.Small,2)] [TestCase(AtlasSize.Small,4)]
        [TestCase(AtlasSize.Medium,8)] [TestCase(AtlasSize.Medium,12)]
        public void PresetsHaveRoomResourcesAndSafeStarts(AtlasSize size,int count)
        {
            var model=new WorldAtlasModel(size,count);Assert.That(model.factions.Count,Is.EqualTo(count));Assert.That(model.sites.Count,Is.GreaterThanOrEqualTo(count*6));
            Assert.That(model.factions.Select(f=>f.capital).Distinct().Count(),Is.EqualTo(count));
            Assert.That(model.sites.Count(s=>s.owner==-2),Is.EqualTo(2));Assert.That(model.sites.Count(s=>s.owner==-3),Is.EqualTo(2));
            foreach(var s in model.sites)
            {
                foreach(var kind in new[]{AtlasResource.Food,AtlasResource.Wood,AtlasResource.Stone})Assert.That(s.deposits.Any(d=>d.kind==kind&&Vector2.Distance(d.point,s.point)<85),Is.True);
                Assert.That(model.Walkable(s.point+Vector2.down*40),Is.True,s.name);
            }
            Assert.That(model.sites.Count(s=>s.deposits.Any(d=>d.kind==AtlasResource.Gold)),Is.InRange(1,model.sites.Count-1));
            Assert.That(model.sites.Count(s=>s.deposits.Any(d=>d.kind==AtlasResource.Niter)),Is.InRange(1,model.sites.Count-1));
        }
        [Test] public void StrategyExpandsFormsAffinityAndPreservesEnclaves()
        {
            var model=new WorldAtlasModel(AtlasSize.Small,4);var enclaves=model.sites.Where(s=>s.owner==-2).Select(s=>s.id).ToArray();
            for(int i=0;i<4;i++)model.AdvanceStrategy();
            Assert.That(model.Holdings(1),Is.GreaterThan(1));Assert.That(model.Relation(0,1),Is.EqualTo("Allied"));Assert.That(model.Relation(0,2),Is.EqualTo("Hostile"));
            foreach(int id in enclaves)Assert.That(model.sites[id].owner,Is.EqualTo(-2));
            Assert.That(model.sites.All(s=>s.owner>=-3&&s.owner<4),Is.True);
        }
        [Test] public void BotCanWinTheAbstractContest()
        {
            var model=new WorldAtlasModel(AtlasSize.Small,2);for(int i=0;i<300&&model.Winner<0;i++)model.AdvanceStrategy();
            Assert.That(model.Winner,Is.EqualTo(1));Assert.That(model.Holdings(0),Is.Zero);
        }
        [Test] public void ClaimsCannotTakeOccupiedLandAndBoundsPreventLeavingMap()
        {
            var model=new WorldAtlasModel(AtlasSize.Small,4);int free=model.sites.FindIndex(s=>s.owner==-1);
            Assert.That(model.Claim(free,0),Is.True);Assert.That(model.Claim(free,1),Is.False);
            Assert.That(model.Claim(-1,0),Is.False);Assert.That(model.Walkable(new Vector2(model.Extent,0)),Is.False);
            Assert.Throws<System.ArgumentException>(()=>new WorldAtlasModel(AtlasSize.Small,12));
        }
        [UnityTest] public IEnumerator ExplorerBuildsSwitchesTravelsAndClaims()
        {
            var go=new GameObject("Atlas integration");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try
            {
                demo.VisitVillage();yield return null;Assert.That(demo.ActiveVillagerCount,Is.EqualTo(18));Assert.That(demo.SceneryInstanceCount,Is.GreaterThan(100));
                Assert.That(demo.Model.factions.Count,Is.EqualTo(4));int free=demo.Model.sites.FindIndex(s=>s.owner==-1);demo.Travel(free);Assert.That(demo.ClaimNearby(),Is.True);
                demo.LoadMap(AtlasSize.Medium,12);yield return null;
                demo.VisitVillage();yield return null;Assert.That(demo.ActiveVillagerCount,Is.EqualTo(18));
                int empty=demo.Model.sites.FindIndex(s=>s.owner==-1);demo.Travel(empty);yield return null;Assert.That(demo.ActiveVillagerCount,Is.Zero);
                Assert.That(demo.Model.factions.Count,Is.EqualTo(12));Assert.That(demo.Model.Walkable(new Vector2(demo.PlayerPosition.x,demo.PlayerPosition.z)),Is.True);
            }
            finally {Object.Destroy(go);}
            yield return null;
        }
    }
}
