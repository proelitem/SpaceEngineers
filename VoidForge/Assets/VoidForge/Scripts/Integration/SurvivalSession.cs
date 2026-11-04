#if VF_LOCAL_ECONOMY
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidForge.Economy;
using VoidForge.UI.Cyberdeck;

namespace VoidForge.Integration
{
    [Serializable] public sealed class AsteroidExcavation
    {
        public string uid;
        public int asteroid;
        public Vector3 point;
        public float radius = .75f;
    }
    [Serializable] public sealed class SurvivalSave
    {
        public int format = 2;
        public ModuleLedgerSave[] modules;
        public ModuleSystemState[] systems;
        public bool starterPowerProvisioned;
        public string oreProfile = World.AsteroidOreProfile.Version;
        public int seed;
        public string ledger;
        public ConstructionGridState[] grids;
        public ConstructionPlan[] plans;
        public ConstructedBlock[] mappedBlocks;
        public AsteroidExcavation[] excavations;
        public int selectedTool;
    }

    // Own the expanded solo loop without overwriting the concurrently authored economy/UI.
    // The economy ledger remains the single inventory/production authority.
    public sealed class SurvivalSession : MonoBehaviour
    {
        public static SurvivalSession Active { get; private set; }
        public static event Action<SurvivalSession> WorldReady;
        public static SurvivalSave Loaded { get; private set; }
        public static string ValidationSaveOverride;
        public static string SavePath { get; private set; }
        public static bool Enabled => SurvivalSettings.Supported && Resources.Load<SurvivalSettings>(SurvivalSettings.Resource) != null;
        static string loadFault;
        public EconomyRuntime economy { get; private set; }
        public EconomyWorld Ledger => economy.World;
        public EquipmentRig equipment { get; private set; }
        public ConstructionSystem construction { get; private set; }
        public SurvivalDeckAdapter deck { get; private set; }
        public ModuleSystems modules { get; private set; }
        public ModuleMotion motion {get;private set;}
        public bool Ready { get; private set; }
        public bool Faulted { get; private set; }
        bool starterPowerProvisioned;
        public readonly List<AsteroidExcavation> Excavations = new List<AsteroidExcavation>();
        Transform ledgerGrid;
        VoidForge.Player.PlayerInputProvider input;
        int revision = -1;
        float nextSave;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Prepare()
        {
            Active = null; Loaded = null; loadFault = null;
            if (!Enabled) return;
            SavePath = ValidationSaveOverride ?? (Application.isEditor ? Path.GetFullPath("Library/SurvivalPlaytest/world-v1.json") : Path.Combine(Application.persistentDataPath,"survival-world-v1.json"));
            var arguments=Environment.GetCommandLineArgs();
            int overrideIndex=Array.IndexOf(arguments,"-vfSurvivalSave");
            if(overrideIndex>=0&&overrideIndex+1<arguments.Length)SavePath=Path.GetFullPath(arguments[overrideIndex+1]);
            string cache = Application.isEditor ? Path.GetFullPath("Library/SurvivalPlaytest") : Path.Combine(Application.persistentDataPath,"SurvivalCache");
            Directory.CreateDirectory(cache);
            // The temporary ledger is only an import boundary. The envelope below is canonical
            // and atomically saves stock, construction plans, grid poses and voxel edits together.
            string boot = Path.Combine(cache,"ledger-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                if (File.Exists(SavePath))
                {
                    Loaded = JsonUtility.FromJson<SurvivalSave>(File.ReadAllText(SavePath));
                    if(Loaded!=null&&Loaded.format==2&&(Loaded.modules==null||Loaded.systems==null))throw new InvalidDataException("Survival format 2 module state missing; save preserved.");
                    if (Loaded == null || (Loaded.format != 1 && Loaded.format != 2) || Loaded.oreProfile != World.AsteroidOreProfile.Version ||
                        string.IsNullOrEmpty(Loaded.ledger) || Loaded.seed != Resources.Load<SurvivalSettings>(SurvivalSettings.Resource).seed)
                        throw new InvalidDataException("Survival save/profile incompatible; original file preserved.");
                    File.WriteAllText(boot,Loaded.ledger);
                }
            }
            catch (Exception error) { loadFault = error.ToString(); Debug.LogException(error); }
            EconomyRuntime.ValidationSaveOverride = boot;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Enabled && SceneManager.GetActiveScene().name == "Main")
                new GameObject("VoidForge survival loop").AddComponent<SurvivalSession>();
        }
        void Awake() { Active = this; }
        IEnumerator Start()
        {
            double deadline=Time.realtimeSinceStartupAsDouble+60;
            while (EconomyRuntime.Active == null || !EconomyRuntime.Active.Ready)
            {
                if(Time.realtimeSinceStartupAsDouble>deadline){Fail("Economy initialization failed or timed out; save preserved.");yield break;}
                yield return null;
            }
            economy = EconomyRuntime.Active;
            economy.saveEnabled = false;
            economy.enabled = false;
            if (loadFault != null) { Fail(loadFault); yield break; }
            equipment = FindFirstObjectByType<EquipmentRig>();
            input = FindFirstObjectByType<VoidForge.Player.PlayerInputProvider>();
            ledgerGrid = economy.GridTransform;
            foreach (var outcrop in FindObjectsByType<MineralOutcrop>(FindObjectsSortMode.None))
                if (Ledger.Deposits.TryGetValue(outcrop.id,out var state) && !state.loose) outcrop.gameObject.SetActive(false);
            var adapter = economy.GetComponent<EconomyMiningAdapter>();
            if (adapter != null) adapter.enabled = false;
            if (Loaded == null) SeedBase();
            else if (Loaded.excavations != null) Excavations.AddRange(Loaded.excavations);
            construction = gameObject.AddComponent<ConstructionSystem>();
            construction.Initialize(this,ledgerGrid,Loaded);
            modules=gameObject.AddComponent<ModuleSystems>();modules.Initialize(this,Loaded);
            motion=gameObject.AddComponent<ModuleMotion>();motion.Initialize(this);
            Sync();
            deck = gameObject.AddComponent<SurvivalDeckAdapter>();
            deck.session = this;
            Ready = true;
            starterPowerProvisioned=Loaded?.starterPowerProvisioned??false;
            if(Loaded?.format==1&&modules.World(0).Count("SolarPanel")==0&&modules.World(0).Count("Reactor")==0&&modules.Available(0)<.01f)
                starterPowerProvisioned=construction.ProvisionLegacyStarterSolar();
            WorldReady?.Invoke(this);
            if (Loaded != null && equipment != null && Loaded.selectedTool >= 0 && Loaded.selectedTool < equipment.tools.Length)
                equipment.SelectTool(equipment.tools[Loaded.selectedTool]);
            Save();
            Debug.Log("[VoidForge Survival] Four-block starter base, blueprint/welding loop and unified solo save ready.");
            Debug.Log("[VoidForge Recette04] Component construction and per-module systems ready; save format 2.");
        }
        void SeedBase()
        {
            Ledger.Blocks.Clear();
            void Add(string type,GridCell cell)
            {
                if(!Ledger.CanPlace(type,cell,0,false))throw new InvalidOperationException("Starter base overlaps or has invalid block: "+type);
                Ledger.Blocks.Add(new BuiltBlock(type,cell,0));
            }
            for (int x=0;x<2;x++) for (int z=0;z<2;z++) Add("ArmorCube",new GridCell(x,0,z));
            Add("Battery",new GridCell(0,1,0));
            Add("Refinery",new GridCell(1,1,0)); // 1 x 2 x 1, tall column.
            Add("Assembler",new GridCell(0,1,1)); // 2 x 1 x 1, complete rear row.
            Add("Cargo",new GridCell(0,2,0)); // Above battery; never inside assembler.
            Ledger.Cargo.SetCapacity(1000);
            Ledger.EnergyKwh = 4;
        }
        public void Sync()
        {
            economy.RefreshWorldInstances();
            modules?.Refresh();
            motion?.Refresh();
            construction?.ApplyPhysicalPoses();
            revision = Ledger.Revision;
            deck?.Publish(true);
        }
        void Update()
        {
            if (!Ready) return;
            Ledger.ReadOnly = economy.NetworkLocked || Faulted;
            if(economy.NetworkLocked&&economy.Seated)economy.ExitSeat();
            if (!Ledger.ReadOnly) modules.Tick(Time.unscaledDeltaTime);
            if(CanAct&&Application.isFocused&&input?.Actions!=null&&input.Actions.RightPrimary.WasPressedThisFrame()&&deck!=null&&!deck.PointerBusy&&
                (economy.Seated||deck.Hud!=null&&deck.Hud.page!=DeckPage.Construction))
            {
                if(economy.Seated)economy.ExitSeat();
                else if(Camera.main!=null)
                {
                    var nearest=economy.Instances.Values.Select(go=>go.GetComponent<EconomyBlock>()).Where(b=>b.type=="Cockpit")
                        .OrderBy(b=>Vector3.SqrMagnitude(b.transform.position-Camera.main.transform.position)).FirstOrDefault();
                    if(nearest!=null)economy.EnterSeat(nearest);
                }
            }
            if (revision != Ledger.Revision) Sync();
            if (Time.unscaledTime >= nextSave) { nextSave = Time.unscaledTime + 5; Save(); }
        }
        public void Tell(string text) { if (deck != null && deck.State != null && deck.State.Notice != text) deck.State.Notify(text); }
        public bool CanAct => Ready && !Faulted && !economy.NetworkLocked && !Ledger.ReadOnly;
        public bool CargoAccess => modules!=null&&modules.Access(modules.ContextId);
        public bool OwnTool(int uiIndex) => economy.ToolOwned(uiIndex);

        public void CreditExcavation(AsteroidExcavation excavation,double[] quantities)
        {
            if (Excavations.Any(e=>e.uid==excavation.uid)) throw new InvalidOperationException("Duplicate voxel excavation credit: " + excavation.uid);
            for (int i=0;i<quantities.Length;i++)
            {
                if (quantities[i]<=0) continue;
                decimal amount = decimal.Round((decimal)quantities[i]*2.5m,6);
                if (amount<=0) continue;
                string id = "voxel-" + excavation.uid + "-" + i;
                Ledger.RegisterDeposit(id,World.AsteroidOreProfile.Items[i],amount);
                Ledger.Mine(id,amount);
                var state = Ledger.Deposits[id];
                if (decimal.Parse(state.remaining,CultureInfo.InvariantCulture)>0)
                { state.loose = true; state.x = excavation.point.x; state.y = excavation.point.y; state.z = excavation.point.z; }
                else Ledger.Deposits.Remove(id);
            }
            Excavations.Add(excavation);
            Sync(); Save();
            Tell("MATIÈRE EXTRAITE • MINERAIS DANS LE SAC / EXCÉDENT À RAMASSER");
        }

        public void Save()
        {
            if (!Ready || Faulted || economy.NetworkLocked || string.IsNullOrEmpty(SavePath)) return;
            var settings = Resources.Load<SurvivalSettings>(SurvivalSettings.Resource);
            var save = new SurvivalSave { starterPowerProvisioned=starterPowerProvisioned,modules=modules.SaveLedgers(),systems=modules.States.ToArray(),seed = settings.seed, ledger = JsonUtility.ToJson(Ledger.Snapshot()),
                grids = construction.Grids.ToArray(), plans = construction.Plans.ToArray(), mappedBlocks = construction.Mappings.ToArray(),
                excavations = Excavations.ToArray(), selectedTool = equipment == null ? 0 : Array.IndexOf(equipment.tools,equipment.Selected) };
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            string temporary = SavePath + ".tmp";
            File.WriteAllText(temporary,JsonUtility.ToJson(save,true));
            if (File.Exists(SavePath)) File.Replace(temporary,SavePath,SavePath+".bak"); else File.Move(temporary,SavePath);
        }
        public void Fail(string reason)
        {
            Faulted = true;
            if (economy != null && economy.World != null) Ledger.ReadOnly = true;
            Tell("RESTAURATION INTERROMPUE • SAUVEGARDE CONSERVÉE");
            StartCoroutine(ShowFault());
            Debug.LogError("[VoidForge Survival] " + reason);
        }
        IEnumerator ShowFault()
        {
            for(int frame=0;frame<300;frame++)
            {
                var hud=FindFirstObjectByType<CyberdeckHud>();
                if(hud!=null&&hud.State!=null){hud.State.Notify("ERREUR DE RESTAURATION • SAUVEGARDE CONSERVÉE • CONSULTER LE JOURNAL");yield break;}
                yield return null;
            }
        }
        void OnApplicationPause(bool pause) { if (pause) Save(); }
        void OnApplicationQuit() { Save(); }
        void OnDestroy() { if (Active == this) Active = null; }
    }
}
#endif
