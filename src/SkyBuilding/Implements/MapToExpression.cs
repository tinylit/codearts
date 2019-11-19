using SkyBuilding.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace SkyBuilding.Implements
{
    /// <summary>
    /// 数据映射
    /// </summary>
    public class MapToExpression : CopyToExpression<MapToExpression>, IMapToExpression, IProfileConfiguration, IProfile
    {
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
        public T MapTo<T>(object obj, T def = default)
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
        public object MapTo(object source, Type conversionType)
        {
            if (source == null) return null;

            try
            {
                return UnsafeMapTo(source, conversionType);
            }
            catch (InvalidCastException)
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

        private static MethodInfo GetMethodInfo<T1, T2, T3>(Func<T1, T2, T3> func) => func.Method;

        private static bool EqaulsString(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<TKey, TValue> ByDataRowToDictionary<TKey, TValue>(DataRow dr, MapToExpression mapTo)
        => ByDataRowToDictionaryLike<TKey, TValue, Dictionary<TKey, TValue>>(dr, mapTo);

        private static TResult ByDataRowToDictionaryLike<TKey, TValue, TResult>(DataRow dr, MapToExpression mapTo) where TResult : IDictionary<TKey, TValue>
        {
            var dic = (TResult)mapTo.ServiceCtor.Invoke(typeof(TResult));

            foreach (DataColumn item in dr.Table.Columns)
            {
                dic.Add((TKey)(object)item.ColumnName, (TValue)dr[item.ColumnName]);
            }

            return dic;
        }

        private static TResult ByDataRowToCollectionLike<TKey, TValue, TResult>(DataRow dr, MapToExpression mapTo) where TResult : ICollection<KeyValuePair<TKey, TValue>>
        {
            var dic = (TResult)mapTo.ServiceCtor.Invoke(typeof(TResult));

            foreach (DataColumn item in dr.Table.Columns)
            {
                dic.Add(new KeyValuePair<TKey, TValue>((TKey)(object)item.ColumnName, (TValue)(object)dr[item.ColumnName]));
            }

            return dic;
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
                    return null;

                return mapTo.UnsafeMapTo(kv.Value, conversionType);
            }

            return default;
        }

        private static Dictionary<string, object> ByIDataRecordToValueTypeOrStringDictionary(IDataRecord dataRecord, MapToExpression mapTo)
            => ByIDataRecordToValueTypeOrStringCollectionLike<Dictionary<string, object>>(dataRecord, mapTo);

        private static TResult ByIDataRecordToValueTypeOrStringCollectionLike<TResult>(IDataRecord dataRecord, MapToExpression mapTo) where TResult : ICollection<KeyValuePair<string, object>>
        {
            var results = (TResult)mapTo.ServiceCtor.Invoke(typeof(TResult));

            for (int i = 0; i < dataRecord.FieldCount; i++)
            {
                string name = dataRecord.GetName(i);

                if (dataRecord.IsDBNull(i))
                {
                    if (mapTo.AllowNullPropagationMapping.Value)
                    {
                        results.Add(new KeyValuePair<string, object>(name, null));
                    }
                    continue;
                }

                results.Add(new KeyValuePair<string, object>(name, dataRecord.GetValue(i)));
            }

            return results;
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
                return ByIDataRecord<TResult>(sourceType, typeof(TResult));

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
                return conversionType.IsInterface ? ByDataTableToIEnumarableLike<TResult>(sourceType, conversionType) : ByDataTableToEnumarableLike<TResult>(sourceType, conversionType);

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

            var method = typeof(MapToExpression).GetMethod(nameof(ByDataTableToList), BindingFlags.NonPublic | BindingFlags.Static);

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
                var typeDefinition = item.GetGenericTypeDefinition();
                var typeArguments = typeDefinition.GetGenericArguments();

                if (typeDefinition == typeof(ICollection<>))
                {
                    var typeArgument = typeArguments.First();

                    if (typeArgument.IsClass || typeArgument.IsValueType || typeof(IEnumerable).IsAssignableFrom(typeArgument))
                        return ByDataTableToCollectionLike<TResult>(sourceType, conversionType, typeArgument);
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

            var method = typeof(MapToExpression).GetMethod(nameof(ByDataTableToCollectionLike), BindingFlags.NonPublic | BindingFlags.Static);

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

            var method = typeof(MapToExpression).GetMethod(nameof(ByDataTableToList), BindingFlags.NonPublic | BindingFlags.Static);

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
                        return dr.IsNull(0) ? default : (TResult)System.Convert.ChangeType(dr[0], conversionType);
                    }

                    return default;
                };
            }

            if (typeof(IEnumerable).IsAssignableFrom(conversionType))
                return conversionType.IsInterface ? ByDataRowToIEnumarable<TResult>(sourceType, conversionType) : ByDataRowToEnumarable<TResult>(sourceType, conversionType);

            return ByDataRowToObject<TResult>(sourceType, conversionType);
        }

        /// <summary>
        /// 解决 DataRow 到类似 IEnumarable的转换。
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
                    return ByDataRowToDictionary<TResult>(sourceType, conversionType, typeArguments);

#if !NET40
                if (typeDefinition == typeof(IReadOnlyDictionary<,>))
                    return ByDataRowToDictionary<TResult>(sourceType, conversionType, typeArguments);
#endif

                if (typeDefinition == typeof(IEnumerable<>) || typeDefinition == typeof(ICollection<>) || typeDefinition == typeof(IList<>))
                {
                    var typeArgument = typeArguments.First();

                    if (typeArgument.IsGenericType && typeArgument.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                        return ByDataRowToDictionary<TResult>(sourceType, conversionType, typeArgument.GetGenericArguments());

                    throw new InvalidCastException();
                }

#if !NET40
                if (typeDefinition == typeof(IReadOnlyCollection<>) || typeDefinition == typeof(IReadOnlyList<>))
                {
                    var typeArgument = typeArguments.First();

                    if (typeArgument.IsGenericType && typeArgument.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                        return ByDataRowToDictionary<TResult>(sourceType, conversionType, typeArgument.GetGenericArguments());

                    throw new InvalidCastException();
                }
#endif

                throw new InvalidCastException();
            }

            return null;
        }

        /// <summary>
        /// 解决 DataRow 到类似 IEnumarable的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        private Func<object, TResult> ByDataRowToEnumarable<TResult>(Type sourceType, Type conversionType)
        {
            var interfaces = conversionType.GetInterfaces();

            foreach (var item in interfaces.Where(x => x.IsGenericType))
            {
                var type = item.GetGenericTypeDefinition();

                if (type == typeof(IDictionary<,>))
                    return ByDataRowToDictionaryLike<TResult>(sourceType, conversionType, type.GetGenericArguments());

                if (type == typeof(ICollection<>))
                {
                    var typeArguments = type.GetGenericArguments();

                    var typeArgument = typeArguments.First();

                    if (typeArgument.IsGenericType && typeArgument.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                        return ByDataRowToCollectionLike<TResult>(sourceType, conversionType, typeArgument.GetGenericArguments());

                    throw new InvalidCastException();
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
        protected virtual Func<object, TResult> ByDataRowToCollectionLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeof(MapToExpression).GetMethod(nameof(ByDataRowToCollectionLike), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArguments.Concat(new Type[] { conversionType }).ToArray());

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
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeof(MapToExpression).GetMethod(nameof(ByDataRowToDictionaryLike), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArguments.Concat(new Type[] { conversionType }).ToArray());

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
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeof(MapToExpression).GetMethod(nameof(ByDataRowToDictionary), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArguments);

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
                typeStore.PropertyStores.Where(x => x.CanWrite).ForEach(info =>
                {
                    list.Add(SwitchCase(Constant(info.Name), Assign(Property(resultExp, info.Member), Convert(valueExp, info.MemberType))));
                });
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores.Where(x => x.CanWrite).ForEach(info =>
                {
                    list.Add(SwitchCase(Constant(info.Name), Assign(Field(resultExp, info.Member), Convert(valueExp, info.MemberType))));
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

            if (typeof(IEnumerable<KeyValuePair<string, object>>).IsAssignableFrom(conversionType))
                return ByIDataRecordToEnumerableKeyStringValueObjectPair<TResult>(sourceType, conversionType);

            return ByIDataRecordToObject<TResult>(sourceType, conversionType);
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

            var errorExp = Parameter(typeof(Exception), "e");

            var isDBNull = sourceType.GetMethod("IsDBNull", new Type[] { typeof(int) });

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
                objExp = TryCatch(Call(valueExp, methodInfo, indexExp), Catch(errorExp, Convert(Call(null, convertMethod, objExp, Constant(conversionType)), conversionType)));
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
        /// 解决 IDataRecord 到 IEnumerable&lt;KeyValuePair&lt;string, object&gt;&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByIDataRecordToEnumerableKeyStringValueObjectPair<TResult>(Type sourceType, Type conversionType)
        {
            if (conversionType.IsInterface || conversionType.IsClass && conversionType == typeof(Dictionary<string, object>))
            {
                if (conversionType.IsClass || conversionType.IsAssignableFrom(typeof(Dictionary<string, object>)))
                {
                    var parameterExp = Parameter(typeof(object), "source");

                    var method = typeof(MapToExpression).GetMethod(nameof(ByIDataRecordToValueTypeOrStringDictionary), BindingFlags.NonPublic | BindingFlags.Static);

                    var bodyExp = Call(null, method, Convert(parameterExp, sourceType), Constant(this));

                    var lamdaExp =
                        conversionType.IsClass ?
                        Lambda<Func<object, TResult>>(bodyExp, parameterExp)
                        :
                        Lambda<Func<object, TResult>>(Convert(bodyExp, conversionType), parameterExp);

                    return lamdaExp.Compile();
                }
            }
            else if (conversionType.IsClass && typeof(ICollection<KeyValuePair<string, object>>).IsAssignableFrom(conversionType))
            {
                var parameterExp = Parameter(typeof(object), "source");

                var method = typeof(MapToExpression).GetMethod(nameof(ByIDataRecordToValueTypeOrStringCollectionLike), BindingFlags.NonPublic | BindingFlags.Static);

                var methodG = method.MakeGenericMethod(conversionType);

                var bodyExp = Call(null, methodG, Convert(parameterExp, sourceType), Constant(this));

                var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

                return lamdaExp.Compile();
            }

            throw new InvalidCastException();
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

            var valueExp = Variable(sourceType, "value");

            var indexExp = Variable(typeof(int), "index");

            var targetExp = Variable(conversionType, "target");

            var negativeExp = Constant(-1);

            var dicExp = Variable(typeof(Dictionary<int, string>), "dic");

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

            var mapType = typeof(MapToExpression);

            var getNames = mapType.GetMethod(nameof(GetKeyWithFields), BindingFlags.NonPublic | BindingFlags.Static);

            var getOrdinal = mapType.GetMethod(nameof(GetOrdinal), BindingFlags.NonPublic | BindingFlags.Static);

            var isDBNull = sourceType.GetMethod("IsDBNull", new Type[] { typeof(int) });

            var getFieldType = sourceType.GetMethod("GetFieldType", new Type[] { typeof(int) });

            var convertMethod = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });

            var list = new List<Expression> { Assign(valueExp, Convert(parameterExp, sourceType)) };

            list.Add(Assign(dicExp, Call(null, getNames, Convert(valueExp, typeof(IDataRecord)))));

            var variables = new List<ParameterExpression> { valueExp, targetExp, indexExp, dicExp };

            var arguments = new List<Expression>();

            commonCtor.ParameterStores
                .ForEach(info => ConfigParameter(info));

            list.Add(Assign(targetExp, New(commonCtor.Member, arguments)));

            if (Kind == PatternKind.Property || Kind == PatternKind.All)
            {
                typeStore.PropertyStores.Where(x => x.CanWrite && !commonCtor.ParameterStores.Any(y => y.Name == x.Name))
                   .ForEach(info => Config(info, Property(targetExp, info.Member)));
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores.Where(x => x.CanWrite && !commonCtor.ParameterStores.Any(y => y.Name == x.Name))
                   .ForEach(info => Config(info, Field(targetExp, info.Member)));
            }

            list.Add(targetExp);

            var lamdaExp = Lambda<Func<object, TResult>>(Block(variables, list), parameterExp);

            return lamdaExp.Compile();

            void Config<T>(StoreItem<T> info, Expression left) where T : MemberInfo
            {
                var memberType = info.MemberType;

                list.Add(Assign(indexExp, Call(null, getOrdinal, dicExp, Constant(info.Name))));

                var testExp = AndAlso(GreaterThan(indexExp, negativeExp), Not(Call(valueExp, isDBNull, indexExp)));

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

                Expression objExp = Call(valueExp, typeMap[typeof(object)], indexExp);

                if (typeMap.TryGetValue(memberType, out MethodInfo methodInfo))
                {
                    var memberTypeCst = Constant(memberType);

                    objExp = Condition(Equal(memberTypeCst, Call(valueExp, getFieldType, indexExp)), Call(valueExp, methodInfo, indexExp), Convert(Call(null, convertMethod, objExp, memberTypeCst), memberType));
                }
                else
                {
                    objExp = Convert(objExp, memberType);
                }

                if (AllowNullPropagationMapping.Value)
                {
                    if (!AllowNullDestinationValues.Value && memberType == typeof(string))
                    {
                        list.Add(IfThenElse(testExp, Assign(left, objExp), Assign(left, Constant(string.Empty))));
                    }
                    else if (memberType == info.MemberType)
                    {
                        list.Add(IfThenElse(testExp, Assign(left, objExp), Assign(left, Default(info.MemberType))));
                    }
                    else
                    {
                        list.Add(IfThenElse(testExp, Assign(left, Convert(objExp, info.MemberType)), Assign(left, Default(info.MemberType))));
                    }
                }
                else if (!AllowNullDestinationValues.Value && memberType == typeof(string))
                {
                    list.Add(IfThenElse(testExp, Assign(left, objExp), Assign(left, Coalesce(left, Constant(string.Empty))))); ;
                }
                else if (memberType == info.MemberType)
                {
                    list.Add(IfThen(testExp, Assign(left, objExp)));
                }
                else
                {
                    list.Add(IfThen(testExp, Assign(left, Convert(objExp, info.MemberType))));
                }
            }

            void ConfigParameter(ParameterStoreItem info)
            {
                var memberType = info.ParameterType;

                list.Add(Assign(indexExp, Call(null, getOrdinal, dicExp, Constant(info.Name))));

                var testExp = AndAlso(GreaterThan(indexExp, negativeExp), Not(Call(valueExp, isDBNull, indexExp)));

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

                Expression objExp = Call(valueExp, typeMap[typeof(object)], indexExp);

                if (typeMap.TryGetValue(memberType, out MethodInfo methodInfo))
                {
                    var memberTypeCst = Constant(memberType);

                    objExp = Condition(Equal(memberTypeCst, Call(valueExp, getFieldType, indexExp)), Call(valueExp, methodInfo, indexExp), Convert(Call(null, convertMethod, objExp, memberTypeCst), memberType));
                }
                else
                {
                    objExp = Convert(objExp, memberType);
                }

                Expression defaultExp = null;

                if (info.IsOptional)
                {
                    defaultExp = Constant(info.DefaultValue, info.ParameterType);
                }
                else
                {
                    defaultExp = Default(info.ParameterType);
                }


                if (AllowNullPropagationMapping.Value)
                {
                    if (!AllowNullDestinationValues.Value && memberType == typeof(string))
                    {
                        list.Add(IfThenElse(testExp, Assign(nameExp, objExp), Assign(nameExp, Constant(string.Empty))));
                    }
                    else if (memberType == info.ParameterType)
                    {
                        list.Add(IfThenElse(testExp, Assign(nameExp, objExp), Assign(nameExp, Default(info.ParameterType))));
                    }
                    else
                    {
                        list.Add(IfThenElse(testExp, Assign(nameExp, Convert(objExp, info.ParameterType)), Assign(nameExp, Default(info.ParameterType))));
                    }
                }
                else if (!AllowNullDestinationValues.Value && memberType == typeof(string))
                {
                    list.Add(IfThenElse(testExp, Assign(nameExp, objExp), Assign(nameExp, Constant(string.Empty))));
                }
                else if (memberType == info.ParameterType)
                {
                    list.Add(IfThenElse(testExp, Assign(nameExp, objExp), Assign(nameExp, Default(info.ParameterType))));
                }
                else
                {
                    list.Add(IfThenElse(testExp, Assign(nameExp, Convert(objExp, info.ParameterType)), Assign(nameExp, Default(info.ParameterType))));
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
        protected virtual Func<object, TResult> ByIDataRecordToByCommon<TResult>(TypeStoreItem typeStore, Type sourceType, Type conversionType)
        {
            var method = ServiceCtor.Method;

            Expression bodyExp = Convert(method.IsStatic
                ? Call(null, method, Constant(conversionType))
                : Call(Constant(ServiceCtor.Target), method, Constant(conversionType))
                , conversionType);

            var list = new List<Expression>();

            var parameterExp = Parameter(typeof(object), "source");

            var nullCst = Constant(null);

            var valueExp = Variable(sourceType, "value");

            var targetExp = Variable(conversionType, "target");

            var indexExp = Variable(typeof(int), "index");

            var dicExp = Variable(typeof(Dictionary<int, string>), "dic");

            var negativeExp = Constant(-1);

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

            var mapType = typeof(MapToExpression);

            var getNames = mapType.GetMethod(nameof(GetKeyWithFields), BindingFlags.NonPublic | BindingFlags.Static);

            var getOrdinal = mapType.GetMethod(nameof(GetOrdinal), BindingFlags.NonPublic | BindingFlags.Static);

            list.Add(Assign(dicExp, Call(null, getNames, Convert(valueExp, typeof(IDataRecord)))));

            var isDBNull = sourceType.GetMethod("IsDBNull", new Type[] { typeof(int) });

            var getFieldType = sourceType.GetMethod("GetFieldType", new Type[] { typeof(int) });

            var convertMethod = typeof(Convert).GetMethod(nameof(System.Convert.ChangeType), new Type[] { typeof(object), typeof(Type) });

            if (Kind == PatternKind.Property || Kind == PatternKind.All)
            {
                typeStore.PropertyStores.Where(x => x.CanWrite)
                   .ForEach(info => Config(info, Property(targetExp, info.Member)));
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores.Where(x => x.CanWrite)
                   .ForEach(info => Config(info, Field(targetExp, info.Member)));
            }

            list.Add(targetExp);

            var lamdaExp = Lambda<Func<object, TResult>>(Block(new[] { valueExp, indexExp, targetExp, dicExp }, list), parameterExp);

            return lamdaExp.Compile();

            void Config<T>(StoreItem<T> info, Expression left) where T : MemberInfo
            {
                var memberType = info.MemberType;

                list.Add(Assign(indexExp, Call(null, getOrdinal, dicExp, Constant(info.Name))));

                var testExp = AndAlso(GreaterThan(indexExp, negativeExp), Not(Call(valueExp, isDBNull, indexExp)));

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

                Expression objExp = Call(valueExp, typeMap[typeof(object)], indexExp);

                if (typeMap.TryGetValue(memberType, out MethodInfo methodInfo))
                {
                    var memberTypeCst = Constant(memberType);

                    objExp = Condition(Equal(memberTypeCst, Call(valueExp, getFieldType, indexExp)), Call(valueExp, methodInfo, indexExp), Convert(Call(null, convertMethod, objExp, memberTypeCst), memberType));
                }
                else
                {
                    objExp = Convert(objExp, memberType);
                }

                if (AllowNullPropagationMapping.Value)
                {
                    if (!AllowNullDestinationValues.Value && memberType == typeof(string))
                    {
                        list.Add(IfThenElse(testExp, Assign(left, objExp), Assign(left, Constant(string.Empty))));
                    }
                    else if (memberType == info.MemberType)
                    {
                        list.Add(IfThenElse(testExp, Assign(left, objExp), Assign(left, Default(info.MemberType))));
                    }
                    else
                    {
                        list.Add(IfThenElse(testExp, Assign(left, Convert(objExp, info.MemberType)), Assign(left, Default(info.MemberType))));
                    }
                }
                else if (!AllowNullDestinationValues.Value && memberType == typeof(string))
                {
                    list.Add(IfThenElse(testExp, Assign(left, objExp), Assign(left, Coalesce(left, Constant(string.Empty))))); ;
                }
                else if (memberType == info.MemberType)
                {
                    list.Add(IfThen(testExp, Assign(left, objExp)));
                }
                else
                {
                    list.Add(IfThen(testExp, Assign(left, Convert(objExp, info.MemberType))));
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
                return ByIDataRecordToByCommon<TResult>(typeStore, sourceType, conversionType);

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

            var method = typeof(MapToExpression).GetMethod(nameof(ByEnumarableToList), BindingFlags.NonPublic | BindingFlags.Static);

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

            var method = typeof(MapToExpression).GetMethod(nameof(ByEnumarableToCollectionLike), BindingFlags.NonPublic | BindingFlags.Static);

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

                if (!typeArgument.IsValueType || !typeArgument.IsGenericType || typeArgument.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) continue;

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

            var method = typeof(MapToExpression).GetMethod(nameof(GetValueByEnumarableKeyValuePair), BindingFlags.NonPublic | BindingFlags.Static);

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
                typeStore.PropertyStores.ForEach(info =>
                {
                    Config(info, Property(resultExp, info.Member));
                });
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores.ForEach(info =>
                {
                    Config(info, Field(resultExp, info.Member));
                });
            }

            list.Add(resultExp);

            var lamdaExp = Lambda<Func<object, TResult>>(Block(new ParameterExpression[] { valueExp, maptoExp, resultExp }, list), parameterExp);

            return lamdaExp.Compile();

            void Config<T>(StoreItem<T> item, Expression node) where T : MemberInfo
            {
                list.Add(Assign(maptoExp, Call(null, methodG, valueExp, Constant(item.Name), Constant(item.MemberType), thisExp)));

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

            var method = typeof(MapToExpression).GetMethod(nameof(ByObjectToList), BindingFlags.NonPublic | BindingFlags.Static);

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

            var method = typeof(MapToExpression).GetMethod(nameof(ByObjectToCollectionLike), BindingFlags.NonPublic | BindingFlags.Static);

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
                typeStore.PropertyStores.Where(x => x.CanRead).ForEach(info =>
                {
                    list.Add(Call(resultExp, method, New(ctorSotre.Member, ConvertConfig(Constant(info.Name), typeArguments[0]), ConvertConfig(Property(targetExp, info.Member), typeArguments[1]))));
                });
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores.Where(x => x.CanRead).ForEach(info =>
                {
                    list.Add(Call(resultExp, method, New(ctorSotre.Member, ConvertConfig(Constant(info.Name), typeArguments[0]), ConvertConfig(Field(targetExp, info.Member), typeArguments[1]))));
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
                typeStore.PropertyStores.Where(x => x.CanRead).ForEach(info =>
                {
                    list.Add(Call(resultExp, method, New(ctorSotre.Member, ConvertConfig(Constant(info.Name), typeArguments[0]), ConvertConfig(Property(targetExp, info.Member), typeArguments[1]))));
                });
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores.Where(x => x.CanRead).ForEach(info =>
                {
                    list.Add(Call(resultExp, method, New(ctorSotre.Member, ConvertConfig(Constant(info.Name), typeArguments[0]), ConvertConfig(Field(targetExp, info.Member), typeArguments[1]))));
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
                typeStore.PropertyStores.Where(x => x.CanRead).ForEach(info =>
                {
                    list.Add(Call(resultExp, method, ConvertConfig(Constant(info.Name), typeArguments[0]), ConvertConfig(Property(targetExp, info.Member), typeArguments[1])));
                });
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores.Where(x => x.CanRead).ForEach(info =>
                {
                    list.Add(Call(resultExp, method, ConvertConfig(Constant(info.Name), typeArguments[0]), ConvertConfig(Field(targetExp, info.Member), typeArguments[1])));
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
                typeStore.PropertyStores.Where(x => x.CanRead).ForEach(info =>
                {
                    list.Add(Call(resultExp, method, ConvertConfig(Constant(info.Name), typeArguments[0]), ConvertConfig(Property(targetExp, info.Member), typeArguments[1])));
                });
            }

            if (Kind == PatternKind.Field || Kind == PatternKind.All)
            {
                typeStore.FieldStores.Where(x => x.CanRead).ForEach(info =>
                {
                    list.Add(Call(resultExp, method, ConvertConfig(Constant(info.Name), typeArguments[0]), ConvertConfig(Field(targetExp, info.Member), typeArguments[1])));
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
