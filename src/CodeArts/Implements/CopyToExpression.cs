using CodeArts.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace CodeArts.Implements
{
    /// <summary>
    /// 拷贝表达式
    /// </summary>
    public class CopyToExpression : CopyToExpression<CopyToExpression>, ICopyToExpression, IProfileConfiguration, IProfile
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public CopyToExpression()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="profile">配置</param>
        public CopyToExpression(IProfileConfiguration profile) : base(profile)
        {
        }
    }

    /// <summary>
    /// 拷贝表达式
    /// </summary>
    public class CopyToExpression<TCopyto> : ProfileExpression<TCopyto>, ICopyToExpression, IProfileConfiguration, IProfile where TCopyto : CopyToExpression<TCopyto>
    {
        private static readonly Type typeSelf = typeof(CopyToExpression<TCopyto>);

        /// <summary>
        /// 类型创建器
        /// </summary>
        public Func<Type, object> ServiceCtor { get; } = Activator.CreateInstance;

        /// <summary>
        /// 匹配模式
        /// </summary>
        public PatternKind Kind { get; } = PatternKind.Property;

        /// <summary>
        /// 允许空目标值。
        /// </summary>
        public bool? AllowNullDestinationValues { get; } = true;

        /// <summary>
        /// 允许空值传播映射。
        /// </summary>
        public bool? AllowNullPropagationMapping { get; } = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        public CopyToExpression() { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="profile">配置</param>
        public CopyToExpression(IProfileConfiguration profile)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }
            ServiceCtor = profile.ServiceCtor ?? Activator.CreateInstance;
            Kind = profile.Kind;
            AllowNullDestinationValues = profile.AllowNullDestinationValues ?? true;
            AllowNullPropagationMapping = profile.AllowNullPropagationMapping ?? false;
        }

        /// <summary>
        /// 对象复制
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="def">默认值</param>
        /// <returns></returns>
        public T Copy<T>(T source, T def = default)
        {
            if (source == null)
                return def;

            try
            {
                return UnsafeCopyTo(source);
            }
            catch (NotSupportedException)
            {
                return def;
            }
        }

        /// <summary>
        /// 对象复制（不安全）
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="source">数据源</param>
        /// <returns></returns>
        private T UnsafeCopyTo<T>(T source)
        {
            var conversionType = source.GetType();

            if (typeof(T) == typeof(object))
                return (T)UnsafeCopyTo(source, conversionType);

            var invoke = Create<T>(conversionType);

            return invoke.Invoke(source);
        }

        /// <summary>
        /// 对象复制（不安全）
        /// </summary>
        /// <param name="source">数据源</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        private object UnsafeCopyTo(object source, Type conversionType)
        {
            var invoke = Create(conversionType, conversionType);

            return invoke.Invoke(source);
        }

        #region 反射使用
        private static MethodInfo GetMethodInfo<T>(Func<T, CopyToExpression<TCopyto>, T> func) => func.Method;

        private static IEnumerable GetEnumerableByEnumerable(IEnumerable source, CopyToExpression<TCopyto> copyTo)
        {
            foreach (var item in source)
            {
                if (item is null)
                    yield return null;

                yield return copyTo.UnsafeCopyTo(item, item.GetType());
            }
        }

        private static List<T> GetListByEnumerable<T>(IEnumerable<T> source, CopyToExpression<TCopyto> copyTo)
            => GetCollectionLikeByEnumarable<T, List<T>>(source, copyTo);

        private static KeyValuePair<TKey, TValue> GetKeyValueLike<TKey, TValue>(KeyValuePair<TKey, TValue> keyValuePair, CopyToExpression<TCopyto> copyTo)
        {
            return new KeyValuePair<TKey, TValue>(copyTo.UnsafeCopyTo(keyValuePair.Key), copyTo.UnsafeCopyTo(keyValuePair.Value));
        }

        private static TResult GetCollectionLikeByEnumarable<T, TResult>(IEnumerable<T> source, CopyToExpression<TCopyto> copyTo) where TResult : ICollection<T>
        {
            var results = (TResult)copyTo.ServiceCtor.Invoke(typeof(TResult));

            foreach (var item in source)
            {
                results.Add(copyTo.UnsafeCopyTo(item));
            }

            return results;
        }

        private static Dictionary<TKey, TValue> GetDictionaryByEnumarable<TKey, TValue>(IDictionary<TKey, TValue> source, CopyToExpression<TCopyto> copyTo) => GetDictionaryLikeByEnumarable<TKey, TValue, Dictionary<TKey, TValue>>(source, copyTo);

        private static TResult GetDictionaryLikeByEnumarable<TKey, TValue, TResult>(IDictionary<TKey, TValue> source, CopyToExpression<TCopyto> copyTo) where TResult : IDictionary<TKey, TValue>
        {
            var results = (TResult)copyTo.ServiceCtor.Invoke(typeof(TResult));

            foreach (var item in source)
            {
                results.Add(new KeyValuePair<TKey, TValue>(copyTo.UnsafeCopyTo(item.Key), copyTo.UnsafeCopyTo(item.Value)));
            }

            return results;
        }
        #endregion

        /// <summary>
        /// 解决 相似对象的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeObject<TResult>(Type sourceType, Type conversionType)
        {
            var typeStore = RuntimeTypeCache.Instance.GetCache(conversionType);

            var commonCtor = typeStore.ConstructorStores
                .Where(x => x.CanRead)
                .OrderBy(x => x.ParameterStores.Count)
                .FirstOrDefault();

            var parameterExp = Parameter(typeof(object), "source");

            var nullCst = Constant(null);

            var valueExp = Variable(sourceType, "value");

            var targetExp = Variable(conversionType, "target");

            var convertMethod = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });

            var list = new List<Expression> { Assign(valueExp, Convert(parameterExp, sourceType)) };

            var variables = new List<ParameterExpression> { valueExp };

            var arguments = new List<Expression>();

            list.Add(Assign(valueExp, Convert(parameterExp, sourceType)));

            if (conversionType == sourceType)
            {
                commonCtor.ParameterStores.ForEach(info =>
                {
                    if (Kind == PatternKind.Property || Kind == PatternKind.All)
                    {
                        var item = typeStore.PropertyStores.FirstOrDefault(x => x.CanRead && x.Name == info.Name);

                        if (!(item is null))
                        {
                            ParameterConfig(info, Property(valueExp, item.Member));

                            return;
                        }
                    }

                    if (Kind == PatternKind.Field || Kind == PatternKind.All)
                    {
                        var item = typeStore.FieldStores.FirstOrDefault(x => x.CanRead && x.Name == info.Name);

                        if (item is null)
                        {
                            arguments.Add(Default(info.ParameterType));
                        }
                        else
                        {
                            ParameterConfig(info, Field(valueExp, item.Member));
                        }
                    }
                });

                list.Add(Assign(targetExp, New(commonCtor.Member, arguments)));

                if (Kind == PatternKind.Property || Kind == PatternKind.All)
                {
                    typeStore.PropertyStores.Where(x => x.CanWrite && x.CanRead)
                       .ForEach(info => PropertyConfig(info, Property(targetExp, info.Member), Property(valueExp, info.Member)));
                }

                if (Kind == PatternKind.Field || Kind == PatternKind.All)
                {
                    typeStore.FieldStores.Where(x => x.CanWrite && x.CanRead)
                       .ForEach(info => PropertyConfig(info, Field(targetExp, info.Member), Field(valueExp, info.Member)));
                }
            }
            else
            {
                var typeCache = RuntimeTypeCache.Instance.GetCache(sourceType);

                commonCtor.ParameterStores.ForEach(info =>
                {
                    switch (Kind)
                    {
                        case PatternKind.Property:
                            var item = typeCache.PropertyStores.FirstOrDefault(x => x.CanRead && x.Name == info.Name);

                            if (item is null)
                            {
                                arguments.Add(Default(info.ParameterType));
                            }
                            else
                            {
                                ParameterConfig(info, Property(valueExp, item.Member));
                            }
                            break;
                        case PatternKind.Field:
                            var item2 = typeCache.FieldStores.FirstOrDefault(x => x.CanRead && x.Name == info.Name);

                            if (item2 is null)
                            {
                                arguments.Add(Default(info.ParameterType));
                            }
                            else
                            {
                                ParameterConfig(info, Field(valueExp, item2.Member));
                            }
                            break;
                        case PatternKind.All:
                            var item3 = typeCache.PropertyStores.FirstOrDefault(x => x.CanRead && x.Name == info.Name);

                            if (item3 is null)
                            {
                                var item4 = typeCache.FieldStores.FirstOrDefault(x => x.CanRead && x.Name == info.Name);

                                if (item4 is null)
                                {
                                    arguments.Add(Default(info.ParameterType));
                                }
                                else
                                {
                                    ParameterConfig(info, Field(valueExp, item4.Member));
                                }
                            }
                            else
                            {
                                ParameterConfig(info, Property(valueExp, item3.Member));
                            }
                            break;
                    }
                });

                list.Add(Assign(targetExp, New(commonCtor.Member, arguments)));

                if (Kind == PatternKind.Property || Kind == PatternKind.All)
                {
                    typeStore.PropertyStores.Where(x => x.CanWrite).ForEach(info =>
                    {
                        var item = typeCache.PropertyStores.FirstOrDefault(x => x.CanRead && x.Name == info.Name);

                        if (item is null) return;

                        PropertyConfig(info, Property(targetExp, info.Member), Property(valueExp, item.Member));
                    });
                }

                if (Kind == PatternKind.Field || Kind == PatternKind.All)
                {
                    typeStore.FieldStores.Where(x => x.CanWrite).ForEach(info =>
                    {
                        var item = typeCache.FieldStores.FirstOrDefault(x => x.CanRead && x.Name == info.Name);

                        if (item is null) return;

                        PropertyConfig(info, Field(targetExp, info.Member), Field(valueExp, item.Member));
                    });
                }
            }

            list.Add(targetExp);

            var lamdaExp = Lambda<Func<object, TResult>>(Block(new[] { valueExp, targetExp }, list), parameterExp);

            return lamdaExp.Compile();

            void PropertyConfig<T>(StoreItem<T> info, Expression left, Expression right) where T : MemberInfo
            {
                if (left.Type != right.Type)
                {
                    try
                    {
                        right = Convert(right, left.Type);
                    }
                    catch (InvalidOperationException)
                    {
                        right = Convert(right.Type.IsValueType ? Call(null, convertMethod, Convert(right, typeof(object)), Constant(left.Type)) : Call(null, convertMethod, right, Constant(left.Type)), left.Type);
                    }
                }

                if (left.Type.IsValueType || AllowNullDestinationValues.Value && AllowNullPropagationMapping.Value)
                {
                    list.Add(Assign(left, right));
                    return;
                }

                if (info.CanRead && !AllowNullPropagationMapping.Value && !AllowNullDestinationValues.Value && left.Type == typeof(string))
                {
                    list.Add(Assign(left, Coalesce(right, Coalesce(left, Constant(string.Empty)))));
                    return;
                }

                if (!AllowNullPropagationMapping.Value)
                {
                    list.Add(IfThen(NotEqual(right, nullCst), Assign(left, right)));
                }
                else if (!info.CanRead || left.Type != typeof(string))
                {
                    list.Add(Assign(left, right));
                    return;
                }

                if (!AllowNullDestinationValues.Value)
                {
                    list.Add(IfThen(Equal(left, nullCst), Assign(left, Constant(string.Empty))));
                }
            }

            void ParameterConfig(ParameterStoreItem info, Expression node)
            {
                var type = info.ParameterType;

                var nameExp = Variable(info.ParameterType, info.Name.ToCamelCase());

                variables.Add(nameExp);
                arguments.Add(nameExp);

                if (type == node.Type)
                {
                    list.Add(Assign(nameExp, node));

                    return;
                }

                if (type.IsNullable())
                {
                    type = Nullable.GetUnderlyingType(type);
                }

                if (type.IsEnum)
                {
                    type = Enum.GetUnderlyingType(type);
                }

                Expression expression;

                try
                {
                    expression = Convert(node, type);
                }
                catch (InvalidOperationException)
                {
                    expression = node.Type.IsValueType ? Call(null, convertMethod, Convert(node, typeof(object)), Constant(type)) : Call(null, convertMethod, node, Constant(type));
                }

                if (type != info.ParameterType)
                {
                    expression = Convert(expression, info.ParameterType);
                }

                list.Add(Assign(nameExp, expression));
            }
        }

        /// <summary>
        /// 相似的对象（相同类型或目标类型继承源类型）
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByLike<TResult>(Type sourceType, Type conversionType)
        {
            if (sourceType.IsValueType)
            {
                if (sourceType.IsKeyValuePair())
                {
                    return ByLikeKeyValuePair<TResult>(sourceType, conversionType);
                }

                if (sourceType.IsPrimitive)
                    return source => (TResult)source;

                return source => source.CastTo<TResult>();
            }

            if (sourceType == typeof(string))
                return source => (TResult)source;

            if (typeof(IEnumerable).IsAssignableFrom(sourceType))
                return ByLikeEnumarable<TResult>(sourceType, conversionType);

            return ByLikeObject<TResult>(sourceType, conversionType);
        }

        /// <summary>
        /// 解决 相似 KeyValuePair&lt;TKey, TValue&gt; 的转换
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeKeyValuePair<TResult>(Type sourceType, Type conversionType)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(GetKeyValueLike), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(conversionType.GetGenericArguments());

            var bodyExp = Call(null, methodG, Convert(parameterExp, sourceType), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决相似 类似 IEnumarable 类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeEnumarable<TResult>(Type sourceType, Type conversionType)
        {
            if (!conversionType.IsGenericType)
                return ByLikeEnumarableToEnumarable<TResult>(sourceType, conversionType);

            var typeDefinition = conversionType.GetGenericTypeDefinition();

            var typeArguments = conversionType.GetGenericArguments();

            if (conversionType.IsInterface)
            {
#if NET40
                if (typeDefinition == typeof(IDictionary<,>))
#else
                if (typeDefinition == typeof(IDictionary<,>) || typeDefinition == typeof(IReadOnlyDictionary<,>))
#endif
                    return ByLikeEnumarableToDictionary<TResult>(sourceType, conversionType, typeArguments);

#if NET40
                if (typeDefinition == typeof(IEnumerable<>) || typeDefinition == typeof(ICollection<>) || typeDefinition == typeof(IList<>))
#else
                if (typeDefinition == typeof(IEnumerable<>) || typeDefinition == typeof(ICollection<>) || typeDefinition == typeof(IList<>) || typeDefinition == typeof(IReadOnlyCollection<>) || typeDefinition == typeof(IReadOnlyList<>))
#endif
                    return ByLikeEnumarableToList<TResult>(sourceType, conversionType, typeArguments.First());

                return ByLikeEnumarableToUnknownInterface<TResult>(sourceType, conversionType, typeArguments);
            }

            var interfaces = conversionType.GetInterfaces();

            foreach (var item in interfaces)
            {
                if (item.IsGenericType)
                {
                    var type = item.GetGenericTypeDefinition();

#if NET40
                    if (type == typeof(IDictionary<,>))
#else
                    if (type == typeof(IDictionary<,>) || type == typeof(IReadOnlyDictionary<,>))
#endif
                        return ByLikeEnumarableToDictionaryLike<TResult>(sourceType, conversionType, item.GetGenericArguments());

#if NET40
                    if (type == typeof(ICollection<>) || type == typeof(IList<>))
#else
                    if (type == typeof(ICollection<>) || type == typeof(IList<>) || type == typeof(IReadOnlyCollection<>) || type == typeof(IReadOnlyList<>))
#endif
                        return ByLikeEnumarableToCollectionLike<TResult>(sourceType, conversionType, item.GetGenericArguments().First());
                }
            }

            return ByLikeEnumarableToUnknownInterface<TResult>(sourceType, conversionType);
        }

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T&gt; 到 未知泛型接口的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeEnumarableToUnknownInterface<TResult>(Type sourceType, Type conversionType) => throw new InvalidCastException();

        /// <summary>
        /// 解决 类似 IEnumarable 到 未知泛型接口的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArguments">泛型约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeEnumarableToUnknownInterface<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new InvalidCastException();

        /// <summary>
        /// 解决类似 IEnumerable 到类似 ICollection&lt;T&gt; 的数据操作。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArgument">泛型【T】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeEnumarableToCollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(GetCollectionLikeByEnumarable), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArgument, conversionType);

            var bodyExp = Call(null, methodG, Convert(parameterExp, typeof(IEnumerable<>).MakeGenericType(typeArgument)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决类似 IEnumerable 到类似 Dictionary&lt;TKey,TValue&gt; 的数据操作。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArguments">泛型【TKey，TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeEnumarableToDictionaryLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(GetDictionaryLikeByEnumarable), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArguments.Concat(new Type[] { conversionType }).ToArray());

            var bodyExp = Call(null, methodG, Convert(parameterExp, typeof(IDictionary<,>).MakeGenericType(typeArguments)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决类似 IEnumerable 到 Dictionary&lt;TKey,TValue&gt; 的数据操作。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArguments">泛型【TKey，TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeEnumarableToDictionary<TResult>(Type sourceType, Type conversionType, Type[] typeArguments)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(GetDictionaryByEnumarable), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArguments);

            var bodyExp = Call(null, methodG, Convert(parameterExp, typeof(IDictionary<,>).MakeGenericType(typeArguments)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(Convert(bodyExp, conversionType), parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决类似 IEnumerable 到 List&lt;T&gt; 的数据操作。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArgument">泛型【T】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeEnumarableToList<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(GetListByEnumerable), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArgument);

            var bodyExp = Call(null, methodG, Convert(parameterExp, typeof(IEnumerable<>).MakeGenericType(typeArgument)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(Convert(bodyExp, conversionType), parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决类似 IEnumerable 到 IEnumerable 的数据转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeEnumarableToEnumarable<TResult>(Type sourceType, Type conversionType)
        {
            var method = GetMethodInfo<IEnumerable>(GetEnumerableByEnumerable);

            var parameterExp = Parameter(typeof(object), "source");

            var bodyExp = Call(null, method, Convert(parameterExp, typeof(IEnumerable)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }
    }
}
