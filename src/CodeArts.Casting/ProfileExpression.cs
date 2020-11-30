using CodeArts.Routers;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using static System.Linq.Expressions.Expression;

namespace CodeArts.Casting
{
    /// <summary>
    /// 配置。
    /// </summary>
    public abstract class ProfileExpression<T> : IProfileExpression, IProfile, IDisposable where T : ProfileExpression<T>
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
        /// 线程安全。
        /// </summary>
        private readonly AsyncLocal<bool> SyncRoot = new AsyncLocal<bool>();

        /// <summary>
        /// 创建工厂。
        /// </summary>
        /// <typeparam name="TResult">返回数据类型。</typeparam>
        /// <returns></returns>
        public virtual Func<object, TResult> Create<TResult>(Type sourceType) => Nested<TResult>.Create(this, sourceType ?? throw new ArgumentNullException(nameof(sourceType)));

        /// <summary>
        /// 构建器。
        /// </summary>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
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
        /// 创建表达式。
        /// </summary>
        /// <typeparam name="TResult">返回数据类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
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
                return sourceType.IsInterface ? ByIEnumarableLike<TResult>(sourceType, conversionType) : ByEnumarableLike<TResult>(sourceType, conversionType);

            return ByObject<TResult>(sourceType, conversionType);
        }

        /// <summary>
        /// 解决 String 到 任意类型 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByString<TResult>(Type sourceType, Type conversionType) => throw new InvalidCastException();

        /// <summary>
        /// 解决 任意类型 到 String 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
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
        /// 解决 相似的对象（同类型，或目标类型为源数据类型的接口或基类）。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLike<TResult>(Type sourceType, Type conversionType) => source => (TResult)source;

        /// <summary>
        /// 解决 任意类型 到 可空类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ToNullable<TResult>(Type sourceType, Type conversionType, Type typeArgument) => throw new InvalidCastException();

        /// <summary>
        /// 解决 任意类型 到 值类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ToValueType<TResult>(Type sourceType, Type conversionType) => throw new InvalidCastException();

        /// <summary>
        /// 解决 值类型 到 任意类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByValueType<TResult>(Type sourceType, Type conversionType) => throw new InvalidCastException();

        #region 可迭代类型

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T&gt; 到 目标类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLike<TResult>(Type sourceType, Type conversionType)
        {
            if (conversionType.IsGenericType)
            {
                if (conversionType.IsInterface)
                {
                    var typeDefinition = conversionType.GetGenericTypeDefinition();

#if NET40
                    if (typeDefinition == typeof(IDictionary<,>))
#else
                    if (typeDefinition == typeof(IDictionary<,>) || typeDefinition == typeof(IReadOnlyDictionary<,>))
#endif
                        return ByIEnumarableLikeToIDictionaryLike<TResult>(sourceType, conversionType, conversionType.GetGenericArguments());
#if NET40
                if (typeDefinition == typeof(ICollection<>) || typeDefinition == typeof(IList<>))
#else
                    if (typeDefinition == typeof(ICollection<>) || typeDefinition == typeof(IList<>) || typeDefinition == typeof(IReadOnlyCollection<>) || typeDefinition == typeof(IReadOnlyList<>))
#endif
                        return ByIEnumarableLikeToICollectionLike<TResult>(sourceType, conversionType, conversionType.GetGenericArguments().First());

                    if (typeDefinition == typeof(IEnumerable<>))
                        return ByIEnumarableLikeToIEnumarableLike<TResult>(sourceType, conversionType, conversionType.GetGenericArguments().First());
                }

                if (conversionType.IsClass && conversionType.IsAbstract)
                    return ByIEnumarableLikeToAbstract<TResult>(sourceType, conversionType, conversionType.GetGenericArguments());

                if (conversionType.IsClass)
                {
                    var types = conversionType.GetInterfaces();

                    foreach (var type in types.Where(x => x.IsGenericType))
                    {
                        var typeDefinition = type.GetGenericTypeDefinition();

                        if (typeDefinition == typeof(IDictionary<,>))
                            return ByIEnumarableLikeToDictionaryLike<TResult>(sourceType, conversionType, type.GetGenericArguments());

                        if (typeDefinition == typeof(ICollection<>) || typeDefinition == typeof(IList<>))
                            return ByIEnumarableLikeToCollectionLike<TResult>(sourceType, conversionType, type.GetGenericArguments().First());
                    }
                }

                return ByIEnumarableLikeToUnknownInterface<TResult>(sourceType, conversionType, conversionType.GetGenericArguments());
            }

            if (conversionType.IsInterface || conversionType.IsAbstract)
            {
                return ByIEnumarableLikeToAbstract<TResult>(sourceType, conversionType);
            }

            return ByIEnumarableLikeToCommon<TResult>(sourceType, conversionType);
        }

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T&gt; 到 目标类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByEnumarableLike<TResult>(Type sourceType, Type conversionType) => ByIEnumarableLike<TResult>(sourceType, conversionType);

        #region IEnumarable to Interface
        /// <summary>
        /// 解决 类似 IEnumarable&lt;T1&gt; 到类似 ICollection&lt;KeyValuePair&lt;TKey,TValue&gt;&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标数据类型。</param>
        /// <param name="typeArgument">泛型【KeyValuePair&lt;TKey,TValue&gt;】约束。</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLikeToCollectionKeyValuePair<TResult>(Type sourceType, Type conversionType, Type typeArgument, Type[] typeArguments) => throw new InvalidCastException();

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T1&gt; 到类似 IDictionary&lt;TKey,TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLikeToDictionaryLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new InvalidCastException();

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T1&gt; 到类似 IDictionary&lt;TKey,TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLikeToIDictionaryLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new InvalidCastException();

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T1&gt; 到类似 ICollection&lt;T2&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型【T2】约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLikeToICollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument) => throw new InvalidCastException();

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T1&gt; 到类似 IEnumarable&lt;T2&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型【T2】约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLikeToIEnumarableLike<TResult>(Type sourceType, Type conversionType, Type typeArgument) => throw new InvalidCastException();

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T1&gt; 到类似 ICollection&lt;T2&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型【T2】约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLikeToCollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument) => throw new InvalidCastException();

        /// <summary>
        /// 解决 IEnumarable&lt;T&gt; 到 未知泛型接口的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArguments">泛型约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLikeToUnknownInterface<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new InvalidCastException();

        #endregion

        /// <summary>
        /// 解决 IEnumarable&lt;T&gt; 到 泛型约束抽象类的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArguments">泛型约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLikeToAbstract<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new InvalidCastException();

        /// <summary>
        /// 解决 IEnumarable&lt;T&gt; 到 抽象类的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLikeToAbstract<TResult>(Type sourceType, Type conversionType) => throw new InvalidCastException();

        #endregion

        /// <summary>
        /// 解决 类 到 目标类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObject<TResult>(Type sourceType, Type conversionType)
        {
            if (conversionType.IsGenericType)
            {
                if (conversionType.IsInterface)
                {
                    var typeDefinition = conversionType.GetGenericTypeDefinition();

                    var typeArguments = conversionType.GetGenericArguments();

#if NET40
                    if (typeDefinition == typeof(IDictionary<,>))
#else
                    if (typeDefinition == typeof(IDictionary<,>) || typeDefinition == typeof(IReadOnlyDictionary<,>))
#endif
                        return ByObjectToIDictionaryLike<TResult>(sourceType, conversionType, typeArguments);

#if NET40
                    if (typeDefinition == typeof(ICollection<>) || typeDefinition == typeof(IList<>))
#else
                    if (typeDefinition == typeof(ICollection<>) || typeDefinition == typeof(IList<>) || typeDefinition == typeof(IReadOnlyCollection<>) || typeDefinition == typeof(IReadOnlyList<>))
#endif
                    {
                        var typeArgument = typeArguments.First();

                        if (typeArgument.IsKeyValuePair())
                            return ByObjectToICollectionKeyValuePair<TResult>(sourceType, conversionType, typeArgument, typeArgument.GetGenericArguments());

                        return ByObjectToICollectionLike<TResult>(sourceType, conversionType, typeArgument);
                    }

                    if (typeDefinition == typeof(IEnumerable<>))
                    {
                        var typeArgument = typeArguments.First();

                        if (typeArgument.IsKeyValuePair())
                            return ByObjectToIEnumarableKeyValuePair<TResult>(sourceType, conversionType, typeArgument, typeArgument.GetGenericArguments());

                        return ByObjectToIEnumarableLike<TResult>(sourceType, conversionType, typeArgument);
                    }

                    return ByObjectToUnknownInterface<TResult>(sourceType, conversionType, typeArguments);
                }

                if (conversionType.IsClass && conversionType.IsAbstract)
                {
                    return ByObjectToAbstract<TResult>(sourceType, conversionType, conversionType.GetGenericArguments());
                }

                var types = conversionType.GetInterfaces();

                foreach (var item in types.Where(x => x.IsGenericType))
                {
                    var typeArguments = item.GetGenericArguments();

                    var typeDefinition = item.GetGenericTypeDefinition();

#if NET40
                    if (typeDefinition == typeof(IDictionary<,>))
#else
                    if (typeDefinition == typeof(IDictionary<,>) || typeDefinition == typeof(IReadOnlyDictionary<,>))
#endif
                        return ByObjectToDictionaryLike<TResult>(sourceType, conversionType, typeArguments);

                    var typeArgument = item.GetGenericArguments().First();

#if NET40
                    if (typeDefinition == typeof(ICollection<>) || typeDefinition == typeof(IList<>))
#else
                    if (typeDefinition == typeof(ICollection<>) || typeDefinition == typeof(IList<>) || typeDefinition == typeof(IReadOnlyCollection<>) || typeDefinition == typeof(IReadOnlyList<>))
#endif
                    {
                        if (typeArgument.IsKeyValuePair())
                            return ByObjectToCollectionKeyValuePairLike<TResult>(sourceType, conversionType, typeArgument, typeArgument.GetGenericArguments());

                        return ByObjectToCollectionLike<TResult>(sourceType, conversionType, typeArgument);
                    }

                    if (typeDefinition == typeof(IEnumerable<>))
                    {
                        if (typeArgument.IsKeyValuePair())
                            return ByObjectToEnumerableKeyValuePairLike<TResult>(sourceType, conversionType, typeArgument, typeArgument.GetGenericArguments());

                        return ByObjectToEnumerableLike<TResult>(sourceType, conversionType, typeArgument);
                    }
                }

                return ByObjectToCommon<TResult>(sourceType, conversionType, conversionType.GetGenericArguments());
            }

            if (conversionType.IsAbstract)
            {
                return ByObjectToAbstract<TResult>(sourceType, conversionType);
            }

            return ByObjectToCommon<TResult>(sourceType, conversionType);
        }

        #region Object To Interface
        /// <summary>
        /// 解决 对象 到 类似 ICollection&lt;KeyValuePair&lt;TKey,TValue&gt;&gt; 类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型【KeyValuePair&lt;TKey,TValue&gt;】约束。</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToICollectionKeyValuePair<TResult>(Type sourceType, Type conversionType, Type typeArgument, Type[] typeArguments) => throw new InvalidCastException();

        /// <summary>
        /// 解决 类 到类似 ICollection&lt;T&gt; 类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型【T】约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToICollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument) => throw new InvalidCastException();

        /// <summary>
        /// 解决 对象 到 类似 IEnumarable&lt;KeyValuePair&lt;TKey,TValue&gt;&gt; 类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型【KeyValuePair&lt;TKey,TValue&gt;】约束。</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToIEnumarableKeyValuePair<TResult>(Type sourceType, Type conversionType, Type typeArgument, Type[] typeArguments) => throw new InvalidCastException();

        /// <summary>
        /// 解决 类 到类似 IEnumarable&lt;T&gt; 类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型【T】约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToIEnumarableLike<TResult>(Type sourceType, Type conversionType, Type typeArgument) => throw new InvalidCastException();

        /// <summary>
        /// 解决 类 到 未知泛型接口的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArguments">泛型约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToUnknownInterface<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new InvalidCastException();
        #endregion

        /// <summary>
        /// 解决 对象 到类似 IDictionary&lt;TKey,TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToDictionaryLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new InvalidCastException();

        /// <summary>
        /// 解决 对象 到类似 IDictionary&lt;TKey,TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToIDictionaryLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new InvalidCastException();

        /// <summary>
        /// 解决 对象 到 泛型抽象类的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArguments">泛型约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToAbstract<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new InvalidCastException();

        /// <summary>
        /// 解决 对象 到 抽象类的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToAbstract<TResult>(Type sourceType, Type conversionType) => throw new InvalidCastException();

        /// <summary>
        /// 解决 对象 到类似 ICollection&lt;KeyValuePair&lt;TKey,TValue&gt;&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型【KeyValuePair&lt;TKey,TValue&gt;】约束。</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToCollectionKeyValuePairLike<TResult>(Type sourceType, Type conversionType, Type typeArgument, Type[] typeArguments) => throw new InvalidCastException();

        /// <summary>
        /// 解决 对象 到类似 ICollection&lt;T&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型【T】约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToCollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument) => throw new InvalidCastException();

        /// <summary>
        /// 解决 对象 到类似 IEnumerable&lt;KeyValuePair&lt;TKey,TValue&gt;&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型【KeyValuePair&lt;TKey,TValue&gt;】约束。</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToEnumerableKeyValuePairLike<TResult>(Type sourceType, Type conversionType, Type typeArgument, Type[] typeArguments) => throw new InvalidCastException();

        /// <summary>
        /// 解决 对象 到类似 IEnumerable&lt;T&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型【T】约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToEnumerableLike<TResult>(Type sourceType, Type conversionType, Type typeArgument) => throw new InvalidCastException();

        /// <summary>
        /// 解决 对象 到 泛型对象的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArguments">泛型约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToCommon<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new InvalidCastException();

        /// <summary>
        /// 解决 对象 到 任意对象 的操作。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToCommon<TResult>(Type sourceType, Type conversionType) => throw new InvalidCastException();

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T&gt; 到 类 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLikeToCommon<TResult>(Type sourceType, Type conversionType) => throw new InvalidCastException();

        /// <summary>
        /// 添加指定目标类型工厂(同种目标类型第一次配置生效)。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="invoke">将任意类型转为目标类型的工厂。</param>
        /// <returns>返回真代表注册成功，返回假代表注册失败（目标类型已被指定其他调用器）。</returns>
        public bool Use<TResult>(Func<T, Type, Func<object, TResult>> invoke) => Invokers.TryAdd(typeof(TResult), new Invoker<TResult>(invoke));

        /// <summary>
        /// 映射 解决特定类型 【TSource】 到特定 【TResult】 的操作。
        /// </summary>
        /// <typeparam name="TSource">源类型。</typeparam>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="resovle">将对象转为目标类型的方案。</param>
        public void Absolute<TSource, TResult>(Func<TSource, TResult> resovle)
        {
            if (resovle is null)
            {
                throw new ArgumentNullException(nameof(resovle));
            }

            Map(type => type == typeof(TSource), source => resovle.Invoke((TSource)source));
        }

        /// <summary>
        /// 映射 解决与指定谓词所定义的条件相匹配的类型 到特定类型 【TResult】 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="router">映射路由。</param>
        public void Map<TResult>(MapRouter<TResult> router)
        {
            if (router is null)
            {
                throw new ArgumentNullException(nameof(router));
            }

            Routers.Add(router);
        }

        /// <summary>
        /// 映射 解决与指定谓词所定义的条件相匹配的类型 到特定类型 【TResult】 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="canResolve">判断源类型是否支持转到目标类型。</param>
        /// <param name="resolve">将对象转为目标类型的方案。</param>
        public void Map<TResult>(Predicate<Type> canResolve, Func<object, TResult> resolve) => Map(new MapRouter<TResult>(canResolve, resolve));

        /// <summary>
        /// 运行 解决类似 【TSource】（相同或其子类）的类型 到特定 【TResult】 类型的转换。
        /// </summary>
        /// <typeparam name="TSource">源类型。</typeparam>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="resovle">将源数据转为目标数据的方案。</param>
        public void Run<TSource, TResult>(Func<TSource, TResult> resovle)
        {
            if (resovle is null)
            {
                throw new ArgumentNullException(nameof(resovle));
            }

            Map(sourceType => sourceType == typeof(TSource) || typeof(TSource).IsAssignableFrom(sourceType), source => resovle.Invoke((TSource)source));
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

        private class Invoker<TResult> : IInvoker
        {
            public Invoker(Func<T, Type, Func<object, TResult>> invoke) => Invoke = invoke ?? throw new ArgumentNullException(nameof(invoke));
            public Func<T, Type, Func<object, TResult>> Invoke { get; }
        }

        private readonly List<IRouter> Routers = new List<IRouter>();
        private readonly ConcurrentDictionary<Type, IInvoker> Invokers = new ConcurrentDictionary<Type, IInvoker>();

        private static class Nested<TResult>
        {
            private static readonly Type conversionType;

            private static readonly ConcurrentDictionary<Type, Func<object, TResult>> ProfileTypeCache = new ConcurrentDictionary<Type, Func<object, TResult>>();

            private static Func<object, TResult> CreateByExtra(ProfileExpression<T> profile, Type sourceType, Type conversionType)
            {
                if (profile.SyncRoot.Value)
                {
                    goto label_core;
                }

                if (profile.Invokers.TryGetValue(conversionType, out IInvoker invoker) && (invoker is Invoker<TResult> invoke))
                {
                    profile.SyncRoot.Value = true;

                    try
                    {
                        return invoke.Invoke.Invoke((T)profile, sourceType);
                    }
                    finally
                    {
                        profile.SyncRoot.Value = false;
                    }
                }

                label_core:
                {
                    if (profile.Routers.Count > 0)
                    {
                        foreach (IDispatcher<TResult> router in profile.Routers.Where(x => x.ConversionType == conversionType))
                        {
                            if (router.CanResolve(sourceType))
                            {
                                return router.Resolve;
                            }
                        }
                    }

                    return ProfileTypeCache.GetOrAdd(sourceType, _ => profile.CreateExpression<TResult>(sourceType));
                }
            }

            public static Func<object, TResult> Create(ProfileExpression<T> profile, Type sourceType)
            {
                var standardType = sourceType.IsNullable() ? Nullable.GetUnderlyingType(sourceType) : sourceType;

                if (profile.Routers.Count > 0 || profile.Invokers.Count > 0)
                {
                    return CreateByExtra(profile, standardType, conversionType);
                }

                return ProfileTypeCache.GetOrAdd(standardType, _ => profile.CreateExpression<TResult>(standardType));
            }

            static Nested() => conversionType = typeof(TResult);
        }
    }
}
