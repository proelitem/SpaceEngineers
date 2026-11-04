using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using VoidForge.Integration;
using VoidForge.UI.Cyberdeck;
using Object=UnityEngine.Object;

namespace VoidForge.Editor
{
    public static class Recette04Workshop
    {
        const string Output="Assets/Resources/VoidForge/Recette04";
        public static void BuildBatch()
        {
            Directory.CreateDirectory(Output+"/Materials");Directory.CreateDirectory(Output+"/Prefabs");AssetDatabase.Refresh();
            ImportEffects();IntegrationWorkshop.BuildBatch();EconomyWorkshop.Build();
            ImportModules();ImportGraphics();
            SurvivalWorkshop.BuildBatch();
            AssetDatabase.SaveAssets();
            Debug.Log("[VoidForge Recette04] Astra modules, Module UI, conforming equipment and Sci-Fi Arsenal tool FX generated.");
        }
        public static void ValidateBatch()
        {
            var catalogue=Resources.Load<BlockCatalogue>("VoidForge/Economy/Blocks");catalogue.Find("Reactor").prefab=ImportReactor();EditorUtility.SetDirty(catalogue);
            ImportGraphics();ImportEffects();AssetDatabase.SaveAssets();Recette04Validation.RunBatch();
        }
        public static void FinalVisualsBatch()
        {
            ImportGraphics();AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
            IntegrationValidation.Run();IntegrationPreview.Render();Recette04MigrationValidation.RunBatch();
        }
        static void ImportModules()
        {
            var catalogue=Resources.Load<BlockCatalogue>("VoidForge/Economy/Blocks");
            string[] ids={"Cargo","Refinery","Assembler"};
            foreach(string id in ids)
            {
                string path="Assets/VoidForge/Art/Modules/2026-11-04/VF_"+id+".fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.globalScale=1;importer.useFileScale=true;importer.isReadable=true;importer.importAnimation=false;importer.SaveAndReimport();
                var root=new GameObject(id+" • Astra 04");
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));model.transform.SetParent(root.transform,false);model.transform.localRotation=Quaternion.Euler(0,180,0);
                var entry=catalogue.Find(id);Bounds allowed=new Bounds(((Vector3)entry.size-Vector3.one)*.5f,(Vector3)entry.size+Vector3.one*.002f);
                foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
                {
                    if(!allowed.Contains(renderer.bounds.min)||!allowed.Contains(renderer.bounds.max))throw new InvalidOperationException("Astra imported grid bounds mismatch "+id+" : "+renderer.bounds);
                    renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>Material(m.name)).ToArray();
                }
                var collider=root.AddComponent<BoxCollider>();collider.center=((Vector3)entry.size-Vector3.one)*.5f;collider.size=entry.size;
                entry.prefab=PrefabUtility.SaveAsPrefabAsset(root,Output+"/Prefabs/"+id+".prefab");Object.DestroyImmediate(root);
            }
            EditorUtility.SetDirty(catalogue);
        }
        public static GameObject ImportReactor()
        {
            const string local="Assets/ThirdParty/VoidForgeReactor";
            Directory.CreateDirectory(local+"/Adapted");AssetDatabase.Refresh();
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(local+"/Prefabs/Generator_01.prefab");
            if(source==null)throw new InvalidOperationException("Run Build/stage-module-reactor.py before generating the reactor casing.");
            var root=new GameObject("Reactor");var model=(GameObject)PrefabUtility.InstantiatePrefab(source);PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);model.transform.SetParent(root.transform,false);
            foreach(var script in root.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);
            foreach(var collider in root.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
            foreach(var t in root.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))if(filter.sharedMesh==null)
            {var empty=filter.GetComponent<MeshRenderer>();if(empty!=null)Object.DestroyImmediate(empty);Object.DestroyImmediate(filter);}
            foreach(var lod in root.GetComponentsInChildren<LODGroup>()){var levels=lod.GetLODs();for(int n=0;n<levels.Length;n++)levels[n].renderers=levels[n].renderers.Where(r=>r!=null).ToArray();lod.SetLODs(levels);}
            var renderers=root.GetComponentsInChildren<MeshRenderer>();if(renderers.Length==0)throw new InvalidOperationException("Reactor casing has no source meshes.");
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            model.transform.localScale*=.94f/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
            bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);model.transform.position-=bounds.center;
            foreach(var renderer in renderers)
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(original=>
                {
                    string id=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original));string path=local+"/Adapted/"+original.name+"-"+id.Substring(0,8)+".mat";
                    var adapted=AssetDatabase.LoadAssetAtPath<Material>(path);if(adapted==null){adapted=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(adapted,path);}
                    adapted.SetTexture("_BaseMap",original.HasProperty("_MainTex")?original.GetTexture("_MainTex"):null);adapted.SetColor("_BaseColor",original.HasProperty("_Color")?original.GetColor("_Color"):Color.white);
                    if(original.HasProperty("_BumpMap")&&original.GetTexture("_BumpMap")!=null){adapted.SetTexture("_BumpMap",original.GetTexture("_BumpMap"));adapted.EnableKeyword("_NORMALMAP");}
                    adapted.SetFloat("_Metallic",.55f);adapted.SetFloat("_Smoothness",.35f);EditorUtility.SetDirty(adapted);return adapted;
                }).ToArray();
            }
            var box=root.AddComponent<BoxCollider>();box.size=Vector3.one;
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,local+"/Adapted/Reactor.prefab");Object.DestroyImmediate(root);return prefab;
        }
        static Material Material(string name)
        {
            name=name.Replace(" (Instance)","");string path=Output+"/Materials/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            Color color;float metal=.4f,rough=.3f;
            switch(name)
            {
                case "VF04_Ivory":color=new Color(.67f,.62f,.51f);metal=.28f;rough=.36f;break;
                case "VF04_Graphite":color=new Color(.035f,.046f,.059f);metal=.62f;rough=.32f;break;
                case "VF04_Steel":color=new Color(.23f,.28f,.32f);metal=.8f;rough=.26f;break;
                case "VF04_Orange":color=new Color(1,.18f,.003f);metal=.38f;rough=.28f;break;
                case "VF04_Amber":color=new Color(1,.2f,.003f);metal=.15f;break;
                case "VF04_Cyan":color=new Color(.008f,.72f,.8f);metal=.15f;break;
                default:throw new InvalidOperationException("Unknown Astra material "+name);
            }
            material.SetColor("_BaseColor",color);material.SetFloat("_Metallic",metal);material.SetFloat("_Smoothness",1-rough);
            if(name=="VF04_Amber"||name=="VF04_Cyan"){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*(name=="VF04_Cyan"?1.7f:1.5f));}
            EditorUtility.SetDirty(material);return material;
        }
        static void ImportGraphics()
        {
            string path="Assets/VoidForge/Art/UI/Cyberdeck/ui-module.png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.sRGBTexture=false;importer.alphaIsTransparency=false;importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=false;importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var graphics=Resources.Load<CyberdeckAssets>("VoidForge/Cyberdeck/Graphics");
            graphics.icons=graphics.icons.Where(i=>i.id!="ui-module").Concat(new[]{new CyberdeckAssets.Icon{id="ui-module",texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path),uv=new Rect(0.054678692f,0.124013529f,0.890642616f,0.739571590f)}}).ToArray();EditorUtility.SetDirty(graphics);
        }
        static void ImportEffects()
        {
            const string local="Assets/ThirdParty/SciFiArsenal";
            string[] names={"SparksYellow","SparksBlue"};var prefabs=new GameObject[2];
            Directory.CreateDirectory(local+"/Adapted");AssetDatabase.Refresh();
            for(int i=0;i<2;i++)
            {
                string source=local+"/Sci-Fi Effects/Prefabs/Environment/Sparks/"+names[i]+".prefab";
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(source);if(prefab==null)throw new InvalidOperationException("Stage Sci-Fi Arsenal particles first: "+source);
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                foreach(var script in go.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);
                foreach(var particle in go.GetComponentsInChildren<ParticleSystem>(true))
                {
                    particle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=particle.main;main.loop=true;main.playOnAwake=false;main.stopAction=ParticleSystemStopAction.None;main.maxParticles=Mathf.Min(main.maxParticles,96);main.simulationSpace=ParticleSystemSimulationSpace.World;
                    main.scalingMode=ParticleSystemScalingMode.Hierarchy;
                    var emission=particle.emission;emission.enabled=true;emission.rateOverTime=Mathf.Clamp(emission.rateOverTime.constantMax,12,45);
                    var renderer=particle.GetComponent<ParticleSystemRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                    renderer.sharedMaterials=renderer.sharedMaterials.Select(original=>
                    {
                        string matPath=local+"/Adapted/"+original.name+"-URP.mat";var adapted=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                        if(adapted==null){adapted=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(adapted,matPath);}
                        var texture=original.HasProperty("_MainTex")?original.GetTexture("_MainTex"):null;
                        adapted.SetTexture("_BaseMap",texture);adapted.SetColor("_BaseColor",Color.white);adapted.SetFloat("_Surface",1);adapted.SetFloat("_Blend",2);adapted.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);adapted.SetFloat("_DstBlend",(float)BlendMode.One);adapted.SetFloat("_ZWrite",0);adapted.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");adapted.renderQueue=3000;EditorUtility.SetDirty(adapted);return adapted;
                    }).ToArray();
                }
                prefabs[i]=PrefabUtility.SaveAsPrefabAsset(go,local+"/Adapted/"+names[i]+"-Loop.prefab");Object.DestroyImmediate(go);
            }
            const string libraryPath="Assets/Resources/VoidForge/Survival/ToolEffects.asset";
            var library=AssetDatabase.LoadAssetAtPath<ToolEffectLibrary>(libraryPath);if(library==null){library=ScriptableObject.CreateInstance<ToolEffectLibrary>();AssetDatabase.CreateAsset(library,libraryPath);}
            library.cuttingSparks=prefabs[0];library.weldingSparks=prefabs[1];EditorUtility.SetDirty(library);
        }
    }
}
