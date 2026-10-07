using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Atlas
{
    public enum AtlasSize { Small, Medium }
    public enum AtlasResource { Food, Wood, Stone, Gold, Niter }
    public sealed class AtlasSite
    {
        public int id, owner = -1, footprint;
        public string name;
        public Vector2 point;
        public float elevation;
        public bool capital;
        public readonly List<AtlasDeposit> deposits = new List<AtlasDeposit>();
    }
    public sealed class AtlasDeposit { public AtlasResource kind; public Vector2 point; public int richness; }
    public sealed class AtlasFaction
    {
        public string name, culture, intent;
        public int family, alliance, capital, strength = 128;
        public Color color;
    }
    // Independent survey model: no dependency on combat, existing saves or the Founding scenario.
    public sealed class WorldAtlasModel
    {
        public AtlasSize Size { get; }
        public float Extent => Size == AtlasSize.Small ? 1600 : 3200;
        public string Name => Size == AtlasSize.Small ? "GREENWARD VALLEY" : "THE SUNDERED COAST";
        public readonly List<AtlasSite> sites = new List<AtlasSite>();
        public readonly List<AtlasFaction> factions = new List<AtlasFaction>();
        public readonly List<string> journal = new List<string>();
        public int Turns { get; private set; }
        public int Winner { get; private set; } = -1;
        public WorldAtlasModel(AtlasSize size, int slots)
        {
            if (slots != 2 && slots != 4 && slots != 8 && slots != 12) throw new ArgumentOutOfRangeException(nameof(slots));
            if (size == AtlasSize.Small && slots > 4) throw new ArgumentException("Small maps support 2 or 4 slots.");
            Size = size;
            int cols = size == AtlasSize.Small ? 6 : 10, rows = size == AtlasSize.Small ? 6 : 8;
            string[] prefixes={"Alder", "Grey", "Silver", "Thorn", "West", "Dawn", "Moss", "Raven", "High", "Salt"};
            for(int z=0;z<rows;z++) for(int x=0;x<cols;x++)
            {
                float px=Mathf.Lerp(-.40f, size==AtlasSize.Small ? .40f : .22f, x/(float)(cols-1))*Extent;
                float pz=Mathf.Lerp(-.40f,.40f,z/(float)(rows-1))*Extent;
                if(size==AtlasSize.Small && Mathf.Abs(px-RiverX(pz))<50) px += px<0 ? -55 : 55;
                var s=new AtlasSite { id=sites.Count, name=prefixes[x]+" "+new[]{"Hollow","Reach","Ford","Heights","Crossing","Meadow","Basin","Watch"}[z%8], point=new Vector2(px,pz) };
                s.elevation=Mathf.Max(8,RawHeight(px,pz)); sites.Add(s);
                for(int k=0;k<3;k++) s.deposits.Add(new AtlasDeposit{kind=(AtlasResource)k,point=s.point+new Vector2(Mathf.Cos(k*2.1f),Mathf.Sin(k*2.1f))*55,richness=3+(x+z+k)%3});
                if(s.id%3==1) s.deposits.Add(new AtlasDeposit{kind=AtlasResource.Gold,point=s.point+new Vector2(-65,40),richness=2+s.id%4});
                if(s.id%7==2) s.deposits.Add(new AtlasDeposit{kind=AtlasResource.Niter,point=s.point+new Vector2(40,-65),richness=2+s.id%3});
            }
            string[] names={"Your banner","House Valewood","The Blight Court","Ashen Host","House Dawnmere","Silver Compact","Rotbound Crown","Pale Legion","House Ironvale","Coastal League","Thorn Plague","Hollow King"};
            for(int i=0;i<slots;i++)
            {
                int family=slots==2 ? i : (i%4<2 ? 0 : 1);
                var f=new AtlasFaction{name=slots==2&&i==1?"Ashen Host":names[i],family=family,culture=family==0?"Human":i%2==0?"Blighted":"Undead",alliance=i,color=Color.HSVToRGB((.55f+i/(float)slots)%1,.65f,.85f),intent=i==0?"You choose where to settle":"Secure resources, then expand"};
                Vector2 target;
                if(slots<=4) target=new Vector2(i%2==0?-.34f:.20f,i<2?-.34f:.34f)*Extent;
                else { float angle=Mathf.PI*2*i/slots; target=new Vector2(-.09f+Mathf.Cos(angle)*.29f,Mathf.Sin(angle)*.35f)*Extent; }
                AtlasSite chosen=null; float best=float.MaxValue;
                foreach(var s in sites) if(s.owner==-1) {float d=Vector2.Distance(s.point,target);if(d<best){best=d;chosen=s;}}
                chosen.owner=i;chosen.capital=true;f.capital=chosen.id;factions.Add(f);
            }
            // Independent enclaves and hostile wilderness are not additional player slots.
            int tagged=0; foreach(var s in sites) if(s.owner==-1 && Vector2.Distance(s.point,Vector2.zero)<Extent*.28f && tagged<4) {s.owner=tagged<2?-2:-3;tagged++;}
            journal.Add("Neutral trade-enclave and hostile-lair sites are marked; their interactions are not active.");
        }
        public float RiverX(float z) => Mathf.Sin(z/Extent*8)*Extent*.07f;
        public float RawHeight(float x,float z)
        {
            float u=x/Extent,v=z/Extent;
            float noise=Mathf.PerlinNoise(u*6+17,v*6+31)*13;
            if(Size==AtlasSize.Small)
            {
                float bank=Mathf.Abs(x-RiverX(z));
                bool crossing=Mathf.Abs(z)<25 || Mathf.Abs(Mathf.Abs(z)-Extent*.25f)<25;
                if(bank<15 && !crossing) return -3;
                float hills=65*Mathf.Pow(Mathf.Abs(u)*2,3)+30*Mathf.Pow(Mathf.Abs(v)*2,4);
                return 7+noise+hills;
            }
            float shore=(u-.31f-Mathf.Sin(v*9)*.045f)*Extent;
            if(shore>0) return -Mathf.Min(30,shore*.3f);
            float ridge=155*Mathf.Exp(-Mathf.Pow((u+.26f)*8,2))*Mathf.Pow(.5f+.5f*Mathf.Sin(v*11+1),2);
            return Mathf.Max(2,7+noise+ridge+35*Mathf.Pow(Mathf.Abs(v)*2,3)-Mathf.Max(0,shore+80)*.13f);
        }
        public float Height(float x,float z)
        {
            float h=RawHeight(x,z);
            foreach(var s in sites) { float d=Vector2.Distance(s.point,new Vector2(x,z)); if(d<95) {float t=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(60,95,d)); h=Mathf.Lerp(h,s.elevation,t);}}
            return h;
        }
        public bool Walkable(Vector2 p)
        {
            if(Mathf.Abs(p.x)>Extent*.485f || Mathf.Abs(p.y)>Extent*.485f || Height(p.x,p.y)<1) return false;
            return Mathf.Abs(Height(p.x+3,p.y)-Height(p.x-3,p.y))<5 && Mathf.Abs(Height(p.x,p.y+3)-Height(p.x,p.y-3))<5;
        }
        public AtlasSite Nearest(Vector2 p) {AtlasSite best=null;float d=float.MaxValue;foreach(var s in sites){float v=Vector2.Distance(p,s.point);if(v<d){d=v;best=s;}}return best;}
        public bool Claim(int site,int faction)
        {
            if(site<0||site>=sites.Count||faction<0||faction>=factions.Count||sites[site].owner!=-1)return false;
            sites[site].owner=faction; return true;
        }
        public string Relation(int a,int b)
        {
            if(b==-1)return "Unclaimed"; if(b==-2)return "Neutral enclave";if(b==-3)return "Hostile lair";
            if(a==b)return "Your faction"; if(factions[a].alliance==factions[b].alliance)return "Allied";
            return factions[a].family==factions[b].family?"Neutral rival":"Hostile";
        }
        public int Holdings(int faction){int n=0;foreach(var s in sites)if(s.owner==faction)n++;return n;}
        public void AdvanceStrategy()
        {
            if(Winner>=0)return;Turns++;
            for(int i=1;i<factions.Count;i++)
            {
                var f=factions[i]; if(Holdings(i)==0)continue;
                if(Turns>=3)
                    for(int j=0;j<i;j++) if(factions[j].family==f.family && Holdings(j)>0 && f.alliance!=factions[j].alliance)
                    {f.alliance=factions[j].alliance;Log(f.name+" joins an affinity alliance with "+factions[j].name+".");break;}
                AtlasSite choice=null;float score=float.MinValue;
                foreach(var s in sites)
                {
                    if(s.owner==-2 || s.owner==-3 || s.owner==i || s.owner>=0 && factions[s.owner].alliance==f.alliance)continue;
                    float distance=float.MaxValue;foreach(var own in sites)if(own.owner==i)distance=Mathf.Min(distance,Vector2.Distance(s.point,own.point));
                    float candidate=100-distance/Extent*300+s.deposits.Count*4+(s.owner==-1?30:-40);
                    if(candidate>score){score=candidate;choice=s;}
                }
                if(choice==null)continue;
                if(choice.owner==-1){choice.owner=i;f.intent="Expanding to "+choice.name;Log(f.name+" establishes a camp at "+choice.name+".");}
                else
                {
                    int defender=choice.owner;
                    // Abstract territorial contest, explicitly separate from the combat simulation.
                    int support=0;for(int j=0;j<factions.Count;j++)if(factions[j].alliance==f.alliance)support+=Holdings(j)*8;
                    if(f.strength+support>=factions[defender].strength+Holdings(defender)*8+16)
                    {choice.owner=i;f.intent="Contesting "+choice.name;Log(f.name+" takes "+choice.name+" in the abstract contest.");}
                    else {f.strength=Mathf.Min(192,f.strength+8);f.intent="Muster strength before contesting "+choice.name;}
                }
            }
            int alliance=-1;bool same=true;for(int i=0;i<factions.Count;i++)if(Holdings(i)>0){if(alliance<0)alliance=factions[i].alliance;else if(alliance!=factions[i].alliance)same=false;}
            if(same){Winner=alliance;Log("Alliance "+alliance+" controls all faction-held territory.");}
        }
        private void Log(string text){journal.Insert(0,text);if(journal.Count>8)journal.RemoveAt(8);}
    }
}
