using Reclamation.Neighborhood;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Reclamation.Editor
{
    public static class HumanVisualBuilder
    {
        [MenuItem("Reclamation/Upgrade Open Scene to Human Characters")]
        public static void UpgradeOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            int count = 0;
            foreach (var agent in Object.FindObjectsByType<NavMeshAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (agent.gameObject.scene != SceneManager.GetActiveScene()) continue;
                if (agent.GetComponent<CivilianRoutine>() == null &&
                    agent.GetComponent<Reclamation.Prototype.HaulWorker>() == null) continue;
                if (Add(agent.gameObject)) count++;
            }
            if (count > 0) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"Added {count} human visuals. Save the scene to keep them. Undo is available.");
        }

        public static bool Add(GameObject actor)
        {
            if (actor.GetComponentInChildren<HumanVisual>(true) != null) return false;
            var original = actor.GetComponent<Renderer>();
            var nav = actor.GetComponent<NavMeshAgent>();
            if (original == null || nav == null) return false;
            var root = new GameObject("Human Visual");
            Undo.RegisterCreatedObjectUndo(root, "Add human visual");
            root.transform.SetParent(actor.transform, false);
            root.transform.localPosition = Vector3.down * nav.baseOffset;
            var shirt = original.sharedMaterial;
            Material skin = MaterialFor(new Color(0.72f, 0.49f, 0.34f));
            Material trousers = MaterialFor(new Color(0.12f, 0.17f, 0.23f));
            Material hair = MaterialFor(new Color(0.13f, 0.08f, 0.055f));
            Material shoes = MaterialFor(new Color(0.055f, 0.06f, 0.07f));
            Part(root.transform, "Shirt", new Vector3(0, 1.2f, 0), new Vector3(0.52f, 0.6f, 0.3f), shirt);
            var head = Part(root.transform, "Head", new Vector3(0, 1.76f, 0), new Vector3(0.34f, 0.4f, 0.32f), skin);
            Part(root.transform, "Hair", new Vector3(0, 1.95f, -0.025f), new Vector3(0.36f, 0.12f, 0.34f), hair);
            Part(root.transform, "Nose", new Vector3(0, 1.74f, 0.18f), new Vector3(0.08f, 0.1f, 0.08f), skin);
            for (int i = -1; i <= 1; i += 2)
                Part(root.transform, "Eye", new Vector3(i * 0.09f, 1.82f, 0.165f), new Vector3(0.045f, 0.045f, 0.02f), shoes);
            Transform armL = Limb(root.transform, "Left arm", new Vector3(-0.35f, 1.44f, 0), 0.56f, 0.17f, shirt);
            Transform armR = Limb(root.transform, "Right arm", new Vector3(0.35f, 1.44f, 0), 0.56f, 0.17f, shirt);
            Part(armL, "Hand", new Vector3(0, -0.6f, 0), Vector3.one * 0.17f, skin);
            Part(armR, "Hand", new Vector3(0, -0.6f, 0), Vector3.one * 0.17f, skin);
            Transform legL = Limb(root.transform, "Left leg", new Vector3(-0.145f, 0.91f, 0), 0.74f, 0.22f, trousers);
            Transform legR = Limb(root.transform, "Right leg", new Vector3(0.145f, 0.91f, 0), 0.74f, 0.22f, trousers);
            Part(legL, "Shoe", new Vector3(0, -0.82f, 0.07f), new Vector3(0.24f, 0.18f, 0.4f), shoes);
            Part(legR, "Shoe", new Vector3(0, -0.82f, 0.07f), new Vector3(0.24f, 0.18f, 0.4f), shoes);
            root.AddComponent<HumanVisual>().Configure(armL, armR, legL, legR, head.GetComponent<Renderer>());
            Undo.RecordObject(original, "Hide capsule renderer");
            original.enabled = false;
            return true;
        }

        private static Transform Limb(Transform parent, string name, Vector3 position, float length, float width, Material material)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false); pivot.localPosition = position;
            Part(pivot, "Clothing", new Vector3(0, -length / 2, 0), new Vector3(width, length, width), material);
            return pivot;
        }

        private static GameObject Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name; part.layer = 2;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position; part.transform.localScale = scale;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }

        private static Material MaterialFor(Color color)
        {
            const string folder = "Assets/Reclamation/GeneratedMaterials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Reclamation", "GeneratedMaterials");
            string path = $"{folder}/Human-{ColorUtility.ToHtmlStringRGBA(color)}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
