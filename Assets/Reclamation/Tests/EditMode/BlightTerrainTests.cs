using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;

namespace Reclamation.Tests
{
    public sealed class BlightTerrainTests
    {
        private static BlightTerrain Layout()
        {
            var terrain = new BlightTerrain();
            terrain.Add(new Rect(-12, -.6f, 10.2f, 1.2f)); terrain.Add(new Rect(1.8f, -.6f, 10.2f, 1.2f));
            terrain.Add(new Rect(-12, -9.6f, 9.8f, 1.2f)); terrain.Add(new Rect(2.2f, -9.6f, 9.8f, 1.2f));
            return terrain;
        }
        [Test] public void WallsBlockSweptMovementAndSightButOpeningsRemainPassable()
        {
            var t = Layout(); Vector3 from = new Vector3(5, 0, -2), to = new Vector3(5, 0, 2);
            Assert.That(t.Clear(from, to, 0), Is.False);
            Assert.That(t.Move(from, to, .43f), Is.EqualTo(from), "A long movement step cannot tunnel.");
            Assert.That(t.Clear(new Vector3(0, 0, -2), new Vector3(0, 0, 2), .8f), Is.True, "Hulk fits main gateway.");
            Assert.That(t.Clear(new Vector3(13.5f, 0, -2), new Vector3(13.5f, 0, 2), .8f), Is.True);
            Assert.That(t.Clear(new Vector3(1.5f, 0, -2), new Vector3(1.5f, 0, 2), .43f), Is.False, "Actor radius matters.");
        }
        [TestCase(.43f)] [TestCase(.8f)] public void RoutingReachesGoalAcrossBothWalls(float radius)
        {
            var t = Layout(); Vector3 current = new Vector3(6, 0, 14), goal = new Vector3(-.7f, 0, -11.5f);
            for (int i = 0; i < 300 && Vector3.Distance(current, goal) > .05f; i++)
            {
                Vector3 waypoint = t.Next(current, goal, radius);
                Assert.That(Vector3.Distance(current, waypoint), Is.GreaterThan(.001f), "Route must make progress.");
                Vector3 next = Vector3.MoveTowards(current, waypoint, .25f);
                Assert.That(t.Clear(current, next, radius), Is.True); current = next;
            }
            Assert.That(Vector3.Distance(current, goal), Is.LessThan(.05f));
        }
        [Test] public void ClearRemovesAllCollisionAndRoutingConstraints()
        {
            var t = Layout(); t.Clear(); Vector3 goal = new Vector3(5, 0, 2);
            Assert.That(t.Count, Is.Zero); Assert.That(t.Next(new Vector3(5, 0, -2), goal, .8f), Is.EqualTo(goal));
        }
    }
}
