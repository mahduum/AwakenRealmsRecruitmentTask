using NUnit.Framework;
using Unity.PerformanceTesting;
using Task2;

namespace Editor.Tests
{
    [TestFixture]
    public class ReinterpretTests
    {
        [Test]
        public void NativeArrayWriteAndGetData()
        {
            var store = new NativeStoreUnsafe(2);

            Data1 data1 = new Data1()
            {
                Index = 5,
                Multiplier = 5.44446f,
                Time = 56.77545432
            };

            Data2 data2 = new Data2()
            {
                Index = 2,
                IsEnemy = true,
                Multiplier = -4.301f,
                TimeLossy = 1234.3254f
            };
            
            store.AddToArray(data1, 0);
            store.AddToArray(data2, 1);

            var res1 = store.GetFromArray<Data1>(0);
            var res2 = store.GetFromArray<Data2>(1);

            Assert.AreEqual(res1.Index, data1.Index);
            Assert.AreEqual(res1.Multiplier, data1.Multiplier);
            Assert.AreEqual(res1.Time, data1.Time);
            Assert.AreEqual(res2.Index, data2.Index);
            Assert.AreEqual(res2.Multiplier, data2.Multiplier);
            Assert.AreEqual(res2.TimeLossy, data2.TimeLossy);
            Assert.AreEqual(res2.IsEnemy, data2.IsEnemy);
        }

        [Test]
        public void NativeHashMapAddAndGetData()
        {
            var store = new NativeStoreUnsafe(2);

            Data1 data1 = new Data1()
            {
                Index = 5,
                Multiplier = 5.44446f,
                Time = 56.77545432
            };

            Data2 data2 = new Data2()
            {
                Index = 2,
                IsEnemy = true,
                Multiplier = -4.301f,
                TimeLossy = 1234.3254f
            };
            
            store.AddOrUpdateDirect(data1);
            store.AddOrUpdateDirect(data2);

            var res1 = store.GetValueDirect<Data1>();
            var res2 = store.GetValueDirect<Data2>();
            
            Assert.AreEqual(res1.Index, data1.Index);
            Assert.AreEqual(res1.Multiplier, data1.Multiplier);
            Assert.AreEqual(res1.Time, data1.Time);

            Assert.AreEqual(res2.Index, data2.Index);
            Assert.AreEqual(res2.Multiplier, data2.Multiplier);
            Assert.AreEqual(res2.TimeLossy, data2.TimeLossy);
            Assert.AreEqual(res2.IsEnemy, data2.IsEnemy);
        }
        
        [PerformanceTest]
    }
}