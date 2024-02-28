using NUnit.Framework;
using Store;

namespace Tests
{
    [TestFixture]
    public class StoreTests
    {
        struct StoreData : ISingletonData
        {
            public int DataValue;
        }
        
        [Test]
        public void AddItem()
        {
            Store.Store store = new Store.Store(4);

            StoreData storeData = new StoreData();
        }
    }
}