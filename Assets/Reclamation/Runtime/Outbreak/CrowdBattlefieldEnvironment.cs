using UnityEngine;
using UnityEngine.Rendering;

namespace Reclamation.Blight
{
    public sealed class CrowdBattlefieldEnvironment : MonoBehaviour
    {
        public bool TerrainEnabled {get;private set;}
        public bool ShadowsEnabled {get;private set;}
        public CrowdBattlefieldAssets Assets {get;private set;}
        public int PropInstances=>TerrainEnabled?128:0;
        private GameObject ground;
        private readonly Matrix4x4[] rocks=new Matrix4x4[64],trunks=new Matrix4x4[32],canopies=new Matrix4x4[32];
        private Light sun;
        private RenderPipelineAsset originalPipeline;
        public void Build(Light key)
        {
            sun=key;Assets=Resources.Load<CrowdBattlefieldAssets>("CrowdPerformance/BattlefieldAssets");
            if(!Assets || !Assets.pipeline)throw new System.InvalidOperationException("Bake battlefield assets before opening the lab.");
            originalPipeline=QualitySettings.renderPipeline;QualitySettings.renderPipeline=Assets.pipeline;
            ground=new GameObject("Rolling battlefield ground",typeof(MeshFilter),typeof(MeshRenderer));ground.transform.SetParent(transform,false);
            ground.GetComponent<MeshFilter>().sharedMesh=Assets.terrain;ground.GetComponent<MeshRenderer>().sharedMaterial=Assets.ground;
            ground.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            for(int i=0;i<64;i++)
            {
                float angle=i*Mathf.PI*2/64;Vector3 p=new Vector3(Mathf.Cos(angle)*46,0,Mathf.Sin(angle)*46);p.y=CrowdBattlefieldAssets.Height(p.x,p.z);
                rocks[i]=Matrix4x4.TRS(p,Quaternion.Euler(0,i*137.5f,0),new Vector3(1.0f+(i%3)*.4f,.8f+(i%4)*.3f,1.2f));
            }
            for(int i=0;i<32;i++)
            {
                float angle=(i+.4f)*Mathf.PI*2/32;Vector3 p=new Vector3(Mathf.Cos(angle)*49,0,Mathf.Sin(angle)*49);p.y=CrowdBattlefieldAssets.Height(p.x,p.z);
                var rotation=Quaternion.Euler(0,i*71,0);float scale=1+(i%4)*.12f;
                trunks[i]=Matrix4x4.TRS(p,rotation,Vector3.one*scale);canopies[i]=Matrix4x4.TRS(p,rotation,Vector3.one*scale);
            }
            Set(false,false);
        }
        public void Set(bool terrain,bool shadows)
        {
            TerrainEnabled=terrain;ShadowsEnabled=shadows;ground.SetActive(terrain);
            sun.shadows=shadows?LightShadows.Soft:LightShadows.None;sun.shadowStrength=.8f;sun.shadowBias=.03f;sun.shadowNormalBias=.25f;
            ground.GetComponent<MeshRenderer>().receiveShadows=shadows;
        }
        public int Submit(Camera camera)
        {
            if(!TerrainEnabled)return 0;
            var cast=ShadowsEnabled?ShadowCastingMode.On:ShadowCastingMode.Off;
            Graphics.DrawMeshInstanced(Assets.rockMesh,0,Assets.rock,rocks,64,null,cast,ShadowsEnabled,gameObject.layer,camera,LightProbeUsage.Off);
            Graphics.DrawMeshInstanced(Assets.trunkMesh,0,Assets.wood,trunks,32,null,cast,ShadowsEnabled,gameObject.layer,camera,LightProbeUsage.Off);
            Graphics.DrawMeshInstanced(Assets.canopyMesh,0,Assets.leaves,canopies,32,null,cast,ShadowsEnabled,gameObject.layer,camera,LightProbeUsage.Off);
            return 3;
        }
        private void OnDestroy(){QualitySettings.renderPipeline=originalPipeline;}
    }
}
