using Task3.Aspects;
using Task3.AuthoringAndComponents;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Task3.Systems
{
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct RangeObjectsSpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            ComponentLookup<LocalToWorld> localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>();
            ComponentLookup<HeadingComponent> headingComponentLookup = SystemAPI.GetComponentLookup<HeadingComponent>();
            
            var world = state.World.Unmanaged;

            var endSim = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var endSimBuff = endSim.CreateCommandBuffer(world);
            var endSimBuffParallel = endSimBuff.AsParallelWriter();
            
            foreach (var (rangeObjTypeGroup, rangeObjTypeGroupLocalToWorld, entity) in
                     SystemAPI.Query<RefRO<RangeObjectTypeGroup>, RefRO<LocalToWorld>>()
                         .WithEntityAccess())
            {
                var rangeObjEntities =
                    CollectionHelper.CreateNativeArray<Entity, RewindableAllocator>(rangeObjTypeGroup.ValueRO.Count,
                        ref world.UpdateAllocator);
                
                state.EntityManager.Instantiate(rangeObjTypeGroup.ValueRO.Prefab, rangeObjEntities);
                endSimBuff.SetSharedComponent(rangeObjEntities, new TranslationBoundsSharedComponent()
                {
                    BoundsExtents = rangeObjTypeGroup.ValueRO.Extents,
                    Origin = rangeObjTypeGroupLocalToWorld.ValueRO.Position
                });

                Debug.Log($"Spawning num entities: {rangeObjTypeGroup.ValueRO.Count} with entity {entity.Index}");
                var setRandomObjectLocalToWorldJob = new SetRangeObjectLocalToWorldJob()
                {
                    LocalToWorldFromEntity = localToWorldLookup,
                    Entities = rangeObjEntities,
                    Center = rangeObjTypeGroupLocalToWorld.ValueRO.Position,
                    Radius = rangeObjTypeGroup.ValueRO.PrefabRadius,
                    Extents = rangeObjTypeGroup.ValueRO.Extents,
                    HeadingFromEntity = headingComponentLookup,
                    Ecb = endSimBuffParallel
                };
                
                state.Dependency = setRandomObjectLocalToWorldJob.Schedule(rangeObjTypeGroup.ValueRO.Count, 64, state.Dependency);
                state.Dependency.Complete();
                
                endSimBuff.DestroyEntity(entity);
            }
            //state.Dependency.Complete();//todo delete
        }
    }

    [BurstCompile]
    struct SetRangeObjectLocalToWorldJob : IJobParallelFor
    {
        [NativeDisableContainerSafetyRestriction] [NativeDisableParallelForRestriction]
        public ComponentLookup<LocalToWorld> LocalToWorldFromEntity;
        [NativeDisableContainerSafetyRestriction] [NativeDisableParallelForRestriction]
        public ComponentLookup<HeadingComponent> HeadingFromEntity;

        public NativeArray<Entity> Entities;
        public float3 Center;
        public float3 Extents;
        public float Radius;

        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute(int i)
        {
            var entity = Entities[i];
            var random = new Random(((uint) (entity.Index + i + 1) * 0x9F6ABC1));
            var dir = random.NextFloat3Direction();

            float3 pos = new float3(
                random.NextFloat(Center.x - Extents.x + Radius, Center.x + Extents.x - Radius),
                random.NextFloat(Center.y - Extents.y + Radius, Center.y + Extents.y - Radius),
                random.NextFloat(Center.z - Extents.z + Radius, Center.z + Extents.z - Radius));

            var localToWorld = new LocalToWorld
            {
                Value = float4x4.TRS(pos, quaternion.LookRotationSafe(dir, math.up()), new float3(1.0f, 1.0f, 1.0f))
            };

            LocalToWorldFromEntity[entity] = localToWorld;
            HeadingFromEntity[entity] = new HeadingComponent() {Value = dir};
            Ecb.RemoveComponent<IsUninitializedTag>(i, entity);//todo not needed
        }
    }
}