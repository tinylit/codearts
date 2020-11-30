using System;
using System.Reflection;

namespace ConsoleEmit
{
    public class MyTest
    {
        private readonly IEmit instance;
        private readonly IInterceptor interceptor;

        public MyTest(IEmit instance, IInterceptor interceptor)
        {
            this.instance = instance;
            this.interceptor = interceptor;
        }

        public void Test()
        {
            throw new NotImplementedException();
        }

        public IEmit TestClas()
        {
            var binder = new InvokeBinder(instance, typeof(MyTest).GetMethod("TestClas"), new object[0]);

            interceptor.Intercept(binder);

            var returnValue = binder.ReturnValue;

            return (IEmit)returnValue;
        }

        public int TestInt32(int i)
        {
            object[] arguments = new object[1]
            {
                i
            };
            IIntercept invokeBinder = new InvokeBinder(instance, (MethodInfo)MethodBase.GetMethodFromHandle(new RuntimeMethodHandle(), typeof(IEmit).TypeHandle), arguments);
            interceptor.Intercept(invokeBinder);
            object returnValue = invokeBinder.ReturnValue;
            if (returnValue is null)
            {
                throw new InvalidOperationException("Interceptors failed to set a return value, or swallowed the exception thrown by the target.");
            }
            return (int)returnValue;
        }

        public int TestInt32(int i, string b)
        {
            object[] arguments = new object[2]
            {
                i,
                b
            };
            IIntercept invokeBinder = new InvokeBinder(instance, (MethodInfo)MethodBase.GetMethodFromHandle(new RuntimeMethodHandle(), typeof(IEmit).TypeHandle), arguments);
            interceptor.Intercept(invokeBinder);
            object returnValue = invokeBinder.ReturnValue;
            if (returnValue is null)
            {
                throw new InvalidOperationException("Interceptors failed to set a return value, or swallowed the exception thrown by the target.");
            }
            return (int)returnValue;
        }

        public T TestG<T>(T i)
        {
            Type[] types = new Type[] { typeof(T) };

            return i;
        }

        public bool TestValueType(int i)
        {
            throw new NotImplementedException();
        }
    }
}
