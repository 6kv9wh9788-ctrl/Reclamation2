using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
namespace Reclamation.Tests
{
    public sealed class BlightCompanyFogTests
    {
        [Test] public void VisibilityExpiresButExplorationRemainsUntilReset()
        {
            var grid = new CompanyFogGrid(); Vector3 point = grid.Center(5, 5);
            grid.Observe(point, 2, (a, b) => true);
            Assert.That(grid.Visible(5, 5), Is.True); Assert.That(grid.Explored(5, 5), Is.True);
            Assert.That(grid.Explored(20, 20), Is.False);
            grid.BeginObservation(); Assert.That(grid.Visible(5, 5), Is.False);
            Assert.That(grid.Explored(5, 5), Is.True);
            grid.Clear(); Assert.That(grid.Explored(5, 5), Is.False);
        }
        [Test] public void ObserversShareSightButWallsBlockIt()
        {
            var grid = new CompanyFogGrid(); var terrain = new BlightTerrain();
            terrain.Add(new Rect(-1, -10, 2, 20));
            grid.Observe(new Vector3(-3, 0, 0), 8, (a, b) => terrain.Clear(a, b, 0));
            Assert.That(grid.Visible(18, 22), Is.False);
            Assert.That(grid.Visible(11, 22), Is.True);
            grid.Observe(new Vector3(3, 0, 0), 8, (a, b) => terrain.Clear(a, b, 0));
            Assert.That(grid.Visible(18, 22), Is.True);
            Assert.That(grid.Visible(11, 22), Is.True);
        }
    }
}
