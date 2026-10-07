using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Blight
{
    // Direct sight reports only: no relay chains or hidden-target references.
    public sealed class CrowdCommunication
    {
        private readonly CrowdBattleSimulation simulation;
        private readonly float[] nextReport;
        private readonly List<int> recipients=new List<int>(32);
        public long Broadcasts, Delivered, CandidateChecks;
        public CrowdCommunication(CrowdBattleSimulation simulation)
        { this.simulation=simulation;nextReport=new float[simulation.Units.Length]; }
        public void Forget(int unit) { nextReport[unit]=0; }
        public void ReportSight(int source)
        {
            var perception=simulation.Perception;var sight=perception.States[source];
            if(sight.visibleTarget<0 || sight.observedAt<perception.Time-.001f || !simulation.Units[source].fighter.Alive || perception.Time<nextReport[source])return;
            nextReport[source]=perception.Time+1;Broadcasts++;
            simulation.LocalAllies(source,recipients,this);
            foreach(int receiver in recipients)
                if(perception.ReceiveReport(receiver,sight.knownPosition,perception.Time))Delivered++;
        }
    }
}
