using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Collections;

namespace Task2
{
    public interface ISingletonData
    {
    }

    public class Store
    {
        private readonly int _maxSingletons;

        private readonly ConcurrentDictionary<Type, ISingletonData> _singletons;
        
        internal Store(int maxSingletons)
        {
            if (maxSingletons <= 0)
            {
                throw new ArgumentException("Max singletons must be greater than 0.");
            }

            _maxSingletons = maxSingletons;
            _singletons = new ConcurrentDictionary<Type, ISingletonData>();
        }

        internal bool AddOrUpdate<TSingletonData>(TSingletonData singleton) where TSingletonData : struct, ISingletonData
        {
            Type type = typeof(TSingletonData);
            
            if (_singletons.TryGetValue(type, out var data))
            {
                return _singletons.TryUpdate(type, singleton,data);
            }

            if (_singletons.Count >= _maxSingletons)
            {
                throw new InvalidOperationException("Max singletons limit reached.");
            }

            return _singletons.TryAdd(type, singleton);
        }

        public bool Has<TSingletonData>() where TSingletonData : struct, ISingletonData
        {
            return _singletons.ContainsKey(typeof(TSingletonData));
        }

        public TSingletonData Value<TSingletonData>() where TSingletonData : struct, ISingletonData
        {
            if (_singletons.TryGetValue(typeof(TSingletonData), out var value))
            {
                return (TSingletonData)value;
            }

            throw new KeyNotFoundException($"Singleton of type {typeof(TSingletonData)} not found.");
        }

        internal bool Remove<TSingletonData>() where TSingletonData : struct, ISingletonData
        {
            return _singletons.Remove(typeof(TSingletonData), out var removed);
        }
        
        internal bool Remove<TSingletonData>(out TSingletonData removedData) where TSingletonData : struct, ISingletonData
        {
            if(_singletons.Remove(typeof(TSingletonData), out var removed))
            {
                removedData = (TSingletonData)removed;
                return true;
            }

            removedData = default;
            return false;
        }
    }
}