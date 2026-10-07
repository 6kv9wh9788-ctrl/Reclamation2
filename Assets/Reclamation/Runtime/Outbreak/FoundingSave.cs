using System;
using System.IO;
using UnityEngine;

namespace Reclamation.Blight
{
    [Serializable] public sealed class FoundingSave
    {
        public int version;
        public bool raidCompleted;
        public bool hasCampaign;
        public DefenseCampaign campaign;
        public FoundingVillage.Snapshot village;
        public Fighter[] fighters;
        public Vector3[] workers;
        public bool[] carrying;
        public float[] gathering;
        public int directive;
        public bool scoutReturning, scoutReport;
        public string report;
        public float clock, simulationTime;
        [Serializable] public sealed class Fighter
        {
            public string name;
            public float health;
            public Vector3 position, forward;
            public int weapon;
        }
        private static bool Number(float n, float min, float max) => !float.IsNaN(n) && !float.IsInfinity(n) && n >= min && n <= max;
        private static bool Position(Vector3 p) => Number(p.x, -15, 15) && Number(p.y, -.1f, .1f) && Number(p.z, -23, 20);
        public void Validate()
        {
            if (version != 1 || village == null || fighters == null || fighters.Length != 12 || workers == null || workers.Length != 2 || carrying == null || carrying.Length != 2 || gathering == null || gathering.Length != 2 || directive < 0 || directive > 3 || report == null || report.Length > 1024 || !Number(clock, 0, 10000000) || !Number(simulationTime, 0, 10000000)) throw new InvalidDataException("Unsupported or incomplete village save.");
            FoundingVillage.FromSnapshot(village);
            if(!hasCampaign)campaign=null;
            if(hasCampaign){if(campaign==null)throw new InvalidDataException("Missing defense progression.");campaign.Validate();if(!village.storehouse||!village.watchpost||campaign.completed>0&&!raidCompleted)throw new InvalidDataException("Defense campaign requires a developed village and completed outcome.");}
            if (raidCompleted && (!village.storehouse || !village.watchpost)) throw new InvalidDataException("Raid completion without a developed village.");
            if (directive == 3 && !village.patrolDoctrine) throw new InvalidDataException("Patrol duty without research.");
            var names = new System.Collections.Generic.HashSet<string>();
            foreach (var f in fighters)
                if (f == null || string.IsNullOrEmpty(f.name) || !names.Add(f.name) || !Number(f.health, 0, 100) || !Position(f.position) || !Number(f.forward.sqrMagnitude, .9f, 1.1f) || !Number(f.forward.y, -.01f, .01f) || f.weapon < 0 || f.weapon > 2) throw new InvalidDataException("Invalid fighter state.");
            if (fighters[0].health <= 0) throw new InvalidDataException("The saved player must be alive.");
            for (int i = 0; i < 2; i++) if (!Position(workers[i]) || !Number(gathering[i], 0, 3)) throw new InvalidDataException("Invalid worker state.");
        }
        public static bool TryRead(string path, out FoundingSave save)
        {
            save = null;
            try
            {
                if (!File.Exists(path) || new FileInfo(path).Length > 1000000) return false;
                save = JsonUtility.FromJson<FoundingSave>(File.ReadAllText(path));
                if (save == null) return false; save.Validate(); return true;
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is InvalidOperationException)
            { save = null; return false; }
        }
        public static FoundingSave Read(string path, out bool backup)
        {
            backup = false;
            if (TryRead(path, out var save)) return save;
            if (TryRead(path + ".bak", out save)) { backup = true; return save; }
            throw new InvalidDataException("No valid village save or backup was found. Current play has not changed.");
        }
        public void Write(string path)
        {
            Validate(); string directory = Path.GetDirectoryName(Path.GetFullPath(path)); Directory.CreateDirectory(directory);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream)) { writer.Write(JsonUtility.ToJson(this, true)); writer.Flush(); stream.Flush(true); }
                if (File.Exists(path)) File.Replace(temporary, path, TryRead(path, out _) ? path + ".bak" : null);
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
