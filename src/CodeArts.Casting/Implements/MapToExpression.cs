using CodeArts.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace CodeArts.Casting.Implements
{
    /// <summary>
    /// 数据映射。
    /// </summary>
    public class MapToExpression : CopyToExpression<MapToExpression>, IMapToExpression, IProfileConfiguration, IProfile
    {
        private static readonly Type typeSelf = typeof(MapToExpression);

        /// <summary>
        /// 构造函数。
        /// </summary>
        public MapToExpression() { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="profile">配置。</param>
        public MapToExpression(IProfileConfiguration profile) : base(profile) { }

        /// <summary>
        /// 对象映射。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="obj">数据源。</param>
        /// <param name="def">默认值。</param>
        /// <returns></returns>
        public T Map<T>(object obj, T def = default)
        {
            if (obj is null)
            {
                return def;
            }

            try
            {
                var value = ThrowsMap<T>(obj);

                if (value == null)
                {
                    return def;
                }

                return value;
            }
            catch (InvalidCastException)
            {
                return def;
            }
        }

        /// <summary>
        /// 拷贝（异常上抛）。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <returns></returns>
        public T ThrowsMap<T>(object source)
        {
            var invoke = Create<T>(source.GetType());

            return invoke.Invoke(source);
        }

        /// <summary>
        /// 对象映射。
        /// </summary>
        /// <param name="source">数据源。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public object Map(object source, Type conversionType)
        {
            if (source is null)
            {
                return null;
            }

            try
            {
                return ThrowsMap(source, conversionType);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 不安全的映射（有异常）。
        /// </summary>
        /// <param name="source">数据源。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public object ThrowsMap(object source, Type conversionType)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (conversionType is null)
            {
                throw new ArgumentNullException(nameof(conversionType));
            }

            var invoke = Create(source.GetType(), conversionType);

            return invoke.Invoke(source);
        }

        #region 反射使用
        private static List<T> ByEnumarableToList<T>(IEnumerable source, MapToExpression mapTo)
            => ByEnumarableToCollectionLike<T, List<T>>(source, mapTo);

        private static TResult ByEnumarableToCollectionLike<T, TResult>(IEnumerable source, MapToExpression mapTo) where TResult : ICollection<T>
        {
            var results = (TResult)mapTo.ServiceCtor.Invoke(typeof(TResult));

            foreach (var item in source)
            {
                results.Add(mapTo.ThrowsMap<T>(item));
            }

            return results;
        }

        private static List<T> ByObjectToList<T>(object source, MapToExpression mapTo)
        => ByObjectToCollectionLike<T, List<T>>(source, mapTo);

        private static TResult ByObjectToCollectionLike<T, TResult>(object source, MapToExpression mapTo) where TResult : ICollection<T>
        {
            var value = mapTo.ThrowsMap<T>(source);

            var results = (TResult)mapTo.ServiceCtor.Invoke(typeof(TResult));

            results.Add(value);

            return results;
        }

        private static object GetValueByEnumarableKeyValuePair<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> valuePairs, string namingKey, string originalKey, Type conversionType, MapToExpression mapTo)
        {
            foreach (var kv in valuePairs.Where(kv => string.Equals(kv.Key.ToString(), namingKey, StringComparison.OrdinalIgnoreCase)))
            {
                if (kv.Value == null)
                {
                    return null;
                }

                return mapTo.ThrowsMap(kv.Value, conversionType);
            }

            if (!string.Equals(namingKey, originalKey, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var kv in valuePairs.Where(kv => string.Equals(kv.Key.ToString(), originalKey, StringComparison.OrdinalIgnoreCase)))
                {
                    if (kv.Value == null)
                    {
                        return null;
                    }

                    return mapTo.ThrowsMap(kv.Value, conversionType);
                }
            }

            return null;
        }

        #endregion

        /// <summary>
        /// 解决 任意类型 到 可空类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型。</typeparam>
        /// <param name="sourceType">源数据类型。</param>
        /// <param name="conversionType">目标数据类型。</param>
        /// <param name="typeArgument">泛型约束。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ToNullable<TResult>(Type sourceType, Type conversionType, Type typeArgument)
            => source => Mapper.ThrowsCast<TResult>(source);

        /// <summary>
        /// 解决 值类型 到 任意类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByValueType<TResult>(Type sourceType, Type conversionType)
            => source => Mapper.ThrowsCast<TResult>(source);

        /// <summary>
        /// 解决 任意类型 到 值类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型。</typeparam>
        /// <param name="sourceType">源数据类型。</param>
        /// <param name="conversionType">目标数据类型。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ToValueType<TResult>(Type sourceType, Type conversionType)
            => source => Mapper.ThrowsCast<TResult>(source);

        /// <summary>
        /// 解决 对象 到 任意对象 的操作。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型。</typeparam>
        /// <param name="sourceType">源数据类型。</param>
        /// <param name="conversionType">目标数据类型。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToCommon<TResult>(Type sourceType, Type conversionType)
        {
            if (conversionType.IsAbstract || conversionType.IsInterface)
                throw new InvalidCastException();

            return ByLikeObject<TResult>(sourceType, conversionType);
        }

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T1&gt; 到类似 ICollection&lt;T2&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型【T2】约束。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByIEnumarableLikeToICollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object));

            var method = typeSelf.GetMethod(nameof(ByEnumarableToList), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArgument);

            var bodyExp = Call(null, methodG, Convert(parameterExp, typeof(IEnumerable)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(Convert(bodyExp, conversionType), parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 IEnumarable&lt;T1&gt; 到 ICollection&lt;T2&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型【T2】约束。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByIEnumarableLikeToCollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object));

            var method = typeSelf.GetMethod(nameof(ByEnumarableToCollectionLike), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArgument, conversionType);

            var bodyExp = Call(null, methodG, Convert(parameterExp, typeof(IEnumerable)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 IEnumarable&lt;T&gt; 到 泛型约束类的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByIEnumarableLikeToCommon<TResult>(Type sourceType, Type conversionType)
        {
            var interfaces = sourceType.GetInterfaces();

            foreach (var type in interfaces.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                var typeArguments = type.GetGenericArguments();

                if (typeArguments.Length > 1) continue;

                var typeArgument = typeArguments.First();

                if (typeArgument.IsValueType && typeArgument.IsGenericType && typeArgument.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                    return ByEnumarableKeyValuePairToObject<TResult>(sourceType, conversionType, type, typeArgument.GetGenericArguments());
            }

            return base.ByIEnumarableLikeToCommon<TResult>(sourceType, conversionType);
        }

        /// <summary>
        ///  解决 IEnumarable&lt;T&gt; 到 泛型约束类的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="interfaceType">接口类型。</param>
        /// <param name="typeArguments">方向约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByEnumarableKeyValuePairToObject<TResult>(Type sourceType, Type conversionType, Type interfaceType, Type[] typeArguments)
        {
            var typeStore = TypeItem.Get(conversionType);

            if (typeStore.ConstructorStores.Any(x => x.ParameterStores.Count == 0))
                return ByEnumarableKeyValuePairToCommon<TResult>(typeStore, sourceType, conversionType, interfaceType, typeArguments);

            return ByEnumarableKeyValuePairToComplex<TResult>(typeStore, sourceType, conversionType, interfaceType, typeArguments);
        }

        /// <summary>
        ///  解决 IEnumarable&lt;T&gt; 到 泛型约束类的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="typeStore">类型存储。</param>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="interfaceType">接口类型。</param>
        /// <param name="typeArguments">方向约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByEnumarableKeyValuePairToCommon<TResult>(TypeItem typeStore, Type sourceType, Type conversionType, Type interfaceType, Type[] typeArguments)
        {
            var methodCtor = ServiceCtor.Method;

            Expression bodyExp = Convert(methodCtor.IsStatic
                ? Call(null, methodCtor, Constant(conversionType))
                : Call(Constant(ServiceCtor.Target), methodCtor, Constant(conversionType))
                , conversionType);

            var parameterExp = Parameter(typeof(object));

            var method = typeSelf.GetMethod(nameof(GetValueByEnumarableKeyValuePair), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArguments);

            var valueExp = Variable(interfaceType);

            var thisExp = Constant(this);

            var nullExp = Constant(null);

            var objectExp = Variable(typeof(object));

            var resultExp = Variable(conversionType);

            var list = new List<Expression>
            {
                Assign(valueExp, Convert(parameterExp, interfaceType)),

                Assign(resultExp, bodyExp)
            };

            if (Kind == PatternKind.Property || Kind == PatternKind.All)
            {
                typeStore.PropertyStores
                    .Where(x => x.CanWrite)
                    .ForEach(info =>
                    {
                        Config(info, Property(resultExp, info.Member));
                    });
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores
                    .Where(x => x.CanWrite)
                    .ForEach(info =>
                    {
                        Config(info, Field(resultExp, info.Member));
                    });
            }

            list.Add(resultExp);

            var lamdaExp = Lambda<Func<object, TResult>>(Block(new ParameterExpression[] { valueExp, objectExp, resultExp }, list), parameterExp);

            return lamdaExp.Compile();

            void Config<T>(StoreItem<T> item, Expression node) where T : MemberInfo
            {
                list.Add(Assign(objectExp, Call(null, methodG, valueExp, Constant(item.Naming), Constant(item.Name), Constant(item.MemberType), thisExp)));

                list.Add(IfThen(NotEqual(objectExp, nullExp), Assign(node, Convert(objectExp, item.MemberType))));
            }
        }

        /// <summary>
        ///  解决 IEnumarable&lt;T&gt; 到 泛型约束类的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="typeStore">类型存储。</param>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="interfaceType">接口类型。</param>
        /// <param name="typeArguments">方向约束。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByEnumarableKeyValuePairToComplex<TResult>(TypeItem typeStore, Type sourceType, Type conversionType, Type interfaceType, Type[] typeArguments)
        {
            var commonCtor = typeStore.ConstructorStores
             .Where(x => x.CanRead)
             .OrderBy(x => x.ParameterStores.Count)
             .FirstOrDefault() ?? throw new NotSupportedException($"“{conversionType}”没有公共构造函数!");

            var parameterExp = Parameter(typeof(object));

            var method = typeSelf.GetMethod(nameof(GetValueByEnumarableKeyValuePair), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArguments);

            var valueExp = Variable(interfaceType);

            var thisExp = Constant(this);

            var nullExp = Constant(null);

            var objectExp = Variable(typeof(object));

            var resultExp = Variable(conversionType);

            var arguments = new List<Expression>();

            var variables = new List<ParameterExpression> { valueExp, objectExp, resultExp };

            var list = new List<Expression>
            {
                Assign(valueExp, Convert(parameterExp, interfaceType))
            };

            commonCtor.ParameterStores
                .ForEach(info => ConfigParameter(info));

            list.Add(Assign(resultExp, New(commonCtor.Member, arguments)));

            if (Kind == PatternKind.Property || Kind == PatternKind.All)
            {
                typeStore.PropertyStores
                    .Where(x => x.CanWrite)
                    .ForEach(info =>
                    {
                        Config(info, Property(resultExp, info.Member));
                    });
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores
                    .Where(x => x.CanWrite)
                    .ForEach(info =>
                    {
                        Config(info, Field(resultExp, info.Member));
                    });
            }

            list.Add(resultExp);

            var lamdaExp = Lambda<Func<object, TResult>>(Block(variables, list), parameterExp);

            return lamdaExp.Compile();

            void ConfigParameter(ParameterItem parameterItem)
            {
                var memberType = parameterItem.ParameterType;

                if (memberType.IsValueType)
                {
                    if (memberType.IsNullable())
                    {
                        memberType = Nullable.GetUnderlyingType(memberType);
                    }

                    if (memberType.IsEnum)
                    {
                        memberType = Enum.GetUnderlyingType(memberType);
                    }
                }

                var variable = Variable(memberType/*, info.Name.ToCamelCase()*/);

                list.Add(Assign(objectExp, Call(null, methodG, valueExp, Constant(parameterItem.Naming), Constant(parameterItem.Name), Constant(memberType), thisExp)));

                list.Add(IfThen(NotEqual(objectExp, nullExp), Assign(variable, Convert(objectExp, parameterItem.ParameterType))));

                arguments.Add(variable);

                variables.Add(variable);
            }

            void Config<T>(StoreItem<T> item, Expression node) where T : MemberInfo
            {
                list.Add(Assign(objectExp, Call(null, methodG, valueExp, Constant(item.Naming), Constant(item.Name), Constant(item.MemberType), thisExp)));

                list.Add(IfThen(NotEqual(objectExp, nullExp), Assign(node, Convert(objectExp, item.MemberType))));
            }
        }

        /// <summary>
        /// 解决 类 到类似 IEnumarable&lt;T&gt; 类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型【T】约束。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToIEnumarableLike<TResult>(Type sourceType, Type conversionType, Type typeArgument) => ByObjectToICollectionLike<TResult>(sourceType, conversionType, typeArgument);

        /// <summary>
        /// 解决 对象 到 类似 IEnumarable&lt;KeyValuePair&lt;TKey,TValue&gt;&gt; 类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标数据类型。</param>
        /// <param name="typeArgument">泛型【KeyValuePair&lt;TKey,TValue&gt;】约束。</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToIEnumarableKeyValuePair<TResult>(Type sourceType, Type conversionType, Type typeArgument, Type[] typeArguments) => ByObjectToICollectionKeyValuePair<TResult>(sourceType, conversionType, typeArgument, typeArguments);

        /// <summary>
        /// 解决 类 到类似 ICollection&lt;T&gt; 类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型【T】约束。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToICollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object));

            var method = typeSelf.GetMethod(nameof(ByObjectToList), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArgument, conversionType);

            var bodyExp = Call(null, methodG, parameterExp, Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 对象 到类似 ICollection&lt;T&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型【T】约束。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToCollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object));

            var method = typeSelf.GetMethod(nameof(ByObjectToCollectionLike), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArgument, conversionType);

            var bodyExp = Call(null, methodG, parameterExp, Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 对象 到 类似 ICollection&lt;KeyValuePair&lt;TKey,TValue&gt;&gt; 类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型【KeyValuePair&lt;TKey,TValue&gt;】约束。</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToICollectionKeyValuePair<TResult>(Type sourceType, Type conversionType, Type typeArgument, Type[] typeArguments)
        {
            var typeStore = TypeItem.Get(sourceType);

            var list = new List<Expression>();

            var listKvType = typeof(List<>).MakeGenericType(typeArgument);

            var resultExp = Variable(listKvType);

            var targetExp = Variable(sourceType);

            var sourceExp = Parameter(typeof(object));

            var method = listKvType.GetMethod("Add", new Type[] { typeArgument });

            list.Add(Assign(targetExp, Convert(sourceExp, sourceType)));

            var methodCtor = ServiceCtor.Method;

            var bodyExp = methodCtor.IsStatic ?
                Call(null, methodCtor, Constant(listKvType)) :
                Call(Constant(ServiceCtor.Target), methodCtor, Constant(listKvType));

            list.Add(Assign(resultExp, Convert(bodyExp, listKvType)));

            var typeStore2 = TypeItem.Get(typeArgument);

            var ctorSotre = typeStore2.ConstructorStores.Where(x => x.ParameterStores.Count == 2).First();

            if (Kind == PatternKind.Property || Kind == PatternKind.All)
            {
                typeStore.PropertyStores
                    .Where(x => x.CanRead)
                    .ForEach(info =>
                    {
                        list.Add(Call(resultExp, method, New(ctorSotre.Member, ConvertTo(Constant(info.Naming), typeArguments[0]), ConvertTo(Property(targetExp, info.Member), typeArguments[1]))));
                    });
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores
                    .Where(x => x.CanRead)
                    .ForEach(info =>
                    {
                        list.Add(Call(resultExp, method, New(ctorSotre.Member, ConvertTo(Constant(info.Naming), typeArguments[0]), ConvertTo(Field(targetExp, info.Member), typeArguments[1]))));
                    });
            }

            list.Add(Convert(resultExp, conversionType));

            var lamdaExp = Lambda<Func<object, TResult>>(Block(new[] { targetExp, resultExp }, list), sourceExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 对象 到类似 ICollection&lt;KeyValuePair&lt;TKey,TValue&gt;&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArgument">泛型【KeyValuePair&lt;TKey,TValue&gt;】约束。</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToCollectionKeyValuePairLike<TResult>(Type sourceType, Type conversionType, Type typeArgument, Type[] typeArguments)
        {
            var typeStore = TypeItem.Get(sourceType);

            var list = new List<Expression>();

            var resultExp = Variable(conversionType);

            var targetExp = Variable(sourceType);

            var sourceExp = Parameter(typeof(object));

            var method = conversionType.GetMethod("Add", new Type[] { typeArgument });

            list.Add(Assign(targetExp, Convert(sourceExp, sourceType)));

            var methodCtor = ServiceCtor.Method;

            var bodyExp = methodCtor.IsStatic ?
                Call(null, methodCtor, Constant(conversionType)) :
                Call(Constant(ServiceCtor.Target), methodCtor, Constant(conversionType));

            list.Add(Assign(resultExp, Convert(bodyExp, conversionType)));

            var typeStore2 = TypeItem.Get(typeArgument);

            var ctorSotre = typeStore2.ConstructorStores.Where(x => x.ParameterStores.Count == 2).First();

            if (Kind == PatternKind.Property || Kind == PatternKind.All)
            {
                typeStore.PropertyStores
                    .Where(x => x.CanRead)
                    .ForEach(info =>
                    {
                        list.Add(Call(resultExp, method, New(ctorSotre.Member, ConvertTo(Constant(info.Naming), typeArguments[0]), ConvertTo(Property(targetExp, info.Member), typeArguments[1]))));
                    });
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores
                    .Where(x => x.CanRead)
                    .ForEach(info =>
                    {
                        list.Add(Call(resultExp, method, New(ctorSotre.Member, ConvertTo(Constant(info.Naming), typeArguments[0]), ConvertTo(Field(targetExp, info.Member), typeArguments[1]))));
                    });
            }

            list.Add(resultExp);

            var lamdaExp = Lambda<Func<object, TResult>>(Block(new[] { targetExp, resultExp }, list), sourceExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 对象 到类似 IDictionary&lt;TKey,TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToIDictionaryLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments)
        {
            var typeStore = TypeItem.Get(sourceType);

            var list = new List<Expression>();

            var dicType = typeof(Dictionary<,>).MakeGenericType(typeArguments);

            var resultExp = Variable(dicType);

            var targetExp = Variable(sourceType);

            var sourceExp = Parameter(typeof(object));

            var method = dicType.GetMethod("Add", typeArguments);

            list.Add(Assign(targetExp, Convert(sourceExp, sourceType)));

            var methodCtor = ServiceCtor.Method;

            var bodyExp = methodCtor.IsStatic ?
                Call(null, methodCtor, Constant(dicType)) :
                Call(Constant(ServiceCtor.Target), methodCtor, Constant(dicType));

            list.Add(Assign(resultExp, Convert(bodyExp, dicType)));

            if (Kind == PatternKind.Property || Kind == PatternKind.All)
            {
                typeStore.PropertyStores
                    .Where(x => x.CanRead)
                    .ForEach(info =>
                    {
                        list.Add(Call(resultExp, method, ConvertTo(Constant(info.Naming), typeArguments[0]), ConvertTo(Property(targetExp, info.Member), typeArguments[1])));
                    });
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores
                    .Where(x => x.CanRead)
                    .ForEach(info =>
                    {
                        list.Add(Call(resultExp, method, ConvertTo(Constant(info.Naming), typeArguments[0]), ConvertTo(Field(targetExp, info.Member), typeArguments[1])));
                    });
            }

            list.Add(Convert(resultExp, conversionType));

            var lamdaExp = Lambda<Func<object, TResult>>(Block(new[] { targetExp, resultExp }, list), sourceExp);

            return lamdaExp.Compile();
        }

        private static Expression ConvertTo(Expression node, Type type)
        {
            if (node.Type == type)
                return node;

            return Convert(node, type);
        }

        /// <summary>
        /// 解决 对象 到类似 IDictionary&lt;TKey,TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToDictionaryLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments)
        {
            var typeStore = TypeItem.Get(sourceType);

            var list = new List<Expression>();

            var resultExp = Variable(conversionType);

            var targetExp = Variable(sourceType);

            var sourceExp = Parameter(typeof(object));

            var method = conversionType.GetMethod("Add", typeArguments);

            list.Add(Assign(targetExp, Convert(sourceExp, sourceType)));

            var methodCtor = ServiceCtor.Method;

            var bodyExp = methodCtor.IsStatic ?
                Call(null, methodCtor, Constant(conversionType)) :
                Call(Constant(ServiceCtor.Target), methodCtor, Constant(conversionType));

            list.Add(Assign(resultExp, Convert(bodyExp, conversionType)));

            if (Kind == PatternKind.Property || Kind == PatternKind.All)
            {
                typeStore.PropertyStores
                    .Where(x => x.CanRead)
                    .ForEach(info =>
                    {
                        list.Add(Call(resultExp, method, ConvertTo(Constant(info.Naming), typeArguments[0]), ConvertTo(Property(targetExp, info.Member), typeArguments[1])));
                    });
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores
                    .Where(x => x.CanRead)
                    .ForEach(info =>
                    {
                        list.Add(Call(resultExp, method, ConvertTo(Constant(info.Naming), typeArguments[0]), ConvertTo(Field(targetExp, info.Member), typeArguments[1])));
                    });
            }

            list.Add(resultExp);

            var lamdaExp = Lambda<Func<object, TResult>>(Block(new[] { targetExp, resultExp }, list), sourceExp);

            return lamdaExp.Compile();
        }
    }
}
