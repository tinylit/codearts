using System;
using System.Threading.Tasks;

namespace CodeArts.Interceptor
{
    /// <summary>
    /// 中间件拦截方法。
    /// </summary>
    public class MiddlewareInterceptAsync : InterceptAsync
    {
        private readonly InterceptAttribute middleware;
        private readonly InterceptAsync intercept;

        /// <summary>
        /// /构造函数。
        /// </summary>
        /// <param name="middleware">拦截器。</param>
        /// <param name="intercept">拦截器。</param>
        public MiddlewareInterceptAsync(InterceptAttribute middleware, InterceptAsync intercept)
        {
            if (middleware is null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            if (intercept is null)
            {
                throw new ArgumentNullException(nameof(intercept));
            }

            this.middleware = middleware;
            this.intercept = intercept;
        }

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="context">上下文。</param>
        public override Task RunAsync(InterceptAsyncContext context) => middleware.RunAsync(context, intercept);
    }

    /// <summary>
    /// 中间件拦截方法。
    /// </summary>
    public class MiddlewareInterceptAsync<T> : InterceptAsync<T>
    {
        private readonly InterceptAttribute middleware;
        private readonly InterceptAsync<T> intercept;

        /// <summary>
        /// /构造函数。
        /// </summary>
        /// <param name="middleware">拦截器。</param>
        /// <param name="intercept">拦截器。</param>
        public MiddlewareInterceptAsync(InterceptAttribute middleware, InterceptAsync<T> intercept)
        {
            if (middleware is null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            if (intercept is null)
            {
                throw new ArgumentNullException(nameof(intercept));
            }

            this.middleware = middleware;
            this.intercept = intercept;
        }

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="context">上下文。</param>
        public override Task<T> RunAsync(InterceptAsyncContext context) => middleware.RunAsync(context, intercept);
    }
}
