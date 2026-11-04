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
    { public int id; public string name; public Vector3 origin; public Quaternion rotation = Quaternion.identity; public Vector3 velocity,angularVelocity; }
    [Serializable] public sealed class ConstructionPlan
    { public string uid,type; public int grid,orientation; public Vector3Int cell; public float progress,work; public bool removing; public ResourceAmount[] installed; }
    [Serializable] public sealed class ConstructedBlock
    { public string uid; public int grid; public Vector3Int cell; }
    public sealed class ConstructionPlanMarker : MonoBehaviour { public string uid; }

    public sealed class ConstructionSystem : MonoBehaviour
    {
        public readonly List<ConstructionGridState> Grids = new List<ConstructionGridState>();
        public readonly List<ConstructionPlan> Plans = new List<ConstructionPlan>();
        public readonly List<ConstructedBlock> Mappings = new List<ConstructedBlock>();
        readonly Dictionary<string,GameObject> planObjects = new Dictionary<string,GameObject>();
        readonly Dictionary<string,float> pendingGrindWork=new Dictionary<string,float>();
        readonly Dictionary<int,GpsTargetBounds> targets = new Dictionary<int,GpsTargetBounds>();
        SurvivalSession session;
        PlayerInputProvider player;
        Material hologram;
        GameObject ghost;
        string ghostType;
        float nextWorkSave;
        BlockWorkDisplay workDisplay;
        int orientation;
        static readonly Vector3Int[] Adjacent = { Vector3Int.right,Vector3Int.left,Vector3Int.up,Vector3Int.down,Vector3Int.forward,Vector3Int.back };
        public bool GhostValid { get; private set; }
        public string PreviewType=>ghost!=null&&ghost.activeSelf?ghostType:null;
        public GameObject PreviewObject=>ghost;

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
                        !float.IsFinite(plan.progress)||plan.progress<0||plan.progress>1||string.IsNullOrEmpty(plan.uid)||planObjects.ContainsKey(plan.uid))
                        throw new InvalidOperationException("Saved construction plan invalid; save preserved.");
                    if(saved.format==1){plan.progress=0;plan.work=0;plan.installed=Array.Empty<ResourceAmount>();}
                    ValidateInstalled(plan);
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
        public int GridOf(BuiltBlock block) => Mappings.Find(m=>m.uid==block.uid)?.grid??0;
        public Vector3Int CellOf(BuiltBlock block) => Mappings.Find(m=>m.uid==block.uid)?.cell??I(block.cell);
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
            if(!session.CanAct||!CanPlan(type,-1,Vector3Int.zero,orientation,position,rotation))return false;
            var vacant=Grids.FirstOrDefault(g=>g.id!=0&&!Occupied(g.id).Any());
            if(vacant==null&&Grids.Count>=12)return false;
            int id=vacant?.id??(Grids.Count==0?0:Grids.Max(g=>g.id)+1);
            if(vacant==null)Grids.Add(new ConstructionGridState{id=id,origin=position,rotation=rotation});
            else{vacant.origin=position;vacant.rotation=rotation;vacant.velocity=vacant.angularVelocity=Vector3.zero;vacant.name="MODULE "+id;}
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
        internal bool ProvisionLegacyStarterSolar()
        {
            const string uid="vf-recette04-starter-solar";
            if(session.Ledger.Blocks.Any(b=>b.uid==uid))return true;
            var frame=Grids.Find(g=>g.id==0);if(frame==null)return false;
            var candidates=new[]{new Vector3Int(0,3,0),new Vector3Int(1,3,0),new Vector3Int(0,3,1)}.Concat(Occupied(0).SelectMany(c=>Adjacent.Select(a=>c+a))).Distinct();
            Physics.SyncTransforms();
            foreach(var cell in candidates)
            {
                if(!CanPlan("SolarPanel",0,cell,0,frame.origin,frame.rotation))continue;
                session.Ledger.Blocks.Add(new BuiltBlock("SolarPanel",C(cell),0){uid=uid});
                Mappings.Add(new ConstructedBlock{uid=uid,grid=0,cell=cell});session.Sync();
                Debug.Log("[VoidForge Recette04] One-time starter solar provisioned on the legacy base; stocks and battery charge preserved.");return true;
            }
            Debug.LogWarning("[VoidForge Recette04] No collision-free connected slot for the legacy starter solar; original world retained.");return false;
        }
        void AddPlan(string type,int grid,Vector3Int cell,int rotation)
        {
            var plan=new ConstructionPlan { uid=Guid.NewGuid().ToString("N"),type=type,grid=grid,cell=cell,orientation=rotation,installed=Array.Empty<ResourceAmount>() };
            Plans.Add(plan);CreatePlanVisual(plan);session.modules?.Refresh();session.motion?.Refresh();ApplyPhysicalPoses();session.modules?.SelectContext(grid);session.Save();
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
        void ValidateInstalled(ConstructionPlan plan)
        {
            var costs=session.Ledger.BuildCosts(plan.type);
            plan.installed??=Array.Empty<ResourceAmount>();
            if(plan.installed.Select(a=>a.id).Distinct().Count()!=plan.installed.Length||plan.installed.Any(a=>a.Value<0||a.Value!=decimal.Truncate(a.Value)||!costs.Any(c=>c.id==a.id&&a.Value<=c.Value))||!float.IsFinite(plan.work)||plan.work<0||plan.work>1.001f)
                throw new InvalidOperationException("Invalid saved installed construction components; save preserved.");
            UpdateProgress(plan);
        }
        public decimal Installed(ConstructionPlan plan,string id)=>plan.installed?.FirstOrDefault(a=>a.id==id)?.Value??0;
        void SetInstalled(ConstructionPlan plan,string id,decimal amount)
        {
            var stock=(plan.installed??Array.Empty<ResourceAmount>()).Where(a=>a.id!=id).ToList();
            if(amount>0)stock.Add(new ResourceAmount(id,amount));plan.installed=stock.ToArray();UpdateProgress(plan);
        }
        void UpdateProgress(ConstructionPlan plan)
        {decimal total=session.Ledger.BuildCosts(plan.type).Sum(a=>a.Value);plan.progress=total>0?(float)((plan.installed??Array.Empty<ResourceAmount>()).Sum(a=>a.Value)/total):0;}
        void ChangedWork(bool force=false)
        {
            session.modules.Touch();
            if(force||Time.unscaledTime>=nextWorkSave){nextWorkSave=Time.unscaledTime+.25f;session.Save();}
        }
        public bool Weld(string uid,float seconds)
        {
            var plan=Plans.Find(p=>p.uid==uid);
            if(!session.CanAct||plan==null||!float.IsFinite(seconds)||seconds<=0)return false;
            if(plan.removing){plan.removing=false;plan.work=0;}
            var costs=session.Ledger.BuildCosts(plan.type);var world=session.modules.World(plan.grid);
            float total=(float)costs.Sum(a=>a.Value);plan.work=Mathf.Min(total,plan.work+Mathf.Min(seconds,2)*total/2);
            bool changed=false;
            while(plan.work>=1)
            {
                var cost=costs.FirstOrDefault(a=>Installed(plan,a.id)<a.Value);
                if(cost==null)break;
                var unit=new[]{new ResourceAmount(cost.id,1)};
                var source=world.Backpack.CanTake(unit)?world.Backpack:session.modules.Access(plan.grid)&&world.Cargo.CanTake(unit)?world.Cargo:null;
                if(source==null){plan.work=Mathf.Min(plan.work,.999f);session.Tell("COMPOSANTS MANQUANTS • "+world.Definition.Item(cost.id).name.ToUpperInvariant());break;}
                if(plan.installed.Sum(a=>a.Value)+1==costs.Sum(a=>a.Value))
                {
                    var frame=Grid(plan.grid);
                    if(!CanPlan(plan.type,plan.grid,plan.cell,plan.orientation,frame.origin,frame.rotation,uid)){plan.work=.999f;break;}
                }
                source.Take(unit);SetInstalled(plan,cost.id,Installed(plan,cost.id)+1);plan.work-=1;changed=true;
            }
            if(plan.progress>=1)
            {
                var block=new BuiltBlock(plan.type,LedgerCell(plan.grid,plan.cell),plan.orientation){uid=plan.uid};
                session.Ledger.Blocks.Add(block);Mappings.Add(new ConstructedBlock{uid=uid,grid=plan.grid,cell=plan.cell});
                RemovePlan(plan);session.Sync();ChangedWork(true);session.Tell("BLOC OPÉRATIONNEL • 100 % DES COMPOSANTS SOUDÉS");return true;
            }
            plan.work=Mathf.Min(plan.work,.999f);if(changed)ChangedWork();return changed;
        }
        void RemovePlan(ConstructionPlan plan)
        {Plans.Remove(plan);if(planObjects.TryGetValue(plan.uid,out var go)){Destroy(go);planObjects.Remove(plan.uid);}}
        public bool Grind(string uid,float seconds=2)
        {
            if(!session.CanAct||!float.IsFinite(seconds)||seconds<=0)return false;
            var plan=Plans.Find(p=>p.uid==uid);
            if(plan==null)
            {
                var block=session.Ledger.Blocks.Find(b=>b.uid==uid);if(block==null)return false;
                int grid=GridOf(block);var world=session.modules.World(grid);
                if(block.type=="Cargo"&&(world.Cargo.Used>1000m*(world.Count("Cargo")-1)||world.Count("Cargo")==1&&world.Assembly.Count>0)||
                    block.type=="Assembler"&&(world.Count("Assembler")==1&&world.Assembly.Count>0||world.AssemblyReservedLitres>1000m*(world.Count("Assembler")-1)))
                {session.Tell("VIDER LA SOUTE / LA FILE AVANT LE DÉMONTAGE");return false;}
                // Stop before changing state if even the next salvaged component cannot fit.
                var last=world.BuildCosts(block.type).Last();
                if(!world.Backpack.CanAdd(new[]{new ResourceAmount(last.id,1)})){session.Tell("SAC PLEIN • DÉMONTAGE SUSPENDU");return false;}
                pendingGrindWork.TryGetValue(uid,out float credit);credit+=Mathf.Min(seconds,2)*(float)world.BuildCosts(block.type).Sum(a=>a.Value)/2;
                if(credit<1){pendingGrindWork[uid]=credit;return true;}pendingGrindWork.Remove(uid);
                plan=new ConstructionPlan{uid=uid,type=block.type,grid=grid,cell=CellOf(block),orientation=block.rotation,installed=world.BuildCosts(block.type).Select(a=>new ResourceAmount(a.id,a.Value)).ToArray(),progress=1,work=credit,removing=true};seconds=0;
                session.Ledger.Blocks.Remove(block);Mappings.RemoveAll(m=>m.uid==uid);
                Plans.Add(plan);CreatePlanVisual(plan);session.Sync();
            }
            if(!plan.removing){plan.removing=true;plan.work=0;}
            var costs=session.Ledger.BuildCosts(plan.type);float total=(float)costs.Sum(a=>a.Value);
            plan.work=Mathf.Min(total,plan.work+Mathf.Min(seconds,2)*total/2);bool changed=false;
            while(plan.work>=1&&plan.installed.Length>0)
            {
                var part=costs.Last(a=>Installed(plan,a.id)>0);var unit=new[]{new ResourceAmount(part.id,1)};
                if(!session.Ledger.Backpack.CanAdd(unit)){plan.work=.999f;session.Tell("SAC PLEIN • DÉMONTAGE SUSPENDU");break;}
                session.Ledger.Backpack.Add(unit);SetInstalled(plan,part.id,Installed(plan,part.id)-1);plan.work-=1;changed=true;
            }
            if(plan.installed.Length==0){RemovePlan(plan);session.Sync();ChangedWork(true);return true;}
            plan.work=Mathf.Min(plan.work,.999f);if(changed)ChangedWork();return changed;
        }
        public string WorkStatus(string uid)
        {
            var plan=Plans.Find(p=>p.uid==uid);var block=session.Ledger.Blocks.Find(b=>b.uid==uid);string type=plan?.type??block?.type;
            if(type==null)return null;
            var costs=session.Ledger.BuildCosts(type);
            string title=session.Ledger.Definition.Block(type).name.ToUpperInvariant()+" • "+Mathf.RoundToInt((plan?.progress??1)*100)+" %";
            return title+"\n"+string.Join("\n",costs.Select(a=>session.Ledger.Definition.Item(a.id).name+"  "+(plan==null?a.Value:Installed(plan,a.id))+" / "+a.Value));
        }
        public void ApplyPhysicalPoses()
        {
            foreach(var block in session.Ledger.Blocks)
            {
                if(!session.economy.Instances.TryGetValue(block.uid,out var instance))continue;
                var frame=Grid(GridOf(block));
                if(session.motion!=null)instance.transform.SetParent(session.motion.Root(frame.id),true);
                instance.transform.SetPositionAndRotation(frame.origin+frame.rotation*(Vector3)CellOf(block),frame.rotation*EconomyRuntime.Rotation(block.rotation));
            }
            foreach(var plan in Plans)if(planObjects.TryGetValue(plan.uid,out var visual))
            {var frame=Grid(plan.grid);if(session.motion!=null)visual.transform.SetParent(session.motion.Root(frame.id),true);visual.transform.SetPositionAndRotation(frame.origin+frame.rotation*(Vector3)plan.cell,frame.rotation*EconomyRuntime.Rotation(plan.orientation));}
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
            if(workDisplay!=null)workDisplay.Hide();
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
            if(workDisplay==null)workDisplay=new GameObject("Construction component progress").AddComponent<BlockWorkDisplay>();
            string targetUid=nearest.HasValue?(nearest.Value.collider.GetComponentInParent<ConstructionPlanMarker>()?.uid??nearest.Value.collider.GetComponentInParent<EconomyBlock>()?.uid):null;
            workDisplay.Show(held!=null&&(held.kind==HandToolKind.Welder||held.kind==HandToolKind.Grinder)?WorkStatus(targetUid):null,nearest?.point??Vector3.zero);
            if(held!=null&&held.IsOperating&&nearest.HasValue)
            {
                var hit=nearest.Value;var plan=hit.collider.GetComponentInParent<ConstructionPlanMarker>();var block=hit.collider.GetComponentInParent<EconomyBlock>();
                if(held.kind==HandToolKind.Welder&&plan!=null)Weld(plan.uid,Time.unscaledDeltaTime);
                if(held.kind==HandToolKind.Grinder&&(plan!=null||block!=null))Grind(plan!=null?plan.uid:block.uid,Time.unscaledDeltaTime);
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
            if(gridId>=0)session.modules.SelectContext(gridId);
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
