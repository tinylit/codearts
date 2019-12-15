using CodeArts.Proxies.Generators;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;

namespace CodeArts.Proxies
{
    /// <summary>
    /// 代理生成器。
    /// </summary>
    public static class ProxyGenerator
    {
        private static IProxyGenerator proxyGenerator;

        private static readonly ConcurrentDictionary<long, Type> TypeCache = new ConcurrentDictionary<long, Type>();

        /// <summary>
        /// 构造函数
        /// </summary>
        static ProxyGenerator() => proxyGenerator = RuntimeServManager.Singleton<IProxyGenerator, DefaultProxyGenerator>(generator => proxyGenerator = generator);

        private static long CreateToken(Type type, ProxyOptions options)
        {
            return ((long)type.MetadataToken << 31) + options.MetadataToken;
        }

        private static Type CreateType(TypeBuilder typeBuilder)
        {
#if NETSTANDARD2_0
            return typeBuilder.CreateTypeInfo().AsType();
#else
            return typeBuilder.CreateType();
#endif
        }

        /// <summary>
        /// 获取无参构造函数的代理。
        /// </summary>
        /// <param name="options">代理配置</param>
        /// <typeparam name="T">代理类</typeparam>
        /// <returns></returns>
        public static IDefaultConstructorProxyOf<T> New<T>(ProxyOptions options) where T : class, new() => new DefaultConstructorProxyOf<T>(TypeCache.GetOrAdd(CreateToken(typeof(T), options), _ => CreateType(proxyGenerator.New(typeof(T), options))));

        /// <summary>
        /// 获取构造函数的代理。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options">代理选项</param>
        /// <returns></returns>
        public static IConstructorProxyOf<T> CreateInstance<T>(ProxyOptions options) where T : class => new ConstructorProxyOf<T>(TypeCache.GetOrAdd(CreateToken(typeof(T), options), _ => CreateType(proxyGenerator.New(typeof(T), options))));

        /// <summary>
        /// 获取构造函数的代理。
        /// </summary>
        /// <param name="classType">类</param>
        /// <param name="options">代理选项</param>
        /// <returns></returns>
        public static IConstructorProxyOf<object> CreateInstance(Type classType, ProxyOptions options) => new ConstructorProxyOf<object>(TypeCache.GetOrAdd(CreateToken(classType, options), _ => CreateType(proxyGenerator.New(classType, options))));

        /// <summary>
        /// 获取指定接口的代理。
        /// </summary>
        /// <param name="options">代理配置</param>
        /// <typeparam name="T">代理接口</typeparam>
        /// <returns></returns>
        public static IProxyOf<T> Of<T>(ProxyOptions options) where T : class => new ProxyOf<T>(TypeCache.GetOrAdd(CreateToken(typeof(T), options), _ => CreateType(proxyGenerator.Of(typeof(T), options))));

        /// <summary>
        /// 获取指定接口的代理。
        /// </summary>
        /// <param name="interfaceType">代理接口</param>
        /// <param name="options">代理配置</param>
        /// <returns></returns>
        public static IProxyOf<object> Of(Type interfaceType, ProxyOptions options) => new ProxyOf<object>(TypeCache.GetOrAdd(CreateToken(interfaceType, options), _ => CreateType(proxyGenerator.Of(interfaceType, options))), interfaceType);

        /// <summary>
        /// 获取指定类型的所有实现接口。(参数为接口类型时，会加入数组中。)
        /// </summary>
        /// <param name="type">类型</param>
        public static Type[] GetAllInterfaces(Type type)
        {
            var interfaces = new List<Type>();

            if (type.IsInterface)
            {
                interfaces.Add(type);
            }

            interfaces.AddRange(type.GetInterfaces());

            return Sort(interfaces.ToArray());
        }

        private static Type[] Sort(Type[] types)
        {
            //NOTE: is there a better, stable way to sort Types. We will need to revise this once we allow open generics
            Array.Sort(types, TypeNameComparer.Instance);
            //                ^^^^^^^^^^^^^^^^^^^^^^^^^
            // Using a `IComparer<T>` object instead of a `Comparison<T>` delegate prevents
            // an unnecessary level of indirection inside the framework (as the latter get
            // wrapped as `IComparer<T>` objects).
            return types;
        }

        private sealed class TypeNameComparer : IComparer<Type>
        {
            public static readonly TypeNameComparer Instance = new TypeNameComparer();

            public int Compare(Type x, Type y)
            {
                // Comparing by `type.AssemblyQualifiedName` would give the same result,
                // but it performs a hidden concatenation (and therefore string allocation)
                // of `type.FullName` and `type.Assembly.FullName`. We can avoid this
                // overhead by comparing the two properties separately.
                int result = string.CompareOrdinal(x.FullName, y.FullName);
                return result == 0 ? string.CompareOrdinal(x.Assembly.FullName, y.Assembly.FullName) : result;
            }
        }

        private sealed class ProxyOf<T> : IProxyOf<T> where T : class
        {
            private readonly Type _ofType;
            private readonly Func<T, IInterceptor, T> Invoke;

            private readonly static ConcurrentDictionary<Type, Func<T, IInterceptor, T>> InvokeCache = new ConcurrentDictionary<Type, Func<T, IInterceptor, T>>();

            public ProxyOf(Type proxyType)
            {
                Invoke = InvokeCache.GetOrAdd(proxyType, interfaceType =>
                {
                    var instanceExp = Expression.Parameter(typeof(T), "instance");

                    var interceptorExp = Expression.Parameter(typeof(IInterceptor), "interceptor");

                    var constructor = interfaceType.GetConstructor(new Type[] { typeof(T), typeof(IInterceptor) });

                    var lamdaExp = Expression.Lambda<Func<T, IInterceptor, T>>(Expression.New(constructor, instanceExp, interceptorExp), instanceExp, interceptorExp);

                    return lamdaExp.Compile();
                });
            }

            public ProxyOf(Type proxyType, Type ofType) : this(proxyType)
            {
                _ofType = ofType;
            }

            public T Of(T instance, IInterceptor interceptor)
            {
                if (_ofType is null || _ofType.IsAssignableFrom(instance.GetType()))
                {
                    return Invoke.Invoke(instance, interceptor ?? throw new ArgumentNullException(nameof(interceptor)));
                }

                throw new NotImplementedException($"实例未继承指定（{_ofType.FullName}）类型!");
            }
        }

        private sealed class ConstructorProxyOf<T> : IConstructorProxyOf<T> where T : class
        {
            private readonly Type proxyType;

            public ConstructorProxyOf(Type proxyType)
            {
                this.proxyType = proxyType;
            }

            public T CreateInstance(IInterceptor interceptor, params object[] arguments) => (T)Activator.CreateInstance(proxyType, (arguments == null || arguments.Length == 0) ? new object[1] { interceptor } : (new object[1] { interceptor }).Concat(arguments).ToArray());
        }

        private sealed class DefaultConstructorProxyOf<T> : IDefaultConstructorProxyOf<T> where T : class, new()
        {
            private readonly Func<IInterceptor, T> Invoke;
            private readonly static ConcurrentDictionary<Type, Func<IInterceptor, T>> InvokeCache = new ConcurrentDictionary<Type, Func<IInterceptor, T>>();

            public DefaultConstructorProxyOf(Type proxyType)
            {
                Invoke = InvokeCache.GetOrAdd(proxyType, classType =>
                {
                    var interceptorExp = Expression.Parameter(typeof(IInterceptor), "interceptor");

                    var constructor = classType.GetConstructor(new Type[] { typeof(IInterceptor) });

                    var lamdaExp = Expression.Lambda<Func<IInterceptor, T>>(Expression.New(constructor, interceptorExp), interceptorExp);

                    return lamdaExp.Compile();
                });
            }

            public T New(IInterceptor interceptor) => Invoke.Invoke(interceptor ?? throw new ArgumentNullException(nameof(interceptor)));
        }
    }
}
