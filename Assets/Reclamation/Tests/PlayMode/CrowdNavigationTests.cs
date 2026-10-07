using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;

namespace Reclamation.Tests
{
    public sealed class CrowdNavigationTests
    {
        [Test] public void All256FriendliesCrossBridgeAndReform()
        {
            var sim=new CrowdBattleSimulation(1280);sim.EnableSquads();sim.EnableNavigation(CrowdNavigation.Layout.Bridge);
            for(int i=0;i<1280;i++)if(sim.Units[i].team==1)sim.Units[i].position=sim.Units[i].home=new Vector3(500+i%40*3,0,500+i/40*3);
            for(int s=0;s<8;s++)sim.Squads.Issue(s,CrowdSquadCommands.Command.Move,new Vector3((s%4-1.5f)*11,0,18+s/4*9));
            for(int frame=0;frame<10800;frame++)
            {
                sim.Step(1f/60f);
                for(int i=0;i<1280;i+=5)if(!sim.Navigation.Clear(sim.Units[i].position))Assert.Fail("Bridge boundary crossed by "+i);
            }
            for(int i=0;i<1280;i+=5)Assert.That(Vector3.Distance(sim.Units[i].position,sim.Squads.Slot(i)),Is.LessThan(.4f),"Stuck unit "+i+" at "+sim.Units[i].position);
        }
        [TestCase(CrowdNavigation.Layout.Wall)]
        [TestCase(CrowdNavigation.Layout.Gap)]
        [TestCase(CrowdNavigation.Layout.Bridge)]
        public void WholeSquadTraversesAndReforms(CrowdNavigation.Layout layout)
        {
            var sim=new CrowdBattleSimulation(160);sim.EnableSquads();sim.EnableNavigation(layout);
            foreach(var u in sim.Units)if(u.team==1)u.position=u.home=new Vector3(40,0,40);
            var goal=new Vector3(-16,0,14);sim.Squads.Issue(0,CrowdSquadCommands.Command.Move,goal);
            for(int frame=0;frame<9000;frame++)
            {
                sim.Step(1f/60f);
                for(int i=0;i<160;i+=5)Assert.That(sim.Navigation.Clear(sim.Units[i].position),Is.True,"Obstacle penetration "+frame+" / "+i);
            }
            for(int i=0;i<160;i+=5)
                Assert.That(Vector3.Distance(sim.Units[i].position,goal+CrowdSquadCommands.Offset(i/5)),Is.LessThan(.4f),"Unit "+i+" at "+sim.Units[i].position);
            for(int i=0;i<160;i+=5)for(int j=i+5;j<160;j+=5)
                Assert.That(Vector3.Distance(sim.Units[i].position,sim.Units[j].position),Is.GreaterThan(.65f));
        }
        [Test] public void HoldAndRedirectWorkDuringRouting()
        {
            var sim=new CrowdBattleSimulation(160);sim.EnableSquads();sim.EnableNavigation(CrowdNavigation.Layout.Wall);
            sim.Squads.Issue(0,CrowdSquadCommands.Command.Move,new Vector3(0,0,15));
            for(int i=0;i<300;i++)sim.Step(1f/60f);
            sim.Squads.Issue(0,CrowdSquadCommands.Command.Hold,Vector3.zero);var p=sim.Units[0].position;
            for(int i=0;i<120;i++)sim.Step(1f/60f);Assert.That(sim.Units[0].position,Is.EqualTo(p));
            sim.Squads.Issue(0,CrowdSquadCommands.Command.Move,new Vector3(-20,0,-25));
            for(int i=0;i<1500;i++)sim.Step(1f/60f);
            Assert.That(Vector3.Distance(sim.Units[0].position,new Vector3(-20,0,-25)+CrowdSquadCommands.Offset(0)),Is.LessThan(.4f));
        }
        [Test] public void BlockedClickSnapsToWalkableDestinationAndWallBlocksSight()
        {
            var nav=new CrowdNavigation(CrowdNavigation.Layout.Wall);var field=nav.Build(Vector3.zero);
            Assert.That(nav.Clear(field.goal),Is.True);
            Assert.That(nav.Visible(new Vector3(0,0,-5),new Vector3(0,0,5)),Is.False);
            Assert.That(nav.Visible(new Vector3(20,0,-5),new Vector3(20,0,5)),Is.True);
        }
    }
}
