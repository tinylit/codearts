using System.Reflection;

namespace CodeArts.DependencyInjection.Abstractions
{
    /// <summary>
    /// 方法调用。
    /// </summary>
    public interface IMethodInvocation
    {
        /// <summary>
        /// 方法主体。
        /// </summary>
        MethodInfo Main { get; }

        /// <summary>
        /// 输入参数。
        /// </summary>
        object[] Inputs { get; }

        /// <summary>
        /// 执行方法。
        /// </summary>
        void Execute();

        /// <summary>
        /// 返回值。
        /// </summary>
        object ReturnValue { get; set; }
    }
}
