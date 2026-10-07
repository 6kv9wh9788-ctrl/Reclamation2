using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Reclamation.Atlas
{
    public sealed partial class WorldAtlasDemo
    {
        private IEnumerator VisibilitySmoke()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--output");string output=i>=0?args[i+1]:Path.Combine(Application.persistentDataPath,"VisibilitySmoke");Directory.CreateDirectory(output);
            bool failed=false;Application.LogCallback log=(s,stack,type)=>{if(type==LogType.Error||type==LogType.Exception)failed=true;};Application.logMessageReceived+=log;
            AutomaticClock=false;
            foreach(var config in new[]{(AtlasSize.Small,4),(AtlasSize.Medium,8),(AtlasSize.Medium,12)})
            {
                LoadMap(config.Item1,config.Item2);VisitCommander();commanderPanel=false;SetHour(12);yield return new WaitForSeconds(.3f);RefreshKnowledge();string label=config.Item1+"-"+config.Item2;
                failed|=Knowledge.ReportedOwner(Model.factions[1].capital)!=AtlasKnowledge.UnknownOwner||rig.BodyVertices>1000;
                ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-commander.png"));yield return new WaitForSeconds(.3f);
                mapOpen=true;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-fog.png"));yield return new WaitForSeconds(.3f);mapOpen=false;
                SetHour(23);yield return new WaitForSeconds(.3f);failed|=LitGuardTorches!=25||ActiveTorchLights>4||ActiveTorchLights==0;
                ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-torches.png"));yield return new WaitForSeconds(.3f);
                SetHour(9);failed|=!BeginScoutEncounter();yield return null;failed|=hasScoutReport;
                for(int step=0;step<1500&&ScoutPhase!=ScoutEncounterPhase.Responding&&scoutEncounter.Active;step++)TickVillage(.2f);
                failed|=!VisitScoutContact();yield return new WaitForSeconds(.3f);failed|=!hasScoutReport;
                ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-report.png"));yield return new WaitForSeconds(.3f);
            }
            File.WriteAllText(Path.Combine(output,"result.txt"),"Passed: "+!failed+"\nFog, reported contact, prototype bodies, commander and night torches on small/4, medium/8 and medium/12. Max four unshadowed watch lights.\nCartographic fog; no new damage combat. Commander health is a presentation placeholder.");
            Application.logMessageReceived-=log;Application.Quit(failed?1:0);
        }
    }
}
