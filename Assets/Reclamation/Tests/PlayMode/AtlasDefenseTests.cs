using System.Collections;
using System.IO;
using NUnit.Framework;
using Reclamation.Atlas;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class AtlasDefenseTests
    {
        private static void Step(WorldAtlasDemo d,float seconds){for(int i=0;i<Mathf.CeilToInt(seconds*60);i++)d.SimulateAtlasBattle(1f/60);}
        private static IEnumerator Create(System.Action<WorldAtlasDemo> run)
        {
            var go=new GameObject("Atlas defense acceptance");var d=go.AddComponent<WorldAtlasDemo>();yield return null;
            try{d.ResumeDefenseCheckpoint=false;d.EnableAtlasDefense();d.AutomaticBattle=false;run(d);}finally{Object.Destroy(go);}yield return null;
        }
        [UnityTest] public IEnumerator UsesApprovedWalkDodgeAndAttackCommitment()
        {
            yield return Create(d=>{
                Vector3 before=d.PlayerPosition;d.SetAtlasBattleInput(Vector3.forward);Step(d,1);
                Assert.That(Vector3.Distance(before,d.PlayerPosition),Is.InRange(3.65f,3.95f));
                d.SetAtlasBattleInput(Vector3.zero);Assert.That(d.RequestAtlasDodge(),Is.True);
                Assert.That(d.AtlasPlayerFighter.Stamina,Is.EqualTo(75).Within(.01f));
                before=d.PlayerPosition;d.SimulateAtlasBattle(.32f);
                Assert.That(Vector3.Distance(before,d.PlayerPosition),Is.InRange(2.45f,2.7f));
                Step(d,.1f);Assert.That(d.RequestAtlasAttack(false),Is.True);
                Assert.That(d.RequestAtlasAttack(true),Is.False);Assert.That(d.RequestAtlasDodge(),Is.False);
                Assert.That(d.AtlasPlayerFighter.Strike.Windup,Is.EqualTo(.24f));
            });
        }
        [UnityTest] public IEnumerator WarningPauseAndFixedPostsWorkOnBothMaps()
        {
            yield return Create(d=>{
                foreach(var size in new[]{AtlasSize.Small,AtlasSize.Medium})
                {
                    d.LoadMap(size,size==AtlasSize.Small?4:8);d.AutomaticBattle=false;
                    Assert.That(d.BattleSurvivors,Is.EqualTo(25));Assert.That(d.BeginAtlasDefense(true),Is.True);
                    Vector3 commander=d.GuardPosition(0);d.SurveyPaused=true;Step(d,4);
                    Assert.That(d.BattleWarning,Is.EqualTo(20));d.SurveyPaused=false;Step(d,2);
                    Assert.That(Vector3.Distance(commander,d.GuardPosition(0)),Is.LessThan(.05f));
                    Assert.That(d.BattleWarning,Is.InRange(17.9f,18.1f));
                    Assert.That(d.SaveAtlasDefense(Path.Combine(Application.temporaryCachePath,"forbidden-midraid.json")),Is.False);
                }
            });
        }
        [UnityTest] public IEnumerator ReserveAdvancesWhileOtherPostsRemainCovered()
        {
            yield return Create(d=>{
                Assert.That(d.BeginAtlasDefense(true),Is.True);
                int leader=1+d.BattleReservePlatoon*6;
                float before=Vector3.Distance(d.GuardPosition(leader),d.DefenseApproachPoint);
                Vector3 commander=d.GuardPosition(0);
                for(int i=0;i<15*60;i++){d.TickVillage(1f/60);d.SimulateAtlasBattle(1f/60);}
                Assert.That(Vector3.Distance(d.GuardPosition(leader),d.DefenseApproachPoint),Is.LessThan(before-20));
                Assert.That(Vector3.Distance(commander,d.GuardPosition(0)),Is.LessThan(.05f));
                for(int i=0;i<25;i++)Assert.That(d.IsSolidAt(d.GuardPosition(i)),Is.False);
            });
        }
        [UnityTest] public IEnumerator RaidersReachTheCompanyAndResolveThroughCombat()
        {
            yield return Create(d=>{
                foreach(var size in new[]{AtlasSize.Small,AtlasSize.Medium})
                {
                    d.LoadMap(size,size==AtlasSize.Small?4:8);d.AutomaticBattle=false;
                    Assert.That(d.BeginAtlasDefense(true),Is.True);
                    for(int i=0;i<150*60&&d.DefenseActive;i++)d.SimulateAtlasBattle(1f/60);
                    Assert.That(d.DefenseState.progression.completed,Is.EqualTo(1),d.AtlasBattleDiagnostic());
                }
            });
        }
        [Test] public void CombatPoseBanksMatchExistingBodyAndEquipment()
        {
            var p=Resources.Load<AtlasCharacterLibrary>("AtlasCombatPoses");var kit=Resources.Load<AtlasOutfitLibrary>("AtlasCombatOutfits");
            Assert.That(p.body.Length,Is.EqualTo(97));Assert.That(kit.gear.Length,Is.EqualTo(97));
            Assert.That(Resources.Load<AtlasCharacterLibrary>("AtlasCharacters").body.Length,Is.EqualTo(33));
            Assert.That(Resources.Load<AtlasCharacterLibrary>("AtlasGuardPoses").body.Length,Is.EqualTo(165));
            for(int i=0;i<97;i++){Assert.That(p.body[i],Is.Not.Null);Assert.That(kit.gear[i].subMeshCount,Is.EqualTo(4));Assert.That(kit.details[i],Is.Not.Null);}
        }
        [UnityTest] public IEnumerator PerksAndCasualtiesPersistAndInvalidMapIsRejected()
        {
            yield return Create(d=>{
                var p=d.DefenseState.progression;p.completed=1;p.playerXp=150;p.commanderXp=150;p.outcome="Test outcome";
                Assert.That(d.ChooseAtlasFounder(FounderPerk.FieldDressing),Is.True);
                Assert.That(d.ChooseAtlasCaptain(CaptainPerk.WatchCaptain),Is.True);
                d.AtlasDefenseFighter(4).Receive(100,false,false);
                string path=Path.Combine(Application.temporaryCachePath,"atlas-defense-"+System.Guid.NewGuid()+".json");
                try
                {
                    Assert.That(d.RestAtlasCompany(),Is.True);Assert.That(d.BattleSurvivors,Is.EqualTo(24));
                    Assert.That(d.SaveAtlasDefense(path),Is.True);Assert.That(d.LoadAtlasDefense(path),Is.True);
                    Assert.That(d.BattleSurvivors,Is.EqualTo(24));Assert.That(d.DefenseState.food,Is.EqualTo(8));
                    Assert.That(d.DefenseState.progression.playerPerk,Is.EqualTo(1));
                    string original=File.ReadAllText(path);File.WriteAllText(path,original.Replace("\"seed\": "+d.DefenseState.seed,"\"seed\": -999"));
                    Assert.That(d.LoadAtlasDefense(path),Is.False);Assert.That(d.BattleSurvivors,Is.EqualTo(24));
                }finally{if(File.Exists(path))File.Delete(path);if(File.Exists(path+".bak"))File.Delete(path+".bak");}
            });
        }
    }
}
