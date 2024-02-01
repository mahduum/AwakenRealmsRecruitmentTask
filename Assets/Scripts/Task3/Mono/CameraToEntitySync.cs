using System;
using Task3.AuthoringAndComponents;
using Task3.Systems;
using UnityEngine;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Mathematics;

namespace Task3.Mono
{
    public class CameraToEntitySync : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private bool _syncEntityOnChangedRotation = true;

        private CameraSystem _cameraSystem;
        private EntityManager _entityManager;

        private Quaternion _previousRotation = Quaternion.identity;

        private bool _velocityChanged;
        private bool _rotationChanged;
        
        private void Awake()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _cameraSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CameraSystem>();//use system to set entity directly
            _entityManager.AddComponentData(_cameraSystem.SystemHandle, new CameraWrapperComponent() {Camera = _camera});
        }

        private void Start()
        {
            _cameraSystem.Enabled = true;
            _cameraSystem.Update();
        }

        private void LateUpdate()
        {
            _velocityChanged = _camera.velocity != Vector3.zero;
            _rotationChanged = !_camera.transform.rotation.Equals(_previousRotation) && _syncEntityOnChangedRotation;

            if (_rotationChanged)
            {
                Quaternion rotation = _camera.transform.rotation;
                _previousRotation = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
            }

            if (_velocityChanged || _rotationChanged)
            {
                _cameraSystem.Enabled = true;
                _cameraSystem.Update();

                //Debug.Log("Camera system enabled.");

                return;
            }

            _cameraSystem.Update();
            //Debug.Log("Camera system disabled.");
            _cameraSystem.Enabled = false;
        }
    }
}