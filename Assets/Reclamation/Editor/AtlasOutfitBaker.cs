using System;
using System.Collections.Generic;
using System.IO;
using Reclamation.Atlas;
using Reclamation.Blight;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Reclamation.Editor
{
    public static class AtlasOutfitBaker
    {
        private static string Path="Assets/Reclamation/Resources/AtlasOutfits.asset";
        public static void Bake() => BakeLibrary(false);
        public static void BakeCombat() => BakeLibrary(false,true);
        public static void BakePresentation() => BakeLibrary(true);
        private static void BakeLibrary(bool presentation,bool combat=false)
        {
            Path = "Assets/Reclamation/Resources/" + (combat ? "AtlasCombatOutfits" : presentation ? "AtlasGuardOutfits" : "AtlasOutfits") + ".asset";
            int count = combat ? 97 : presentation ? 165 : 33;
            if(File.Exists(Path))throw new InvalidOperationException("Preserve the existing outfit library before rebuilding.");
            var source=Resources.Load<AtlasCharacterLibrary>(combat ? "AtlasCombatPoses" : presentation ? "AtlasGuardPoses" : "AtlasCharacters");
            var a=ScriptableObject.CreateInstance<AtlasOutfitLibrary>();AssetDatabase.CreateAsset(a,Path);a.gear=new Mesh[count];a.details=new Mesh[count];
            var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("Atlas outfit bake");SceneManager.MoveGameObjectToScene(root,scene);
            try
            {
                var rig=root.AddComponent<JointMannequin>();rig.Build();
                for(int pose=0;pose<count;pose++)
                {
                    if(combat)AtlasCharacterBaker.SampleCombatPose(rig,pose);else AtlasCharacterBaker.SamplePose(rig, pose % 33, presentation ? (pose / 33) / 4f : 1);
                    // Preserve the existing silhouette, separate its material categories.
                    var gear=UnityEngine.Object.Instantiate(source.gear[pose]);gear.name="Atlas layered equipment "+pose;
                    var vertices=gear.vertices;var elbow=rig.Joints["Elbow_L"];var offset=new Vector3(0,-.15f,.075f);
                    for(int vertex=52;vertex<78;vertex++)vertices[vertex]=elbow.TransformPoint(Quaternion.Euler(65,0,0)*(elbow.InverseTransformPoint(vertices[vertex])-offset)+offset);
                    gear.vertices=vertices;gear.RecalculateNormals();gear.RecalculateBounds();
                    var groups=new List<int>[] {new List<int>(),new List<int>(),new List<int>(),new List<int>()};var tris=gear.triangles;
                    for(int t=0;t<tris.Length;t+=3){int group=tris[t]<26?0:tris[t]<52?1:tris[t]<78?2:3;groups[group].AddRange(new[]{tris[t],tris[t+1],tris[t+2]});}
                    gear.subMeshCount=4;for(int g=0;g<4;g++)gear.SetTriangles(groups[g],g);a.gear[pose]=gear;AssetDatabase.AddObjectToAsset(gear,a);
                    var v=new List<Vector3>();var triangles=new List<int>[] {new List<int>(),new List<int>(),new List<int>()};
                    void Face(string bone,int mat,params Vector3[] points){int start=v.Count;foreach(var p in points)v.Add(rig.Joints[bone].TransformPoint(p));for(int j=1;j<points.Length-1;j++)triangles[mat].AddRange(new[]{start,start+j,start+j+1});}
                    void Box(string bone,Vector3 c,Vector3 size,int mat)
                    {
                        var q=new Vector3[8];for(int i=0;i<8;i++)q[i]=c+Vector3.Scale(new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1),size*.5f);
                        Face(bone,mat,q[0],q[4],q[6],q[2]);Face(bone,mat,q[1],q[3],q[7],q[5]);Face(bone,mat,q[0],q[1],q[5],q[4]);Face(bone,mat,q[2],q[6],q[7],q[3]);Face(bone,mat,q[0],q[2],q[3],q[1]);Face(bone,mat,q[4],q[5],q[7],q[6]);
                    }
                    // Padded split tunic skirts, thick leather belt, buckle and supply pouch.
                    foreach(float side in new[]{-1f,1f})
                    {
                        Box("Pelvis",new Vector3(side*.105f,-.11f,.07f),new Vector3(.19f,.22f,.15f),0);
                        Box("Pelvis",new Vector3(side*.105f,-.11f,-.07f),new Vector3(.19f,.22f,.15f),0);
                        for(int seam=0;seam<3;seam++)Box("Pelvis",new Vector3(side*(.035f+seam*.06f),-.12f,.148f),new Vector3(.008f,.20f,.008f),1);
                        string suffix=side<0?"L":"R";
                        Box("Shoulder_"+suffix,new Vector3(side*.018f,-.025f,0),new Vector3(.14f,.095f,.16f),1);
                        Box("Knee_"+suffix,new Vector3(0,-.29f,0),new Vector3(.08f,.055f,.09f),1);
                    }
                    Box("Pelvis",new Vector3(0,.015f,0),new Vector3(.355f,.06f,.245f),1);
                    Box("Pelvis",new Vector3(0,.015f,.132f),new Vector3(.07f,.055f,.017f),2);
                    Box("Pelvis",new Vector3(.205f,-.045f,-.025f),new Vector3(.10f,.145f,.105f),1);
                    Box("Pelvis",new Vector3(.205f,.012f,-.025f),new Vector3(.11f,.035f,.115f),0);
                    // Straps and visible fasteners over the chest plate.
                    foreach(float side in new[]{-1f,1f}){Box("Chest",new Vector3(side*.10f,.075f,.135f),new Vector3(.028f,.25f,.018f),1);Box("Chest",new Vector3(side*.10f,.06f,.149f),new Vector3(.035f,.035f,.012f),2);}
                    var mesh=new Mesh{name="Padded tunic belt and straps "+pose};mesh.SetVertices(v);mesh.subMeshCount=3;for(int g=0;g<3;g++)mesh.SetTriangles(triangles[g],g);mesh.RecalculateNormals();mesh.RecalculateBounds();a.details[pose]=mesh;AssetDatabase.AddObjectToAsset(mesh,a);
                }
                EditorUtility.SetDirty(a);AssetDatabase.SaveAssets();Debug.Log("ATLAS_OUTFITS_BAKED: "+a.details[0].vertexCount+" detail vertices, 33 shared poses.");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
