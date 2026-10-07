using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Blight
{
    // Isolated articulation study. No combat actor, damage, root motion or rigidbodies.
    public sealed class JointMannequin : MonoBehaviour
    {
        public enum Pose { Relaxed, Walk, Guard, LightAttack, HeavyAttack, Block, Dodge, Run }
        public readonly Dictionary<string, Transform> Joints = new Dictionary<string, Transform>();
        private readonly Dictionary<Transform, Quaternion> rest = new Dictionary<Transform, Quaternion>();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Renderer> markers = new List<Renderer>();
        private readonly List<Transform> fingers = new List<Transform>();
        private Material body, seam, accent;
        private Transform hips, chest, sword;
        private float fingerLengthScale = 1;
        public event System.Action PoseSampled;
        public Transform Weapon => sword;
        public bool Built => hips;
        public void Build()
        {
            if (Built) return;
            body = Material(new Color(.73f, .77f, .76f));
            seam = Material(new Color(.18f, .26f, .28f));
            accent = Material(new Color(.83f, .46f, .20f));
            hips = Joint("Pelvis", transform, new Vector3(0, .98f, 0), .10f);
            Form("Pelvis shell", hips, new[] { new Vector3(-.10f,.10f,.085f), new Vector3(-.06f,.16f,.105f), new Vector3(.06f,.15f,.10f), new Vector3(.13f,.125f,.085f) });
            Transform waist = Joint("Waist", hips, new Vector3(0,.13f,0), .07f);
            chest = Joint("Chest", waist, new Vector3(0,.10f,0), .075f);
            Form("Torso shell", chest, new[] { new Vector3(-.09f,.125f,.09f), new Vector3(.02f,.16f,.105f), new Vector3(.15f,.205f,.115f), new Vector3(.23f,.19f,.095f), new Vector3(.29f,.075f,.065f) });
            Transform neck = Joint("Neck", chest, new Vector3(0,.30f,0), .047f);
            Transform head = Joint("Head", neck, new Vector3(0,.13f,0), 0);
            Form("Head shell", head, new[] { new Vector3(-.11f,.055f,.064f), new Vector3(-.07f,.078f,.081f), new Vector3(.02f,.091f,.095f), new Vector3(.10f,.065f,.072f), new Vector3(.125f,.015f,.02f) });
            Piece("Face direction", head, new Vector3(0,-.005f,.094f), new Vector3(.022f,.045f,.035f), body);
            for (int side = -1; side <= 1; side += 2)
            {
                string suffix = side < 0 ? "L" : "R";
                Transform shoulder = Joint("Shoulder_" + suffix, chest, new Vector3(side*.245f,.245f,0), .062f);
                Form("Upper arm " + suffix, shoulder, new[] { new Vector3(-.275f,.038f,.037f), new Vector3(-.19f,.057f,.058f), new Vector3(-.08f,.065f,.064f), new Vector3(-.015f,.052f,.053f) });
                Transform elbow = Joint("Elbow_" + suffix, shoulder, new Vector3(0,-.30f,0), .039f);
                Form("Forearm " + suffix, elbow, new[] { new Vector3(-.245f,.031f,.029f), new Vector3(-.16f,.042f,.046f), new Vector3(-.055f,.052f,.052f), new Vector3(-.012f,.034f,.035f) });
                Transform wrist = Joint("Wrist_" + suffix, elbow, new Vector3(0,-.27f,0), .028f);
                Form("Palm " + suffix, wrist, new[] { new Vector3(-.09f,.044f,.019f), new Vector3(-.035f,.043f,.024f), new Vector3(-.008f,.029f,.024f) });
                for (int finger = 0; finger < 4; finger++)
                {
                    Transform parent = wrist;
                    int anatomical = side > 0 ? 3-finger : finger;
                    float length = (anatomical == 3 ? .020f : anatomical == 0 ? .026f : .029f) * fingerLengthScale;
                    for (int segment = 0; segment < 3; segment++)
                    {
                        Transform joint = Joint(suffix + "_Finger" + finger + "_" + segment, parent,
                            segment == 0 ? new Vector3((finger-1.5f)*.021f,-.088f,0) : new Vector3(0,-length,0), 0);
                        Piece("Finger segment", joint, new Vector3(0,-length*.48f,0), new Vector3(.017f,length,.020f), body);
                        fingers.Add(joint); parent = joint;
                    }
                }
                Transform thumb = Joint("Thumb_" + suffix, wrist, new Vector3(side*.042f,-.03f,.002f), 0);
                thumb.localRotation = Quaternion.Euler(-20,0,side*40);
                Piece("Thumb", thumb, Vector3.down*.025f, new Vector3(.023f,.06f,.024f), body);
                Transform hip = Joint("Hip_" + suffix, hips, new Vector3(side*.10f,-.05f,0), .06f);
                Form("Thigh " + suffix, hip, new[] { new Vector3(-.375f,.043f,.046f), new Vector3(-.25f,.073f,.078f), new Vector3(-.06f,.082f,.083f), new Vector3(-.015f,.063f,.065f) });
                Transform knee = Joint("Knee_" + suffix, hip, new Vector3(0,-.40f,0), .043f);
                Form("Shin " + suffix, knee, new[] { new Vector3(-.355f,.030f,.033f), new Vector3(-.24f,.048f,.055f), new Vector3(-.10f,.060f,.061f), new Vector3(-.015f,.041f,.042f) });
                Transform ankle = Joint("Ankle_" + suffix, knee, new Vector3(0,-.39f,0), .033f);
                Piece("Foot " + suffix, ankle, new Vector3(0,-.085f,.06f), new Vector3(.105f,.11f,.25f), body);
            }
            sword = new GameObject("Weapon socket / inspection sword").transform;
            sword.SetParent(Joints["Wrist_R"], false);
            sword.localPosition = new Vector3(0,-.085f,.025f);
            sword.localRotation = Quaternion.Euler(0,0,-90);
            Piece("Grip", sword, Vector3.zero, new Vector3(.025f,.14f,.028f), seam);
            Piece("Guard", sword, Vector3.up*.09f, new Vector3(.18f,.025f,.04f), accent);
            Piece("Blade", sword, Vector3.up*.43f, new Vector3(.045f,.66f,.012f), body);
            foreach (Transform joint in Joints.Values) rest[joint] = joint.localRotation;
            Sample(Pose.Relaxed, 0);
        }
        private Material Material(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.color = color; if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .12f);
            materials.Add(material); return material;
        }
        private Transform Joint(string name, Transform parent, Vector3 position, float radius)
        {
            var joint = new GameObject(name).transform; joint.SetParent(parent,false); joint.localPosition=position; Joints.Add(name,joint);
            if (radius > 0)
            {
                var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere); shell.name="Joint cap"; shell.transform.SetParent(joint,false);
                shell.transform.localScale=Vector3.one*radius*2; shell.GetComponent<Collider>().enabled=false;
                shell.GetComponent<Renderer>().sharedMaterial=seam;
                var dot=GameObject.CreatePrimitive(PrimitiveType.Sphere); dot.name="Joint marker"; dot.transform.SetParent(joint,false);
                dot.transform.localScale=Vector3.one*(radius*2+.014f); dot.GetComponent<Collider>().enabled=false;
                var renderer=dot.GetComponent<Renderer>(); renderer.sharedMaterial=accent; renderer.enabled=false; markers.Add(renderer);
            }
            return joint;
        }
        private void Piece(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(parent,false);
            go.transform.localPosition=position; go.transform.localScale=scale; go.GetComponent<Collider>().enabled=false;
            go.GetComponent<Renderer>().sharedMaterial=material;
        }
        // Separate closed lofts make part boundaries inspectable without hiding them under armor.
        private void Form(string name, Transform parent, Vector3[] rings)
        {
            const int sides=16;
            var vertices=new List<Vector3>(); var triangles=new List<int>();
            foreach(Vector3 ring in rings) for(int j=0;j<sides;j++)
            { float angle=j*2*Mathf.PI/sides; vertices.Add(new Vector3(Mathf.Cos(angle)*ring.y,ring.x,Mathf.Sin(angle)*ring.z)); }
            for(int i=0;i<rings.Length-1;i++) for(int j=0;j<sides;j++)
            { int a=i*sides+j,b=i*sides+(j+1)%sides,c=a+sides,d=b+sides; triangles.AddRange(new[]{a,c,b,b,c,d}); }
            int bottom=vertices.Count; vertices.Add(new Vector3(0,rings[0].x,0)); int top=vertices.Count; vertices.Add(new Vector3(0,rings[rings.Length-1].x,0));
            for(int j=0;j<sides;j++) { int k=(j+1)%sides; triangles.AddRange(new[]{bottom,j,k,top,(rings.Length-1)*sides+k,(rings.Length-1)*sides+j}); }
            var mesh=new Mesh{name=name}; mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); meshes.Add(mesh);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterial=body;
        }
        public void ShowJoints(bool show) { foreach(Renderer marker in markers) marker.enabled=show; }
        public void ShowWeapon(bool show) { if(sword) sword.gameObject.SetActive(show); }
        public void Sample(Pose pose, float phase)
        {
            if(!Built) return;
            foreach(var pair in rest) pair.Key.localRotation=pair.Value;
            hips.localPosition=new Vector3(0,.98f,0);
            float cycle=phase*2*Mathf.PI;
            Rotate("Shoulder_R",0,0,10); Rotate("Shoulder_L",0,0,-10);
            Rotate("Elbow_R",-6,0,0); Rotate("Elbow_L",-6,0,0);
            Rotate("Wrist_R",0,-80,0); Rotate("Wrist_L",0,80,0);
            Rotate("Hip_R",0,0,3); Rotate("Hip_L",0,0,-3);
            bool armed=pose!=Pose.Relaxed && pose!=Pose.Walk && pose!=Pose.Run;
            if(pose==Pose.Walk)
            {
                for(int side=-1;side<=1;side+=2)
                {
                    string suffix=side<0?"L":"R"; float wave=Mathf.Sin(cycle)*side;
                    Rotate("Hip_"+suffix,-wave*23,0,side*3);
                    Rotate("Knee_"+suffix,Mathf.Max(0,-wave)*35+3,0,0);
                    Rotate("Shoulder_"+suffix,wave*18,0,side*10); Rotate("Elbow_"+suffix,-12,0,0);
                    Rotate("Ankle_"+suffix,wave*10-4,0,0);
                }
                hips.localPosition+=Vector3.down*.035f+Vector3.up*(.005f*Mathf.Cos(cycle*2));
                for(int side=-1;side<=1;side+=2)
                {
                    string suffix=side<0?"L":"R";
                    float step=Mathf.Repeat(phase+(side<0?.5f:0),1);
                    float swing=Mathf.Clamp01((step-.6f)/.4f);
                    float z=step<.6f?Mathf.Lerp(.17f,-.17f,step/.6f):Mathf.Lerp(-.17f,.17f,Mathf.SmoothStep(0,1,swing));
                    float lift=step<.6f?0:.10f*Mathf.Pow(Mathf.Sin(swing*Mathf.PI),2);
                    ModularHumanRig.SolveGrip(Joints["Hip_"+suffix],Joints["Knee_"+suffix],Joints["Ankle_"+suffix],
                        transform.TransformPoint(new Vector3(side*.14f,.14f+lift,z)),transform.forward);
                    Joints["Ankle_"+suffix].rotation=transform.rotation;
                }
                Rotate("Chest",0,Mathf.Sin(cycle)*4,0);
            }
            else if(pose==Pose.Run)
            {
                // Looping in-place run: short support, lifted recovery, opposing arms.
                hips.localPosition=new Vector3(0,.90f+.035f*Mathf.Pow(Mathf.Sin(cycle),2),0);
                Rotate("Chest",12,Mathf.Sin(cycle)*8,0);
                for(int side=-1;side<=1;side+=2)
                {
                    string suffix=side<0?"L":"R";
                    float step=Mathf.Repeat(phase+(side<0?.5f:0),1);
                    float swing=Mathf.Clamp01((step-.38f)/.62f);
                    float z=step<.38f?Mathf.Lerp(.28f,-.28f,step/.38f):Mathf.Lerp(-.28f,.28f,Mathf.SmoothStep(0,1,swing));
                    float lift=step<.38f?0:.26f*Mathf.Pow(Mathf.Sin(swing*Mathf.PI),2);
                    ModularHumanRig.SolveGrip(Joints["Hip_"+suffix],Joints["Knee_"+suffix],Joints["Ankle_"+suffix],
                        transform.TransformPoint(new Vector3(side*.14f,.14f+lift,z)),transform.forward);
                    Joints["Ankle_"+suffix].rotation=transform.rotation;
                    Rotate("Shoulder_"+suffix,Mathf.Cos(cycle)*side*34,0,side*13);
                    Rotate("Elbow_"+suffix,-85+Mathf.Sin(cycle)*side*12,0,0);
                    Rotate("Wrist_"+suffix,0,side*-80,0);
                }
            }
            else if(armed)
            {
                // Author the arm chain before placing the prop; no hand-to-weapon IK.
                Rotate("Shoulder_R",-20,-10,12); Rotate("Elbow_R",-65,0,0); Rotate("Wrist_R",0,-90,-8);
                Rotate("Shoulder_L",-5,0,-12); Rotate("Elbow_L",-15,0,0);
                if(pose==Pose.LightAttack || pose==Pose.HeavyAttack)
                {
                    bool heavy=pose==Pose.HeavyAttack;
                    float load=Mathf.SmoothStep(0,1,Mathf.Clamp01(phase/.45f));
                    float strike=Mathf.SmoothStep(0,1,Mathf.Clamp01((phase-.45f)/.22f));
                    float recover=Mathf.SmoothStep(0,1,Mathf.Clamp01((phase-.67f)/.33f));
                    float anticipation=load*(1-strike), contact=strike*(1-recover);
                    Rotate("Chest",0,-12*anticipation+15*contact,0);
                    Rotate("Shoulder_R",-20-(heavy?105:50)*anticipation-45*contact,-10+15*anticipation,12+18*anticipation);
                    Rotate("Elbow_R",-65+25*anticipation+45*contact,0,0);
                    Rotate("Shoulder_L",-5-15*contact,0,-12-8*contact);
                    Rotate("Wrist_R",0,-90,-8-55*contact);
                }
                if(pose==Pose.Block) { Rotate("Shoulder_R",-55,-10,18); Rotate("Elbow_R",-70,0,0); }
                if(pose==Pose.Dodge)
                {
                    float duck=Mathf.Sin(Mathf.Clamp01(phase)*Mathf.PI);
                    hips.localPosition+=Vector3.down*(.20f*duck); Rotate("Chest",18*duck,0,-7*duck);
                    for(int side=-1;side<=1;side+=2)
                    {
                        string suffix=side<0?"L":"R";
                        ModularHumanRig.SolveGrip(Joints["Hip_"+suffix],Joints["Knee_"+suffix],Joints["Ankle_"+suffix],
                            transform.TransformPoint(new Vector3(side*.15f,.14f,side*.08f*duck)),transform.forward);
                        Joints["Ankle_"+suffix].rotation=transform.rotation;
                    }
                }
            }
            foreach(Transform finger in fingers)
            {
                bool right=finger.name.StartsWith("R_");
                finger.localRotation=Quaternion.Euler(right && armed ? -65 : -12,0,0);
            }
            PoseSampled?.Invoke();
        }
        private void Rotate(string name,float x,float y,float z) { Joints[name].localRotation=Quaternion.Euler(x,y,z); }
        private static void Release(UnityEngine.Object value) { if(Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
        private void OnDestroy() { foreach(Mesh mesh in meshes) if(mesh) Release(mesh); foreach(Material material in materials) if(material) Release(material); }
    }
}
