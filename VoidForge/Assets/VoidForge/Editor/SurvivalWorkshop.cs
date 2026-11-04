#if VF_LOCAL_DIGGER && VF_LOCAL_ECONOMY
using System;
using System.IO;
using System.Linq;
using Digger.Modules.Core.Sources;
using Digger.Modules.Core.Sources.Jobs;
using Digger.Modules.Runtime.Sources;
using Digger.Modules.Runtime.Sources.Editor;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using VoidForge.Integration;
using VoidForge.World;
using Object=UnityEngine.Object;

namespace VoidForge.Editor
{
    public static class SurvivalWorkshop
    {
        const string Local="Assets/ThirdParty/VoidForgeSurvival";
        const string Output="Assets/Resources/VoidForge/Survival";
        [MenuItem("VoidForge/Survival/1. Generate expanded solo scene")]
        public static void BuildBatch()
        {
            if(AssetDatabase.LoadAssetAtPath<BlockCatalogue>("Assets/Resources/VoidForge/Economy/Blocks.asset")==null)
                throw new InvalidOperationException("Stage the producer economy/UI delivery first; see docs/SURVIE.md.");
            Directory.CreateDirectory(Local);Directory.CreateDirectory(Output);AssetDatabase.Refresh();
            IntegrationWorkshop.BuildBatch();
            var assets=Resources.Load<AsteroidAssetSet>(AsteroidAssetSet.ResourcePath);
            if(assets==null||!assets.IsUsable)throw new InvalidOperationException("Run Build/import-local-assets.ps1 before the survival workshop.");
            var volumes=BuildVolumes(assets);
            var field=BuildField(assets,volumes);
            var material=AssetDatabase.LoadAssetAtPath<Material>(Output+"/ConstructionHologram.mat");
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(material,Output+"/ConstructionHologram.mat");}
            material.SetColor("_BaseColor",new Color(.05f,.45f,1,.3f));material.SetFloat("_Surface",1);material.SetFloat("_ZWrite",0);
            material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_Cull",(float)CullMode.Off);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=(int)RenderQueue.Transparent;
            var settings=AssetDatabase.LoadAssetAtPath<SurvivalSettings>(Output+"/Settings.asset");
            if(settings==null){settings=ScriptableObject.CreateInstance<SurvivalSettings>();AssetDatabase.CreateAsset(settings,Output+"/Settings.asset");}
            settings.asteroidField=field;settings.hologram=material;settings.seed=421103;
            EditorUtility.SetDirty(settings);EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
            MainSceneBuilder.BuildBatch();
            Debug.Log("[VoidForge Survival] Staged source-shaped volumes, 11 mineral materials, 64 minable asteroids and construction settings.");
        }

        static AsteroidVolumeData BuildVolumes(AsteroidAssetSet assets)
        {
            string path=Local+"/AsteroidVolumes.asset";
            var result=AssetDatabase.LoadAssetAtPath<AsteroidVolumeData>(path);
            if(result==null){result=ScriptableObject.CreateInstance<AsteroidVolumeData>();AssetDatabase.CreateAsset(result,path);}
            result.resolution=41;result.sampleStep=1f/32;
            result.variants=new AsteroidVolumeData.Variant[assets.variants.Length];
            int n=result.resolution;
            for(int i=0;i<assets.variants.Length;i++)
            {
                var mesh=assets.variants[i].lodMeshes[0];
                if(mesh.vertexCount>ushort.MaxValue)throw new InvalidOperationException("Digger native mesh voxelizer requires <=65535 vertices: "+mesh.name);
                var source=mesh.vertices;
                var points=new float3[source.Length];
                for(int j=0;j<source.Length;j++)points[j]=(float3)source[j]/result.sampleStep;
                using var vertices=new NativeArray<float3>(points,Allocator.TempJob);
                var sourceTriangles=mesh.triangles;
                var indices=new ushort[sourceTriangles.Length];
                for(int j=0;j<sourceTriangles.Length;j++)indices[j]=checked((ushort)sourceTriangles[j]);
                using var triangles=new NativeArray<ushort>(indices,Allocator.TempJob);
                using var voxels=new NativeArray<Voxel>(n*n*n,Allocator.TempJob);
                var job=new MeshToVoxelsJob { SizeVox=new int3(n,n,n),Origin=new int3((n-1)/2),Vertices=vertices,Triangles=triangles,Voxels=voxels };
                job.Schedule(voxels.Length,64).Complete();
                var values=new float[voxels.Length];
                for(int j=0;j<values.Length;j++)values[j]=-voxels[j].Value*result.sampleStep;
                if(!values.Any(v=>v<-.02f)||!values.Any(v=>v>.02f))throw new InvalidOperationException("Invalid signed asteroid volume: "+mesh.name);
                result.variants[i]=new AsteroidVolumeData.Variant { signedDistance=values };
                Debug.Log($"[VoidForge Survival] Source mesh voxelized: {mesh.name}, {sourceTriangles.Length/3} triangles, {values.Length} samples.");
            }
            EditorUtility.SetDirty(result);return result;
        }

        static GameObject BuildField(AsteroidAssetSet assets,AsteroidVolumeData volumes)
        {
            var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(string.IsNullOrEmpty(previous.path))previous=EditorSceneManager.OpenScene(MainSceneBuilder.ScenePath,OpenSceneMode.Single);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            try
            {
                var root=new GameObject("Minable asteroid field");
                string dataPath=Local+"/AsteroidTerrain.asset";
                var data=AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
                if(data==null){data=new TerrainData();AssetDatabase.CreateAsset(data,dataPath);}
                data.heightmapResolution=1025;data.alphamapResolution=16;data.size=new Vector3(256,64,256);data.enableHolesTextureCompression=false;
                var layers=new TerrainLayer[AsteroidOreProfile.Items.Length];
                var rock=assets.variants[0].material;
                for(int i=0;i<layers.Length;i++)
                {
                    string name=AsteroidOreProfile.Items[i];
                    string texturePath=Local+"/"+name+".asset";
                    var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    if(texture==null){texture=new Texture2D(32,32,TextureFormat.RGBA32,true);AssetDatabase.CreateAsset(texture,texturePath);}
                    var pixels=new Color[32*32];
                    for(int y=0;y<32;y++)for(int x=0;x<32;x++)pixels[y*32+x]=AsteroidOreProfile.Colors[i]*(.8f+.2f*Mathf.PerlinNoise(x*.29f+i,y*.37f));
                    texture.SetPixels(pixels);texture.Apply();EditorUtility.SetDirty(texture);
                    string layerPath=Local+"/"+name+".terrainlayer";
                    var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
                    if(layer==null){layer=new TerrainLayer();AssetDatabase.CreateAsset(layer,layerPath);}
                    layer.diffuseTexture=i==0?rock.GetTexture("_BaseMap") as Texture2D:texture;
                    layer.normalMapTexture=i==0?rock.GetTexture("_BumpMap") as Texture2D:null;
                    layer.tileSize=new Vector2(2,2);layer.smoothness=i==0?.1f:.32f;layer.metallic=i==0?0:.35f;
                    layers[i]=layer;EditorUtility.SetDirty(layer);
                }
                data.terrainLayers=layers;
                var alpha=new float[16,16,layers.Length];for(int z=0;z<16;z++)for(int x=0;x<16;x++)alpha[z,x,0]=1;data.SetAlphamaps(0,0,alpha);
                var terrainGo=Terrain.CreateTerrainGameObject(data);terrainGo.transform.SetParent(root.transform,false);terrainGo.transform.position=new Vector3(-128,-64,-128);
                var terrain=terrainGo.GetComponent<Terrain>();terrain.drawInstanced=true;terrain.shadowCastingMode=ShadowCastingMode.Off;
                var master=new GameObject("Digger asteroid master").AddComponent<DiggerMaster>();master.transform.SetParent(root.transform,false);
                master.SceneDataFolder="VoidForgeSurvivalV1";master.ChunkSize=17;master.ResolutionMult=1;master.AutoVoxelHeight=true;
                master.CreateLODs=true;master.EnableContributeGI=false;master.EnableOcclusionCulling=false;master.AutoSaveMeshesAsAssets=false;
                DiggerMasterRuntimeEditor.SetupRuntimeScripts();
                var runtime=Object.FindFirstObjectByType<DiggerMasterRuntime>();runtime.enablePersistence=false;runtime.transform.SetParent(root.transform,false);
                var system=root.GetComponentInChildren<DiggerSystem>();
                if(system==null)throw new InvalidOperationException("Digger terrain setup failed.");
                var field=root.AddComponent<MinableAsteroidField>();field.terrain=terrain;field.system=system;field.assets=assets;field.volumes=volumes;
                field.count=64;field.seed=421103;
                terrain.drawHeightmap=false;terrainGo.GetComponent<TerrainCollider>().enabled=false;
                EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();
                return PrefabUtility.SaveAsPrefabAsset(root,Local+"/AsteroidField.prefab");
            }
            finally
            {
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene,true);
            }
        }
    }
}
#endif
