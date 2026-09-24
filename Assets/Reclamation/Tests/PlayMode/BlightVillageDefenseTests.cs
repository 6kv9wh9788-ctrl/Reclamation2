using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;
namespace Reclamation.Tests
{
    public sealed class BlightVillageDefenseTests
    {
        private GameObject root; private BlightCombatLab lab;
        [UnitySetUp] public IEnumerator Setup()
        {
            root=new GameObject("Village tests"); lab=root.AddComponent<BlightCombatLab>();
            lab.InputEnabled=lab.AutomaticSimulation=lab.CombatAudioEnabled=false;
            yield return null; lab.SetVillageDefense(true); yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup() { Object.Destroy(root); yield return null; }
        private void DefeatWaves()
        {
            foreach (Transform t in root.transform) if (t.gameObject.activeSelf && t.name.StartsWith("Wave "))
            { var f=lab.GetFighter(t); if (f!=null && f.Alive) f.Receive(10000,false,false); }
        }
        [UnityTest] public IEnumerator RosterGatesReserveAndQueuedDefenseAreConsistent()
        {
            Assert.That(lab.LivingCompanions,Is.EqualTo(24)); Assert.That(lab.LivingEnemies,Is.Zero);
            for (int i=0;i<3;i++) Assert.That(lab.CompanyCapacity(i),Is.EqualTo(8));
            var bodies=new System.Collections.Generic.List<Transform>();
            foreach (Transform t in root.transform) if (t.gameObject.activeSelf && lab.GetFighter(t)!=null) bodies.Add(t);
            for (int a=0;a<bodies.Count;a++) for (int b=a+1;b<bodies.Count;b++)
                Assert.That(Vector3.Distance(bodies[a].position,bodies[b].position),Is.GreaterThan(.86f),"Initial formations must not overlap.");
            Assert.That(lab.CompanyReserveHeld,Is.True);
            Assert.That(lab.TerrainPassable(new Vector3(0,0,-10),new Vector3(0,0,0),.43f),Is.True);
            Assert.That(lab.TerrainPassable(new Vector3(10,0,-10),new Vector3(10,0,0),.43f),Is.False);
            Assert.That(lab.TerrainPassable(new Vector3(-30,0,8),new Vector3(-20,0,8),.43f),Is.True);
            Assert.That(lab.ReleaseCompanyReserve(),Is.True); Assert.That(lab.CompanyReserveHeld,Is.False);
            Assert.That(lab.GetCompanyOrder(2),Is.EqualTo(CompanyOrder.Defend));
            lab.OpenCompanyMap(); lab.QueueCompanyAssault();
            Assert.That(lab.StartVillageDefense(),Is.False,"Planning pauses the simulation.");
            lab.CloseCompanyMap(true); Assert.That(lab.CompanyReserveHeld,Is.True);
            Assert.That(lab.CompanyPlanActive,Is.False,"Defense deployment is not the old fortress assault.");
            lab.RecallCompany(); for (int i=0;i<3;i++) Assert.That(lab.GetCompanyOrder(i),Is.EqualTo(CompanyOrder.Withdraw));
            yield return null;
        }
        [UnityTest] public IEnumerator WavesRespectPreparationPauseAndVictory()
        {
            for (int i=0;i<8;i++) lab.Simulate(.25f);
            Assert.That(lab.VillageWave,Is.Zero); Assert.That(lab.VillageStarted,Is.False);
            Assert.That(lab.StartVillageDefense(),Is.True); Assert.That(lab.StartVillageDefense(),Is.False);
            lab.SetPaused(true);
            for (int i=0;i<60;i++) lab.Simulate(.25f);
            Assert.That(lab.VillageWave,Is.Zero); lab.SetPaused(false);
            for (int step=0;step<650 && !lab.VillageWon;step++)
            {
                lab.Simulate(.25f); DefeatWaves();
                if (step%4==0) yield return null;
            }
            Assert.That(lab.VillageWave,Is.EqualTo(3)); Assert.That(lab.VillageWon,Is.True);
            Assert.That(lab.VillageIntegrity,Is.EqualTo(100)); Assert.That(lab.VillageLost,Is.False);
            lab.ResetFight(); yield return null;
            Assert.That(lab.VillageWave,Is.Zero); Assert.That(lab.VillageStarted,Is.False); Assert.That(lab.LivingCompanions,Is.EqualTo(24));
        }
        [UnityTest] public IEnumerator UnopposedOccupationLosesVillageAndLeavingRestoresCompany()
        {
            lab.StartVillageDefense();
            for (int step=0;step<49;step++) { lab.Simulate(.25f); if (step%4==0) yield return null; }
            bool keeper=false;
            foreach (Transform t in root.transform)
            {
                if (!t.gameObject.activeSelf) continue;
                var f=lab.GetFighter(t); if (f==null) continue;
                if (t.name=="Company fighter") { t.position=new Vector3(0,0,-48); continue; }
                if (t.name.StartsWith("Wave ") && !keeper)
                { keeper=true; t.position=lab.CampPosition; f.Interrupt(60); }
                else f.Receive(10000,false,false);
            }
            Assert.That(keeper,Is.True);
            for (int step=0;step<120 && !lab.VillageLost;step++)
            { lab.Simulate(.25f); if (step%4==0) yield return null; }
            Assert.That(lab.VillageLost,Is.True); Assert.That(lab.VillageWon,Is.False);
            lab.SetVillageDefense(false); yield return null;
            Assert.That(lab.VillageDefense,Is.False); Assert.That(lab.LivingCompanions,Is.EqualTo(6));
            Assert.That(lab.CompanyCapacity(0),Is.EqualTo(2));
            Assert.That(lab.VillageWave,Is.Zero);
        }
    }
}
