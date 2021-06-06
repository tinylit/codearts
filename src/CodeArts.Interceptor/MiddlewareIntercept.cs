using System;

namespace CodeArts.Interceptor
{
    /// <summary>
    /// 中间件拦截方法。
    /// </summary>
    public class MiddlewareIntercept : Intercept
    {
        private readonly InterceptAttribute middleware;
        private readonly Intercept intercept;

        /// <summary>
        /// /构造函数。
        /// </summary>
        /// <param name="middleware">拦截器。</param>
        /// <param name="intercept">拦截器。</param>
        public MiddlewareIntercept(InterceptAttribute middleware, Intercept intercept)
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
        public override void Run(InterceptContext context) => middleware.Run(context, intercept);
    }
}
