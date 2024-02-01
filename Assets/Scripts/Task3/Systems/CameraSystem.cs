using Task3.AuthoringAndComponents;
using Unity.Entities;
using Unity.Entities.Content;
using UnityEngine;

namespace Task3.Systems
{
    [DisableAutoCreation]
    public partial class CameraSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<CameraWrapperComponent>();
        }

        protected override void OnUpdate()
        {
            var camera = EntityManager.GetComponentData<CameraWrapperComponent>(SystemHandle).Camera;//todo? system can unpack the values or it can receive ready values?

            //Debug.Log("Update camera.");
            foreach (var cameraInfo in SystemAPI.Query<RefRW<CameraInfoComponent>>())
            {
                //Debug.Log($"Update camera position: {cameraInfo.ValueRW.Position}.");

                cameraInfo.ValueRW.Position = camera.gameObject.transform.position;
                
            }
        }
    }
}