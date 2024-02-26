using System;
using NUnit.Compatibility;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Task2
{
    public class NativeStore
    {
        private NativeArray<Fixed16> _autoLayoutFixed16Array;
        private NativeArray<Fixed32> _autoLayoutFixed32Array;
        private NativeArray<Fixed64> _autoLayoutFixed64Array;
        
        private NativeHashMap<int, NativeReference<Fixed16>> _autoLayoutFixed16Map;
        
        public NativeStore(int maxSingletons)
        {
            _autoLayoutFixed16Array = new NativeArray<Fixed16>(maxSingletons, Allocator.Domain);
            _autoLayoutFixed16Map =
                new NativeHashMap<int, NativeReference<Fixed16>>(maxSingletons, Allocator.Domain);
        }

        public void AddToArray<T>(T data, int index) where T : unmanaged, IAutoLayoutFixedSingletonData
        {
            if (typeof(IAutoLayoutFixedSingletonData).IsAssignableFrom(typeof(T)) == false
                && typeof(Fixed16).IsAssignableFrom(typeof(T))
                && typeof(Fixed16).IsCastableFrom(typeof(T))) 
            {
                throw new InvalidCastException($"Cannot assign from {nameof(IAutoLayoutFixedSingletonData)}");
            }
            
            if (index >= _autoLayoutFixed16Array.Length)
            {
                throw new IndexOutOfRangeException();
            }

            var reinterpret = _autoLayoutFixed16Array.Reinterpret<Fixed16, T>();
            reinterpret[index] = data;
        }

        public void AddOrUpdateDirect<T>(T data) where T : unmanaged, IAutoLayoutFixedSingletonData
        {
            var type = typeof(T);
            var key = type.GetHashCode();
            
            //first check if it is already there

            // Type writeType = null;
            //
            // if (InSizeGreaterThan<Fixed16, T>() == false)
            // {
            //     writeType = typeof(Fixed16);
            // }
            // else if (InSizeGreaterThan<Fixed32, T>() == false)
            // {
            //     writeType = typeof(Fixed32);
            // }
            // else if (InSizeGreaterThan<Fixed64, T>() == false)
            // {
            //     writeType = typeof(Fixed64);
            // }
            // else
            // {
            //     throw new InvalidOperationException("Structs of size greater than 64 bytes are not supported!");
            // }
            
            

            // Fixed16 reinterpret = *((Fixed16*) &data);
            //
            // if (_autoLayoutFixed16Map.TryGetValue(key, out var reference))
            // {
            //     reference.Value = reinterpret;
            // }
            // else
            // {
            //     _autoLayoutFixed16Map.Add(key, new NativeReference<Fixed16>(reinterpret, Allocator.Domain));
            // }
        }
        
        public bool HasValueDirect<T>() where T : unmanaged, IAutoLayoutFixedSingletonData
        {
            return _autoLayoutFixed16Map.ContainsKey(typeof(T).GetHashCode());
        }
        
        public T GetValueDirect<T>() where T : unmanaged, IAutoLayoutFixedSingletonData
        {
            // if (_autoLayoutFixed16Map.TryGetValue(typeof(T).GetHashCode(), out NativeReference<Fixed16> reference))
            // {
            //     return *(T*)reference.GetUnsafePtr();
            // }

            return default;
        }

        public bool RemoveValueDirect<T>() where T : unmanaged, IAutoLayoutFixedSingletonData
        {
            return _autoLayoutFixed16Map.Remove(typeof(T).GetHashCode());
        }

        public T GetFromArray<T>(int index) where T : unmanaged, IAutoLayoutFixedSingletonData
        {
            if (index >= _autoLayoutFixed16Array.Length)
            {
                throw new IndexOutOfRangeException();
            }

            return _autoLayoutFixed16Array.ReinterpretLoad<T>(index);
        }

        ~NativeStore()
        {
            _autoLayoutFixed16Array.Dispose();
            _autoLayoutFixed16Map.Dispose();
        }

        static void CheckSize<T, U>() where T : unmanaged where U : unmanaged
        {
            var tSize = UnsafeUtility.SizeOf<T>();
            var uSize = UnsafeUtility.SizeOf<U>();

            if (tSize != uSize)
            {
                throw new InvalidOperationException("Structs must be the same size for valid data reinterpretation!");
            }
        }
        
        static bool InSizeGreaterThan<T, U>() where T : unmanaged where U : unmanaged
        {
            var tSize = UnsafeUtility.SizeOf<T>();
            var uSize = UnsafeUtility.SizeOf<U>();

            return uSize > tSize;
        }
    }
}