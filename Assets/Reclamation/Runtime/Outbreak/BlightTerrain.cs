using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Blight
{
    // Small static visibility graph for the gateway greybox; no NavMesh bake required.
    public sealed class BlightTerrain
    {
        private readonly List<Rect> walls = new List<Rect>();
        private readonly List<bool> occludes = new List<bool>();
        public int Count => walls.Count;
        public Rect Bounds { get; set; } = new Rect(-15, -23, 30, 43);
        public Rect WallAt(int index) => walls[index];
        public void Add(Rect wall, bool blocksSight = true) { walls.Add(wall); occludes.Add(blocksSight); }
        public void Clear() { walls.Clear(); occludes.Clear(); }
        public bool Clear(Vector3 from, Vector3 to, float radius, bool sightOnly = false)
        {
            for (int i = 0; i < walls.Count; i++)
            {
                if (sightOnly && !occludes[i]) continue;
                Rect wall = walls[i];
                float enter = 0, exit = 1;
                if (Slab(from.x, to.x - from.x, wall.xMin - radius, wall.xMax + radius, ref enter, ref exit) &&
                    Slab(from.z, to.z - from.z, wall.yMin - radius, wall.yMax + radius, ref enter, ref exit)) return false;
            }
            return true;
        }
        private static bool Slab(float origin, float delta, float min, float max, ref float enter, ref float exit)
        {
            if (Mathf.Abs(delta) < .000001f) return origin >= min && origin <= max;
            float a = (min - origin) / delta, b = (max - origin) / delta;
            enter = Mathf.Max(enter, Mathf.Min(a, b)); exit = Mathf.Min(exit, Mathf.Max(a, b));
            return enter <= exit;
        }
        public Vector3 Move(Vector3 from, Vector3 to, float radius)
        {
            if (Clear(from, to, radius)) return to;
            Vector3 x = new Vector3(to.x, 0, from.z), z = new Vector3(from.x, 0, to.z);
            bool canX = Clear(from, x, radius), canZ = Clear(from, z, radius);
            if (canX && canZ) return (x - from).sqrMagnitude > (z - from).sqrMagnitude ? x : z;
            return canX ? x : canZ ? z : from;
        }
        public Vector3 NearbyGoal(Vector3 goal, float radius)
        {
            if (Clear(goal, goal, radius)) return goal;
            for (float ring = .25f; ring <= 2; ring += .25f)
                for (int angle = 0; angle < 16; angle++)
                {
                    float radians = angle * Mathf.PI / 8;
                    Vector3 point = goal + new Vector3(Mathf.Cos(radians), 0, Mathf.Sin(radians)) * ring;
                    if (Bounds.Contains(new Vector2(point.x, point.z)) &&
                        Clear(point, point, radius) && Clear(point, goal, .03f)) return point;
                }
            return goal;
        }
        public Vector3 Next(Vector3 start, Vector3 goal, float radius)
        {
            if (Clear(start, goal, radius)) return goal;
            var nodes = new List<Vector3> { start, goal };
            foreach (Rect wall in walls)
            {
                float margin = radius + .12f;
                foreach (float x in new[] { wall.xMin - margin, wall.xMax + margin })
                    foreach (float z in new[] { wall.yMin - margin, wall.yMax + margin })
                    {
                        Vector3 point = new Vector3(x, 0, z);
                        if (Bounds.Contains(new Vector2(x, z)) && Clear(point, point, radius)) nodes.Add(point);
                    }
            }
            var distance = new float[nodes.Count]; var previous = new int[nodes.Count]; var visited = new bool[nodes.Count];
            for (int i = 0; i < nodes.Count; i++) { distance[i] = float.PositiveInfinity; previous[i] = -1; }
            distance[0] = 0;
            for (int step = 0; step < nodes.Count; step++)
            {
                int best = -1;
                for (int i = 0; i < nodes.Count; i++) if (!visited[i] && (best < 0 || distance[i] < distance[best])) best = i;
                if (best < 0 || float.IsInfinity(distance[best])) break;
                if (best == 1) break;
                visited[best] = true;
                for (int next = 0; next < nodes.Count; next++)
                {
                    if (visited[next] || !Clear(nodes[best], nodes[next], radius)) continue;
                    float cost = distance[best] + Vector3.Distance(nodes[best], nodes[next]);
                    if (cost < distance[next]) { distance[next] = cost; previous[next] = best; }
                }
            }
            if (previous[1] < 0) return start;
            int waypoint = 1;
            while (previous[waypoint] > 0) waypoint = previous[waypoint];
            return nodes[waypoint];
        }
    }
}
