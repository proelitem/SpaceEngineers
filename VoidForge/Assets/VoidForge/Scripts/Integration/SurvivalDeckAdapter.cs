#if VF_LOCAL_ECONOMY
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using VoidForge.Economy;
using VoidForge.UI.Cyberdeck;

namespace VoidForge.Integration
{
    // Expanded scene owns the existing HUD command boundary; no authored UI asset is edited.
    public sealed class SurvivalDeckAdapter : MonoBehaviour
    {
        public SurvivalSession session;
        public CyberdeckHud Hud { get; private set; }
        public CyberdeckSession State => Hud!=null?Hud.State:null;
        CyberdeckPointer pointer;
        CyberdeckSurface surface;
        float nextSnapshot;
        int signature=int.MinValue;
        bool mapReady;
        public bool PointerBusy => pointer!=null&&(pointer.IsPointing||pointer.IsInteracting);

        IEnumerator Start()
        {
            while(Hud==null)
            {
                Hud=FindFirstObjectByType<CyberdeckHud>();
                if(Hud==null)yield return null;
            }
            var binding=FindFirstObjectByType<CyberdeckRuntime>();
            if(binding!=null)binding.ReleaseInterface();
            session.economy.DetachInterface();
            pointer=Hud.GetComponent<CyberdeckPointer>();
            surface=FindFirstObjectByType<CyberdeckSurface>();
            Hud.BlockAvailable=id=>session.economy.BlockSupported(id);
            Hud.BlockSize=id=>session.economy.BlockSize(id);
            State.AssemblyCommand=Queue;
            State.CancelAssemblyRequested+=Cancel;
            State.RefineryPowerRequested+=Power;
            State.RefineryPauseRequested+=PauseRefinery;
            State.AssemblyPauseRequested+=PauseAssembly;
            State.StorageRequested+=Storage;
            State.TransferRequested+=Transfer;
            State.RefineryOreRequested+=Ore;
            State.BlockRequested+=Block;
            State.SeatExitRequested+=Leave;
            State.BlockIngredients=Ingredients;
            if(session.equipment.Selected!=null)State.selectedTool=ToolIndex(session.equipment.Selected);
            Hud.ActionInvoked+=Action;
            State.Changed+=RelabelWorkshop;
            Publish(true);
        }
        bool Workshop => session.CanAct&&session.CargoAccess;
        void Commit(string text){session.Sync();session.Save();session.Tell(text);}
        bool Queue(string id,int count)
        {bool ok=Workshop&&session.Ledger.Enqueue(id,count);if(ok)Commit("FABRICATION AJOUTÉE • MATIÈRES RÉSERVÉES");else session.Tell("APPROCHER L’ATELIER • ASSEMBLEUR ET MATIÈRES NÉCESSAIRES");return ok;}
        void Cancel(string id){if(Workshop&&session.Ledger.Cancel(id))Commit("FILE ANNULÉE • MATIÈRES RESTITUÉES");}
        void Power(bool on){if(Workshop){session.Ledger.RefineryPower=on;Commit(on?"RAFFINERIE EN MARCHE":"RAFFINERIE ARRÊTÉE");}}
        void PauseRefinery(bool pause){if(Workshop){session.Ledger.RefineryPaused=pause;Commit(pause?"RAFFINAGE EN PAUSE":"RAFFINAGE REPRIS");}}
        void PauseAssembly(bool pause){if(Workshop){session.Ledger.AssemblyPaused=pause;Commit(pause?"ASSEMBLAGE EN PAUSE":"ASSEMBLAGE REPRIS");}}
        void Ore(string id){if(Workshop){session.Ledger.PriorityOre=id;session.Ledger.RefineryProgress=0;Commit("MINERAI PRIORITAIRE SÉLECTIONNÉ");}}
        void Storage(string id){State.selectedStorage=id=="cargo"&&session.CargoAccess?"cargo":"backpack";Publish(true);}
        void Block(string id){session.Tell("A : POSER LE PLAN BLEU • X : TOURNER • SOUDEUSE : TERMINER");}
        void Leave(){if(session.economy.Seated)session.economy.ExitSeat();else Hud.Show(DeckPage.Inventory);}
        void Transfer(string id)
        {
            if(!Workshop)return;
            bool fromCargo=State.selectedStorage=="cargo";
            var source=fromCargo?session.Ledger.Cargo:session.Ledger.Backpack;
            decimal quantity=source.Get(id);
            if(!fromCargo&&session.equipment.Selected!=null&&id==EconomyRuntime.ToolId(session.equipment.Selected.kind))quantity=Math.Max(0,quantity-1);
            if(session.Ledger.Transfer(fromCargo,id,quantity))Commit("TRANSFERT EFFECTUÉ");else session.Tell("TRANSFERT REFUSÉ • CAPACITÉ OU OUTIL SÉLECTIONNÉ");
        }
        List<DeckIngredient> Ingredients(string id)
        {
            return (session.Ledger.BuildCosts(id)??Array.Empty<ResourceAmount>()).Select(a=>Ingredient(a,session.Ledger.ConstructionStock(a.id,session.CargoAccess))).ToList();
        }
        DeckIngredient Ingredient(ResourceAmount a,decimal stock)
        {var item=session.Ledger.Definition.Item(a.id);return new DeckIngredient{id=a.id,name=item.name.ToUpperInvariant(),icon=item.icon,unit=item.unit,perUnit=(float)a.Value,stock=(float)stock};}
        void Update()
        {
            if(Hud==null)return;
            var node=Hud.Expanded?XRNode.Head:XRNode.LeftHand;
            Hud.GetComponent<Canvas>().enabled=Application.isEditor||(Application.isFocused&&EquipmentRig.Tracked(node));
            if(Time.unscaledTime>=nextSnapshot){nextSnapshot=Time.unscaledTime+.3f;Publish(false);}
        }
        public void Publish(bool force)
        {
            if(State==null||!session.Ready||pointer!=null&&pointer.IsInteracting)return;
            var world=session.Ledger;bool access=session.CargoAccess;
            if(Camera.main!=null)State.playerPosition=Camera.main.transform.position;
            State.equippedTool=ToolIndex(session.equipment.Held);
            State.preview=false;
            if(State.selectedStorage!="cargo"||!access)State.selectedStorage="backpack";
            int next=world.Revision*397^(access?1:0)^(session.economy.NetworkLocked?2:0)^State.selectedStorage.GetHashCode()^
                Mathf.FloorToInt(world.RefineryProgress*10)^Mathf.FloorToInt(world.EnergyKwh*25)^(int)Hud.page*65521^State.equippedTool*8191^(session.economy.Seated?128:0);
            if(Hud.page==DeckPage.Map)next^=Vector3Int.FloorToInt(State.playerPosition).GetHashCode();
            if(world.Assembly.Count>0)next^=Mathf.FloorToInt(world.Assembly[0].progress*10)*8191;
            if(!force&&next==signature)return;signature=next;
            State.storages=new List<DeckStorage>{new DeckStorage{id="backpack",name="SAC PERSONNEL",available=true},new DeckStorage{id="cargo",name="BASE • SOUTE",available=access}};
            var stock=State.selectedStorage=="cargo"?world.Cargo:world.Backpack;
            State.cargoConnected=true;State.cargoName=State.selectedStorage=="cargo"?"BASE DE DÉPART":"SAC PERSONNEL";
            State.shipName="BASE DE DÉPART";State.usedLitres=(float)stock.Used;State.capacityLitres=(float)stock.Capacity;
            State.transferAvailable=Workshop;State.cockpitConnected=Workshop;
            State.refineryAvailable=world.HasMachine("Refinery");State.assemblerAvailable=world.HasMachine("Assembler");
            State.refineryPowered=world.RefineryPower;State.refineryPaused=world.RefineryPaused;State.assemblyPaused=world.AssemblyPaused;State.outputBlocked=world.OutputBlocked;
            State.battery=world.MaxEnergy>0?world.EnergyKwh/world.MaxEnergy:0;State.refineryProgress=world.RefineryProgress;State.refineryKw=world.RefiningRecipe!=null&&world.RefineryPower&&!world.RefineryPaused?120:0;
            State.items=stock.Snapshot().Select(a=>{var item=world.Definition.Item(a.id);return new DeckItem(a.id,item.name.ToUpperInvariant(),item.icon,(float)a.Value,item.family!="minerais"&&item.family!="recyclage",item.unit);}).ToList();
            State.toolsAvailable=new[]{session.OwnTool(0),session.OwnTool(1),session.OwnTool(2)};
            State.recipes=world.Definition.recipes.Where(r=>r.machine=="assembler").Select(r=>new DeckRecipe{id=r.id,name=r.name.ToUpperInvariant(),icon=r.icon,available=true,maxCraftable=world.Maximum(r),ingredients=world.Definition.Inputs(r).Select(a=>Ingredient(a,world.Cargo.Get(a.id))).ToList()}).ToList();
            State.assembly=world.Assembly.Select(j=>{var r=world.Definition.Recipe(j.recipe);return new DeckJob{uid=j.uid,recipeId=r.id,name=r.name.ToUpperInvariant(),icon=r.icon,quantity=j.remaining,progress=j.progress};}).ToList();
            var ores=world.Definition.recipes.Where(r=>r.machine=="refinery"&&world.Cargo.Get(r.inputs[0].id)>0).OrderByDescending(r=>r.inputs[0].id==world.PriorityOre).ToArray();
            State.refining=ores.Select(r=>{var item=world.Definition.Item(r.inputs[0].id);return new DeckItem(item.id,item.name.ToUpperInvariant(),item.icon,(float)world.Cargo.Get(item.id));}).ToList();
            var active=world.Definition.Recipe(world.RefiningRecipe)??ores.FirstOrDefault();
            State.refineryOutputs=active==null?new List<DeckItem>():active.outputs.Select(a=>{var item=world.Definition.Item(a.id);return new DeckItem(a.id,item.name.ToUpperInvariant(),item.icon,(float)a.Value,true,item.unit);}).ToList();
            State.refineryBatch=active==null?0:(float)active.inputs[0].Value;
            if(Camera.main!=null)State.playerPosition=Camera.main.transform.position;
            State.waypoints.RemoveAll(w=>w.id.StartsWith("outcrop-",StringComparison.Ordinal)||w.id=="workshop");
            foreach(var grid in session.construction.Grids)
            {
                string id="construction-grid-"+grid.id;
                if(!State.waypoints.Any(w=>w.id==id))State.waypoints.Add(new DeckWaypoint{id=id,name=grid.id==0?"BASE DE DÉPART":"CONSTRUCTION "+grid.id,category="BASES",icon="map-outpost",target=session.construction.TargetForGrid(grid.id)});
            }
            if(!mapReady)
            {
                var asteroids=FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Asteroid_",StringComparison.Ordinal)).ToArray();
                foreach(var t in asteroids)
                    if(!State.waypoints.Any(w=>w.id==t.name))State.waypoints.Add(new DeckWaypoint{id=t.name,name=t.name.Replace("Asteroid_","ASTÉROÏDE "),category="REPÈRES",icon="map-asteroid",target=t});
                if(asteroids.Length>0)
                {
                    mapReady=true;State.localMapRange=Mathf.Max(30,asteroids.Max(t=>Vector3.Distance(t.position,State.playerPosition))*1.2f);
                    Hud.Zoom(State.localMapRange/Hud.MapRange);
                }
            }
            State.Publish();
        }
        static int ToolIndex(HandTool tool)=>tool==null?-1:tool.kind==HandToolKind.MiningDrill?0:tool.kind==HandToolKind.Welder?1:2;
        void RelabelWorkshop()
        {
            if(Hud==null||session.economy.Seated)return;
            foreach(var text in Hud.GetComponentsInChildren<Text>())
            {
                if(text.text=="COCKPIT CONNECTÉ")text.text="ATELIER CONNECTÉ";
                else if(text.text=="COCKPIT DÉCONNECTÉ")text.text="ATELIER HORS PORTÉE";
                else if(text.text=="SORTIR DU COCKPIT")text.text="RETOUR INVENTAIRE";
                else if(text.text=="APPROCHER UN COCKPIT • A / E POUR S’ASSEOIR")text.text="APPROCHER LA BASE POUR UTILISER L’ATELIER";
            }
        }
        void Action(string action)
        {
            if(action!="expand"&&action!="collapse")return;
            if(Hud.Expanded&&Camera.main!=null)
            {Hud.transform.SetParent(Camera.main.transform,false);Hud.transform.localPosition=new Vector3(0,-.08f,.62f);Hud.transform.localRotation=Quaternion.identity;Hud.transform.localScale=Vector3.one*(.62f/CyberdeckHud.Width);}
            else if(surface!=null)
            {
                Hud.transform.SetParent(surface.hudAnchor,false);Hud.transform.localPosition=new Vector3(0,0,.0008f);Hud.transform.localRotation=Quaternion.Euler(0,180,0);
                float scale=Mathf.Min(surface.safeMeters.x/CyberdeckHud.Width,surface.safeMeters.y/CyberdeckHud.Height);var parent=surface.hudAnchor.lossyScale;
                Hud.transform.localScale=new Vector3(scale/Mathf.Abs(parent.x),scale/Mathf.Abs(parent.y),scale/Mathf.Abs(parent.z));
            }
        }
    }
}
#endif
