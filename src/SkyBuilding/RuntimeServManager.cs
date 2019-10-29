using SkyBuilding.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace SkyBuilding
{
    /// <summary>
    /// 服务池
    /// </summary>
    public static class RuntimeServManager
    {
        /// <summary>
        /// 服务
        /// </summary>
        private static readonly Dictionary<Type, Type> ServiceCache = new Dictionary<Type, Type>();

        /// <summary>
        /// 服务
        /// </summary>
        private static readonly Dictionary<Type, Type> DefaultCache = new Dictionary<Type, Type>();

        /// <summary>
        /// 获取服务实现的函数
        /// </summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Func<object>> MethodCache =
            new System.Collections.Concurrent.ConcurrentDictionary<Type, Func<object>>();

        /// <summary>
        /// 添加服务
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        public static void TryAddSingleton<TService>()
            where TService : class
           => Nested<TService>.TryAdd(() => SkyBuilding.Singleton<TService>.Instance);

        /// <summary>
        /// 添加服务
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <param name="factory">实现工厂</param>
        public static void TryAddSingleton<TService>(Func<TService> factory)
            where TService : class
           => Nested<TService>.TryAdd(factory, true);

        /// <summary>
        /// 添加服务
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <typeparam name="TImplementation">服务实现</typeparam>
        public static void TryAddSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
            => Nested<TService>.TryAdd(() => Nested<TService, TImplementation>.Instance);

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <returns></returns>
        public static TService Singleton<TService>()
            where TService : class
         => Nested<TService>.Instance ?? throw new NotImplementedException();

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <typeparam name="TImplementation">服务实现</typeparam>
        /// <returns></returns>
        public static TService Singleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
            => Nested<TService>.Instance ?? Nested<TService, TImplementation>.Instance;

        /// <summary>
        /// 添加服务
        /// </summary>
        /// <param name="listenSingletonChanged">监听单例变化</param>
        /// <typeparam name="TService">服务类型</typeparam>
        public static TService Singleton<TService, TImplementation>(Action<TService> listenSingletonChanged)
            where TService : class
            where TImplementation : class, TService
        {
            Nested<TService>.Listen(listenSingletonChanged);

            return Nested<TService>.Instance ?? Nested<TService, TImplementation>.Instance;
        }

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <param name="factory">实现工厂</param>
        /// <returns></returns>
        public static TService Singleton<TService>(Func<TService> factory)
            where TService : class
            => Nested<TService>.Instance ?? factory.Invoke();

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <param name="factory">实现工厂</param>
        /// <param name="listenSingletonChanged">监听单例变化</param>
        /// <returns></returns>
        public static TService Singleton<TService>(Func<TService> factory, Action<TService> listenSingletonChanged)
            where TService : class
        {
            Nested<TService>.Listen(listenSingletonChanged);

            return Nested<TService>.Instance ?? factory.Invoke();
        }

        /// <summary>
        /// 静态内部类
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        private static class Nested<TService>
            where TService : class
        {
            private static Lazy<TService> _lazy;

            private static Action<TService> OnSingletonChanged;

            private static volatile bool _useBaseTryAdd = true;

            /// <summary>
            /// 静态构造函数
            /// </summary>
            static Nested()
            {
                ServiceCache[typeof(TService)] = typeof(Nested<TService>);
            }

            public static void Listen(Action<TService> listenSingletonChanged)
            {
                if (listenSingletonChanged is null)
                {
                    throw new ArgumentNullException(nameof(listenSingletonChanged));
                }

                OnSingletonChanged += listenSingletonChanged;
            }

            /// <summary>
            /// 尝试添加构造函数
            /// </summary>
            /// <typeparam name="TImplementation"></typeparam>
            /// <param name="factory"></param>
            public static void TryAdd(Func<TService> factory, bool defineService = false)
            {
                if (factory is null)
                {
                    throw new ArgumentNullException(nameof(factory));
                }

                if (defineService || _useBaseTryAdd)
                {
                    if (defineService)
                        _useBaseTryAdd = false;

                    var isValueCreated = _lazy?.IsValueCreated ?? false;

                    _lazy = new Lazy<TService>(factory);

                    if (isValueCreated)
                    {
                        OnSingletonChanged?.Invoke(_lazy.Value);
                    }
                }
            }

            /// <summary>
            /// 服务实例
            /// </summary>
            public static TService Instance => _lazy?.Value;
        }

        /// <summary>
        /// 静态内部类
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <typeparam name="TImplementation">服务实现类</typeparam>
        private static class Nested<TService, TImplementation>
            where TService : class
            where TImplementation : class, TService
        {
            private static readonly Lazy<TImplementation> _lazy;

            private static MethodInfo GetMethodInfo<T>(Func<Type, T> func) => func.Method;

            /// <summary>
            /// 获取服务实现
            /// </summary>
            /// <param name="serviceType">服务类型</param>
            /// <returns></returns>
            private static object GetServiceObject(Type serviceType)
            {
                if (ServiceCache.TryGetValue(serviceType, out Type implementType))
                {
                    if (DefaultCache.TryGetValue(serviceType, out Type defaultType))
                    {
                        //? 同时存在时以“Nested<TService, TImplementation>”类型存储。
                        var invoke = MethodCache.GetOrAdd(defaultType, type =>
                        {
                            var lamdaExp = Lambda<Func<object>>(Coalesce(Property(null, implementType, "Instance"), Property(null, type, "Instance")));

                            return lamdaExp.Compile();
                        });

                        return invoke.Invoke();
                    }

                    var invoke2 = MethodCache.GetOrAdd(implementType, type =>
                    {
                        var lamdaExp = Lambda<Func<object>>(Property(null, type, "Instance"));

                        return lamdaExp.Compile();
                    });

                    return invoke2.Invoke();
                }

                throw new NotImplementedException();
            }

            /// <summary>
            /// 静态构造函数
            /// </summary>
            /// <exception cref="NotSupportedException">实现类型中所有构造函数都包含值类型且值类型不是可选参数</exception>
            static Nested()
            {
                //~ 包含值类型且为非可选参数时，出现异常。

                var conversionType = typeof(TImplementation);

                var baseType = conversionType.BaseType;

                if (baseType is null || !baseType.IsGenericType || baseType.GetGenericTypeDefinition() != typeof(DesignMode.Singleton<>))
                {
                    var typeStore = RuntimeTypeCache.Instance.GetCache(conversionType);

                    var invoke = MakeImplement(typeStore.ConstructorStores //! 优先获取已实现指定参数的构造函
                        .Where(x => x.ParameterStores.All(y => y.IsOptional || ServiceCache.ContainsKey(y.ParameterType)))
                        .OrderByDescending(x => x.ParameterStores.Count)
                        .FirstOrDefault() ?? typeStore.ConstructorStores  //? 包含公共无参构造函数，可能实现的构造函数。
                            .Where(x => x.ParameterStores.Count == 0 || x.ParameterStores.All(y => y.IsOptional || y.ParameterType.IsInterface || y.ParameterType.IsClass))
                            .OrderBy(x => x.ParameterStores.Count)
                            .FirstOrDefault() ?? throw new NotSupportedException($"服务“{typeStore.FullName}”不包含任何可用于依赖注入的构造函数!"));

                    _lazy = new Lazy<TImplementation>(() => invoke.Invoke());
                }
                else
                {
                    var propertyExp = Property(null, conversionType, "Instance");

                    var lamdaExp = Lambda<Func<TImplementation>>(propertyExp);

                    var invoke = lamdaExp.Compile();

                    _lazy = new Lazy<TImplementation>(() => invoke.Invoke());
                }

                DefaultCache[typeof(TService)] = typeof(Nested<TService, TImplementation>);
            }

            /// <summary>
            /// 构造服务实现
            /// </summary>
            /// <param name="storeItem">构造函数</param>
            /// <returns></returns>
            private static Func<TImplementation> MakeImplement(ConstructorStoreItem storeItem)
            {
                var list = new List<Expression>();

                var typeMethod = GetMethodInfo(GetServiceObject);

                storeItem.ParameterStores.ForEach(info =>
                {
                    if (info.IsOptional)
                    {
                        list.Add(Convert(Constant(info.DefaultValue), info.ParameterType));
                    }
                    else
                    {
                        list.Add(Convert(Call(typeMethod, Constant(info.ParameterType)), info.ParameterType));
                    }
                });

                var bodyEx = New(storeItem.Member, list);

                var lamdaEx = Lambda<Func<TImplementation>>(bodyEx);

                return lamdaEx.Compile();
            }

            /// <summary>
            /// 单例
            /// </summary>
            public static TImplementation Instance => _lazy.Value;
        }
    }
}
