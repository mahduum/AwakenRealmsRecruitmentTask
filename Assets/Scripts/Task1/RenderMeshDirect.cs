using UnityEngine;
using Random = UnityEngine.Random;

namespace Task1
{
    /*
     * The most straightforward solution for when we need to render a fixed number of static meshes is by using method `Graphics.RenderMeshInstanced` which replaced old method `Graphics.DrawMeshInstanced`,
     * Advantages:
     * - Easy interface that requires very little setup :)
     * - Up to 1023 meshes can be rendered in a single draw call
     * - Light probes can be set automatically
     * - Automatic sorting rendered meshes
     * - Automatic culling of rendered meshes
     * 
     * Limitations:
     * - We provide up to 1023 instances in a single batch so a single draw call can also encompass maximally 1023 elements
     * - Draw calls are grouped by materials and to take advantage of instancing meshes must share materials (however we can override material properties per instance)
     * - Unity treats meshes from a single batch as a single entity, and it creates one single bounds for all rendered meshes; what follows is that automatically
     *      provided culling will affect all meshes from batch as a whole and not individually, therefore only when all mesh instances from `RenderMeshInstanced` call
     *      are outside of the camera view frustum will culling on them take effect. Otherwise even it we can see just a part rendered instances the part of them that is
     *      already outside of the camera frustum is still being rendered.
     * - We must provide all arguments directly and that includes transformation matrices for each mesh. That means that if we want objects to change positions we must handle
     *      all changes to their transforms on the CPU and provide them directly inside method invocation each update (or whenever we want their positions to change). This can
     *      somehow optimized by using multi threaded parallel computing with DOTS Jobs but nonetheless would require a sync point each frame (if we want to update each frame).
     *
     * Alternative:
     *      As an alternative `Graphics.RenderMeshIndirect` can be used instead, where we can issue rendering command arguments indirectly via structured buffer that will be executed
     *      with a single call to this method. Commands can by issued from CPU or GPU (for example from a compute shader). Additionally we can use more structured buffers to send
     *      additional data to the shader per instance transform data and this can also be done from CPU or GPU (where a compute shader is responsible for transforming and operating
     *      on data and calculate transformations. The data can also be set on the CPU if we need to (with `buffer.SetData(...)) or written to dynamically each frame with methods
     *      `buffer.LockBufferForWrite<T>(startIndex, count)` and `buffer.UnlockBufferAfterWrite<T>(writtenCount)` and this way we have greater flexibility on the number of meshes we
     *      want to update and on the number and which meshes we want to render current frame (although the buffer length must be fixed size, but we don't need to use all of it each frame).
     *      What follows is that with indirect rendering method we are no longer limited by 1023 instances to draw in a single call but we can provide arbitrary number of elements within
     *      our buffer capacity to be rendered each frame.
     *      The older version of this method: `Graphics.DrawMeshInstancedIndirect`, which is now obsolete had much more drawbacks than the new one, it didn't provide automatic culling nor sorting
     *      for rendered elements and this part had to be taken care of manually via additional data in passed via structured buffers. The current actual method that is discussed here,
     *      does provide sorting and culling withing the render bounds of a passed in `RenderParams.worldBounds` parameter. However, if we want finer culling, for example a one that acts
     *      on individual meshes and not on the whole rendered entity composed of multiple instanced meshes we still must provide additional code for that, for example calculating in a Job which
     *      instances are outside the camera view frustum and should not be included in indirect arguments that is passed to the `RenderMeshIndirect` method.
     *      
     * Requirements for GPU instancing:
     * - Works only for objects that share the same material
     * - Material must have GPU option available and enabled (we can provide it for our own shaders or modifying the existent shaders by including option
     *      `#pragma multi_compile_instancing` and provide additional macros for shaders that will provide necessary data for instancing (if they are not included already),
     *      like vertex data per instance `UNITY_VERTEX_INPUT_INSTANCE_ID` and appropriate space conversion matrices that are set by the GPU once per draw inside the uniforms like `unity_ObjectToWorld`, `unity_WorldToObject` etc.
     *      We can enable overriding material properties for instancing by using class `MaterialPropertyBlock` in C# end and adding buffers for instancing inside given shader
     *      `UNITY_INSTANCING_BUFFER_START/_END(UnityPerMaterial)` and each property we need to override: `UNITY_DEFINE_INSTANCED_PROP(<someType>, <_SomeProperty>)`
     * - Current platform must support instancing
     */
    [ExecuteAlways]
    public class RenderMeshDirect : MonoBehaviour
    {
        [SerializeField] private Mesh _mesh = default;
        [SerializeField] private Material _material = default;
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
            InitializeData();
        }

        private void InitializeData()
        {
            _matrices = new Matrix4x4[_instancesCount];

            for (int i = 0; i < _matrices.Length; i++)
            {
                _matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * _sphereRadius + transform.position,
                    Quaternion.Euler(
                        Random.value * 360f, Random.value * 360f, Random.value * 360f
                    ),
                    Vector3.one * Random.Range(0.5f, 1.5f));
            }

            _dataInitialized = true;
        }

        private void Update()
        {
            if (_dataInitialized == false)
            {
                InitializeData();
            }
            
            DrawInstanced();
        }

        private void DrawInstanced()
        {
            var rp = new RenderParams(_material);
            Graphics.RenderMeshInstanced(rp, _mesh, 0, _matrices, _instancesCount);
        }
    }
}