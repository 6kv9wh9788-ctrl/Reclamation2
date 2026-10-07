using UnityEngine;
using UnityEngine.Rendering;

namespace Reclamation.Blight
{
    public sealed class CrowdBattlefieldAssets : ScriptableObject
    {
        public Mesh[] equipment;
        public Material metal,ground,rock,wood,leaves;
        public Mesh terrain,rockMesh,trunkMesh,canopyMesh;
        public RenderPipelineAsset pipeline;
        public Mesh Gear(int team,int lod,int motion,int frame)=>equipment[((team*2+lod)*3+motion)*16+frame];
        public static float Height(float x,float z)=>.7f*Mathf.Sin(x*.07f)*Mathf.Cos(z*.065f)+.25f*Mathf.Sin(z*.12f);
    }
}
