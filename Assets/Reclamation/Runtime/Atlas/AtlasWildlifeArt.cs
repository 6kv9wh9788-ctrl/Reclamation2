using UnityEngine;
namespace Reclamation.Atlas
{
    public sealed partial class AtlasArtKit
    {
        private void BuildWildlifeAssets()
        {
            Color fur=new Color(.42f,.29f,.18f),cream=new Color(.76f,.67f,.51f),deer=new Color(.48f,.27f,.13f),feather=new Color(.29f,.34f,.38f);
            Make("RabbitBody",b=>{b.Oval(new Vector3(0,.25f,0),new Vector3(.17f,.20f,.29f),fur,8,5);b.Oval(new Vector3(0,.3f,-.27f),Vector3.one*.095f,cream,8,4);});
            Make("RabbitHead",b=>{b.Oval(Vector3.zero,new Vector3(.12f,.12f,.14f),fur,8,5);foreach(float s in new[]{-1f,1f}){b.Oval(new Vector3(s*.065f,.18f,-.02f),new Vector3(.04f,.20f,.045f),fur,6,4);b.Oval(new Vector3(s*.065f,.19f,.02f),new Vector3(.022f,.145f,.012f),cream,6,4);b.Oval(new Vector3(s*.101f,.025f,.07f),Vector3.one*.019f,Dark,6,3);}b.Oval(new Vector3(0,-.035f,.13f),new Vector3(.045f,.035f,.05f),cream,6,4);});
            Make("RabbitLeg",b=>b.Oval(new Vector3(0,-.055f,.03f),new Vector3(.075f,.075f,.13f),fur,6,4));
            Make("DeerBody",b=>{b.Oval(new Vector3(0,1.05f,0),new Vector3(.29f,.36f,.61f),deer,10,6);b.Oval(new Vector3(0,.93f,.08f),new Vector3(.24f,.22f,.50f),cream,8,4);b.Oval(new Vector3(0,1.24f,-.55f),new Vector3(.10f,.12f,.20f),cream,6,4);});
            Make("DeerHead",b=>{b.Oval(new Vector3(0,.18f,.07f),new Vector3(.15f,.36f,.18f),deer,8,5);b.Oval(new Vector3(0,.48f,.19f),new Vector3(.15f,.17f,.29f),deer,8,5);b.Oval(new Vector3(0,.42f,.41f),new Vector3(.09f,.075f,.09f),Dark,6,4);foreach(float s in new[]{-1f,1f}){b.Oval(new Vector3(s*.19f,.60f,.08f),new Vector3(.15f,.075f,.065f),deer,6,4);b.Oval(new Vector3(s*.133f,.52f,.25f),Vector3.one*.024f,Dark,6,3);}});
            Make("Antlers",b=>{foreach(float s in new[]{-1f,1f}){b.Beam(new Vector3(s*.08f,.58f,.1f),new Vector3(s*.22f,.96f,-.04f),.033f,cream);b.Beam(new Vector3(s*.17f,.82f,0),new Vector3(s*.32f,.94f,.09f),.025f,cream);b.Beam(new Vector3(s*.12f,.72f,.05f),new Vector3(s*.08f,.85f,.21f),.024f,cream);}});
            Make("DeerLeg",b=>{b.Round(new Vector3(0,-.35f,0),new Vector3(.055f,.7f,.06f),deer,6,default,.7f);b.Oval(new Vector3(0,-.74f,.03f),new Vector3(.07f,.08f,.105f),Dark,6,4);});
            Make("BirdBody",b=>{b.Oval(new Vector3(0,.13f,0),new Vector3(.095f,.10f,.17f),feather,8,4);b.Oval(new Vector3(0,.21f,.12f),new Vector3(.075f,.07f,.08f),feather,6,4);b.Round(new Vector3(0,.2f,.21f),new Vector3(.025f,.11f,.025f),Straw,5,Quaternion.Euler(90,0,0),0);foreach(float s in new[]{-1f,1f})b.Oval(new Vector3(s*.06f,.235f,.15f),Vector3.one*.014f,Dark,5,3);b.Box(new Vector3(0,.12f,-.20f),new Vector3(.10f,.025f,.16f),feather);});
            Make("BirdWing",b=>b.Oval(new Vector3(.13f,0,-.03f),new Vector3(.18f,.018f,.115f),feather,6,3));
        }
    }
}
