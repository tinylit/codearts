using CodeArts.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace CodeArts.Implements
{
    /// <summary>
    /// 数据映射
    /// </summary>
    public class MapToExpression : CopyToExpression<MapToExpression>, IMapToExpression, IProfileConfiguration, IProfile
    {
        private static readonly Type typeSelf = typeof(MapToExpression);
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Dictionary<Type, MethodInfo>> TypeMap = new System.Collections.Concurrent.ConcurrentDictionary<Type, Dictionary<Type, MethodInfo>>();

        /// <summary>
        /// 构造函数
        /// </summary>
        public MapToExpression() { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="profile">配置</param>
        public MapToExpression(IProfileConfiguration profile) : base(profile) { }

        /// <summary>
        /// 对象映射
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="obj">数据源</param>
        /// <param name="def">默认值</param>
        /// <returns></returns>
        public T Map<T>(object obj, T def = default)
        {
            if (obj == null) return def;

            try
            {
                var value = UnsafeMapTo<T>(obj);

                if (value == null)
                    return def;

                return value;
            }
            catch (InvalidCastException)
            {
                return def;
            }
        }

        /// <summary>
        /// 不安全的映射（有异常）
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="source">数据源</param>
        /// <returns></returns>
        private T UnsafeMapTo<T>(object source)
        {
            var invoke = Create<T>(source.GetType());

            return invoke.Invoke(source);
        }

        /// <summary>
        /// 对象映射
        /// </summary>
        /// <param name="source">数据源</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        public object Map(object source, Type conversionType)
        {
            if (source == null) return null;

            try
            {
                return UnsafeMapTo(source, conversionType);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 不安全的映射（有异常）
        /// </summary>
        /// <param name="source">数据源</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        private object UnsafeMapTo(object source, Type conversionType)
        {
            var invoke = Create(source.GetType(), conversionType);

            return invoke.Invoke(source);
        }

        #region 反射使用

        private static Dictionary<int, string> GetKeyWithFields(IDataRecord dataRecord)
        {
            var dic = new Dictionary<int, string>();

            for (int i = 0; i < dataRecord.FieldCount; i++)
            {
                var name = dataRecord.GetName(i);

                dic.Add(i, name.ToLower());
            }

            return dic;
        }

        private static Dictionary<int, object> GetKeyWithValues(IDataRecord dataRecord)
        {
            var dic = new Dictionary<int, object>();

            for (int i = 0; i < dataRecord.FieldCount; i++)
            {
                if (dataRecord.IsDBNull(i))
                    continue;

                dic.Add(i, dataRecord.GetValue(i));
            }

            return dic;
        }

        private static int GetOrdinal(Dictionary<int, string> names, string name)
        {
            name = name.ToLower();

            foreach (var kv in names)
            {
                if (name == kv.Value)
                    return kv.Key;
            }

            return -1;
        }

        private static bool IsDbNull(Dictionary<int, object> values, int index)
        {
            return !values.TryGetValue(index, out object value) || value is null;
        }

        private static object GetValue(Dictionary<int, object> values, int index)
        {
            return values.TryGetValue(index, out object value) ? value : default;
        }

        private static MethodInfo GetMethodInfo<T1, T2, T3>(Func<T1, T2, T3> func) => func.Method;

        private static bool EqaulsString(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private static Dictionary<string, TValue> ByDataRowToDictionaryKeyIsString<TValue>(DataRow dr, MapToExpression mapTo)
        => ByDataRowToDictionaryKeyIsStringLike<TValue, Dictionary<string, TValue>>(dr, mapTo);

        private static TResult ByDataRowToDictionaryKeyIsStringLike<TValue, TResult>(DataRow dr, MapToExpression mapTo) where TResult : IDictionary<string, TValue>
        {
            var dic = (TResult)mapTo.ServiceCtor.Invoke(typeof(TResult));

            foreach (DataColumn item in dr.Table.Columns)
            {
                dic.Add(item.ColumnName, (TValue)dr[item.Ordinal]);
            }

            return dic;
        }

        private static TResult ByDataRowToCollectionKeyValuePairKeyIsStringLike<TValue, TResult>(DataRow dr, MapToExpression mapTo) where TResult : ICollection<KeyValuePair<string, TValue>>
        {
            var results = (TResult)mapTo.ServiceCtor.Invoke(typeof(TResult));

            foreach (DataColumn item in dr.Table.Columns)
            {
                results.Add(new KeyValuePair<string, TValue>(item.ColumnName, (TValue)dr[item.Ordinal]));
            }

            return results;
        }


        private static Dictionary<string, TValue> ByIDataRecordToDictionaryKeyIsString<TValue>(IDataRecord dr, MapToExpression mapTo)
        => ByIDataRecordToDictionaryKeyIsStringLike<TValue, Dictionary<string, TValue>>(dr, mapTo);

        private static TResult ByIDataRecordToDictionaryKeyIsStringLike<TValue, TResult>(IDataRecord dr, MapToExpression mapTo) where TResult : IDictionary<string, TValue>
        {
            var dic = (TResult)mapTo.ServiceCtor.Invoke(typeof(TResult));

            for (int i = 0; i < dr.FieldCount; i++)
            {
                string name = dr.GetName(i);

                if (dr.IsDBNull(i))
                {
                    if (mapTo.AllowNullPropagationMapping.Value)
                    {
                        dic.Add(name, default);
                    }
                    continue;
                }

                dic.Add(name, (TValue)dr.GetValue(i));
            }

            return dic;
        }

        private static TResult ByIDataRecordToCollectionKeyValuePairKeyIsStringLike<TValue, TResult>(IDataRecord dr, MapToExpression mapTo) where TResult : ICollection<KeyValuePair<string, TValue>>
        {
            var results = (TResult)mapTo.ServiceCtor.Invoke(typeof(TResult));

            for (int i = 0; i < dr.FieldCount; i++)
            {
                string name = dr.GetName(i);

                if (dr.IsDBNull(i))
                {
                    if (mapTo.AllowNullPropagationMapping.Value)
                    {
                        results.Add(new KeyValuePair<string, TValue>(name, default));
                    }
                    continue;
                }

                results.Add(new KeyValuePair<string, TValue>(name, (TValue)dr.GetValue(i)));
            }

            return results;
        }

        private static List<T> ByDataTableToList<T>(DataTable table, MapToExpression mapTo) => ByDataTableToCollectionLike<T, List<T>>(table, mapTo);

        private static TResult ByDataTableToCollectionLike<T, TResult>(DataTable table, MapToExpression mapTo) where TResult : ICollection<T>
        {
            var results = (TResult)mapTo.ServiceCtor.Invoke(typeof(TResult));

            foreach (var item in table.Rows)
            {
                results.Add(mapTo.UnsafeMapTo<T>(item));
            }

            return results;
        }

        private static List<T> ByEnumarableToList<T>(IEnumerable source, MapToExpression mapTo)
            => ByEnumarableToCollectionLike<T, List<T>>(source, mapTo);

        private static TResult ByEnumarableToCollectionLike<T, TResult>(IEnumerable source, MapToExpression mapTo) where TResult : ICollection<T>
        {
            var results = (TResult)mapTo.ServiceCtor.Invoke(typeof(TResult));

            foreach (var item in source)
            {
                results.Add(mapTo.UnsafeMapTo<T>(item));
            }

            return results;
        }

        private static List<T> ByObjectToList<T>(object source, MapToExpression mapTo)
        => ByObjectToCollectionLike<T, List<T>>(source, mapTo);

        private static TResult ByObjectToCollectionLike<T, TResult>(object source, MapToExpression mapTo) where TResult : ICollection<T>
        {
            var value = mapTo.UnsafeMapTo<T>(source);

            var results = (TResult)mapTo.ServiceCtor.Invoke(typeof(TResult));

            results.Add(value);

            return results;
        }

        private static object GetValueByEnumarableKeyValuePair<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> valuePairs, string key, Type conversionType, MapToExpression mapTo)
        {
            foreach (var kv in valuePairs.Where(kv => string.Equals(kv.Key.ToString(), key, StringComparison.OrdinalIgnoreCase)))
            {
                if (kv.Value == null)
                {
                    return null;
                }

                return mapTo.UnsafeMapTo(kv.Value, conversionType);
            }

            return null;
        }

        #endregion

        /// <summary>
        /// 创建表达式
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <returns></returns>
        protected override Func<object, TResult> CreateExpression<TResult>(Type sourceType)
        {
            if (typeof(IDataRecord).IsAssignableFrom(sourceType))
            {
                return ByIDataRecord<TResult>(sourceType, typeof(TResult));
            }

            return base.CreateExpression<TResult>(sourceType);
        }

        /// <summary>
        /// 解决 任意类型 到 可空类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型约束</param>
        /// <returns></returns>
        protected override Func<object, TResult> ToNullable<TResult>(Type sourceType, Type conversionType, Type typeArgument)
            => source => source.CastTo<TResult>();

        /// <summary>
        /// 解决 值类型 到 任意类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByValueType<TResult>(Type sourceType, Type conversionType)
            => source => source.CastTo<TResult>();

        /// <summary>
        /// 解决 任意类型 到 值类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <returns></returns>
        protected override Func<object, TResult> ToValueType<TResult>(Type sourceType, Type conversionType)
            => source => source.CastTo<TResult>();

        /// <summary>
        /// 解决 对象 到 任意对象 的操作，
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToCommon<TResult>(Type sourceType, Type conversionType)
        {
            if (sourceType == typeof(DataSet))
                throw new InvalidCastException();

            if (sourceType == typeof(DataTable))
            {
                return ByDataTable<TResult>(sourceType, conversionType);
            }

            if (sourceType == typeof(DataRow))
            {
                return ByDataRow<TResult>(sourceType, conversionType);
            }

            if (conversionType.IsAbstract || conversionType.IsInterface)
                throw new InvalidCastException();

            return ByLikeObject<TResult>(sourceType, conversionType);
        }

        #region Table
        /// <summary>
        /// 解决 DataTable 到 目标类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByDataTable<TResult>(Type sourceType, Type conversionType)
        {
            if (conversionType.IsValueType || conversionType == typeof(string))
            {
                return source =>
                {
                    if (source is DataTable dt)
                    {
                        foreach (var dr in dt.Rows)
                        {
                            return UnsafeMapTo<TResult>(dr);
                        }
                    }

                    return default;
                };
            }

            if (typeof(IEnumerable).IsAssignableFrom(conversionType))
            {
                return conversionType.IsInterface
                    ? ByDataTableToIEnumarableLike<TResult>(sourceType, conversionType)
                    : ByDataTableToEnumarableLike<TResult>(sourceType, conversionType);
            }

            return source =>
            {
                if (source is DataTable dt)
                {
                    foreach (var dr in dt.Rows)
                    {
                        return UnsafeMapTo<TResult>(dr);
                    }
                }

                return default;
            };
        }

        /// <summary>
        /// 解决 DataTable 到 IEnumarable 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByDataTableToIEnumarableLike<TResult>(Type sourceType, Type conversionType)
        {
            if (conversionType.IsGenericType)
            {
                var typeDefinition = conversionType.GetGenericTypeDefinition();

                if (typeDefinition == typeof(IEnumerable<>) || typeDefinition == typeof(ICollection<>) || typeDefinition == typeof(IList<>))
                    return ByDataTableToList<TResult>(sourceType, conversionType, conversionType.GetGenericArguments().First());

#if !NET40
                if (typeDefinition == typeof(IReadOnlyCollection<>) || typeDefinition == typeof(IReadOnlyList<>))
                    return ByDataTableToList<TResult>(sourceType, conversionType, conversionType.GetGenericArguments().First());
#endif

                return ByDataTableToUnknownInterface<TResult>(sourceType, conversionType, conversionType.GetGenericArguments());
            }

            return ByDataTableToUnknownInterface<TResult>(sourceType, conversionType);
        }

        /// <summary>
        /// 解决 DataTable 都 未知接口的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByDataTableToUnknownInterface<TResult>(Type sourceType, Type conversionType) => throw new InvalidCastException();

        /// <summary>
        /// 解决 DataTable 到 未知泛型类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArguments">泛型约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByDataTableToUnknownInterface<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => throw new InvalidCastException();

        /// <summary>
        /// 解决 DataTable 到 类似 IEnumarable&lt;T&gt; 的 转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArgument">泛型约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByDataTableToIEnumarableLike<TResult>(Type sourceType, Type conversionType, Type typeArgument) => ByDataTableToICollectionLike<TResult>(sourceType, conversionType, typeArgument);

        /// <summary>
        /// 解决 DataTable 到 类似 ICollection&lt;T&gt; 的 转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArgument">泛型约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByDataTableToICollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(ByDataTableToList), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArgument);

            var bodyExp = Call(null, methodG, Convert(parameterExp, sourceType), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(Convert(bodyExp, conversionType), parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 DataTable 到 IEnumarable 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标蕾西</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标蕾西</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByDataTableToEnumarableLike<TResult>(Type sourceType, Type conversionType)
        {
            if (conversionType.IsAbstract)
            {
                return ByDataTableToEnumarableAbstract<TResult>(sourceType, conversionType);
            }

            var interfaces = conversionType.GetInterfaces();

            foreach (var item in interfaces.Where(x => x.IsGenericType))
            {
                var typeArguments = item.GetGenericArguments();
                var typeDefinition = item.GetGenericTypeDefinition();

                if (typeDefinition == typeof(ICollection<>))
                {
                    var typeArgument = typeArguments.First();

                    if (typeArgument.IsClass || typeArgument.IsValueType || typeof(IEnumerable).IsAssignableFrom(typeArgument))
                    {
                        return ByDataTableToCollectionLike<TResult>(sourceType, conversionType, typeArgument);
                    }
                }
            }

            return source =>
            {
                if (source is DataTable dt)
                {
                    foreach (var dr in dt.Rows)
                    {
                        return UnsafeMapTo<TResult>(dr);
                    }
                }

                return default;
            };
        }

        /// <summary>
        /// 解决 DataTable 到 类似 IEnumarable 抽象类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByDataTableToEnumarableAbstract<TResult>(Type sourceType, Type conversionType) => throw new InvalidCastException();

        /// <summary>
        /// 解决 DataTable 到 类似 ICollection&lt;T&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArgument">泛型【T】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByDataTableToCollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(ByDataTableToCollectionLike), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArgument, conversionType);

            var bodyExp = Call(null, methodG, Convert(parameterExp, sourceType), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 DataTable 到 List&lt;T&gt; 继承接口的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArgument">泛型【T】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByDataTableToList<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(ByDataTableToList), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArgument);

            var bodyExp = Call(null, methodG, Convert(parameterExp, sourceType), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(Convert(bodyExp, conversionType), parameterExp);

            return lamdaExp.Compile();
        }
        #endregion

        #region DataRow
        /// <summary>
        /// 解决 DataRow 到 目标类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByDataRow<TResult>(Type sourceType, Type conversionType)
        {
            if (conversionType.IsValueType || conversionType == typeof(string))
            {
                return source =>
                {
                    if (source is DataRow dr)
                    {
                        if (!dr.IsNull(0))
                        {
                            return (TResult)System.Convert.ChangeType(dr[0], conversionType);
                        }
                    }

                    return default;
                };
            }

            if (typeof(IEnumerable).IsAssignableFrom(conversionType))
            {
                return conversionType.IsInterface
                    ? ByDataRowToIEnumarable<TResult>(sourceType, conversionType)
                    : ByDataRowToEnumarable<TResult>(sourceType, conversionType);
            }

            return ByDataRowToObject<TResult>(sourceType, conversionType);
        }

        /// <summary>
        /// 解决 DataRow 到类似 IEnumarable 的接口转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByDataRowToIEnumarable<TResult>(Type sourceType, Type conversionType)
        {
            if (conversionType.IsGenericType)
            {
                var typeDefinition = conversionType.GetGenericTypeDefinition();

                var typeArguments = conversionType.GetGenericArguments();

                if (typeDefinition == typeof(IDictionary<,>))
                {
                    return ByDataRowToDictionary<TResult>(sourceType, conversionType, typeArguments);
                }

#if !NET40
                if (typeDefinition == typeof(IReadOnlyDictionary<,>))
                {
                    return ByDataRowToDictionary<TResult>(sourceType, conversionType, typeArguments);
                }
#endif

                if (typeDefinition == typeof(IEnumerable<>) || typeDefinition == typeof(ICollection<>) || typeDefinition == typeof(IList<>))
                {
                    var typeArgument = typeArguments.First();

                    if (typeArgument.IsGenericType && typeArgument.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                    {
                        return ByDataRowToDictionary<TResult>(sourceType, conversionType, typeArgument.GetGenericArguments());
                    }
                }

#if !NET40
                else if (typeDefinition == typeof(IReadOnlyCollection<>) || typeDefinition == typeof(IReadOnlyList<>))
                {
                    var typeArgument = typeArguments.First();

                    if (typeArgument.IsGenericType && typeArgument.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                    {
                        return ByDataRowToDictionary<TResult>(sourceType, conversionType, typeArgument.GetGenericArguments());
                    }
                }
#endif
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// 解决 DataRow 到类似 IEnumarable 的类转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByDataRowToEnumarable<TResult>(Type sourceType, Type conversionType)
        {
            var interfaces = conversionType.GetInterfaces();

            foreach (var item in interfaces.Where(x => x.IsGenericType))
            {
                var type = item.GetGenericTypeDefinition();

                if (type == typeof(IDictionary<,>))
                {
                    return ByDataRowToDictionaryLike<TResult>(sourceType, conversionType, item.GetGenericArguments());
                }

                if (type == typeof(ICollection<>))
                {
                    var typeArguments = item.GetGenericArguments();

                    var typeArgument = typeArguments.First();

                    if (typeArgument.IsGenericType && typeArgument.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                    {
                        return ByDataRowToCollectionKeyValuePairLike<TResult>(sourceType, conversionType, typeArgument.GetGenericArguments());
                    }
                }
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// 解决 DataRow 到类似 ICollection&lt;KeyValuePair&lt;TKey,TValue&gt;&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByDataRowToCollectionKeyValuePairLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments)
        {
            if (typeArguments[0] == typeof(string))
            {
                return ByDataRowToCollectionKeyValuePairKeyIsStringLike<TResult>(sourceType, conversionType, typeArguments[1]);
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// 解决 DataRow 到类似 ICollection&lt;KeyValuePair&lt;<see cref="string"/>,TValue&gt;&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArgument">泛型【TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByDataRowToCollectionKeyValuePairKeyIsStringLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(ByDataRowToCollectionKeyValuePairKeyIsStringLike), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(new Type[] { typeArgument, conversionType });

            var bodyExp = Call(null, methodG, Convert(parameterExp, sourceType), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 DataRow 到类似 IDictionary&lt;TKey,TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByDataRowToDictionaryLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments)
        {
            if (typeArguments[0] == typeof(string))
            {
                return ByDataRowToDictionaryKeyIsStringLike<TResult>(sourceType, conversionType, typeArguments[1]);
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// 解决 DataRow 到类似 IDictionary&lt;<see cref="string"/>,TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArgument">泛型【TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByDataRowToDictionaryKeyIsStringLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(ByDataRowToDictionaryKeyIsStringLike), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(new Type[] { typeArgument, conversionType });

            var bodyExp = Call(null, methodG, Convert(parameterExp, sourceType), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 DataRow 到 Dictionary&lt;TKey,TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByDataRowToDictionary<TResult>(Type sourceType, Type conversionType, Type[] typeArguments)
        {
            if (typeArguments[0] == typeof(string))
            {
                return ByDataRowToDictionaryKeyIsString<TResult>(sourceType, conversionType, typeArguments[1]);
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// 解决 DataRow 到 Dictionary&lt;<see cref="string"/>,TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArgument">泛型【TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByDataRowToDictionaryKeyIsString<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(ByDataRowToDictionaryKeyIsString), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArgument);

            var bodyExp = Call(null, methodG, Convert(parameterExp, sourceType), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(Convert(bodyExp, conversionType), parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 DataRow 到 对象 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByDataRowToObject<TResult>(Type sourceType, Type conversionType)
        {
            var list = new List<SwitchCase>();

            var resultExp = Parameter(conversionType, "result");

            var nameExp = Parameter(typeof(string), "name");

            var valueExp = Parameter(typeof(object), "value");

            var typeStore = RuntimeTypeCache.Instance.GetCache(conversionType);

            if (Kind == PatternKind.Property || Kind == PatternKind.All)
            {
                typeStore.PropertyStores
                    .Where(x => x.CanWrite)
                    .ForEach(info =>
                    {
                        var testValues = new List<Expression> { Constant(info.Name) };

                        if (!string.Equals(info.Name, info.Naming, StringComparison.OrdinalIgnoreCase))
                        {
                            testValues.Add(Constant(info.Naming));
                        }

                        list.Add(SwitchCase(Assign(Property(resultExp, info.Member), Convert(valueExp, info.MemberType)), testValues));
                    });
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores
                    .Where(x => x.CanWrite)
                    .ForEach(info =>
                    {
                        var testValues = new List<Expression> { Constant(info.Name) };

                        if (!string.Equals(info.Name, info.Naming, StringComparison.OrdinalIgnoreCase))
                        {
                            testValues.Add(Constant(info.Naming));
                        }

                        list.Add(SwitchCase(Assign(Field(resultExp, info.Member), Convert(valueExp, info.MemberType)), testValues));
                    });
            }

            var bodyExp = Switch(nameExp, null, GetMethodInfo<string, string, bool>(EqaulsString), list.ToArray());

            var lamdaExp = Lambda<Action<TResult, string, object>>(bodyExp, resultExp, nameExp, valueExp);

            var invoke = lamdaExp.Compile();

            return source =>
            {
                if (source is DataRow dr)
                {
                    var result = (TResult)ServiceCtor.Invoke(conversionType);

                    foreach (DataColumn item in dr.Table.Columns)
                    {
                        invoke.Invoke(result, item.ColumnName, dr[item]);
                    }

                    return result;
                }

                return default;
            };
        }
        #endregion

        #region IDataRecord

        /// <summary>
        /// 解决 IDataRecord 到 目标类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIDataRecord<TResult>(Type sourceType, Type conversionType)
        {
            if (conversionType.IsValueType || conversionType == typeof(string))
            {
                return ByIDataRecordToValueTypeOrString<TResult>(sourceType, conversionType);
            }

            if (typeof(IEnumerable).IsAssignableFrom(conversionType))
            {
                return conversionType.IsInterface
                    ? ByIDataRecordToIEnumarable<TResult>(sourceType, conversionType)
                    : ByIDataRecordToEnumarable<TResult>(sourceType, conversionType);
            }

            return ByIDataRecordToObject<TResult>(sourceType, conversionType);
        }

        /// <summary>
        /// 解决 IDataRecord 到类似 IEnumarable 的接口转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIDataRecordToIEnumarable<TResult>(Type sourceType, Type conversionType)
        {
            if (conversionType.IsGenericType)
            {
                var typeDefinition = conversionType.GetGenericTypeDefinition();

                var typeArguments = conversionType.GetGenericArguments();

                if (typeDefinition == typeof(IDictionary<,>))
                {
                    return ByIDataRecordToDictionary<TResult>(sourceType, conversionType, typeArguments);
                }

#if !NET40
                if (typeDefinition == typeof(IReadOnlyDictionary<,>))
                {
                    return ByIDataRecordToDictionary<TResult>(sourceType, conversionType, typeArguments);
                }
#endif

                if (typeDefinition == typeof(IEnumerable<>) || typeDefinition == typeof(ICollection<>) || typeDefinition == typeof(IList<>))
                {
                    var typeArgument = typeArguments.First();

                    if (typeArgument.IsGenericType && typeArgument.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                    {
                        return ByIDataRecordToDictionary<TResult>(sourceType, conversionType, typeArgument.GetGenericArguments());
                    }
                }

#if !NET40
                else if (typeDefinition == typeof(IReadOnlyCollection<>) || typeDefinition == typeof(IReadOnlyList<>))
                {
                    var typeArgument = typeArguments.First();

                    if (typeArgument.IsGenericType && typeArgument.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                    {
                        return ByIDataRecordToDictionary<TResult>(sourceType, conversionType, typeArgument.GetGenericArguments());
                    }
                }
#endif
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// 解决 IDataRecord 到类似 IEnumarable 的类转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIDataRecordToEnumarable<TResult>(Type sourceType, Type conversionType)
        {
            var interfaces = conversionType.GetInterfaces();

            foreach (var item in interfaces.Where(x => x.IsGenericType))
            {
                var type = item.GetGenericTypeDefinition();

                if (type == typeof(IDictionary<,>))
                {
                    return ByIDataRecordToDictionaryLike<TResult>(sourceType, conversionType, item.GetGenericArguments());
                }

                if (type == typeof(ICollection<>))
                {
                    var typeArguments = item.GetGenericArguments();

                    var typeArgument = typeArguments.First();

                    if (typeArgument.IsGenericType && typeArgument.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                    {
                        return ByIDataRecordToCollectionKeyValuePairLike<TResult>(sourceType, conversionType, typeArgument.GetGenericArguments());
                    }
                }
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// 解决 IDataRecord 到 Dictionary&lt;TKey,TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIDataRecordToDictionary<TResult>(Type sourceType, Type conversionType, Type[] typeArguments)
        {
            if (typeArguments[0] == typeof(string))
            {
                return ByIDataRecordToDictionaryKeyIsString<TResult>(sourceType, conversionType, typeArguments[1]);
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// 解决 IDataRecord 到 Dictionary&lt;<see cref="string"/>,TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArgument">泛型【TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIDataRecordToDictionaryKeyIsString<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(ByIDataRecordToDictionaryKeyIsString), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArgument);

            var bodyExp = Call(null, methodG, Convert(parameterExp, sourceType), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(Convert(bodyExp, conversionType), parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 IDataRecord 到类似 IDictionary&lt;TKey,TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIDataRecordToDictionaryLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments)
        {
            if (typeArguments[0] == typeof(string))
            {
                return ByIDataRecordToDictionaryKeyIsStringLike<TResult>(sourceType, conversionType, typeArguments[1]);
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// 解决 IDataRecord 到类似 IDictionary&lt;<see cref="string"/>,TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArgument">泛型【TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIDataRecordToDictionaryKeyIsStringLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(ByIDataRecordToDictionaryKeyIsStringLike), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(new Type[] { typeArgument, conversionType });

            var bodyExp = Call(null, methodG, Convert(parameterExp, sourceType), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 IDataRecord 到类似 ICollection&lt;KeyValuePair&lt;TKey,TValue&gt;&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIDataRecordToCollectionKeyValuePairLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments)
        {
            if (typeArguments[0] == typeof(string))
            {
                return ByIDataRecordToCollectionKeyValuePairKeyIsStringLike<TResult>(sourceType, conversionType, typeArguments[1]);
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// 解决 IDataRecord 到类似 ICollection&lt;KeyValuePair&lt;<see cref="string"/>,TValue&gt;&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArgument">泛型【TValue】约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIDataRecordToCollectionKeyValuePairKeyIsStringLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(ByIDataRecordToCollectionKeyValuePairKeyIsStringLike), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(new Type[] { typeArgument, conversionType });

            var bodyExp = Call(null, methodG, Convert(parameterExp, sourceType), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 IDataRecord 到 值类型或String 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIDataRecordToValueTypeOrString<TResult>(Type sourceType, Type conversionType)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var indexExp = Constant(0);

            var valueExp = Variable(sourceType, "value");

            var isDBNull = sourceType.GetMethod("IsDBNull", new Type[] { typeof(int) });

            var takeFieldType = sourceType.GetMethod("GetFieldType", new Type[] { typeof(int) });

            var convertMethod = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });

            var typeMap = TypeMap.GetOrAdd(sourceType, type =>
            {
                var types = new Type[1] { typeof(int) };

                return new Dictionary<Type, MethodInfo>
                {
                    [typeof(bool)] = type.GetMethod("GetBoolean", types),
                    [typeof(byte)] = type.GetMethod("GetByte", types),
                    [typeof(char)] = type.GetMethod("GetChar", types),
                    [typeof(short)] = type.GetMethod("GetInt16", types),
                    [typeof(int)] = type.GetMethod("GetInt32", types),
                    [typeof(long)] = type.GetMethod("GetInt64", types),
                    [typeof(float)] = type.GetMethod("GetFloat", types),
                    [typeof(double)] = type.GetMethod("GetDouble", types),
                    [typeof(decimal)] = type.GetMethod("GetDecimal", types),
                    [typeof(Guid)] = type.GetMethod("GetGuid", types),
                    [typeof(DateTime)] = type.GetMethod("GetDateTime", types),
                    [typeof(string)] = type.GetMethod("GetString", types),
                    [typeof(object)] = type.GetMethod("GetValue", types)
                };
            });

            var list = new List<Expression>
            {
                Assign(valueExp, Convert(parameterExp, sourceType))
            };

            var originalType = conversionType;

            var testExp = Not(Call(valueExp, isDBNull, indexExp));

            if (conversionType.IsValueType)
            {
                if (conversionType.IsEnum)
                {
                    conversionType = Enum.GetUnderlyingType(conversionType);
                }
                else if (conversionType.IsNullable())
                {
                    conversionType = Nullable.GetUnderlyingType(conversionType);
                }
            }

            Expression objExp = Call(valueExp, typeMap[typeof(object)], indexExp);

            if (typeMap.TryGetValue(conversionType, out MethodInfo methodInfo))
            {
                var conversionTypeExp = Constant(conversionType);

                objExp = Condition(Equal(conversionTypeExp, Call(valueExp, takeFieldType, indexExp)), Call(valueExp, methodInfo, indexExp), Convert(Call(null, convertMethod, objExp, conversionTypeExp), conversionType));
            }
            else
            {
                objExp = Convert(objExp, conversionType);
            }

            if (originalType != conversionType)
            {
                objExp = Convert(objExp, originalType);
            }

            list.Add(Condition(testExp, objExp, Default(originalType)));

            var bodyExp = Block(new[] { valueExp }, list);

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 IDataRecord 到 复杂构造函数 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="typeStore">构造函数</param>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIDataRecordToComplex<TResult>(TypeStoreItem typeStore, Type sourceType, Type conversionType)
        {
            var commonCtor = typeStore.ConstructorStores
                 .Where(x => x.CanRead)
                 .OrderBy(x => x.ParameterStores.Count)
                 .FirstOrDefault();

            var parameterExp = Parameter(typeof(object), "source");

            var nullCst = Constant(null);

            var errorExp = Parameter(typeof(Exception), "e");

            var valueExp = Variable(sourceType, "value");

            var indexExp = Variable(typeof(int), "index");

            var targetExp = Variable(conversionType, "target");

            var negativeExp = Constant(-1);

            var dicKeys = Variable(typeof(Dictionary<int, string>), "__key_2_names");

            var dicValues = Variable(typeof(Dictionary<int, object>), "__key_2_values");

            var getNames = typeSelf.GetMethod(nameof(GetKeyWithFields), BindingFlags.NonPublic | BindingFlags.Static);

            var getValues = typeSelf.GetMethod(nameof(GetKeyWithValues), BindingFlags.NonPublic | BindingFlags.Static);

            var getOrdinal = typeSelf.GetMethod(nameof(GetOrdinal), BindingFlags.NonPublic | BindingFlags.Static);

            var getValue = typeSelf.GetMethod(nameof(GetValue), BindingFlags.NonPublic | BindingFlags.Static);

            var isDBNull = typeSelf.GetMethod(nameof(IsDbNull), BindingFlags.NonPublic | BindingFlags.Static);

            var convertMethod = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });

            var list = new List<Expression> { Assign(valueExp, Convert(parameterExp, sourceType)) };

            list.Add(Assign(dicKeys, Call(null, getNames, Convert(valueExp, typeof(IDataRecord)))));

            list.Add(Assign(dicValues, Call(null, getValues, Convert(valueExp, typeof(IDataRecord)))));

            var variables = new List<ParameterExpression> { valueExp, targetExp, indexExp, dicKeys, dicValues };

            var arguments = new List<Expression>();

            commonCtor.ParameterStores
                .ForEach(info => ConfigParameter(info));

            list.Add(Assign(targetExp, New(commonCtor.Member, arguments)));

            if (Kind == PatternKind.Property || Kind == PatternKind.All)
            {
                typeStore.PropertyStores
                    .Where(x => x.CanWrite && !commonCtor.ParameterStores.Any(y => y.Name == x.Name))
                    .ForEach(info => Config(info, Property(targetExp, info.Member)));
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores
                    .Where(x => x.CanWrite && !commonCtor.ParameterStores.Any(y => y.Name == x.Name))
                    .ForEach(info => Config(info, Field(targetExp, info.Member)));
            }

            list.Add(targetExp);

            var lamdaExp = Lambda<Func<object, TResult>>(Block(variables, list), parameterExp);

            return lamdaExp.Compile();

            void Config<T>(StoreItem<T> info, Expression left) where T : MemberInfo
            {
                var memberType = info.MemberType;

                list.Add(Assign(indexExp, Call(null, getOrdinal, dicKeys, Constant(info.Name))));

                if (!string.Equals(info.Name, info.Naming, StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(IfThen(Equal(indexExp, negativeExp), Assign(indexExp, Call(null, getOrdinal, dicKeys, Constant(info.Naming)))));
                }

                if (memberType.IsValueType)
                {
                    if (memberType.IsEnum)
                    {
                        memberType = Enum.GetUnderlyingType(memberType);
                    }
                    else if (memberType.IsNullable())
                    {
                        memberType = Nullable.GetUnderlyingType(memberType);
                    }
                }

                Expression valExp = Call(null, getValue, dicValues, indexExp);

                Expression objExp = TryCatch(Convert(valExp, memberType), Catch(errorExp, Convert(Call(null, convertMethod, valExp, Constant(memberType)), memberType)));

                if (AllowNullPropagationMapping.Value)
                {
                    list.Add(IfThen(GreaterThan(indexExp, negativeExp), IfThenElse(Call(null, isDBNull, dicValues, indexExp), Assign(left, Default(info.MemberType)), memberType == info.MemberType ? Assign(left, objExp) : Assign(left, Convert(objExp, info.MemberType)))));
                }
                else
                {
                    list.Add(IfThen(AndAlso(GreaterThan(indexExp, negativeExp), Not(Call(null, isDBNull, dicValues, indexExp))), memberType == info.MemberType ? Assign(left, objExp) : Assign(left, Convert(objExp, info.MemberType))));
                }
            }

            void ConfigParameter(ParameterStoreItem info)
            {
                var memberType = info.ParameterType;

                list.Add(Assign(indexExp, Call(null, getOrdinal, dicKeys, Constant(info.Name))));

                if (!string.Equals(info.Name, info.Naming, StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(IfThen(Equal(indexExp, negativeExp), Assign(indexExp, Call(null, getOrdinal, dicKeys, Constant(info.Naming)))));
                }

                if (memberType.IsValueType)
                {
                    if (memberType.IsEnum)
                    {
                        memberType = Enum.GetUnderlyingType(memberType);
                    }
                    else if (memberType.IsNullable())
                    {
                        memberType = Nullable.GetUnderlyingType(memberType);
                    }
                }

                var nameExp = Variable(info.ParameterType, info.Name.ToCamelCase());

                Expression valExp = Call(null, getValue, dicValues, indexExp);

                Expression objExp = TryCatch(Convert(valExp, memberType), Catch(errorExp, Convert(Call(null, convertMethod, valExp, Constant(memberType)), memberType)));

                Expression defaultExp = null;

                if (info.IsOptional)
                {
                    defaultExp = Constant(info.DefaultValue, info.ParameterType);
                }
                else if (!AllowNullDestinationValues.Value && info.ParameterType == typeof(string))
                {
                    defaultExp = Constant(string.Empty);
                }
                else
                {
                    defaultExp = Default(info.ParameterType);
                }

                var conditionExp = AndAlso(GreaterThan(indexExp, negativeExp), Not(Call(null, isDBNull, dicValues, indexExp)));

                if (AllowNullPropagationMapping.Value)
                {
                    list.Add(IfThenElse(conditionExp, memberType == info.ParameterType ? Assign(nameExp, objExp) : Assign(nameExp, Convert(objExp, info.ParameterType)), Assign(nameExp, defaultExp)));
                }
                else
                {
                    list.Add(IfThenElse(conditionExp, memberType == info.ParameterType ? Assign(nameExp, objExp) : Assign(nameExp, Convert(objExp, info.ParameterType)), Assign(nameExp, defaultExp)));
                }

                variables.Add(nameExp);
                arguments.Add(nameExp);
            }
        }

        /// <summary>
        /// 解决 IDataRecord 到 公共无参构造函数 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="typeStore">构造函数</param>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIDataRecordToCommon<TResult>(TypeStoreItem typeStore, Type sourceType, Type conversionType)
        {
            var method = ServiceCtor.Method;

            Expression bodyExp = Convert(method.IsStatic
                ? Call(null, method, Constant(conversionType))
                : Call(Constant(ServiceCtor.Target), method, Constant(conversionType))
                , conversionType);

            var list = new List<Expression>();

            var parameterExp = Parameter(typeof(object), "source");

            var iVar = Parameter(typeof(int), "i");

            var nullCst = Constant(null);

            var valueExp = Variable(sourceType, "value");

            var targetExp = Variable(conversionType, "target");

            list.Add(Assign(valueExp, Convert(parameterExp, sourceType)));

            list.Add(Assign(targetExp, bodyExp));

            var typeMap = TypeMap.GetOrAdd(sourceType, type =>
            {
                var types = new Type[1] { typeof(int) };

                return new Dictionary<Type, MethodInfo>
                {
                    [typeof(bool)] = type.GetMethod("GetBoolean", types),
                    [typeof(byte)] = type.GetMethod("GetByte", types),
                    [typeof(char)] = type.GetMethod("GetChar", types),
                    [typeof(short)] = type.GetMethod("GetInt16", types),
                    [typeof(int)] = type.GetMethod("GetInt32", types),
                    [typeof(long)] = type.GetMethod("GetInt64", types),
                    [typeof(float)] = type.GetMethod("GetFloat", types),
                    [typeof(double)] = type.GetMethod("GetDouble", types),
                    [typeof(decimal)] = type.GetMethod("GetDecimal", types),
                    [typeof(Guid)] = type.GetMethod("GetGuid", types),
                    [typeof(DateTime)] = type.GetMethod("GetDateTime", types),
                    [typeof(string)] = type.GetMethod("GetString", types),
                    [typeof(object)] = type.GetMethod("GetValue", types)
                };
            });

            var isDBNull = sourceType.GetMethod("IsDBNull", new Type[] { typeof(int) });

            var getFieldType = sourceType.GetMethod("GetFieldType", new Type[] { typeof(int) });

            var convertMethod = typeof(Convert).GetMethod(nameof(System.Convert.ChangeType), new Type[] { typeof(object), typeof(Type) });

            var listCases = new List<SwitchCase>();

            if (Kind == PatternKind.Property || Kind == PatternKind.All)
            {
                typeStore.PropertyStores
                    .Where(x => x.CanWrite)
                    .ForEach(info => Config(info, Property(targetExp, info.Member)));
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores
                    .Where(x => x.CanWrite)
                    .ForEach(info => Config(info, Field(targetExp, info.Member)));
            }

            #region for

            var lenVar = Property(valueExp, "FieldCount");

            var getName = sourceType.GetMethod("GetName", new Type[] { typeof(int) });

            var body = Switch(Call(valueExp, getName, iVar), null, GetMethodInfo<string, string, bool>(EqaulsString), listCases.ToArray());

            list.Add(Assign(iVar, Constant(0)));

            LabelTarget break_label = Label(typeof(void));
            LabelTarget continue_label = Label(typeof(void));

            list.Add(Loop(IfThenElse(
                             LessThan(iVar, lenVar),
                             Block(
                                 body,
                                 AddAssign(iVar, Constant(1)),
                                 Continue(continue_label, typeof(void))
                             ),
                             Break(break_label, typeof(void))
                 ), break_label, continue_label));

            #endregion

            list.Add(targetExp);

            var lamdaExp = Lambda<Func<object, TResult>>(Block(new[] { valueExp, iVar, targetExp }, list), parameterExp);

            return lamdaExp.Compile();

            void Config<T>(StoreItem<T> info, Expression left) where T : MemberInfo
            {
                var memberType = info.MemberType;

                var testValues = new List<Expression>
                {
                    Constant(info.Name)
                };

                if (!string.Equals(info.Name, info.Naming, StringComparison.OrdinalIgnoreCase))
                {
                    testValues.Add(Constant(info.Naming));
                }

                var assigns = new List<Expression>();

                if (memberType.IsValueType)
                {
                    if (memberType.IsEnum)
                    {
                        memberType = Enum.GetUnderlyingType(memberType);
                    }
                    else if (memberType.IsNullable())
                    {
                        memberType = Nullable.GetUnderlyingType(memberType);
                    }
                }

                var memberTypeCst = Constant(memberType);

                Expression objExp = Call(valueExp, typeMap[typeof(object)], iVar);

                if (typeMap.TryGetValue(memberType, out MethodInfo methodInfo))
                {
                    objExp = Condition(Equal(memberTypeCst, Call(valueExp, getFieldType, iVar)), Call(valueExp, methodInfo, iVar), Convert(Call(null, convertMethod, objExp, memberTypeCst), memberType));
                }
                else
                {
                    objExp = Convert(Call(null, convertMethod, objExp, memberTypeCst), memberType);
                }

                if (AllowNullPropagationMapping.Value)
                {
                    listCases.Add(SwitchCase(IfThenElse(Call(valueExp, isDBNull, iVar), Assign(left, Default(info.MemberType)), memberType == info.MemberType ? Assign(left, objExp) : Assign(left, Convert(objExp, info.MemberType))), testValues));
                }
                else
                {
                    listCases.Add(SwitchCase(IfThen(Not(Call(valueExp, isDBNull, iVar)), memberType == info.MemberType ? Assign(left, objExp) : Assign(left, Convert(objExp, info.MemberType))), testValues));
                }
            }
        }

        /// <summary>
        /// 解决 IDataRecord 到 对象 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIDataRecordToObject<TResult>(Type sourceType, Type conversionType)
        {
            var typeStore = RuntimeTypeCache.Instance.GetCache(conversionType);

            if (typeStore.ConstructorStores.Any(x => x.ParameterStores.Count == 0))
                return ByIDataRecordToCommon<TResult>(typeStore, sourceType, conversionType);

            return ByIDataRecordToComplex<TResult>(typeStore, sourceType, conversionType);
        }

        #endregion

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T1&gt; 到类似 ICollection&lt;T2&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型【T2】约束</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByIEnumarableLikeToICollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(ByEnumarableToList), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArgument);

            var bodyExp = Call(null, methodG, Convert(parameterExp, typeof(IEnumerable)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(Convert(bodyExp, conversionType), parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 IEnumarable&lt;T1&gt; 到 ICollection&lt;T2&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="typeArgument">泛型【T2】约束</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByIEnumarableLikeToCollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(ByEnumarableToCollectionLike), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArgument, conversionType);

            var bodyExp = Call(null, methodG, Convert(parameterExp, typeof(IEnumerable)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 IEnumarable&lt;T&gt; 到 泛型约束类的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
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
                    return ByEnumarableKeyValuePairToCommon<TResult>(sourceType, conversionType, type, typeArgument.GetGenericArguments());
            }

            return base.ByIEnumarableLikeToCommon<TResult>(sourceType, conversionType);
        }

        /// <summary>
        ///  解决 IEnumarable&lt;T&gt; 到 泛型约束类的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="interfaceType">接口类型</param>
        /// <param name="typeArguments">方向约束</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByEnumarableKeyValuePairToCommon<TResult>(Type sourceType, Type conversionType, Type interfaceType, Type[] typeArguments)
        {
            var methodCtor = ServiceCtor.Method;

            Expression bodyExp = Convert(methodCtor.IsStatic
                ? Call(null, methodCtor, Constant(conversionType))
                : Call(Constant(ServiceCtor.Target), methodCtor, Constant(conversionType))
                , conversionType);

            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(GetValueByEnumarableKeyValuePair), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArguments);

            var valueExp = Variable(interfaceType, "value");

            var thisExp = Constant(this);

            var nullExp = Constant(null);

            var maptoExp = Variable(typeof(object), "mapto");

            var resultExp = Variable(conversionType, "target");

            var list = new List<Expression>
            {
                Assign(valueExp, Convert(parameterExp, interfaceType)),

                Assign(resultExp, bodyExp)
            };

            var typeStore = RuntimeTypeCache.Instance.GetCache(conversionType);

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

            var lamdaExp = Lambda<Func<object, TResult>>(Block(new ParameterExpression[] { valueExp, maptoExp, resultExp }, list), parameterExp);

            return lamdaExp.Compile();

            void Config<T>(StoreItem<T> item, Expression node) where T : MemberInfo
            {
                list.Add(Assign(maptoExp, Call(null, methodG, valueExp, Constant(item.Naming), Constant(item.MemberType), thisExp)));

                list.Add(IfThen(NotEqual(maptoExp, nullExp), Assign(node, Convert(maptoExp, item.MemberType))));
            }
        }

        /// <summary>
        /// 解决 类 到类似 IEnumarable&lt;T&gt; 类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型【T】约束</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToIEnumarableLike<TResult>(Type sourceType, Type conversionType, Type typeArgument) => ByObjectToICollectionLike<TResult>(sourceType, conversionType, typeArgument);

        /// <summary>
        /// 解决 对象 到 类似 IEnumarable&lt;KeyValuePair&lt;TKey,TValue&gt;&gt; 类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型【KeyValuePair&lt;TKey,TValue&gt;】约束</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToIEnumarableKeyValuePair<TResult>(Type sourceType, Type conversionType, Type typeArgument, Type[] typeArguments) => ByObjectToICollectionKeyValuePair<TResult>(sourceType, conversionType, typeArgument, typeArguments);

        /// <summary>
        /// 解决 类 到类似 ICollection&lt;T&gt; 类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型【T】约束</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToICollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(ByObjectToList), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArgument, conversionType);

            var bodyExp = Call(null, methodG, parameterExp, Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 对象 到类似 ICollection&lt;T&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型【T】约束</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToCollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(ByObjectToCollectionLike), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArgument, conversionType);

            var bodyExp = Call(null, methodG, parameterExp, Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 对象 到 类似 ICollection&lt;KeyValuePair&lt;TKey,TValue&gt;&gt; 类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型【KeyValuePair&lt;TKey,TValue&gt;】约束</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToICollectionKeyValuePair<TResult>(Type sourceType, Type conversionType, Type typeArgument, Type[] typeArguments)
        {
            var typeStore = RuntimeTypeCache.Instance.GetCache(sourceType);

            var list = new List<Expression>();

            var listKvType = typeof(List<>).MakeGenericType(typeArgument);

            var resultExp = Variable(listKvType, "result");

            var targetExp = Variable(sourceType, "target");

            var sourceExp = Parameter(typeof(object), "source");

            var method = listKvType.GetMethod("Add", new Type[] { typeArgument });

            list.Add(Assign(targetExp, Convert(sourceExp, sourceType)));

            var methodCtor = ServiceCtor.Method;

            var bodyExp = methodCtor.IsStatic ?
                Call(null, methodCtor, Constant(listKvType)) :
                Call(Constant(ServiceCtor.Target), methodCtor, Constant(listKvType));

            list.Add(Assign(resultExp, Convert(bodyExp, listKvType)));

            var typeStore2 = RuntimeTypeCache.Instance.GetCache(typeArgument);

            var ctorSotre = typeStore2.ConstructorStores.Where(x => x.ParameterStores.Count == 2).First();

            if (Kind == PatternKind.Property || Kind == PatternKind.All)
            {
                typeStore.PropertyStores
                    .Where(x => x.CanRead)
                    .ForEach(info =>
                    {
                        list.Add(Call(resultExp, method, New(ctorSotre.Member, ConvertConfig(Constant(info.Naming), typeArguments[0]), ConvertConfig(Property(targetExp, info.Member), typeArguments[1]))));
                    });
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores
                    .Where(x => x.CanRead)
                    .ForEach(info =>
                    {
                        list.Add(Call(resultExp, method, New(ctorSotre.Member, ConvertConfig(Constant(info.Naming), typeArguments[0]), ConvertConfig(Field(targetExp, info.Member), typeArguments[1]))));
                    });
            }

            list.Add(Convert(resultExp, conversionType));

            var lamdaExp = Lambda<Func<object, TResult>>(Block(new[] { targetExp, resultExp }, list), sourceExp);

            return lamdaExp.Compile();

            Expression ConvertConfig(Expression node, Type type)
            {
                if (node.Type == type) return node;

                return Convert(node, type);
            }
        }

        /// <summary>
        /// 解决 对象 到类似 ICollection&lt;KeyValuePair&lt;TKey,TValue&gt;&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArgument">泛型【KeyValuePair&lt;TKey,TValue&gt;】约束</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToCollectionKeyValuePairLike<TResult>(Type sourceType, Type conversionType, Type typeArgument, Type[] typeArguments)
        {
            var typeStore = RuntimeTypeCache.Instance.GetCache(sourceType);

            var list = new List<Expression>();

            var resultExp = Variable(conversionType, "result");

            var targetExp = Variable(sourceType, "target");

            var sourceExp = Parameter(typeof(object), "source");

            var method = conversionType.GetMethod("Add", new Type[] { typeArgument });

            list.Add(Assign(targetExp, Convert(sourceExp, sourceType)));

            var methodCtor = ServiceCtor.Method;

            var bodyExp = methodCtor.IsStatic ?
                Call(null, methodCtor, Constant(conversionType)) :
                Call(Constant(ServiceCtor.Target), methodCtor, Constant(conversionType));

            list.Add(Assign(resultExp, Convert(bodyExp, conversionType)));

            var typeStore2 = RuntimeTypeCache.Instance.GetCache(typeArgument);

            var ctorSotre = typeStore2.ConstructorStores.Where(x => x.ParameterStores.Count == 2).First();

            if (Kind == PatternKind.Property || Kind == PatternKind.All)
            {
                typeStore.PropertyStores
                    .Where(x => x.CanRead)
                    .ForEach(info =>
                    {
                        list.Add(Call(resultExp, method, New(ctorSotre.Member, ConvertConfig(Constant(info.Naming), typeArguments[0]), ConvertConfig(Property(targetExp, info.Member), typeArguments[1]))));
                    });
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores
                    .Where(x => x.CanRead)
                    .ForEach(info =>
                    {
                        list.Add(Call(resultExp, method, New(ctorSotre.Member, ConvertConfig(Constant(info.Naming), typeArguments[0]), ConvertConfig(Field(targetExp, info.Member), typeArguments[1]))));
                    });
            }

            list.Add(resultExp);

            var lamdaExp = Lambda<Func<object, TResult>>(Block(new[] { targetExp, resultExp }, list), sourceExp);

            return lamdaExp.Compile();

            Expression ConvertConfig(Expression node, Type type)
            {
                if (node.Type == type) return node;

                return Convert(node, type);
            }
        }

        /// <summary>
        /// 解决 对象 到类似 IDictionary&lt;TKey,TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToIDictionaryLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments)
        {
            var typeStore = RuntimeTypeCache.Instance.GetCache(sourceType);

            var list = new List<Expression>();

            var dicType = typeof(Dictionary<,>).MakeGenericType(typeArguments);

            var resultExp = Variable(dicType, "result");

            var targetExp = Variable(sourceType, "target");

            var sourceExp = Parameter(typeof(object), "source");

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
                        list.Add(Call(resultExp, method, ConvertConfig(Constant(info.Naming), typeArguments[0]), ConvertConfig(Property(targetExp, info.Member), typeArguments[1])));
                    });
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores
                    .Where(x => x.CanRead)
                    .ForEach(info =>
                    {
                        list.Add(Call(resultExp, method, ConvertConfig(Constant(info.Naming), typeArguments[0]), ConvertConfig(Field(targetExp, info.Member), typeArguments[1])));
                    });
            }

            list.Add(Convert(resultExp, conversionType));

            var lamdaExp = Lambda<Func<object, TResult>>(Block(new[] { targetExp, resultExp }, list), sourceExp);

            return lamdaExp.Compile();

            Expression ConvertConfig(Expression node, Type type)
            {
                if (node.Type == type) return node;

                return Convert(node, type);
            }
        }

        /// <summary>
        /// 解决 对象 到类似 IDictionary&lt;TKey,TValue&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型</typeparam>
        /// <param name="sourceType">源数据类型</param>
        /// <param name="conversionType">目标数据类型</param>
        /// <param name="typeArguments">泛型【TKey,TValue】约束</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToDictionaryLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments)
        {
            var typeStore = RuntimeTypeCache.Instance.GetCache(sourceType);

            var list = new List<Expression>();

            var resultExp = Variable(conversionType, "result");

            var targetExp = Variable(sourceType, "target");

            var sourceExp = Parameter(typeof(object), "source");

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
                        list.Add(Call(resultExp, method, ConvertConfig(Constant(info.Naming), typeArguments[0]), ConvertConfig(Property(targetExp, info.Member), typeArguments[1])));
                    });
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores
                    .Where(x => x.CanRead)
                    .ForEach(info =>
                    {
                        list.Add(Call(resultExp, method, ConvertConfig(Constant(info.Naming), typeArguments[0]), ConvertConfig(Field(targetExp, info.Member), typeArguments[1])));
                    });
            }

            list.Add(resultExp);

            var lamdaExp = Lambda<Func<object, TResult>>(Block(new[] { targetExp, resultExp }, list), sourceExp);

            return lamdaExp.Compile();

            Expression ConvertConfig(Expression node, Type type)
            {
                if (node.Type == type) return node;

                return Convert(node, type);
            }
        }
    }
}
