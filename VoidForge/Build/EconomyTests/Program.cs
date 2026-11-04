using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VoidForge.Economy;

static class Program
{
    static EconomyDefinition definition;
    static readonly JsonSerializerOptions json=new JsonSerializerOptions{IncludeFields=true,IgnoreReadOnlyProperties=true,WriteIndented=true};
    static readonly List<string> checks=new List<string>();
    static ResourceAmount A(string id,decimal n)=>new ResourceAmount(id,n);
    static void Check(bool pass,string name){if(!pass)throw new Exception(name);checks.Add(name);}
    static void Reject(Action action,string name){bool rejected=false;try{action();}catch(InvalidOperationException){rejected=true;}Check(rejected,name);}
    static EconomyWorld World()
    {
        var w=new EconomyWorld(definition);w.Blocks.Add(new BuiltBlock("ArmorCube",new GridCell(0,0,0),0));w.Blocks.Add(new BuiltBlock("Cargo",new GridCell(1,0,0),0));w.Blocks.Add(new BuiltBlock("Battery",new GridCell(2,0,0),0));w.Blocks.Add(new BuiltBlock("Assembler",new GridCell(3,0,0),0));w.Blocks.Add(new BuiltBlock("Refinery",new GridCell(5,0,0),0));w.EnergyKwh=4;return w;
    }
    static void Tick(EconomyWorld w,int frames=100){for(int n=0;n<frames;n++)w.Tick(.1f);}
    static void Main(string[] args)
    {
        definition=JsonSerializer.Deserialize<EconomyDefinition>(File.ReadAllText(args[0]),json);definition.Validate();
        Check(definition.items.Length==46&&definition.recipes.Length==35&&definition.blocks.Length==12,"complete live catalogue: 46 items,35 recipes,12 blocks");
        var buffered=World();buffered.Blocks.Add(new BuiltBlock("Cargo",new GridCell(6,0,0),0));buffered.Cargo.SetCapacity(2000);buffered.Cargo.Add(new[]{A("ingot-iron",10000)});
        Check(buffered.Maximum(definition.Recipe("craft-component-plates"))==396&&!buffered.Enqueue("craft-component-plates",397)&&buffered.Cargo.Get("ingot-iron")==10000,"assembler input buffer bounds reservations without consuming rejected work");
        var stock=new ResourceStock(definition,10);
        Check(!stock.Add(new[]{A("component-plates",.5m)})&&!stock.Add(new[]{A("unknown",1)})&&!stock.Add(new[]{A("ore-iron",-1)}),"fractional units,unknown items,negative additions rejected");
        Check(stock.Add(new[]{A("component-plates",4)})&&!stock.Add(new[]{A("component-plates",2)})&&stock.Get("component-plates")==4,"capacity rejection is atomic");
        Check(!stock.Take(new[]{A("component-plates",5)})&&stock.Get("component-plates")==4,"insufficient stock consumes nothing");
        var destination=new ResourceStock(definition,2);Check(stock.TransferTo(destination,"component-plates",4)&&stock.Get("component-plates")==3&&destination.Get("component-plates")==1,"transfer limited by target capacity,conserves stock");
        var w=World();w.Cargo.Add(new[]{A("ingot-iron",42)});
        Check(!w.Enqueue("craft-component-motor",1)&&w.Cargo.Get("ingot-iron")==42,"multi-input recipe refuses missing nickel atomically");
        Check(w.Enqueue("craft-component-plates",2)&&w.Cargo.Get("ingot-iron")==0,"queue reserves exact ingredient quantities");
        w.AssemblyPaused=true;Tick(w);Check(w.Cargo.Get("component-plates")==0,"paused assembly does not produce");
        w.AssemblyPaused=false;w.EnergyKwh=0;Tick(w);Check(w.Cargo.Get("component-plates")==0,"unpowered assembly does not produce");
        w.EnergyKwh=4;Tick(w,24);Check(w.Cargo.Get("component-plates")==1&&w.Assembly[0].remaining==1,"one completed unit delivered once");
        Check(w.Cancel(w.Assembly[0].uid)&&w.Cargo.Get("ingot-iron")==21&&w.Cargo.Get("component-plates")==1,"cancel refunds only remaining reserved units");
        Tick(w);Check(w.Cargo.Get("component-plates")==1,"completed work is not delivered twice");
        w=World();w.Cargo.Add(new[]{A("ore-iron",10)});Tick(w,25);Check(w.Cargo.Get("ore-iron")==0&&w.Cargo.Get("ingot-iron")==7,"refinery10kg iron ore yields7kg iron");
        w=World();w.Cargo.Add(new[]{A("ore-stone",10)});Tick(w,25);Check(w.Cargo.Get("gravel")==.14m&&w.Cargo.Get("ingot-iron")==.3m&&w.Cargo.Get("ingot-nickel")==.024m&&w.Cargo.Get("wafer-silicon")==.04m,"stone yields four coproducts exactly once");
        w=World();w.Cargo.Add(new[]{A("ingot-iron",21)});w.Enqueue("craft-component-plates",1);w.Cargo.SetCapacity(.1m);Tick(w);Check(w.Assembly.Count==1&&w.Cargo.Get("component-plates")==0,"full output preserves pending work without overflow");
        w=World();w.Backpack.Add(new[]{A("component-plates",3)});Check(!w.Build("ArmorCube",new GridCell(0,1,0),0,false)&&w.Backpack.Get("component-plates")==3,"building rejects insufficient components atomically");
        w.Cargo.Add(new[]{A("component-plates",1)});Check(!w.Build("ArmorCube",new GridCell(0,0,0),0,true)&&w.Cargo.Get("component-plates")==1,"occupied cell does not consume construction stock");
        Check(!w.Build("ArmorCube",new GridCell(0,1,0),24,true),"invalid grid orientation rejected");
        Check(w.Build("ArmorCube",new GridCell(0,1,0),0,true)&&w.Backpack.Get("component-plates")==0&&w.Cargo.Get("component-plates")==0,"building combines backpack and accessible cargo once");
        var placed=w.At(new GridCell(0,1,0));Check(w.Dismantle(placed.uid,false)&&w.Backpack.Get("component-plates")==4,"dismantling returns actual one-metre bill");
        Check(!w.Dismantle(w.At(new GridCell(2,0,0)).uid,true),"dismantling cannot split a connected grid");
        Check(GridRotations.All.Length==24&&GridRotations.All.Select(r=>r.right.x+","+r.right.y+","+r.right.z+":"+r.up.x+","+r.up.y+","+r.up.z).Distinct().Count()==24,"24 unique right-handed grid orientations");
        foreach(int rotation in Enumerable.Range(0,24))Check(w.Cells(new BuiltBlock{type="Cockpit",cell=new GridCell(0,0,0),rotation=rotation}).Distinct().Count()==12,"cockpit occupies12 unique cells in rotation"+rotation);
        w=World();w.RegisterDeposit("iron","ore-iron",5);Check(w.Mine("iron",8)==5&&w.Mine("iron",8)==0,"finite deposit cannot be mined twice");
        Check(w.RecordExcavation("edit1",1,2,3,10)==10&&w.RecordExcavation("edit1",1,2,3,10)==0&&w.Backpack.Get("ore-stone")==10,"duplicate Digger completion cannot duplicate credit");
        w.Backpack.SetCapacity(w.Backpack.Used);w.RecordExcavation("edit2",2,3,4,5);Check(w.Deposits["loose-edit2"].remaining=="5"&&w.Backpack.Get("ore-stone")==10,"full backpack preserves removed stone in world pickup");
        w.Cargo.Add(new[]{A("ingot-iron",42)});w.Enqueue("craft-component-plates",2);Tick(w,3);
        var serialized=JsonSerializer.Serialize(w.Snapshot(),json);var restored=EconomyWorld.Restore(definition,JsonSerializer.Deserialize<EconomySave>(serialized,json));
        Check(restored.Cargo.Get("ingot-iron")==0&&restored.Assembly.Count==1&&restored.Assembly[0].remaining==2&&restored.Excavations.Count==2&&restored.Deposits["iron"].remaining=="0","save roundtrip preserves reserved work,excavations and depleted deposits");
        Check(restored.RecordExcavation("edit1",1,2,3,10)==0,"deduplication survives save reload");
        restored.ReadOnly=true;Check(!restored.Enqueue("craft-component-plates",1)&&!restored.Transfer(true,"ingot-iron",1)&&restored.Mine("iron",1)==0,"read-only network guard rejects resource commands");
        var corrupt=JsonSerializer.Deserialize<EconomySave>(serialized,json);corrupt.assembly[0].reservedPerUnit[0].amount="1";Reject(()=>EconomyWorld.Restore(definition,corrupt),"tampered reservation rejected on load");
        var x3=JsonSerializer.Deserialize<EconomyDefinition>(JsonSerializer.Serialize(definition,json),json);x3.assemblerEfficiency=3;Check(x3.Inputs(x3.Recipe("craft-component-plates"))[0].Value==7,"x3 assembly consumes7kg iron per plate");
        var moduleA=World();moduleA.Backpack.Add(new[]{A("component-plates",3)});
        var moduleB=new EconomyWorld(definition,moduleA.Backpack);
        Check(ReferenceEquals(moduleA.Backpack,moduleB.Backpack)&&!ReferenceEquals(moduleA.Cargo,moduleB.Cargo),"module worlds share only the player backpack");
        moduleA.Cargo.Add(new[]{A("ore-iron",10)});moduleA.MachineAvailable=type=>false;Tick(moduleA,30);
        Check(moduleA.Cargo.Get("ore-iron")==10&&!moduleA.HasMachine("Refinery"),"disabled module systems cannot process stock");
        moduleA.MachineAvailable=type=>moduleA.Count(type)>0;moduleA.EnergyConsumer=(type,amount)=>false;Tick(moduleA,30);
        Check(moduleA.Cargo.Get("ore-iron")==10,"module power refusal prevents production despite legacy reservoir");
        moduleA.EnergyConsumer=(type,amount)=>true;Tick(moduleA,30);
        Check(moduleA.Cargo.Get("ingot-iron")==7,"authoritative module power admits exactly one refining batch");
        moduleA.Blocks.Add(new BuiltBlock("SolarPanel",new GridCell(8,0,0),0));moduleA.ExternalEnergyManaged=true;moduleA.EnergyKwh=0;Tick(moduleA);
        Check(moduleA.EnergyKwh==0,"external solar accounting is not duplicated by the legacy ledger");
        var empty=new EconomyWorld(definition);empty.Cargo.SetCapacity(0);var emptySave=empty.Snapshot();
        Check(EconomyWorld.Restore(definition,emptySave).Blocks.Count==0,"fully dismantled worlds remain valid saves");
        var otherSave=moduleB.Snapshot();var recoveredModule=EconomyWorld.Restore(definition,otherSave,moduleA.Backpack);
        Check(ReferenceEquals(recoveredModule.Backpack,moduleA.Backpack)&&moduleA.Backpack.Get("component-plates")==3,"secondary module restore cannot overwrite the shared player backpack");
        string report=args.Length>1?args[1]:Path.Combine(Path.GetDirectoryName(args[0]),"core-validation.json");Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(report)));File.WriteAllText(report,JsonSerializer.Serialize(new{success=true,count=checks.Count,checks},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("PASS "+checks.Count+" economy checks");
    }
}
