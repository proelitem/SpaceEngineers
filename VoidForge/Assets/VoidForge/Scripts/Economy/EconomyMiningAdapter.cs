using System.Collections;
using UnityEngine;
#if VF_LOCAL_DIGGER
using System;
using System.Reflection;
using System.Threading.Tasks;
using Digger.Modules.Core.Sources;
using VoidForge.Integration;
using VoidForge.World;
#endif

namespace VoidForge.Economy
{
    // Uses the licensed Digger API locally; no vendor source is changed or copied.
    public sealed class EconomyMiningAdapter:MonoBehaviour
    {
#if VF_LOCAL_DIGGER
        MiningPrototype prototype;
        EconomyRuntime runtime;
        EquipmentRig equipment;
        bool busy,ready;
        float nextDig;
        public bool Ready=>ready;
        IEnumerator Start()
        {
            runtime=GetComponent<EconomyRuntime>();prototype=FindFirstObjectByType<MiningPrototype>();equipment=FindFirstObjectByType<EquipmentRig>();
            if(prototype==null)yield break;
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var testing=typeof(MiningPrototype).GetField("_testing",flags);var editing=typeof(MiningPrototype).GetField("_busy",flags);
            yield return null;
            while(prototype!=null&&((bool)testing.GetValue(prototype)||(bool)editing.GetValue(prototype)||prototype.digger.IsRunningAsync))yield return null;
            if(prototype==null)yield break;
            prototype.enabled=false;
            var replay=Replay();while(!replay.IsCompleted)yield return null;
            if(replay.IsFaulted){Debug.LogException(replay.Exception);runtime.MarkMiningFault();yield break;}
            ready=true;
            if(prototype.label!=null)prototype.label.text="GISEMENT DE PIERRE\nForeuse • collecte réelle dans le sac\nStocks, gisements et trous sauvegardés";
        }
        async Task Replay()
        {
            foreach(var edit in runtime.World.Excavations)
            {
                await Awaitable.MainThreadAsync();
                if(this==null||prototype==null)return;
                await prototype.digger.ModifyAsync(new Vector3(edit.x,edit.y,edit.z),BrushType.Sphere,ActionType.Dig,0,1,.75f);
            }
            if(prototype!=null)prototype.HideSupportTerrain();
        }
        void Update()
        {
            if(!ready||busy||runtime==null||runtime.NetworkLocked||!Application.isFocused||equipment==null||!equipment.HasTrackedRightHand||prototype.digger.IsRunningAsync)return;
            var tool=equipment.Held;if(tool==null||tool.kind!=HandToolKind.MiningDrill||!tool.IsOperating||tool.tip==null||Time.unscaledTime<nextDig||runtime.World.Excavations.Count>=4096)return;
            if(Physics.Raycast(new Ray(tool.tip.position,tool.tip.forward),out var hit,1,~0,QueryTriggerInteraction.Ignore)&&hit.collider.transform.IsChildOf(prototype.transform))
            {nextDig=Time.unscaledTime+.35f;_ = Dig(hit.point-hit.normal*.15f);}
        }
        public async Task Dig(Vector3 point)
        {
            if(busy||runtime.NetworkLocked||prototype.digger.IsRunningAsync)return;
            busy=true;double removed=0;string id=Guid.NewGuid().ToString("N");
            try
            {
                await Awaitable.MainThreadAsync();
                await prototype.digger.ModifyAsync(point,BrushType.Sphere,ActionType.Dig,0,1,.75f,callback:result=>removed=result.RemovedMatterQuantity);
                await Awaitable.MainThreadAsync();
                if(this==null||prototype==null||runtime==null)return;
                prototype.HideSupportTerrain();
                // Digger reports SDF matter change, not kilograms. Explicit prototype calibration.
                if(removed>0&&!double.IsNaN(removed)&&!double.IsInfinity(removed))runtime.World.RecordExcavation(id,point.x,point.y,point.z,decimal.Round((decimal)Math.Min(400000,removed)*2.5m,6));
                runtime.Save();runtime.PublishSession();
            }
            catch(Exception e){Debug.LogException(e);}
            finally{busy=false;}
        }
        void OnDisable(){ready=false;}
#else
        public bool Ready=>true;
#endif
    }
}
