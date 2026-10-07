using UnityEngine;
namespace Reclamation.Atlas
{
    // Shared poses from the approved joint-built prototype; no per-soldier skeleton.
    public sealed class AtlasCharacterLibrary : ScriptableObject
    {
        public Mesh[] body, gear;
        public Vector3[] hand;
        public Matrix4x4[] head;
    }
}
