namespace CodeArts.Middleware
{
    /// <summary>
    /// 拦截执行方法。
    /// </summary>
    public class Intercept
    {
        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="context">上下文。</param>
        public virtual void Run(InterceptContext context) => context.Main.Invoke(context.Target, context.Inputs);
    }

    /// <summary>
    /// 拦截执行方法。
    /// </summary>
    public class Intercept<T>
    {
        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="context">上下文。</param>
        public virtual T Run(InterceptContext context) => (T)context.Main.Invoke(context.Target, context.Inputs);
    }
}
