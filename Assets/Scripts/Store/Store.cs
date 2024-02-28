using System;
using System.Collections.Generic;
using UnityEngine;

namespace Store
{
    public interface ISingletonData
    {
    }

    internal abstract class StoreIndexHolder
    {
        internal abstract void Reset();
    }
    
    internal class StoreIndexHolder<T> : StoreIndexHolder where T : struct, ISingletonData
    {
        // ReSharper disable once StaticMemberInGenericType
        internal static int Index = -1;
        // ReSharper disable once StaticMemberInGenericType
        internal static int Generation = -1;

        public StoreIndexHolder()
        {
            Index = -1;
            Generation = -1;
        }

        ~StoreIndexHolder()
        {
            Debug.Log($"Destructor called for index: {Index}");
            Index = -1;
            Generation = -1;
        }

        internal override void Reset()
        {
            Debug.Log($"Resetting static values for index: {Index}");

            Index = -1;
            Generation = -1;
        }
    }
    
    public class Store
    {
        private readonly (ISingletonData singletonData, StoreIndexHolder indexHolder)[] _singletonsData;//todo maybe make this array static so it can be cleared when new Store is created or ensure that there is only one store by making it a singleton?
                                                                                                        //todo what if it is not a singleton and we may have different Store instances in different parts of application?

        private readonly Queue<int> _freeIndices = new Queue<int>();
        
        //todo ensure that there is only single store and enforce cleaning
        public Store(int maxSingletons)
        {
            if (maxSingletons <= 0)
            {
                throw new ArgumentException("Max singletons must be greater than 0.");
            }
            
            _singletonsData = new (ISingletonData, StoreIndexHolder)[maxSingletons];

            for (int i = 0; i < _singletonsData.Length; i++)
            {
                _freeIndices.Enqueue(i);
            }
        }

        public void AddOrUpdate<TSingletonData>(TSingletonData singleton) where TSingletonData : struct, ISingletonData
        {
            Debug.Log($"Index for type {typeof(TSingletonData)} on add is {StoreIndexHolder<TSingletonData>.Index}");
            if (StoreIndexHolder<TSingletonData>.Index < 0)
            {
                if (_freeIndices.Count > 0)
                {
                    int freeIndex = _freeIndices.Dequeue();
                    Debug.Log($"Adding singleton with free index: {freeIndex}");
                    _singletonsData[freeIndex] = (singleton, new StoreIndexHolder<TSingletonData>());
                    StoreIndexHolder<TSingletonData>.Index = freeIndex;
                    StoreIndexHolder<TSingletonData>.Generation++;
                    return;
                }
                
                throw new InvalidOperationException("Max singletons limit reached.");
            }

            Debug.Log($"Updating singleton with index: {StoreIndexHolder<TSingletonData>.Index}");

            _singletonsData[StoreIndexHolder<TSingletonData>.Index].singletonData = singleton;
            StoreIndexHolder<TSingletonData>.Generation++;
        }

        public bool Has<TSingletonData>() where TSingletonData : struct, ISingletonData
        {
            return StoreIndexHolder<TSingletonData>.Index >= 0;
        }

        public TSingletonData Value<TSingletonData>() where TSingletonData : struct, ISingletonData
        {
            return (TSingletonData)_singletonsData[StoreIndexHolder<TSingletonData>.Index].singletonData;
        }

        public bool Remove<TSingletonData>() where TSingletonData : struct, ISingletonData
        {
            int index = StoreIndexHolder<TSingletonData>.Index;
            if (index < 0)
                return false;
            //_singletonsData[index].indexHolder.Reset();
            _singletonsData[index] = (null, null);
            Debug.Log($"Removed index {index} is new free index.");
            _freeIndices.Enqueue(index);
            return true;
        }
        
        public bool Remove<TSingletonData>(out TSingletonData removedData) where TSingletonData : struct, ISingletonData
        {
            int index = StoreIndexHolder<TSingletonData>.Index;
            if (index < 0)
            {
                removedData = default;
                return false;
            }

            removedData = (TSingletonData) _singletonsData[index].singletonData;
            //_singletonsData[index].indexHolder.Reset();
            _singletonsData[index] = (null, null);
            _freeIndices.Enqueue(index);

            return false;
        }

        ~Store()
        {
            Debug.Log($"Store destructor called.");
            // foreach (var (_, indexHolder) in _singletonsData)
            // {
            //     indexHolder?.Reset();
            // }
        }
    }
}