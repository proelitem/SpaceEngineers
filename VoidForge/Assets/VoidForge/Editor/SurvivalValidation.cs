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
    public static class SurvivalValidation
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
        static string output=>Path.GetFullPath("Hermes_Works/Preuves/INTEGRATION_03/play-validation.json");

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
                    Require(session.Ledger.Blocks.Count==savedBlocks&&session.construction.Plans.Count==savedPlans,"Constructed blocks and unfinished holograms survive reload");
                    Physics.SyncTransforms();Require(Physics.Raycast(probe,out var hit,30,~0,QueryTriggerInteraction.Ignore)&&Mathf.Abs(hit.distance-finalDistance)<.08f,"Saved excavation restored in collider geometry");
                    Finish(null);
                }
            }
            catch(Exception error){Finish(error.ToString());}
        }
        static void ValidateConstruction(SurvivalSession session)
        {
            var construction=session.construction;string before=Stock(session);int built=session.Ledger.Blocks.Count;
            Require(construction.PlaceFoundation("ArmorCube",new Vector3(-1,-.3f,2),Quaternion.identity),"First foundation can be placed freely in collision-free space");
            Require(session.Ledger.Blocks.Count==built&&Stock(session)==before,"Placing a blue plan neither constructs a block nor consumes materials");
            var plan=construction.Plans.Last();
            Require(construction.Weld(plan.uid,1)&&session.Ledger.Blocks.Count==built,"Unfinished weld remains a hologram");
            Require(construction.Weld(plan.uid,1.1f)&&session.Ledger.Blocks.Count==built+1&&Stock(session)!=before,"Completed weld consumes real components exactly once");
            Require(!construction.Weld(plan.uid,2),"Completed plan cannot consume components twice");
            Physics.SyncTransforms();
            Require(!construction.PlaceOnGrid("ArmorCube",plan.grid,Vector3Int.zero,0),"Occupied grid cell rejected");
            Require(!construction.PlaceOnGrid("ArmorCube",plan.grid,new Vector3Int(4,0,0),0),"Nonadjacent block rejected");
            Require(construction.PlaceOnGrid("ArmorCube",plan.grid,Vector3Int.right,0),"Second plan snaps to the foundation grid");
            var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.position=new Vector3(-2,-.3f,2);Physics.SyncTransforms();
            Require(!construction.PlaceOnGrid("ArmorCube",plan.grid,Vector3Int.left,0),"External world collision rejects placement");Object.DestroyImmediate(obstacle);
            Require(construction.PlaceOnGrid("Refinery",plan.grid,new Vector3Int(1,1,0),0),"Expensive block can be planned before components are available");
            var expensive=construction.Plans.Last();before=Stock(session);
            Require(!construction.Weld(expensive.uid,3)&&Stock(session)==before,"Insufficient materials leave the blue plan and stock unchanged");
            var block=session.Ledger.Blocks.Last();
            Require(construction.Grind(block.uid)&&!construction.Grind(block.uid),"Grinding returns components once and rejects duplicate salvage");
            Require(construction.Plans.Count==2,"Unwelded plans remain independently stored");
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
