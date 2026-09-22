using UnityEngine;

namespace Reclamation.Blight
{
    public sealed class StylizedCharacterPreview : MonoBehaviour
    {
        private readonly Animation[] characters = new Animation[2];
        private readonly string[] names = { "Idle", "Walk", "Run", "LightAttack", "HeavyAttack", "Dodge", "Block", "Stagger", "Death", "Crawl" };
        private string selected = "Idle";
        private void Start()
        {
            for (int i = 0; i < 2; i++)
            {
                CharacterArtData data = StylizedCharacterArt.Load(i == 1);
                if (data == null) { Debug.LogError("Missing ReclamationArt JSON resources."); continue; }
                var model = new GameObject(data.name); model.transform.SetParent(transform, false);
                model.transform.localPosition = Vector3.right * (i == 0 ? -0.75f : 0.75f);
                var bones = new Transform[data.bones.Length];
                for (int b = 0; b < bones.Length; b++)
                {
                    bones[b] = new GameObject(data.bones[b].name).transform;
                    bones[b].SetParent(data.bones[b].parent < 0 ? model.transform : bones[data.bones[b].parent], false);
                    bones[b].localPosition = StylizedCharacterArt.Vector(data.bones[b].position);
                }
                var art = model.AddComponent<StylizedCharacterArt>(); art.Build(data, bones, true);
                characters[i] = art.BuildPreviewAnimations(data, bones);
            }
            var camera = new GameObject("Character preview camera").AddComponent<Camera>(); camera.transform.SetParent(transform);
            camera.transform.position = new Vector3(2.6f, 1.9f, 5); camera.transform.LookAt(new Vector3(0, 1, 0));
            camera.fieldOfView = 34; camera.backgroundColor = new Color(.14f, .18f, .22f); camera.clearFlags = CameraClearFlags.SolidColor;
            var sun = new GameObject("Preview light").AddComponent<Light>(); sun.transform.SetParent(transform);
            sun.type = LightType.Directional; sun.intensity = 1.2f; sun.transform.rotation = Quaternion.Euler(35, -35, 0);
        }
        private void OnGUI()
        {
            float scale = Mathf.Max(.25f, Mathf.Min(Screen.width / 1100f, Screen.height / 750f));
            Matrix4x4 old = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            GUI.Box(new Rect(12, 12, 510, 58), "RECLAMATION — CHARACTER MOTION STUDY\n" + selected + " | Choose a clip; click again to replay");
            for (int i = 0; i < names.Length; i++)
                if (GUI.Button(new Rect(12 + i % 5 * 126, Screen.height / scale - 86 + i / 5 * 36, 120, 30), names[i]))
                {
                    selected = names[i];
                    foreach (Animation animation in characters)
                        if (animation != null) { animation.Stop(); animation.Play(selected); }
                }
            GUI.matrix = old;
        }
    }
}
