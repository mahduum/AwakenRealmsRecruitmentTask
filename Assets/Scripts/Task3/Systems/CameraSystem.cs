using Task3.AuthoringAndComponents;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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
            //todo if player transform changed, with tag need update when attached when player delta movement was != 0
            
            var cameraWrapper = EntityManager.GetComponentData<CameraWrapperComponent>(SystemHandle);//todo? system can unpack the values or it can receive ready values?
            var camera = cameraWrapper.Camera;
            //update position on component wrapper by player entity:
            var player = SystemAPI.GetSingletonEntity<PlayerComponent>();
            var playerTransform = SystemAPI.GetComponentRO<LocalTransform>(player);

            camera.transform.position = playerTransform.ValueRO.Position + new float3 (0, 0, -30f);
        }

        protected override void OnDestroy()
        {
            //release wrapper
            base.OnDestroy();
        }
    }
}