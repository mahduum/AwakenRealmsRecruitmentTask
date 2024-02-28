using System;
using System.Collections.Generic;

namespace Store
{
    internal static class StoreIndexHolder<T> where T : struct, ISingletonData
    {
        internal static int Index = -1;
        internal static int Generation = -1;
    }
    
    public interface ISingletonData
    {
    }

    public class Store
    {
        private readonly ISingletonData[] _singletonsData;

        private int _nextAvailableIndex = 0;

        private readonly Queue<int> _freeIndices = new Queue<int>();
        
        public Store(int maxSingletons)
        {
            if (maxSingletons <= 0)
            {
                throw new ArgumentException("Max singletons must be greater than 0.");
            }
            
            _singletonsData = new ISingletonData[maxSingletons];

            for (int i = 0; i < _singletonsData.Length; i++)
            {
                _freeIndices.Enqueue(i);
            }
        }

        public void AddOrUpdate<TSingletonData>(TSingletonData singleton) where TSingletonData : struct, ISingletonData
        {
            if (StoreIndexHolder<TSingletonData>.Index < 0)
            {
                if (_freeIndices.Count > 0)
                {
                    int freeIndex = _freeIndices.Dequeue();
                    _singletonsData[freeIndex] = singleton;
                    StoreIndexHolder<TSingletonData>.Index = freeIndex;
                    StoreIndexHolder<TSingletonData>.Generation++;
                }
                
                throw new InvalidOperationException("Max singletons limit reached.");
            }

            _singletonsData[StoreIndexHolder<TSingletonData>.Index] = singleton;
            StoreIndexHolder<TSingletonData>.Generation++;
        }

        public bool Has<TSingletonData>() where TSingletonData : struct, ISingletonData
        {
            return StoreIndexHolder<TSingletonData>.Index >= 0;
        }

        public TSingletonData Value<TSingletonData>() where TSingletonData : struct, ISingletonData
        {
            return (TSingletonData)_singletonsData[StoreIndexHolder<TSingletonData>.Index];
        }

        internal bool Remove<TSingletonData>() where TSingletonData : struct, ISingletonData
        {
            int index = StoreIndexHolder<TSingletonData>.Index;
            if (index < 0)
                return false;
            _singletonsData[index] = null;
            _freeIndices.Enqueue(index);
            return true;
        }
        
        internal bool Remove<TSingletonData>(out TSingletonData removedData) where TSingletonData : struct, ISingletonData
        {
            int index = StoreIndexHolder<TSingletonData>.Index;
            if (index < 0)
            {
                removedData = default;
                return false;
            }

            removedData = (TSingletonData) _singletonsData[index];
            _singletonsData[index] = null;
            _freeIndices.Enqueue(index);

            return false;
        }
    }
}