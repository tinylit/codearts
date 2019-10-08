using SkyBuilding.Runtime;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using static System.Linq.Expressions.Expression;

namespace SkyBuilding
{
    /// <summary>
    /// 配置
    /// </summary>
    public abstract class ProfileExpression<T> : IProfileExpression, IProfile where T : ProfileExpression<T>
    {
        private static readonly ConcurrentDictionary<Type, Func<IProfile, Type, Func<object, object>>> LamdaCache = new ConcurrentDictionary<Type, Func<IProfile, Type, Func<object, object>>>();
        private static object[] ToObjectArray(object value)
        {
            return new object[] { value };
        }
        private static MethodInfo GetMethodInfo(Func<object, object[]> func)
        {
            return func.Method;
        }

        /// <summary>
        /// 创建工厂
        /// </summary>
        /// <typeparam name="TResult">返回数据类型</typeparam>
        /// <returns></returns>
        public virtual Func<object, TResult> Create<TResult>(Type sourceType) => Nested<TResult>.Create((T)this, sourceType ?? throw new ArgumentNullException(nameof(sourceType)));

        /// <summary>
        /// 构建器
        /// </summary>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, object> Create(Type sourceType, Type conversionType)
        {
            if (conversionType.IsNullable())
            {
                conversionType = Nullable.GetUnderlyingType(conversionType);
            }

            var factory = LamdaCache.GetOrAdd(conversionType, _ =>
            {
                var profileType = typeof(IProfile);

                var delegateType = typeof(Delegate);

                var toObjectArrMethod = GetMethodInfo(ToObjectArray);

                var invokeMethod = delegateType.GetMethod("DynamicInvoke");

                var createMethod = profileType.GetMethod("Create", new Type[] { typeof(Type) });

                var paramterExp = Parameter(profileType, "profile");

                var paramterSourceExp = Parameter(typeof(object), "source");

                var paramterTypeExp = Parameter(typeof(Type), "sourceType");

                var genericMethod = createMethod.MakeGenericMethod(conversionType);

                var methodCallExp = Call(paramterExp, genericMethod, paramterTypeExp);

                var convertExp = Convert(methodCallExp, delegateType);

                var invokeVar = Variable(delegateType, "lamda");

                var bodyExp = Lambda(Call(invokeVar, invokeMethod, Call(toObjectArrMethod, paramterSourceExp)), paramterSourceExp);

                var lamda = Lambda<Func<IProfile, Type, Func<object, object>>>(Block(new[] { invokeVar }, Assign(invokeVar, methodCallExp), bodyExp), paramterExp, paramterTypeExp);

                return lamda.Compile();
            });

            return factory.Invoke(this, sourceType);
        }

        /// <summary>
        /// 创建表达式
        /// </summary>
        /// <typeparam name="TResult">返回数据类型</typeparam>
        /// <returns></returns>
        protected virtual Func<object, TResult> CreateExpression<TResult>(Type sourceType)
        {
            Type conversionType = typeof(TResult);

            if (conversionType.IsNullable())
                return ToNullable<TResult>(sourceType, conversionType, conversionType.GetGenericArguments().First());

            if (conversionType == sourceType || conversionType.IsAssignableFrom(sourceType))
                return ByLike<TResult>(sourceType, conversionType);

            if (sourceType.IsValueType)
                return ByValueType<TResult>(sourceType, conversionType);

            if (conversionType.IsValueType)
                return ToValueType<TResult>(sourceType, conversionType);

            if (sourceType == typeof(string))
                return ByString<TResult>(sourceType, conversionType);

            if (conversionType == typeof(string))
                return ToString<TResult>(sourceType, conversionType);

            if (typeof(IEnumerable).IsAssignableFrom(sourceType))
                return ByEnumarable<TResult>(sourceType, conversionType);

            return ByObject<TResult>(sourceType, conversionType);
        }

        /// <summary>
        /// 通过字符串转换
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="sourceType"></param>
        /// <param name="conversionType"></param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByString<TResult>(Type sourceType, Type conversionType) => throw new InvalidCastException();

        /// <summary>
        /// 通过字符串转换
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="sourceType"></param>
        /// <param name="conversionType"></param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ToString<TResult>(Type sourceType, Type conversionType)
        {
            var toString = sourceType.GetMethod("ToString", new Type[0] { });

            if (toString.DeclaringType == typeof(object))
                throw new InvalidCastException();

            var parameterExp = Parameter(sourceType, "source");

            var bodyExp = Call(parameterExp, toString);

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 相似的对象（同类型，或目标类型为源数据类型的接口或基类）
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLike<TResult>(Type sourceType, Type conversionType) => source => (TResult)source;

        /// <summary>
        /// 转为可空类型
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="genericType">泛型约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ToNullable<TResult>(Type sourceType, Type conversionType, Type genericType) => throw new InvalidCastException();

        /// <summary>
        /// 通过值类型转换
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ToValueType<TResult>(Type sourceType, Type conversionType) => throw new InvalidCastException();

        /// <summary>
        /// 通过值类型转换
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByValueType<TResult>(Type sourceType, Type conversionType) => throw new InvalidCastException();

        /// <summary>
        /// 通过对象转换
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObject<TResult>(Type sourceType, Type conversionType)
        {
            if (conversionType.IsGenericType)
            {
                if (conversionType.IsInterface)
                {
                    var typeDefinition = conversionType.GetGenericTypeDefinition();

                    if (typeDefinition == typeof(ICollection<>) || typeDefinition == typeof(IEnumerable<>))
                    {
                        var genericType = conversionType.GetGenericArguments().First();

                        if (genericType.IsKeyValuePair())
                            return ByObjectToEnumarableKeyValue<TResult>(sourceType, conversionType, genericType, genericType.GetGenericArguments());

                        return ByObjectToEnumarable<TResult>(sourceType, conversionType, conversionType.GetGenericArguments().First());
                    }
                }

                if (conversionType.IsClass && !conversionType.IsAbstract)
                {
                    var types = conversionType.GetInterfaces();

                    foreach (var item in types.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>)))
                    {
                        var genericType = item.GetGenericArguments().First();

                        if (genericType.IsKeyValuePair())
                            return ByObjectToCollectionKeyValueLike<TResult>(sourceType, conversionType, genericType, genericType.GetGenericArguments());

                        return ByObjectToCollectionLike<TResult>(sourceType, conversionType, genericType);
                    }
                }
            }

            return ByObjectToCommon<TResult>(sourceType, conversionType);
        }

        /// <summary>
        /// 对象转对象
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToCommon<TResult>(Type sourceType, Type conversionType) => throw new InvalidCastException();

        /// <summary>
        /// 对象转可迭代类型
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="genericType">泛型约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToEnumarable<TResult>(Type sourceType, Type conversionType, Type genericType) => throw new InvalidCastException();

        /// <summary>
        /// 对象转可迭代类型(KeyValuePair<,>)
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="genericType">泛型约束</param>
        /// <param name="typeArguments">KeyValuePair<,>泛型参数</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToEnumarableKeyValue<TResult>(Type sourceType, Type conversionType, Type genericType, Type[] typeArguments) => throw new InvalidCastException();


        /// <summary>
        /// 对象转集合
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="genericType">泛型约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToCollectionLike<TResult>(Type sourceType, Type conversionType, Type genericType)
            => throw new InvalidCastException();

        /// <summary>
        /// 对象转集合(KeyValuePair<,>)
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="genericType">泛型约束</param>
        /// <param name="typeArguments">KeyValuePair<,>泛型参数</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToCollectionKeyValueLike<TResult>(Type sourceType, Type conversionType, Type genericType, Type[] typeArguments) => throw new InvalidCastException();

        /// <summary>
        /// 通过可迭代类型转换
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByEnumarable<TResult>(Type sourceType, Type conversionType)
        {
            if (conversionType.IsGenericType)
            {
                if (conversionType.IsInterface)
                {
                    if (conversionType.GetGenericTypeDefinition() == typeof(ICollection<>))
                        return ByEnumarableToCollection<TResult>(sourceType, conversionType, conversionType.GetGenericArguments().First());

                    if (conversionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                        return ByEnumarableToEnumarable<TResult>(sourceType, conversionType, conversionType.GetGenericArguments().First());
                }

                if (conversionType.IsClass && !conversionType.IsAbstract)
                {
                    var types = conversionType.GetInterfaces();

                    if (types.Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>)))
                        return ByEnumarableToCollectionLike<TResult>(sourceType, conversionType, types.First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>)).GetGenericArguments().First());
                }
            }

            return ByEnumarableToCommon<TResult>(sourceType, conversionType);
        }

        /// <summary>
        /// 可迭代类型转换为集合
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="genericType">泛型约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByEnumarableToCollectionLike<TResult>(Type sourceType, Type conversionType, Type genericType) => throw new InvalidCastException();

        /// <summary>
        /// 可迭代类型转换为对象
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByEnumarableToCommon<TResult>(Type sourceType, Type conversionType) => throw new InvalidCastException();

        /// <summary>
        /// 可迭代类型转换为可迭代类型
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="genericType">泛型约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByEnumarableToEnumarable<TResult>(Type sourceType, Type conversionType, Type genericType) => throw new InvalidCastException();

        /// <summary>
        /// 可迭代类型转换为集合
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="genericType">泛型约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByEnumarableToCollection<TResult>(Type sourceType, Type conversionType, Type genericType) => throw new InvalidCastException();

        /// <summary>
        /// 添加指定目标类型工厂
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="invoke">将任意类型转为目标类型的工厂</param>
        public void Use<TResult>(Func<T, Type, Func<object, TResult>> invoke) => Nested<TResult>.Invoke = invoke;

        /// <summary>
        /// 映射
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="plan">将对象转为目标类型的方案</param>
        public void Map<TResult>(Type sourceType, Func<object, TResult> plan)
        {
            if (sourceType is null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            Map(type => type == sourceType, plan);
        }

        /// <summary>
        /// 映射
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="predicate">判断源类型是否支持转到目标类型</param>
        /// <param name="plan">将对象转为目标类型的方案</param>
        public void Map<TResult>(Predicate<Type> predicate, Func<object, TResult> plan)
        {
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (plan is null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            Nested<TResult>.TryAdd(new MapRouter<TResult>
            {
                CanResolve = predicate,
                Plan = plan
            });
        }

        /// <summary>
        /// 运行(目标类型和源类型相同，或目标类型继承或实现源类型)
        /// </summary>
        /// <typeparam name="TSource">源数据类型</typeparam>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="plan">将源数据转为目标数据的方案</param>
        public void Run<TSource, TResult>(Func<TSource, TResult> plan)
        {
            if (plan is null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            Map(sourceType => sourceType == typeof(TSource) || typeof(TSource).IsAssignableFrom(sourceType), source =>
            {
                return plan.Invoke((TSource)source);
            });
        }

        /// <summary>
        /// 路由
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        private class MapRouter<TResult>
        {
            /// <summary>
            /// 是否能解决
            /// </summary>
            public Predicate<Type> CanResolve { get; set; }

            /// <summary>
            /// 解决方案
            /// </summary>
            public Func<object, TResult> Plan { get; set; }
        }

        /// <summary>
        /// 静态内部类
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        private static class Nested<TResult>
        {
            private static readonly AsyncLocal<bool> ThreadSecurity = new AsyncLocal<bool>();

            private static readonly List<MapRouter<TResult>> MapRouterCache = new List<MapRouter<TResult>>();

            private static readonly ConcurrentDictionary<Type, Func<object, TResult>> Cache = new ConcurrentDictionary<Type, Func<object, TResult>>();

            public static Func<object, TResult> Create(T profile, Type sourceType)
            => Cache.GetOrAdd(sourceType.IsNullable() ? Nullable.GetUnderlyingType(sourceType) : sourceType, type =>
            {
                if (ThreadSecurity.Value)
                    return profile.CreateExpression<TResult>(sourceType);

                foreach (var item in MapRouterCache)
                {
                    if (item.CanResolve(sourceType)) return item.Plan;
                }

                if (Invoke is null)
                    return profile.CreateExpression<TResult>(sourceType);

                ThreadSecurity.Value = true;

                try
                {
                    return Invoke.Invoke(profile, sourceType);
                }
                finally
                {
                    ThreadSecurity.Value = false;
                }
            });

            public static Func<T, Type, Func<object, TResult>> Invoke = null;

            /// <summary>
            /// 添加映射路由
            /// </summary>
            /// <param name="mapRouter">映射路由</param>
            public static void TryAdd(MapRouter<TResult> mapRouter) => MapRouterCache.Insert(0, mapRouter);

            /// <summary>
            /// 静态构造函数（优化编译器）
            /// </summary>
            static Nested()
            {
            }
        }
    }
}
