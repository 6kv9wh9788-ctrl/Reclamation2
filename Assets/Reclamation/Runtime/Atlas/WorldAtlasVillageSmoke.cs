using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Reclamation.Atlas
{
    public sealed partial class WorldAtlasDemo
    {
        private IEnumerator VillageSmoke()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--output");string output=index>=0?args[index+1]:Path.Combine(Application.persistentDataPath,"VillageSmoke");Directory.CreateDirectory(output);
            bool failed=false;Application.LogCallback observe=(text,stack,type)=>{if(type==LogType.Error||type==LogType.Exception)failed=true;};Application.logMessageReceived+=observe;
            AutomaticClock=false;
            foreach(var configuration in new[]{(AtlasSize.Small,4),(AtlasSize.Medium,8),(AtlasSize.Medium,12)})
            {
                var size=configuration.Item1;string label=size+"-"+configuration.Item2;LoadMap(size,configuration.Item2);VisitVillage();debug=true;yield return null;SetHour(9);yield return new WaitForSeconds(.3f);
                failed|=ActiveVillagerCount!=18;ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-street.png"));yield return new WaitForSeconds(.3f);
                var site=Model.sites[selected];var layout=LayoutFor(site.id);
                VisitCommander();SetHour(10);yield return new WaitForSeconds(.3f);
                failed|=ActiveGuardCount!=25;ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-commander.png"));yield return new WaitForSeconds(.3f);commanderPanel=false;
                for(int tick=0;tick<300;tick++)TickVillage(.2f);
                for(int guard=0;guard<25;guard++)failed|=IsSolidAt(GuardPosition(guard));
                foreach(var sample in new[]{(0,9f,"farming"),(1,9f,"chopping"),(4,9f,"mining"),(2,9f,"trading"),(0,19f,"drinking"),(1,19f,"eating")})
                {
                    SetHour(sample.Item2);Vector2 focus=site.point+AtlasVillageRoutine.Position(Hour,sample.Item1,layout);
                    hero.position=Ground(focus+Vector2.right*.65f);yaw=sample.Item1%3==2?layout.Angle+135:135;pitch=18;zoom=5;SurveyPaused=true;
                    foreach(var renderer in hero.GetComponentsInChildren<Renderer>())renderer.enabled=false;
                    for(int frame=0;frame<3;frame++)
                    {
                        UpdateVillagePeople(.85f);UpdateCamera(true);yield return null;
                        ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-"+sample.Item3+"-"+frame+".png"));yield return new WaitForSeconds(.15f);
                    }
                    foreach(var renderer in hero.GetComponentsInChildren<Renderer>())renderer.enabled=true;SurveyPaused=false;
                }
                VisitVillage();SetHour(9);

                for(int stop=0;stop<3;stop++)
                {
                    VisitWorkplace();SetHour(10);yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-workplace-"+stop+".png"));yield return new WaitForSeconds(.3f);
                    failed|=!Model.Walkable(new Vector2(hero.position.x,hero.position.z));
                }
                for(int resident=0;resident<18;resident++)if(resident%3!=2)
                    for(float hour=8.6f;hour<16.9f;hour+=.1f){Vector2 p=site.point+AtlasVillageRoutine.Position(hour,resident,layout);failed|=!Model.Walkable(p);}
                VisitVillage();SetHour(9);
                Vector3 prior=hero.position;selected=Model.sites.FindIndex(s=>s.owner==-1);VisitTownHall();failed|=hero.position!=prior;selected=site.id;

                SurveyPaused=true;var overview=site.point+layout.Hinterland(new Vector2(60,0));hero.position=Ground(overview);zoom=180;pitch=65;yaw=layout.HinterlandAngle+90;UpdateCamera(true);yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-hinterland.png"));yield return new WaitForSeconds(.3f);SurveyPaused=false;
                var high=site.point+layout.Rotate(new Vector2(0,-4));hero.position=new Vector3(high.x,Surface(high),high.y);zoom=70;pitch=50;UpdateCamera(true);
                yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-layout.png"));yield return new WaitForSeconds(.3f);
                PreviewFootprint(1);yield return null;
                for(int resident=0;resident<18;resident++)for(float hour=6;hour<22;hour+=.1f)
                {
                    Vector2 p=site.point+AtlasVillageRoutine.Position(hour,resident,layout);failed|=IsSolidAt(new Vector3(p.x,Surface(p),p.y));
                }
                ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-expansion-preview.png"));yield return new WaitForSeconds(.3f);
                if(footprintRoot){footprintRoot.gameObject.SetActive(false);Destroy(footprintRoot.gameObject);}
                VisitTownHall();yield return null;
                Vector2 doorway=site.point+layout.Rotate(new Vector2(0,30)),inside=site.point+layout.Rotate(new Vector2(0,36));
                var start=new Vector3(doorway.x,Surface(doorway),doorway.y);var end=new Vector3(inside.x,Surface(inside),inside.y);
                var entered=ResolveVillageMove(start,end-start);failed|=Vector3.Distance(entered,end)>.05f;hero.position=entered;zoom=8;pitch=18;UpdateDaylight();UpdateCamera(true);
                failed|=!IsInsideTownHall;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-hall-interior.png"));yield return new WaitForSeconds(.3f);
                hero.position=ResolveVillageMove(hero.position,start-hero.position);UpdateDaylight();failed|=IsInsideTownHall;
                Vector2 house=site.point+layout.House(0),door=site.point+layout.Door(0);var hp=new Vector3(house.x,Surface(house),house.y);var dp=new Vector3(door.x,Surface(door),door.y);
                failed|=Vector3.Distance(ResolveVillageMove(dp,(hp-dp)*2),hp)<3;
                VisitVillage();SetHour(19);yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-evening.png"));yield return new WaitForSeconds(.3f);
                SetHour(23);failed|=ActiveVillagerCount!=0;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-night.png"));yield return new WaitForSeconds(.3f);
                SurveyPaused=true;float before=Hour;TickVillage(120);failed|=Hour!=before;SurveyPaused=false;
                SetHour(7.5f);yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-commute.png"));yield return new WaitForSeconds(.3f);
            }
            File.WriteAllText(Path.Combine(output,"result.txt"),"Passed: "+!failed+"\nBoth map sizes: seeded layouts, daytime residents, town hall entry/exit, wall blocking, evening/night/commute views, sleeping population and pause.\nNo combat, conquest, economy or weather integration.");
            Application.logMessageReceived-=observe;Debug.Log(failed?"VILLAGE_SMOKE_FAILED":"VILLAGE_SMOKE_PASSED");Application.Quit(failed?1:0);
        }
    }
}

