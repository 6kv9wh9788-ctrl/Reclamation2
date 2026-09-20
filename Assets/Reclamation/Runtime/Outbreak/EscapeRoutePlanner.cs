using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Reclamation.Outbreak
{
    // A bounded search for a complete escape route. No teleporting or movement outside the NavMesh.
    public sealed class EscapeRoutePlanner
    {
        private static readonly float[] Angles = { 0, 30, -30, 60, -60, 90, -90, 120, -120, 150, -150, 180 };
        private static readonly float[] Distances = { 7, 3 };
        private readonly NavMeshPath path = new();

        public bool TryFind(NavMeshAgent nav, Vector3 threat, IReadOnlyList<OutbreakAgent> population,
            out Vector3 destination)
        {
            // Actor roots include baseOffset (1 m in the generated neighborhood).
            // Search at foot level so a narrow horizontal probe can still find the mesh.
            Vector3 origin = nav.transform.position - Vector3.up * nav.baseOffset;
            destination = origin;
            Vector3 away = origin - threat; away.y = 0;
            if (away.sqrMagnitude < 0.01f) away = nav.transform.forward;
            away.Normalize();
            float startSafety = ThreatDistance(origin, threat, population);
            // Permit a lateral detour near a boundary, but reject routes through a pursuer.
            float minimumSafety = Mathf.Max(0, Mathf.Min(1.7f, startSafety - 0.2f));
            float best = float.NegativeInfinity;
            bool found = false;
            foreach (float length in Distances)
                foreach (float angle in Angles)
                {
                    Vector3 direction = Quaternion.Euler(0, angle, 0) * away;
                    Vector3 desired = origin + direction * length;
                    if (!NavMesh.SamplePosition(desired, out NavMeshHit hit, 0.65f, nav.areaMask) ||
                        !nav.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) continue;
                    Vector3 offset = hit.position - origin; offset.y = 0;
                    if (offset.magnitude < 1) continue;
                    float safety = ThreatDistance(hit.position, threat, population);
                    if (safety < startSafety + 0.25f) continue;
                    Vector3[] corners = path.corners;
                    float travel = 0;
                    bool safe = true;
                    for (int i = 1; i < corners.Length && safe; i++)
                    {
                        float segment = Vector3.Distance(corners[i - 1], corners[i]);
                        travel += segment;
                        int samples = Mathf.Max(1, Mathf.CeilToInt(segment / 0.5f));
                        for (int sample = 1; sample <= samples; sample++)
                            if (ThreatDistance(Vector3.Lerp(corners[i - 1], corners[i], (float)sample / samples),
                                threat, population) < minimumSafety) { safe = false; break; }
                    }
                    if (!safe) continue;
                    float clearance = NavMesh.FindClosestEdge(hit.position, out NavMeshHit edge, nav.areaMask)
                        ? Mathf.Min(edge.distance, 2) : 0;
                    float crowdCost = 0;
                    if (population != null)
                        foreach (var person in population)
                            if (person != null && person.gameObject.activeInHierarchy && person.transform != nav.transform)
                                crowdCost += Mathf.Max(0, 2 - FlatDistance(hit.position, person.transform.position));
                    float score = safety * 2 + clearance * 0.6f - travel * 0.12f - crowdCost;
                    if (score <= best) continue;
                    best = score; destination = hit.position; found = true;
                }
            return found;
        }

        private static float ThreatDistance(Vector3 position, Vector3 fallback, IReadOnlyList<OutbreakAgent> population)
        {
            float distance = FlatDistance(position, fallback);
            if (population != null)
                foreach (var person in population)
                    if (person != null && person.gameObject.activeInHierarchy &&
                        person.State == InfectionState.Turned && person.Contagious)
                        distance = Mathf.Min(distance, FlatDistance(position, person.transform.position));
            return distance;
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0;
            return Vector3.Distance(a, b);
        }
    }
}
