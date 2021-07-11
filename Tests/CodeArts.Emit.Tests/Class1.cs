using CodeArts.AOP;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static CodeArts.Emit.Tests.Tests;

namespace CodeArts.Emit.Tests
{
    public class IDependencyProxy : IDependency
    {
        [NonSerialized]
        private readonly Tests.IDependency ____instance__ = new Tests.Dependency();

        private static readonly MethodInfo ____token__AopTest;

        static IDependencyProxy()
        {
            ____token__AopTest = null;
        }

        public virtual bool AopTest()
        {
            object[] inputs = new object[0];
            object obj = CodeArts.InterceptCaching.Intercept(new InterceptContext(____instance__, ____token__AopTest, inputs));
            if (obj == null)
            {
                return default;
            }
            return (bool)obj;
        }

        bool IDependency.AopTest() => AopTest();

        bool IDependency.AopTestByRef(int i, ref int j) => AopTest(i, ref j);

        public virtual bool AopTest(int i, ref int j)
        {
            object[] inputs = new object[2] { i, j };
            try
            {
                object obj = CodeArts.InterceptCaching.Intercept(new InterceptContext(____instance__, ____token__AopTest, inputs));
                if (obj == null)
                {
                    return default;
                }
                return (bool)obj;
            }
            finally
            {
                j = (int)inputs[1];
            }
        }

        /// <inheritdoc />
        public bool AopTestByOut(int i, out int j)
        {
            try
            {
                j = 0;

                return true;
            }
            catch (Exception e)
            {
                j = 1;

                return false;
            }
        }

        public T Get<T>() where T : struct => default;
    }
}
