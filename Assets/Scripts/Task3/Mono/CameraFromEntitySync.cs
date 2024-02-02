using System;
using Task3.AuthoringAndComponents;
using Task3.Systems;
using UnityEngine;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Mathematics;

namespace Task3.Mono
{
    public class CameraFromEntitySync : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        private CameraSystem _cameraSystem;
        private EntityManager _entityManager;

        private bool _velocityChanged;
        private bool _rotationChanged;
        
        private void Awake()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _cameraSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CameraSystem>();
            _entityManager.AddComponentData(_cameraSystem.SystemHandle, new CameraWrapperComponent()
            {
                Camera = _camera,
            });
        }
    }
}