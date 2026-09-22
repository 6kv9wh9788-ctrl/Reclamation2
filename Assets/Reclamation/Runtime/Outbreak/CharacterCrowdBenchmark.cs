using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Reclamation.Blight
{
    // Rendering + walk animation baseline. No AI, combat, pathfinding or ragdolls.
    public sealed class CharacterCrowdBenchmark : MonoBehaviour
    {
        private readonly List<GameObject> actors = new List<GameObject>();
        private readonly List<float> samples = new List<float>();
        private Camera view;
        private Material floorMaterial;
        private Coroutine spawning;
        private int requested;
        private float warmup, elapsed;
        private bool measuring;
        private string result = "Choose a count. Each run warms up 5 seconds, then measures 20 seconds.";
        private string exportPath;
        private string csv;

        private void Start()
        {
            var cameraObject = new GameObject("Crowd camera"); cameraObject.transform.SetParent(transform);
            view = cameraObject.AddComponent<Camera>(); view.backgroundColor = new Color(.12f, .15f, .18f);
            view.clearFlags = CameraClearFlags.SolidColor; view.farClipPlane = 150;
            var light = new GameObject("Crowd light").AddComponent<Light>(); light.transform.SetParent(transform);
            light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(40, -30, 0);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.SetParent(transform);
            floor.transform.position = Vector3.down * .15f; floor.transform.localScale = new Vector3(60, .3f, 60);
            floorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            floorMaterial.color = new Color(.2f, .23f, .22f); floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
            Run(16);
        }

        public void Run(int count)
        {
            Clear(); requested = Mathf.Clamp(count, 1, 128); spawning = StartCoroutine(Spawn());
        }

        private IEnumerator Spawn()
        {
            int columns = Mathf.CeilToInt(Mathf.Sqrt(requested));
            float extent = columns * 1.6f;
            view.transform.position = new Vector3(0, extent * .75f + 3, extent * .9f + 4);
            view.transform.LookAt(Vector3.up * .8f);
            for (int i = 0; i < requested; i++)
            {
                var actor = new GameObject("Benchmark human " + i); actor.transform.SetParent(transform, false);
                actor.transform.localPosition = new Vector3((i % columns - (columns - 1) * .5f) * 1.6f, 0,
                    (i / columns - (columns - 1) * .5f) * 1.6f);
                actors.Add(actor);
                var rig = actor.AddComponent<ModularHumanRig>(); rig.Build(i % 2 != 0, true);
                rig.SetAppearance(i % 3 != 0, i % 4 == 0, i % 2);
                rig.SetAnimationPhase((i * .618034f) % 1);
                result = "Building " + actors.Count + " / " + requested;
                yield return null; // Keep the editor responsive during setup.
            }
            spawning = null; warmup = 5; elapsed = 0; samples.Clear(); measuring = true;
        }

        private void Update()
        {
            if (!measuring) return;
            if (warmup > 0) { warmup -= Time.unscaledDeltaTime; result = "Warming up..."; return; }
            samples.Add(Time.unscaledDeltaTime * 1000); elapsed += Time.unscaledDeltaTime;
            result = "Measuring " + elapsed.ToString("F0") + " / 20 seconds";
            if (elapsed < 20) return;
            measuring = false;
            var sorted = samples.ToArray(); System.Array.Sort(sorted);
            float p95 = sorted[Mathf.Clamp(Mathf.CeilToInt(sorted.Length * .95f) - 1, 0, sorted.Length - 1)];
            float mean = elapsed * 1000 / samples.Count;
            result = requested + " humans | " + (1000 / mean).ToString("F1") + " average FPS | p95 " + p95.ToString("F1") + " ms";
            // Frame intervals include VSync/editor overhead; these are NOT GPU timings.
            csv = "sample,frame_interval_ms\n";
            var output = new System.Text.StringBuilder(csv);
            for (int i = 0; i < samples.Count; i++) output.Append(i).Append(',').Append(samples[i].ToString("F4", CultureInfo.InvariantCulture)).Append('\n');
            csv = output.ToString();
            int rendererCount = 0, materialSlots = 0;
            foreach (GameObject actor in actors) foreach (SkinnedMeshRenderer renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
            { rendererCount++; materialSlots += renderer.sharedMaterials.Length; }
            Debug.Log("Crowd revision 2 | " + result + " | " + Screen.width + "x" + Screen.height + " | " + SystemInfo.graphicsDeviceName +
                " | " + SystemInfo.processorType + " | VSync " + QualitySettings.vSyncCount + " | frame cap " + Application.targetFrameRate +
                " | Editor " + Application.isEditor + " | skinned renderers " + rendererCount + " | material slots " + materialSlots +
                " | shared variants " + CrowdMeshCache.ActiveVariants + " | walk/render only; no battle simulation.");
        }

        private void Clear()
        {
            if (spawning != null) { StopCoroutine(spawning); spawning = null; }
            measuring = false; csv = null; exportPath = null;
            foreach (GameObject actor in actors) if (actor != null) { actor.SetActive(false); Destroy(actor); }
            actors.Clear(); samples.Clear();
        }

        private void OnGUI()
        {
            float scale = Mathf.Max(.2f, Mathf.Min(Screen.width / 1100f, Screen.height / 700f));
            Matrix4x4 old = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            GUI.Box(new Rect(10, 10, 760, 175), "CHARACTER CROWD BASELINE — no AI / combat / ragdolls");
            int[] counts = {16, 32, 64, 128};
            for (int i = 0; i < counts.Length; i++)
                if (GUI.Button(new Rect(24 + i * 105, 42, 98, 28), counts[i] + " humans")) Run(counts[i]);
            if (GUI.Button(new Rect(452, 42, 110, 28), "Clear / cancel")) { Clear(); result = "Cleared."; }
            if (csv != null && GUI.Button(new Rect(570, 42, 180, 28), "Export frame samples"))
            {
                try { exportPath = Path.Combine(Application.persistentDataPath, "character-crowd-" + requested + ".csv"); File.WriteAllText(exportPath, csv); Debug.Log("Saved " + exportPath); }
                catch (System.Exception exception) { exportPath = "Export failed: " + exception.Message; }
            }
            GUI.Label(new Rect(24, 82, 730, 25), result);
            GUI.Label(new Rect(24, 110, 730, 60), "Compare runs at the same resolution and quality. Frame intervals include VSync and editor overhead.\n" +
                "128 is this first baseline's cap, not our final battle target. Export location is logged to Console.\n" + (exportPath ?? "Use a standalone build later for representative performance."));
            GUI.matrix = old;
        }

        private void OnDestroy() { if (floorMaterial != null) Destroy(floorMaterial); }
    }
}
