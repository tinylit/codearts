using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeArts.Tests
{
    [AttributeUsage(AttributeTargets.All, Inherited = true)]
    public class TestAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.All, Inherited = true)]
    public class Test2Attribute : TestAttribute
    {

    }

    public class Test3Attribute : TestAttribute
    {

    }

    [Test2]
    public interface IA1
    {
        [Test3]
        void Test();
    }

    [Test]
    public class A1 : IA1
    {
        [Test]
        public virtual void Test() { }
    }

    public class A2 : A1
    {
        [Test2]
        public override void Test()
        {
            base.Test();
        }
    }

    [TestClass]
    public class TestAttributes
    {
        [TestMethod]
        public void Test()
        {
            var method = typeof(A2).GetMethod(nameof(A2.Test));

            var array1 = method.GetCustomAttributes(typeof(TestAttribute), true);

            var array3 = typeof(A2).GetCustomAttributesData();
        }
    }
}
