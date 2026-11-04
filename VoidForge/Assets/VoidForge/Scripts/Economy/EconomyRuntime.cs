using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VoidForge.Integration;
using VoidForge.Player;
using VoidForge.UI.Cyberdeck;

namespace VoidForge.Economy
{
    public sealed class EconomyRuntime : MonoBehaviour
    {
        public static EconomyRuntime Active {get;private set;}
        public static string ValidationSaveOverride;
        public bool MiningFaulted {get;private set;}
        public EconomyWorld World {get;private set;}
        public BlockCatalogue Catalogue {get;private set;}
        public bool Ready => World!=null;
        public bool Seated => seat!=null;
        public bool NetworkLocked => NetworkManager.Singleton!=null&&NetworkManager.Singleton.IsListening;
        public Vector3 gridOrigin=new Vector3(-5,-2,0);
        public bool saveEnabled=true;
        public string SavePath {get;private set;}
        public readonly Dictionary<string,GameObject> Instances=new Dictionary<string,GameObject>();
        public event Action<string> Notice;
        CyberdeckSession session;
        PlayerInputProvider player;
        EquipmentRig equipment;
        readonly Dictionary<string,MineralOutcrop> outcrops=new Dictionary<string,MineralOutcrop>();
        Transform grid;
        EconomyBlock seat;
        Rigidbody body;
        Vector3 exitPosition;
        bool collisions, locomotionEnabled, turnEnabled;
        ZeroGLocomotion locomotion;
        SnapTurn turn;
        float nextSnapshot,nextSave,nextTool, nextHint;
        int syncedRevision=-1;
        int snapshotSignature=int.MinValue;
        string selectedBlock="ArmorCube";
        int rotation;
        LineRenderer ghost;
        bool wasNetwork;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(SceneManager.GetActiveScene().name=="Main"&&FindFirstObjectByType<EconomyRuntime>()==null)new GameObject("VoidForge economy").AddComponent<EconomyRuntime>();
        }
        void Awake(){Active=this;}
        IEnumerator Start()
        {
            if(World!=null)yield break;
            yield return null;
            player=FindFirstObjectByType<PlayerInputProvider>();
            equipment=FindFirstObjectByType<EquipmentRig>();
            var text=Resources.Load<TextAsset>("VoidForge/Economy/Catalogue");
            var blocks=Resources.Load<BlockCatalogue>("VoidForge/Economy/Blocks");
            if(text==null||blocks==null){Debug.LogError("[VoidForge Economy] Catalogue or block prefabs missing. Run EconomyWorkshop.Build.");yield break;}
            string path=ValidationSaveOverride??(Application.isEditor?Path.Combine(Application.dataPath,"../Library/EconomyPlaytest/world-v1.json"):Path.Combine(Application.persistentDataPath,"economy-world-v1.json"));
            try{Initialize(JsonUtility.FromJson<EconomyDefinition>(text.text),blocks,Path.GetFullPath(path));}
            catch(Exception e){Debug.LogException(e);Tell("SAUVEGARDE INCOMPATIBLE OU ILLISIBLE • FICHIER CONSERVÉ");yield break;}
            var prototype=FindFirstObjectByType<BlockGrid>();if(prototype!=null)prototype.gameObject.SetActive(false);
            gameObject.AddComponent<EconomyMiningAdapter>();
            Debug.Log("[VoidForge Economy] Live inventory, 35 production recipes, 11 one-metre blocks and persistent finite deposits ready.");
        }
        public void Initialize(EconomyDefinition definition,BlockCatalogue catalogue,string path)
        {
            if(World!=null)throw new InvalidOperationException("Economy already initialized");
            Catalogue=catalogue;SavePath=path;
            foreach(var block in definition.blocks)if(catalogue.Find(block.id)==null||!catalogue.Find(block.id).Available)throw new InvalidOperationException("Block prefab missing: "+block.id);
            if(saveEnabled&&File.Exists(path))World=EconomyWorld.Restore(definition,JsonUtility.FromJson<EconomySave>(File.ReadAllText(path)));
            else {World=new EconomyWorld(definition);SeedNewWorld();}
            grid=new GameObject("Playable one-metre grid").transform;grid.SetParent(transform,false);grid.position=gridOrigin;
            SyncWorld();CreateDeposits();
            if(saveEnabled&&!File.Exists(path))Save();
        }
        void SeedNewWorld()
        {
            // One-time new-world equipment and workshop; never reapplied to a loaded save.
            for(int x=0;x<6;x++)for(int z=0;z<5;z++)World.Blocks.Add(new BuiltBlock("ArmorCube",new GridCell(x,0,z),0));
            World.Blocks.Add(new BuiltBlock("Cargo",new GridCell(0,1,0),0));
            World.Blocks.Add(new BuiltBlock("Battery",new GridCell(1,1,0),0));
            World.Blocks.Add(new BuiltBlock("Refinery",new GridCell(2,1,0),0));
            World.Blocks.Add(new BuiltBlock("Assembler",new GridCell(3,1,0),0));
            World.Blocks.Add(new BuiltBlock("SolarPanel",new GridCell(5,1,0),0));
            World.Blocks.Add(new BuiltBlock("Cockpit",new GridCell(0,1,2),0));
            World.EnergyKwh=4;
            World.Backpack.Add(new[]{A("tool-drill",1),A("tool-welder",1),A("tool-grinder",1),A("component-plates",16),A("component-construction",12)});
            World.Cargo.Add(new[]{A("ingot-iron",100),A("ingot-nickel",20),A("wafer-silicon",20),A("ingot-cobalt",5)});
        }
        static ResourceAmount A(string id,decimal value)=>new ResourceAmount(id,value);
        public bool HasCargoAccess => Ready&&World.Count("Cargo")>0&&(Seated||(Camera.main!=null&&Vector3.Distance(Camera.main.transform.position,gridOrigin+new Vector3(2,1,2))<=8));
        public bool ToolOwned(int n)=>Ready&&n>=0&&n<3&&World.Backpack.Get(new[]{"tool-drill","tool-welder","tool-grinder"}[n])>=1;
        public bool BlockSupported(string id)=>Ready&&World.Definition.Block(id)!=null&&Catalogue.Find(id)?.Available==true;
        public Vector3Int BlockSize(string id){var b=World?.Definition.Block(id);return b==null?Vector3Int.one:new Vector3Int(b.x,b.y,b.z);}
        public void Attach(CyberdeckSession target)
        {
            if(session==target)return;Detach();session=target;session.preview=false;snapshotSignature=int.MinValue;
            session.AssemblyCommand=Queue;session.CancelAssemblyRequested+=Cancel;
            session.StorageRequested+=SelectStorage;session.TransferRequested+=Transfer;
            session.RefineryPowerRequested+=Power;session.RefineryPauseRequested+=PauseRefinery;session.AssemblyPauseRequested+=PauseAssembly;
            session.BlockRequested+=SelectBlock;session.RefineryOreRequested+=ChooseOre;session.SeatExitRequested+=ExitSeat;
            session.BlockIngredients=BlockIngredients;
            PublishSession();
        }
        void Detach()
        {
            if(session==null)return;
            session.AssemblyCommand=null;session.CancelAssemblyRequested-=Cancel;session.StorageRequested-=SelectStorage;session.TransferRequested-=Transfer;
            session.RefineryPowerRequested-=Power;session.RefineryPauseRequested-=PauseRefinery;session.AssemblyPauseRequested-=PauseAssembly;
            session.BlockRequested-=SelectBlock;session.RefineryOreRequested-=ChooseOre;session.SeatExitRequested-=ExitSeat;session.BlockIngredients=null;session=null;
        }
        public List<DeckIngredient> BlockIngredients(string id)
        {
            var costs=World?.BuildCosts(id);return costs==null?new List<DeckIngredient>():costs.Select(x=>Ingredient(x,World.ConstructionStock(x.id,HasCargoAccess))).ToList();
        }
        DeckIngredient Ingredient(ResourceAmount amount,decimal stock)
        {var d=World.Definition.Item(amount.id);return new DeckIngredient{id=d.id,name=d.name.ToUpperInvariant(),icon=d.icon,unit=d.unit,perUnit=(float)amount.Value,stock=(float)stock};}
        bool CanCommand(bool requireSeat=false)
        {
            if(MiningFaulted){Tell("RESTAURATION DU MINAGE ÉCHOUÉE • SAUVEGARDE CONSERVÉE");return false;}
            if(!Ready||NetworkLocked||(requireSeat&&!Seated)){Tell(NetworkLocked?"ÉCONOMIE SOLO • QUITTER LA SESSION RÉSEAU":requireSeat?"S’ASSEOIR DANS UN COCKPIT":"ÉCONOMIE NON CONNECTÉE");return false;}return true;
        }
        bool Queue(string id,int count){if(!CanCommand(true))return false;bool ok=World.Enqueue(id,count);Tell(ok?"FABRICATION AJOUTÉE • INGRÉDIENTS RÉSERVÉS":"STOCK INSUFFISANT OU ASSEMBLEUR ABSENT");if(ok)Save();PublishSession();return ok;}
        void Cancel(string id){if(!CanCommand(true))return;bool ok=World.Cancel(id);Tell(ok?"FILE ANNULÉE • INGRÉDIENTS RESTITUÉS":"ANNULATION IMPOSSIBLE • LIBÉRER DE LA SOUTE");if(ok)Save();PublishSession();}
        void SelectStorage(string id){if(session==null)return;session.selectedStorage=id=="cargo"&&HasCargoAccess?"cargo":"backpack";PublishSession();}
        void Transfer(string id)
        {
            if(!CanCommand()||!HasCargoAccess){Tell("APPROCHER LA SOUTE POUR TRANSFÉRER");return;}
            bool fromCargo=session.selectedStorage=="cargo";var source=fromCargo?World.Cargo:World.Backpack;decimal quantity=source.Get(id);
            if(!fromCargo&&equipment!=null&&equipment.Held!=null&&id==ToolId(equipment.Held.kind))quantity=Math.Max(0,quantity-1);
            bool ok=World.Transfer(fromCargo,id,quantity);Tell(ok?"TRANSFERT EFFECTUÉ":"TRANSFERT IMPOSSIBLE • STOCK PLEIN OU OUTIL EN MAIN");if(ok)Save();PublishSession();
        }
        void Power(bool on){if(!CanCommand(true))return;World.RefineryPower=on;Save();PublishSession();}
        void PauseRefinery(bool pause){if(!CanCommand(true))return;World.RefineryPaused=pause;Save();PublishSession();}
        void PauseAssembly(bool pause){if(!CanCommand(true))return;World.AssemblyPaused=pause;Save();PublishSession();}
        void ChooseOre(string id){if(!CanCommand(true))return;World.PriorityOre=id;World.RefineryProgress=0;Save();PublishSession();}
        void SelectBlock(string id){if(!CanCommand()||!BlockSupported(id))return;selectedBlock=id;Tell("PLAN SÉLECTIONNÉ • SOUDEUSE + GÂCHETTE POUR POSER • X POUR TOURNER");}
        public static string ToolId(HandToolKind kind)=>kind==HandToolKind.MiningDrill?"tool-drill":kind==HandToolKind.Welder?"tool-welder":"tool-grinder";
        void Tell(string message){Notice?.Invoke(message);if(session!=null)session.Notify(message);}

        void Update()
        {
            if(!Ready)return;
            World.ReadOnly=NetworkLocked||MiningFaulted;
            if(NetworkLocked!=wasNetwork){wasNetwork=NetworkLocked;if(NetworkLocked)ExitSeat();Tell(NetworkLocked?"ÉCONOMIE SOLO • COMMANDES ET PRODUCTION SUSPENDUES":"ÉCONOMIE SOLO REPRISE");}
            if(!NetworkLocked){World.Tick(Time.unscaledDeltaTime);if(Time.unscaledTime>=nextSave){nextSave=Time.unscaledTime+5;Save();}}
            if(World.Revision!=syncedRevision)SyncWorld();
            HandleInputs();
            if(Time.unscaledTime>=nextSnapshot){nextSnapshot=Time.unscaledTime+.3f;PublishSession();}
        }
        void HandleInputs()
        {
            if(!Application.isFocused||NetworkLocked){if(ghost!=null)ghost.enabled=false;return;}
            if(player==null)player=FindFirstObjectByType<PlayerInputProvider>();
            if(equipment==null)equipment=FindFirstObjectByType<EquipmentRig>();
            bool use=player?.Actions!=null&&player.Actions.RightPrimary.WasPressedThisFrame();
            bool rotate=player?.Actions!=null&&player.Actions.LeftPrimary.WasPressedThisFrame();
            if(Keyboard.current!=null){use|=Keyboard.current.eKey.wasPressedThisFrame;rotate|=Keyboard.current.rKey.wasPressedThisFrame;}
            if(use){if(Seated)ExitSeat();else TryEnterNearestSeat();}
            if(rotate)rotation=(rotation+1)%24;
            var tool=equipment!=null&&equipment.HasTrackedRightHand?equipment.Held:null;
            if(tool==null||tool.tip==null||Seated){if(ghost!=null)ghost.enabled=false;return;}
            if(session!=null&&session.gameObject.GetComponent<CyberdeckPointer>() is CyberdeckPointer pointer&&pointer.IsPointing){if(ghost!=null)ghost.enabled=false;return;}
            bool hit=Physics.Raycast(new Ray(tool.tip.position,tool.tip.forward),out var raycast,1f,~0,QueryTriggerInteraction.Ignore);
            var block=hit?raycast.collider.GetComponentInParent<EconomyBlock>():null;
            var deposit=hit?raycast.collider.GetComponentInParent<MineralOutcrop>():null;
            if(tool.kind==HandToolKind.Welder&&block!=null)
            {
                Vector3 local=grid.InverseTransformPoint(raycast.point+raycast.normal*.02f);
                var cell=new GridCell(Mathf.RoundToInt(local.x),Mathf.RoundToInt(local.y),Mathf.RoundToInt(local.z));
                bool valid=World.CanPlace(selectedBlock,cell,rotation)&&World.CanAfford(selectedBlock,HasCargoAccess);
                DrawGhost(cell,selectedBlock,rotation,valid);
                if(tool.IsOperating&&Time.unscaledTime>=nextTool){nextTool=Time.unscaledTime+.35f;if(BuildAt(selectedBlock,cell,rotation))Tell("BLOC CONSTRUIT • "+World.Definition.Block(selectedBlock).name.ToUpperInvariant());else Hint("PLACEMENT OCCUPÉ, NON ADJACENT OU COMPOSANTS MANQUANTS");}
            }
            else
            {
                if(ghost!=null)ghost.enabled=false;
                if(!tool.IsOperating||Time.unscaledTime<nextTool)return;
                nextTool=Time.unscaledTime+.35f;
                if(tool.kind==HandToolKind.MiningDrill&&deposit!=null)
                {decimal n=World.Mine(deposit.id,8);if(n>0){deposit.Refresh();Tell("+ "+n.ToString("0.###",CultureInfo.InvariantCulture)+" kg • "+World.Definition.Item(deposit.item).name);Save();}else Hint("SAC PLEIN OU GISEMENT ÉPUISÉ");}
                else if(tool.kind==HandToolKind.Grinder&&block!=null)
                {if(World.Dismantle(block.uid,HasCargoAccess)){SyncWorld();Save();Tell("BLOC DÉMONTÉ • COMPOSANTS RÉCUPÉRÉS");}else Hint("DÉMONTAGE IMPOSSIBLE • STOCK, PRODUCTION OU CONNEXION À CONSERVER");}
            }
        }
        void Hint(string text){if(Time.unscaledTime>=nextHint){nextHint=Time.unscaledTime+2;Tell(text);}}
        public bool BuildAt(string id,GridCell cell,int orientation)
        {if(!CanCommand()||!BlockSupported(id)||!World.Build(id,cell,orientation,HasCargoAccess))return false;SyncWorld();Save();PublishSession();return true;}
        public static Vector3 Vector(GridCell c)=>new Vector3(c.x,c.y,c.z);
        public static Quaternion Rotation(int index){var r=GridRotations.All[index];return Quaternion.LookRotation(Vector(r.forward),Vector(r.up));}
        void DrawGhost(GridCell cell,string id,int orientation,bool valid)
        {
            if(ghost==null){ghost=new GameObject("Construction wire ghost").AddComponent<LineRenderer>();ghost.transform.SetParent(transform,false);ghost.sharedMaterial=EconomyBlockModels.Material("ghost",new Color(.3f,.9f,.8f));ghost.startWidth=ghost.endWidth=.012f;ghost.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
            var points=new List<Vector3>();
            foreach(var c in World.Cells(new BuiltBlock{type=id,cell=cell,rotation=orientation}))
            {
                Vector3 centre=grid.TransformPoint(Vector(c));
                int[] order={0,1,3,2,0,4,5,1,5,7,3,7,6,2,6,4};
                foreach(int k in order)points.Add(centre+new Vector3((k&1)==0?-.49f:.49f,(k&2)==0?-.49f:.49f,(k&4)==0?-.49f:.49f));
            }
            ghost.enabled=true;ghost.positionCount=points.Count;ghost.SetPositions(points.ToArray());ghost.startColor=ghost.endColor=valid?Color.cyan:new Color(1,.25f,.1f);
        }
        void SyncWorld()
        {
            if(grid==null)return;
            var ids=new HashSet<string>(World.Blocks.Select(b=>b.uid));
            foreach(var pair in Instances.Where(x=>!ids.Contains(x.Key)).ToArray()){Destroy(pair.Value);Instances.Remove(pair.Key);}
            foreach(var b in World.Blocks)
            {
                if(Instances.ContainsKey(b.uid))continue;
                var instance=Instantiate(Catalogue.Find(b.type).prefab,grid);instance.name=b.type+" • "+b.uid.Substring(0,6);
                instance.transform.SetLocalPositionAndRotation(Vector(b.cell),Rotation(b.rotation));
                var marker=instance.AddComponent<EconomyBlock>();marker.uid=b.uid;marker.type=b.type;marker.runtime=this;
                Instances.Add(b.uid,instance);
            }
            syncedRevision=World.Revision;
            SyncLoosePickups();
        }
        void SyncLoosePickups()
        {
            foreach(var state in World.Deposits.Values.Where(x=>x.loose))
            {
                if(outcrops.TryGetValue(state.id,out var existing)){existing.Refresh();continue;}
                var go=new GameObject("Loose stone pickup");go.transform.SetParent(transform,false);go.transform.position=new Vector3(state.x,state.y,state.z);
                var node=go.AddComponent<MineralOutcrop>();node.Initialize(this,state.id,state.item,9);outcrops.Add(state.id,node);
            }
        }
        public void MarkMiningFault(){MiningFaulted=true;if(World!=null)World.ReadOnly=true;Tell("RESTAURATION DU MINAGE ÉCHOUÉE • SAUVEGARDE CONSERVÉE");}
        void CreateDeposits()
        {
            var ids=new[]{"ore-iron","ore-nickel","ore-silicon","ore-cobalt","ore-magnesium","ore-silver","ore-gold","ore-platinum","ore-uranium","ore-stone","ore-ice","scrap-metal"};
            for(int i=0;i<ids.Length;i++)
            {
                string id="outcrop-"+i.ToString("00");World.RegisterDeposit(id,ids[i],i==0?12000:i==9?12000:i==11?500:5000);
                var go=new GameObject(World.Definition.Item(ids[i]).name+" • gisement fini");go.transform.SetParent(transform,false);
                go.transform.position=new Vector3(-10+(i%6)*4,-.2f,9+(i/6)*5);
                var node=go.AddComponent<MineralOutcrop>();node.Initialize(this,id,ids[i],i);outcrops.Add(id,node);
            }
            SyncLoosePickups();
        }
        public void AddWaypoints(CyberdeckSession target)
        {
            if(!Ready)return;
            if(!target.waypoints.Any(w=>w.id=="workshop"))target.waypoints.Add(new DeckWaypoint{id="workshop",name="ATELIER",category="BASES",icon="map-outpost",position=gridOrigin+new Vector3(2,1,2)});
            foreach(var pair in outcrops)if(!target.waypoints.Any(w=>w.id==pair.Key))target.waypoints.Add(new DeckWaypoint{id=pair.Key,name=World.Definition.Item(pair.Value.item).name.ToUpperInvariant(),category="MINERAIS",icon="map-asteroid",target=pair.Value.transform});
        }
        public bool EnterSeat(EconomyBlock cockpit)
        {
            if(player==null)player=FindFirstObjectByType<PlayerInputProvider>();
            if(!CanCommand()||cockpit==null||cockpit.type!="Cockpit"||Seated||player==null||Camera.main==null)return false;
            if(Vector3.Distance(Camera.main.transform.position,cockpit.transform.position)>3.5f)return false;
            seat=cockpit;exitPosition=player.transform.position;body=player.GetComponent<Rigidbody>();
            locomotion=player.GetComponent<ZeroGLocomotion>();turn=player.GetComponent<SnapTurn>();
            if(locomotion!=null){locomotionEnabled=locomotion.enabled;locomotion.enabled=false;}if(turn!=null){turnEnabled=turn.enabled;turn.enabled=false;}
            if(body!=null){collisions=body.detectCollisions;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.detectCollisions=false;}
            Vector3 head=cockpit.transform.TransformPoint(new Vector3(.5f,1.65f,1));player.transform.position+=head-Camera.main.transform.position;
            Tell("COCKPIT CONNECTÉ • A / E POUR SORTIR");PublishSession();return true;
        }
        void TryEnterNearestSeat()
        {if(Camera.main==null)return;var nearest=Instances.Values.Select(g=>g.GetComponent<EconomyBlock>()).Where(b=>b.type=="Cockpit").OrderBy(b=>Vector3.SqrMagnitude(Camera.main.transform.position-b.transform.position)).FirstOrDefault();if(!EnterSeat(nearest))Tell("APPROCHER UN COCKPIT • A / E POUR S’ASSEOIR");}
        public void ExitSeat()
        {
            if(!Seated)return;seat=null;
            if(player!=null)player.transform.position=exitPosition;
            if(body!=null){body.linearVelocity=Vector3.zero;body.detectCollisions=collisions;}
            if(locomotion!=null)locomotion.enabled=locomotionEnabled;if(turn!=null)turn.enabled=turnEnabled;
            Tell("COCKPIT DÉCONNECTÉ");PublishSession();
        }
        public void PublishSession()
        {
            if(session==null||!Ready)return;
            if(session.GetComponent<CyberdeckPointer>() is CyberdeckPointer pointer&&pointer.IsInteracting)return;
            session.preview=false;bool access=HasCargoAccess;
            if(Camera.main!=null)session.playerPosition=Camera.main.transform.position;
            if(session.selectedStorage!="cargo"||!access)session.selectedStorage="backpack";
            int signature=World.Revision*397^(Seated?1:0)^(access?2:0)^(World.RefineryPower?4:0)^(World.RefineryPaused?8:0)^(World.AssemblyPaused?16:0)^(World.OutputBlocked?32:0)^(NetworkLocked?64:0)^session.selectedStorage.GetHashCode();
            var hud=session.GetComponent<CyberdeckHud>();
            if(hud!=null)
            {
                signature^=(int)hud.page*16777619;
                if(hud.page==DeckPage.Refinery)signature^=Mathf.FloorToInt(World.RefineryProgress*10)*8191;
                if(hud.page==DeckPage.Assembly&&World.Assembly.Count>0)signature^=Mathf.FloorToInt(World.Assembly[0].progress*10)*8191;
            }
            signature^=Mathf.FloorToInt(World.EnergyKwh*25)*65521;
            if(signature==snapshotSignature)return;snapshotSignature=signature;
            var stock=session.selectedStorage=="cargo"?World.Cargo:World.Backpack;
            session.storages.Clear();session.storages.Add(new DeckStorage{id="backpack",name="SAC PERSONNEL",available=true});session.storages.Add(new DeckStorage{id="cargo",name="ATELIER • SOUTE",available=access});
            session.cargoConnected=true;session.cargoName=session.selectedStorage=="cargo"?"ATELIER":"SAC PERSONNEL";
            session.usedLitres=(float)stock.Used;session.capacityLitres=(float)stock.Capacity;session.transferAvailable=access&&!NetworkLocked;
            session.shipName="ATELIER VOIDFORGE";session.cockpitConnected=Seated&&!NetworkLocked;
            session.refineryAvailable=World.HasMachine("Refinery");session.assemblerAvailable=World.HasMachine("Assembler");
            session.refineryPowered=World.RefineryPower;session.refineryPaused=World.RefineryPaused;session.assemblyPaused=World.AssemblyPaused;session.outputBlocked=World.OutputBlocked;
            session.battery=World.MaxEnergy>0?World.EnergyKwh/World.MaxEnergy:0;session.refineryKw=World.RefineryPower&&World.RefiningRecipe!=null&&!World.RefineryPaused?120:0;session.refineryProgress=World.RefineryProgress;
            session.items=stock.Snapshot().Select(x=>{var d=World.Definition.Item(x.id);return new DeckItem(d.id,d.name.ToUpperInvariant(),d.icon,(float)x.Value,d.family!="minerais"&&d.family!="recyclage",d.unit);}).ToList();
            session.toolsAvailable=new[]{ToolOwned(0),ToolOwned(1),ToolOwned(2)};
            session.recipes=World.Definition.recipes.Where(r=>r.machine=="assembler").Select(r=>new DeckRecipe{id=r.id,name=r.name.ToUpperInvariant(),icon=r.icon,available=true,maxCraftable=World.Maximum(r),ingredients=World.Definition.Inputs(r).Select(x=>Ingredient(x,World.Cargo.Get(x.id))).ToList()}).ToList();
            session.assembly=World.Assembly.Select(j=>{var r=World.Definition.Recipe(j.recipe);return new DeckJob{uid=j.uid,recipeId=r.id,name=r.name.ToUpperInvariant(),icon=r.icon,quantity=j.remaining,progress=j.progress};}).ToList();
            var ores=World.Definition.recipes.Where(r=>r.machine=="refinery"&&World.Cargo.Get(r.inputs[0].id)>0).OrderByDescending(r=>r.inputs[0].id==World.PriorityOre).ToList();
            session.refining=ores.Select(r=>{var d=World.Definition.Item(r.inputs[0].id);return new DeckItem(d.id,d.name.ToUpperInvariant(),d.icon,(float)World.Cargo.Get(d.id));}).ToList();
            var active=World.Definition.Recipe(World.RefiningRecipe)??ores.FirstOrDefault();
            session.refineryOutputs=active==null?new List<DeckItem>():active.outputs.Select(x=>{var d=World.Definition.Item(x.id);return new DeckItem(d.id,d.name.ToUpperInvariant(),d.icon,(float)x.Value,true,d.unit);}).ToList();
            session.refineryBatch=active==null?0:(float)active.inputs[0].Value;
            session.Publish();
        }
        public void Save()
        {
            if(!Ready||!saveEnabled||NetworkLocked||string.IsNullOrEmpty(SavePath))return;
            string directory=Path.GetDirectoryName(SavePath);Directory.CreateDirectory(directory);
            string temporary=SavePath+".tmp";File.WriteAllText(temporary,JsonUtility.ToJson(World.Snapshot(),true));
            if(File.Exists(SavePath))File.Replace(temporary,SavePath,SavePath+".bak");else File.Move(temporary,SavePath);
        }
        void OnApplicationPause(bool pause){if(pause)Save();}
        void OnApplicationQuit(){Save();}
        void OnDestroy(){ExitSeat();Detach();if(Active==this)Active=null;}
    }
    public sealed class EconomyBlock:MonoBehaviour {public string uid,type;public EconomyRuntime runtime;}

    public sealed class MineralOutcrop:MonoBehaviour
    {
        public string id,item;
        EconomyRuntime runtime;
        Transform visual;
        TextMesh label;
        float initial;
        public void Initialize(EconomyRuntime runtime,string id,string item,int index)
        {
            this.runtime=runtime;this.id=id;this.item=item;initial=runtime.World.Deposits[id].loose?Mathf.Max(1,(float)decimal.Parse(runtime.World.Deposits[id].remaining,CultureInfo.InvariantCulture)):index==0||index==9?12000:index==11?500:5000;
            visual=new GameObject("Finite deposit visual").transform;visual.SetParent(transform,false);
            var source=Resources.Load<VoidForge.World.AsteroidAssetSet>(VoidForge.World.AsteroidAssetSet.ResourcePath);
            if(source!=null&&source.IsUsable)source.AddVisual(visual.gameObject,index);
            else {var rock=GameObject.CreatePrimitive(PrimitiveType.Sphere);rock.transform.SetParent(visual,false);rock.transform.localScale=new Vector3(1.2f,.9f,1);rock.GetComponent<Renderer>().sharedMaterial=EconomyBlockModels.Material("ore-rock",new Color(.2f,.22f,.24f));}
            Color[] colors={new Color(.6f,.3f,.1f),Color.gray,new Color(.7f,.8f,.85f),new Color(.1f,.3f,.8f),new Color(.8f,.8f,.7f),new Color(.8f,.8f,.9f),new Color(1,.6f,.1f),new Color(.7f,.85f,.85f),new Color(.35f,.4f,.15f),new Color(.3f,.3f,.3f),new Color(.5f,.9f,1),new Color(.5f,.3f,.2f)};
            for(int n=0;n<4;n++){var seam=GameObject.CreatePrimitive(PrimitiveType.Cube);seam.transform.SetParent(visual,false);seam.transform.localPosition=new Vector3((n%2-.5f)*.5f,.25f,(n/2-.5f)*.45f);seam.transform.localScale=new Vector3(.12f,.15f,.7f);seam.transform.localRotation=Quaternion.Euler(15,25+n*12,0);seam.GetComponent<Renderer>().sharedMaterial=EconomyBlockModels.Material("ore-"+index,colors[index]);}
            var text=new GameObject("Ore label");text.transform.SetParent(transform,false);text.transform.localPosition=Vector3.up*.9f;
            label=text.AddComponent<TextMesh>();label.characterSize=.035f;label.fontSize=40;label.anchor=TextAnchor.MiddleCenter;label.color=Color.cyan;
            Refresh();
        }
        public void Refresh()
        {
            decimal left=decimal.Parse(runtime.World.Deposits[id].remaining,CultureInfo.InvariantCulture);
            visual.gameObject.SetActive(left>0);visual.localScale=Vector3.one*Mathf.Max(.15f,Mathf.Pow((float)left/initial,1f/3));
            label.text=runtime.World.Definition.Item(item).name.ToUpperInvariant()+"\n"+(left>0?left.ToString("0.##",CultureInfo.InvariantCulture)+" kg":"ÉPUISÉ");
        }
        void LateUpdate(){if(label!=null&&Camera.main!=null)label.transform.rotation=Quaternion.LookRotation(label.transform.position-Camera.main.transform.position,Camera.main.transform.up);}
    }
}
