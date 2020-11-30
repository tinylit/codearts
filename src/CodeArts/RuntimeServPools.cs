using CodeArts.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;

namespace CodeArts
{
    /// <summary>
    /// 服务池。
    /// </summary>
    public static class RuntimeServPools
    {
        /// <summary>
        /// 服务。
        /// </summary>
        private static readonly Dictionary<Type, Type> ServiceCache = new Dictionary<Type, Type>();

        /// <summary>
        /// 服务。
        /// </summary>
        private static readonly Dictionary<Type, Type> DefaultCache = new Dictionary<Type, Type>();

        /// <summary>
        /// 获取服务实现的函数。
        /// </summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Expression> ExpressionCache =
            new System.Collections.Concurrent.ConcurrentDictionary<Type, Expression>();

        /// <summary>
        /// 添加服务。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        public static bool TryAddSingleton<TService>()
            where TService : class
           => Nested<TService>.TryAdd(() => CodeArts.Singleton<TService>.Instance);

        /// <summary>
        /// 添加服务。
        /// </summary>
        /// <param name="instance">实现。</param>
        /// <typeparam name="TService">服务类型。</typeparam>
        public static bool TryAddSingleton<TService>(TService instance)
            where TService : class
            => TryAddSingleton(() => instance);

        /// <summary>
        /// 添加服务。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <param name="factory">实现工厂。</param>
        public static bool TryAddSingleton<TService>(Func<TService> factory)
            where TService : class
           => Nested<TService>.TryAdd(factory ?? throw new ArgumentNullException(nameof(factory)), true);

        /// <summary>
        /// 添加服务。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <typeparam name="TImplementation">服务实现。</typeparam>
        public static bool TryAddSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
            => Nested<TService>.TryAdd(() => Nested<TService, TImplementation>.Instance);

        /// <summary>
        /// 获取服务。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <returns></returns>
        public static TService Singleton<TService>()
            where TService : class
         => Nested<TService>.Instance ?? throw new NotImplementedException();

        /// <summary>
        /// 获取服务。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <typeparam name="TImplementation">服务实现。</typeparam>
        /// <returns></returns>
        public static TService Singleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
            => Nested<TService>.Instance ?? Nested<TService, TImplementation>.Instance;
        private static class Nested<TService> where TService : class
        {
            private static Lazy<TService> _lazy;

            private static volatile bool _useBaseTryAdd = true;

            static Nested()
            {
                ServiceCache[typeof(TService)] = typeof(Nested<TService>);
            }

            public static bool TryAdd(Func<TService> factory, bool defineService = false)
            {
                if (defineService || _useBaseTryAdd)
                {
                    if (defineService)
                    {
                        _useBaseTryAdd = false;
                    }

                    if (_lazy is null || !_lazy.IsValueCreated)
                    {
                        _lazy = new Lazy<TService>(factory, true);

                        return true;
                    }
                }

                return false;
            }

            public static TService Instance => _lazy?.Value;
        }

        private static class Nested<TService, TImplementation>
            where TService : class
            where TImplementation : class, TService
        {
            private static readonly Lazy<TImplementation> _lazy;

            static Nested()
            {
                //~ 包含值类型且为非可选参数时，出现异常。

                var conversionType = typeof(TImplementation);

                var typeStore = TypeStoreItem.Get(conversionType);

                var invoke = MakeImplement(typeStore.ConstructorStores
                    .Where(x => x.ParameterStores.All(y => y.IsOptional || ServiceCache.ContainsKey(y.ParameterType)))
                    .OrderByDescending(x => x.ParameterStores.Count)
                    .FirstOrDefault() ?? throw new NotSupportedException($"服务“{typeStore.FullName}”不包含任何可用于依赖注入的构造函数!"));

                _lazy = new Lazy<TImplementation>(invoke);

                DefaultCache[typeof(TService)] = typeof(Nested<TService, TImplementation>);
            }

            private static Func<TImplementation> MakeImplement(ConstructorStoreItem storeItem)
            {
                var bodyEx = New(storeItem.Member, storeItem.ParameterStores.Select(item =>
                {
                    if (ServiceCache.TryGetValue(item.ParameterType, out Type implementType))
                    {
                        if (DefaultCache.TryGetValue(item.ParameterType, out Type defaultType))
                        {
                            //? 同时存在时以“Nested<TService, TImplementation>”类型存储。
                            return ExpressionCache.GetOrAdd(defaultType, type => Coalesce(Property(null, implementType, "Instance"), Property(null, type, "Instance")));
                        }

                        return ExpressionCache.GetOrAdd(implementType, type => Property(null, type, "Instance"));
                    }

                    return Convert(Constant(item.DefaultValue), item.ParameterType);
                }));

                var lamdaEx = Lambda<Func<TImplementation>>(bodyEx);

                return lamdaEx.Compile();
            }

            public static TImplementation Instance => _lazy.Value;
        }
    }
}
