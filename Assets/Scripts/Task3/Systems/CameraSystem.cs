using Task3.AuthoringAndComponents;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Task3.Systems
{
    public partial class CameraSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<CameraWrapperComponent>();
            RequireForUpdate<PlayerComponent>();
        }

        protected override void OnUpdate()
        {
            var cameraWrapper = EntityManager.GetComponentData<CameraWrapperComponent>(SystemHandle);
            var camera = cameraWrapper.Camera;
            var player = SystemAPI.GetSingletonEntity<PlayerComponent>();
            var playerTransform = SystemAPI.GetComponentRO<LocalTransform>(player);

            var offsetToPlayer = new float3 (0, 0, -30f);
            camera.transform.position = playerTransform.ValueRO.Position + offsetToPlayer;
        }
    }
}