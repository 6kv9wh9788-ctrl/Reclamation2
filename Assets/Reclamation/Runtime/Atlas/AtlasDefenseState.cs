using System;
using System.IO;
using Reclamation.Blight;
using UnityEngine;

namespace Reclamation.Atlas
{
    [Serializable] public sealed class AtlasDefenseState
    {
        public int version = 1, size, site, seed, food = 10;
        public DefenseCampaign progression = new DefenseCampaign();
        public float[] health = new float[26];
        public AtlasDefenseState() { for (int i=0;i<health.Length;i++) health[i]=100; }
        public void Validate(WorldAtlasModel model, int home, int layoutSeed)
        {
            if(version!=1||size!=(int)model.Size||site!=home||seed!=layoutSeed||food<0||food>100||health==null||health.Length!=26||progression==null)
                throw new InvalidDataException("Checkpoint does not match this map and company.");
            progression.Validate();
            foreach(float value in health)if(float.IsNaN(value)||float.IsInfinity(value)||value<0||value>100)throw new InvalidDataException("Invalid company health.");
            if(health[0]<=0)throw new InvalidDataException("Cannot resume a defeated player.");
        }
        public void Write(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                using(var writer=new StreamWriter(stream)){writer.Write(JsonUtility.ToJson(this,true));writer.Flush();stream.Flush(true);}
                if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
            }
            finally { if(File.Exists(temp))File.Delete(temp); }
        }
    }
}
