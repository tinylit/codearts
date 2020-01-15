using CodeArts.Runtime;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using static System.Linq.Expressions.Expression;

namespace CodeArts
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
        /// <param name="sourceType">源类型</param>
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
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByString<TResult>(Type sourceType, Type conversionType) => throw new NotSupportedException();

        /// <summary>
        /// 解决 任意类型 到 String 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ToString<TResult>(Type sourceType, Type conversionType)
        {
            var toString = sourceType.GetMethod("ToString", new Type[0] { });

            if (toString.DeclaringType == typeof(object))
                throw new NotSupportedException();

            var parameterExp = Parameter(sourceType, "source");

            var bodyExp = Call(parameterExp, toString);

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 相似的对象（同类型，或目标类型为源数据类型的接口或基类）
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLike<TResult>(Type sourceType, Type conversionType) => source => (TResult)source;

        /// <summary>
        /// 解决 任意类型 到 可空类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ToNullable<TResult>(Type sourceType, Type conversionType, Type typeArgument) => throw new NotSupportedException();

        /// <summary>
        /// 解决 任意类型 到 值类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ToValueType<TResult>(Type sourceType, Type conversionType) => throw new NotSupportedException();

        /// <summary>
        /// 解决 值类型 到 任意类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByValueType<TResult>(Type sourceType, Type conversionType) => throw new NotSupportedException();

        #region 可迭代类型

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T&gt; 到 目标类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
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
                            return ByIEnumarableLikeToDictionaryLike<TResult>(sourceType, conversionType, conversionType.GetGenericArguments());

                        if (typeDefinition == typeof(ICollection<>) || typeDefinition == typeof(IList<>))
                            return ByIEnumarableLikeToCollectionLike<TResult>(sourceType, conversionType, conversionType.GetGenericArguments().First());
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
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByEnumarableLike<TResult>(Type sourceType, Type conversionType) => ByIEnumarableLike<TResult>(sourceType, conversionType);

        #region IEnumarable to Interface
        /// <summary>
        /// 解决 类似 IEnumarable&lt;T1&gt; 到类似 ICollection&lt;KeyValuePair&lt;TKey,TValue&gt;&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型【KeyValuePair&lt;TKey,TValue&gt;】约束</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLikeToCollectionKeyValuePair<TResult>(Type sourceType, Type conversionType, Type typeArgument, Type[] typeArguments) => throw new NotSupportedException();

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T1&gt; 到类似 IDictionary&lt;TKey,TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLikeToDictionaryLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new NotSupportedException();

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T1&gt; 到类似 IDictionary&lt;TKey,TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLikeToIDictionaryLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new NotSupportedException();

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T1&gt; 到类似 ICollection&lt;T2&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型【T2】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLikeToICollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument) => throw new NotSupportedException();

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T1&gt; 到类似 IEnumarable&lt;T2&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型【T2】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLikeToIEnumarableLike<TResult>(Type sourceType, Type conversionType, Type typeArgument) => throw new NotSupportedException();

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T1&gt; 到类似 ICollection&lt;T2&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型【T2】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLikeToCollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument) => throw new NotSupportedException();

        /// <summary>
        /// 解决 IEnumarable&lt;T&gt; 到 未知泛型接口的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArguments">泛型约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLikeToUnknownInterface<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new NotSupportedException();

        #endregion

        /// <summary>
        /// 解决 IEnumarable&lt;T&gt; 到 泛型约束抽象类的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArguments">泛型约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLikeToAbstract<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new NotSupportedException();

        /// <summary>
        /// 解决 IEnumarable&lt;T&gt; 到 抽象类的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLikeToAbstract<TResult>(Type sourceType, Type conversionType) => throw new NotSupportedException();

        #endregion

        /// <summary>
        /// 解决 类 到 目标类型的转换。
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
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型【KeyValuePair&lt;TKey,TValue&gt;】约束</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToICollectionKeyValuePair<TResult>(Type sourceType, Type conversionType, Type typeArgument, Type[] typeArguments) => throw new NotSupportedException();

        /// <summary>
        /// 解决 类 到类似 ICollection&lt;T&gt; 类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型【T】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToICollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument) => throw new NotSupportedException();

        /// <summary>
        /// 解决 对象 到 类似 IEnumarable&lt;KeyValuePair&lt;TKey,TValue&gt;&gt; 类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型【KeyValuePair&lt;TKey,TValue&gt;】约束</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToIEnumarableKeyValuePair<TResult>(Type sourceType, Type conversionType, Type typeArgument, Type[] typeArguments) => throw new NotSupportedException();

        /// <summary>
        /// 解决 类 到类似 IEnumarable&lt;T&gt; 类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型【T】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToIEnumarableLike<TResult>(Type sourceType, Type conversionType, Type typeArgument) => throw new NotSupportedException();

        /// <summary>
        /// 解决 类 到 未知泛型接口的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArguments">泛型约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToUnknownInterface<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new NotSupportedException();
        #endregion

        /// <summary>
        /// 解决 对象 到类似 IDictionary&lt;TKey,TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToDictionaryLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new NotSupportedException();

        /// <summary>
        /// 解决 对象 到类似 IDictionary&lt;TKey,TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToIDictionaryLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new NotSupportedException();

        /// <summary>
        /// 解决 对象 到 泛型抽象类的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArguments">泛型约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToAbstract<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new NotSupportedException();

        /// <summary>
        /// 解决 对象 到 抽象类的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToAbstract<TResult>(Type sourceType, Type conversionType) => throw new NotSupportedException();

        /// <summary>
        /// 解决 对象 到类似 ICollection&lt;KeyValuePair&lt;TKey,TValue&gt;&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型【KeyValuePair&lt;TKey,TValue&gt;】约束</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToCollectionKeyValuePairLike<TResult>(Type sourceType, Type conversionType, Type typeArgument, Type[] typeArguments) => throw new NotSupportedException();

        /// <summary>
        /// 解决 对象 到类似 ICollection&lt;T&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型【T】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToCollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument) => throw new NotSupportedException();

        /// <summary>
        /// 解决 对象 到类似 IEnumerable&lt;KeyValuePair&lt;TKey,TValue&gt;&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型【KeyValuePair&lt;TKey,TValue&gt;】约束</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToEnumerableKeyValuePairLike<TResult>(Type sourceType, Type conversionType, Type typeArgument, Type[] typeArguments) => throw new NotSupportedException();

        /// <summary>
        /// 解决 对象 到类似 IEnumerable&lt;T&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型【T】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToEnumerableLike<TResult>(Type sourceType, Type conversionType, Type typeArgument) => throw new NotSupportedException();

        /// <summary>
        /// 解决 对象 到 泛型对象的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArguments">泛型约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToCommon<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new NotSupportedException();

        /// <summary>
        /// 解决 对象 到 任意对象 的操作，
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToCommon<TResult>(Type sourceType, Type conversionType) => throw new NotSupportedException();

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T&gt; 到 类 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIEnumarableLikeToCommon<TResult>(Type sourceType, Type conversionType) => throw new NotSupportedException();

        /// <summary>
        /// 添加指定目标类型工厂
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="invoke">将任意类型转为目标类型的工厂</param>
        public void Use<TResult>(Func<T, Type, Func<object, TResult>> invoke) => Nested<TResult>.Invoke = invoke;

        /// <summary>
        /// 映射 解决特定类型 【TSource】 到特定 【TResult】 的操作
        /// </summary>
        /// <typeparam name="TSource">源类型</typeparam>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="plan">将对象转为目标类型的方案</param>
        public void Absolute<TSource, TResult>(Func<TSource, TResult> plan)
        {
            if (plan is null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            Map(type => type == typeof(TSource), source => plan.Invoke((TSource)source));
        }

        /// <summary>
        /// 映射 解决与指定谓词所定义的条件相匹配的类型 到特定类型 【TResult】 的转换。
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
        /// 运行 解决类似 【TSource】（相同或其子类）的类型 到特定 【TResult】 类型的转换。
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

            Map(sourceType => sourceType == typeof(TSource) || typeof(TSource).IsAssignableFrom(sourceType), source => plan.Invoke((TSource)source));
        }

        /// <summary>
        /// 路由
        /// </summary>
        /// <typeparam name="TResult">结果类型</typeparam>
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
        /// <typeparam name="TResult">结果类型</typeparam>
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
