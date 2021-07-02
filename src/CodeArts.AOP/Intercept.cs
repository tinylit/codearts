namespace CodeArts.AOP
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
        public virtual void Run(InterceptContext context) => context.ReturnValue = context.Main.Invoke(context.Target, context.Inputs);
    }
}
