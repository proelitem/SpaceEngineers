#if VF_LOCAL_DIGGER && VF_LOCAL_ECONOMY
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoidForge.World
{
    // Far source meshes and generated Digger meshes share exactly the same spatial material.
    // A Digger chunk can contain several asteroids: partition triangles rather than changing its whole texture.
    public sealed class AsteroidSurfaceBinding:MonoBehaviour
    {
        sealed class Replacement {public Mesh native,visual;}
        readonly Dictionary<MeshFilter,Replacement> replacements=new Dictionary<MeshFilter,Replacement>();
        readonly Dictionary<int,Material> materials=new Dictionary<int,Material>();
        MinableAsteroidField field;
        public void Initialize(MinableAsteroidField owner)
        {
            field=owner;
            var shader=Resources.Load<Shader>("VoidForge/Survival/AsteroidIdentity");
            if(shader==null)shader=Shader.Find("VoidForge/AsteroidIdentity");
            if(shader==null)throw new System.InvalidOperationException("Asteroid identity shader missing.");
            foreach(var asteroid in field.Asteroids)
            {
                var original=field.assets.variants[asteroid.variant].material;
                var material=new Material(shader){name="Asteroid material "+asteroid.index};
                material.SetTexture("_BaseMap",original.GetTexture("_BaseMap"));material.SetColor("_BaseColor",original.GetColor("_BaseColor"));
                material.SetMatrix("_WorldToAsteroid",Matrix4x4.Scale(Vector3.one/asteroid.diameter)*asteroid.transform.worldToLocalMatrix);
                for(int i=0;i<2;i++)
                {
                    if(i>=asteroid.veins.Length){material.SetVector("_VeinA"+i,Vector4.zero);continue;}
                    var vein=asteroid.veins[i];material.SetVector("_VeinA"+i,new Vector4(vein.a.x,vein.a.y,vein.a.z,vein.radius));
                    material.SetVector("_VeinB"+i,vein.b);material.SetColor("_VeinColor"+i,AsteroidOreProfile.Colors[vein.material]);
                }
                materials.Add(asteroid.index,material);
                foreach(var renderer in asteroid.original.GetComponentsInChildren<MeshRenderer>(true))renderer.sharedMaterial=material;
            }
        }
        public Material MaterialFor(int index)=>materials[index];
        public void RefreshChunks()
        {
            foreach(var filter in field.terrain.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||filter.sharedMesh==null)continue;
                replacements.TryGetValue(filter,out var previous);
                var source=previous!=null&&filter.sharedMesh==previous.visual?previous.native:filter.sharedMesh;
                if(source==null||source.vertexCount==0)continue;
                var vertices=source.vertices;var triangles=source.triangles;
                var groups=new Dictionary<int,List<int>>();
                for(int i=0;i<triangles.Length;i+=3)
                {
                    Vector3 point=filter.transform.TransformPoint((vertices[triangles[i]]+vertices[triangles[i+1]]+vertices[triangles[i+2]])/3);
                    MinableAsteroid asteroid=null;float closest=float.PositiveInfinity;
                    foreach(var candidate in field.Asteroids){float d=(candidate.transform.position-point).sqrMagnitude/(candidate.diameter*candidate.diameter);if(d<closest){closest=d;asteroid=candidate;}}
                    if(!groups.TryGetValue(asteroid.index,out var indices)){indices=new List<int>();groups.Add(asteroid.index,indices);}
                    indices.Add(triangles[i]);indices.Add(triangles[i+1]);indices.Add(triangles[i+2]);
                }
                var mesh=Instantiate(source);mesh.name=source.name+" stable asteroid materials";mesh.subMeshCount=groups.Count;
                var shared=new Material[groups.Count];int sub=0;
                foreach(var group in groups.OrderBy(g=>g.Key)){mesh.SetTriangles(group.Value,sub);shared[sub++]=materials[group.Key];}
                filter.sharedMesh=mesh;renderer.sharedMaterials=shared;
                replacements[filter]=new Replacement{native=source,visual=mesh};
                if(previous?.visual!=null)Destroy(previous.visual);
            }
        }
        void OnDestroy()
        {foreach(var m in materials.Values)Destroy(m);foreach(var replacement in replacements.Values)if(replacement.visual!=null)Destroy(replacement.visual);}
    }
}
#endif
