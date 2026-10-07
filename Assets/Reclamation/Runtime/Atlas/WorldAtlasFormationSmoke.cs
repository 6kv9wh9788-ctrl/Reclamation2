using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Reclamation.Atlas
{
    public sealed partial class WorldAtlasDemo
    {
        private IEnumerator GuardPosePhotos(string output, string label, int guard)
        {
            Vector3 oldPosition=hero.position;float oldYaw=yaw,oldPitch=pitch,oldZoom=zoom;
            paused=true;wildlifePhotoView=true;hero.gameObject.SetActive(false);
            foreach(float angle in new[]{155f,245f})
            {
                hero.position=guards[guard].root.position+Vector3.down*.5f;
                yaw=guards[guard].rig.transform.eulerAngles.y+angle;pitch=15;zoom=3.5f;UpdateCamera(true);
                yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-"+angle+".png"));yield return new WaitForSeconds(.3f);
            }
            hero.position=oldPosition;yaw=oldYaw;pitch=oldPitch;zoom=oldZoom;
            hero.gameObject.SetActive(true);wildlifePhotoView=false;paused=false;UpdateCamera(true);
        }
        private IEnumerator FormationSmoke()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--output");
            string output=index>=0?args[index+1]:Path.Combine(Application.persistentDataPath,"FormationSmoke");
            Directory.CreateDirectory(output);bool failed=false;
            Application.LogCallback observe=(text,stack,type)=>{if(type==LogType.Error||type==LogType.Exception)failed=true;};
            Application.logMessageReceived+=observe;AutomaticClock=false;
            foreach(var config in new[]{(AtlasSize.Small,4),(AtlasSize.Medium,8),(AtlasSize.Medium,12)})
            {
                LoadMap(config.Item1,config.Item2);VisitCommander();SetHour(9);debug=false;
                string label=config.Item1+"-"+config.Item2;
                yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-compact-badges.png"));yield return new WaitForSeconds(.3f);
                yield return GuardPosePhotos(output, label+"-relaxed", 1);
                failed|=!BeginScoutEncounter();
                for(int i=0;i<1500&&ScoutPhase!=ScoutEncounterPhase.Responding;i++)TickVillage(.2f);
                failed|=ScoutPhase!=ScoutEncounterPhase.Responding;
                int p=ActiveCompany.RespondingPlatoon;if(p<0)continue;
                for(int i=0;i<200;i++)TickVillage(.2f);
                Vector3 leader=GuardPosition(1+p*6);hero.position=leader+Vector3.left*9+Vector3.back*7;
                hero.position=HinterlandGround(new Vector2(hero.position.x,hero.position.z));yaw=35;pitch=40;zoom=18;UpdateCamera(true);
                yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-column.png"));yield return new WaitForSeconds(.3f);
                for(int i=0;i<2000&&FormationPhase(p)!=AtlasFormationPhase.Line&&scoutEncounter.Active;i++)TickVillage(.2f);
                failed|=FormationPhase(p)!=AtlasFormationPhase.Line;
                VisitScoutContact();commanderPanel=false;debug=false;
                yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-line.png"));yield return new WaitForSeconds(.3f);
                yield return GuardPosePhotos(output, label+"-alert", 1+p*6);
                float gap=float.MaxValue;
                for(int i=1;i<6;i++)for(int j=i+1;j<6;j++)gap=Mathf.Min(gap,Vector3.Distance(GuardPosition(1+p*6+i),GuardPosition(1+p*6+j)));
                failed|=gap<1.5f;
                for(int i=0;i<600&&scoutEncounter.Active;i++)TickVillage(.2f);
                failed|=ScoutPhase!=ScoutEncounterPhase.Secured||FormationPhase(p)!=AtlasFormationPhase.Returning;
                for(int i=0;i<100;i++)TickVillage(.2f);
                yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-returning.png"));yield return new WaitForSeconds(.3f);
                for(int i=0;i<2400&&FormationPhase(p)==AtlasFormationPhase.Returning;i++)TickVillage(.2f);
                failed|=FormationPhase(p)!=AtlasFormationPhase.Posted;
                VisitCommander();debug=true;
                yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-home.png"));yield return new WaitForSeconds(.3f);
                File.WriteAllText(Path.Combine(output,label+"-metrics.txt"),"Final phase: "+FormationPhase(p)+"\nLine minimum troop spacing: "+gap+"\nEncounter: "+ScoutPhase);
            }
            File.WriteAllText(Path.Combine(output,"result.txt"),"Passed: "+!failed+"\nColumn, defensive line, autonomous deterrence, return to posts and compact badge captures across three presets.");
            Application.logMessageReceived-=observe;Application.Quit(failed?1:0);
        }
    }
}
