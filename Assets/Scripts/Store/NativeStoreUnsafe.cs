using System;
using System.Runtime.InteropServices;
using NUnit.Compatibility;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Store
{
    public interface IAutoLayoutFixedSingletonData
    {
    }
    
    [StructLayout(LayoutKind.Auto, Size = 4)]
    public struct Fixed4 : IAutoLayoutFixedSingletonData
    {
    }
    
    [StructLayout(LayoutKind.Auto, Size = 8)]
    public struct Fixed8 : IAutoLayoutFixedSingletonData
    {
    }
    
    [StructLayout(LayoutKind.Auto, Size = 16)]
    public struct Fixed16 : IAutoLayoutFixedSingletonData
    {
    }
    
    [StructLayout(LayoutKind.Auto, Size = 32)]
    public struct Fixed32 : IAutoLayoutFixedSingletonData
    {
    }
    
    [StructLayout(LayoutKind.Auto, Size = 64)]
    public struct Fixed64 : IAutoLayoutFixedSingletonData
    {
    }

    [StructLayout(LayoutKind.Auto, Size = 16)]
    public struct Data1 : IAutoLayoutFixedSingletonData//16
    {
        public int Index;//4
        public float Multiplier;//4
        public double Time;//8
    }

    [StructLayout(LayoutKind.Auto)]
    public struct Data2 : IAutoLayoutFixedSingletonData//13
    {
        public bool IsEnemy;//1
        public float Multiplier;//4
        public float TimeLossy;//4
        public float Index;//4
    }
    
    public unsafe class NativeStoreUnsafe
    {
        private NativeArray<Fixed16> _autoLayoutFixed16Array;
        private NativeHashMap<int, NativeReference<Fixed16>> _autoLayoutFixed16Map;

        public NativeStoreUnsafe(int maxSingletons)
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

            CheckSize<T, Fixed16>();

            Fixed16 reinterpret = *((Fixed16*) &data);

            if (_autoLayoutFixed16Map.TryGetValue(key, out var reference))
            {
                reference.Value = reinterpret;
            }
            else
            {
                _autoLayoutFixed16Map.Add(key, new NativeReference<Fixed16>(reinterpret, Allocator.Domain));
            }
        }

        public bool HasValueDirect<T>() where T : unmanaged, IAutoLayoutFixedSingletonData
        {
            return _autoLayoutFixed16Map.ContainsKey(typeof(T).GetHashCode());
        }
        
        public T GetValueDirect<T>() where T : unmanaged, IAutoLayoutFixedSingletonData
        {
            if (_autoLayoutFixed16Map.TryGetValue(typeof(T).GetHashCode(), out NativeReference<Fixed16> reference))
            {
                return *(T*)reference.GetUnsafePtr();
            }

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

        ~NativeStoreUnsafe()
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
    }
}