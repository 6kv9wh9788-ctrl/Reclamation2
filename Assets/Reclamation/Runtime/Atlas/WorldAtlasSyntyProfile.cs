using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Reclamation.Atlas
{
    public sealed partial class WorldAtlasDemo
    {
        // Same-build A/B diagnostic, deliberately separate from combat outcome smoke.
        // Exercises pose/render cost at the existing company size, not the army target.
        private IEnumerator SyntyProfile()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--output");
            string output=at>=0&&at+1<args.Length?args[at+1]:Path.Combine(Application.temporaryCachePath,"SyntyProfile");Directory.CreateDirectory(output);
            AutomaticBattle=false;AutomaticClock=false;ResumeDefenseCheckpoint=false;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            string report="Local uncapped presentation diagnostic; not target-hardware certification.\n"+SystemInfo.processorType+" / "+SystemInfo.graphicsDeviceName+"\n"+Screen.width+"x"+Screen.height+"\n";
            foreach(var size in new[]{AtlasSize.Small,AtlasSize.Medium})
            foreach(bool company in new[]{false,true})
            {
                SyntyTrial=true;SyntyRoleTrial=true;SyntyCompanyTrial=company;LoadMap(size,size==AtlasSize.Small?4:8);yield return null;
                SetHour(12);SpawnAtlasRaiders();defensePanel=false;debug=false;zoom=24;pitch=25;
                // Identical visible lineup in both modes. No synthetic fighter damage.
                for(int i=1;i<battleActors.Count;i++)
                {battleActors[i].root.position=hero.position+hero.right*((i%8-3.5f)*1.6f)+hero.forward*(4+i/8*2);battleActors[i].moving=true;}
                UpdateCamera(true);
                var times=new List<float>();
                for(int frame=0;frame<720;frame++)
                {
                    SyncBattleVisuals(1f/60);
                    yield return null;
                    if(frame>=120)times.Add(Time.unscaledDeltaTime*1000);
                }
                times.Sort();float sum=0;foreach(float ms in times)sum+=ms;
                int count=0,vertices=0,skins=0;foreach(var v in GetComponentsInChildren<AtlasSyntyVisual>(true)){count++;vertices+=v.SkinVertices;skins+=v.GetComponentsInChildren<SkinnedMeshRenderer>().Length;}
                report+=size+" / "+(company?"full company":"19-role baseline")+" / actors "+count+" / skin renderers "+skins+" / allocated vertices "+vertices+" / samples "+times.Count+" / mean ms "+(sum/times.Count).ToString("F2")+" / median ms "+times[times.Count/2].ToString("F2")+" / p95 ms "+times[(int)(times.Count*.95f)].ToString("F2")+"\n";
                ScreenCapture.CaptureScreenshot(Path.Combine(output,size+"-"+company+".png"));yield return new WaitForSeconds(.2f);
            }
            File.WriteAllText(Path.Combine(output,"result.txt"),report);Application.Quit(0);
        }
    }
}
