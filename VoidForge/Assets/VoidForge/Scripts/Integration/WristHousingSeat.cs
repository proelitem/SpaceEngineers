using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoidForge.Integration
{
    // A conforming housing underside, attached to the same skinned sleeve samples as the arm.
    // The flat display sits on this shaped seat; there are no visible straps or an air gap.
    [DefaultExecutionOrder(10020)]
    [RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
    public sealed class WristHousingSeat:MonoBehaviour
    {
        public WristDisplayMount mount;
        public WristDisplayMount.SleeveSample[] contacts;
        public Vector3[] top;
        public int columns=11,rows=7;
        Mesh mesh;
        Vector3[] vertices;
        public float MaxContactGap {get;private set;}
        public void Configure(WristDisplayMount owner)
        {
            mount=owner;int count=columns*rows;contacts=new WristDisplayMount.SleeveSample[count];top=new Vector3[count];
            var surface=mount.surface;Vector3 centre=surface.screen.bounds.center;
            Vector3 right=surface.hudAnchor.right,up=surface.hudAnchor.up,facing=surface.hudAnchor.forward;
            for(int y=0;y<rows;y++)for(int x=0;x<columns;x++)
            {
                int i=y*columns+x;
                Vector3 target=centre+right*((x/(float)(columns-1)-.5f)*.194f)+up*((y/(float)(rows-1)-.5f)*.076f);
                float best=float.PositiveInfinity;WristDisplayMount.SleeveSample chosen=null;
                foreach(var sample in mount.sleeveSamples)
                {
                    Vector3 delta=sample.WorldPosition-target;
                    float distance=Mathf.Pow(Vector3.Dot(delta,right),2)+Mathf.Pow(Vector3.Dot(delta,up),2);
                    // Samples on the underside of the arm cannot support a device on its palm side.
                    float depth=Vector3.Dot(delta,facing);
                    float score=distance+Mathf.Max(0,-depth-mount.housingDepth-.035f)*.02f;
                    if(score<best){best=score;chosen=sample;}
                }
                if(chosen==null)throw new InvalidOperationException("No forearm surface for housing contact.");
                contacts[i]=chosen;top[i]=transform.InverseTransformPoint(target-facing*(mount.housingDepth-.001f));
            }
            Build();Refresh();
        }
        void Awake(){if(contacts!=null&&contacts.Length>0)Build();}
        void Build()
        {
            int count=columns*rows;vertices=new Vector3[count*2];var indices=new System.Collections.Generic.List<int>();
            void Quad(int a,int b,int c,int d){indices.AddRange(new[]{a,b,c,a,c,d});}
            for(int y=0;y<rows-1;y++)for(int x=0;x<columns-1;x++)
            {int a=y*columns+x,b=a+1,c=a+columns+1,d=a+columns;Quad(a,d,c,b);Quad(a+count,b+count,c+count,d+count);}
            for(int x=0;x<columns-1;x++){Quad(x,x+1,x+1+count,x+count);int a=(rows-1)*columns+x;Quad(a+1,a,a+count,a+1+count);}
            for(int y=0;y<rows-1;y++){int a=y*columns;Quad(a+columns,a,a+count,a+columns+count);a+=columns-1;Quad(a,a+columns,a+columns+count,a+count);}
            mesh=new Mesh{name="Conforming cyberdeck housing underside"};mesh.vertices=vertices;mesh.triangles=indices.ToArray();mesh.MarkDynamic();GetComponent<MeshFilter>().sharedMesh=mesh;
            GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        void LateUpdate()=>Refresh();
        public void Refresh()
        {
            if(mesh==null||mount==null)return;
            int count=contacts.Length;MaxContactGap=0;
            for(int i=0;i<count;i++)
            {
                Vector3 contact=contacts[i].WorldPosition;
                vertices[i]=transform.InverseTransformPoint(contact-mount.surface.hudAnchor.forward*.0002f);vertices[i+count]=top[i];
                MaxContactGap=Mathf.Max(MaxContactGap,Vector3.Distance(transform.TransformPoint(vertices[i]),contact));
            }
            mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
        }
        void OnDestroy(){if(mesh!=null&&Application.isPlaying)Destroy(mesh);}
    }
}
