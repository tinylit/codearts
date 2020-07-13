using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace CodeArts.Tests
{
    [TestClass]
    public class KeyGenTest
    {
        [TestMethod]
        public void Test()
        {
            var key = KeyGen.Create(6639549677571083173);

            var key2 = KeyGen.Create(6680757690605506560);

            RuntimeServManager.TryAddSingleton<IKeyGen>(new SnowflakeKeyGen(5, 12));

            var id = KeyGen.New();


            var list = new List<Key>();
            for (int i = 0; i < 100000; i++)
            {
                list.Add(KeyGen.New());
            }
            var date = list[0].ToLocalTime();

            var enumerable = list.Distinct().ToList();
        }
    }
}
