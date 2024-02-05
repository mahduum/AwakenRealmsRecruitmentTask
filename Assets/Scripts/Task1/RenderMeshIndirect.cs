using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Task1
{
    [ExecuteAlways]
    public class RenderMeshIndirect : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int LocalToWorld = Shader.PropertyToID("_LocalToWorld");
        
        [SerializeField] Material _material;
        [SerializeField] Mesh _mesh;
        [SerializeField] private float _sphereRadius = 10f;
        [SerializeField] private uint _instanceCount = 10;
        [SerializeField] private bool _withMovementEnabled = false;

        private GraphicsBuffer _transformsBuffer;
        GraphicsBuffer _commandBuffer;
        GraphicsBuffer.IndirectDrawIndexedArgs[] _indirectArgs;
        const int CommandsCount = 2;
        
        NativeArray<float4x4> _matrices;
        private Vector4[] _baseColors;
        private JobHandle _jobHandle;

        private MaterialPropertyBlock _propertyBlock;

        private bool _beganRender;
        private bool _buffersAndDataInitialized = false;

        private void Awake()
        {
            DisposeAll();
        }

        void Start()
        {
            InitializeBuffersAndData();
        }

        private void OnValidate()
        {
            DisposeAll();
            InitializeBuffersAndData();
        }

        private void InitializeBuffersAndData()
        {
            if (_jobHandle.IsCompleted == false)
            {
                _jobHandle.Complete();
            }
            
            InitializeBuffers();
            InitializeData();
            _buffersAndDataInitialized = true;
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
                _baseColors[i] = new Vector4(Random.value, Random.value, Random.value, Random.Range(0.5f, 1f));
            }

            _transformsBuffer.SetData(_matrices);
        }

        private void InitializeBuffers()
        {
            _transformsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.LockBufferForWrite,
                CommandsCount * (int)_instanceCount, UnsafeUtility.SizeOf<float4x4>());
            _commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, CommandsCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            _indirectArgs = new GraphicsBuffer.IndirectDrawIndexedArgs[CommandsCount];
            _matrices = new NativeArray<float4x4>(CommandsCount * (int) _instanceCount, Allocator.Persistent);
            _baseColors = new Vector4[CommandsCount * (int) _instanceCount];
        }

        void OnApplicationQuit()
        {
            if (_beganRender)
            {
                _transformsBuffer.UnlockBufferAfterWrite<float4x4>((int)_instanceCount * CommandsCount);
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
            _baseColors = null;
            _buffersAndDataInitialized = false;
        }

        void Update()
        {
            if (_buffersAndDataInitialized == false)
            {
                InitializeBuffersAndData();
            }
            
            if (_withMovementEnabled && Application.isPlaying)
            {
                RenderIndirectMovingMeshes();
                return;
            }
            
            RenderMeshesIndirect();
        }

        private void RenderIndirectMovingMeshes()
        {
            _jobHandle.Complete();

            if (_beganRender)
            {
                _transformsBuffer.UnlockBufferAfterWrite<float4x4>((int)_instanceCount * CommandsCount);
                RenderMeshesIndirect();
                _beganRender = false;
            }
            
            if (_beganRender == false)
            {
                _beganRender = true;
                
                var content = _transformsBuffer.LockBufferForWrite<float4x4>(0, (int)_instanceCount * CommandsCount);
                
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

        private void RenderMeshesIndirect()
        {
            _transformsBuffer.SetData(_matrices);
            RenderParams rp = new RenderParams(_material);
            rp.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one); // bounds that will be used for culling
            rp.matProps ??= _propertyBlock ?? new MaterialPropertyBlock();
            rp.matProps.SetBuffer(LocalToWorld, _transformsBuffer);
            rp.matProps.SetVectorArray(BaseColorId, _baseColors);
            _indirectArgs[0].indexCountPerInstance = _mesh.GetIndexCount(0);
            _indirectArgs[0].instanceCount = _instanceCount;
            _indirectArgs[1].indexCountPerInstance = _mesh.GetIndexCount(0);
            _indirectArgs[1].instanceCount = _instanceCount;
            _commandBuffer.SetData(_indirectArgs);
            Graphics.RenderMeshIndirect(rp, _mesh, _commandBuffer, CommandsCount);
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