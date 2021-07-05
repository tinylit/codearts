using CodeArts.AOP;
using System;
using System.Threading.Tasks;

namespace CodeArts
{
    /// <summary>
    /// 拦截属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public abstract class InterceptAttribute : Attribute
    {
        /// <summary>
        /// 运行方法。
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <param name="intercept">拦截器。</param>
        public abstract void Run(InterceptContext context, Intercept intercept);

        /// <summary>
        /// 运行方法。
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <param name="intercept">拦截器。</param>

        public abstract Task RunAsync(InterceptAsyncContext context, InterceptAsync intercept);

        /// <summary>
        /// 运行方法。
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <param name="intercept">拦截器。</param>

        public abstract Task<T> RunAsync<T>(InterceptAsyncContext context, InterceptAsync<T> intercept);
    }
}
