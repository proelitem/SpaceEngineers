#if VF_LOCAL_DIGGER && VF_LOCAL_ECONOMY
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using VoidForge.Economy;
using VoidForge.Integration;
using VoidForge.World;

namespace VoidForge.Editor
{
    // Run against a read-only COPY of the Quest world. Never reset or overwrite the device save.
    public static class Recette04MigrationValidation
    {
        [Serializable] sealed class Report {public bool success;public string error;public int originalFormat,blocks,migratedBlocks,plans,excavations;public bool starterPowerProvisioned;public string[] checks,errors;}
        static readonly List<string> checks=new List<string>(),errors=new List<string>();
        static SurvivalSave original;static EconomySave ledger;static string stock,save;
        static int phase;static double deadline,drain;
        static bool optionsEnabled,xr,restoreXR;static EnterPlayModeOptions options;
        static string fault;
        public static void RunBatch()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-vfSurvivalSave");
            if(index<0||index+1>=args.Length)throw new InvalidOperationException("Pass a new -vfSurvivalSave under Library/SurvivalValidation.");
            save=Path.GetFullPath(args[index+1]);string root=Path.GetFullPath("Library/SurvivalValidation")+Path.DirectorySeparatorChar;
            if(!save.StartsWith(root,StringComparison.OrdinalIgnoreCase)||File.Exists(save))throw new InvalidOperationException("Migration test must use a NEW isolated save path.");
            string source=Path.GetFullPath("Library/Recette04Publication/QuestBefore/survival-world-v1.json");
            original=JsonUtility.FromJson<SurvivalSave>(File.ReadAllText(source));ledger=JsonUtility.FromJson<EconomySave>(original.ledger);
            Directory.CreateDirectory(Path.GetDirectoryName(save));File.Copy(source,save);
            optionsEnabled=EditorSettings.enterPlayModeOptionsEnabled;options=EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            var standalone=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);restoreXR=standalone!=null;if(restoreXR){xr=standalone.InitManagerOnStart;standalone.InitManagerOnStart=false;}
            EditorSceneManager.OpenScene(MainSceneBuilder.ScenePath);checks.Clear();errors.Clear();phase=0;fault=null;drain=0;deadline=EditorApplication.timeSinceStartup+300;
            SurvivalSession.WorldReady+=FreezeProduction;
            Application.logMessageReceived+=Log;EditorApplication.update+=Tick;EditorApplication.isPlaying=true;
        }
        static void FreezeProduction(SurvivalSession session)=>session.enabled=false;
        static void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text+"\n"+stack);}
        static void Check(bool condition,string name){if(!condition)throw new InvalidOperationException(name);checks.Add(name);}
        [Serializable] sealed class Stocks {public ResourceAmount[] bag,cargo;}
        static string Stock(EconomyWorld world)=>JsonUtility.ToJson(new Stocks{bag=world.Backpack.Snapshot(),cargo=world.Cargo.Snapshot()});
        static void Tick()
        {
            if(phase==3)
            {
                if(EditorApplication.isPlaying)return;if(drain==0)drain=EditorApplication.timeSinceStartup+2;if(EditorApplication.timeSinceStartup<drain)return;Complete();return;
            }
            if(EditorApplication.timeSinceStartup>deadline){Fail("Migration/replay timed out after 300 seconds.");return;}
            if(phase==1&&!EditorApplication.isPlaying){phase=2;deadline=EditorApplication.timeSinceStartup+300;EditorApplication.isPlaying=true;return;}
            var session=SurvivalSession.Active;var field=UnityEngine.Object.FindFirstObjectByType<MinableAsteroidField>();
            if(!EditorApplication.isPlaying||session==null||!session.Ready||field==null||field.Busy||!field.Asteroids[0].voxelReady)return;
            try
            {
                field.enabled=false;
                Check(!session.Faulted&&!field.Faulted,"World restoration and voxel replay report no fault");
                if(phase==0)
                {
                    Check(SurvivalSession.Loaded.format==original.format,"Original Quest envelope actually loaded");
                    Check(ledger.blocks.All(b=>session.Ledger.Blocks.Any(current=>current.uid==b.uid&&current.type==b.type&&current.cell.Equals(b.cell)&&current.rotation==b.rotation)),"All original constructed block identities preserved");
                    var additions=session.Ledger.Blocks.Where(b=>ledger.blocks.All(previous=>previous.uid!=b.uid)).ToArray();
                    Check(additions.Length==0||additions.Length==1&&additions[0].uid=="vf-recette04-starter-solar"&&additions[0].type=="SolarPanel","Only the explicit one-time starter solar may be added to a depleted legacy base");
                    Check(session.construction.Plans.Select(p=>p.uid).OrderBy(x=>x).SequenceEqual(original.plans.Select(p=>p.uid).OrderBy(x=>x)),"Every existing blueprint identity preserved");
                    Check(session.Ledger.Backpack.Snapshot().Select(a=>a.id+":"+a.amount).SequenceEqual(ledger.backpack.OrderBy(a=>a.id,StringComparer.Ordinal).Select(a=>a.id+":"+a.amount)),"Personal stock preserved exactly by migration");
                    Check(session.Ledger.Cargo.Snapshot().Select(a=>a.id+":"+a.amount).SequenceEqual(ledger.cargo.OrderBy(a=>a.id,StringComparer.Ordinal).Select(a=>a.id+":"+a.amount)),"Primary cargo stock preserved exactly by migration");
                    Check(session.Excavations.Select(e=>e.uid).OrderBy(x=>x).SequenceEqual(original.excavations.Select(e=>e.uid).OrderBy(x=>x)),"All existing excavation identities preserved without duplicate credit");
                    Check(session.construction.Grids.Count==original.grids.Length,"Existing construction grids preserved");
                    Check(original.format!=1||session.construction.Plans.All(p=>p.progress==0&&(p.installed==null||p.installed.Length==0)),"Version 1 unpaid welding progress migrates to unbuilt, without inventing components");
                    session.deck?.Publish(true);Recette04Capture.Render(session);
                    session.Save();stock=Stock(session.Ledger);
                    var migrated=JsonUtility.FromJson<SurvivalSave>(File.ReadAllText(save));Check(migrated.format==2&&migrated.systems!=null&&migrated.modules!=null,"Atomic format 2 save includes per-module state");
                    Check(additions.Length==0||migrated.starterPowerProvisioned,"Starter solar grant has a durable migration marker");
                    phase=1;EditorApplication.isPlaying=false;return;
                }
                if(phase==2)
                {
                    Check(SurvivalSession.Loaded.format==2&&Stock(session.Ledger)==stock,"Second load of migrated save preserves all player and cargo stock");
                    Check(session.Excavations.Count==original.excavations.Length&&session.construction.Plans.Count==original.plans.Length,"Second voxel replay and blueprint restore preserve counts");
                    Check(session.Ledger.Blocks.Count(b=>b.uid=="vf-recette04-starter-solar")<=1,"Reload cannot duplicate the migration solar or its components");
                    phase=3;EditorApplication.isPlaying=false;
                }
            }
            catch(Exception e){Fail(e.ToString());}
        }
        static void Fail(string error){fault=error;phase=3;drain=0;EditorApplication.isPlaying=false;}
        static void Complete()
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=Log;SurvivalSession.WorldReady-=FreezeProduction;
            EditorSettings.enterPlayModeOptionsEnabled=optionsEnabled;EditorSettings.enterPlayModeOptions=options;
            var standalone=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);if(restoreXR)standalone.InitManagerOnStart=xr;
            var persisted=JsonUtility.FromJson<SurvivalSave>(File.ReadAllText(save));var persistedLedger=JsonUtility.FromJson<EconomySave>(persisted.ledger);
            var report=new Report{migratedBlocks=persistedLedger.blocks.Length,starterPowerProvisioned=persisted.starterPowerProvisioned,success=fault==null&&errors.Count==0,error=fault,originalFormat=original.format,blocks=ledger.blocks.Length,plans=original.plans.Length,excavations=original.excavations.Length,checks=checks.ToArray(),errors=errors.ToArray()};
            Directory.CreateDirectory("Hermes_Works/Preuves/INTEGRATION_04");File.WriteAllText("Hermes_Works/Preuves/INTEGRATION_04/migration-validation.json",JsonUtility.ToJson(report,true));
            EditorApplication.delayCall+=()=>{Debug.Log("VF_MIGRATION04 "+JsonUtility.ToJson(report));EditorApplication.Exit(report.success?0:1);};
        }
    }
}
#endif
