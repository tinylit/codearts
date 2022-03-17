using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeArts.Tests
{
    public class A : Singleton<A>
    {
        public int MyProperty { get; } = 8;
    }

    public class B
    {
        public int MyProperty { get; } = 8;
    }

    public interface ISingletonA
    {

    }

    public class SingletonA : ISingletonA
    {

    }

    public class SingletonB : Singleton<SingletonB>
    {
        private readonly ISingletonA singleton;

        private SingletonB(ISingletonA singleton)
        {
            this.singleton = singleton;
        }
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

        [TestMethod]
        public void Test()
        {
            ISingletonA singletonA = new SingletonA();

            RuntimeServPools.TryAddSingleton(singletonA);

            ISingletonA singletonAServ = RuntimeServPools.Singleton<ISingletonA>();

            Assert.AreEqual(singletonA, singletonAServ);

            ISingletonA singletonASingleton = Singleton<ISingletonA>.Instance;

            Assert.AreEqual(singletonA, singletonASingleton);

            var singleton = SingletonB.Instance;

            Assert.IsNotNull(singleton);
        }
    }
}
