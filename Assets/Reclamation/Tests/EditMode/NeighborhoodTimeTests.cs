using System;
using NUnit.Framework;
using Reclamation.Neighborhood;

namespace Reclamation.Tests
{
    public sealed class NeighborhoodTimeTests
    {
        [Test] public void MidnightAdvancesDayAndWrapsClock()
        {
            var time = new DayTime(1439);
            time.Advance(1, 4, 1);
            Assert.That(time.Day, Is.EqualTo(2));
            Assert.That(time.MinuteOfDay, Is.EqualTo(3));
        }
        [Test] public void ZeroSpeedPreservesTime()
        {
            var time = new DayTime(465);
            time.Advance(100, 4, 0);
            Assert.That(time.TotalMinutes, Is.EqualTo(465));
        }
        [Test] public void AccelerationIsProportional()
        {
            var time = new DayTime(0);
            time.Advance(10, 4, 12);
            Assert.That(time.MinuteOfDay, Is.EqualTo(480));
        }
        [Test] public void InvalidTimeInputsAreRejected()
        {
            var time = new DayTime(0);
            Assert.Throws<ArgumentOutOfRangeException>(() => time.Advance(-1, 4, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => time.Advance(double.NaN, 4, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DayTime(double.PositiveInfinity));
        }
        [Test] public void ScheduleChangesAtDefinedBoundaries()
        {
            Assert.That(DailyRoutine.Resolve(479, 0), Is.EqualTo(RoutineDestination.Home));
            Assert.That(DailyRoutine.Resolve(480, 0), Is.EqualTo(RoutineDestination.Cafe));
            Assert.That(DailyRoutine.Resolve(600, 0), Is.EqualTo(RoutineDestination.Home));
            Assert.That(DailyRoutine.Resolve(720, 0), Is.EqualTo(RoutineDestination.Park));
            Assert.That(DailyRoutine.Resolve(960, 0), Is.EqualTo(RoutineDestination.Cafe));
            Assert.That(DailyRoutine.Resolve(1080, 0), Is.EqualTo(RoutineDestination.Home));
        }
        [Test] public void StaggerDelaysDeparturesAndRepeatsDaily()
        {
            Assert.That(DailyRoutine.Resolve(480, 60), Is.EqualTo(RoutineDestination.Home));
            Assert.That(DailyRoutine.Resolve(540, 60), Is.EqualTo(RoutineDestination.Cafe));
            Assert.That(DailyRoutine.Resolve(1980, 60), Is.EqualTo(RoutineDestination.Cafe));
            Assert.That(DailyRoutine.Resolve(0, 60), Is.EqualTo(RoutineDestination.Home));
        }
    }
}
