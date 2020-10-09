using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using UnitTest.Domain.Entities;

namespace CodeArts.ORM.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var users = new List<FeiUsers>();

            var user = users.DefaultIfEmpty(null).First();
        }
    }
}
