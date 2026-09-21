using NUnit.Framework;
using Reclamation.Neighborhood;
using Reclamation.Outbreak;
using UnityEngine;

namespace Reclamation.Tests
{
    public sealed class CitizenOverheadTests
    {
        [Test] public void CenterIsBrightAndEdgesRemainTranslucent()
        {
            Assert.That(CitizenOverheadDisplay.FocusOpacity(new Vector2(0.5f, 0.5f)), Is.EqualTo(0.92f).Within(0.001f));
            Assert.That(CitizenOverheadDisplay.FocusOpacity(Vector2.zero), Is.EqualTo(0.32f).Within(0.001f));
        }

        [Test] public void HiddenInfectionLooksLikeHealthyCondition()
        {
            Assert.That(CitizenOverheadDisplay.PublicCondition(InfectionState.Exposed),
                Is.EqualTo(CitizenOverheadDisplay.PublicCondition(InfectionState.Healthy)));
            Assert.That(CitizenOverheadDisplay.PublicCondition(InfectionState.Symptomatic), Is.Not.Empty);
        }
    }
}
