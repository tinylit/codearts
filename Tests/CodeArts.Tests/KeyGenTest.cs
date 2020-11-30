using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace CodeArts.Tests
{
    [TestClass]
    public class KeyGenTest
    {
        [TestMethod]
        public void Test()
        {
            var key = KeyGen.Create(6720191477021941760);

            var key2 = KeyGen.Create(6680757690605506560);

            RuntimeServPools.TryAddSingleton<IKeyGenFactory>(new SnowflakeFactory(5, 12));

            var id = KeyGen.New();

            var list = new List<Key>();
            for (int i = 0; i < 100000; i++)
            {
                list.Add(KeyGen.New());
            }

            var results = list.Distinct().ToList();

            Assert.IsTrue(list.Count == results.Count);
        }

        [TestMethod]
        public void Test2()
        {
            var list = new List<long>(1000000);

            for (int i = 1; i < 100; i++)
            {
                int index = (i - 1) * 10000;
                int len = i * 10000;

                Task.Factory.StartNew(() =>
                {
                    list.Add(KeyGen.Id());
                });
            }

            var results = list.Distinct().ToList();

            Assert.IsTrue(list.Count == results.Count);
        }

        [TestMethod]
        public void Test3()
        {
            var keyGen1 = KeyGenFactory.Create();
            var keyGen2 = KeyGenFactory.Create();
            var keyGen3 = KeyGenFactory.Create();

            var list1 = new long[1000000];
            var list2 = new long[1000000];
            var list3 = new long[1000000];

            var list = new List<Task>();

            for (int i = 1; i <= 100; i++)
            {
                int len = i * 10000;
                int index = (i - 1) * 10000;

                list.Add(Task.Factory.StartNew(() =>
                {
                    int j = index;

                    for (; j < len; j++)
                    {
                        list1[j] = (keyGen1.Id());
                        list2[j] = (keyGen2.Id());
                        list3[j] = (keyGen3.Id());
                    }

                    Debug.WriteLine(j.ToString());
                }));
            }

            Task.WhenAll(list).Wait();

            var results = list1.Distinct().ToList();
            var results2 = list2.Distinct().ToList();
            var results3 = list3.Distinct().ToList();

            Assert.IsTrue(list1.Length == results.Count);
            Assert.IsTrue(list2.Length == results2.Count);
            Assert.IsTrue(list3.Length == results3.Count);
        }
    }
}
