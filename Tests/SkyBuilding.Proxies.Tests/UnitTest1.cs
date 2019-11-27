using SkyBuilding.Proxies.Generators;
using SkyBuilding.Proxies.Hooks;
using Xunit;

namespace SkyBuilding.Proxies.Tests
{
    public interface IEmitTest
    {
        void Test();

        bool TestValueType(int i);

        T Test<T>(T i);

        T TestConstraint<T>(T t) where T : struct;

        object TestRef(ref int i);
    }

    public class EmitTest : IEmitTest
    {
        private readonly object value;

        public EmitTest()
        {

        }

        public EmitTest(object value)
        {
            this.value = value;
        }

        public virtual void Test()
        {

        }

        public T Test<T>(T i)
        {
            return i;
        }

        public virtual T TestConstraint<T>(T t) where T : struct
        {
            return t;
        }

        object IEmitTest.TestRef(ref int i)
        {
            i += 10;

            return i;
        }

        public bool TestValueType(int i)
        {
            return i > 0;
        }
    }

    public class Interceptor : IInterceptor
    {
        public void Intercept(IIntercept intercept)
        {
            intercept.Proceed();
        }
    }

    public class UnitTest1
    {
        [Fact]
        public void TestInterface()
        {
            var of = ProxyGenerator.Of<IEmitTest>(new ProxyOptions(new NonIsByRefMethodsHook()));

            var instance = new EmitTest();
            var interceptor = new Interceptor();

            var proxy = of.Of(instance, interceptor);

            proxy.Test();

            var i = 6;
            var x = proxy.TestRef(ref i);

            var x2 = proxy.TestConstraint(15);

            var x3 = proxy.Test(16);

            var x4 = proxy.TestValueType(17);

        }

        [Fact]
        public void TestByNew()
        {
            var of = ProxyGenerator.New<EmitTest>(ProxyOptions.Default);

            var interceptor = new Interceptor();

            var proxy = of.New(interceptor);

            proxy.Test();

            var x2 = proxy.TestConstraint(15);

            var x3 = proxy.Test(16);

            var x4 = proxy.TestValueType(17);
        }
        [Fact]
        public void TestByCreateInstance()
        {
            var of = ProxyGenerator.CreateInstance<EmitTest>(ProxyOptions.Default);

            var interceptor = new Interceptor();

            var proxy = of.CreateInstance(interceptor, 18);

            proxy.Test();

            var x2 = proxy.TestConstraint(15);

            var x3 = proxy.Test(16);

            var x4 = proxy.TestValueType(17);
        }
    }
}
