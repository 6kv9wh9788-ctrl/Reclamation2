using UnityEngine;

namespace Reclamation.Blight
{
    // Shared baked poses: no per-unit Animator, skeleton, or finger transforms.
    public sealed class CrowdAnimationLibrary : ScriptableObject
    {
        public const int Frames = 16;
        public Mesh[] nearRun, farRun, nearAttack, farAttack;
        public Mesh nearIdle, farIdle;
        public Material friendly, enemy;
        public Mesh Get(int lod, int motion, int frame)
        {
            if(motion==2) return lod==0?nearIdle:farIdle;
            frame=Mathf.Clamp(frame,0,Frames-1);
            return motion==0?(lod==0?nearRun[frame]:farRun[frame]):(lod==0?nearAttack[frame]:farAttack[frame]);
        }
    }
}
