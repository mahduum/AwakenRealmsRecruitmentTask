using Task3.AuthoringAndComponents;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Task3.Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct RangeDistanceCollectorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            //state.Enabled = false;
            state.RequireForUpdate<CameraInfoComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //todo do it in a job or in a multihash
            //var cameraPosition = SystemAPI.GetSingleton<CameraInfoComponent>().Position;
            var camEntity = SystemAPI.QueryBuilder().WithAllRW<CameraInfoComponent>().Build()
                .ToEntityArray(Allocator.Temp)[0];//todo why????????
            var cameraPosition = state.EntityManager.GetComponentData<CameraInfoComponent>(camEntity).Position;
            foreach (var (distanceToViewer, localTransform)
                     in SystemAPI.Query<RefRW<RangeReferenceDistanceSqComponent>, RefRO<LocalTransform>>())
            {
                distanceToViewer.ValueRW.Value = math.distancesq(cameraPosition, localTransform.ValueRO.Position);
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}