using System.Threading.Tasks;

namespace CodeArts.Emit
{
    /// <summary>
    /// 拦截执行方法。
    /// </summary>
    public class InterceptAsync
    {
        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="context">上下文。</param>
        public virtual Task RunAsync(InterceptContext context) => (Task)context.Main.Invoke(context.Target, context.Inputs);
    }

    /// <summary>
    /// 拦截执行方法。
    /// </summary>
    public class InterceptAsync<T>
    {
        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="context">上下文。</param>
        public virtual Task<T> RunAsync(InterceptContext context) => (Task<T>)context.Main.Invoke(context.Target, context.Inputs);
    }
}
