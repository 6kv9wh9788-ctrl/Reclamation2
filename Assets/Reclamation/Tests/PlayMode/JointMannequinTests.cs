using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Reclamation.Tests
{
    public sealed class JointMannequinTests
    {
        private GameObject root;
        private JointMannequinLab lab;
        [UnitySetUp] public IEnumerator Setup()
        {
            root=new GameObject("Disposable mannequin lab"); lab=root.AddComponent<JointMannequinLab>(); lab.enabled=false;
            foreach(Transform item in root.GetComponentsInChildren<Transform>(true)) item.gameObject.layer=30;
            lab.View.cullingMask=1<<30;
            yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup() { Object.Destroy(root); yield return null; }
        [UnityTest] public IEnumerator SharedHierarchyKeepsLimbLengthsAndWeaponAttachment()
        {
            var rig=lab.Character;
            Assert.That(rig.Joints["Wrist_R"].parent,Is.EqualTo(rig.Joints["Elbow_R"]));
            Assert.That(rig.Joints["Elbow_R"].parent,Is.EqualTo(rig.Joints["Shoulder_R"]));
            Assert.That(rig.Weapon.parent,Is.EqualTo(rig.Joints["Wrist_R"]));
            Vector3 weaponLocal=rig.Weapon.localPosition;
            var offsets=new Dictionary<Transform,Vector3>(); foreach(Transform joint in rig.Joints.Values) if(joint.name!="Pelvis") offsets[joint]=joint.localPosition;
            foreach(JointMannequin.Pose pose in Enum.GetValues(typeof(JointMannequin.Pose)))
                for(int frame=0;frame<=120;frame++)
                {
                    rig.Sample(pose,frame/120f);
                    foreach(var pair in offsets) Assert.That(Vector3.Distance(pair.Key.localPosition,pair.Value),Is.LessThan(.00001f),pose+" stretched "+pair.Key.name);
                    Assert.That(rig.Weapon.localPosition,Is.EqualTo(weaponLocal));
                    foreach(Transform joint in rig.Joints.Values) Assert.That(float.IsNaN(joint.position.x),Is.False);
                }
            Assert.That(root.GetComponentsInChildren<Rigidbody>().Length,Is.Zero);
            foreach(Collider collider in root.GetComponentsInChildren<Collider>()) if(collider.gameObject.name!="Inspection stage") Assert.That(collider.enabled,Is.False);
            Assert.That(root.GetComponentsInChildren<BlightCombatLab>().Length,Is.Zero);
            yield return null; LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ArmChainsStayOutsideTorsoDuringMotion()
        {
            var rig=lab.Character;
            foreach(JointMannequin.Pose pose in Enum.GetValues(typeof(JointMannequin.Pose)))
                for(int frame=0;frame<=120;frame++)
                {
                    rig.Sample(pose,frame/120f);
                    Transform torso=rig.Joints["Chest"];
                    foreach(string side in new[]{"L","R"})
                    {
                        Vector3 shoulder=torso.InverseTransformPoint(rig.Joints["Shoulder_"+side].position);
                        Vector3 elbow=torso.InverseTransformPoint(rig.Joints["Elbow_"+side].position);
                        Vector3 wrist=torso.InverseTransformPoint(rig.Joints["Wrist_"+side].position);
                        for(int sample=1;sample<=8;sample++)
                        {
                            Vector3 upper=Vector3.Lerp(shoulder,elbow,sample/8f),lower=Vector3.Lerp(elbow,wrist,sample/8f);
                            foreach(Vector3 point in new[]{upper,lower})
                                if(point.y>-.1f && point.y<.25f)
                                    Assert.That(point.x*point.x/(.205f*.205f)+point.z*point.z/(.16f*.16f),Is.GreaterThan(1),pose+" "+side+" arm inside torso at "+frame);
                        }
                    }
                }
            rig.Sample(JointMannequin.Pose.Relaxed,0);
            foreach(string side in new[]{"L","R"}) Assert.That(rig.Joints["Wrist_"+side].position.y,Is.LessThan(rig.Joints["Elbow_"+side].position.y));
            yield return null; LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RunLoopsWithAlternatingFootSupport()
        {
            var rig=lab.Character;
            lab.Select(JointMannequin.Pose.Run);
            Assert.That(rig.Weapon.gameObject.activeSelf,Is.False);
            var first=new Dictionary<Transform,Vector3>();
            rig.Sample(JointMannequin.Pose.Run,0);
            foreach(Transform joint in rig.Joints.Values) first[joint]=joint.position;
            rig.Sample(JointMannequin.Pose.Run,1);
            foreach(var pair in first) Assert.That(Vector3.Distance(pair.Key.position,pair.Value),Is.LessThan(.0001f),pair.Key.name+" loop seam");
            bool leftSupport=false,rightSupport=false,flight=false;
            for(int frame=0;frame<120;frame++)
            {
                rig.Sample(JointMannequin.Pose.Run,frame/120f);
                float left=rig.Joints["Ankle_L"].position.y, right=rig.Joints["Ankle_R"].position.y;
                Assert.That(left,Is.GreaterThanOrEqualTo(.139f)); Assert.That(right,Is.GreaterThanOrEqualTo(.139f));
                leftSupport |= left<.145f && right>.20f;
                rightSupport |= right<.145f && left>.20f;
                flight |= left>.15f && right>.15f;
            }
            Assert.That(leftSupport && rightSupport && flight,Is.True,"Run must alternate support and include a flight phase.");
            yield return null; LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CapturePoseReview()
        {
            string directory=Environment.GetEnvironmentVariable("RECLAMATION_MANNEQUIN_CAPTURES");
            if(string.IsNullOrEmpty(directory)) { yield return null; yield break; }
            Directory.CreateDirectory(directory);
            var rig=lab.Character;
            foreach(JointMannequin.Pose pose in Enum.GetValues(typeof(JointMannequin.Pose)))
            {
                lab.Select(pose); rig.Sample(pose,pose==JointMannequin.Pose.Walk?.2f:pose==JointMannequin.Pose.Dodge?.5f:.65f);
                yield return null;
                Capture(Path.Combine(directory,pose+"-front.png"),new Vector3(0,1,3.8f));
                Capture(Path.Combine(directory,pose+"-side.png"),new Vector3(3.8f,1,0));
                Capture(Path.Combine(directory,pose+"-angle.png"),new Vector3(2.6f,1.4f,3));
            }
            // Repeated sampling must reproduce an identical pose, without accumulating rotations.
            rig.Sample(JointMannequin.Pose.Guard,.4f); Quaternion before=rig.Joints["Wrist_R"].rotation;
            for(int i=0;i<100;i++) rig.Sample(JointMannequin.Pose.Guard,.4f);
            Assert.That(Quaternion.Angle(before,rig.Joints["Wrist_R"].rotation),Is.LessThan(.001f));
            LogAssert.NoUnexpectedReceived();
        }
        private void Capture(string path,Vector3 position)
        {
            Camera camera=lab.View; camera.rect=new Rect(0,0,1,1); camera.transform.position=position; camera.transform.LookAt(new Vector3(0,.92f,0));
            var target=new RenderTexture(900,900,24); var image=new Texture2D(900,900,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try { camera.targetTexture=target; camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,900,900),0,0); image.Apply(); File.WriteAllBytes(path,image.EncodeToPNG()); }
            finally { camera.targetTexture=null; RenderTexture.active=previous; target.Release(); Object.Destroy(target); Object.Destroy(image); }
        }
    }
}
