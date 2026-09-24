using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
namespace Reclamation.Tests
{
    public sealed class BlightMapProjectionTests
    {
        [Test] public void NorthIsUpAndWorldPositionsRoundTrip()
        {
            Rect map = new Rect(42, 67, 420, 602);
            Assert.That(BlightMapProjection.ToMap(map, new Vector3(-15, 0, 20)), Is.EqualTo(map.min));
            Assert.That(BlightMapProjection.ToMap(map, new Vector3(15, 0, -23)), Is.EqualTo(map.max));
            Vector3 point = new Vector3(-11, 0, 5);
            Assert.That(Vector3.Distance(point, BlightMapProjection.ToWorld(map, BlightMapProjection.ToMap(map, point))), Is.LessThan(.0001f));
        }
    }
}
