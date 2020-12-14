using System;

namespace CodeArts.Casting
{
    /// <summary>
    /// 调度器。
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// 是否能解决指定源类型。
        /// </summary>
        Predicate<Type> CanResolve { get; }
    }

    /// <summary>
    /// 调度器。
    /// </summary>
    /// <typeparam name="TResult">结果类型。</typeparam>
    public interface IDispatcher<TResult> : IDispatcher
    {
        /// <summary>
        /// 解决。
        /// </summary>
        Func<object, TResult> Resolve { get; }
    }
}
