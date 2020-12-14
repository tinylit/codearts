using System;

namespace CodeArts.Casting.Routers
{
    /// <summary>
    /// 映射路由。
    /// </summary>
    /// <typeparam name="TResult">目标类型。</typeparam>
    public sealed class MapRouter<TResult> : IDispatcher<TResult>, IDispatcher, IRouter
    {
        private static readonly Type conversionType = typeof(TResult);

        /// <summary>
        /// 构造器。
        /// </summary>
        /// <param name="canResolve">是否能解决指定源类型。</param>
        /// <param name="resolve">解决。</param>
        public MapRouter(Predicate<Type> canResolve, Func<object, TResult> resolve)
        {
            CanResolve = canResolve ?? throw new ArgumentNullException(nameof(canResolve));
            Resolve = resolve ?? throw new ArgumentNullException(nameof(resolve));
        }

        /// <summary>
        /// 目标类型。
        /// </summary>
        public Type ConversionType => conversionType;

        /// <summary>
        /// 是否能解决指定源类型。
        /// </summary>
        public Predicate<Type> CanResolve { get; }

        /// <summary>
        /// 解决。
        /// </summary>
        public Func<object, TResult> Resolve { get; }
    }
}
