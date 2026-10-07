using System.Collections;
using NUnit.Framework;
using Reclamation.Atlas;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class AtlasPresentationTests
    {
        [UnityTest] public IEnumerator GuardsTurnGraduallyAndRaiseWeaponsOnlyOnResponse()
        {
            var go = new GameObject("Guard presentation acceptance");
            var demo = go.AddComponent<WorldAtlasDemo>(); yield return null;
            try
            {
                demo.AutomaticClock = false; demo.VisitCommander(); demo.SetHour(9);
                for (int i=0;i<25;i++) Assert.That(demo.GuardAlertness(i), Is.Zero);
                bool observedTurn=false;
                for(int step=0;step<500;step++)
                {
                    var facing=new Vector3[25];
                    for(int i=0;i<25;i++) facing[i]=demo.GuardVisualFacing(i);
                    demo.TickVillage(.1f);
                    for(int i=0;i<25;i++)
                    {
                        float angle=Vector3.Angle(facing[i],demo.GuardVisualFacing(i));
                        Assert.That(angle,Is.LessThanOrEqualTo(18.05f),"Presentation must not snap on route corners");
                        observedTurn |= angle > 1;
                    }
                }
                Assert.That(observedTurn,Is.True);
                Assert.That(demo.BeginScoutEncounter(),Is.True);
                for(int i=0;i<1500&&demo.ScoutPhase!=ScoutEncounterPhase.Responding;i++)demo.TickVillage(.2f);
                Assert.That(demo.ScoutPhase,Is.EqualTo(ScoutEncounterPhase.Responding));
                int first=1+demo.ActiveCompany.RespondingPlatoon*6;
                float start=demo.GuardAlertness(first);demo.TickVillage(.1f);
                Assert.That(demo.GuardAlertness(first),Is.InRange(start,Mathf.Min(1,start+.201f)));
                for(int i=0;i<10;i++)demo.TickVillage(.1f);
                Assert.That(demo.GuardAlertness(first),Is.EqualTo(1));
                Assert.That(demo.GuardAlertness(0),Is.Zero,"Company commander does not inherit reserve alert pose");
                var before=demo.GuardVisualFacing(first);demo.SurveyPaused=true;demo.TickVillage(1);
                Assert.That(Vector3.Angle(before,demo.GuardVisualFacing(first)),Is.LessThan(.01f));
                demo.SurveyPaused=false;
                for(int i=0;i<2500&&demo.ScoutPhase!=ScoutEncounterPhase.Secured;i++)demo.TickVillage(.2f);
                Assert.That(demo.ScoutPhase,Is.EqualTo(ScoutEncounterPhase.Secured));
                for(int i=0;i<10;i++)demo.TickVillage(.1f);
                Assert.That(demo.GuardAlertness(first),Is.Zero,"Returning guards relax their weapons");
            }
            finally { Object.Destroy(go); } yield return null;
        }

        [Test] public void GuardPoseBanksMatchEquipmentAndKeepOriginalLibrary()
        {
            var poses=Resources.Load<AtlasCharacterLibrary>("AtlasGuardPoses");
            var kit=Resources.Load<AtlasOutfitLibrary>("AtlasGuardOutfits");
            var original=Resources.Load<AtlasCharacterLibrary>("AtlasCharacters");
            Assert.That(original.body.Length,Is.EqualTo(33));
            Assert.That(poses.body.Length,Is.EqualTo(165));
            Assert.That(kit.gear.Length,Is.EqualTo(poses.body.Length));
            for(int i=0;i<poses.body.Length;i++)
            {
                Assert.That(poses.body[i].vertexCount,Is.EqualTo(original.body[i%33].vertexCount));
                Assert.That(kit.gear[i].subMeshCount,Is.EqualTo(4));
                Assert.That(kit.details[i],Is.Not.Null);
                Assert.That(float.IsNaN(poses.hand[i].x),Is.False);
            }
            Assert.That(Vector3.Distance(poses.hand[32],poses.hand[164]),Is.GreaterThan(.1f));
        }
    }
}

