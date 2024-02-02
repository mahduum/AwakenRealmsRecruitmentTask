using Task3.Aspects;
using Task3.AuthoringAndComponents;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Task3.Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct RangeDistanceCollectorSystem : ISystem
    {
        [ReadOnly]
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<RangeReferenceDistanceSqComponent> _distanceSqLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerComponent>();
            //state.Enabled = false;
            _transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
            _distanceSqLookup = SystemAPI.GetComponentLookup<RangeReferenceDistanceSqComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _transformLookup.Update(ref state);
            _distanceSqLookup.Update(ref state);
            var player = SystemAPI.GetSingletonEntity<PlayerComponent>();
            var playerPosition = SystemAPI.GetComponentRO<LocalTransform>(player).ValueRO.Position;
            
            var distanceCollectionQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform, RangeSettingsShared>()
                .WithAllRW<RangeReferenceDistanceSqComponent>().Build();

            var distanceCollectionJob = new DistanceToReferencePointCollectionJob()
            {
                PlayerPosition = playerPosition
            };

            state.Dependency = distanceCollectionJob.ScheduleParallel(distanceCollectionQuery, state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    public partial struct DistanceToReferencePointCollectionJob : IJobEntity
    {
        public float3 PlayerPosition;
        
        private void Execute(ref RangeReferenceDistanceSqComponent distanceToViewerSq,
            in LocalTransform localTransform)
        {
            //todo add more data to distance sq, for example delta distSq, include heading, predict distance, exclude entities that just changed zone
            distanceToViewerSq.Value = math.distancesq(PlayerPosition, localTransform.Position);
        }
    }
    
    [BurstCompile]
    public partial struct DistanceToReferencePointCollectionJobParallel : IJobParallelFor
    {
        [NativeDisableContainerSafetyRestriction, NativeDisableParallelForRestriction]
        public ComponentLookup<RangeReferenceDistanceSqComponent> DistanceSqLookup;
        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup;
        public NativeArray<Entity> Entities;
        public float3 CameraPosition;
        public void Execute(int index)
        {
            var entity = Entities[index];
            DistanceSqLookup.GetRefRW(entity).ValueRW.Value = math.distancesq(CameraPosition, TransformLookup[entity].Position);
        }
    }
    
    [BurstCompile]
    partial struct HashGridForRangersJob : IJobEntity
    {
        [ReadOnly] public NativeArray<int> ChunkBaseEntityIndices;
        public NativeParallelMultiHashMap<int3, int>.ParallelWriter ParallelHashMap;
        public float InverseCellRadius;
        void Execute([ChunkIndexInQuery] int chunkIndexInQuery, [EntityIndexInChunk] int entityIndexInChunk, in LocalTransform localTransform)
        {
            int entityIndexInQuery = ChunkBaseEntityIndices[chunkIndexInQuery] + entityIndexInChunk;
            var hash = (new int3(math.floor(localTransform.Position * InverseCellRadius)));//hash map usage, each hash is mapped to rounded location of an entity, here InverseBoidCellRadius is the factor that will group differently boids with different inverse radius value
            ParallelHashMap.Add(hash, entityIndexInQuery);
            
            //inverse cell center will be hash * radius + half radius and calculate the distance from that point and check if it is close enough to the range barrier
            //
        }
    }

    [BurstCompile]
    partial struct CollectDistancesFromHashGridJob : IJobParallelFor
    {
        [NativeDisableContainerSafetyRestriction, NativeDisableParallelForRestriction]
        public ComponentLookup<RangeReferenceDistanceSqComponent> DistanceSqLookup;
        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup;
        
        [ReadOnly]
        public NativeArray<Entity> Entities;
        
        [ReadOnly]
        public NativeArray<int3> HashMapKeysArray;
        
        [ReadOnly]
        public NativeParallelMultiHashMap<int3, int> MultiHashMap;
        public float3 CameraPosition;
        public RangeSettingsShared RangeSettingsShared;
        public int CellRadius;
        public void Execute(int index)
        {
            var key = HashMapKeysArray[index];
            var cellCenter = (float3)(key * CellRadius) + CellRadius / 2.0f;//todo must be squared
            var distanceSq = math.distancesq(CameraPosition, cellCenter);

            ref var ranges = ref RangeSettingsShared.Ranges.Value.Values;

            var cellRadiusSq = math.pow(CellRadius, 2);
            var cellIntersectsRangeLimit = false;
            for (int i = 0; i < ranges.Length; i++)
            {
                var current = ranges[i];
                cellIntersectsRangeLimit =
                    math.abs(math.pow(current.Min, 2) - distanceSq) < cellRadiusSq ||
                    math.abs(math.pow(current.Max, 2) - distanceSq) < cellRadiusSq;
                if (cellIntersectsRangeLimit)
                {
                    break;
                }
            }

            if (cellIntersectsRangeLimit && MultiHashMap.TryGetFirstValue(key, out int entityInQuery, out var it))
            {
                do
                {
                    var entity = Entities[entityInQuery];
                    DistanceSqLookup.GetRefRW(entity).ValueRW.Value =
                        math.distancesq(TransformLookup[entity].Position, CameraPosition);
                }
                while (MultiHashMap.TryGetNextValue(out entityInQuery, ref it));
            }
        }
    }
}