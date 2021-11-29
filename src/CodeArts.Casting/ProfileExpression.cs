using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using static System.Linq.Expressions.Expression;

namespace CodeArts.Casting
{
    internal static class MapExtensions
    {
        public const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        public const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        public static readonly MethodInfo Clone;
        public static readonly MethodInfo MapGeneric;
        public static readonly MethodInfo CreateMapGeneric;

        static MapExtensions()
        {

            Clone = typeof(ICloneable).GetMethod(nameof(ICloneable.Clone));

            MapGeneric = typeof(IMapConfiguration).GetMethods()
                            .First(x => x.Name == nameof(IMapConfiguration.Map) && x.IsGenericMethod);

            CreateMapGeneric = typeof(IProfile).GetMethod(nameof(IProfile.CreateMap));
        }
    }

    /// <summary>
    /// 配置。
    /// </summary>
    public abstract class ProfileExpression<T, TProfile> : IProfileExpression, IProfile, IDisposable where T : ProfileExpression<T, TProfile>, TProfile where TProfile : IProfile
    {
        private static readonly ConcurrentDictionary<Type, Func<IProfile, Type, Func<object, object>>> LamdaCache = new ConcurrentDictionary<Type, Func<IProfile, Type, Func<object, object>>>();

        private readonly ConcurrentDictionary<Type, IInvoker> Invokers = new ConcurrentDictionary<Type, IInvoker>();
        private readonly ConcurrentDictionary<Type, List<IRouter>> Routers = new ConcurrentDictionary<Type, List<IRouter>>();

        private static object[] ToObjectArray(object value) => new object[] { value };
        private static MethodInfo GetMethodInfo(Func<object, object[]> func) => func.Method;

        /// <summary>
        /// 线程安全。
        /// </summary>
        private readonly AsyncLocal<bool> SyncRoot = new AsyncLocal<bool>();

        /// <summary>
        /// 映射表达式(全局注册)。
        /// </summary>
        protected IReadOnlyList<IMapExpression> Maps { get; }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="maps">映射表达式。</param>
        /// <exception cref="ArgumentNullException"><paramref name="maps"/> is null.</exception>
        protected ProfileExpression(IEnumerable<IMapExpression> maps)
        {
            if (maps is null)
            {
                throw new ArgumentNullException(nameof(maps));
            }
#if NET40
            Maps = (new List<IMapExpression>(maps))
                .ToReadOnlyList();
#else
            Maps = new List<IMapExpression>(maps);
#endif
        }

        /// <summary>
        /// 映射。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="source">源数据。</param>
        /// <param name="def">默认值。</param>
        /// <returns><paramref name="source"/> is null, 返回 <paramref name="def"/>。</returns>
        public TResult Map<TResult>(object source, TResult def = default)
        {
            if (source is null)
            {
                return def;
            }

            var invoke = Nested<TResult>.Create((T)this, source.GetType());

            return invoke.Invoke(source);
        }

        /// <summary>
        /// 映射。
        /// </summary>
        /// <param name="source">源数据。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns><paramref name="source"/> is null，返回 <paramref name="conversionType"/> 类型的默认值。</returns>
        public object Map(object source, Type conversionType)
        {
            if (source is null)
            {
                if (conversionType.IsValueType)
                {
                    return Activator.CreateInstance(conversionType);
                }

                return null;
            }

            if (conversionType == typeof(object))
            {
                return source;
            }

            if (conversionType.IsNullable())
            {
                conversionType = Nullable.GetUnderlyingType(conversionType);
            }

            return LamdaCache.GetOrAdd(conversionType, Create)
                    .Invoke(this, source.GetType())
                    .Invoke(source);
        }

        Func<object, TResult> IProfile.CreateMap<TResult>(Type sourceType) => Nested<TResult>.Create((T)this, sourceType);

        private static readonly Type IProfileType = typeof(IProfile);
        private static readonly MethodInfo CreateMethod = IProfileType.GetMethod(nameof(IProfile.CreateMap), new Type[] { typeof(Type) });

        private static Func<IProfile, Type, Func<object, object>> Create(Type conversionType)
        {
            var paramterExp = Parameter(IProfileType, "profile");

            var paramterSourceExp = Parameter(typeof(object), "source");

            var paramterTypeExp = Parameter(typeof(Type), "sourceType");

            var genericMethod = CreateMethod.MakeGenericMethod(conversionType);

            var methodCallExp = Call(paramterExp, genericMethod, paramterTypeExp);

            var invokeFn = typeof(Func<,>).MakeGenericType(typeof(object), conversionType).GetMethod("Invoke");

            var bodyExp = conversionType.IsValueType
                ? Lambda(Convert(Call(methodCallExp, invokeFn, paramterSourceExp), typeof(object)), paramterSourceExp)
                : Lambda(Call(methodCallExp, invokeFn, paramterSourceExp), paramterSourceExp);

            var lambda = Lambda<Func<IProfile, Type, Func<object, object>>>(bodyExp, new ParameterExpression[] { paramterExp, paramterTypeExp });

            return lambda.Compile();
        }

        /// <summary>
        /// 添加指定目标类型工厂(同种目标类型第一次配置生效)。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="invoke">将任意类型转为目标类型的工厂。</param>
        /// <returns>返回真代表注册成功，返回假代表注册失败（目标类型已被指定其他调用器）。</returns>
        public bool Use<TResult>(Func<TProfile, Type, Func<object, TResult>> invoke) => Invokers.TryAdd(typeof(TResult), new Invoker<TResult>(invoke));

        /// <summary>
        /// 映射 解决特定类型 【TSource】 到特定 【TResult】 的操作。
        /// </summary>
        /// <typeparam name="TSource">源类型。</typeparam>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="project">将对象转为目标类型的方案。</param>
        public void Absolute<TSource, TResult>(Func<TSource, TResult> project)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            Map(type => type == typeof(TSource), source => project.Invoke((TSource)source));
        }

        /// <summary>
        /// 映射 解决与指定谓词所定义的条件相匹配的类型 到特定类型 【TResult】 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="canResolve">判断源类型是否支持转到目标类型。</param>
        /// <param name="project">将对象转为目标类型的方案。</param>
        public void Map<TResult>(Predicate<Type> canResolve, Func<object, TResult> project) => Routers.GetOrAdd(typeof(TResult), _ => new List<IRouter>()).Add(new MapRouter<TResult>(canResolve, project));

        /// <summary>
        /// 运行 解决类似 【TSource】（相同或其子类）的类型 到特定 【TResult】 类型的转换。
        /// </summary>
        /// <typeparam name="TSource">源类型。</typeparam>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="project">将源数据转为目标数据的方案。</param>
        public void Run<TSource, TResult>(Func<TSource, TResult> project)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            Map(sourceType => sourceType == typeof(TSource) || typeof(TSource).IsAssignableFrom(sourceType), source => project.Invoke((TSource)source));
        }

        /// <summary>
        /// 创建。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> Create<TResult>(Type sourceType, Type conversionType)
        {
            foreach (SimpleExpression item in Maps.OfType<SimpleExpression>())
            {
                if (item.IsMatch(sourceType, conversionType))
                {
                    return item.ToSolve<TResult>(sourceType, conversionType);
                }
            }

            foreach (var profile in Maps
                .Where(x => x.IsMatch(sourceType, conversionType))
                .OfType<IProfile>())
            {
                var createMap = MapExtensions.CreateMapGeneric.MakeGenericMethod(conversionType);

                return (Func<object, TResult>)createMap.Invoke(profile, new object[1] { sourceType });
            }

            throw new InvalidCastException();
        }

        private bool disposedValue = false; // 要检测冗余调用

        /// <summary>
        /// 释放资源。
        /// </summary>
        /// <param name="disposing">全部释放。</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Routers.Clear();
                Invokers.Clear();

                if (disposing)
                {
                    GC.SuppressFinalize(this);
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
        }

        private interface IInvoker
        {
        }

        private class Invoker<TResult> : IInvoker
        {
            private readonly Func<TProfile, Type, Func<object, TResult>> invoke;

            public Invoker(Func<TProfile, Type, Func<object, TResult>> invoke) => this.invoke = invoke ?? throw new ArgumentNullException(nameof(invoke));

            public Func<object, TResult> ToSolve(TProfile prefile, Type sourceType) => invoke.Invoke(prefile, sourceType);
        }

        private interface IRouter
        {
            Predicate<Type> CanResolve { get; }
        }

        private class MapRouter<TResult> : IRouter
        {
            /// <summary>
            /// 构造器。
            /// </summary>
            /// <param name="canResolve">是否能解决指定源类型。</param>
            /// <param name="project">解决。</param>
            public MapRouter(Predicate<Type> canResolve, Func<object, TResult> project)
            {
                CanResolve = canResolve ?? throw new ArgumentNullException(nameof(canResolve));
                Project = project ?? throw new ArgumentNullException(nameof(project));
            }

            /// <summary>
            /// 是否能解决指定源类型。
            /// </summary>
            public Predicate<Type> CanResolve { get; }

            /// <summary>
            /// 解决。
            /// </summary>
            public Func<object, TResult> Project { get; }
        }

        private static class Nested<TResult>
        {
            private static readonly Type runtimeType;

            private static readonly Type conversionType;

            private static readonly ConcurrentDictionary<Type, Func<T, Type, Func<object, TResult>>> NullableCache = new ConcurrentDictionary<Type, Func<T, Type, Func<object, TResult>>>();

            private static readonly ConcurrentDictionary<Type, Func<object, TResult>> CommonCache = new ConcurrentDictionary<Type, Func<object, TResult>>();

            private static Func<object, TResult> CreateByExtra(T profile, Type sourceType)
            {
                if (profile.Invokers.TryGetValue(conversionType, out IInvoker invoker) && (invoker is Invoker<TResult> invoke))
                {
                    if (profile.SyncRoot.Value)
                    {
                        goto label_core;
                    }

                    profile.SyncRoot.Value = true;

                    try
                    {
                        return invoke.ToSolve((TProfile)profile, sourceType);
                    }
                    finally
                    {
                        profile.SyncRoot.Value = false;
                    }
                }

label_core:
                {
                    if (profile.Routers.TryGetValue(conversionType, out var routers))
                    {
                        foreach (MapRouter<TResult> router in routers)
                        {
                            if (router.CanResolve(sourceType))
                            {
                                return router.Project;
                            }
                        }
                    }

                    return CommonCache.GetOrAdd(sourceType, type => CreateEx(profile, type, runtimeType));
                }
            }

            public static Func<object, TResult> Create(T profile, Type sourceType)
            {
                var standardType = sourceType.IsNullable() ? Nullable.GetUnderlyingType(sourceType) : sourceType;

                if (profile.Routers.Count > 0 || profile.Invokers.Count > 0)
                {
                    return CreateByExtra(profile, standardType);
                }

                return CommonCache.GetOrAdd(sourceType, type => CreateEx(profile, type, runtimeType));
            }

            private static Func<object, TResult> CreateEx(T profile, Type sourceType, Type runtimeType)
            {
                if (runtimeType.IsInterface)
                {
                    throw new InvalidCastException($"无法推测有效的接口({runtimeType})实现!");
                }

                if (runtimeType.IsAbstract)
                {
                    throw new InvalidCastException($"不支持使用抽象类型({runtimeType})转换!");
                }

                if (runtimeType.IsNullable())
                {
                    return NullableCache.GetOrAdd(Nullable.GetUnderlyingType(runtimeType), CreateUnderlyingType).Invoke(profile, sourceType);
                }

                return profile.Create<TResult>(sourceType, runtimeType);
            }

            private static Func<T, Type, Func<object, TResult>> CreateUnderlyingType(Type underlyingType)
            {
                var profileExp = Parameter(typeof(T));
                var sourceTypeExp = Parameter(typeof(Type));

                var sourceExp = Parameter(typeof(object));

                var bodyExp = Call(typeof(Nested<>).MakeGenericType(typeof(T), typeof(TProfile), underlyingType)
                        .GetMethod(nameof(Create), MapExtensions.StaticFlags), new Expression[] { profileExp, sourceTypeExp });

                var invokeFn = typeof(Func<,>).MakeGenericType(typeof(object), underlyingType).GetMethod("Invoke");

                var valueExp = Lambda(New(typeof(TResult).GetConstructor(new Type[1] { underlyingType }), Call(bodyExp, invokeFn, sourceExp)), new ParameterExpression[] { sourceExp });

                var lambdaExp = Lambda<Func<T, Type, Func<object, TResult>>>(valueExp, new ParameterExpression[] { profileExp, sourceTypeExp });

                return lambdaExp.Compile();
            }

            static Nested()
            {
                conversionType = runtimeType = typeof(TResult);

                if (conversionType.IsInterface)
                {
                    if (conversionType.IsGenericType)
                    {
                        var typeDefinition = conversionType.GetGenericTypeDefinition();

                        if (typeDefinition == typeof(IList<>)
                            || typeDefinition == typeof(IReadOnlyList<>)
                            || typeDefinition == typeof(ICollection<>)
                            || typeDefinition == typeof(IReadOnlyCollection<>)
                            || typeDefinition == typeof(IEnumerable<>))
                        {
                            runtimeType = typeof(List<>).MakeGenericType(conversionType.GetGenericArguments());
                        }
                        else if (typeDefinition == typeof(IDictionary<,>)
                            || typeDefinition == typeof(IReadOnlyDictionary<,>))
                        {
                            runtimeType = typeof(Dictionary<,>).MakeGenericType(conversionType.GetGenericArguments());
                        }
                    }
                    else if (conversionType == typeof(IEnumerable)
                        || conversionType == typeof(ICollection)
                        || conversionType == typeof(IList))
                    {
                        runtimeType = typeof(List<object>);
                    }
                }
            }
        }
    }
}
