using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Reclamation.Blight
{
    // A welded surface over the approved mannequin, skinned to its existing joints.
    // This component never authors a pose or changes a joint transform.
    public sealed class JointMannequinSkin : MonoBehaviour
    {
        public SkinnedMeshRenderer Surface { get; private set; }
        public Mesh BodyMesh => Surface ? Surface.sharedMesh : null;
        public float GenerationSeconds { get; private set; }
        private Material material;
        private bool ownsMesh;
        private Renderer[] original;
        private readonly List<Field> fields = new List<Field>();
        private Transform[] bones;
        private readonly Transform[] shoulderHelpers = new Transform[2];
        private JointMannequin rig;
        private const float Blend = .018f;
        private const float Step = .012f;
        private struct Field
        {
            public Vector3 a, b, radii;
            public Quaternion inverse;
            public float radiusA, radiusB;
            public int bone;
            public bool ellipsoid;
            public float Distance(Vector3 point)
            {
                if (!ellipsoid)
                {
                    Vector3 line = b - a;
                    float t = Mathf.Clamp01(Vector3.Dot(point - a, line) / Mathf.Max(.000001f, line.sqrMagnitude));
                    return (point - a - line * t).magnitude - Mathf.Lerp(radiusA, radiusB, t);
                }
                Vector3 p = inverse * (point - a);
                Vector3 q = new Vector3(p.x / radii.x, p.y / radii.y, p.z / radii.z);
                float k0 = q.magnitude;
                if (k0 < .00001f) return -Mathf.Min(radii.x, Mathf.Min(radii.y, radii.z));
                float k1 = new Vector3(q.x / radii.x, q.y / radii.y, q.z / radii.z).magnitude;
                return k0 * (k0 - 1) / k1;
            }
        }
        public void Build(JointMannequin mannequin, Mesh prepared = null)
        {
            if (Surface) return;
            if (!mannequin || !mannequin.Built) throw new InvalidOperationException("Build the mannequin before its skin.");
            var timer = System.Diagnostics.Stopwatch.StartNew();
            rig = mannequin;
            original = rig.GetComponentsInChildren<Renderer>(true);
            var boneList = new List<Transform>(rig.Joints.Values);
            for(int i=0;i<2;i++)
            {
                string side=i==0?"L":"R";
                Transform shoulder=rig.Joints["Shoulder_"+side];
                var helper=new GameObject("Skin shoulder blend "+side).transform;
                helper.SetParent(shoulder.parent,false); helper.localPosition=shoulder.localPosition;
                shoulderHelpers[i]=helper; boneList.Add(helper);
            }
            bones = boneList.ToArray();
            rig.PoseSampled += UpdateShoulderHelpers;
            UpdateShoulderHelpers();
            AddEllipsoid("Pelvis", Vector3.zero, new Vector3(.166f, .125f, .108f));
            AddEllipsoid("Waist", Vector3.up * .02f, new Vector3(.133f, .175f, .093f));
            AddEllipsoid("Chest", Vector3.up * .095f, new Vector3(.205f, .21f, .112f));
            AddCapsule("Neck", Position("Neck") - Vector3.up * .06f, Position("Head"), .052f, .045f);
            AddEllipsoid("Head", Vector3.zero, new Vector3(.086f, .125f, .095f));
            AddEllipsoid("Head", new Vector3(0, -.015f, .094f), new Vector3(.014f, .028f, .023f));
            foreach (string side in new[] { "L", "R" })
            {
                Vector3 shoulder = Position("Shoulder_" + side), elbow = Position("Elbow_" + side), wrist = Position("Wrist_" + side);
                AddCapsule("Chest", Position("Chest") + Vector3.up * .225f, shoulder, .069f, .065f);
                AddCapsule("Shoulder_" + side, shoulder, Vector3.Lerp(shoulder, elbow, .38f), .068f, .060f);
                AddCapsule("Shoulder_" + side, Vector3.Lerp(shoulder, elbow, .38f), elbow, .060f, .041f);
                AddCapsule("Elbow_" + side, elbow, Vector3.Lerp(elbow, wrist, .38f), .041f, .049f);
                AddCapsule("Elbow_" + side, Vector3.Lerp(elbow, wrist, .38f), wrist, .049f, .028f);
                AddEllipsoid("Wrist_" + side, new Vector3(0, -.044f, 0), new Vector3(.046f, .060f, .025f));
                for (int finger = 0; finger < 4; finger++)
                    for (int segment = 0; segment < 3; segment++)
                    {
                        string name = side + "_Finger" + finger + "_" + segment;
                        Transform joint = rig.Joints[name];
                        Vector3 start = Position(name);
                        Vector3 end = segment < 2 ? Position(side + "_Finger" + finger + "_" + (segment + 1)) :
                            rig.transform.InverseTransformPoint(joint.TransformPoint(Vector3.down * (finger == (side == "R" ? 0 : 3) ? .018f : .023f)));
                        AddCapsule(name, start, end, .010f, .0085f);
                    }
                Transform thumb = rig.Joints["Thumb_" + side];
                AddCapsule("Thumb_" + side, Position("Thumb_" + side), rig.transform.InverseTransformPoint(thumb.TransformPoint(Vector3.down * .052f)), .014f, .011f);
                Vector3 hip = Position("Hip_" + side), knee = Position("Knee_" + side), ankle = Position("Ankle_" + side);
                AddCapsule("Hip_" + side, hip, Vector3.Lerp(hip, knee, .35f), .088f, .092f);
                AddCapsule("Hip_" + side, Vector3.Lerp(hip, knee, .35f), knee, .092f, .050f);
                AddCapsule("Knee_" + side, knee, Vector3.Lerp(knee, ankle, .36f), .050f, .072f);
                AddCapsule("Knee_" + side, Vector3.Lerp(knee, ankle, .36f), ankle, .072f, .030f);
                AddEllipsoid("Ankle_" + side, new Vector3(0, -.075f, -.009f), new Vector3(.041f, .064f, .055f));
                AddEllipsoid("Ankle_" + side, new Vector3(0, -.085f, .066f), new Vector3(.052f, .052f, .080f));
                AddEllipsoid("Ankle_" + side, new Vector3(0, -.10f, .119f), new Vector3(.046f, .032f, .047f));
            }
            if (prepared && prepared.bindposes.Length != bones.Length) throw new InvalidOperationException("Baked skin does not match this skeleton; rebuild it.");
            ownsMesh = !prepared;
            Mesh mesh = prepared ? prepared : Generate();
            var child = new GameObject("Continuous skinned body"); child.transform.SetParent(rig.transform, false);
            Surface = child.AddComponent<SkinnedMeshRenderer>();
            Surface.sharedMesh = mesh; Surface.bones = bones; Surface.rootBone = rig.Joints["Pelvis"];
            Surface.quality = SkinQuality.Bone4; Surface.updateWhenOffscreen = true;
            Surface.localBounds = new Bounds(Vector3.up, new Vector3(3, 4, 3));
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = new Color(.69f, .75f, .74f); material.SetFloat("_Smoothness", .16f);
            Surface.sharedMaterial = material;
            GenerationSeconds = (float)timer.Elapsed.TotalSeconds;
            SetVisible(true);
        }
        public void SetVisible(bool visible)
        {
            if (!Surface) return;
            Surface.enabled = visible;
            foreach (Renderer renderer in original)
                if (renderer && renderer.name != "Joint marker" && !renderer.transform.IsChildOf(rig.Weapon)) renderer.enabled = !visible;
        }
        private Vector3 Position(string name) => rig.transform.InverseTransformPoint(rig.Joints[name].position);
        private int Bone(string name) => Array.IndexOf(bones, rig.Joints[name]);
        private void AddCapsule(string bone, Vector3 a, Vector3 b, float ra, float rb)
            => fields.Add(new Field { a = a, b = b, radiusA = ra, radiusB = rb, bone = Bone(bone) });
        private void AddEllipsoid(string bone, Vector3 offset, Vector3 radii)
        {
            Transform joint = rig.Joints[bone];
            fields.Add(new Field { a = rig.transform.InverseTransformPoint(joint.TransformPoint(offset)), radii = radii,
                inverse = Quaternion.Inverse(Quaternion.Inverse(rig.transform.rotation) * joint.rotation), bone = Bone(bone), ellipsoid = true });
        }
        private float Distance(Vector3 point)
        {
            float nearest = float.PositiveInfinity;
            foreach (Field field in fields)
            {
                float next = field.Distance(point);
                if (float.IsPositiveInfinity(nearest)) { nearest = next; continue; }
                float h = Mathf.Max(Blend - Mathf.Abs(nearest - next), 0) / Blend;
                nearest = Mathf.Min(nearest, next) - h * h * Blend * .25f;
            }
            return nearest;
        }
        private Vector3 Normal(Vector3 p)
        {
            const float h = .002f;
            return new Vector3(Distance(p + Vector3.right*h)-Distance(p-Vector3.right*h),
                Distance(p+Vector3.up*h)-Distance(p-Vector3.up*h),Distance(p+Vector3.forward*h)-Distance(p-Vector3.forward*h)).normalized;
        }
        private BoneWeight Weights(Vector3 point)
        {
            var influence = new float[bones.Length];
            float nearest = float.PositiveInfinity;
            foreach (Field field in fields) nearest = Mathf.Min(nearest, field.Distance(point));
            foreach (Field field in fields)
            {
                float delta = field.Distance(point) - nearest;
                if (delta < .050f) influence[field.bone] += Mathf.Exp(-delta / .012f);
            }
            // Use a broad anatomical transition at the shoulder instead of tiny
            // nearest-surface islands that stretch into spikes as the arm rises.
            foreach (string side in new[] { "L", "R" })
            {
                Vector3 shoulder = Position("Shoulder_" + side);
                float outward = point.x * (side == "R" ? 1 : -1);
                if (outward < .13f || point.y < shoulder.y - .19f || point.y > shoulder.y + .09f) continue;
                float lower = Mathf.Clamp01((shoulder.y - point.y) / .19f);
                float arm = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(Mathf.Lerp(.14f,.20f,lower), Mathf.Lerp(.30f,.275f,lower), outward));
                float zone = Mathf.SmoothStep(0,1,Mathf.InverseLerp(shoulder.y-.19f,shoulder.y-.12f,point.y));
                float sum = 0; foreach(float value in influence) sum += value;
                for(int i=0;i<influence.Length;i++) influence[i] *= 1-zone;
                float chestWeight=Mathf.Max(0,1-2*arm), armWeight=Mathf.Max(0,2*arm-1);
                influence[Bone("Chest")] += sum*zone*chestWeight;
                influence[Bone("Shoulder_"+side)] += sum*zone*armWeight;
                influence[Array.IndexOf(bones,shoulderHelpers[side=="L"?0:1])] += sum*zone*(1-chestWeight-armWeight);
            }
            int[] indices = new int[4]; float[] values = new float[4]; float total = 0;
            for (int slot = 0; slot < 4; slot++)
            {
                int best = 0; for (int i = 1; i < influence.Length; i++) if (influence[i] > influence[best]) best = i;
                indices[slot] = best; values[slot] = influence[best]; total += values[slot]; influence[best] = 0;
            }
            return new BoneWeight { boneIndex0=indices[0],boneIndex1=indices[1],boneIndex2=indices[2],boneIndex3=indices[3],
                weight0=values[0]/total,weight1=values[1]/total,weight2=values[2]/total,weight3=values[3]/total };
        }
        private Mesh Generate()
        {
            Vector3 minimum = new Vector3(-.51f,-.04f,-.23f), maximum = new Vector3(.51f,1.84f,.28f);
            int nx=Mathf.CeilToInt((maximum.x-minimum.x)/Step)+1, ny=Mathf.CeilToInt((maximum.y-minimum.y)/Step)+1, nz=Mathf.CeilToInt((maximum.z-minimum.z)/Step)+1;
            int Index(int x,int y,int z) => (z*ny+y)*nx+x;
            Vector3 Point(int id) { int x=id%nx,y=(id/nx)%ny,z=id/(nx*ny); return minimum+new Vector3(x,y,z)*Step; }
            var samples=new float[nx*ny*nz];
            for(int z=0;z<nz;z++) for(int y=0;y<ny;y++) for(int x=0;x<nx;x++) samples[Index(x,y,z)]=Distance(minimum+new Vector3(x,y,z)*Step);
            var vertices=new List<Vector3>(); var normals=new List<Vector3>(); var weights=new List<BoneWeight>(); var triangles=new List<int>();
            var edges=new Dictionary<ulong,int>();
            int Vertex(int a,int b)
            {
                ulong key=((ulong)(uint)Mathf.Min(a,b)<<32)|(uint)Mathf.Max(a,b);
                if(edges.TryGetValue(key,out int existing)) return existing;
                float t=samples[a]/(samples[a]-samples[b]); Vector3 point=Vector3.Lerp(Point(a),Point(b),t);
                int index=vertices.Count; vertices.Add(point); normals.Add(Normal(point)); weights.Add(Weights(point)); edges.Add(key,index); return index;
            }
            void Triangle(int a,int b,int c)
            {
                if(Vector3.Dot(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]),normals[a]+normals[b]+normals[c])<0) { int swap=b;b=c;c=swap; }
                triangles.Add(a);triangles.Add(b);triangles.Add(c);
            }
            int[,] tetrahedra={{0,1,3,7},{0,3,2,7},{0,2,6,7},{0,6,4,7},{0,4,5,7},{0,5,1,7}};
            var cube=new int[8]; var inside=new int[4]; var outside=new int[4];
            for(int z=0;z<nz-1;z++) for(int y=0;y<ny-1;y++) for(int x=0;x<nx-1;x++)
            {
                int mask=0;
                for(int corner=0;corner<8;corner++) { cube[corner]=Index(x+(corner&1),y+((corner>>1)&1),z+((corner>>2)&1)); if(samples[cube[corner]]<0) mask|=1<<corner; }
                if(mask==0 || mask==255) continue;
                for(int tet=0;tet<6;tet++)
                {
                    int ni=0,no=0;
                    for(int corner=0;corner<4;corner++) { int id=cube[tetrahedra[tet,corner]]; if(samples[id]<0) inside[ni++]=id; else outside[no++]=id; }
                    if(ni==0 || ni==4) continue;
                    if(ni==1) Triangle(Vertex(inside[0],outside[0]),Vertex(inside[0],outside[1]),Vertex(inside[0],outside[2]));
                    else if(ni==3) Triangle(Vertex(outside[0],inside[0]),Vertex(outside[0],inside[1]),Vertex(outside[0],inside[2]));
                    else { int a=Vertex(inside[0],outside[0]),b=Vertex(inside[0],outside[1]),c=Vertex(inside[1],outside[0]),d=Vertex(inside[1],outside[1]); Triangle(a,b,c);Triangle(b,d,c); }
                }
            }
            var bind=new Matrix4x4[bones.Length]; for(int i=0;i<bind.Length;i++) bind[i]=bones[i].worldToLocalMatrix*rig.transform.localToWorldMatrix;
            var mesh=new Mesh { name="Welded mannequin skin", indexFormat=IndexFormat.UInt32 };
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(triangles,0);mesh.boneWeights=weights.ToArray();mesh.bindposes=bind;mesh.RecalculateBounds();return mesh;
        }
        private void UpdateShoulderHelpers()
        {
            for(int i=0;i<2;i++) shoulderHelpers[i].localRotation=Quaternion.Slerp(Quaternion.identity,rig.Joints["Shoulder_"+(i==0?"L":"R")].localRotation,.5f);
        }
        private static void Release(UnityEngine.Object value) { if(Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
        private void OnDestroy()
        {
            if (rig) rig.PoseSampled -= UpdateShoulderHelpers;
            if (Surface && ownsMesh) Release(Surface.sharedMesh);
            if (material) Release(material);
        }
    }
}
