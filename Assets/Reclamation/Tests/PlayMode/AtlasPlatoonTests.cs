using System.Collections;
using System.Linq;
using NUnit.Framework;
using Reclamation.Atlas;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class AtlasPlatoonTests
    {
        [Test] public void StableLeadersRotateDutiesAndReportsGateDelegation()
        {
            var company = new AtlasCompanySchedule(42);
            var same = new AtlasCompanySchedule(42);
            Assert.That(company.Platoons.Select(p => p.Name).Distinct().Count(), Is.EqualTo(4));
            CollectionAssert.AreEqual(company.Platoons.Select(p => p.Name), same.Platoons.Select(p => p.Name));
            for (int watch = 0; watch < 4; watch++)
            {
                company.UpdateWatch(watch * 6);
                Assert.That(company.Platoons.Select(p => p.Duty).Distinct().Count(), Is.EqualTo(4));
                foreach (var p in company.Platoons) Assert.That(p.LeaderGuardIndex, Is.EqualTo(1 + p.Id * 6));
                company.BeginContact();
                Assert.That(company.DelegateResponse(), Is.False);
                var farm = company.Platoons.Single(p => p.Duty == AtlasGuardDuty.FarmPatrol);
                var reserve = company.Platoons.Single(p => p.Duty == AtlasGuardDuty.Reserve);
                company.ObserveContact(reserve.Id); Assert.That(company.ReportingPlatoon, Is.EqualTo(-1));
                company.ObserveContact(farm.Id); Assert.That(company.ReportDelivered, Is.False);
                Assert.That(company.Platoons.All(p => p.Order == AtlasPlatoonOrder.Routine), Is.True);
                Assert.That(company.DelegateResponse(), Is.True);
                Assert.That(company.RespondingPlatoon, Is.EqualTo(reserve.Id));
                Assert.That(company.DelegateResponse(), Is.False);
                Assert.That(company.Platoons.Count(p => p.Order == AtlasPlatoonOrder.SecureApproach), Is.EqualTo(1));
                company.ReportSupport(2); Assert.That(reserve.Report, Does.Contain("moving"));
                company.ReportSupport(3); int sent = reserve.ReportsSent;
                company.ReportSupport(6); Assert.That(reserve.ReportsSent, Is.EqualTo(sent));
                company.CompleteContact(true); Assert.That(company.LatestReport, Does.Contain("Approach secured"));
                Assert.That(company.Platoons.All(p => p.Order == AtlasPlatoonOrder.Routine), Is.True);
            }
            Assert.That(company.Reports.Count(), Is.EqualTo(6), "History must stay bounded");
        }

        [UnityTest] public IEnumerator LeadersReportRespondAndResumeOnAllPresets()
        {
            var go = new GameObject("Platoon integration"); var demo = go.AddComponent<WorldAtlasDemo>(); yield return null;
            try
            {
                demo.AutomaticClock = false;
                foreach (var setting in new[] { (AtlasSize.Small, 4), (AtlasSize.Medium, 8), (AtlasSize.Medium, 12) })
                {
                    demo.LoadMap(setting.Item1, setting.Item2); yield return null; demo.VisitCommander(); demo.SetHour(9);
                    var company = demo.ActiveCompany;
                    Assert.That(demo.ActiveGuardCount, Is.EqualTo(25));
                    Assert.That(company.Platoons.Count, Is.EqualTo(4));
                    foreach (var p in company.Platoons) Assert.That(demo.GuardDuty(p.Id * 6), Is.EqualTo(p.Duty));
                    Assert.That(demo.BeginScoutEncounter(), Is.True);
                    for (int i = 0; i < 1500 && demo.ScoutPhase == ScoutEncounterPhase.Unseen; i++) demo.TickVillage(.2f);
                    Assert.That(demo.ScoutPhase, Is.EqualTo(ScoutEncounterPhase.Reporting));
                    Assert.That(company.ReportingPlatoon, Is.GreaterThanOrEqualTo(0));
                    Assert.That(company.ReportDelivered, Is.False);
                    Assert.That(demo.ResponseGuidePlatoon,Is.EqualTo(-1),"No response identity before delegation");
                    demo.SurveyPaused = true; demo.TickVillage(100);
                    Assert.That(company.ReportDelivered, Is.False); demo.SurveyPaused = false;
                    for (int i = 0; i < 20 && !company.ReportDelivered; i++) demo.TickVillage(.2f);
                    Assert.That(company.ReportDelivered, Is.True);
                    var reserve = company.Platoons[company.RespondingPlatoon];
                    Assert.That(demo.ResponseGuidePlatoon,Is.EqualTo(reserve.Id));
                    Assert.That(demo.ResponseGuidePlatoon,Is.Not.EqualTo(company.ReportingPlatoon));
                    Assert.That(demo.ResponseGuideName,Is.EqualTo(reserve.Name));
                    Assert.That(demo.ResponseGuideHighlighted,Is.True);
                    Vector3 leaderStart = demo.GuardPosition(reserve.LeaderGuardIndex);
                    demo.SetHour(18); Assert.That(reserve.Duty, Is.EqualTo(AtlasGuardDuty.Reserve), "Incident pins the watch");
                    for (int i = 0; i < 2500 && demo.ScoutPhase != ScoutEncounterPhase.Secured && demo.ScoutPhase != ScoutEncounterPhase.Escaped; i++) demo.TickVillage(.2f);
                    Assert.That(demo.ScoutPhase, Is.EqualTo(ScoutEncounterPhase.Secured));
                    Assert.That(Vector3.Distance(leaderStart, demo.GuardPosition(reserve.LeaderGuardIndex)), Is.GreaterThan(10));
                    Assert.That(reserve.ReportsSent, Is.EqualTo(2), "In position and outcome report");
                    Assert.That(company.LatestReport, Does.Contain("Approach secured"));
                    Assert.That(company.Platoons.All(p => p.Order == AtlasPlatoonOrder.Routine), Is.True);
                    demo.TickVillage(.1f);
                    foreach (var p in company.Platoons) Assert.That(p.Duty, Is.EqualTo(AtlasCompanySchedule.Duty(18, p.Id * 6)));
                    demo.LoadMap(setting.Item1, setting.Item2); yield return null; demo.VisitCommander();
                    Assert.That(demo.ActiveCompany.Reports.Count(), Is.Zero);
                    Assert.That(demo.ResponseGuidePlatoon,Is.EqualTo(-1));
                    Assert.That(demo.ResponseGuideHighlighted,Is.False);
                    Assert.That(demo.ActiveCompany.ReportDelivered, Is.False);
                }
            }
            finally { Object.Destroy(go); }
            yield return null;
        }
    }
}
