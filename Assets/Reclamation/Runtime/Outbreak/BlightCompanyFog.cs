using System;
using UnityEngine;

namespace Reclamation.Blight
{
    // One-metre cells for this small lab. Exploration persists only for the operation.
    public sealed class CompanyFogGrid
    {
        public const int Width = 30, Height = 43;
        private readonly bool[] visible = new bool[Width * Height];
        private readonly bool[] explored = new bool[Width * Height];
        public float Scale { get; set; } = 1;
        public Vector3 Center(int x, int z) => new Vector3(-14.5f + x, 0, -22.5f + z) * Scale;
        public bool Visible(int x, int z) => visible[z * Width + x];
        public bool Explored(int x, int z) => explored[z * Width + x];
        public void Clear() { Array.Clear(visible, 0, visible.Length); Array.Clear(explored, 0, explored.Length); }
        public void BeginObservation() => Array.Clear(visible, 0, visible.Length);
        public void Observe(Vector3 observer, float radius, Func<Vector3, Vector3, bool> sight)
        {
            for (int z = 0; z < Height; z++) for (int x = 0; x < Width; x++)
            {
                Vector3 point = Center(x, z);
                if ((point - observer).sqrMagnitude > radius * radius || !sight(observer, point)) continue;
                int index = z * Width + x; visible[index] = explored[index] = true;
            }
        }
    }

    public sealed partial class BlightCombatLab
    {
        private readonly CompanyFogGrid companyFog = new CompanyFogGrid();
        private float nextFogObservation;
        public bool CompanyTerrainExplored(int x, int z) => companyFog.Explored(x, z);
        public bool CompanyTerrainVisible(int x, int z) => companyFog.Visible(x, z);
        private void ClearCompanyFog() { companyFog.Clear(); nextFogObservation = 0; }
        private void RefreshCompanyFog()
        {
            companyFog.Scale = CompanyScale;
            companyFog.BeginObservation();
            foreach (Actor human in actors)
                if (!human.enemy && human.fighter.Alive)
                    companyFog.Observe(human.root.position, 8, CompanyTerrainSight);
            nextFogObservation = simulationTime + .2f;
        }
        private bool CompanyTerrainSight(Vector3 from, Vector3 point)
        {
            // Reveal the near surface of a wall cell; contact detection still uses
            // exact, unshortened line of sight and never this terrain approximation.
            Vector3 nearSurface = Vector3.MoveTowards(point, from, .71f * CompanyScale);
            return TerrainSight(from, nearSurface);
        }
        private void DrawCompanyFog(Rect map)
        {
            float w = map.width / CompanyFogGrid.Width, h = map.height / CompanyFogGrid.Height;
            // Merge same-state cells per row instead of issuing up to 1,290 GUI
            // rectangles per event. This avoids needless graphics-buffer pressure.
            for (int z = 0; z < CompanyFogGrid.Height; z++)
            {
                int x = 0;
                while (x < CompanyFogGrid.Width)
                {
                    if (companyFog.Visible(x, z)) { x++; continue; }
                    int start = x; bool explored = companyFog.Explored(x, z);
                    while (x < CompanyFogGrid.Width && !companyFog.Visible(x, z) && companyFog.Explored(x, z) == explored) x++;
                    Color shade = explored ? new Color(.02f, .03f, .05f, .65f) : new Color(.035f, .045f, .06f, 1);
                    Fill(new Rect(map.x + start * w, map.y + (CompanyFogGrid.Height - 1 - z) * h, (x - start) * w + .25f, h + .25f), shade);
                }
            }
        }
    }
}
