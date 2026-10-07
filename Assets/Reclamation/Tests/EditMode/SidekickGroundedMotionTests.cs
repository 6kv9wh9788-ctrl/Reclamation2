using NUnit.Framework;
using UnityEngine;
using Reclamation.Blight;

public sealed class SidekickGroundedMotionTests
{
    [Test]
    public void WalkingFootRemainsOnGroundDuringStanceAndClearsItDuringSwing()
    {
        for (int i = 0; i <= 59; i++)
            Assert.That(SidekickDuelBridge.GroundedStep(i / 100f, .43f, .065f).y, Is.EqualTo(0).Within(.00001f));
        Assert.That(SidekickDuelBridge.GroundedStep(.8f, .43f, .065f).y, Is.GreaterThan(.06f));
        Assert.That(Vector3.Distance(SidekickDuelBridge.GroundedStep(.999999f, .43f, .065f),
            SidekickDuelBridge.GroundedStep(0, .43f, .065f)), Is.LessThan(.0001f));
        Assert.That(Vector3.Distance(SidekickDuelBridge.GroundedStep(.599999f, .43f, .065f),
            SidekickDuelBridge.GroundedStep(.600001f, .43f, .065f)), Is.LessThan(.0001f));
    }
    [Test]
    public void SwordContactMatchesDamageBoundaryWithoutPoseJump()
    {
        SidekickDuelBridge.AttackWeights(DuelAction.Windup, 1, out float beforeLoad, out float beforeStrike);
        SidekickDuelBridge.AttackWeights(DuelAction.Recovery, 0, out float afterLoad, out float afterStrike);
        Assert.That(beforeStrike, Is.EqualTo(1).Within(.00001f));
        Assert.That(afterStrike, Is.EqualTo(beforeStrike).Within(.00001f));
        Assert.That(afterLoad, Is.EqualTo(beforeLoad).Within(.00001f));
        SidekickDuelBridge.AttackWeights(DuelAction.Recovery, 1, out float load, out float strike);
        Assert.That(load + strike, Is.EqualTo(0).Within(.00001f));
    }
}
