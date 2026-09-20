using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Outbreak
{
    public enum RefugeAssignment { None, Evacuating, Sheltered, Quarantined }

    public sealed class SafeZone : MonoBehaviour
    {
        [SerializeField] private Transform shelterEntrance;
        [SerializeField] private Transform quarantineEntrance;
        [SerializeField, Min(1)] private int shelterCapacity = 4;
        [SerializeField, Min(1)] private int quarantineCapacity = 2;
        private readonly Dictionary<OutbreakAgent, RefugeAssignment> assignments = new();
        [SerializeField] private PerimeterDefense perimeter;
        public PerimeterDefense Perimeter => perimeter;
        public void AttachPerimeter(PerimeterDefense defense) => perimeter = defense;

        public int ShelterCapacity => shelterCapacity;
        public int QuarantineCapacity => quarantineCapacity;
        public int ShelteredCount => Count(RefugeAssignment.Sheltered);
        public int QuarantinedCount => Count(RefugeAssignment.Quarantined);
        public int EvacuatingCount => Count(RefugeAssignment.Evacuating);
        public bool Breached { get; private set; }

        public void Configure(Transform shelter, Transform quarantine, int shelterBeds, int quarantineBeds)
        {
            shelterEntrance = shelter;
            quarantineEntrance = quarantine;
            shelterCapacity = Mathf.Max(1, shelterBeds);
            quarantineCapacity = Mathf.Max(1, quarantineBeds);
        }

        public RefugeAssignment GetAssignment(OutbreakAgent person) =>
            person != null && assignments.TryGetValue(person, out RefugeAssignment result) ? result : RefugeAssignment.None;

        public bool RequestEvacuation(OutbreakAgent person)
        {
            if (person == null || !person.gameObject.activeInHierarchy || person.Isolated || person.State == InfectionState.Turned ||
                person.State == InfectionState.Neutralized || GetAssignment(person) != RefugeAssignment.None) return false;
            if (person.VisibleSymptoms ? ReservedQuarantinePlaces >= quarantineCapacity :
                ReservedShelterPlaces >= shelterCapacity) return false;
            assignments[person] = RefugeAssignment.Evacuating;
            person.AssignSafeZone(this);
            return true;
        }

        public bool CancelEvacuation(OutbreakAgent person)
        {
            if (GetAssignment(person) != RefugeAssignment.Evacuating) return false;
            assignments.Remove(person);
            person.ClearSafeZone(this);
            return true;
        }

        public bool TryGetDestination(OutbreakAgent person, out Vector3 destination)
        {
            destination = transform.position;
            if (GetAssignment(person) != RefugeAssignment.Evacuating) return false;
            bool requiresQuarantine = person.VisibleSymptoms;
            if (requiresQuarantine)
            {
                if (ReservedQuarantinePlaces > quarantineCapacity || quarantineEntrance == null) return false;
                destination = quarantineEntrance.position;
            }
            else
            {
                if (ReservedShelterPlaces > shelterCapacity || shelterEntrance == null) return false;
                destination = shelterEntrance.position;
            }
            return true;
        }

        public bool TryAdmit(OutbreakAgent person)
        {
            if (GetAssignment(person) != RefugeAssignment.Evacuating) return false;
            RefugeAssignment destination = person.VisibleSymptoms
                ? RefugeAssignment.Quarantined : RefugeAssignment.Sheltered;
            int capacity = destination == RefugeAssignment.Quarantined ? quarantineCapacity : shelterCapacity;
            if (Count(destination) >= capacity) return false;
            int slot = Count(destination);
            assignments[person] = destination;
            person.CompleteAdmission(this, destination, SlotPosition(destination, slot));
            return true;
        }

        public bool TryTransferToQuarantine(OutbreakAgent person)
        {
            if (GetAssignment(person) != RefugeAssignment.Sheltered || !person.VisibleSymptoms ||
                QuarantinedCount >= quarantineCapacity) return false;
            int slot = QuarantinedCount;
            assignments[person] = RefugeAssignment.Quarantined;
            person.CompleteAdmission(this, RefugeAssignment.Quarantined,
                SlotPosition(RefugeAssignment.Quarantined, slot));
            return true;
        }

        private Vector3 SlotPosition(RefugeAssignment assignment, int slot)
        {
            Transform entrance = assignment == RefugeAssignment.Quarantined ? quarantineEntrance : shelterEntrance;
            return entrance.position + new Vector3((slot % 2) * 1.1f - 0.55f, 0, (slot / 2) * 1.1f);
        }

        private void Update()
        {
            var remove = new List<OutbreakAgent>();
            bool newBreach = false;
            foreach (var pair in assignments)
            {
                OutbreakAgent person = pair.Key;
                if (person == null || person.State == InfectionState.Neutralized)
                {
                    remove.Add(person);
                    continue;
                }
                if (pair.Value == RefugeAssignment.Sheltered && person.State == InfectionState.Turned)
                {
                    Breached = true;
                    newBreach = true;
                }
                else if (pair.Value == RefugeAssignment.Quarantined && person.State == InfectionState.Turned)
                    person.HoldInQuarantine();
            }
            if (newBreach)
                foreach (var pair in assignments)
                    if (pair.Value == RefugeAssignment.Sheltered && pair.Key != null)
                    {
                        pair.Key.ReleaseFromZone(this, "Refuge breached");
                        remove.Add(pair.Key);
                    }
            foreach (OutbreakAgent person in remove) assignments.Remove(person);
        }

        private int ReservedShelterPlaces => ShelteredCount + CountEvacuees(false);
        private int ReservedQuarantinePlaces => QuarantinedCount + CountEvacuees(true);

        private int CountEvacuees(bool symptomatic)
        {
            int count = 0;
            foreach (var pair in assignments)
                if (pair.Value == RefugeAssignment.Evacuating && pair.Key != null &&
                    pair.Key.VisibleSymptoms == symptomatic) count++;
            return count;
        }

        private int Count(RefugeAssignment assignment)
        {
            int count = 0;
            foreach (RefugeAssignment value in assignments.Values) if (value == assignment) count++;
            return count;
        }
    }
}
