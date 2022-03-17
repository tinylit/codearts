using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Expression> ExpressionCache = new System.Collections.Concurrent.ConcurrentDictionary<Type, Expression>();

        /// <summary>
        /// 添加服务。
        /// </summary>
        /// <param name="instance">实现。</param>
        /// <typeparam name="TService">服务类型。</typeparam>
        public static bool TryAddSingleton<TService>(TService instance)
            where TService : class
            => Nested<TService>.TryAdd(instance, true);

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
        => AutoNested<TService>.Instance;

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

        private class Nested<TService> where TService : class
        {
            private static Lazy<TService> _lazy = new Lazy<TService>(() => null);

            private static volatile bool _uninitialized = true;
            private static volatile bool _useBaseTryAdd = true;

            static Nested() => ServiceCache[typeof(TService)] = typeof(Nested<TService>);

            protected static void AddDefaultImpl(Func<TService> factory)
            {
                if (_uninitialized)
                {
                    TryAdd(factory, false);
                }
            }

            public static bool TryAdd(TService instance, bool defineService = false)
            {
                if (defineService || _useBaseTryAdd)
                {
                    if (defineService)
                    {
                        _useBaseTryAdd = false;
                    }

                    _uninitialized &= false;

                    if (!_lazy.IsValueCreated)
                    {
#if NETSTANDARD2_1_OR_GREATER
                        _lazy = new Lazy<TService>(instance);
#else
                        _lazy = new Lazy<TService>(() => instance);
#endif

                        return true;
                    }
                }

                return false;
            }

            public static bool TryAdd(Func<TService> factory, bool defineService = false)
            {
                if (defineService || _useBaseTryAdd)
                {
                    if (defineService)
                    {
                        _useBaseTryAdd = false;
                    }

                    _uninitialized &= false;

                    if (!_lazy.IsValueCreated)
                    {
                        _lazy = new Lazy<TService>(factory, true);

                        return true;
                    }
                }

                return false;
            }

            public static TService Instance => _lazy.Value;
        }

        private class AutoNested<TService> : Nested<TService> where TService : class
        {
            static AutoNested()
            {
                var type = typeof(TService);

                if (type.IsInterface || type.IsAbstract)
                {
                    AddDefaultImpl(() => throw new NotImplementedException($"未注入{type.FullName}服务的实现，可以使用【RuntimeServPools.TryAddSingleton<{type.Name}, {type.Name}Impl>()】注入服务实现，或使用【RuntimeServPools.Singleton<{type.Name}, Default{type.Name.TrimStart('i', 'I')}Impl>】安全获取实例，若未注入实现会生成【Default{type.Name.TrimStart('i', 'I')}Impl】的实例。"));
                }
                else
                {
                    AddDefaultImpl(() => Nested<TService, TService>.Instance);
                }
            }

            public new static TService Instance => Nested<TService>.Instance; //? 触发静态构造函数。
        }

        private static class Nested<TService, TImplementation>
        where TService : class
        where TImplementation : class, TService
        {
            private static readonly Lazy<TImplementation> _lazy;

            static Nested()
            {
                //~ 包含值类型且为非可选参数时，出现异常。

                var serviceType = typeof(TService);
                var conversionType = typeof(TImplementation);

                if (conversionType.IsInterface)
                {
                    throw new NotSupportedException($"单例服务({conversionType.FullName}=>{serviceType.FullName})的实现（{conversionType.FullName}）是接口，不能被实例化!");
                }

                if (conversionType.IsAbstract)
                {
                    throw new NotSupportedException($"单例服务({conversionType.FullName}=>{serviceType.FullName})的实现（{conversionType.FullName}）是抽象类，不能被实例化!");
                }

                var constructorInfos = conversionType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .OrderBy(x => x.IsPublic ? 0 : 1)
                    .ThenByDescending(x => x.GetParameters().Length)
                    .ToList();

                var constructorInfo = Resolved(constructorInfos, false) ?? Resolved(constructorInfos, true);

                if (constructorInfo is null)
                {
                    foreach (var item in constructorInfos.Skip(constructorInfos.Count - 1))
                    {
                        var parameterInfos = item.GetParameters();

                        foreach (var parameterInfo in parameterInfos)
                        {
                            if (parameterInfo.IsOptional)
                            {
                                continue;
                            }

                            throw new NotSupportedException($"单例服务（{conversionType.FullName}=>{serviceType.FullName}）的构造函数参数（{parameterInfo.ParameterType.FullName}）未注入单例支持，可以使用【RuntimeServPools.TryAddSingleton<{parameterInfo.ParameterType.Name}, {parameterInfo.ParameterType.Name}Impl>()】注入服务实现。");
                        }
                    }
                }

                var invoke = MakeImplement(constructorInfo);

                _lazy = new Lazy<TImplementation>(invoke);

                DefaultCache[typeof(TService)] = typeof(Nested<TService, TImplementation>);
            }

            private static ConstructorInfo Resolved(List<ConstructorInfo> constructorInfos, bool isAssignableFrom)
            {
                foreach (var constructorInfo in constructorInfos)
                {
                    bool flag = true;

                    var parameterInfos = constructorInfo.GetParameters();

                    foreach (var parameterInfo in parameterInfos)
                    {
                        if (parameterInfo.IsOptional || IsSurport(parameterInfo.ParameterType, isAssignableFrom))
                        {
                            continue;
                        }

                        flag = false;

                        break;
                    }

                    if (flag)
                    {
                        return constructorInfo;
                    }
                }

                return null;
            }

            private static bool IsSurport(Type parameterType, bool isAssignableFrom)
            {
                if (ServiceCache.ContainsKey(parameterType))
                {
                    return true;
                }

                if (isAssignableFrom)
                {
                    foreach (var kv in ServiceCache)
                    {
                        if (parameterType.IsAssignableFrom(kv.Value))
                        {
                            ServiceCache[parameterType] = kv.Value;

                            return true;
                        }
                    }
                }

                return false;
            }

            private static Func<TImplementation> MakeImplement(ConstructorInfo constructorInfo)
            {
                var parameterInfos = constructorInfo.GetParameters();

                List<Expression> expressions = new List<Expression>(parameterInfos.Length);

                foreach (var parameterInfo in parameterInfos)
                {
                    if (ServiceCache.TryGetValue(parameterInfo.ParameterType, out Type implementType))
                    {
                        if (DefaultCache.TryGetValue(parameterInfo.ParameterType, out Type defaultType))
                        {
                            //? 同时存在时以“Nested<TService, TImplementation>”类型存储。
                            expressions.Add(ExpressionCache.GetOrAdd(defaultType, type => Coalesce(Property(null, implementType, "Instance"), Property(null, type, "Instance"))));

                            continue;
                        }

                        expressions.Add(ExpressionCache.GetOrAdd(implementType, type => Property(null, type, "Instance")));

                        continue;
                    }

                    expressions.Add(Convert(Constant(parameterInfo.DefaultValue), parameterInfo.ParameterType));
                }

                var bodyEx = New(constructorInfo, expressions);

                var lamdaEx = Lambda<Func<TImplementation>>(bodyEx);

                return lamdaEx.Compile();
            }

            public static TImplementation Instance => _lazy.Value;
        }
    }
}
