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
            catch
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
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 不安全的映射（有异常）
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="source">数据源</param>
        /// <returns></returns>
        private object UnsafeMapTo(object source, Type conversionType)
        {
            var invoke = Create(source.GetType(), conversionType);

            return invoke.Invoke(source);
        }

        #region 反射使用

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
                dic.Add((TKey)(object)item.ColumnName, (TValue)(object)dr[item.ColumnName]);
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

        private static List<T> ByDataTableToList<T>(DataTable table, MapToExpression mapTo)
        => ByDataTableToCollectionLike<T, List<T>>(table, mapTo);

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
        /// 可空类型
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="genericType">泛型参数</param>
        /// <returns></returns>
        protected override Func<object, TResult> ToNullable<TResult>(Type sourceType, Type conversionType, Type genericType)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var valueExp = Variable(conversionType, "value");

            var typeStore = RuntimeTypeCache.Instance.GetCache(conversionType);

            var ctorInfo = typeStore.ConstructorStores.First(x => x.ParameterStores.Count == 1);

            Expression coreExp = parameterExp;

            if (sourceType != genericType)
            {
                var invoke = Create(sourceType, genericType);

                coreExp = invoke.Method.IsStatic ?
                    Call(null, invoke.Method, parameterExp)
                    :
                    Call(Constant(invoke.Target), invoke.Method, parameterExp);
            }

            var bodyExp = New(ctorInfo.Member, Convert(coreExp, sourceType));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 值类型转目标类型
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByValueType<TResult>(Type sourceType, Type conversionType)
            => source => source.CastTo<TResult>();

        /// <summary>
        /// 源类型转值类型
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected override Func<object, TResult> ToValueType<TResult>(Type sourceType, Type conversionType)
            => source => source.CastTo<TResult>();

        /// <summary>
        /// 对象转普通数据
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
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

            return ByObjectToObject<TResult>(sourceType, conversionType);
        }

        #region Table
        /// <summary>
        /// DataTable 数据源
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
                return ByDataTableToEnumarable<TResult>(sourceType, conversionType);

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

        protected virtual Func<object, TResult> ByDataTableToEnumarable<TResult>(Type sourceType, Type conversionType)
        {
            var interfaces = conversionType.GetInterfaces();

            foreach (var item in interfaces.Where(x => x.IsGenericType))
            {
                var type = item.GetGenericTypeDefinition();

                if (type == typeof(IEnumerable<>))
                {
                    var typeArguments = type.GetGenericArguments();

                    var typeArgument = typeArguments.First();

                    if (typeArgument.IsClass || typeArgument.IsValueType || typeof(IEnumerable).IsAssignableFrom(typeArgument))
                        return ByDataTableToEnumarableValue<TResult>(sourceType, conversionType, typeArgument);
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

        protected virtual Func<object, TResult> ByDataTableToEnumarableValue<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            if (conversionType.IsInterface)
            {
                var typeDefinition = conversionType.GetGenericTypeDefinition();

                var types = conversionType.GetGenericArguments();

                if (typeDefinition == typeof(IEnumerable<>) || typeDefinition == typeof(ICollection<>) || typeDefinition == typeof(IList<>))
                    return ByDataTableToList<TResult>(sourceType, conversionType, typeArgument);

#if !NET40
                if (typeDefinition == typeof(IReadOnlyCollection<>))
                    return ByDataTableToList<TResult>(sourceType, conversionType, typeArgument);
#endif

                throw new InvalidCastException();
            }

            var interfaces = conversionType.GetInterfaces();

            foreach (var item in interfaces.Where(x => x.IsGenericType))
            {
                var type = item.GetGenericTypeDefinition();

                if (type == typeof(ICollection<>))
                    return ByDataTableToCollectionValueLike<TResult>(sourceType, conversionType, typeArgument);
            }

            throw new InvalidCastException();
        }

        protected virtual Func<object, TResult> ByDataTableToCollectionValueLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeof(MapToExpression).GetMethod(nameof(ByDataTableToCollectionLike), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArgument, conversionType);

            var bodyExp = Call(null, methodG, Convert(parameterExp, sourceType), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

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
        /// DataRow 数据源
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
                return ByDataRowToEnumarable<TResult>(sourceType, conversionType);

            return ByDataRowToObject<TResult>(conversionType);
        }

        private Func<object, TResult> ByDataRowToEnumarable<TResult>(Type sourceType, Type conversionType)
        {
            if (conversionType.IsInterface)
            {
                var typeDefinition = conversionType.GetGenericTypeDefinition();

                var types = conversionType.GetGenericArguments();

                if (typeDefinition == typeof(IDictionary<,>))
                    return ByDataRowToDictionaryLike<TResult>(sourceType, conversionType, types);

#if !NET40
                if (typeDefinition == typeof(IReadOnlyDictionary<,>))
                    return ByDataRowToDictionaryLike<TResult>(sourceType, conversionType, types);
#endif

                if (typeDefinition == typeof(IEnumerable<>) || typeDefinition == typeof(ICollection<>) || typeDefinition == typeof(IList<>))
                {
                    var typeArgument = types.First();

                    if (typeArgument.IsGenericType && typeArgument.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                        return ByDataRowToDictionary<TResult>(sourceType, conversionType, typeArgument.GetGenericArguments());

                    throw new InvalidCastException();
                }

#if !NET40
                if (typeDefinition == typeof(IReadOnlyCollection<>))
                {
                    var typeArgument = types.First();

                    if (typeArgument.IsGenericType && typeArgument.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                        return ByDataRowToDictionary<TResult>(sourceType, conversionType, typeArgument.GetGenericArguments());

                    throw new InvalidCastException();
                }
#endif

                throw new InvalidCastException();
            }

            var interfaces = conversionType.GetInterfaces();

            foreach (var item in interfaces.Where(x => x.IsGenericType))
            {
                var type = item.GetGenericTypeDefinition();

                if (type == typeof(IDictionary<,>))
                    return ByDataRowToDictionary<TResult>(sourceType, conversionType, type.GetGenericArguments());

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

        protected virtual Func<object, TResult> ByDataRowToCollectionLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeof(MapToExpression).GetMethod(nameof(ByDataRowToCollectionLike), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArguments.Concat(new Type[] { conversionType }).ToArray());

            var bodyExp = Call(null, methodG, Convert(parameterExp, sourceType), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        protected virtual Func<object, TResult> ByDataRowToDictionaryLike<TResult>(Type sourceType, Type conversionType, Type[] typeArguments)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeof(MapToExpression).GetMethod(nameof(ByDataRowToDictionaryLike), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArguments.Concat(new Type[] { conversionType }).ToArray());

            var bodyExp = Call(null, methodG, Convert(parameterExp, sourceType), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        protected virtual Func<object, TResult> ByDataRowToDictionary<TResult>(Type sourceType, Type conversionType, Type[] typeArguments)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeof(MapToExpression).GetMethod(nameof(ByDataRowToDictionary), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(typeArguments);

            var bodyExp = Call(null, methodG, Convert(parameterExp, sourceType), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(Convert(bodyExp, conversionType), parameterExp);

            return lamdaExp.Compile();
        }

        private Func<object, TResult> ByDataRowToObject<TResult>(Type conversionType)
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
                if (conversionType.IsNullable())
                {
                    conversionType = Nullable.GetUnderlyingType(conversionType);
                }
                else
                {
                    if (conversionType.IsEnum)
                    {
                        conversionType = Enum.GetUnderlyingType(conversionType);
                    }
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

        protected virtual Func<object, TResult> ByIDataRecord<TResult>(Type sourceType, Type conversionType)
        {
            if (conversionType.IsValueType || conversionType == typeof(string))
            {
                return ByIDataRecordToValueTypeOrString<TResult>(sourceType, conversionType);
            }

            return ByIDataRecordToObject<TResult>(sourceType, conversionType);
        }

        protected virtual Func<object, TResult> ByIDataRecordToAnonymous<TResult>(Type sourceType, Type conversionType)
        {
            var typeStore = RuntimeTypeCache.Instance.GetCache(conversionType);

            var commonCtor = typeStore.ConstructorStores
                .Where(x => x.CanRead)
                .OrderBy(x => x.ParameterStores.Count)
                .FirstOrDefault();

            var parameterExp = Parameter(typeof(object), "source");

            var nullCst = Constant(null);

            var valueExp = Variable(sourceType, "value");

            var indexExp = Variable(typeof(int), "index");

            var negativeExp = Constant(-1);

            var errorExp = Parameter(typeof(Exception), "e");

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

            var getOrdinal = sourceType.GetMethod("GetOrdinal", new Type[] { typeof(string) });

            var isDBNull = sourceType.GetMethod("IsDBNull", new Type[] { typeof(int) });

            var convertMethod = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });

            var list = new List<Expression> { Assign(valueExp, Convert(parameterExp, sourceType)) };

            var variables = new List<ParameterExpression> { valueExp, indexExp };

            var arguments = new List<Expression>();

            commonCtor.ParameterStores.ForEach(info => Config(info));

            list.Add(New(commonCtor.Member, arguments));

            var lamdaExp = Lambda<Func<object, TResult>>(Block(variables, list), parameterExp);

            return lamdaExp.Compile();

            void Config(ParameterStoreItem info)
            {
                var memberType = info.ParameterType;

                list.Add(Assign(indexExp, Call(valueExp, getOrdinal, Constant(info.Name))));

                var testExp = GreaterThan(indexExp, negativeExp);

                if (memberType.IsValueType)
                {
                    if (memberType.IsNullable())
                    {
                        memberType = Nullable.GetUnderlyingType(memberType);
                    }
                    else
                    {
                        if (memberType.IsEnum)
                        {
                            memberType = Enum.GetUnderlyingType(memberType);
                        }

                        testExp = AndAlso(testExp, Not(Call(valueExp, isDBNull, indexExp)));
                    }
                }

                var nameExp = Variable(info.ParameterType, info.Name.ToCamelCase());

                Expression objExp = Call(valueExp, typeMap[typeof(object)], indexExp);

                if (typeMap.TryGetValue(memberType, out MethodInfo methodInfo))
                {
                    objExp = TryCatch(Call(valueExp, methodInfo, indexExp), Catch(errorExp, Convert(Call(null, convertMethod, objExp, Constant(memberType)), memberType)));
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

                if (memberType == info.ParameterType)
                {
                    list.Add(IfThenElse(testExp, Assign(nameExp, objExp), Assign(nameExp, defaultExp)));
                }
                else
                {
                    list.Add(IfThenElse(testExp, Assign(nameExp, Convert(objExp, info.ParameterType)), Assign(nameExp, defaultExp)));
                }

                variables.Add(nameExp);
                arguments.Add(nameExp);
            }
        }

        protected virtual Func<object, TResult> ByIDataRecordToByCommon<TResult>(Type sourceType, Type conversionType)
        {
            var method = ServiceCtor.Method;

            Expression bodyExp = Convert(method.IsStatic
                ? Call(null, method, Constant(conversionType))
                : Call(Constant(ServiceCtor.Target), method, Constant(conversionType))
                , conversionType);

            var list = new List<Expression>();

            var typeStore = RuntimeTypeCache.Instance.GetCache(conversionType);

            var parameterExp = Parameter(typeof(object), "source");

            var nullCst = Constant(null);

            var valueExp = Variable(sourceType, "value");

            var targetExp = Variable(conversionType, "target");

            var indexExp = Variable(typeof(int), "index");

            var negativeExp = Constant(-1);

            var errorExp = Parameter(typeof(Exception), "e");

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

            var getOrdinal = sourceType.GetMethod("GetOrdinal", new Type[] { typeof(string) });

            var isDBNull = sourceType.GetMethod("IsDBNull", new Type[] { typeof(int) });

            var convertMethod = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });

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

            var lamdaExp = Lambda<Func<object, TResult>>(Block(new[] { valueExp, indexExp, targetExp }, list), parameterExp);

            return lamdaExp.Compile();

            void Config<T>(StoreItem<T> info, Expression left) where T : MemberInfo
            {
                var memberType = info.MemberType;

                list.Add(Assign(indexExp, Call(valueExp, getOrdinal, Constant(info.Name))));

                var testExp = GreaterThan(indexExp, negativeExp);

                if (memberType.IsValueType)
                {
                    if (memberType.IsNullable())
                    {
                        memberType = Nullable.GetUnderlyingType(memberType);
                    }
                    else
                    {
                        if (memberType.IsEnum)
                        {
                            memberType = Enum.GetUnderlyingType(memberType);
                        }

                        testExp = AndAlso(testExp, Not(Call(valueExp, isDBNull, indexExp)));
                    }
                }

                Expression objExp = Call(valueExp, typeMap[typeof(object)], indexExp);

                if (typeMap.TryGetValue(memberType, out MethodInfo methodInfo))
                {
                    objExp = TryCatch(Call(valueExp, methodInfo, indexExp), Catch(errorExp, Convert(Call(null, convertMethod, objExp, Constant(memberType)), memberType)));
                }
                else
                {
                    objExp = Convert(objExp, memberType);
                }

                if (memberType == info.MemberType)
                {
                    list.Add(IfThen(testExp, Assign(left, objExp)));
                }
                else
                {
                    list.Add(IfThen(testExp, Assign(left, Convert(objExp, info.MemberType))));
                }
            }
        }

        protected virtual Func<object, TResult> ByIDataRecordToObject<TResult>(Type sourceType, Type conversionType)
        {
            if (conversionType.IsClass && conversionType.IsGenericType && conversionType.Name.StartsWith("<>"))
                return ByIDataRecordToAnonymous<TResult>(sourceType, conversionType);

            return ByIDataRecordToByCommon<TResult>(sourceType, conversionType);
        }

        #endregion

        protected override Func<object, TResult> ByEnumarableToEnumarable<TResult>(Type sourceType, Type conversionType, Type genericType)
        => ByEnumarableToCollection<TResult>(sourceType, conversionType, genericType);

        protected override Func<object, TResult> ByEnumarableToCollection<TResult>(Type sourceType, Type conversionType, Type genericType)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeof(MapToExpression).GetMethod(nameof(ByEnumarableToList), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(genericType);

            var bodyExp = Call(null, methodG, Convert(parameterExp, typeof(IEnumerable)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(Convert(bodyExp, conversionType), parameterExp);

            return lamdaExp.Compile();
        }

        protected override Func<object, TResult> ByEnumarableToCollectionLike<TResult>(Type sourceType, Type conversionType, Type genericType)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeof(MapToExpression).GetMethod(nameof(ByEnumarableToCollectionLike), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(genericType, conversionType);

            var bodyExp = Call(null, methodG, Convert(parameterExp, typeof(IEnumerable)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        protected override Func<object, TResult> ByObjectToEnumarable<TResult>(Type sourceType, Type conversionType, Type genericType)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeof(MapToExpression).GetMethod(nameof(ByObjectToList), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(genericType);

            var bodyExp = Call(null, methodG, parameterExp, Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(Convert(bodyExp, conversionType), parameterExp);

            return lamdaExp.Compile();
        }

        protected override Func<object, TResult> ByObjectToEnumarableKeyValue<TResult>(Type sourceType, Type conversionType, Type genericType, Type[] typeArguments)
        {
            var typeStore = RuntimeTypeCache.Instance.GetCache(sourceType);

            var list = new List<Expression>();

            var targetType = typeof(Dictionary<,>).MakeGenericType(typeArguments);

            var targetExp = Variable(sourceType, "target");

            var sourceExp = Parameter(typeof(object), "source");


            var resultExp = Variable(targetType, "result");

            var method = targetType.GetMethod("Add", typeArguments);

            list.Add(Assign(targetExp, Convert(sourceExp, sourceType)));

            var methodCtor = ServiceCtor.Method;

            var bodyExp = methodCtor.IsStatic ?
                Call(null, methodCtor, Constant(targetType)) :
                Call(Constant(ServiceCtor.Target), methodCtor, Constant(targetType));

            list.Add(Assign(resultExp, Convert(bodyExp, targetType)));

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

        protected override Func<object, TResult> ByObjectToCollectionLike<TResult>(Type sourceType, Type conversionType, Type genericType)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeof(MapToExpression).GetMethod(nameof(ByObjectToCollectionLike), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(genericType, conversionType);

            var bodyExp = Call(null, methodG, parameterExp, Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        protected override Func<object, TResult> ByObjectToCollectionKeyValueLike<TResult>(Type sourceType, Type conversionType, Type genericType, Type[] typeArguments)
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

            if (method is null)
            {
                var methodKv = conversionType.GetMethod("Add", new Type[] { genericType }) ?? throw new NotSupportedException();

                var typeStore2 = RuntimeTypeCache.Instance.GetCache(genericType);

                var ctorSotre = typeStore2.ConstructorStores.Where(x => x.ParameterStores.Count == 2).First();

                if (Kind == PatternKind.Property || Kind == PatternKind.All)
                {
                    typeStore.PropertyStores.Where(x => x.CanRead).ForEach(info =>
                    {
                        list.Add(Call(resultExp, methodKv, New(ctorSotre.Member, ConvertConfig(Constant(info.Name), typeArguments[0]), ConvertConfig(Property(targetExp, info.Member), typeArguments[1]))));
                    });
                }

                if (Kind == PatternKind.Field || Kind == PatternKind.All)
                {
                    typeStore.FieldStores.Where(x => x.CanRead).ForEach(info =>
                    {
                        list.Add(Call(resultExp, methodKv, New(ctorSotre.Member, ConvertConfig(Constant(info.Name), typeArguments[0]), ConvertConfig(Field(targetExp, info.Member), typeArguments[1]))));
                    });
                }
            }
            else
            {
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
