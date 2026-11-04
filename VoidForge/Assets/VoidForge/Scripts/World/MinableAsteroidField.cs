#if VF_LOCAL_DIGGER && VF_LOCAL_ECONOMY
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Digger.Modules.Core.Sources;
using UnityEngine;
using VoidForge.Integration;

namespace VoidForge.World
{
    public sealed class MinableAsteroid : MonoBehaviour
    {
        public int index,variant;
        public float diameter;
        public bool voxelReady;
        public AsteroidOreProfile.Vein[] veins;
        public GameObject original;
    }

    public sealed class MinableAsteroidField : MonoBehaviour
    {
        public Terrain terrain;
        public DiggerSystem system;
        public AsteroidAssetSet assets;
        public AsteroidVolumeData volumes;
        public int count = 64,seed = 421103;
        public readonly List<MinableAsteroid> Asteroids = new List<MinableAsteroid>();
        public bool Busy { get; private set; }
        public bool Faulted { get; private set; }
        float nextAction;
        AsteroidSurfaceBinding surfaces;
        public AsteroidSurfaceBinding Surfaces=>surfaces;

        void Awake()
        {
            if(!Application.isPlaying)return;
            if (assets==null||!assets.IsUsable||volumes==null) { Fail("Voxel asteroid local dependencies missing.");return; }
            var random=new System.Random(seed);
            for(int i=0;i<count;i++)
            {
                float size=i==0?8:Range(random,4,12);
                Vector3 position=new Vector3(0,0,10);
                if(i>0)
                {
                    bool found=false;
                    for(int attempt=0;attempt<512;attempt++)
                    {
                        position=new Vector3(Range(random,-75,75),Range(random,-22,22),Range(random,-75,75));
                        if(position.magnitude<12||Vector3.Distance(position,new Vector3(-5,-2,0))<size*.5f+8||
                            Asteroids.Any(a=>Vector3.Distance(a.transform.position,position)<(a.diameter+size)*.6f+1))continue;
                        found=true;break;
                    }
                    if(!found)throw new InvalidOperationException("Cannot place non-overlapping asteroid " + i);
                }
                var root=new GameObject($"Asteroid_{i:00}");root.transform.SetParent(transform,false);
                root.transform.SetPositionAndRotation(position,Quaternion.Euler(Range(random,0,360),Range(random,0,360),Range(random,0,360)));
                var marker=root.AddComponent<MinableAsteroid>();marker.index=i;marker.variant=i%assets.variants.Length;marker.diameter=size;
                marker.veins=AsteroidOreProfile.Generate(seed,i);
                root.AddComponent<GpsTargetBounds>().size=assets.variants[marker.variant].lodMeshes[0].bounds.size*size;
                marker.original=new GameObject("Source asset • converts to Digger on approach");marker.original.transform.SetParent(root.transform,false);
                marker.original.transform.localScale=Vector3.one*size;assets.AddVisual(marker.original,marker.variant);
                Asteroids.Add(marker);
            }
            surfaces=gameObject.AddComponent<AsteroidSurfaceBinding>();surfaces.Initialize(this);
            HideTerrain();
            Debug.Log($"[VoidForge Survival] {count} asset-shaped asteroids registered; all convertible/minable, profile {AsteroidOreProfile.Version}.");
        }
        async void Start()
        {
            while(SurvivalSession.Active==null||!SurvivalSession.Active.Ready)
            {
                if(SurvivalSession.Active!=null&&SurvivalSession.Active.Faulted)return;
                await Task.Yield();
                if(this==null)return;
            }
            if(!Faulted) await Activate(0);
        }
        void Update()
        {
            var session=SurvivalSession.Active;
            if(Busy||Faulted||session==null||!session.CanAct||Camera.main==null)return;
            var nearest=Asteroids.Where(a=>!a.voxelReady).OrderBy(a=>Vector3.Distance(Camera.main.transform.position,a.transform.position)-a.diameter*.6f).FirstOrDefault();
            if(nearest!=null&&Vector3.Distance(Camera.main.transform.position,nearest.transform.position)-nearest.diameter*.6f<8)
            { _=Activate(nearest.index);return; }
            var rig=session.equipment;
            if(!Application.isFocused||rig==null||!rig.HasTrackedRightHand||Time.unscaledTime<nextAction)return;
            var tool=rig.Held;
            if(tool==null||tool.kind!=HandToolKind.MiningDrill||!tool.IsOperating||session.deck?.PointerBusy==true)return;
            if(Physics.Raycast(tool.tip.position,tool.tip.forward,out var hit,1,~0,QueryTriggerInteraction.Ignore)&&hit.collider.transform.IsChildOf(terrain.transform))
            {
                var asteroid=Asteroids.Where(a=>a.voxelReady).OrderBy(a=>Vector3.SqrMagnitude(a.transform.position-hit.point)).FirstOrDefault();
                if(asteroid!=null&&Vector3.Distance(asteroid.transform.position,hit.point)<asteroid.diameter*.9f)
                {nextAction=Time.unscaledTime+.3f;_=Dig(asteroid.index,hit.point-hit.normal*.15f,.75f);}
            }
            // Persistent overflow pickups reuse the economy ledger's finite-stock transaction.
            else if(Physics.Raycast(tool.tip.position,tool.tip.forward,out hit,1,~0,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<VoidForge.Economy.MineralOutcrop>() is var pickup&&pickup!=null)
            {
                nextAction=Time.unscaledTime+.3f;
                session.Ledger.Mine(pickup.id,8);pickup.Refresh();session.Sync();session.Save();
            }
        }
        public async Task Activate(int index)
        {
            if(Busy||Faulted||Asteroids[index].voxelReady)return;
            Busy=true;
            try
            {
                await Awaitable.MainThreadAsync();
                var asteroid=Asteroids[index];
                var operation=new AsteroidSeedOperation { centre=asteroid.transform.position,rotation=asteroid.transform.rotation,diameter=asteroid.diameter,
                    data=volumes,variant=asteroid.variant,veins=asteroid.veins };
                // Preparation/terrain-cache reads happen on main; Digger then schedules the
                // worker phase and native Jobs without blocking the whole render frame.
                await system.Modify(operation,true);
                foreach(var edit in SurvivalSession.Active.Excavations.Where(e=>e.asteroid==index)) await Cut(edit.point,edit.radius);
                await Awaitable.MainThreadAsync();
                if(this==null)return;
                asteroid.original.SetActive(false);
                asteroid.voxelReady=true;surfaces.RefreshChunks();HideTerrain();Physics.SyncTransforms();
                Debug.Log($"[VoidForge Survival] Asteroid {index} voxel collider ready; source variant {asteroid.variant}, veins {asteroid.veins.Length}.");
            }
            catch(Exception error){await Awaitable.MainThreadAsync();if(this!=null)Fail(error.ToString());}
            finally{Busy=false;}
        }
        async Task<OreDigOperation> Cut(Vector3 point,float radius)
        {
            var operation=new OreDigOperation();
            operation.operation.Params=new ModificationParameters { Position=point,Brush=BrushType.Sphere,Action=ActionType.Dig,Opacity=1,Size=Vector3.one*radius,TextureIndex=0,PaintWhileDigging=false };
            await system.Modify(operation,true);
            await Awaitable.MainThreadAsync();
            surfaces.RefreshChunks();
            return operation;
        }
        public async Task<bool> Dig(int asteroid,Vector3 point,float radius)
        {
            var session=SurvivalSession.Active;
            if(Busy||Faulted||session==null||!session.CanAct||asteroid<0||asteroid>=Asteroids.Count||!Asteroids[asteroid].voxelReady||session.Excavations.Count>=8192||
                !float.IsFinite(radius)||radius<=0||radius>2||!float.IsFinite(point.x)||!float.IsFinite(point.y)||!float.IsFinite(point.z)||
                Vector3.Distance(point,Asteroids[asteroid].transform.position)>Asteroids[asteroid].diameter*.9f+radius)return false;
            Busy=true;
            try
            {
                double start=Time.realtimeSinceStartupAsDouble;
                await Awaitable.MainThreadAsync();
                var operation=await Cut(point,radius);
                await Awaitable.MainThreadAsync();
                if(this==null||session==null)return false;
                HideTerrain();Physics.SyncTransforms();
                if(operation.removedByMaterial.Sum()<=0)return false;
                session.CreditExcavation(new AsteroidExcavation { uid=Guid.NewGuid().ToString("N"),asteroid=asteroid,point=point,radius=radius },operation.removedByMaterial);
                Debug.Log($"[VoidForge Survival] Voxel excavation credited once, {(Time.realtimeSinceStartupAsDouble-start)*1000:0} ms; material samples {string.Join(",",operation.removedByMaterial.Select(x=>x.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture)))}.");
                return true;
            }
            catch(Exception error){await Awaitable.MainThreadAsync();if(this!=null)Fail(error.ToString());return false;}
            finally{Busy=false;}
        }
        void HideTerrain(){terrain.drawHeightmap=false;terrain.GetComponent<TerrainCollider>().enabled=false;}
        void Fail(string error){Faulted=true;Debug.LogError("[VoidForge Survival] Asteroid conversion/mining failed: "+error);SurvivalSession.Active?.Fail(error);}
        static float Range(System.Random random,float a,float b)=>a+(float)random.NextDouble()*(b-a);
    }
}
#endif
