using NUnit.Framework;
using Store;

namespace Tests
{
    [TestFixture]
    public class StoreTests
    {
        struct StoreData0 : ISingletonData
        {
            public int DataValue;
        }
        
        struct StoreData1 : ISingletonData
        {
            public int DataValue;
        }
        
        struct StoreData2 : ISingletonData
        {
            public int DataValue;
        }
        
        struct StoreData3 : ISingletonData
        {
            public int DataValue;
        }
        
        [Test]
        public void AddOrUpdateItem()
        {
            Store.Store store = new Store.Store(4);
            
            store.AddOrUpdate(new StoreData0(){DataValue = 10});
            store.AddOrUpdate(new StoreData1(){DataValue = 11});
            store.AddOrUpdate(new StoreData2(){DataValue = 12});
            store.AddOrUpdate(new StoreData3(){DataValue = 13});
            
            Assert.AreEqual(10, store.Value<StoreData0>().DataValue);
            Assert.AreEqual(11, store.Value<StoreData1>().DataValue);
            Assert.AreEqual(12, store.Value<StoreData2>().DataValue);
            Assert.AreEqual(13, store.Value<StoreData3>().DataValue);
            
            store.AddOrUpdate(new StoreData0(){DataValue = 20});
            store.AddOrUpdate(new StoreData1(){DataValue = 21});
            store.AddOrUpdate(new StoreData2(){DataValue = 22});
            store.AddOrUpdate(new StoreData3(){DataValue = 23});
            
            Assert.AreEqual(20, store.Value<StoreData0>().DataValue);
            Assert.AreEqual(21, store.Value<StoreData1>().DataValue);
            Assert.AreEqual(22, store.Value<StoreData2>().DataValue);
            Assert.AreEqual(23, store.Value<StoreData3>().DataValue);
        }
        
        [Test]
        public void ReuseIndices()
        {
            Store.Store store = new Store.Store(2);
            
            store.AddOrUpdate(new StoreData0(){DataValue = 10});
            store.AddOrUpdate(new StoreData1(){DataValue = 11});
            
            Assert.AreEqual(10, store.Value<StoreData0>().DataValue);
            Assert.AreEqual(11, store.Value<StoreData1>().DataValue);

            store.Remove<StoreData0>();
            store.AddOrUpdate(new StoreData3(){DataValue = 21});
            
            Assert.AreEqual(21, store.Value<StoreData3>().DataValue);
            Assert.AreEqual(11, store.Value<StoreData1>().DataValue);
        }
        
        [Test]
        public void OverflowStore()
        {
            Store.Store store = new Store.Store(1);
            
            store.AddOrUpdate(new StoreData0(){DataValue = 10});
            store.AddOrUpdate(new StoreData1(){DataValue = 11});
        }
    }
}