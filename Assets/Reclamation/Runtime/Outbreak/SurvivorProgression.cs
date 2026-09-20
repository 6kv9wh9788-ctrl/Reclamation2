using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Outbreak
{
    public enum SurvivorRank { Civilian, Trained, Veteran }

    [Serializable]
    public sealed class ProgressionRecord
    {
        public string id;
        public int experience;
        public List<string> completedObjectives = new();
    }

    [Serializable]
    internal sealed class ProgressionDatabase
    {
        public List<ProgressionRecord> survivors = new();
    }

    public static class ProgressionRules
    {
        public const int TrainedExperience = 100;
        public const int VeteranExperience = 300;
        public static SurvivorRank Rank(int experience) => experience >= VeteranExperience ? SurvivorRank.Veteran :
            experience >= TrainedExperience ? SurvivorRank.Trained : SurvivorRank.Civilian;
        public static int NextRankAt(int experience) => experience < TrainedExperience ? TrainedExperience :
            experience < VeteranExperience ? VeteranExperience : VeteranExperience;

        public static CombatAttributes Attributes(int experience)
        {
            var result = CombatAttributes.Civilian();
            float trained = Mathf.Clamp01(experience / (float)TrainedExperience);
            float veteran = Mathf.Clamp01((experience - TrainedExperience) /
                (float)(VeteranExperience - TrainedExperience));
            // At 300 XP this exactly matches the established veteran preset. Values
            // interpolate rather than jumping, and never exceed conservative caps.
            result.strength = Mathf.Min(12, result.strength + Mathf.RoundToInt(2 * trained + 6 * veteran));
            result.dexterity = Mathf.Min(9, result.dexterity + Mathf.RoundToInt(2 * trained + 3 * veteran));
            result.agility = Mathf.Min(9, result.agility + Mathf.RoundToInt(1 * trained + 4 * veteran));
            result.speed = Mathf.Min(8, result.speed + Mathf.RoundToInt(1 * trained + 3 * veteran));
            result.intelligence = Mathf.Min(9, result.intelligence + Mathf.RoundToInt(2 * trained + 3 * veteran));
            result.endurance = Mathf.Min(8, result.endurance + Mathf.RoundToInt(2 * trained + 2 * veteran));
            return result;
        }
    }

    public static class SurvivorProgressionStore
    {
        private const string Key = "Reclamation.SurvivorProgression.v1";

        public static int Load(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !PlayerPrefs.HasKey(Key)) return 0;
            var database = JsonUtility.FromJson<ProgressionDatabase>(PlayerPrefs.GetString(Key));
            if (database == null || database.survivors == null) return 0;
            foreach (var record in database.survivors)
                if (record != null && record.id == id) return Mathf.Max(0, record.experience);
            return 0;
        }

        public static void Save(string id, int experience)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            ProgressionDatabase database = PlayerPrefs.HasKey(Key)
                ? JsonUtility.FromJson<ProgressionDatabase>(PlayerPrefs.GetString(Key)) : null;
            database ??= new ProgressionDatabase();
            database.survivors ??= new List<ProgressionRecord>();
            ProgressionRecord found = null;
            foreach (var record in database.survivors) if (record != null && record.id == id) { found = record; break; }
            if (found == null) { found = new ProgressionRecord { id = id }; database.survivors.Add(found); }
            found.experience = Mathf.Max(0, experience);
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(database)); PlayerPrefs.Save();
        }

        public static bool CompleteObjective(string id, string objective)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(objective)) return false;
            ProgressionDatabase database = PlayerPrefs.HasKey(Key)
                ? JsonUtility.FromJson<ProgressionDatabase>(PlayerPrefs.GetString(Key)) : null;
            database ??= new ProgressionDatabase(); database.survivors ??= new List<ProgressionRecord>();
            ProgressionRecord found = null;
            foreach (var record in database.survivors) if (record != null && record.id == id) { found = record; break; }
            if (found == null) { found = new ProgressionRecord { id = id }; database.survivors.Add(found); }
            found.completedObjectives ??= new List<string>();
            if (found.completedObjectives.Contains(objective)) return false;
            found.completedObjectives.Add(objective);
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(database)); PlayerPrefs.Save(); return true;
        }

        public static void ResetAll() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); }
    }
}
