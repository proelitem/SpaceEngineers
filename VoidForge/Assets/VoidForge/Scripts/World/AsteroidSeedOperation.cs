#if VF_LOCAL_DIGGER
using System.Collections.Generic;
using Digger.Modules.Core.Sources;
using Digger.Modules.Core.Sources.Jobs;
using Digger.Modules.Core.Sources.Operations;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace VoidForge.World
{
    // Native Digger operation: generate only the asteroid's chunks, without extending an
    // Add brush down to the heightmap. The heightmap itself is never rendered/collidable.
    public sealed class AsteroidSeedOperation : IOperation<AsteroidSeedJob>
    {
        public Vector3 centre;
        public Quaternion rotation;
        public float diameter;
        public AsteroidVolumeData data;
        public int variant;
        public AsteroidOreProfile.Vein[] veins;

        public ModificationArea GetAreaToModify(DiggerSystem digger)
        {
            Vector3 p = centre - digger.Terrain.transform.position;
            Vector3 h = digger.HeightmapScale;
            float radius = diameter * .9f + Mathf.Max(h.x,h.y,h.z) * 2;
            int chunk = digger.SizeOfMesh;
            return new ModificationArea { NeedsModification = true,
                Min = new Vector3i(Mathf.Max(0,Mathf.FloorToInt((p.x-radius)/h.x/chunk)),Mathf.FloorToInt((p.y-radius)/h.y/chunk),Mathf.Max(0,Mathf.FloorToInt((p.z-radius)/h.z/chunk))),
                Max = new Vector3i(Mathf.Min(digger.TerrainChunkWidth,Mathf.FloorToInt((p.x+radius)/h.x/chunk)),Mathf.FloorToInt((p.y+radius)/h.y/chunk),Mathf.Min(digger.TerrainChunkHeight,Mathf.FloorToInt((p.z+radius)/h.z/chunk))) };
        }
        public AsteroidSeedJob Do(VoxelChunk chunk)
        {
            // Do runs on the caller/main thread, before Digger switches to its worker phase.
            // Prewarm every affected XZ hole entry to keep Terrain.GetHoles off workers.
            chunk.Cutter.GetHoles(chunk.ChunkPosition,chunk.VoxelPosition);
            return new AsteroidSeedJob { voxels = new NativeArray<Voxel>(chunk.VoxelArray,Allocator.Persistent),
                sdf = new NativeArray<float>(data.variants[variant].signedDistance,Allocator.Persistent),
                veins = new NativeArray<AsteroidOreProfile.Vein>(veins,Allocator.Persistent),
                origin = chunk.AbsoluteWorldPosition, scale = chunk.HeightmapScale, centre = centre,
                inverseRotation = math.inverse(new quaternion(rotation.x,rotation.y,rotation.z,rotation.w)),
                diameter = diameter, count = chunk.SizeVox, resolution = data.resolution, sampleStep = data.sampleStep };
        }
        public ModificationResult Complete(AsteroidSeedJob job,VoxelChunk chunk)
        {
            job.voxels.CopyTo(chunk.VoxelArray);
            job.voxels.Dispose();job.sdf.Dispose();job.veins.Dispose();
            return ModificationResult.Empty;
        }
    }

    [BurstCompile]
    public struct AsteroidSeedJob : IJobParallelFor
    {
        public NativeArray<Voxel> voxels;
        [ReadOnly] public NativeArray<float> sdf;
        [ReadOnly] public NativeArray<AsteroidOreProfile.Vein> veins;
        public float3 origin,scale,centre;
        public quaternion inverseRotation;
        public float diameter,sampleStep;
        public int count,resolution;

        public void Execute(int index)
        {
            int3 pi = new int3(index/(count*count),(index/count)%count,index%count);
            float3 local = math.mul(inverseRotation,origin+(float3)pi*scale-centre)/diameter;
            float3 g = local/sampleStep + (resolution-1)*.5f;
            int3 low = (int3)math.floor(g);
            if (math.any(low < 0) || math.any(low >= resolution-1)) return;
            float3 t = g-low;
            float distance = 0;
            for(int x=0;x<2;x++)for(int y=0;y<2;y++)for(int z=0;z<2;z++)
                distance += sdf[((low.x+x)*resolution+low.y+y)*resolution+low.z+z] *
                    (x==0?1-t.x:t.x)*(y==0?1-t.y:t.y)*(z==0?1-t.z:t.z);
            distance *= diameter;
            if (distance > scale.y*2) return;
            var voxel = voxels[index];
            // Union with existing solids; never overwrite an already-seeded neighbour.
            if (distance >= voxel.Value) return;
            voxel.SetValue(distance,scale.y);
            voxel.Alteration = Voxel.FarAboveSurface;
            uint material = 0;
            for(int i=0;i<veins.Length;i++)
            {
                var vein=veins[i];float3 axis=(float3)vein.b-(float3)vein.a;
                float along=math.saturate(math.dot(local-(float3)vein.a,axis)/math.lengthsq(axis));
                if(math.lengthsq(local-(float3)vein.a-axis*along)<=vein.radius*vein.radius){material=(uint)vein.material;break;}
            }
            voxel.FirstTextureIndex=voxel.SecondTextureIndex=material;
            voxel.NormalizedTextureLerp=0;
            voxels[index]=voxel;
        }
    }

    // Delegate the cutting math to Digger. Read the very same per-voxel material that
    // Digger renders, and count only the unique interior samples owned by each chunk.
    public sealed class OreDigOperation : IOperation<VoxelModificationJob>
    {
        public readonly BasicOperation operation = new BasicOperation();
        public readonly double[] removedByMaterial = new double[11];
        public ModificationArea GetAreaToModify(DiggerSystem digger) => new AsteroidSeedOperation
        { centre=operation.Params.Position,diameter=math.cmax(operation.Params.Size)*1.3f }.GetAreaToModify(digger);
        public VoxelModificationJob Do(VoxelChunk chunk) => operation.Do(chunk);
        public ModificationResult Complete(VoxelModificationJob job,VoxelChunk chunk)
        {
            int n=chunk.SizeVox;
            for(int x=1;x<n-1;x++)for(int y=1;y<n-1;y++)for(int z=1;z<n-1;z++)
            {
                int index=(x*n+y)*n+z;
                Voxel before=chunk.VoxelArray[index];float after=job.Voxels[index].Value;
                if(before.Value<0&&after>before.Value&&before.FirstTextureIndex<removedByMaterial.Length)
                    removedByMaterial[before.FirstTextureIndex] += after-before.Value;
            }
            return operation.Complete(job,chunk);
        }
    }
}
#endif
