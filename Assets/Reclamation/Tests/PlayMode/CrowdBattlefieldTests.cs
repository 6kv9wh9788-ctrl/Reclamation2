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
    public sealed class CrowdBattlefieldTests
    {
        [UnityTest] public IEnumerator LayersPreserveCombatStateAndRestorePipeline()
        {
            var pipeline=QualitySettings.renderPipeline;var root=new GameObject("Battlefield layer test");
            var lab=root.AddComponent<BattleCrowdPerformanceLab>();lab.Automatic=false;lab.Reset(1280,true);
            try
            {
                var sim=lab.Simulation;var fighter=sim.Units[0].fighter;Vector3 position=sim.Units[0].position;
                foreach(int flags in new[]{0,1,2,4,7})
                {
                    lab.SetLayers((flags&1)!=0,(flags&2)!=0,(flags&4)!=0);
                    Assert.That(lab.Simulation,Is.SameAs(sim));Assert.That(sim.Units[0].fighter,Is.SameAs(fighter));Assert.That(sim.Units[0].position,Is.EqualTo(position));
                    Assert.That(sim.Steps,Is.Zero);Assert.That(lab.EnvironmentLayer.PropInstances,Is.EqualTo((flags&1)!=0?128:0));
                }
                var a=lab.EnvironmentLayer.Assets;Assert.That(a.terrain.vertexCount,Is.EqualTo(10201));Assert.That(a.equipment.Length,Is.EqualTo(192));
                Assert.That(a.Gear(0,0,0,0).vertexCount,Is.GreaterThan(a.Gear(0,1,0,0).vertexCount));
                Assert.That(a.Gear(0,0,0,0).boneWeights.Length,Is.Zero);Assert.That(QualitySettings.renderPipeline,Is.SameAs(a.pipeline));
            }
            finally{Object.Destroy(root);}
            yield return null;Assert.That(QualitySettings.renderPipeline,Is.SameAs(pipeline));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ShadowToggleChangesRenderedGroundAndCombinedSceneRenders()
        {
            var root=new GameObject("Battlefield render test");var lab=root.AddComponent<BattleCrowdPerformanceLab>();lab.Automatic=false;lab.Reset(1280,false);
            foreach(Transform t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;lab.View.cullingMask=1<<30;
            string directory=Environment.GetEnvironmentVariable("RECLAMATION_CROWD_CAPTURES");
            Color32[] Capture(string name)
            {
                var rt=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);var previous=RenderTexture.active;
                try
                {
                    lab.Submit();lab.View.targetTexture=rt;lab.View.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                    if(!string.IsNullOrEmpty(directory)){Directory.CreateDirectory(directory);File.WriteAllBytes(Path.Combine(directory,name+".png"),image.EncodeToPNG());}
                    return image.GetPixels32();
                }
                finally{lab.View.targetTexture=null;RenderTexture.active=previous;rt.Release();Object.Destroy(rt);Object.Destroy(image);}
            }
            try
            {
                lab.SetLayers(false,false,false);yield return null;yield return null;var unshadowed=Capture("baseline");
                lab.SetLayers(false,true,false);yield return null;yield return null;var shadowed=Capture("shadows");
                int darker=0;for(int i=0;i<shadowed.Length;i++)if(unshadowed[i].r>20 && unshadowed[i].g>20 && unshadowed[i].b>20 && Mathf.Max(unshadowed[i].r,Mathf.Max(unshadowed[i].g,unshadowed[i].b))-Mathf.Min(unshadowed[i].r,Mathf.Min(unshadowed[i].g,unshadowed[i].b))<30 && unshadowed[i].r+unshadowed[i].g+unshadowed[i].b-(shadowed[i].r+shadowed[i].g+shadowed[i].b)>18)darker++;
                Assert.That(darker,Is.GreaterThan(100),"Shadow workload must produce visible darkening, not just enable a flag.");
                lab.SetLayers(true,true,true);yield return null;yield return null;Capture("combined");LogAssert.NoUnexpectedReceived();
            }
            finally{Object.Destroy(root);}
            yield return null;
        }
    }
}

