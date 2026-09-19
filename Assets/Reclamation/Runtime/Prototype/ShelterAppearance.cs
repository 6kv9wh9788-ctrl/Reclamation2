using UnityEngine;

namespace Reclamation.Prototype
{
    // Visual-only: the blueprint owns all accounting and construction state.
    [RequireComponent(typeof(ShelterBlueprint))]
    public sealed class ShelterAppearance : MonoBehaviour
    {
        private ShelterBlueprint site;
        private GameObject finished;
        private GameObject foundation;
        private Material timber;
        private Material marker;

        private void Start()
        {
            site = GetComponent<ShelterBlueprint>();
            timber = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            timber.color = new Color(0.55f, 0.32f, 0.15f);
            marker = new Material(timber) { color = new Color(0.1f, 0.75f, 0.85f) };
            foundation = Part("Blueprint footprint", new Vector3(0, 0.04f, 0), new Vector3(5, 0.08f, 4), marker, transform);
            finished = new GameObject("Completed shelter");
            finished.transform.SetParent(transform, false);
            Part("Floor", new Vector3(0, 0.1f, 0), new Vector3(5, 0.2f, 4), timber, finished.transform);
            Part("Back wall", new Vector3(0, 1.4f, 1.9f), new Vector3(5, 2.8f, 0.2f), timber, finished.transform);
            Part("Left wall", new Vector3(-2.4f, 1.4f, 0), new Vector3(0.2f, 2.8f, 4), timber, finished.transform);
            Part("Right wall", new Vector3(2.4f, 1.4f, 0), new Vector3(0.2f, 2.8f, 4), timber, finished.transform);
            Part("Roof", new Vector3(0, 2.9f, 0), new Vector3(5.4f, 0.25f, 4.4f), timber, finished.transform);
            finished.SetActive(false);
        }

        private static GameObject Part(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.layer = 2;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            // This milestone is a visual shelter, not a navigable building interior.
            go.GetComponent<Collider>().enabled = false;
            return go;
        }

        private void Update()
        {
            foundation.SetActive(!site.Cancelled && !site.Complete);
            finished.SetActive(site.Complete);
        }

        private void OnDestroy()
        {
            if (timber != null) Destroy(timber);
            if (marker != null) Destroy(marker);
        }
    }
}
