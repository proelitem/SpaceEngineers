using System.Collections.Generic;
using UnityEngine;

namespace VoidForge.Economy
{
    // Original compact industrial geometry for the four formerly missing functional blocks.
    public static class EconomyBlockModels
    {
        static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        public static Material Material(string id,Color color)
        {
            if(materials.TryGetValue(id,out var existing)&&existing!=null)return existing;
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name="VoidForge "+id;m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.35f);materials[id]=m;return m;
        }
        static void Part(GameObject root,string name,Vector3 position,Vector3 scale,Material mat,PrimitiveType type=PrimitiveType.Cube)
        {
            var p=GameObject.CreatePrimitive(type);p.name=name;p.transform.SetParent(root.transform,false);p.transform.localPosition=position;p.transform.localScale=scale;p.GetComponent<Renderer>().sharedMaterial=mat;
            Object.DestroyImmediate(p.GetComponent<Collider>());
        }
        public static GameObject Create(string id,Vector3Int size)
        {
            var root=new GameObject("VF "+id+" 1m");var centre=((Vector3)size-Vector3.one)*.5f;
            var ivory=Material("ivory",new Color(.78f,.75f,.66f));var dark=Material("graphite",new Color(.055f,.085f,.09f));var amber=Material("amber",new Color(1,.47f,.035f));var cyan=Material("cyan",new Color(.05f,.6f,.7f));
            if(id=="SolarPanel")
            {
                Part(root,"Mount",new Vector3(0,-.3f,0),new Vector3(.2f,.4f,.2f),dark);
                Part(root,"Photovoltaic backing",new Vector3(0,0,0),new Vector3(.94f,.08f,.94f),ivory);
                for(int x=0;x<3;x++)for(int z=0;z<3;z++)Part(root,"Photovoltaic cell",new Vector3((x-1)*.29f,.052f,(z-1)*.29f),new Vector3(.26f,.025f,.26f),Material("solar",new Color(.015f,.055f,.19f)));
            }
            else
            {
                Part(root,"Rear shell",centre+new Vector3(0,0,.18f),new Vector3(size.x-.08f,size.y-.08f,size.z-.44f),ivory);
                Part(root,"Left frame",centre+new Vector3(-size.x*.44f,0,0),new Vector3(.1f,size.y-.08f,.9f),ivory);
                Part(root,"Right frame",centre+new Vector3(size.x*.44f,0,0),new Vector3(.1f,size.y-.08f,.9f),ivory);
                Part(root,"Front service recess",centre+new Vector3(0,0,-.12f),new Vector3(size.x*.8f,size.y*.78f,.03f),dark);
                Part(root,"Top rail",centre+new Vector3(0,size.y*.39f,-.46f),new Vector3(size.x*.76f,.075f,.06f),amber);
                if(id=="Refinery")
                {
                    for(int i=0;i<2;i++)Part(root,"Recessed refining tank",centre+new Vector3((i-.5f)*.34f,0,-.33f),new Vector3(.25f,.63f,.25f),amber,PrimitiveType.Cylinder);
                    Part(root,"Feed hatch",centre+new Vector3(0,.68f,-.46f),new Vector3(.55f,.18f,.04f),cyan);
                }
                else if(id=="Assembler")
                {
                    Part(root,"Enclosed working bay",centre+new Vector3(0,0,-.15f),new Vector3(1.35f,.5f,.05f),dark);
                    Part(root,"Industrial arm pedestal",centre+new Vector3(-.4f,-.17f,-.34f),new Vector3(.18f,.15f,.16f),amber);
                    Part(root,"Industrial arm",centre+new Vector3(-.2f,.02f,-.34f),new Vector3(.5f,.12f,.14f),ivory);
                    Part(root,"Empty control screen",centre+new Vector3(.62f,.04f,-.46f),new Vector3(.25f,.28f,.03f),cyan);
                }
                else if(id=="Cargo")
                {
                    Part(root,"Flush cargo hatch",new Vector3(0,0,-.45f),new Vector3(.7f,.7f,.05f),ivory);
                    Part(root,"Cargo locking bar",new Vector3(0,0,-.465f),new Vector3(.12f,.42f,.06f),amber);
                }
                else Part(root,"Service panel",centre+new Vector3(0,0,-.46f),new Vector3(size.x*.55f,size.y*.5f,.04f),cyan);
            }
            var collider=root.AddComponent<BoxCollider>();collider.center=centre;collider.size=size;
            return root;
        }
    }
}
