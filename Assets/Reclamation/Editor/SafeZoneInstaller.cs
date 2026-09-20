using Reclamation.Outbreak;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reclamation.Editor
{
    public static class SafeZoneInstaller
    {
        [MenuItem("Reclamation/Add Safe Zone to Open Outbreak Scene")]
        public static void InstallInOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            OutbreakDirector director = Object.FindFirstObjectByType<OutbreakDirector>();
            if (director == null)
            {
                EditorUtility.DisplayDialog("Outbreak scene required",
                    "Open PatientZeroLab before adding the safe zone.", "OK");
                return;
            }
            if (director.SafeZone != null)
            {
                Selection.activeGameObject = director.SafeZone.gameObject;
                Debug.Log("This outbreak scene already has a safe zone.");
                return;
            }
            SafeZone zone = Create(new Vector3(20, 0, -24));
            Undo.RecordObject(director, "Attach safe zone");
            director.AttachSafeZone(zone);
            EditorUtility.SetDirty(director);
            EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
            Selection.activeGameObject = zone.gameObject;
            Debug.Log("Safe zone installed. Save the scene, press Play, and issue Evacuate orders from the outbreak panel.");
        }

        public static SafeZone Create(Vector3 center)
        {
            var root = new GameObject("Player Safe Zone");
            Undo.RegisterCreatedObjectUndo(root, "Create safe zone");
            root.transform.position = center;

            GameObject shelter = Pad(root.transform, "Refuge (4 beds)", new Vector3(-3, 0.04f, 0),
                new Vector3(5, 0.08f, 7), new Color(0.16f, 0.5f, 0.72f));
            GameObject quarantine = Pad(root.transform, "Quarantine (2 beds)", new Vector3(3, 0.04f, 0),
                new Vector3(4, 0.08f, 5), new Color(0.82f, 0.61f, 0.12f));
            Transform shelterEntry = Point(root.transform, "Refuge entrance", new Vector3(-3, 0, -3));
            Transform quarantineEntry = Point(root.transform, "Quarantine entrance", new Vector3(3, 0, -2));
            var zone = root.AddComponent<SafeZone>();
            zone.Configure(shelterEntry, quarantineEntry, 4, 2);
            shelter.layer = quarantine.layer = 2;
            return zone;
        }

        private static GameObject Pad(Transform parent, string name, Vector3 localPosition, Vector3 scale, Color color)
        {
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = name;
            pad.transform.SetParent(parent, false);
            pad.transform.localPosition = localPosition;
            pad.transform.localScale = scale;
            Object.DestroyImmediate(pad.GetComponent<Collider>());
            const string folder = "Assets/Reclamation/GeneratedMaterials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Reclamation", "GeneratedMaterials");
            string path = $"{folder}/SafeZone-{ColorUtility.ToHtmlStringRGBA(color)}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
                AssetDatabase.CreateAsset(material, path);
            }
            pad.GetComponent<Renderer>().sharedMaterial = material;
            return pad;
        }

        private static Transform Point(Transform parent, string name, Vector3 localPosition)
        {
            var point = new GameObject(name).transform;
            point.SetParent(parent, false);
            point.localPosition = localPosition;
            return point;
        }
    }
}
