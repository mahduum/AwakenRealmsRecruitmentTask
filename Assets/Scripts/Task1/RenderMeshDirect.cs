using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Task1
{
    [ExecuteAlways]
    public class RenderMeshDirect : MonoBehaviour
    {
        [SerializeField] private Mesh _mesh = default;
        [Tooltip("Fallback material")]
        [SerializeField] private Material _material = default;
        [SerializeField] private List<Material> _materials = default;
        [SerializeField] private int _instancesCount = 1023;
        [SerializeField] private float _sphereRadius = 10f;

        private Matrix4x4[] _matrices;

        private MaterialPropertyBlock _propertyBlock;

        private bool _dataInitialized;
        
        private void Awake()
        {
            InitializeData();
        }

        private void OnValidate()
        {
            InitializeData(true);
        }

        private bool InitializeData(bool forceInitialize = false)
        {
            if (_dataInitialized && forceInitialize == false)
            {
                return _dataInitialized;
            }

            if (_mesh == null || _material == null)
            {
                return _dataInitialized = false;
            }
            
            _matrices = new Matrix4x4[_instancesCount];

            for (int i = 0; i < _matrices.Length; i++)
            {
                _matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * _sphereRadius + transform.position,
                    Quaternion.Euler(
                        Random.value * 360f, Random.value * 360f, Random.value * 360f
                    ),
                    Vector3.one * Random.Range(0.5f, 1.5f));
            }

            return _dataInitialized = true;
        }

        private void Update()
        {
            if (InitializeData() == false)
            {
                return;
            }
            
            DrawInstanced();
        }

        private void DrawInstanced()
        {
            for (int i = 0; i < _mesh.subMeshCount; i++)
            {
                var material = i < _materials.Count ? _materials[i] : _material;
                var rp = new RenderParams(material);
                Graphics.RenderMeshInstanced(rp, _mesh, i, _matrices, _instancesCount);
            }
            
        }
    }
}