using NUnit.Framework;
using Reclamation.AI;

namespace Reclamation.Tests
{
    public sealed class HaulJobScorerTests
    {
        [Test]
        public void HigherPolicyPriorityCanOutweighTravelDistance()
        {
            float nearbyNormalJob = HaulJobScorer.Score(1, 2f);
            float distantPriorityJob = HaulJobScorer.Score(2, 40f);

            Assert.That(distantPriorityJob, Is.GreaterThan(nearbyNormalJob));
        }

        [Test]
        public void NearerJobWinsWhenPrioritiesMatch()
        {
            Assert.That(
                HaulJobScorer.Score(1, 3f),
                Is.GreaterThan(HaulJobScorer.Score(1, 12f)));
        }
    }
}
