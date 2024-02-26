using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Task1
{
    public class RenderMeshIndirect : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int LocalToWorld = Shader.PropertyToID("_LocalToWorld");

        [SerializeField] Mesh _mesh;
        [Tooltip("Fallback material")]
        [SerializeField] Material _material;
        [SerializeField] List<Material> _materials;
        [SerializeField] private float _sphereRadius = 10f;
        [SerializeField] private uint _instanceCount = 10;
        [SerializeField] private bool _withMovementEnabled = false;

        private GraphicsBuffer _transformsBuffer;
        GraphicsBuffer _commandBuffer;
        GraphicsBuffer.IndirectDrawIndexedArgs[] _indirectArgs;
        const int CommandsCount = 2;
        
        NativeArray<float4x4> _matrices;
        private JobHandle _jobHandle;

        private MaterialPropertyBlock _propertyBlock;

        private bool _beganRender;
        private bool _buffersAndDataInitialized = false;

        void Start()
        {
            InitializeBuffersAndData();
        }

        private void InitializeBuffersAndData()
        {
            if (IsDataAvailable() == false)
            {
                Debug.LogWarning("Missing assets!");
                return;
            }
            
            InitializeBuffers();
            InitializeData();
        }

        private void InitializeData()
        {
            for (int i = 0; i < _matrices.Length; i++)
            {
                _matrices[i] = float4x4.TRS(Random.insideUnitSphere * _sphereRadius + transform.position,
                    Quaternion.Euler(
                        Random.value * 360f, Random.value * 360f, Random.value * 360f
                    ),
                    Vector3.one * Random.Range(0.5f, 1.5f));
            }
            
            _transformsBuffer.SetData(_matrices);
            _buffersAndDataInitialized = true;
        }

        private bool IsDataAvailable()
        {
            if (_mesh == null || _material == null)
            {
                return false;
            }
            
            return true;
        }

        private void InitializeBuffers()
        {
            _transformsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.LockBufferForWrite,
                (int)_instanceCount, UnsafeUtility.SizeOf<float4x4>());
            _commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, CommandsCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            _indirectArgs = new GraphicsBuffer.IndirectDrawIndexedArgs[CommandsCount];
            _matrices = new NativeArray<float4x4>((int) _instanceCount, Allocator.Persistent);
        }

        void OnDestroy()
        {
            if (_beganRender)
            {
                _transformsBuffer.UnlockBufferAfterWrite<float4x4>((int)_instanceCount);
                _beganRender = false;
            }
            DisposeAll();
        }

        private void DisposeAll()
        {
            _jobHandle.Complete();
            _commandBuffer?.Release();
            _commandBuffer?.Dispose();
            _transformsBuffer?.Release();
            _transformsBuffer?.Dispose();
            _matrices.Dispose();
            _buffersAndDataInitialized = false;
        }

        void Update()
        {
            if (_buffersAndDataInitialized == false)
            {
                Debug.LogError("Data not initialized!");
                return;
            }
            
            if (_withMovementEnabled)
            {
                RenderIndirectMovingMeshesStart();
                return;
            }
            
            RenderMeshesIndirect();
        }

        private void LateUpdate()
        {
            if (_withMovementEnabled)
            {
                RenderIndirectMovingMeshesEnd();
            }
        }

        private void RenderIndirectMovingMeshesStart()
        {
            if (_beganRender == false)
            {
                _beganRender = true;
                
                var content = _transformsBuffer.LockBufferForWrite<float4x4>(0, (int)_matrices.Length);
                
                var rotateJob = new RotateJobParallel()
                {
                    Matrices = _matrices,
                    DeltaTime = Time.deltaTime,
                    Center = transform.position,
                    Up = transform.up
                }.Schedule(_matrices.Length, 64);

                var writeToBuffer = new WriteToBufferJob
                {
                    DstMatrices = content,
                    SrcMatrices = _matrices
                }.Schedule(rotateJob);

                _jobHandle = writeToBuffer;
            }
        }

        private void RenderIndirectMovingMeshesEnd()
        {
            _jobHandle.Complete();
            
            if (_beganRender)
            {
                _transformsBuffer.UnlockBufferAfterWrite<float4x4>((int)_matrices.Length);
                RenderMeshesIndirect();
                _beganRender = false;
            }
        }

        private void RenderMeshesIndirect()
        {
            RenderParams[] rp = new RenderParams[2];
            for (int i = 0; i < _mesh.subMeshCount; i++)
            {
                var material = i < _materials.Count ? _materials[i] : _material;
                rp[i] = new RenderParams(material)
                {
                    worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one) // bounds that will be used for culling
                };
                
                rp[i].matProps ??= _propertyBlock ?? new MaterialPropertyBlock();
                rp[i].matProps.SetBuffer(LocalToWorld, _transformsBuffer);
                
                _indirectArgs[i].indexCountPerInstance = _mesh.GetIndexCount(i);
                _indirectArgs[i].instanceCount = _instanceCount;
                _indirectArgs[i].baseVertexIndex = _mesh.GetBaseVertex(i);
                _indirectArgs[i].startIndex = _mesh.GetIndexStart(i);
            }
            
            _commandBuffer.SetData(_indirectArgs);

            for (int i = 0; i < _mesh.subMeshCount; i++)
            {
                Graphics.RenderMeshIndirect(rp[i], _mesh, _commandBuffer, CommandsCount, i);
            }
        }
    }

    [BurstCompile]
    public struct RotateJobParallel : IJobParallelFor
    {
        [NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction]
        public NativeArray<float4x4> Matrices;
        public float DeltaTime;
        public float3 Center;
        public float3 Up;
        
        [BurstCompile]
        public void Execute(int i)
        {
            var mat = Matrices[i];
            var c3 = mat.c3;

            var position = new float3(c3.x, c3.y, c3.z);
            var rotation = quaternion.AxisAngle(Up, DeltaTime * math.distancesq(Center, position) * 1/Matrices.Length);
            float3 rotated = math.mul(rotation, position);

            Matrices[i] = new float4x4(mat.c0, mat.c1, mat.c2, new float4(rotated.x, rotated.y, rotated.z, 1f));
        }
    }

    public struct WriteToBufferJob : IJob
    {
        public NativeArray<float4x4> SrcMatrices;
        public NativeArray<float4x4> DstMatrices;

        public void Execute()
        {
            SrcMatrices.CopyTo(DstMatrices);
        }
    }
}