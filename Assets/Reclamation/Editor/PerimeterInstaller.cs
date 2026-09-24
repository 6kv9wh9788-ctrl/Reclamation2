using Reclamation.Outbreak;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reclamation.Editor
{
    public static class PerimeterInstaller
    {
        [MenuItem("Reclamation/Legacy/Add Buildable Perimeter to Open Refuge")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var director = Object.FindFirstObjectByType<OutbreakDirector>();
            if (director == null || director.SafeZone == null || director.gameObject.scene != SceneManager.GetActiveScene())
            {
                EditorUtility.DisplayDialog("Refuge required", "Open your Patient Zero scene and add its safe zone first.", "OK"); return;
            }
            var zone = director.SafeZone;
            if (zone.Perimeter != null) { Selection.activeGameObject = zone.Perimeter.gameObject; return; }
            var root = new GameObject("Buildable Perimeter");
            Undo.RegisterCreatedObjectUndo(root, "Add buildable perimeter");
            root.transform.SetParent(zone.transform, false);
            var defense = root.AddComponent<PerimeterDefense>();
            var sections = new[]
            {
                Section(root.transform, "Gate", new Vector3(3.5f, 0, 5), new Vector3(3, 2.4f, 0.5f), Vector3.forward, true),
                Section(root.transform, "North west fence", new Vector3(-3, 0, 5), new Vector3(10, 2.4f, 0.5f), Vector3.forward),
                Section(root.transform, "North east fence", new Vector3(6.5f, 0, 5), new Vector3(3, 2.4f, 0.5f), Vector3.forward),
                Section(root.transform, "East fence", new Vector3(8, 0, -0.5f), new Vector3(0.5f, 2.4f, 11.5f), Vector3.right),
                Section(root.transform, "South fence", new Vector3(0, 0, -6), new Vector3(16.5f, 2.4f, 0.5f), Vector3.back),
                Section(root.transform, "West fence", new Vector3(-8, 0, -0.5f), new Vector3(0.5f, 2.4f, 11.5f), Vector3.left)
            };
            defense.Configure(zone, sections);
            Undo.RecordObject(zone, "Attach perimeter"); zone.AttachPerimeter(defense);
            EditorUtility.SetDirty(zone); EditorSceneManager.MarkSceneDirty(zone.gameObject.scene);
            AssetDatabase.SaveAssets(); Selection.activeGameObject = root;
            Debug.Log("Perimeter slots added. Save the scene. In Play, evacuate a worker, then build sections from the panel. Gate starts open.");
        }

        [MenuItem("Reclamation/Legacy/Scenario/Visitor Becomes Brute")]
        public static void Brute() => SetVisitor(ZombieClass.Brute);
        [MenuItem("Reclamation/Legacy/Scenario/Visitor Becomes Ordinary Zombie")]
        public static void Ordinary() => SetVisitor(ZombieClass.Ordinary);

        private static void SetVisitor(ZombieClass variant)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (var person in Object.FindObjectsByType<OutbreakAgent>(FindObjectsSortMode.None))
                if (person.DisplayName == "Visitor" && person.gameObject.scene == SceneManager.GetActiveScene())
                {
                    Undo.RecordObject(person, "Change visitor variant"); person.SetZombieClass(variant);
                    EditorUtility.SetDirty(person); EditorSceneManager.MarkSceneDirty(person.gameObject.scene);
                    Debug.Log($"Visitor will become {variant} after normal infection progression. Save the scene."); return;
                }
            Debug.LogWarning("No Visitor found in the open scene.");
        }

        private static DefenseSection Section(Transform parent, string name, Vector3 position, Vector3 size, Vector3 normal, bool gate = false)
        {
            var go = new GameObject(name); go.layer = 2;
            go.transform.SetParent(parent, false); go.transform.localPosition = position;
            var section = go.AddComponent<DefenseSection>();
            go.GetComponent<BoxCollider>().enabled = false;
            go.GetComponent<UnityEngine.AI.NavMeshObstacle>().enabled = false;
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Wood visual"; visual.layer = 2; visual.transform.SetParent(go.transform, false);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.localPosition = Vector3.up * 0.04f;
            visual.transform.localScale = new Vector3(size.x, 0.08f, size.z);
            const string path = "Assets/Reclamation/GeneratedMaterials/PerimeterWood.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Reclamation/GeneratedMaterials"))
                    AssetDatabase.CreateFolder("Assets/Reclamation", "GeneratedMaterials");
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.42f, 0.25f, 0.1f) };
                AssetDatabase.CreateAsset(material, path);
            }
            visual.GetComponent<Renderer>().sharedMaterial = material;
            section.Configure(size, normal, gate, visual.transform);
            return section;
        }
    }
}
