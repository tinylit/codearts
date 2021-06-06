using System;
using System.Reflection;

namespace CodeArts.Interceptor
{
    /// <summary>
    /// 异步拦截上下文。
    /// </summary>
    public class InterceptAsyncContext
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="target">上下文。</param>
        /// <param name="main">调用函数。</param>
        /// <param name="inputs">函数参数。</param>
        public InterceptAsyncContext(object target, MethodInfo main, object[] inputs)
        {
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (main is null)
            {
                throw new ArgumentNullException(nameof(main));
            }

            if (inputs is null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }

            Target = target;
            Main = main;
            Inputs = inputs;
        }

        /// <summary>
        /// 上下文。
        /// </summary>
        public object Target { get; }

        /// <summary>
        /// 方法主体。
        /// </summary>
        public MethodInfo Main { get; }

        /// <summary>
        /// 输入参数。
        /// </summary>
        public object[] Inputs { get; }
    }
}
