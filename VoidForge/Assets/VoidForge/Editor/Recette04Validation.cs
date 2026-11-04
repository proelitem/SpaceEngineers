#if VF_LOCAL_DIGGER && VF_LOCAL_ECONOMY
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Digger.Modules.Core.Sources;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using VoidForge.Integration;
using VoidForge.UI.Cyberdeck;
using VoidForge.World;
using Object=UnityEngine.Object;

namespace VoidForge.Editor
{
    // Actual Main play, actual Digger mesh/collider, then a second load of the same isolated save.
    public static class Recette04Validation
    {
        [Serializable] sealed class Report
        {
            public bool success,headsetTested;
            public string unity,error,save;
            public string[] checks,errors;
            public int asteroids,excavations;
            public float colliderAdvanceMetres;
        }
        static readonly List<string> checks=new List<string>(),errors=new List<string>();
        static bool optionsEnabled,xr,restoreXR;
        static EnterPlayModeOptions options;
        static double deadline;
        static int phase;
        static Task<bool> dig;
        static Ray probe;
        static float initialDistance,finalDistance;
        static string savedStock;
        static int savedEdits,savedBlocks,savedPlans;
        static string ore;
        static decimal oreBefore;
        static string pendingError;
        static double drainUntil;
        static string output=>Path.GetFullPath("Hermes_Works/Preuves/INTEGRATION_04/play-validation.json");

        public static void RunBatch()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-vfSurvivalSave");
            if(index<0||index+1>=args.Length||File.Exists(args[index+1]))throw new InvalidOperationException("Pass a NEW -vfSurvivalSave path under Library/SurvivalValidation; never run on a player's save.");
            string root=Path.GetFullPath("Library/SurvivalValidation")+Path.DirectorySeparatorChar;
            if(!Path.GetFullPath(args[index+1]).StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Validation save must stay under Library/SurvivalValidation.");
            checks.Clear();errors.Clear();phase=0;dig=null;
            pendingError=null;drainUntil=0;
            if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                IntegrationPreview.Render();
            }
            optionsEnabled=EditorSettings.enterPlayModeOptionsEnabled;options=EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            var standalone=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            restoreXR=standalone!=null;if(restoreXR){xr=standalone.InitManagerOnStart;standalone.InitManagerOnStart=false;}
            EditorSceneManager.OpenScene(MainSceneBuilder.ScenePath);
            deadline=EditorApplication.timeSinceStartup+300;
            ValidateMassForces();
            Application.logMessageReceived+=Log;
            EditorApplication.update+=Tick;
            EditorApplication.isPlaying=true;
        }
        static void Log(string message,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+stack);}
        static void Require(bool condition,string description)
        {if(!condition)throw new InvalidOperationException(description);checks.Add(description);}
        static string Stock(SurvivalSession session)=>JsonUtility.ToJson(new StockState {bag=session.Ledger.Backpack.Snapshot(),cargo=session.Ledger.Cargo.Snapshot()});
        [Serializable] sealed class StockState { public VoidForge.Economy.ResourceAmount[] bag,cargo; }

        static void Tick()
        {
            if(phase==6)
            {
                if(EditorApplication.isPlaying)return;
                if(drainUntil==0)drainUntil=EditorApplication.timeSinceStartup+2;
                if(EditorApplication.timeSinceStartup<drainUntil)return;
                Complete();return;
            }
            if(EditorApplication.timeSinceStartup>deadline){Finish("Timeout waiting for expanded Main/voxel test (300 s).");return;}
            if(EditorApplication.isCompiling)return;
            if(phase==4&&!EditorApplication.isPlaying)
            {phase=5;deadline=EditorApplication.timeSinceStartup+300;EditorApplication.isPlaying=true;return;}
            if(!EditorApplication.isPlaying)return;
            var session=SurvivalSession.Active;var field=Object.FindFirstObjectByType<MinableAsteroidField>();
            if(session==null||!session.Ready||session.deck==null||session.deck.State==null||field==null||field.Busy||!field.Asteroids[0].voxelReady||Object.FindFirstObjectByType<GpsTargetGuide>()==null)return;
            try
            {
                if(session.Faulted||field.Faulted)throw new InvalidOperationException("Expanded scene reported a load/mining fault.");
                field.enabled=false;
                if(phase==0)
                {
                    Require(Object.FindFirstObjectByType<MiningPrototype>()==null,"Old mining demonstration absent");
                    Require(Object.FindFirstObjectByType<BlockGrid>()==null,"Old block display absent");
                    Require(field.Asteroids.Count==64,"64 asset-based asteroids, all share minable registration");
                    Require(session.Ledger.Count("ArmorCube")==4&&session.Ledger.Blocks.Count==8,"Starter platform has four blocks plus four machines");
                    Require(session.Ledger.Count("Battery")==1&&session.Ledger.Count("Refinery")==1&&session.Ledger.Count("Assembler")==1&&session.Ledger.Count("Cargo")==1,"Starter workshop machines and storage present");
                    int reserved=session.Ledger.Blocks.Sum(b=>session.Ledger.Cells(b).Count());
                    Require(session.Ledger.Blocks.SelectMany(session.Ledger.Cells).Distinct().Count()==reserved,"Starter machines respect their actual multi-cell footprints");
                    var equipment=session.equipment;
                    Require(equipment!=null&&equipment.tools.Count(t=>t.gameObject.activeSelf)==1,"Exactly one selected tool visible at the right thigh");
                    session.deck.State.Equip(2);
                    Require(equipment.Selected.kind==HandToolKind.Grinder&&equipment.Held==null,"Cyberdeck selection changes thigh tool without remote grabbing");
                    var gps=Object.FindFirstObjectByType<GpsTargetGuide>();
                    Require(gps!=null,"GPS guidance installed in Main");
                    Require(GpsTargetGuide.EdgeDirection(new Vector3(2,0,-3)).x>0&&GpsTargetGuide.EdgeDirection(new Vector3(-2,0,3)).x<0,"Offscreen directions remain correct for front and rear targets");
                    var waypoint=session.deck.State.waypoints.First(w=>w.id=="Asteroid_00");session.deck.State.Target(waypoint);
                    ValidateConstruction(session);
                    Recette04Capture.Render(session);
                    Require(ShaderUtil.GetShaderMessages(Resources.Load<Shader>("VoidForge/Survival/AsteroidIdentity")).All(m=>m.severity.ToString()!="Error"),"Asteroid identity shader has no native compiler errors");
                    Require(field.Surfaces!=null&&field.Asteroids.All(a=>a.original.GetComponentsInChildren<MeshRenderer>(true).All(r=>r.sharedMaterial==field.Surfaces.MaterialFor(a.index))),"Every distant asteroid retains its own stable material identity");
                    var surfaces=field.terrain.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.GetComponent<MeshFilter>()?.sharedMesh!=null&&r.GetComponent<MeshFilter>().sharedMesh.vertexCount>0).ToArray();
                    Require(surfaces.Length>0&&surfaces.All(r=>r.sharedMaterials.All(m=>m.shader.name=="VoidForge/AsteroidIdentity")),"Generated Digger chunks with actual geometry use the same stable identity shader: "+string.Join(",",surfaces.Select(r=>r.name+":"+string.Join("/",r.sharedMaterials.Select(m=>m.shader.name)))));
                    Physics.SyncTransforms();bool found=false;
                    foreach(var direction in new[]{Vector3.forward,Vector3.right,Vector3.up})
                    {
                        probe=new Ray(field.Asteroids[0].transform.position+direction*12,-direction);
                        if(Physics.Raycast(probe,out var hit,24,~0,QueryTriggerInteraction.Ignore)&&hit.collider.transform.IsChildOf(field.terrain.transform))
                        {initialDistance=hit.distance;dig=field.Dig(0,hit.point-hit.normal*.15f,.75f);found=true;break;}
                    }
                    Require(found,"Source-shaped Digger asteroid has a reachable real collider");phase=1;return;
                }
                if(phase==1)
                {
                    if(!dig.IsCompleted)return;
                    Require(!dig.IsFaulted&&dig.Result,"Real surface excavation succeeded");
                    Physics.SyncTransforms();Require(Physics.Raycast(probe,out var hit,30,~0,QueryTriggerInteraction.Ignore),"Excavated collider can still be probed");
                    finalDistance=hit.distance;Require(finalDistance>initialDistance+.05f,"Excavation changes actual collision surface by more than 5 cm");
                    int material=field.Asteroids[0].veins[0].material;
                    ore=AsteroidOreProfile.Items[material];oreBefore=session.Ledger.Backpack.Get(ore);
                    bool found=false;
                    foreach(var chunk in Object.FindObjectsByType<VoxelChunk>(FindObjectsSortMode.None))
                    {
                        var voxels=chunk.VoxelArray;if(voxels==null)continue;int n=chunk.SizeVox;
                        for(int i=0;i<voxels.Length;i++)
                        {
                            if(voxels[i].FirstTextureIndex!=material||voxels[i].Value>=-.1f)continue;
                            var point=(Vector3)chunk.AbsoluteWorldPosition+Vector3.Scale(new Vector3(i/(n*n),(i/n)%n,i%n),(Vector3)chunk.HeightmapScale);
                            if(Vector3.Distance(point,field.Asteroids[0].transform.position)>6)continue;
                            dig=field.Dig(0,point,.75f);found=true;break;
                        }
                        if(found)break;
                    }
                    Require(found,"Buried ore is encoded in the same Digger voxels used for rendering");phase=2;return;
                }
                if(phase==2)
                {
                    if(!dig.IsCompleted)return;
                    Require(!dig.IsFaulted&&dig.Result&&session.Ledger.Backpack.Get(ore)>oreBefore,"Digging a vein credits its actual ore rather than generic stone");
                    Require(session.Excavations.Count==2&&session.Excavations.Select(e=>e.uid).Distinct().Count()==2,"Two excavations credited once each");
                    Physics.SyncTransforms();if(Physics.Raycast(probe,out var finalHit,30,~0,QueryTriggerInteraction.Ignore))finalDistance=finalHit.distance;
                    session.Save();savedStock=Stock(session);savedEdits=session.Excavations.Count;savedBlocks=session.Ledger.Blocks.Count;savedPlans=session.construction.Plans.Count;
                    var save=JsonUtility.FromJson<SurvivalSave>(File.ReadAllText(SurvivalSession.SavePath));
                    Require(save.excavations.Length==savedEdits&&save.plans.Length==savedPlans&&!string.IsNullOrEmpty(save.ledger),"One atomic save contains ledger, construction and voxel edits");
                    phase=4;EditorApplication.isPlaying=false;return;
                }
                if(phase==5)
                {
                    Require(SurvivalSession.Loaded!=null,"Second Play restores the isolated save");
                    Require(Stock(session)==savedStock&&session.Excavations.Count==savedEdits,"Replay restores stock without duplicate mining credit");
                    Require(session.construction.Grids.Any(g=>g.name=="MODULE TEST 04")&&session.construction.Plans.Any(p=>p.progress>0&&p.progress<1),"Module names and actually installed blueprint components survive reload");
                    Require(session.Ledger.Blocks.Count==savedBlocks&&session.construction.Plans.Count==savedPlans,"Constructed blocks and unfinished holograms survive reload");
                    Physics.SyncTransforms();Require(Physics.Raycast(probe,out var hit,30,~0,QueryTriggerInteraction.Ignore)&&Mathf.Abs(hit.distance-finalDistance)<.08f,"Saved excavation restored in collider geometry");
                    Finish(null);
                }
            }
            catch(Exception error){Finish(error.ToString());}
        }
        static void ValidateMassForces()
        {
            var old=Physics.simulationMode;var light=new GameObject("QA unloaded module").AddComponent<Rigidbody>();var heavy=new GameObject("QA cargo-loaded module").AddComponent<Rigidbody>();
            try
            {
                Physics.simulationMode=SimulationMode.Script;light.useGravity=heavy.useGravity=false;light.linearDamping=heavy.linearDamping=0;light.mass=100;heavy.mass=200;
                light.AddForce(Vector3.right*1000,ForceMode.Force);heavy.AddForce(Vector3.right*1000,ForceMode.Force);Physics.Simulate(.02f);
                Require(Mathf.Abs(light.linearVelocity.x-2*heavy.linearVelocity.x)<.00001f&&light.linearVelocity.x>0,"Real physics: double cargo mass halves acceleration at the same force");
                light.linearVelocity=heavy.linearVelocity=Vector3.right*3;light.AddForce(Vector3.left*1000,ForceMode.Force);heavy.AddForce(Vector3.left*1000,ForceMode.Force);Physics.Simulate(.02f);
                Require(Mathf.Abs((3-light.linearVelocity.x)-2*(3-heavy.linearVelocity.x))<.00001f,"Real physics: double cargo mass halves braking at the same force");
            }
            finally{Physics.simulationMode=old;Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(heavy.gameObject);}
        }
        static void ValidateConstruction(SurvivalSession session)
        {
            var construction=session.construction;var world=session.Ledger;var hud=session.deck.Hud;int built=world.Blocks.Count;
            hud.Show(DeckPage.Construction);hud.MoveBlock(3);hud.Invoke("select-block");
            Require(hud.State.selectedBlock=="Refinery","Refinery carousel selection binds the refinery ID");
            hud.Invoke("select-block");Require(hud.State.selectedBlock==null,"Repeated selection deselects the same blueprint");
            Require(session.economy.BlockSize("Refinery")==new Vector3Int(1,2,1),"Refinery reserves one by two vertical cells");
            var catalog=session.economy.Catalogue;
            foreach(string id in new[]{"Cargo","Refinery","Assembler"})
            {
                Require(AssetDatabase.GetAssetPath(catalog.Find(id).prefab)=="Assets/Resources/VoidForge/Recette04/Prefabs/"+id+".prefab","Actual Astra machine prefab selected: "+id);
                Require(catalog.Find(id).prefab.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3)>4000,"Detailed approved machine mesh, rather than armor fallback: "+id);
            }
            foreach(var entry in catalog.entries.Where(e=>e.Available))
            {
                Require(construction.PlaceFoundation(entry.id,new Vector3(-1,-.3f,2),Quaternion.identity),"Can preview catalogue block: "+entry.id);
                var blueprint=construction.Plans.Last();var visual=Object.FindObjectsByType<ConstructionPlanMarker>(FindObjectsSortMode.None).Single(p=>p.uid==blueprint.uid);
                var expected=entry.prefab.GetComponentsInChildren<MeshFilter>().Select(f=>f.sharedMesh.GetInstanceID()).OrderBy(x=>x);
                Require(expected.SequenceEqual(visual.GetComponentsInChildren<MeshFilter>().Select(f=>f.sharedMesh.GetInstanceID()).OrderBy(x=>x)),"Blueprint uses the exact corresponding asset meshes: "+entry.id);
                Require(construction.Grind(blueprint.uid),"Empty blueprint can be ground away: "+entry.id);
            }
            decimal plates=world.Backpack.Get("component-plates");
            Require(construction.PlaceFoundation("ArmorCube",new Vector3(-1,-.3f,2),Quaternion.identity),"Free foundation without collision");
            var plan=construction.Plans.Last();session.modules.SelectContext(plan.grid);
            Require(construction.Weld(plan.uid,.5f)&&world.Blocks.Count==built&&plan.progress>0&&plan.progress<1,"Partial welding consumes and installs actual components while remaining a blueprint");
            Require(world.Backpack.Get("component-plates")==plates-1&&construction.WorkStatus(plan.uid).Contains("1 / 4"),"Work display reports installed component count");
            Require(construction.Weld(plan.uid,1.5f)&&world.Blocks.Count==built+1&&world.Backpack.Get("component-plates")==plates-4,"100 percent weld creates the real operational block");
            Require(!construction.Weld(plan.uid,2),"Complete block cannot consume twice");Physics.SyncTransforms();
            Require(!construction.PlaceOnGrid("ArmorCube",plan.grid,Vector3Int.zero,0),"Occupied cell rejected");
            Require(construction.PlaceOnGrid("ArmorCube",plan.grid,Vector3Int.right,0),"Adjacent grid plan accepted");
            var partial=construction.Plans.Last();construction.Weld(partial.uid,.5f);
            Require(construction.Grind(partial.uid,2)&&construction.Plans.All(p=>p.uid!=partial.uid)&&world.Backpack.Get("component-plates")==plates-4,"Grinding an incomplete blueprint removes it and refunds only installed components");
            Require(construction.PlaceOnGrid("Refinery",plan.grid,Vector3Int.up,0),"Refinery blueprint uses its own two-cell volume");
            var expensive=construction.Plans.Last();string prior=Stock(session);construction.Weld(expensive.uid,2);
            Require(construction.Plans.Contains(expensive)&&world.Blocks.Count==built+1,"Missing refinery components cannot produce an operational machine");
            Require(construction.Grind(expensive.uid,2)&&Stock(session)==prior,"Partial expensive blueprint salvage conserves every resource");
            string uid=world.Blocks.Last().uid;
            Require(construction.Grind(uid,.5f)&&world.Blocks.Count==built&&construction.Plans.Any(p=>p.uid==uid&&p.progress<1),"First grinding progress disables the operational block and exposes its blueprint");
            Require(construction.Grind(uid,1.5f)&&world.Backpack.Get("component-plates")==plates&&!construction.Grind(uid),"Grinding to zero removes the block and refunds components to the player exactly once");
            Require(construction.PlaceFoundation("ArmorCube",new Vector3(-1,-.3f,2),Quaternion.identity),"An empty construction slot can be reused");
            var pending=construction.Plans.Last();construction.Weld(pending.uid,.5f);
            Require(session.modules.Rename(pending.grid,"MODULE TEST 04"),"Nearby looked-at/building module can be renamed");
            session.modules.SelectContext(0);var battery=world.Blocks.First(b=>b.type=="Battery");float charge=session.modules.Available(0);
            Require(charge>0&&session.modules.Toggle(battery.uid)&&session.modules.Available(0)==0,"Battery switch removes its charge from available module energy");
            Require(session.modules.Toggle(battery.uid)&&Mathf.Abs(session.modules.Available(0)-charge)<.00001f,"Battery OFF/ON preserves stored charge");
            var refinery=world.Blocks.First(b=>b.type=="Refinery");Require(session.modules.Toggle(refinery.uid)&&!session.modules.World(0).HasMachine("Refinery"),"System OFF removes refinery from operational production");session.modules.Toggle(refinery.uid);
            Require(!ReferenceEquals(session.modules.World(pending.grid).Cargo,session.modules.World(0).Cargo)&&ReferenceEquals(session.modules.World(pending.grid).Backpack,world.Backpack),"Separate module cargo domains share only the personal backpack");
            float mass=session.modules.DryMass(0),loaded=mass+session.modules.CargoMass(0);Require(mass>0&&loaded>mass,"Cargo and reserved production inputs contribute real mass");
            var body=session.motion.Root(0).GetComponent<Rigidbody>();Require(body.isKinematic,"Starter platform remains an anchored base");
            void Supply(string type){Require(world.Backpack.Add(world.BuildCosts(type)),"Isolated energy test component fixture fits backpack: "+type);}
            Supply("SolarPanel");Require(construction.PlaceFoundation("SolarPanel",new Vector3(4,3,2),Quaternion.identity),"Solar test foundation placed");
            var solarPlan=construction.Plans.Last();Require(construction.Weld(solarPlan.uid,2),"Actual solar panel welded");var solarBlock=world.Blocks.Single(b=>b.uid==solarPlan.uid);
            var solarObject=session.economy.Instances[solarBlock.uid];var oldRotation=solarObject.transform.rotation;Vector3 sunlight=-RenderSettings.sun.transform.forward;
            solarObject.transform.rotation=Quaternion.FromToRotation(Vector3.up,sunlight);Physics.SyncTransforms();
            Require(session.modules.SolarExposure(solarBlock)>.98f,"Solar generation uses actual panel orientation toward sun");
            var shade=GameObject.CreatePrimitive(PrimitiveType.Cube);shade.transform.position=solarObject.transform.position+sunlight*2;shade.transform.localScale=Vector3.one*3;Physics.SyncTransforms();
            Require(session.modules.SolarExposure(solarBlock)<.01f,"World occluder removes solar generation");Object.DestroyImmediate(shade);
            solarObject.transform.rotation=Quaternion.FromToRotation(Vector3.up,-sunlight);Physics.SyncTransforms();Require(session.modules.SolarExposure(solarBlock)==0,"Back-facing solar panel produces no power");solarObject.transform.rotation=oldRotation;Physics.SyncTransforms();
            Supply("Cargo");Require(construction.PlaceOnGrid("Cargo",solarPlan.grid,Vector3Int.right,0),"Local fuel cargo plan placed");var cargoPlan=construction.Plans.Last();Require(construction.Weld(cargoPlan.uid,2),"Local fuel cargo welded");
            Supply("Reactor");Require(construction.PlaceOnGrid("Reactor",solarPlan.grid,Vector3Int.right*2,0),"Reused asset reactor plan placed");var reactorPlan=construction.Plans.Last();Require(construction.Weld(reactorPlan.uid,2),"Reactor fully operational only after all parts welded");
            var powerWorld=session.modules.World(solarPlan.grid);Require(powerWorld.Cargo.Add(new[]{new VoidForge.Economy.ResourceAmount("ingot-uranium",.001m)}),"Isolated uranium fixture fits local cargo");
            decimal fuel=powerWorld.Cargo.Get("ingot-uranium");session.modules.Tick(.1f);
            Require(session.modules.Generation(solarPlan.grid)>=300&&powerWorld.Cargo.Get("ingot-uranium")<fuel,"Enabled reactor burns real local uranium for energy");
            session.modules.SelectContext(solarPlan.grid);session.modules.Toggle(reactorPlan.uid);session.modules.Toggle(solarPlan.uid);fuel=powerWorld.Cargo.Get("ingot-uranium");session.modules.Tick(.1f);
            Require(session.modules.Generation(solarPlan.grid)==0&&powerWorld.Cargo.Get("ingot-uranium")==fuel,"Reactor and solar OFF stop generation and fuel consumption");
            powerWorld.Cargo.TransferTo(world.Backpack,"ingot-uranium",fuel);
            Require(construction.Grind(reactorPlan.uid)&&construction.Grind(cargoPlan.uid)&&construction.Grind(solarPlan.uid),"Energy test machines dismantle without losing stock");
            session.modules.SelectContext(0);session.deck.Publish(true);hud.Show(DeckPage.Systems);
            Require(hud.Controls.ContainsKey("module-rename")&&hud.State.systems.Count==4&&hud.State.systems.All(s=>!s.name.Contains("ARMURE")),"Module tab lists only actual constructed systems and rename control");
            var surface=session.equipment.GetComponentInChildren<CyberdeckSurface>();WristHudAttachment.Attach(hud,surface);
            var attachment=hud.GetComponent<WristHudAttachment>();attachment.Refresh();
            Require(Vector3.Distance(hud.transform.position,surface.hudAnchor.position)<=.0002f&&hud.transform.parent==null,"Collapsed HUD stays 0.15 mm above the screen in world metres");
            var seat=session.equipment.GetComponentInChildren<WristHousingSeat>();seat.Refresh();Require(seat.MaxContactGap<=.0003f,"Full housing contact mesh follows its skinned sleeve anchors");
            Require(Mathf.Abs(Quaternion.Angle(session.equipment.astronaut.PalmCorrection,Quaternion.identity)-45)<.001f,"Anatomical palm frame compensates controller pitch by 45 degrees");
            foreach(var tool in session.equipment.tools)
            {
                session.equipment.SelectTool(tool);session.equipment.Grab(tool);
                var contact=GameObject.CreatePrimitive(PrimitiveType.Cube);contact.name="Isolated tool FX contact";contact.transform.position=tool.tip.position+tool.tip.forward*.3f;contact.transform.localScale=Vector3.one*.08f;Physics.SyncTransforms();
                for(int frame=0;frame<90;frame++)tool.Operate(1);
                var fx=tool.GetComponent<HandToolEffects>();
                Require(tool.IsOperating&&fx.Running&&tool.GetComponentsInChildren<ParticleSystem>().All(p=>p.main.loop&&p.isEmitting),"Held trigger maintains reused Arsenal loop: "+tool.kind);
                tool.Operate(0);Require(!tool.IsOperating&&!fx.Running,"Trigger release stops tool and particle emission: "+tool.kind);
                session.equipment.Release();Object.DestroyImmediate(contact);
            }
            hud.Show(DeckPage.Inventory);
        }
        static void Finish(string error)
        {
            if(error!=null)errors.Add(error);
            pendingError=error;phase=6;drainUntil=0;EditorApplication.isPlaying=false;
        }
        static void Complete()
        {
            EditorApplication.update-=Tick;
            var report=new Report { success=pendingError==null&&errors.Count==0,headsetTested=false,unity=Application.unityVersion,error=pendingError,
                errors=errors.ToArray(),checks=checks.ToArray(),save=SurvivalSession.SavePath,asteroids=64,excavations=savedEdits,colliderAdvanceMetres=finalDistance-initialDistance };
            Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(report,true));
            Application.logMessageReceived-=Log;
            EditorSettings.enterPlayModeOptionsEnabled=optionsEnabled;EditorSettings.enterPlayModeOptions=options;
            var standalone=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);if(restoreXR&&standalone!=null)standalone.InitManagerOnStart=xr;
            EditorApplication.delayCall+=()=>{Debug.Log("VF_SURVIVAL_VALIDATION "+JsonUtility.ToJson(report));EditorApplication.Exit(report.success?0:1);};
        }
    }
}
#endif
