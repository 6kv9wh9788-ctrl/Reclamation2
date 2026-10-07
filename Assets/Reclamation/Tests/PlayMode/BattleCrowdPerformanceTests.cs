using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Reclamation.Tests
{
    public sealed class BattleCrowdPerformanceTests
    {
        [Test] public void TargetPopulationHas256FriendlyAnd1024EnemyUnits()
        {
            var sim=new CrowdBattleSimulation(1280);Assert.That(sim.FriendlyCount,Is.EqualTo(256));Assert.That(sim.Units.Length-sim.FriendlyCount,Is.EqualTo(1024));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new CrowdBattleSimulation(1281));
        }
        [Test] public void CombatProducesDamageAndSustainsThePopulationDeterministically()
        {
            var a=new CrowdBattleSimulation(64);var b=new CrowdBattleSimulation(64);
            for(int i=0;i<900;i++){a.Step(1f/60f);b.Step(1f/60f);}
            Assert.That(a.Hits,Is.GreaterThan(0));Assert.That(a.Attacks,Is.GreaterThan(0));Assert.That(a.Respawns,Is.GreaterThan(0));
            Assert.That(a.Hits,Is.EqualTo(b.Hits));Assert.That(a.Units.Length,Is.EqualTo(64));
            for(int i=0;i<a.Units.Length;i++)
            {
                Assert.That(a.Units[i].position,Is.EqualTo(b.Units[i].position));
                int target=a.Units[i].target;if(target>=0)Assert.That(a.Units[target].team,Is.Not.EqualTo(a.Units[i].team));
            }
        }
        [Test] public void OrdersMoveOnlyTheFriendlyPolicyAndHoldRetainsPosition()
        {
            var sim=new CrowdBattleSimulation(64);sim.FriendlyOrder=CrowdBattleSimulation.Order.Hold;
            Vector3 home=sim.Units[0].position;sim.Step(1f/60f);Assert.That(sim.Units[0].position,Is.EqualTo(home));
            sim=new CrowdBattleSimulation(64);home=sim.Units[0].position;sim.FriendlyOrder=CrowdBattleSimulation.Order.Retreat;sim.Step(1f/60f);
            Assert.That(sim.Units[0].position.z,Is.LessThan(home.z));
        }
        [Test] public void PercentilesUseFrameTimesRatherThanAveragedFps()
        {Assert.That(BattleCrowdPerformanceLab.Percentile(new double[]{1,2,3,4,100},.95),Is.EqualTo(100));}
        [UnityTest] public IEnumerator LibraryAndRenderedCrowdRemainSmallAndShared()
        {
            var library=Resources.Load<CrowdAnimationLibrary>("CrowdPerformance/AnimationLibrary");Assert.That(library,Is.Not.Null);
            Assert.That(library.nearRun.Length,Is.EqualTo(16));Assert.That(library.nearRun[0].vertexCount,Is.LessThan(1500));
            Assert.That(library.farRun[0].vertexCount,Is.LessThan(library.nearRun[0].vertexCount));Assert.That(library.nearRun[0].boneWeights.Length,Is.Zero);
            Assert.That(library.friendly.enableInstancing,Is.True);Assert.That(library.friendly.shader.isSupported,Is.True);
            var root=new GameObject("Disposable crowd lab");var lab=root.AddComponent<BattleCrowdPerformanceLab>();lab.Automatic=false;
            foreach(Transform t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=30;lab.View.cullingMask=1<<30;
            try
            {
                lab.Reset(1280,false);Assert.That(root.GetComponentsInChildren<Animator>().Length,Is.Zero);Assert.That(root.GetComponentsInChildren<SkinnedMeshRenderer>().Length,Is.Zero);
                yield return null;lab.Submit();
                string directory=Environment.GetEnvironmentVariable("RECLAMATION_CROWD_CAPTURES");
                if(!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);var rt=new RenderTexture(1600,900,24);var image=new Texture2D(1600,900,TextureFormat.RGB24,false);var previous=RenderTexture.active;
                    try{lab.View.targetTexture=rt;lab.View.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(directory,"crowd-1280.png"),image.EncodeToPNG());}
                    finally{lab.View.targetTexture=null;RenderTexture.active=previous;rt.Release();Object.Destroy(rt);Object.Destroy(image);}
                }
                LogAssert.NoUnexpectedReceived();
            }
            finally{Object.Destroy(root);}
            yield return null;
        }
    }
}
