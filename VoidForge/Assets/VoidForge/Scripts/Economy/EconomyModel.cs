using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VoidForge.Economy
{
    [Serializable] public sealed class ResourceAmount { public string id, amount; public decimal Value => decimal.Parse(amount, CultureInfo.InvariantCulture); public ResourceAmount() { } public ResourceAmount(string id, decimal value) { this.id = id; amount = value.ToString(CultureInfo.InvariantCulture); } }
    [Serializable] public sealed class ResourceDefinition { public string id, name, icon, family, unit, litres; public decimal Litres => decimal.Parse(litres, CultureInfo.InvariantCulture); }
    [Serializable] public sealed class ProductionRecipe { public string id, name, icon, machine; public ResourceAmount[] inputs, outputs; public float seconds; }
    [Serializable] public sealed class BuildDefinition { public string id, name, icon; public int x = 1, y = 1, z = 1; public ResourceAmount[] components; public float batteryKwh, solarKw; }
    [Serializable] public sealed class EconomyDefinition
    {
        public string version, sourceProfile;
        public int assemblerEfficiency = 1;
        public ResourceDefinition[] items;
        public ProductionRecipe[] recipes;
        public BuildDefinition[] blocks;
        public void Validate()
        {
            if (assemblerEfficiency != 1 && assemblerEfficiency != 3 && assemblerEfficiency != 10) throw new InvalidOperationException("Invalid assembly efficiency");
            if (items == null || items.Length == 0 || recipes == null || blocks == null) throw new InvalidOperationException("Incomplete economy catalogue");
            if (items.Select(x => x.id).Distinct().Count() != items.Length || recipes.Select(x => x.id).Distinct().Count() != recipes.Length || blocks.Select(x => x.id).Distinct().Count() != blocks.Length) throw new InvalidOperationException("Duplicate catalogue ID");
            foreach (var item in items) if (string.IsNullOrEmpty(item.id) || item.Litres <= 0) throw new InvalidOperationException("Invalid item");
            foreach (var recipe in recipes)
            {
                ValidateAmounts(recipe.inputs); ValidateAmounts(recipe.outputs);
                if (recipe.seconds <= 0 || float.IsNaN(recipe.seconds) || float.IsInfinity(recipe.seconds)) throw new InvalidOperationException("Invalid recipe duration");
            }
            foreach (var block in blocks) { ValidateAmounts(block.components); if (block.x < 1 || block.y < 1 || block.z < 1 || block.x * block.y * block.z > 128) throw new InvalidOperationException("Invalid block bounds"); }
        }
        void ValidateAmounts(ResourceAmount[] amounts)
        {
            if (amounts == null || amounts.Length == 0 || amounts.Select(x => x.id).Distinct().Count() != amounts.Length) throw new InvalidOperationException("Invalid ingredients");
            foreach (var x in amounts) if (Item(x.id) == null || x.Value <= 0 || (Item(x.id).unit == "u" && x.Value != decimal.Truncate(x.Value))) throw new InvalidOperationException("Unknown or invalid ingredient " + x.id);
        }
        public ResourceDefinition Item(string id) => Array.Find(items, x => x.id == id);
        public ProductionRecipe Recipe(string id) => Array.Find(recipes, x => x.id == id);
        public BuildDefinition Block(string id) => Array.Find(blocks, x => x.id == id);
        public ResourceAmount[] Inputs(ProductionRecipe recipe)
        {
            int efficiency = recipe.machine == "assembler" ? assemblerEfficiency : 1;
            // One micro-kg upward rounding per unit, then multiply the batch. Split queues cost the same.
            return recipe.inputs.Select(x => new ResourceAmount(x.id, decimal.Ceiling(x.Value / efficiency * 1000000m) / 1000000m)).ToArray();
        }
    }

    public sealed class ResourceStock
    {
        readonly EconomyDefinition definition;
        readonly Dictionary<string, decimal> amounts = new Dictionary<string, decimal>(StringComparer.Ordinal);
        public decimal Capacity { get; private set; }
        public decimal Used => amounts.Sum(x => x.Value * definition.Item(x.Key).Litres);
        public ResourceStock(EconomyDefinition definition, decimal capacity) { this.definition = definition; SetCapacity(capacity); }
        public void SetCapacity(decimal value) { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); Capacity = value; }
        public decimal Get(string id) => amounts.TryGetValue(id, out var amount) ? amount : 0;
        public decimal RoomFor(string id)
        {
            var item = definition.Item(id); if (item == null) return 0;
            decimal room = Math.Max(0, (Capacity - Used) / item.Litres);
            return item.unit == "u" ? decimal.Floor(room) : decimal.Floor(room * 1000000m) / 1000000m;
        }
        public bool CanAdd(ResourceAmount[] items, int count = 1)
        {
            if (!Valid(items, count)) return false;
            return Used + items.Sum(x => x.Value * count * definition.Item(x.id).Litres) <= Capacity;
        }
        public bool CanTake(ResourceAmount[] items, int count = 1) => Valid(items, count) && items.All(x => Get(x.id) >= x.Value * count);
        bool Valid(ResourceAmount[] items, int count) => count > 0 && count <= 100000 && items != null && items.Length > 0 && items.Select(x => x.id).Distinct().Count() == items.Length && items.All(x => definition.Item(x.id) != null && x.Value > 0 && (definition.Item(x.id).unit != "u" || decimal.Truncate(x.Value) == x.Value));
        public bool Add(ResourceAmount[] items, int count = 1)
        {
            if (!CanAdd(items, count)) return false;
            foreach (var x in items) amounts[x.id] = Get(x.id) + x.Value * count; return true;
        }
        public bool Take(ResourceAmount[] items, int count = 1)
        {
            if (!CanTake(items, count)) return false;
            foreach (var x in items) { decimal left = Get(x.id) - x.Value * count; if (left == 0) amounts.Remove(x.id); else amounts[x.id] = left; } return true;
        }
        public int Maximum(ResourceAmount[] input) => input == null || input.Length == 0 ? 0 : (int)Math.Min(999, input.Min(x => decimal.Floor(Get(x.id) / x.Value)));
        public bool TransferTo(ResourceStock target, string id, decimal requested)
        {
            if (target == null || target == this || definition.Item(id) == null || requested <= 0) return false;
            decimal n = Math.Min(requested, Math.Min(Get(id), target.RoomFor(id)));
            if (n <= 0 || (definition.Item(id).unit == "u" && n != decimal.Truncate(n))) return false;
            var transfer = new[] { new ResourceAmount(id, n) };
            if (!target.CanAdd(transfer) || !CanTake(transfer)) return false;
            Take(transfer); target.Add(transfer); return true;
        }
        public ResourceAmount[] Snapshot() => amounts.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => new ResourceAmount(x.Key, x.Value)).ToArray();
        public void Restore(ResourceAmount[] saved)
        {
            var next = new ResourceStock(definition, Capacity);
            if (saved != null && saved.Length > 0 && !next.Add(saved)) throw new InvalidOperationException("Invalid or over-capacity saved stock");
            amounts.Clear(); foreach (var x in next.amounts) amounts.Add(x.Key, x.Value);
        }
    }

    [Serializable] public struct GridCell : IEquatable<GridCell>
    {
        public int x, y, z;
        public GridCell(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
        public bool Equals(GridCell v) => x == v.x && y == v.y && z == v.z;
        public override bool Equals(object obj) => obj is GridCell v && Equals(v);
        public override int GetHashCode() => unchecked((x * 397 ^ y) * 397 ^ z);
        public static GridCell operator +(GridCell a, GridCell b) => new GridCell(a.x+b.x,a.y+b.y,a.z+b.z);
        public static GridCell operator *(GridCell a, int n) => new GridCell(a.x*n,a.y*n,a.z*n);
    }
    public static class GridRotations
    {
        public struct Basis { public GridCell right, up, forward; public GridCell Apply(GridCell p) => right*p.x+up*p.y+forward*p.z; }
        public static readonly Basis[] All = Create();
        static Basis[] Create()
        {
            var axes = new[] {new GridCell(1,0,0),new GridCell(0,1,0),new GridCell(0,0,1),new GridCell(-1,0,0),new GridCell(0,-1,0),new GridCell(0,0,-1)};
            var result = new List<Basis> { new Basis { right=axes[0],up=axes[1],forward=axes[2] } };
            foreach (var up in axes) foreach (var forward in axes)
            {
                if (up.x*forward.x+up.y*forward.y+up.z*forward.z != 0) continue;
                var right = new GridCell(up.y*forward.z-up.z*forward.y,up.z*forward.x-up.x*forward.z,up.x*forward.y-up.y*forward.x);
                if (right.Equals(axes[0]) && up.Equals(axes[1]) && forward.Equals(axes[2])) continue;
                result.Add(new Basis { right=right,up=up,forward=forward });
            }
            return result.ToArray();
        }
    }
    [Serializable] public sealed class BuiltBlock { public string uid, type; public GridCell cell; public int rotation; public BuiltBlock() { } public BuiltBlock(string type, GridCell cell, int rotation) { uid=Guid.NewGuid().ToString("N");this.type=type;this.cell=cell;this.rotation=rotation; } }
    [Serializable] public sealed class ProductionJob { public string uid, recipe; public int remaining; public float progress; public ResourceAmount[] reservedPerUnit; }
    [Serializable] public sealed class DepositState { public string id, item, remaining; public bool loose; public float x,y,z; }
    [Serializable] public sealed class Excavation { public string id; public float x,y,z; }
    [Serializable] public sealed class EconomySave
    {
        public int format=1;
        public string definitionVersion;
        public int assemblerEfficiency;
        public ResourceAmount[] backpack, cargo;
        public BuiltBlock[] blocks;
        public ProductionJob[] assembly;
        public DepositState[] deposits;
        public float energyKwh;
        public bool refineryPower=true, refineryPause, assemblyPause;
        public string priorityOre;
        public string[] committedMining;
        public Excavation[] excavations;
    }

    // Pure deterministic ledger: commands are the only writers; UI consumes snapshots.
    public sealed class EconomyWorld
    {
        public readonly EconomyDefinition Definition;
        public readonly ResourceStock Backpack, Cargo;
        public readonly List<BuiltBlock> Blocks = new List<BuiltBlock>();
        public readonly List<ProductionJob> Assembly = new List<ProductionJob>();
        public readonly Dictionary<string, DepositState> Deposits = new Dictionary<string, DepositState>();
        public readonly List<Excavation> Excavations=new List<Excavation>();
        readonly HashSet<string> committedMining = new HashSet<string>();
        public bool RefineryPower=true, RefineryPaused, AssemblyPaused, ReadOnly;
        public string PriorityOre, Status;
        public float EnergyKwh, RefineryProgress;
        public bool OutputBlocked {get; private set;}
        public string RefiningRecipe {get; private set;}
        public event Action Changed;
        public int Revision {get;private set;}
        public EconomyWorld(EconomyDefinition definition)
        {
            definition.Validate();Definition=definition;Backpack=new ResourceStock(definition,400);Cargo=new ResourceStock(definition,1000);
        }
        void Publish() {Revision++;Changed?.Invoke();}
        public int Count(string type) => Blocks.Count(x=>x.type==type);
        public float MaxEnergy => Blocks.Sum(b=>Definition.Block(b.type).batteryKwh);
        public float SolarKw => Blocks.Sum(b=>Definition.Block(b.type).solarKw);
        public decimal AssemblyReservedLitres => Assembly.Sum(j=>j.remaining*j.reservedPerUnit.Sum(x=>x.Value*Definition.Item(x.id).Litres));
        public bool HasMachine(string type) => Count(type)>0 && Count("Cargo")>0;
        public ResourceAmount[] BuildCosts(string type) => Definition.Block(type)?.components;
        public decimal ConstructionStock(string id,bool cargoAccess) => Backpack.Get(id)+(cargoAccess?Cargo.Get(id):0);
        public bool CanAfford(string type,bool cargoAccess) => BuildCosts(type) is ResourceAmount[] costs && costs.All(x=>ConstructionStock(x.id,cargoAccess)>=x.Value);
        public IEnumerable<GridCell> Cells(BuiltBlock block)
        {
            var definition=Definition.Block(block.type);
            if(definition==null||block.rotation<0||block.rotation>=24)yield break;
            var rotation=GridRotations.All[block.rotation];
            for(int x=0;x<definition.x;x++)for(int y=0;y<definition.y;y++)for(int z=0;z<definition.z;z++)yield return block.cell+rotation.Apply(new GridCell(x,y,z));
        }
        public BuiltBlock At(GridCell cell) => Blocks.Find(b=>Cells(b).Contains(cell));
        public bool CanPlace(string type,GridCell cell,int rotation,bool requireConnection=true)
        {
            if(Definition.Block(type)==null||rotation<0||rotation>=24||Math.Abs((long)cell.x)>1024||Math.Abs((long)cell.y)>1024||Math.Abs((long)cell.z)>1024||Blocks.Count>=512)return false;
            var proposed=new BuiltBlock{type=type,cell=cell,rotation=rotation};var cells=Cells(proposed).ToArray();
            if(cells.Any(c=>At(c)!=null))return false;
            if(!requireConnection||Blocks.Count==0)return true;
            var offsets=new[]{new GridCell(1,0,0),new GridCell(-1,0,0),new GridCell(0,1,0),new GridCell(0,-1,0),new GridCell(0,0,1),new GridCell(0,0,-1)};
            return cells.Any(c=>offsets.Any(o=>At(c+o)!=null));
        }
        public bool Build(string type,GridCell cell,int rotation,bool cargoAccess)
        {
            if(ReadOnly||!CanPlace(type,cell,rotation)||!CanAfford(type,cargoAccess))return false;
            var next=new BuiltBlock(type,cell,rotation);
            foreach(var x in BuildCosts(type))
            {
                decimal n=Math.Min(Backpack.Get(x.id),x.Value);
                if(n>0)Backpack.Take(new[]{new ResourceAmount(x.id,n)});
                if(x.Value>n)Cargo.Take(new[]{new ResourceAmount(x.id,x.Value-n)});
            }
            Blocks.Add(next);Cargo.SetCapacity(1000m*Count("Cargo"));Publish();return true;
        }
        public bool Dismantle(string uid,bool cargoAccess)
        {
            var block=Blocks.Find(x=>x.uid==uid);if(ReadOnly||block==null||Blocks.Count<=1)return false;
            if((block.type=="Cargo"&&Count("Cargo")==1&&(Cargo.Used>0||Assembly.Count>0)) || (block.type=="Assembler"&&Count("Assembler")==1&&Assembly.Count>0))return false;
            if(block.type=="Assembler"&&AssemblyReservedLitres>1000m*(Count("Assembler")-1))return false;
            // Do not split the grid, including multi-cell blocks.
            var remaining=Blocks.Where(x=>x!=block).ToList();var all=new HashSet<GridCell>(remaining.SelectMany(Cells));
            var seen=new HashSet<GridCell>();var queue=new Queue<GridCell>();queue.Enqueue(all.First());
            var offsets=new[]{new GridCell(1,0,0),new GridCell(-1,0,0),new GridCell(0,1,0),new GridCell(0,-1,0),new GridCell(0,0,1),new GridCell(0,0,-1)};
            while(queue.Count>0){var c=queue.Dequeue();if(!all.Contains(c)||!seen.Add(c))continue;foreach(var o in offsets)queue.Enqueue(c+o);}
            if(seen.Count!=all.Count)return false;
            var target=cargoAccess&&Count("Cargo")>(block.type=="Cargo"?1:0)?Cargo:Backpack;
            var salvage=Definition.Block(block.type).components;
            if(!target.CanAdd(salvage))return false;
            if(block.type=="Cargo"&&Cargo.Used+(target==Cargo?salvage.Sum(x=>x.Value*Definition.Item(x.id).Litres):0)>1000m*(Count("Cargo")-1))return false;
            target.Add(salvage);Blocks.Remove(block);Cargo.SetCapacity(1000m*Count("Cargo"));EnergyKwh=Math.Min(EnergyKwh,MaxEnergy);Publish();return true;
        }
        public bool Transfer(bool fromCargo,string id,decimal quantity)
        {if(ReadOnly)return false;bool ok=(fromCargo?Cargo:Backpack).TransferTo(fromCargo?Backpack:Cargo,id,quantity);if(ok)Publish();return ok;}
        public int Maximum(ProductionRecipe recipe)
        {
            if(recipe==null)return 0;var inputs=Definition.Inputs(recipe);int stock=Cargo.Maximum(inputs);
            if(recipe.machine!="assembler")return stock;
            decimal volume=inputs.Sum(x=>x.Value*Definition.Item(x.id).Litres),room=Math.Max(0,1000m*Count("Assembler")-AssemblyReservedLitres);
            return Math.Min(stock,(int)Math.Min(999,decimal.Floor(room/volume)));
        }
        public bool Enqueue(string recipeId,int count)
        {
            var recipe=Definition.Recipe(recipeId);
            if(ReadOnly||recipe==null||recipe.machine!="assembler"||!HasMachine("Assembler")||count<1||count>999||Assembly.Count>=32)return false;
            if(count>Maximum(recipe))return false;
            var inputs=Definition.Inputs(recipe);if(!Cargo.Take(inputs,count))return false;
            Assembly.Add(new ProductionJob{uid=Guid.NewGuid().ToString("N"),recipe=recipe.id,remaining=count,reservedPerUnit=inputs});Publish();return true;
        }
        public bool Cancel(string uid)
        {
            var job=Assembly.Find(x=>x.uid==uid);if(ReadOnly||job==null||!Cargo.CanAdd(job.reservedPerUnit,job.remaining))return false;
            Cargo.Add(job.reservedPerUnit,job.remaining);Assembly.Remove(job);Publish();return true;
        }
        public void RegisterDeposit(string id,string item,decimal quantity)
        {
            if(Deposits.ContainsKey(id))return;
            if(Definition.Item(item)==null||quantity<0)throw new ArgumentException("Invalid deposit");
            Deposits.Add(id,new DepositState{id=id,item=item,remaining=quantity.ToString(CultureInfo.InvariantCulture)});
        }
        public decimal Mine(string depositId,decimal requested)
        {
            if(ReadOnly||requested<=0||!Deposits.TryGetValue(depositId,out var deposit))return 0;
            decimal remaining=decimal.Parse(deposit.remaining,CultureInfo.InvariantCulture),n=Math.Min(requested,Math.Min(remaining,Backpack.RoomFor(deposit.item)));
            if(n<=0||!Backpack.Add(new[]{new ResourceAmount(deposit.item,n)}))return 0;
            deposit.remaining=(remaining-n).ToString(CultureInfo.InvariantCulture);Publish();return n;
        }
        public bool CreditExcavation(string operationId,decimal stoneKg)
        {
            if(ReadOnly||string.IsNullOrEmpty(operationId)||committedMining.Contains(operationId)||stoneKg<=0)return false;
            if(!Backpack.Add(new[]{new ResourceAmount("ore-stone",stoneKg)}))return false;
            committedMining.Add(operationId);Publish();return true;
        }
        public decimal RecordExcavation(string operationId,float x,float y,float z,decimal stoneKg)
        {
            if(ReadOnly||string.IsNullOrEmpty(operationId)||committedMining.Contains(operationId)||stoneKg<=0||stoneKg>1000000||Excavations.Count>=4096)return 0;
            decimal n=Math.Min(stoneKg,Backpack.RoomFor("ore-stone"));
            if(n>0&&!Backpack.Add(new[]{new ResourceAmount("ore-stone",n)}))return 0;
            if(stoneKg>n){string id="loose-"+operationId;Deposits.Add(id,new DepositState{id=id,item="ore-stone",remaining=(stoneKg-n).ToString(CultureInfo.InvariantCulture),loose=true,x=x,y=y,z=z});}
            committedMining.Add(operationId);Excavations.Add(new Excavation{id=operationId,x=x,y=y,z=z});Publish();return n;
        }
        public void Tick(float seconds)
        {
            if(ReadOnly||seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
            // No offline catch-up and no huge completion bursts after a pause.
            seconds=Math.Min(seconds,.25f);OutputBlocked=false;
            float max=MaxEnergy;EnergyKwh=Math.Min(max,EnergyKwh+SolarKw*seconds/3600);
            if(HasMachine("Refinery")&&RefineryPower&&!RefineryPaused)
            {
                var candidates=Definition.recipes.Where(x=>x.machine=="refinery"&&Cargo.CanTake(x.inputs));
                var recipe=candidates.FirstOrDefault(x=>x.inputs[0].id==PriorityOre)??candidates.FirstOrDefault();
                if(RefiningRecipe!=recipe?.id)RefineryProgress=0;
                RefiningRecipe=recipe?.id;
                if(recipe!=null)
                {
                    if(!FitsAfterRefining(recipe)){OutputBlocked=true;}
                    else if(UseEnergy(120*seconds/3600))
                    {
                        RefineryProgress+=seconds/recipe.seconds;
                        if(RefineryProgress>=1){Cargo.Take(recipe.inputs);Cargo.Add(recipe.outputs);RefineryProgress=0;Publish();}
                    }
                }
                else RefineryProgress=0;
            }
            if(HasMachine("Assembler")&&!AssemblyPaused&&Assembly.Count>0)
            {
                var job=Assembly[0];var recipe=Definition.Recipe(job.recipe);
                if(job.progress>=1)
                {
                    if(Cargo.Add(recipe.outputs)){job.remaining--;job.progress=0;if(job.remaining==0)Assembly.RemoveAt(0);Publish();}else OutputBlocked=true;
                }
                else if(Cargo.CanAdd(recipe.outputs)&&UseEnergy(50*seconds/3600))job.progress=Math.Min(1,job.progress+seconds/recipe.seconds);
                else if(!Cargo.CanAdd(recipe.outputs))OutputBlocked=true;
            }
            Status=OutputBlocked?"SORTIE SATURÉE":EnergyKwh<=.00001f?"ÉNERGIE INSUFFISANTE":null;
        }
        bool FitsAfterRefining(ProductionRecipe recipe) => Cargo.Used-recipe.inputs.Sum(x=>x.Value*Definition.Item(x.id).Litres)+recipe.outputs.Sum(x=>x.Value*Definition.Item(x.id).Litres)<=Cargo.Capacity;
        bool UseEnergy(float amount){if(EnergyKwh+1e-8f<amount)return false;EnergyKwh=Math.Max(0,EnergyKwh-amount);return true;}
        public EconomySave Snapshot() => new EconomySave {definitionVersion=Definition.version,assemblerEfficiency=Definition.assemblerEfficiency,backpack=Backpack.Snapshot(),cargo=Cargo.Snapshot(),blocks=Blocks.ToArray(),assembly=Assembly.ToArray(),deposits=Deposits.Values.OrderBy(x=>x.id,StringComparer.Ordinal).ToArray(),energyKwh=EnergyKwh,refineryPower=RefineryPower,refineryPause=RefineryPaused,assemblyPause=AssemblyPaused,priorityOre=PriorityOre,committedMining=committedMining.ToArray(),excavations=Excavations.ToArray()};
        public static EconomyWorld Restore(EconomyDefinition definition,EconomySave save)
        {
            if(save==null||save.format!=1||save.definitionVersion!=definition.version||save.assemblerEfficiency!=definition.assemblerEfficiency)throw new InvalidOperationException("Incompatible economy save; preserve it for migration");
            var world=new EconomyWorld(definition);
            if(save.blocks==null||save.blocks.Length==0||save.blocks.Length>512||save.blocks.Select(x=>x.uid).Distinct().Count()!=save.blocks.Length)throw new InvalidOperationException("Invalid saved grid");
            foreach(var b in save.blocks){if(string.IsNullOrEmpty(b.uid)||!world.CanPlace(b.type,b.cell,b.rotation,false))throw new InvalidOperationException("Invalid saved block");world.Blocks.Add(b);}
            world.Cargo.SetCapacity(1000m*world.Count("Cargo"));world.Backpack.Restore(save.backpack);world.Cargo.Restore(save.cargo);
            if(save.assembly!=null)foreach(var j in save.assembly)
            {
                var r=definition.Recipe(j.recipe);var cost=r==null?null:definition.Inputs(r);
                if(world.Assembly.Count>=32||r==null||r.machine!="assembler"||j.remaining<1||j.remaining>999||j.progress<0||j.progress>1||float.IsNaN(j.progress)||string.IsNullOrEmpty(j.uid)||world.Assembly.Any(x=>x.uid==j.uid)||j.reservedPerUnit==null||cost.Length!=j.reservedPerUnit.Length||!cost.All(x=>j.reservedPerUnit.Any(y=>y.id==x.id&&y.Value==x.Value)))throw new InvalidOperationException("Invalid saved production reservation");
                world.Assembly.Add(j);
            }
            if(world.AssemblyReservedLitres>1000m*world.Count("Assembler"))throw new InvalidOperationException("Saved assembler input exceeds physical buffer");
            if(save.deposits!=null)foreach(var d in save.deposits){if(world.Deposits.ContainsKey(d.id)||definition.Item(d.item)==null||decimal.Parse(d.remaining,CultureInfo.InvariantCulture)<0)throw new InvalidOperationException("Invalid saved deposit");world.Deposits.Add(d.id,d);}
            world.EnergyKwh=save.energyKwh;
            if(float.IsNaN(world.EnergyKwh)||float.IsInfinity(world.EnergyKwh)||world.EnergyKwh<0||world.EnergyKwh>world.MaxEnergy+.001f)throw new InvalidOperationException("Invalid saved energy");
            world.RefineryPower=save.refineryPower;world.RefineryPaused=save.refineryPause;world.AssemblyPaused=save.assemblyPause;world.PriorityOre=save.priorityOre;
            if(save.committedMining!=null)foreach(var id in save.committedMining)world.committedMining.Add(id);
            if(save.excavations!=null)foreach(var edit in save.excavations){if(world.Excavations.Count>=4096||!world.committedMining.Contains(edit.id)||world.Excavations.Any(x=>x.id==edit.id)||float.IsNaN(edit.x)||float.IsNaN(edit.y)||float.IsNaN(edit.z)||Math.Abs(edit.x)>1000||Math.Abs(edit.y)>1000||Math.Abs(edit.z)>1000)throw new InvalidOperationException("Invalid saved excavation");world.Excavations.Add(edit);}
            return world;
        }
    }
}
