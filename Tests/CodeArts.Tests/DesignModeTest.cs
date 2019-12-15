using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeArts.Tests
{
    public class A : DesignMode.Singleton<A>
    {
        public int MyProperty { get; } = 8;
    }

    public class B
    {
        public int MyProperty { get; } = 8;
    }

    [TestClass]
    public class DesignModeTest
    {
        [TestMethod]
        public void Singleton()
        {
            var a1 = A.Instance;
            var a2 = Singleton<A>.Instance;

            Assert.AreEqual(a1, a2);

            var b1 = Singleton<B>.Instance;
            var b2 = Singleton<B>.Instance;

            Assert.AreEqual(b1, b2);
        }
    }
}
