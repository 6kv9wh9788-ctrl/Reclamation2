using System.Collections.Generic;
using UnityEngine;
namespace Reclamation.Atlas
{
    public sealed class AtlasCharacterVisual : MonoBehaviour
    {
        private AtlasCharacterLibrary library;
        private MeshFilter body, gear, details;
        private AtlasOutfitLibrary outfits;
        private Transform head, torch, flame;
        private readonly List<Material> owned = new List<Material>();
        private float phase;
        private AtlasCharacterLibrary combatLibrary;
        private AtlasOutfitLibrary combatOutfits;
        private int combatIndex=-1;
        public void SetCombatState(Reclamation.Blight.DuelFighter fighter,bool moving,bool sprint)
        {
            if(!combatLibrary){combatLibrary=Resources.Load<AtlasCharacterLibrary>("AtlasCombatPoses");combatOutfits=Resources.Load<AtlasOutfitLibrary>("AtlasCombatOutfits");}
            if(!combatLibrary||!combatOutfits)throw new System.InvalidOperationException("Bake atlas combat presentation first.");
            if(!fighter.Alive)moving=false;
            CurrentClip=moving?sprint?"Run":"Walk":"Idle";combatIndex=-1;
            var action=fighter.Action;
            if(action==Reclamation.Blight.DuelAction.Windup||action==Reclamation.Blight.DuelAction.Recovery)
            {
                float progress=action==Reclamation.Blight.DuelAction.Windup?fighter.Progress*.67f:.67f+fighter.Progress*.33f;
                combatIndex=33+(fighter.Heavy?16:0)+Mathf.Min(15,(int)(progress*16));
            }
            else if(action==Reclamation.Blight.DuelAction.Dodge)combatIndex=81+Mathf.Min(15,(int)(fighter.Progress*16));
            else if(fighter.Blocking)combatIndex=65;
            SetGuardAlertness(fighter.Alive?1:0);Sample();
        }
        private AtlasCharacterLibrary guardLibrary;
        private AtlasOutfitLibrary guardOutfits;
        public float Alertness { get; private set; } = 1;
        public bool UsesGuardPoses => guardLibrary != null;
        public void SetGuardAlertness(float value) { Alertness = Mathf.Clamp01(value); }
        public void UseGuardPoses()
        {
            guardLibrary = Resources.Load<AtlasCharacterLibrary>("AtlasGuardPoses");
            guardOutfits = Resources.Load<AtlasOutfitLibrary>("AtlasGuardOutfits");
            if (!guardLibrary || !guardOutfits) throw new System.InvalidOperationException("Bake guard presentation poses first.");
            Alertness = 0; Sample();
        }
        public string CurrentClip { get; private set; } = "Idle";
        public float PlaybackSpeed { get; set; } = 1;
        public bool TorchLit { get; private set; }
        public Vector3 FlamePosition => flame.position;
        public int Variant { get; private set; }
        public int BodyVertices => library.body[0].vertexCount;
        public void Play(string clip) { CurrentClip = clip; }
        private Material Mat(Color color, bool glow = false, float smoothness = .04f, float metallic = 0)
        {
            var m = new Material(Shader.Find(glow ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            m.color = color;if(!glow){m.SetFloat("_Smoothness",smoothness);m.SetFloat("_Metallic",metallic);} owned.Add(m); return m;
        }
        private Transform Prop(string label, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = label; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            Destroy(go.GetComponent<Collider>()); go.GetComponent<Renderer>().sharedMaterial = material; return go.transform;
        }
        public void Build(int variant, bool commander = false, bool enemy = false, bool platoonCommander = false)
        {
            library = Resources.Load<AtlasCharacterLibrary>("AtlasCharacters");
            if (!library) throw new System.InvalidOperationException("Bake AtlasCharacters before running the atlas.");
            outfits=Resources.Load<AtlasOutfitLibrary>("AtlasOutfits");if(!outfits)throw new System.InvalidOperationException("Bake atlas outfits first.");
            Variant = variant; phase = Mathf.Repeat(variant * .381f, 1);
            var shape = new GameObject("Prototype body", typeof(MeshFilter), typeof(MeshRenderer)); shape.transform.SetParent(transform, false);
            body = shape.GetComponent<MeshFilter>();
            Color cloth = enemy ? new Color(.43f,.16f,.12f) : new Color(.12f,.31f,.32f);
            Color[] skins = {new Color(.73f,.48f,.31f),new Color(.4f,.24f,.16f),new Color(.87f,.65f,.44f),new Color(.57f,.36f,.22f)};
            var leather = Mat(new Color(.16f,.11f,.075f));
            shape.GetComponent<MeshRenderer>().sharedMaterials = new[] {Mat(cloth * ( .8f + variant % 4 * .12f)), Mat(skins[variant % 4],false,.12f), leather};
            var kit = new GameObject("Helmet breastplate shield and sword", typeof(MeshFilter), typeof(MeshRenderer)); kit.transform.SetParent(transform,false);
            gear = kit.GetComponent<MeshFilter>();
            var metal=Mat(commander?new Color(.55f,.43f,.19f):Color.Lerp(new Color(.28f,.32f,.34f),new Color(.44f,.46f,.43f),variant%3/2f),false,.22f,.55f);
            var hide=Mat(new Color(.27f,.16f,.085f),false,.08f);
            kit.GetComponent<MeshRenderer>().sharedMaterials=new[]{metal,commander||variant%3==0?metal:hide,Mat(cloth*.85f),metal};
            var garments=new GameObject("Padded tunic belt pouch and straps",typeof(MeshFilter),typeof(MeshRenderer));garments.transform.SetParent(transform,false);details=garments.GetComponent<MeshFilter>();
            garments.GetComponent<MeshRenderer>().sharedMaterials=new[]{Mat(cloth*(commander?1.15f:1)),hide,metal};
            head = new GameObject("Head details").transform; head.SetParent(transform,false);
            var dark = Mat(new Color(.07f,.045f,.025f));
            for(int side=-1;side<=1;side+=2) Prop("Eye",PrimitiveType.Sphere,head,new Vector3(side*.033f,-.045f,.079f),new Vector3(.018f,.013f,.013f),dark);
            if(variant%3==0)Prop("Beard",PrimitiveType.Sphere,head,new Vector3(0,-.092f,.058f),new Vector3(.09f,.065f,.057f),dark);
            if(commander)Prop("Command crest",PrimitiveType.Cube,head,new Vector3(0,.18f,0),new Vector3(.045f,.13f,.18f),Mat(new Color(.7f,.12f,.07f)));
            else if(platoonCommander)Prop("Platoon crest",PrimitiveType.Cube,head,new Vector3(0,.16f,0),new Vector3(.035f,.09f,.14f),Mat(new Color(.75f,.78f,.7f)));
            else if(variant%3==1)Prop("Helmet plume",PrimitiveType.Cube,head,new Vector3(0,.14f,-.035f),new Vector3(.025f,.05f,.08f),Mat(cloth));
            torch = new GameObject("Night watch torch").transform; torch.SetParent(transform,false);
            Prop("Handle",PrimitiveType.Cylinder,torch,Vector3.up*.16f,new Vector3(.045f,.22f,.045f),leather);
            Prop("Iron cup",PrimitiveType.Cylinder,torch,Vector3.up*.4f,new Vector3(.09f,.065f,.09f),dark);
            flame = Prop("Flame",PrimitiveType.Sphere,torch,Vector3.up*.51f,new Vector3(.10f,.23f,.10f),Mat(new Color(1,.47f,.055f),true));
            Prop("Flame core",PrimitiveType.Sphere,flame,new Vector3(0,-.12f,0),new Vector3(.65f,.65f,.65f),Mat(new Color(1,.88f,.36f),true));
            transform.localScale = new Vector3(.96f+variant%3*.045f,.96f+variant%5*.02f,.96f+variant%3*.025f);
            SetTorch(false); Sample();
        }
        public void SetTorch(bool lit) { TorchLit = lit; if(torch)torch.gameObject.SetActive(lit); }
        private void LateUpdate()
        {
            if(!library)return;
            phase = Mathf.Repeat(phase + Time.deltaTime * PlaybackSpeed * (CurrentClip=="Run"?1.8f:.95f),1);
            Sample();
        }
        private void Sample()
        {
            int index = CurrentClip=="Idle"?32:(CurrentClip=="Run"?16:0)+Mathf.Min(15,(int)(phase*16));
            var poses = guardLibrary ? guardLibrary : library;
            var kit = guardOutfits ? guardOutfits : outfits;
            if (guardLibrary) index += Mathf.RoundToInt(Alertness * 4) * 33;
            if(combatIndex>=0){poses=combatLibrary;kit=combatOutfits;index=combatIndex;}
            body.sharedMesh=poses.body[index]; gear.sharedMesh=kit.gear[index];details.sharedMesh=kit.details[index];
            torch.localPosition=poses.hand[index]; head.localPosition=poses.head[index].GetColumn(3); head.localRotation=poses.head[index].rotation;
            flame.localScale=new Vector3(.10f,.21f+Mathf.Sin(phase*31)*.035f,.10f);
        }
        private void OnDestroy(){foreach(var m in owned)if(m)Destroy(m);}
    }
}
