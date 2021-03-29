using CodeArts.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace CodeArts.Casting.Implements
{
    /// <summary>
    /// 拷贝表达式。
    /// </summary>
    public class CopyToExpression : CopyToExpression<CopyToExpression>, ICopyToExpression, IProfileConfiguration, IProfile
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CopyToExpression()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="profile">配置。</param>
        public CopyToExpression(IProfileConfiguration profile) : base(profile)
        {
        }
    }

    /// <summary>
    /// 拷贝表达式。
    /// </summary>
    public class CopyToExpression<TCopyto> : ProfileExpression<TCopyto>, ICopyToExpression, IProfileConfiguration, IProfile where TCopyto : CopyToExpression<TCopyto>
    {
        private static readonly Type typeSelf = typeof(CopyToExpression<TCopyto>);

        private static readonly MethodInfo MapGenericMethod;
        private static readonly MethodInfo CastGenericMethod;
        private static readonly Type NullableType = typeof(Nullable<>);

        static CopyToExpression()
        {
            var mapperMethods = typeof(Mapper).GetMethods(BindingFlags.Public | BindingFlags.Static);

            MapGenericMethod = mapperMethods.Single(x => x.Name == nameof(Mapper.ThrowsMap) && x.IsGenericMethod);

            CastGenericMethod = mapperMethods.Single(x => x.Name == nameof(Mapper.ThrowsCast) && x.IsGenericMethod);
        }

        /// <summary>
        /// 类型创建器。
        /// </summary>
        public Func<Type, object> ServiceCtor { get; } = Activator.CreateInstance;

        /// <summary>
        /// 匹配模式。
        /// </summary>
        public PatternKind Kind { get; } = PatternKind.Property;

        /// <summary>
        /// 深度映射。
        /// </summary>
        public bool? IsDepthMapping { get; } = true;

        /// <summary>
        /// 允许空目标值。
        /// </summary>
        public bool? AllowNullDestinationValues { get; } = true;

        /// <summary>
        /// 允许空值传播映射。
        /// </summary>
        public bool? AllowNullPropagationMapping { get; } = false;

        private static readonly Type ObjectType = typeof(object);

        /// <summary>
        /// 构造函数。
        /// </summary>
        public CopyToExpression() { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="profile">配置。</param>
        public CopyToExpression(IProfileConfiguration profile)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }
            ServiceCtor = profile.ServiceCtor ?? Activator.CreateInstance;
            Kind = profile.Kind;
            IsDepthMapping = profile.IsDepthMapping ?? true;
            AllowNullDestinationValues = profile.AllowNullDestinationValues ?? true;
            AllowNullPropagationMapping = profile.AllowNullPropagationMapping ?? false;
        }

        /// <summary>
        /// 对象复制。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <param name="def">默认值。</param>
        /// <returns></returns>
        public T Copy<T>(T source, T def = default)
        {
            if (source == null)
                return def;

            try
            {
                return UnsafeCopyTo(source);
            }
            catch (InvalidCastException)
            {
                return def;
            }
        }

        /// <summary>
        /// 对象复制（不安全）。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <returns></returns>
        private T UnsafeCopyTo<T>(T source)
        {
            var conversionType = source.GetType();

            if (typeof(T) == ObjectType)
                return (T)UnsafeCopyTo(source, conversionType);

            var invoke = Create<T>(conversionType);

            return invoke.Invoke(source);
        }

        /// <summary>
        /// 对象复制（不安全）。
        /// </summary>
        /// <param name="source">数据源。</param>
        /// <param name="conversionType">目标类型。</param>
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
        /// 创建工厂。
        /// </summary>
        /// <typeparam name="TResult">返回数据类型。</typeparam>
        /// <returns></returns>
        public override Func<object, TResult> Create<TResult>(Type sourceType)
        {
            if (sourceType is null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            if ((sourceType == typeof(TResult) || typeof(TResult).IsAssignableFrom(sourceType)) && typeof(ICloneable).IsAssignableFrom(sourceType))
            {
                return source =>
                {
                    if (source is ICloneable cloneable)
                    {
                        return (TResult)cloneable.Clone();
                    }

                    return default;
                };
            }

            return base.Create<TResult>(sourceType);
        }

        /// <summary>
        /// 构建器。
        /// </summary>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected override Func<object, object> Create(Type sourceType, Type conversionType)
        {
            if (sourceType is null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            if (conversionType is null)
            {
                throw new ArgumentNullException(nameof(conversionType));
            }

            if ((sourceType == conversionType || conversionType.IsAssignableFrom(sourceType)) && typeof(ICloneable).IsAssignableFrom(sourceType))
            {
                return source =>
                {
                    if (source is ICloneable cloneable)
                    {
                        return cloneable.Clone();
                    }

                    return null;
                };
            }

            return base.Create(sourceType, conversionType);
        }

        /// <summary>
        /// 解决 相似对象的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeObject<TResult>(Type sourceType, Type conversionType)
        {
            var typeStore = TypeItem.Get(conversionType);

            var parameterExp = Parameter(typeof(object));

            var nullCst = Constant(null);

            var valueExp = Variable(sourceType);

            var targetExp = Variable(conversionType);

            var convertMethod = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });

            var list = new List<Expression> { Assign(valueExp, Convert(parameterExp, sourceType)) };

            var variables = new List<ParameterExpression> { valueExp };

            var arguments = new List<Expression>();

            list.Add(Assign(valueExp, Convert(parameterExp, sourceType)));

            if (conversionType == sourceType)
            {
                CreateInstance(typeStore);

                if (Kind == PatternKind.Property || Kind == PatternKind.All)
                {
                    typeStore.PropertyStores
                        .Where(x => x.CanWrite && x.CanRead)
                        .ForEach(info => PropertyConfig(info, Property(targetExp, info.Member), Property(valueExp, info.Member)));
                }

                if (Kind == PatternKind.Field || Kind == PatternKind.All)
                {
                    typeStore.FieldStores
                        .Where(x => x.CanWrite && x.CanRead)
                        .ForEach(info => PropertyConfig(info, Field(targetExp, info.Member), Field(valueExp, info.Member)));
                }
            }
            else
            {
                var typeCache = TypeItem.Get(sourceType);

                CreateInstance(typeCache);

                if (Kind == PatternKind.Property || Kind == PatternKind.All)
                {
                    typeStore.PropertyStores
                        .Where(x => x.CanWrite)
                        .ForEach(info =>
                        {
                            var item = typeCache.PropertyStores
                            .FirstOrDefault(x => x.CanRead && x.Name == info.Name)
                            ?? typeCache.PropertyStores
                            .FirstOrDefault(x => x.CanRead && x.Naming == info.Naming)
                            ?? typeCache.PropertyStores
                            .FirstOrDefault(x => x.CanRead && (x.Name == info.Naming || x.Naming == info.Name));

                            if (item is null)
                            {
                                return;
                            }

                            PropertyConfig(info, Property(targetExp, info.Member), Property(valueExp, item.Member));
                        });
                }

                if (Kind == PatternKind.Field || Kind == PatternKind.All)
                {
                    typeStore.FieldStores
                        .Where(x => x.CanWrite)
                        .ForEach(info =>
                        {
                            var item = typeCache.FieldStores
                            .FirstOrDefault(x => x.CanRead && x.Name == info.Name)
                            ?? typeCache.FieldStores
                            .FirstOrDefault(x => x.CanRead && x.Naming == info.Naming)
                            ?? typeCache.FieldStores
                            .FirstOrDefault(x => x.CanRead && (x.Name == info.Naming || x.Naming == info.Name));

                            if (item is null)
                            {
                                return;
                            }

                            PropertyConfig(info, Field(targetExp, info.Member), Field(valueExp, item.Member));
                        });
                }
            }

            list.Add(targetExp);

            var lamdaExp = Lambda<Func<object, TResult>>(Block(new[] { valueExp, targetExp }, list), parameterExp);

            return lamdaExp.Compile();

            void CreateInstance(TypeItem storeItem)
            {
                var commonCtor = typeStore.ConstructorStores
                    .Where(x => x.CanRead)
                    .OrderBy(x => x.ParameterStores.Count)
                    .First();

                commonCtor.ParameterStores.ForEach(info =>
                {
                    switch (Kind)
                    {
                        case PatternKind.All:
                        case PatternKind.Property:
                            var item = storeItem.PropertyStores
                                .FirstOrDefault(x => x.CanRead && x.Name == info.Name)
                                ?? storeItem.PropertyStores
                                .FirstOrDefault(x => x.CanRead && x.Name == info.Naming);

                            if (item is null)
                            {
                                if (Kind == PatternKind.All)
                                {
                                    goto case PatternKind.Field;
                                }

                                arguments.Add(Default(info.ParameterType));
                            }
                            else
                            {
                                ParameterConfig(info, Property(valueExp, item.Member));
                            }
                            break;
                        case PatternKind.Field:
                            var item2 = storeItem.FieldStores
                                .FirstOrDefault(x => x.CanRead && x.Name == info.Name)
                                ?? storeItem.FieldStores
                                .FirstOrDefault(x => x.CanRead && x.Name == info.Naming);

                            if (item2 is null)
                            {
                                arguments.Add(Default(info.ParameterType));
                            }
                            else
                            {
                                ParameterConfig(info, Field(valueExp, item2.Member));
                            }
                            break;
                        default:
                            arguments.Add(Default(info.ParameterType));
                            break;
                    }
                });

                list.Add(Assign(targetExp, New(commonCtor.Member, arguments)));
            }

            void PropertyConfig<T>(StoreItem<T> info, Expression left, Expression right) where T : MemberInfo
            {
                if (left.Type != right.Type)
                {
                    if (left.Type.IsValueType || left.Type == typeof(string) || !IsDepthMapping.Value)
                    {
                        try
                        {
                            right = Convert(right, left.Type);
                        }
                        catch (InvalidOperationException)
                        {
                            var rightObjExp = right.Type.IsValueType
                                ? Convert(right, typeof(object))
                                : right;

                            var body = Convert(Call(null, convertMethod, rightObjExp, Constant(left.Type)), left.Type);

                            if (left.Type.IsValueType || left.Type == typeof(string))
                            {
                                right = TryCatch(body, Catch(typeof(Exception), Call(null, CastGenericMethod.MakeGenericMethod(left.Type), rightObjExp)));
                            }
                            else
                            {
                                right = TryCatch(body, Catch(typeof(Exception), Default(left.Type)));
                            }
                        }
                    }
                }

                if (left.Type.IsValueType)
                {
                    if (AllowNullDestinationValues.Value || !left.Type.IsNullable())
                    {
                        list.Add(Assign(left, right));
                    }
                    else
                    {
                        list.Add(IfThen(NotEqual(right, nullCst), Assign(left, right)));
                    }

                    if (info.CanRead && !AllowNullDestinationValues.Value)
                    {
                        var typeCtor = left.Type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).First();

                        var defaultExp = Default(Nullable.GetUnderlyingType(left.Type));

                        list.Add(IfThen(Equal(left, nullCst), Assign(left, New(typeCtor, defaultExp))));
                    }

                    return;
                }

                if (left.Type == typeof(string))
                {
                    if (info.CanRead && !AllowNullPropagationMapping.Value && !AllowNullDestinationValues.Value)
                    {
                        list.Add(Assign(left, Coalesce(right, Coalesce(left, Constant(string.Empty)))));

                        return;
                    }

                    if (AllowNullDestinationValues.Value)
                    {
                        list.Add(Assign(left, right));
                    }
                    else
                    {
                        list.Add(IfThen(NotEqual(right, nullCst), Assign(left, right)));
                    }

                    if (info.CanRead && !AllowNullDestinationValues.Value)
                    {
                        list.Add(IfThen(Equal(left, nullCst), Assign(left, Constant(string.Empty))));
                    }

                    return;
                }

                if (IsDepthMapping.Value)
                {
                    list.Add(Assign(left, Call(null, MapGenericMethod.MakeGenericMethod(left.Type), right.Type.IsValueType ? Convert(right, ObjectType) : right)));
                }
                else
                {
                    list.Add(Assign(left, right));
                }
            }

            void ParameterConfig(ParameterItem info, Expression node)
            {
                var parameterType = info.ParameterType;

                var nameExp = Variable(info.ParameterType/*, info.Name.ToCamelCase()*/);

                variables.Add(nameExp);
                arguments.Add(nameExp);

                if (parameterType != node.Type)
                {
                    if (parameterType.IsValueType || parameterType == typeof(string) || !IsDepthMapping.Value)
                    {
                        try
                        {
                            node = Convert(node, parameterType);
                        }
                        catch (InvalidOperationException)
                        {
                            var rightObjExp = node.Type.IsValueType
                                ? Convert(node, typeof(object))
                                : node;

                            var body = Convert(Call(null, convertMethod, rightObjExp, Constant(parameterType)), parameterType);

                            if (parameterType.IsValueType || parameterType == typeof(string))
                            {
                                node = TryCatch(body, Catch(typeof(Exception), Call(null, CastGenericMethod.MakeGenericMethod(parameterType), rightObjExp)));
                            }
                            else
                            {
                                node = TryCatch(body, Catch(typeof(Exception), Default(parameterType)));
                            }
                        }
                    }
                }

                if (parameterType.IsValueType)
                {
                    if (AllowNullDestinationValues.Value || !parameterType.IsNullable())
                    {
                        list.Add(Assign(nameExp, node));
                    }
                    else
                    {
                        list.Add(IfThen(NotEqual(node, nullCst), Assign(nameExp, node)));
                    }

                    if (!AllowNullDestinationValues.Value)
                    {
                        if (info.IsOptional && info.DefaultValue != null)
                        {
                            list.Add(IfThen(Equal(nameExp, nullCst), Assign(nameExp, Convert(Constant(info.DefaultValue), parameterType))));
                        }
                        else
                        {
                            var typeCtor = parameterType.GetConstructors(BindingFlags.Public | BindingFlags.Instance).First();

                            var defaultExp = Default(Nullable.GetUnderlyingType(parameterType));

                            list.Add(IfThen(Equal(nameExp, nullCst), Assign(nameExp, New(typeCtor, defaultExp))));
                        }
                    }

                    return;
                }

                if (parameterType == typeof(string))
                {
                    if (!AllowNullPropagationMapping.Value && !AllowNullDestinationValues.Value)
                    {
                        list.Add(Assign(nameExp, Coalesce(node, Coalesce(nameExp, Constant(string.Empty)))));

                        return;
                    }

                    if (AllowNullDestinationValues.Value)
                    {
                        list.Add(Assign(nameExp, node));
                    }
                    else
                    {
                        list.Add(IfThen(NotEqual(node, nullCst), Assign(nameExp, node)));
                    }

                    if (!AllowNullDestinationValues.Value)
                    {
                        list.Add(IfThen(Equal(nameExp, nullCst), Assign(nameExp, Constant(string.Empty))));
                    }

                    return;
                }

                if (IsDepthMapping.Value)
                {
                    list.Add(Assign(nameExp, Call(null, MapGenericMethod.MakeGenericMethod(info.ParameterType), node.Type.IsValueType ? Convert(node, ObjectType) : node)));
                }
                else
                {
                    list.Add(Assign(nameExp, node));
                }
            }
        }

        /// <summary>
        /// 相似的对象（相同类型或目标类型继承源类型）。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
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

                return source => Mapper.ThrowsCast<TResult>(source);
            }

            if (sourceType == typeof(string))
                return source => (TResult)source;

            if (typeof(IEnumerable).IsAssignableFrom(sourceType))
                return ByLikeEnumarable<TResult>(sourceType, conversionType);

            return ByLikeObject<TResult>(sourceType, conversionType);
        }

        /// <summary>
        /// 解决 相似 KeyValuePair&lt;TKey, TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeKeyValuePair<TResult>(Type sourceType, Type conversionType)
        {
            var parameterExp = Parameter(typeof(object));

            var method = typeSelf.GetMethod(nameof(GetKeyValueLike), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(conversionType.GetGenericArguments());

            var bodyExp = Call(null, methodG, Convert(parameterExp, sourceType), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决相似 类似 IEnumarable 类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
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
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeEnumarableToUnknownInterface<TResult>(Type sourceType, Type conversionType) => throw new InvalidCastException();

        /// <summary>
        /// 解决 类似 IEnumarable 到 未知泛型接口的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArguments">泛型约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeEnumarableToUnknownInterface<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new InvalidCastException();

        /// <summary>
        /// 解决类似 IEnumerable 到类似 ICollection&lt;T&gt; 的数据操作。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型【T】约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeEnumarableToCollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object));

            var method = typeSelf.GetMethod(nameof(GetCollectionLikeByEnumarable), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArgument, conversionType);

            var bodyExp = Call(null, methodG, Convert(parameterExp, typeof(IEnumerable<>).MakeGenericType(typeArgument)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决类似 IEnumerable 到类似 Dictionary&lt;TKey,TValue&gt; 的数据操作。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArguments">泛型【TKey，TValue】约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeEnumarableToDictionaryLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments)
        {
            var parameterExp = Parameter(typeof(object));

            var method = typeSelf.GetMethod(nameof(GetDictionaryLikeByEnumarable), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArguments.Concat(new Type[] { conversionType }).ToArray());

            var bodyExp = Call(null, methodG, Convert(parameterExp, typeof(IDictionary<,>).MakeGenericType(typeArguments)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决类似 IEnumerable 到 Dictionary&lt;TKey,TValue&gt; 的数据操作。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArguments">泛型【TKey，TValue】约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeEnumarableToDictionary<TResult>(Type sourceType, Type conversionType, Type[] typeArguments)
        {
            var parameterExp = Parameter(typeof(object));

            var method = typeSelf.GetMethod(nameof(GetDictionaryByEnumarable), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArguments);

            var bodyExp = Call(null, methodG, Convert(parameterExp, typeof(IDictionary<,>).MakeGenericType(typeArguments)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(Convert(bodyExp, conversionType), parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决类似 IEnumerable 到 List&lt;T&gt; 的数据操作。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型【T】约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeEnumarableToList<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object));

            var method = typeSelf.GetMethod(nameof(GetListByEnumerable), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArgument);

            var bodyExp = Call(null, methodG, Convert(parameterExp, typeof(IEnumerable<>).MakeGenericType(typeArgument)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(Convert(bodyExp, conversionType), parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决类似 IEnumerable 到 IEnumerable 的数据转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeEnumarableToEnumarable<TResult>(Type sourceType, Type conversionType)
        {
            var method = GetMethodInfo<IEnumerable>(GetEnumerableByEnumerable);

            var parameterExp = Parameter(typeof(object));

            var bodyExp = Call(null, method, Convert(parameterExp, typeof(IEnumerable)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }
    }
}
