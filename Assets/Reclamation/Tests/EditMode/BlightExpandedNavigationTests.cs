using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
namespace Reclamation.Tests
{
    public sealed class BlightExpandedNavigationTests
    {
        [Test] public void RouteCanUseCornersOutsideOldLabBounds()
        {
            var terrain = new BlightTerrain { Bounds = new Rect(-33,-50.6f,66,94.6f) };
            terrain.Add(new Rect(14,31,5,5));
            Vector3 current = new Vector3(10,0,33), goal = new Vector3(23,0,33);
            for (int i = 0; i < 8 && Vector3.Distance(current,goal) > .1f; i++)
            {
                Vector3 next = terrain.Next(current,goal,.43f);
                Assert.That(Vector3.Distance(current,next), Is.GreaterThan(.01f));
                Assert.That(terrain.Clear(current,next,.43f), Is.True); current = next;
            }
            Assert.That(Vector3.Distance(current,goal), Is.LessThan(.1f));
        }
    }
}
