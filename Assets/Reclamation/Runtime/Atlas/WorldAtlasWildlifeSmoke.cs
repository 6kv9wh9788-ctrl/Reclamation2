using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Reclamation.Atlas
{
    public sealed partial class WorldAtlasDemo
    {
        private bool wildlifePhotoView;
        private IEnumerator WildlifeSmoke()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--output");string output=index>=0?args[index+1]:Path.Combine(Application.persistentDataPath,"WildlifeSmoke");Directory.CreateDirectory(output);
            bool failed=false;Application.LogCallback log=(s,stack,type)=>{if(type==LogType.Exception||type==LogType.Error)failed=true;};Application.logMessageReceived+=log;AutomaticClock=false;
            foreach(var config in new[]{(AtlasSize.Small,4),(AtlasSize.Medium,8),(AtlasSize.Medium,12)})
            {
                LoadMap(config.Item1,config.Item2);VisitCommander();SetHour(12);commanderPanel=false;yield return new WaitForSeconds(.3f);
                string label=config.Item1+"-"+config.Item2;failed|=ActiveWildlifeCount!=18;
                ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-outfits.png"));yield return new WaitForSeconds(.3f);
                paused=true;wildlifePhotoView=true;hero.gameObject.SetActive(false);
                foreach(int animal in new[]{0,6,10})
                {
                    hero.position=animals[animal].root.position+Vector3.down*.9f;yaw=animals[animal].root.eulerAngles.y+145;pitch=15;zoom=animals[animal].kind==AtlasAnimalKind.Deer?4:2;UpdateCamera(true);
                    yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-"+animals[animal].kind+".png"));yield return new WaitForSeconds(.3f);
                }
                wildlifePhotoView=false;hero.gameObject.SetActive(true);paused=false;hero.position=animals[6].root.position+Vector3.back*2;TickWildlife(.1f);failed|=animals[6].state!=AtlasAnimalState.Fleeing;
                VisitCommander();SetHour(23);yield return new WaitForSeconds(.3f);failed|=ActiveTorchLights>4;
                ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-night.png"));yield return new WaitForSeconds(.3f);
            }
            File.WriteAllText(Path.Combine(output,"result.txt"),"Passed: "+!failed+"\n18 bounded animals, rabbit/deer/bird captures, approach reaction, outfits and night material check on small/4, medium/8, medium/12.\nWildlife cosmetic; no hunting, reproduction or rewards.");Application.logMessageReceived-=log;Application.Quit(failed?1:0);
        }
    }
}
