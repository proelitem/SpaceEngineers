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
    [Serializable] public sealed class ConstructionGridState
    { public int id; public Vector3 origin; public Quaternion rotation = Quaternion.identity; }
    [Serializable] public sealed class ConstructionPlan
    { public string uid,type; public int grid,orientation; public Vector3Int cell; public float progress; }
    [Serializable] public sealed class ConstructedBlock
    { public string uid; public int grid; public Vector3Int cell; }
    public sealed class ConstructionPlanMarker : MonoBehaviour { public string uid; }

    public sealed class ConstructionSystem : MonoBehaviour
    {
        public readonly List<ConstructionGridState> Grids = new List<ConstructionGridState>();
        public readonly List<ConstructionPlan> Plans = new List<ConstructionPlan>();
        public readonly List<ConstructedBlock> Mappings = new List<ConstructedBlock>();
        readonly Dictionary<string,GameObject> planObjects = new Dictionary<string,GameObject>();
        readonly Dictionary<int,GpsTargetBounds> targets = new Dictionary<int,GpsTargetBounds>();
        SurvivalSession session;
        PlayerInputProvider player;
        Material hologram;
        GameObject ghost;
        string ghostType;
        float nextAction;
        int orientation;
        static readonly Vector3Int[] Adjacent = { Vector3Int.right,Vector3Int.left,Vector3Int.up,Vector3Int.down,Vector3Int.forward,Vector3Int.back };
        public bool GhostValid { get; private set; }

        public void Initialize(SurvivalSession owner,Transform ledgerGrid,SurvivalSave saved)
        {
            session = owner;
            player = FindFirstObjectByType<PlayerInputProvider>();
            hologram = Resources.Load<SurvivalSettings>(SurvivalSettings.Resource).hologram;
            if (saved == null)
                Grids.Add(new ConstructionGridState { id=0,origin=ledgerGrid.position,rotation=ledgerGrid.rotation });
            else
            {
                if (saved.grids==null||saved.grids.Length==0||saved.grids.Select(g=>g.id).Distinct().Count()!=saved.grids.Length)
                    throw new InvalidOperationException("Saved construction grid invalid; save preserved.");
                Grids.AddRange(saved.grids);
                if (saved.mappedBlocks!=null) Mappings.AddRange(saved.mappedBlocks);
                if (saved.plans!=null) Plans.AddRange(saved.plans);
                foreach(var plan in Plans)
                {
                    if(session.Ledger.Definition.Block(plan.type)==null||Grids.All(g=>g.id!=plan.grid)||plan.orientation<0||plan.orientation>=24||
                        !float.IsFinite(plan.progress)||plan.progress<0||plan.progress>=1||string.IsNullOrEmpty(plan.uid)||planObjects.ContainsKey(plan.uid))
                        throw new InvalidOperationException("Saved construction plan invalid; save preserved.");
                    CreatePlanVisual(plan);
                }
                foreach(var map in Mappings)
                    if(Grids.All(g=>g.id!=map.grid)||session.Ledger.Blocks.All(b=>b.uid!=map.uid))throw new InvalidOperationException("Saved block/grid mapping invalid.");
            }
        }
        ConstructionGridState Grid(int id) => Grids.Single(g=>g.id==id);
        public static Vector3 V(GridCell cell) => new Vector3(cell.x,cell.y,cell.z);
        static GridCell C(Vector3Int cell) => new GridCell(cell.x,cell.y,cell.z);
        static Vector3Int I(GridCell cell) => new Vector3Int(cell.x,cell.y,cell.z);
        static GridCell LedgerCell(int grid,Vector3Int cell) => new GridCell(grid*64+cell.x,cell.y,cell.z);
        IEnumerable<Vector3Int> Cells(string type,Vector3Int cell,int rotation)
        {
            return session.Ledger.Cells(new BuiltBlock { type=type,cell=C(cell),rotation=rotation }).Select(I);
        }
        int GridOf(BuiltBlock block) => Mappings.Find(m=>m.uid==block.uid)?.grid??0;
        Vector3Int CellOf(BuiltBlock block) => Mappings.Find(m=>m.uid==block.uid)?.cell??I(block.cell);
        IEnumerable<Vector3Int> Occupied(int grid,string ignorePlan=null)
        {
            foreach(var block in session.Ledger.Blocks.Where(b=>GridOf(b)==grid)) foreach(var cell in Cells(block.type,CellOf(block),block.rotation)) yield return cell;
            foreach(var plan in Plans.Where(p=>p.grid==grid&&p.uid!=ignorePlan)) foreach(var cell in Cells(plan.type,plan.cell,plan.orientation)) yield return cell;
        }
        public bool CanPlan(string type,int grid,Vector3Int cell,int rotation,Vector3 origin,Quaternion gridRotation,string ignorePlan=null)
        {
            if(session.Ledger.Definition.Block(type)==null||session.economy.Catalogue.Find(type)?.Available!=true||rotation<0||rotation>=24||
                Math.Abs(cell.x)>24||Math.Abs(cell.y)>24||Math.Abs(cell.z)>24||Plans.Count+session.Ledger.Blocks.Count-(ignorePlan==null?0:1)>=512)return false;
            var proposed=Cells(type,cell,rotation).ToArray();
            var occupied=grid<0?new HashSet<Vector3Int>():new HashSet<Vector3Int>(Occupied(grid,ignorePlan));
            if(proposed.Any(occupied.Contains))return false;
            if(occupied.Count>0&&!proposed.Any(c=>Adjacent.Any(a=>occupied.Contains(c+a))))return false;
            foreach(var c in proposed)
            {
                Vector3 centre=origin+gridRotation*(Vector3)c;
                // Test the real world, including the player, asteroids and other grids.
                if(Physics.OverlapBox(centre,Vector3.one*.485f,gridRotation,~0,QueryTriggerInteraction.Ignore).Length>0)return false;
            }
            return true;
        }
        public bool PlaceFoundation(string type,Vector3 position,Quaternion rotation)
        {
            if(!session.CanAct||Grids.Count>=12||!CanPlan(type,-1,Vector3Int.zero,orientation,position,rotation))return false;
            int id=Grids.Count==0?0:Grids.Max(g=>g.id)+1;
            Grids.Add(new ConstructionGridState { id=id,origin=position,rotation=rotation });
            AddPlan(type,id,Vector3Int.zero,orientation);
            return true;
        }
        public bool PlaceOnGrid(string type,int grid,Vector3Int cell,int rotation)
        {
            if(!session.CanAct||Grids.All(g=>g.id!=grid))return false;
            var frame=Grid(grid);
            if(!CanPlan(type,grid,cell,rotation,frame.origin,frame.rotation))return false;
            AddPlan(type,grid,cell,rotation);return true;
        }
        void AddPlan(string type,int grid,Vector3Int cell,int rotation)
        {
            var plan=new ConstructionPlan { uid=Guid.NewGuid().ToString("N"),type=type,grid=grid,cell=cell,orientation=rotation };
            Plans.Add(plan);CreatePlanVisual(plan);session.Save();
            session.Tell("PLAN BLEU POSÉ • SOUDER AVEC LES COMPOSANTS NÉCESSAIRES");
        }
        GameObject Hologram(string type,string name)
        {
            var go=Instantiate(session.economy.Catalogue.Find(type).prefab);
            go.name=name;
            foreach(var collider in go.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            foreach(var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterials=Enumerable.Repeat(hologram,renderer.sharedMaterials.Length).ToArray();
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return go;
        }
        void CreatePlanVisual(ConstructionPlan plan)
        {
            var grid=Grid(plan.grid);
            var go=Hologram(plan.type,"Construction hologram • "+plan.type);
            go.transform.SetParent(transform,false);
            go.transform.SetPositionAndRotation(grid.origin+grid.rotation*(Vector3)plan.cell,grid.rotation*EconomyRuntime.Rotation(plan.orientation));
            var size=session.economy.BlockSize(plan.type);
            var box=go.AddComponent<BoxCollider>();box.center=((Vector3)size-Vector3.one)*.5f;box.size=size;box.isTrigger=true;
            go.AddComponent<ConstructionPlanMarker>().uid=plan.uid;
            planObjects.Add(plan.uid,go);
        }
        public bool Weld(string uid,float seconds)
        {
            var plan=Plans.Find(p=>p.uid==uid);
            if(!session.CanAct||plan==null||!float.IsFinite(seconds)||seconds<=0)return false;
            if(!session.Ledger.CanAfford(plan.type,session.CargoAccess))
            {session.Tell("COMPOSANTS MANQUANTS • VOIR COÛT DÉTAILS DU CYBERDECK");return false;}
            plan.progress=Mathf.Min(1,plan.progress+seconds/2);
            if(plan.progress<1)return true;
            var grid=Grid(plan.grid);
            if(!CanPlan(plan.type,plan.grid,plan.cell,plan.orientation,grid.origin,grid.rotation,plan.uid)) {plan.progress=.99f;return false;}
            var ledgerCell=LedgerCell(plan.grid,plan.cell);
            if(!session.Ledger.CanPlace(plan.type,ledgerCell,plan.orientation,false)||!Spend(plan.type)){plan.progress=.99f;return false;}
            var built=new BuiltBlock(plan.type,ledgerCell,plan.orientation);
            session.Ledger.Blocks.Add(built);
            Mappings.Add(new ConstructedBlock { uid=built.uid,grid=plan.grid,cell=plan.cell });
            session.Ledger.Cargo.SetCapacity(1000m*session.Ledger.Count("Cargo"));
            Plans.Remove(plan);Destroy(planObjects[uid]);planObjects.Remove(uid);
            session.Sync();session.Save();session.Tell("BLOC SOUDÉ • "+session.Ledger.Definition.Block(plan.type).name.ToUpperInvariant());
            return true;
        }
        bool Spend(string type)
        {
            var bag=new List<ResourceAmount>();var cargo=new List<ResourceAmount>();
            foreach(var cost in session.Ledger.BuildCosts(type))
            {
                decimal fromBag=Math.Min(cost.Value,session.Ledger.Backpack.Get(cost.id));
                if(fromBag>0)bag.Add(new ResourceAmount(cost.id,fromBag));
                if(cost.Value>fromBag)cargo.Add(new ResourceAmount(cost.id,cost.Value-fromBag));
            }
            if((bag.Count>0&&!session.Ledger.Backpack.CanTake(bag.ToArray()))||
                (cargo.Count>0&&(!session.CargoAccess||!session.Ledger.Cargo.CanTake(cargo.ToArray()))))return false;
            if(bag.Count>0)session.Ledger.Backpack.Take(bag.ToArray());
            if(cargo.Count>0)session.Ledger.Cargo.Take(cargo.ToArray());
            return true;
        }
        public bool Grind(string uid)
        {
            if(!session.CanAct)return false;
            var plan=Plans.Find(p=>p.uid==uid);
            if(plan!=null){Plans.Remove(plan);Destroy(planObjects[uid]);planObjects.Remove(uid);session.Save();return true;}
            var block=session.Ledger.Blocks.Find(b=>b.uid==uid);if(block==null)return false;
            var world=session.Ledger;
            if((block.type=="Cargo"&&world.Count("Cargo")==1&&(world.Cargo.Used>0||world.Assembly.Count>0))||
                (block.type=="Assembler"&&world.Count("Assembler")==1&&world.Assembly.Count>0)||
                (block.type=="Assembler"&&world.AssemblyReservedLitres>1000m*(world.Count("Assembler")-1)))return false;
            int grid=GridOf(block);
            var cells=new HashSet<Vector3Int>(world.Blocks.Where(b=>b.uid!=uid&&GridOf(b)==grid).SelectMany(b=>Cells(b.type,CellOf(b),b.rotation)));
            foreach(var pending in Plans.Where(p=>p.grid==grid))foreach(var c in Cells(pending.type,pending.cell,pending.orientation))cells.Add(c);
            if(cells.Count>0)
            {
                var seen=new HashSet<Vector3Int>();var queue=new Queue<Vector3Int>();queue.Enqueue(cells.First());
                while(queue.Count>0){var c=queue.Dequeue();if(!cells.Contains(c)||!seen.Add(c))continue;foreach(var a in Adjacent)queue.Enqueue(c+a);}
                if(seen.Count!=cells.Count)return false;
            }
            var target=session.CargoAccess&&world.Count("Cargo")>(block.type=="Cargo"?1:0)?world.Cargo:world.Backpack;
            var salvage=world.BuildCosts(block.type);
            if(!target.CanAdd(salvage))return false;
            if(block.type=="Cargo"&&world.Cargo.Used+(target==world.Cargo?salvage.Sum(a=>a.Value*world.Definition.Item(a.id).Litres):0)>1000m*(world.Count("Cargo")-1))return false;
            target.Add(salvage);world.Blocks.Remove(block);Mappings.RemoveAll(m=>m.uid==uid);
            world.Cargo.SetCapacity(1000m*world.Count("Cargo"));world.EnergyKwh=Mathf.Min(world.EnergyKwh,world.MaxEnergy);
            session.Sync();session.Save();return true;
        }
        public void ApplyPhysicalPoses()
        {
            foreach(var block in session.Ledger.Blocks)
            {
                if(!session.economy.Instances.TryGetValue(block.uid,out var instance))continue;
                var frame=Grid(GridOf(block));
                instance.transform.SetPositionAndRotation(frame.origin+frame.rotation*(Vector3)CellOf(block),frame.rotation*EconomyRuntime.Rotation(block.rotation));
            }
            foreach(var frame in Grids)TargetForGrid(frame.id);
        }
        public Transform TargetForGrid(int id)
        {
            if(!targets.TryGetValue(id,out var target))
            {
                target=new GameObject("Construction GPS "+id).AddComponent<GpsTargetBounds>();
                target.transform.SetParent(transform,false);targets.Add(id,target);
            }
            var cells=Occupied(id).ToArray();
            var bounds=new Bounds(cells.Length>0?(Vector3)cells[0]:Vector3.zero,Vector3.one);
            foreach(var cell in cells){bounds.Encapsulate((Vector3)cell-Vector3.one*.5f);bounds.Encapsulate((Vector3)cell+Vector3.one*.5f);}
            var frame=Grid(id);target.transform.SetPositionAndRotation(frame.origin+frame.rotation*bounds.center,frame.rotation);target.size=bounds.size;
            return target.transform;
        }
        void Update()
        {
            if(session==null||!session.Ready||!session.CanAct||!Application.isFocused||session.equipment==null||
                !session.equipment.HasTrackedRightHand||session.economy.Seated||player==null||player.Actions==null){if(ghost!=null)ghost.SetActive(false);return;}
            var deck=session.deck;
            if(deck==null||deck.State==null||deck.PointerBusy){if(ghost!=null)ghost.SetActive(false);return;}
            var held=session.equipment.Held;
            Transform source=held!=null?held.tip:session.equipment.RightController;
            var ray=new Ray(source.position,source.forward);
            RaycastHit? nearest=null;
            foreach(var hit in Physics.RaycastAll(ray,held==null?2.5f:1f,~0,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance))
            {
                if(hit.collider.GetComponentInParent<HandTool>()!=null||hit.collider.GetComponentInParent<PlayerInputProvider>()!=null)continue;
                if(hit.collider.isTrigger&&hit.collider.GetComponentInParent<ConstructionPlanMarker>()==null)continue;
                nearest=hit;break;
            }
            if(held!=null&&held.IsOperating&&nearest.HasValue)
            {
                var hit=nearest.Value;var plan=hit.collider.GetComponentInParent<ConstructionPlanMarker>();var block=hit.collider.GetComponentInParent<EconomyBlock>();
                if(held.kind==HandToolKind.Welder&&plan!=null)Weld(plan.uid,Time.unscaledDeltaTime);
                if(held.kind==HandToolKind.Grinder&&Time.unscaledTime>=nextAction&&(plan!=null||block!=null))
                {nextAction=Time.unscaledTime+.35f;if(!Grind(plan!=null?plan.uid:block.uid))session.Tell("DÉMONTAGE REFUSÉ • STOCK OU CONNEXION À PRÉSERVER");}
            }
            if(deck.Hud.page!=DeckPage.Construction||string.IsNullOrEmpty(deck.State.selectedBlock))
            {if(ghost!=null)ghost.SetActive(false);return;}
            if(player.Actions.LeftPrimary.WasPressedThisFrame())orientation=(orientation+1)%24;
            string type=deck.State.selectedBlock;
            int gridId=-1;Vector3Int cell=Vector3Int.zero;
            Vector3 position=ray.GetPoint(2);
            Vector3 forward=Vector3.ProjectOnPlane(source.forward,Vector3.up);
            Quaternion gridRotation=Quaternion.LookRotation(forward.sqrMagnitude>.01f?forward.normalized:Vector3.forward,Vector3.up);
            if(nearest.HasValue)
            {
                var hit=nearest.Value;
                var plan=hit.collider.GetComponentInParent<ConstructionPlanMarker>();var block=hit.collider.GetComponentInParent<EconomyBlock>();
                if(plan!=null)gridId=Plans.Find(p=>p.uid==plan.uid)?.grid??-1;
                else if(block!=null){var built=session.Ledger.Blocks.Find(b=>b.uid==block.uid);if(built!=null)gridId=GridOf(built);}
                if(gridId>=0)
                {
                    var frame=Grid(gridId);gridRotation=frame.rotation;
                    Vector3 local=Quaternion.Inverse(gridRotation)*(hit.point+hit.normal*.02f-frame.origin);
                    cell=Vector3Int.RoundToInt(local);position=frame.origin;
                }
                else position=hit.point+hit.normal*.6f;
            }
            GhostValid=CanPlan(type,gridId,cell,orientation,position,gridRotation);
            if(ghost==null||ghostType!=type){if(ghost!=null)Destroy(ghost);ghost=Hologram(type,"Construction placement preview");ghostType=type;}
            ghost.SetActive(true);ghost.transform.SetPositionAndRotation(position+gridRotation*(Vector3)cell,gridRotation*EconomyRuntime.Rotation(orientation));
            var color=new MaterialPropertyBlock();color.SetColor("_BaseColor",GhostValid?new Color(.05f,.45f,1,.3f):new Color(1,.1f,.05f,.3f));
            foreach(var renderer in ghost.GetComponentsInChildren<Renderer>())renderer.SetPropertyBlock(color);
            if(player.Actions.RightPrimary.WasPressedThisFrame())
            {
                if(!GhostValid)session.Tell("EMPLACEMENT OCCUPÉ OU NON ADJACENT");
                else if(gridId<0)PlaceFoundation(type,position,gridRotation);else PlaceOnGrid(type,gridId,cell,orientation);
            }
        }
    }
}
#endif
