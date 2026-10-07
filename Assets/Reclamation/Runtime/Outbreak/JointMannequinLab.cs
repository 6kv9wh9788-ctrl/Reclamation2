using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reclamation.Blight
{
    public sealed class JointMannequinLab : MonoBehaviour
    {
        public Shader bodyShader;
        public JointMannequin Character { get; private set; }
        public Camera View { get; private set; }
        public JointMannequinSkin Skin { get; private set; }
        private bool continuous = true;
        private JointMannequin.Pose pose;
        private float phase, yaw=25, pitch=7, distance=3.7f, speed=.5f;
        private bool playing=true, joints, weapon, turntable, closeup;
        private Material floorMaterial;
        private readonly string[] labels={"Relaxed","Walk","Guard","Light attack","Heavy attack","Block","Dodge","Run"};
        private float Scale => Mathf.Max(.4f,Mathf.Min(Screen.width/1280f,Screen.height/800f));
        private void Awake()
        {
            var root=new GameObject("Mannequin / shared skeleton"); root.transform.SetParent(transform,false);
            Character=root.AddComponent<JointMannequin>(); Character.Build(); Character.ShowWeapon(false);
            Skin=root.AddComponent<JointMannequinSkin>(); Skin.Build(Character, Resources.Load<Mesh>("Player/ContinuousMannequinBody"));
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name="Inspection stage"; floor.transform.SetParent(transform,false);
            floor.transform.localPosition=Vector3.down*.03f; floor.transform.localScale=new Vector3(5,.06f,5);
            floorMaterial=new Material(bodyShader ? bodyShader : Shader.Find("Universal Render Pipeline/Lit")); floorMaterial.color=new Color(.12f,.17f,.20f);
            floor.GetComponent<Renderer>().sharedMaterial=floorMaterial;
            var cameraObject=new GameObject("Mannequin camera",typeof(Camera)); cameraObject.transform.SetParent(transform,false); View=cameraObject.GetComponent<Camera>();
            View.clearFlags=CameraClearFlags.SolidColor; View.backgroundColor=new Color(.055f,.08f,.105f); View.fieldOfView=34; View.nearClipPlane=.05f;
            var lightObject=new GameObject("Key light",typeof(Light)); lightObject.transform.SetParent(transform,false);
            var light=lightObject.GetComponent<Light>(); light.type=LightType.Directional; light.intensity=1.3f; light.transform.rotation=Quaternion.Euler(40,-35,0);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight=new Color(.55f,.6f,.65f);
            PositionCamera();
        }
        public void SetContinuous(bool value) { continuous=value; Skin.SetVisible(value); }
        public void Select(JointMannequin.Pose next) { pose=next; phase=0; weapon=pose!=JointMannequin.Pose.Relaxed && pose!=JointMannequin.Pose.Walk && pose!=JointMannequin.Pose.Run; Character.ShowWeapon(weapon); Character.Sample(pose,phase); }
        private void Update()
        {
            Mouse mouse=Mouse.current;
            if(mouse!=null && mouse.position.ReadValue().x>280*Scale && GUIUtility.hotControl==0)
            {
                if(mouse.rightButton.isPressed) { Vector2 d=mouse.delta.ReadValue(); yaw+=d.x*.25f; pitch=Mathf.Clamp(pitch-d.y*.15f,-15,50); }
                distance=Mathf.Clamp(distance-mouse.scroll.ReadValue().y*.003f,1.6f,5);
            }
            if(turntable) yaw+=Time.unscaledDeltaTime*20;
            if(playing) phase=Mathf.Repeat(phase+Time.unscaledDeltaTime*speed*(pose==JointMannequin.Pose.Run?2.8f:1),1);
            Character.Sample(pose,phase); PositionCamera();
        }
        private void PositionCamera()
        {
            Vector3 focus=transform.position+Vector3.up*(closeup?1.32f:.92f);
            View.transform.position=focus+Quaternion.Euler(pitch,yaw,0)*Vector3.forward*(closeup?distance*.65f:distance);
            View.transform.LookAt(focus); View.rect=new Rect(.24f,0,.76f,1);
        }
        private void OnGUI()
        {
            Matrix4x4 previous=GUI.matrix; GUI.matrix=Matrix4x4.Scale(Vector3.one*Scale);
            GUI.Box(new Rect(12,12,260,745),"BODY DEFORMATION LAB");
            GUI.Label(new Rect(28,49,226,64),"Continuous body study\nSame skeleton and motions\nToggle the original mannequin");
            for(int i=0;i<labels.Length;i++) if(GUI.Button(new Rect(28,122+i*35,226,30),(int)pose==i?"> "+labels[i]:labels[i])) Select((JointMannequin.Pose)i);
            playing=GUI.Toggle(new Rect(28,415,220,25),playing,"Play motion");
            GUI.Label(new Rect(28,447,220,20),"Pose phase: "+phase.ToString("F2"));
            float next=GUI.HorizontalSlider(new Rect(28,479,220,20),phase,0,1); if(!Mathf.Approximately(next,phase)) { phase=next; playing=false; Character.Sample(pose,phase); }
            GUI.Label(new Rect(28,506,220,22),"Playback speed"); speed=GUI.HorizontalSlider(new Rect(28,536,220,20),speed,.1f,1);
            SetContinuous(GUI.Toggle(new Rect(28,562,220,25),continuous,"Continuous body"));
            joints=GUI.Toggle(new Rect(28,590,220,25),joints,"Highlight joints"); Character.ShowJoints(joints);
            weapon=GUI.Toggle(new Rect(28,618,220,25),weapon,"Inspection sword"); Character.ShowWeapon(weapon);
            closeup=GUI.Toggle(new Rect(28,646,220,25),closeup,"Upper-body view");
            turntable=GUI.Toggle(new Rect(28,674,220,25),turntable,"Turntable");
            GUI.Label(new Rect(28,706,226,45),"Right-drag: orbit / Scroll: zoom\nInspect front, side and back.");
            GUI.matrix=previous;
        }
        private IEnumerator Start()
        {
            string[] args=Environment.GetCommandLineArgs(); if(Array.IndexOf(args,"--mannequin-smoke")<0) yield break;
            bool failed=Skin==null || Skin.BodyMesh==null;
            Debug.Log("BODY_SKIN_OK: "+Skin.BodyMesh.vertexCount+" vertices; "+Skin.GenerationSeconds.ToString("F2")+" seconds");
            foreach(JointMannequin.Pose next in Enum.GetValues(typeof(JointMannequin.Pose)))
            {
                Select(next); Character.Sample(next,.5f); SetContinuous(false); SetContinuous(true); yield return null;
                foreach(Transform joint in Character.Joints.Values) if(float.IsNaN(joint.position.x)) failed=true;
                Debug.Log("MANNEQUIN_POSE_OK: "+next);
            }
            Select(JointMannequin.Pose.Relaxed);
            Debug.Log(failed?"MANNEQUIN_SMOKE_FAILED":"MANNEQUIN_SMOKE_PASSED"); Application.Quit(failed?1:0);
        }
        private void OnDestroy() { if(floorMaterial) Destroy(floorMaterial); }
    }
}
