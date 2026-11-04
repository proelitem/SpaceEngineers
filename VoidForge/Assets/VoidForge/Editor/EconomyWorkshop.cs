using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VoidForge.Economy;
using VoidForge.Integration;
using VoidForge.Player;
using VoidForge.UI.Cyberdeck;
using Object=UnityEngine.Object;

namespace VoidForge.Editor
{
    public static class EconomyWorkshop
    {
        const string Root="Assets/Resources/VoidForge/Economy";
        static readonly List<string> checks=new List<string>();
        static double readyAt;
        static string Evidence=>Path.GetFullPath("docs/validation/economy-2026-11-03");
        [Serializable] sealed class Report {public bool success;public string error,unityVersion;public int icons,recipes,blocks;public string[] checks;}
        [MenuItem("VoidForge/Economy/1. Générer les ressources")]
        public static void Build()
        {
            var definition=JsonUtility.FromJson<EconomyDefinition>(File.ReadAllText(Root+"/Catalogue.json"));definition.Validate();
            Directory.CreateDirectory(Root+"/Prefabs");Directory.CreateDirectory(Root+"/Materials");AssetDatabase.Refresh();
            var source=AssetDatabase.LoadAssetAtPath<BlockCatalogue>("Assets/Resources/VoidForge/Integration/BlockCatalogue.asset");
            if(source==null)throw new InvalidOperationException("Existing integration catalogue required; run IntegrationWorkshop first.");
            var entries=new List<BlockCatalogue.Entry>();
            foreach(var block in definition.blocks)
            {
                var entry=new BlockCatalogue.Entry{id=block.id,size=new Vector3Int(block.x,block.y,block.z)};
                var existing=source.Find(block.id);
                if(existing!=null&&existing.Available)entry.prefab=existing.prefab;
                else
                {
                    var go=EconomyBlockModels.Create(block.id,entry.size);
                    var bounds=new Bounds(((Vector3)entry.size-Vector3.one)*.5f,entry.size);
                    foreach(var renderer in go.GetComponentsInChildren<Renderer>())
                    {
                        if(!bounds.Contains(renderer.bounds.min)||!bounds.Contains(renderer.bounds.max))throw new InvalidOperationException("Geometry exceeds reserved grid cells: "+block.id+" / "+renderer.name);
                    }
                    foreach(var renderer in go.GetComponentsInChildren<Renderer>())
                    {
                        var mat=renderer.sharedMaterial;string name=mat.name.Replace(' ','-');string path=Root+"/Materials/"+name+".mat";
                        var saved=AssetDatabase.LoadAssetAtPath<Material>(path);
                        if(saved==null){if(!AssetDatabase.Contains(mat))AssetDatabase.CreateAsset(mat,path);saved=mat;}
                        renderer.sharedMaterial=saved;
                    }
                    entry.prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/"+block.id+".prefab");Object.DestroyImmediate(go);
                }
                if(entry.prefab==null)throw new InvalidOperationException("Cannot build prefab "+block.id);
                entries.Add(entry);
            }
            var catalogue=AssetDatabase.LoadAssetAtPath<BlockCatalogue>(Root+"/Blocks.asset");
            if(catalogue==null){catalogue=ScriptableObject.CreateInstance<BlockCatalogue>();AssetDatabase.CreateAsset(catalogue,Root+"/Blocks.asset");}
            catalogue.entries=entries.ToArray();EditorUtility.SetDirty(catalogue);AssetDatabase.SaveAssets();
            var graphics=AssetDatabase.LoadAssetAtPath<CyberdeckAssets>("Assets/Resources/VoidForge/Cyberdeck/Graphics.asset");
            if(graphics==null||graphics.icons.Length!=75||graphics.icons.Any(x=>x.texture==null))CyberdeckWorkshop.Build();
            Debug.Log("[VoidForge Economy] 11 functional construction definitions and 75 individual Cyberdeck graphics generated.");
        }
        public static void RunBatch(){readyAt=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Begin;}
        static void Begin()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<readyAt)return;
            EditorApplication.update-=Begin;
            try{Build();readyAt=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Review;}
            catch(Exception e){Finish(new Report{error=e.ToString()});}
        }
        static void Check(bool pass,string name){if(!pass)throw new InvalidOperationException(name);checks.Add(name);}
        static void Review()
        {
            if(EditorApplication.timeSinceStartup<readyAt)return;EditorApplication.update-=Review;
            var report=new Report{unityVersion=Application.unityVersion};
            try
            {
                Directory.CreateDirectory(Evidence);checks.Clear();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                var runtime=new GameObject("Economy validation").AddComponent<EconomyRuntime>();runtime.saveEnabled=false;
                var definition=JsonUtility.FromJson<EconomyDefinition>(Resources.Load<TextAsset>("VoidForge/Economy/Catalogue").text);
                runtime.Initialize(definition,Resources.Load<BlockCatalogue>("VoidForge/Economy/Blocks"),Path.GetFullPath("Library/EconomyValidation/not-used.json"));
                var hudGo=Object.Instantiate(Resources.Load<GameObject>("VoidForge/Cyberdeck/Hud"));hudGo.transform.localScale=Vector3.one;
                var hud=hudGo.GetComponent<CyberdeckHud>();hud.Initialize();runtime.Attach(hud.State);runtime.AddWaypoints(hud.State);
                hud.BlockAvailable=runtime.BlockSupported;hud.BlockSize=runtime.BlockSize;
                report.icons=hud.assets.icons.Length;report.recipes=definition.recipes.Length;report.blocks=definition.blocks.Length;
                Check(report.icons==75&&hud.assets.icons.All(x=>x.texture!=null),"75 native Texture2D icons resolve");
                Check(report.recipes==35&&report.blocks==11,"complete live production and construction catalogue");
                Check(!hud.State.preview&&hud.State.items.Any(x=>x.id=="tool-drill"),"HUD shows actual backpack, not preview values");
                hud.Show(DeckPage.Refinery);Check(!hud.Controls.ContainsKey("refinery-power"),"industry controls locked outside cockpit");
                hud.Show(DeckPage.Inventory);hud.Invoke("cargo-source");Check(hud.Controls.ContainsKey("cargo-source-backpack")&&hud.Controls.ContainsKey("cargo-source-cargo"),"two actual storage sources exposed");
                // The editor camera is an actual player head for seat-distance validation.
                var player=new GameObject("Validation player",typeof(PlayerInputProvider),typeof(Rigidbody));
                var head=new GameObject("Validation head",typeof(Camera));head.tag="MainCamera";head.transform.SetParent(player.transform,false);
                var cockpit=runtime.Instances.Values.Select(x=>x.GetComponent<EconomyBlock>()).First(x=>x.type=="Cockpit");player.transform.position=cockpit.transform.position+Vector3.back;
                Check(runtime.EnterSeat(cockpit),"real nearby cockpit interaction enters seat");
                Check(hud.State.cockpitConnected&&hud.State.refineryAvailable&&hud.State.assemblerAvailable,"cockpit publishes real connected machines");
                hud.Invoke("cargo-source"); // close any inventory dropdown
                hud.State.SelectStorage("cargo");Check(hud.State.items.Any(x=>x.id=="ingot-iron"&&Mathf.Approximately(x.quantity,100)),"cargo selection reads its own ledger");
                runtime.World.Mine("outcrop-00",30);Check(runtime.World.Backpack.Get("ore-iron")==30,"finite mineable ore credited to backpack");
                runtime.World.Transfer(false,"ore-iron",30);Check(runtime.World.Cargo.Get("ore-iron")==30&&runtime.World.Backpack.Get("ore-iron")==0,"transfer conserves ore between actual stocks");
                runtime.World.RefineryPaused=true;for(int i=0;i<25;i++)runtime.World.Tick(.1f);Check(runtime.World.Cargo.Get("ore-iron")==30,"refinery pause stops processing");runtime.World.RefineryPaused=false;
                for(int i=0;i<25;i++)runtime.World.Tick(.1f);
                Check(runtime.World.Cargo.Get("ore-iron")==20&&runtime.World.Cargo.Get("ingot-iron")==107,"actual refinery consumes10ore and delivers7iron");
                runtime.PublishSession();hud.Show(DeckPage.Assembly);
                for(int i=0;i<24&&!hud.Controls.ContainsKey("recipe-craft-component-motor");i++)hud.ScrollRecipes(1);
                Check(hud.Invoke("recipe-craft-component-motor"),"recipe pagination reaches motor");
                while(hud.Quantity>1)hud.Invoke("quantity-minus");
                Check(hud.Invoke("assembly-add")&&runtime.World.Cargo.Get("ingot-iron")==87&&runtime.World.Cargo.Get("ingot-nickel")==15,"HUD queues motor with both real ingredients reserved");
                for(int i=0;i<25;i++)runtime.World.Tick(.1f);
                Check(runtime.World.Cargo.Get("component-motor")==1,"production delivers motor to actual cargo");
                runtime.PublishSession();hud.Show(DeckPage.Construction);hud.Invoke("block-cost");
                Check(hud.GetComponentsInChildren<UnityEngine.UI.Text>().Any(x=>x.text.Contains("COMPOSANTS POUR UN BLOC")),"construction exposes complete component costs");
                Check(runtime.BuildAt("ArmorCube",new GridCell(-1,0,0),0)&&runtime.World.Backpack.Get("component-plates")==12,"world construction consumes4 actual one-metre plates");
                int count=runtime.World.Blocks.Count;Check(!runtime.BuildAt("ArmorCube",new GridCell(-1,0,0),0)&&runtime.World.Blocks.Count==count&&runtime.World.Backpack.Get("component-plates")==12,"duplicate placement consumes nothing");
                var save=JsonUtility.ToJson(runtime.World.Snapshot());var loaded=EconomyWorld.Restore(definition,JsonUtility.FromJson<EconomySave>(save));
                Check(loaded.Backpack.Get("component-plates")==12&&loaded.Cargo.Get("component-motor")==1&&loaded.Deposits["outcrop-00"].remaining=="11970","Unity JSON roundtrip preserves stock,crafted output and depletion");
                runtime.ExitSeat();Check(!hud.State.cockpitConnected,"leaving seat revokes industry controls");Check(runtime.EnterSeat(cockpit),"cockpit can reconnect after exit");
                runtime.PublishSession();
                var cameraGo=new GameObject("Flat HUD camera",typeof(Camera));var camera=cameraGo.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=CyberdeckHud.Height*.5f;camera.transform.position=new Vector3(0,0,-1000);camera.nearClipPlane=.1f;camera.farClipPlane=2000;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.01f,.03f,.04f);
                hudGo.GetComponent<Canvas>().worldCamera=camera;
                foreach(DeckPage page in Enum.GetValues(typeof(DeckPage))){hud.Show(page);if(page==DeckPage.Map)hud.Zoom(30/hud.MapRange);Capture(hud,camera,page.ToString().ToLowerInvariant()+".png");}
                Check(ShaderUtil.GetShaderMessages(hud.assets.glyphMaterial.shader).All(x=>x.severity.ToString()!="Error"),"URP glyph shader compiles");
                report.success=true;
            }
            catch(Exception e){report.error=e.ToString();Debug.LogException(e);}
            Finish(report);
        }
        static void Capture(CyberdeckHud hud,Camera camera,string name)
        {
            Canvas.ForceUpdateCanvases();var rt=new RenderTexture(1914,822,24);rt.Create();camera.targetTexture=rt;
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(1914,822,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1914,822),0,0);texture.Apply();
            Color amber=texture.GetPixel(hud.page==DeckPage.Inventory?250:hud.page==DeckPage.Construction?720:hud.page==DeckPage.Map?1680:1180,127);
            Check(amber.r>.7f&&amber.g>.3f&&amber.b<.4f,"native live HUD pixels: "+name);
            File.WriteAllBytes(Path.Combine(Evidence,name),texture.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(texture);rt.Release();Object.DestroyImmediate(rt);
        }
        static void Finish(Report report)
        {Directory.CreateDirectory(Evidence);report.checks=checks.ToArray();File.WriteAllText(Path.Combine(Evidence,"unity-validation.json"),JsonUtility.ToJson(report,true));if(!report.success)Debug.LogError(report.error);EditorApplication.Exit(report.success?0:1);}
    }
}
