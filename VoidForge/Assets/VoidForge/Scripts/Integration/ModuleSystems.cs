#if VF_LOCAL_ECONOMY
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoidForge.Economy;
using VoidForge.Player;
using VoidForge.UI.Cyberdeck;

namespace VoidForge.Integration
{
    [Serializable] public sealed class ModuleLedgerSave { public int grid; public string ledger; }
    [Serializable] public sealed class ModuleSystemState { public string uid; public bool enabled=true; public float charge; }

    // One cargo/production/energy domain per physical construction. The backpack is shared by reference.
    [DefaultExecutionOrder(-10)]
    public sealed class ModuleSystems : MonoBehaviour
    {
        public readonly List<ModuleSystemState> States=new List<ModuleSystemState>();
        readonly Dictionary<int,EconomyWorld> worlds=new Dictionary<int,EconomyWorld>();
        readonly Dictionary<int,float> movementEnergy=new Dictionary<int,float>();
        readonly Dictionary<string,float> actual=new Dictionary<string,float>();
        readonly Dictionary<int,float> bus=new Dictionary<int,float>();
        readonly Dictionary<int,float> generation=new Dictionary<int,float>(),load=new Dictionary<int,float>();
        SurvivalSession session;
        int context;
        float surveyAt;
        public int ContextId=>context;
        public EconomyWorld ContextWorld=>World(context);
        public string ContextName=>session.construction.Grids.Find(g=>g.id==context)?.name??"MODULE";
        public int Revision {get;private set;}
        public void Touch()=>Revision++;
        public static bool Structural(string type)=>type.StartsWith("Armor",StringComparison.Ordinal);
        public int GridOf(string uid)=>session.construction.Mappings.Find(m=>m.uid==uid)?.grid??0;
        public EconomyWorld World(int grid)=>worlds.TryGetValue(grid,out var world)?world:session.Ledger;
        public bool Access(int grid)
        {
            if(!worlds.TryGetValue(grid,out var world)||world.Count("Cargo")==0)return false;
            if(session.economy.Seated)return GridOf(session.economy.SeatedBlock.uid)==grid;
            return Camera.main!=null&&session.economy.Instances.Values.Any(go=>go.GetComponent<EconomyBlock>() is EconomyBlock b&&b.type=="Cargo"&&GridOf(b.uid)==grid&&Vector3.Distance(Camera.main.transform.position,go.transform.position)<=8);
        }
        public void Initialize(SurvivalSession owner,SurvivalSave saved)
        {
            session=owner;
            if(saved?.systems!=null)States.AddRange(saved.systems);
            if(States.Any(s=>s==null||string.IsNullOrEmpty(s.uid)||!float.IsFinite(s.charge)||s.charge<0)||States.Select(s=>s.uid).Distinct().Count()!=States.Count)
                throw new InvalidOperationException("Invalid saved module system state; save preserved.");
            Refresh();
            if(saved?.modules!=null)
            {
                if(saved.modules.Select(m=>m.grid).Distinct().Count()!=saved.modules.Length)throw new InvalidOperationException("Duplicate saved module ledger.");
                foreach(var entry in saved.modules)
                {
                    if(entry.grid==0||!worlds.ContainsKey(entry.grid))throw new InvalidOperationException("Invalid saved module ledger grid.");
                    var recovered=EconomyWorld.Restore(session.Ledger.Definition,JsonUtility.FromJson<EconomySave>(entry.ledger),session.Ledger.Backpack);
                    var expected=worlds[entry.grid].Blocks.OrderBy(b=>b.uid).Select(b=>JsonUtility.ToJson(b)).ToArray();
                    if(!expected.SequenceEqual(recovered.Blocks.OrderBy(b=>b.uid).Select(b=>JsonUtility.ToJson(b))))throw new InvalidOperationException("Module ledger block mismatch.");
                    worlds[entry.grid]=recovered;BindPower(entry.grid,recovered);
                }
            }
            if(saved?.systems==null)
            {
                // Version 1 had one energy reservoir. Split it across its actual primary-grid batteries.
                float remaining=session.Ledger.EnergyKwh;
                foreach(var b in World(0).Blocks.Where(b=>b.type=="Battery")){var state=State(b.uid);state.charge=Mathf.Min(remaining,session.Ledger.Definition.Block(b.type).batteryKwh);remaining-=state.charge;}
            }
            foreach(var s in States)
            {
                var b=session.Ledger.Blocks.Find(b=>b.uid==s.uid);
                if(b==null||Structural(b.type)||s.charge>session.Ledger.Definition.Block(b.type).batteryKwh+.0001f)throw new InvalidOperationException("Saved battery exceeds its capacity.");
            }
            if(worlds.Values.Any(w=>w.Cargo.Used>w.Cargo.Capacity))throw new InvalidOperationException("Saved cargo exceeds its owning module capacity; save preserved for migration.");
            UpdateEnergy();
        }
        ModuleSystemState State(string uid)
        {
            var state=States.Find(s=>s.uid==uid);
            if(state==null){state=new ModuleSystemState{uid=uid};States.Add(state);}return state;
        }
        public bool Enabled(string uid)=>States.Find(s=>s.uid==uid)?.enabled??true;
        public void Refresh()
        {
            foreach(var frame in session.construction.Grids)
            {
                if(string.IsNullOrWhiteSpace(frame.name))frame.name=frame.id==0?"BASE DE DÉPART":"MODULE "+frame.id;
                if(!worlds.TryGetValue(frame.id,out var world))
                {
                    world=new EconomyWorld(session.Ledger.Definition,session.Ledger.Backpack,frame.id==0?session.Ledger.Cargo:null,frame.id==0?session.Ledger.Assembly:null);
                    worlds.Add(frame.id,world);BindPower(frame.id,world);
                    if(frame.id==0){world.RefineryPower=session.Ledger.RefineryPower;world.RefineryPaused=session.Ledger.RefineryPaused;world.AssemblyPaused=session.Ledger.AssemblyPaused;world.PriorityOre=session.Ledger.PriorityOre;}
                }
                world.Blocks.Clear();world.Blocks.AddRange(session.Ledger.Blocks.Where(b=>GridOf(b.uid)==frame.id));
                world.Cargo.SetCapacity(1000m*world.Count("Cargo"));
                foreach(var block in world.Blocks.Where(b=>!Structural(b.type)))State(block.uid);
            }
            States.RemoveAll(s=>session.Ledger.Blocks.All(b=>b.uid!=s.uid));
            Revision++;
        }
        void BindPower(int grid,EconomyWorld world)
        {
            world.ExternalEnergyManaged=true;
            world.MachineAvailable=type=>world.Blocks.Any(b=>b.type==type&&Enabled(b.uid));
            world.EnergyConsumer=(type,amount)=>Consume(grid,type,amount);
        }
        public bool DrawPower(int grid,float amount)
        {if(!session.CanAct||!Consume(grid,"movement",amount))return false;movementEnergy[grid]=Get(movementEnergy,grid)+amount;return true;}
        bool Consume(int grid,string type,float amount)
        {
            var batteries=World(grid).Blocks.Where(b=>b.type=="Battery"&&Enabled(b.uid)).Select(b=>State(b.uid)).ToArray();
            if(batteries.Sum(s=>s.charge)+Get(bus,grid)+1e-8f<amount)return false;
            float direct=Mathf.Min(amount,Get(bus,grid));bus[grid]=Get(bus,grid)-direct;amount-=direct;
            foreach(var battery in batteries){float n=Mathf.Min(amount,battery.charge);battery.charge-=n;amount-=n;if(amount<=0)break;}
            float dt=Mathf.Min(Time.unscaledDeltaTime,.25f);
            if(dt>0)
            {
                float kw=LastConsumptionKw(type);load[grid]=Get(load,grid)+kw;
                var machine=World(grid).Blocks.FirstOrDefault(b=>b.type==type&&Enabled(b.uid));if(machine!=null)actual[machine.uid]=(actual.TryGetValue(machine.uid,out var previous)?previous:0)+kw;
            }
            return true;
        }
        static float LastConsumptionKw(string type)=>type=="Refinery"?120:type=="Assembler"?50:0;
        static float Get(Dictionary<int,float> values,int id)=>values.TryGetValue(id,out var value)?value:0;
        public bool Toggle(string uid)
        {
            var block=session.Ledger.Blocks.Find(b=>b.uid==uid);
            if(!session.CanAct||block==null||Structural(block.type)||GridOf(uid)!=context||!Near(context))return false;
            State(uid).enabled=!State(uid).enabled;Revision++;session.Save();session.deck?.Publish(true);return true;
        }
        public bool Near(int grid)=>session.economy.Seated?GridOf(session.economy.SeatedBlock.uid)==grid:
            Camera.main!=null&&Vector3.Distance(Camera.main.transform.position,session.construction.TargetForGrid(grid).position)<=12;
        public bool Rename(int grid,string value)
        {
            var frame=session.construction.Grids.Find(g=>g.id==grid);
            if(!session.CanAct||frame==null||!Near(grid)||string.IsNullOrWhiteSpace(value)||value.Length>32||value.Any(char.IsControl))return false;
            frame.name=value.Trim();Revision++;session.Save();session.deck?.Publish(true);return true;
        }
        public void SelectContext(int grid){if(worlds.ContainsKey(grid)&&context!=grid){context=grid;Revision++;}}
        void Survey()
        {
            if(session.economy.Seated){SelectContext(GridOf(session.economy.SeatedBlock.uid));return;}
            if(Camera.main==null)return;
            foreach(var hit in Physics.RaycastAll(Camera.main.transform.position,Camera.main.transform.forward,12,~0,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance))
            {
                var b=hit.collider.GetComponentInParent<EconomyBlock>();var p=hit.collider.GetComponentInParent<ConstructionPlanMarker>();
                if(b!=null){SelectContext(GridOf(b.uid));return;}
                if(p!=null){var plan=session.construction.Plans.Find(x=>x.uid==p.uid);if(plan!=null){SelectContext(plan.grid);return;}}
            }
            if(!Near(context))
            {
                var nearest=session.construction.Grids.OrderBy(g=>Vector3.Distance(Camera.main.transform.position,session.construction.TargetForGrid(g.id).position)).FirstOrDefault();
                if(nearest!=null)SelectContext(nearest.id);
            }
        }
        public float SolarExposure(BuiltBlock block)
        {
            if(!session.economy.Instances.TryGetValue(block.uid,out var go))return 0;
            var sun=RenderSettings.sun; if(sun==null||!sun.enabled)return 0;
            Vector3 direction=-sun.transform.forward;float exposure=Mathf.Max(0,Vector3.Dot(go.transform.up,direction));
            if(exposure<=0)return 0;
            int visible=0;
            foreach(float x in new[]{-.3f,0,.3f})
            {
                Vector3 origin=go.transform.TransformPoint(new Vector3(x,.52f,0));
                bool blocked=Physics.OverlapSphere(origin,.003f,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.transform.IsChildOf(go.transform))||
                    Physics.RaycastAll(origin,direction,1000,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.collider.transform.IsChildOf(go.transform));
                if(!blocked)visible++;
            }
            return exposure*visible/3;
        }
        public void Tick(float seconds)
        {
            if(Time.unscaledTime>=surveyAt){surveyAt=Time.unscaledTime+.2f;Survey();}
            float dt=Mathf.Clamp(seconds,0,.25f);
            foreach(var pair in worlds)
            {
                int grid=pair.Key;var world=pair.Value;world.ReadOnly=!session.CanAct;
                if(world.ReadOnly)continue;
                foreach(var block in world.Blocks)actual[block.uid]=0;
                generation[grid]=0;
                foreach(var panel in world.Blocks.Where(b=>b.type=="SolarPanel"&&Enabled(b.uid)))
                {float kw=world.Definition.Block(panel.type).solarKw*SolarExposure(panel);generation[grid]+=kw;actual[panel.uid]=-kw;}
                // Reactor support consumes uranium only if a compatible reactor is present in the catalogue.
                foreach(var reactor in world.Blocks.Where(b=>b.type=="Reactor"&&Enabled(b.uid)))
                {
                    decimal fuel=(decimal)(dt*.0001f);
                    if(fuel>0&&world.Cargo.Take(new[]{new ResourceAmount("ingot-uranium",fuel)})){generation[grid]+=300;actual[reactor.uid]=-300;}
                }
                float incoming=generation[grid]*dt/3600;
                foreach(var b in world.Blocks.Where(b=>b.type=="Battery"&&Enabled(b.uid)))
                {var s=State(b.uid);float n=Mathf.Min(incoming,world.Definition.Block(b.type).batteryKwh-s.charge);s.charge+=n;incoming-=n;}
                bus[grid]=incoming;
                load[grid]=dt>0?Get(movementEnergy,grid)*3600/dt:0;movementEnergy[grid]=0;
                foreach(var b in world.Blocks.Where(b=>!Structural(b.type)&&Enabled(b.uid)&&b.type!="Battery"&&b.type!="SolarPanel"&&b.type!="Reactor"))
                {float idle=IdleKw(b.type);if(idle>0&&Consume(grid,"idle",idle*dt/3600)){load[grid]+=idle;actual[b.uid]+=idle;}}
                world.RefineryPower=world.Blocks.Any(b=>b.type=="Refinery"&&Enabled(b.uid));
                world.EnergyKwh=Available(grid);world.Tick(dt);
                world.EnergyKwh=Available(grid);
            }
            session.Ledger.EnergyKwh=Available(0);
        }
        static float IdleKw(string type)=>type=="Refinery"?1:type=="Assembler"?.5f:type=="Thruster"?.1f:type=="Gyroscope"?.05f:.01f;
        void UpdateEnergy(){foreach(var pair in worlds)pair.Value.EnergyKwh=Available(pair.Key);session.Ledger.EnergyKwh=Available(0);}
        public float Available(int grid)=>World(grid).Blocks.Where(b=>b.type=="Battery"&&Enabled(b.uid)).Sum(b=>State(b.uid).charge);
        public float Capacity(int grid)=>World(grid).Blocks.Where(b=>b.type=="Battery"&&Enabled(b.uid)).Sum(b=>World(grid).Definition.Block(b.type).batteryKwh);
        public float Generation(int grid)=>Get(generation,grid);
        public float Load(int grid)=>Get(load,grid);
        public ModuleLedgerSave[] SaveLedgers()
        {
            var primary=World(0);session.Ledger.RefineryPower=primary.RefineryPower;session.Ledger.RefineryPaused=primary.RefineryPaused;session.Ledger.AssemblyPaused=primary.AssemblyPaused;session.Ledger.PriorityOre=primary.PriorityOre;
            return worlds.Where(p=>p.Key!=0).Select(p=>new ModuleLedgerSave{grid=p.Key,ledger=JsonUtility.ToJson(p.Value.Snapshot())}).ToArray();
        }
        public List<DeckSystem> Snapshot(int grid)=>World(grid).Blocks.Where(b=>!Structural(b.type)).Select(b=>new DeckSystem{uid=b.uid,name=World(grid).Definition.Block(b.type).name.ToUpperInvariant(),enabled=Enabled(b.uid),status=!Enabled(b.uid)?"ARRÊT":b.type=="SolarPanel"?Mathf.RoundToInt(SolarExposure(b)*100)+" % SOLEIL":b.type=="Battery"?State(b.uid).charge.ToString("0.00")+" kWh":Available(grid)>0||Generation(grid)>0?"EN MARCHE":"SANS ÉNERGIE",kilowatts=Enabled(b.uid)&&actual.TryGetValue(b.uid,out var watts)?watts:0}).ToList();
        // Original compact-kit mass profile: resource quantities in kg; component unit masses derived from ingredients.
        public float ItemMass(string id,decimal quantity)
        {
            var def=session.Ledger.Definition;var item=def.Item(id);if(item.unit=="kg")return (float)quantity;
            var recipe=def.recipes.FirstOrDefault(r=>r.outputs.Any(a=>a.id==id)&&r.machine=="assembler");
            float per=recipe==null?1:recipe.inputs.Sum(a=>ItemMass(a.id,a.Value))/(float)recipe.outputs.First(a=>a.id==id).Value;
            return per*(float)quantity;
        }
        public float CargoMass(int grid)=>World(grid).Cargo.Snapshot().Sum(a=>ItemMass(a.id,a.Value))+World(grid).Assembly.Sum(j=>j.reservedPerUnit.Sum(a=>ItemMass(a.id,a.Value*j.remaining)));
        public float DryMass(int grid)=>World(grid).Blocks.Sum(b=>World(grid).BuildCosts(b.type).Sum(a=>ItemMass(a.id,a.Value)))+
            session.construction.Plans.Where(p=>p.grid==grid).Sum(p=>(p.installed??Array.Empty<ResourceAmount>()).Sum(a=>ItemMass(a.id,a.Value)));
    }
}
#endif
