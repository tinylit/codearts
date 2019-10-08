using SkyBuilding.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace SkyBuilding.Implements
{
    /// <summary>
    /// 配置
    /// </summary>
    public class CastToExpression : ProfileExpression<CastToExpression>, ICastToExpression, IProfile
    {
        /// <summary>
        /// 对象转换
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="obj">源数据</param>
        /// <param name="def">默认值</param>
        /// <returns></returns>
        public T CastTo<T>(object obj, T def = default)
        {
            if (obj == null) return def;

            try
            {
                var value = UnsafeCastTo<T>(obj);

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
        /// 对象转换（不安全）
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="obj">数据源</param>
        /// <returns></returns>
        private T UnsafeCastTo<T>(object obj)
        {
            var invoke = Create<T>(obj.GetType());

            return invoke.Invoke(obj);
        }

        /// <summary>
        /// 对象转换
        /// </summary>
        /// <param name="obj">源数据</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        public object CastTo(object obj, Type conversionType)
        {
            if (conversionType is null)
                throw new ArgumentNullException(nameof(conversionType));

            if (obj == null) return null;

            var invoke = Create(obj.GetType(), conversionType);

            try
            {
                return invoke.Invoke(obj);
            }
            catch
            {
                return null;
            }
        }

        #region 反射使用

        private static MethodInfo GetMethodInfo<T>(Func<object, T> func) => func.Method;

        private static MethodInfo GetMethodInfo<T>(Func<object, Type, T> func) => func.Method;

        private static IEnumerable ByObjectToEnumerable(object source)
        {
            yield return source;
        }

        private static IEnumerable<T> ByEnumarableToEnumarable<T>(IEnumerable source, CastToExpression castTo)
        {
            foreach (var item in source)
            {
                yield return castTo.UnsafeCastTo<T>(item);
            }
        }

        private static List<T> ByEnumarableToList<T>(IEnumerable source, CastToExpression castTo)
        => ByEnumarableToCollectionLike<T, List<T>>(source, castTo);

        private static TResult ByEnumarableToCollectionLike<T, TResult>(IEnumerable source, CastToExpression castTo) where TResult : ICollection<T>
        {
            var results = Activator.CreateInstance<TResult>();

            foreach (var item in source)
            {
                results.Add(castTo.UnsafeCastTo<T>(item));
            }

            return results;
        }

        private static List<T> ByObjectToList<T>(object source, CastToExpression castTo)
        => ByObjectToCollectionLike<T, List<T>>(source, castTo);

        private static TResult ByObjectToCollectionLike<T, TResult>(object source, CastToExpression castTo) where TResult : ICollection<T>
        {
            var value = castTo.UnsafeCastTo<T>(source);

            var results = Activator.CreateInstance<TResult>();

            results.Add(value);

            return results;
        }

        #endregion

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
            var invoke = Create(sourceType, genericType);

            return source => (TResult)invoke.Invoke(source);
        }

        /// <summary>
        /// 值类型转目标类型
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByValueType<TResult>(Type sourceType, Type conversionType)
        {
            if (conversionType.IsValueType)
            {
                if (typeof(IConvertible).IsAssignableFrom(sourceType))
                {
                    return source => (TResult)System.Convert.ChangeType(source, conversionType);
                }
            }

            return ByCommonCtor<TResult>(sourceType, conversionType);
        }

        /// <summary>
        /// 源类型转值类型
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected override Func<object, TResult> ToValueType<TResult>(Type sourceType, Type conversionType)
        {
            if (sourceType == typeof(string))
            {
                if (conversionType.IsEnum)
                    return source => (TResult)Enum.Parse(conversionType, source.ToString(), true);

                if (conversionType == typeof(Guid) || conversionType == typeof(Version))
                    return source => (TResult)Activator.CreateInstance(conversionType, source);
            }

            if (typeof(IConvertible).IsAssignableFrom(sourceType))
            {
                return source => (TResult)System.Convert.ChangeType(source, conversionType);
            }

            return ByCommonCtor<TResult>(sourceType, conversionType);
        }

        /// <summary>
        /// 对象转可迭代类型
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToEnumarable<TResult>(Type sourceType, Type conversionType)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = GetMethodInfo(ByObjectToEnumerable);

            var bodyExp = Call(method, parameterExp);

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 对象转可迭代类型
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="genericType">泛型参数</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToEnumarable<TResult>(Type sourceType, Type conversionType, Type genericType)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeof(CastToExpression).GetMethod(nameof(ByObjectToList), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(genericType);

            var bodyExp = Call(methodG, parameterExp, Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(Convert(bodyExp, conversionType), parameterExp);

            return lamdaExp.Compile();
        }

        protected override Func<object, TResult> ByObjectToCollectionLike<TResult>(Type sourceType, Type conversionType, Type genericType)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeof(CastToExpression).GetMethod(nameof(ByObjectToCollectionLike), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(genericType, conversionType);

            var bodyExp = Call(methodG, parameterExp, Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 对象转普通数据
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToCommon<TResult>(Type sourceType, Type conversionType)
        {
            if (conversionType == typeof(IEnumerable))
            {
                return ByObjectToEnumarable<TResult>(sourceType, conversionType);
            }

            return ByCommonCtor<TResult>(sourceType, conversionType);
        }

        /// <summary>
        /// 可迭代类型转可迭代类型
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="genericType">泛型参数</param>
        /// <returns></returns>

        protected override Func<object, TResult> ByEnumarableToEnumarable<TResult>(Type sourceType, Type conversionType, Type genericType)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeof(CastToExpression).GetMethod(nameof(ByEnumarableToEnumarable), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(genericType);

            var bodyExp = Call(methodG, Convert(parameterExp, typeof(IEnumerable)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 可迭代类型转集合
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="genericType">泛型参数</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByEnumarableToCollection<TResult>(Type sourceType, Type conversionType, Type genericType)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeof(CastToExpression).GetMethod(nameof(ByEnumarableToList), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(genericType);

            var bodyExp = Call(methodG, Convert(parameterExp, typeof(IEnumerable)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(Convert(bodyExp, conversionType), parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 可迭代类型转集合
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <param name="genericType">泛型参数</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByEnumarableToCollectionLike<TResult>(Type sourceType, Type conversionType, Type genericType)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeof(CastToExpression).GetMethod(nameof(ByEnumarableToCollectionLike), BindingFlags.NonPublic | BindingFlags.Static);

            var methodG = method.MakeGenericMethod(genericType, conversionType);

            var bodyExp = Call(methodG, Convert(parameterExp, typeof(IEnumerable)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 可迭代类型转普通类型
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByEnumarableToCommon<TResult>(Type sourceType, Type conversionType) => ByCommonCtor<TResult>(sourceType, conversionType);

        /// <summary>
        /// 通过普通类型构造函数
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByCommonCtor<TResult>(Type sourceType, Type conversionType)
        {
            var typeStore = RuntimeTypeCache.Instance.GetCache(conversionType);

            //! 绝对匹配
            foreach (var item in typeStore.ConstructorStores)
            {
                if (item.ParameterStores.Any(x => x.ParameterType == sourceType || x.IsOptional))
                {
                    return ByLikeCtor<TResult>(item, sourceType);
                }
            }

            //? 有继承关系
            foreach (var item in typeStore.ConstructorStores)
            {
                if (item.ParameterStores.Any(x => x.ParameterType.IsAssignableFrom(sourceType) || x.IsOptional))
                {
                    return ByLikeCtor<TResult>(item, sourceType);
                }
            }

            return ByCommonCtor<TResult>(typeStore.ConstructorStores, sourceType, conversionType);
        }

        /// <summary>
        /// 通过相似对象构造函数
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="storeItem">构造函数</param>
        /// <param name="sourceType">源类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeCtor<TResult>(ConstructorStoreItem storeItem, Type sourceType)
        {
            var parameterExp = Parameter(typeof(object));

            var convertUnary = Convert(parameterExp, sourceType);

            List<Expression> list = new List<Expression>();

            storeItem.ParameterStores.ForEach(info =>
            {
                if (info.ParameterType == sourceType)
                {
                    list.Add(convertUnary);
                }
                else if (info.ParameterType.IsAssignableFrom(sourceType))
                {
                    list.Add(Convert(parameterExp, sourceType));
                }
                else
                {
                    list.Add(Constant(info.DefaultValue));
                }
            });

            var newExp = New(storeItem.Member, list);

            var lamda = Lambda<Func<object, TResult>>(Block(convertUnary, newExp), parameterExp);

            return lamda.Compile();
        }

        /// <summary>
        /// 通过对象构造函数
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="storeItem">构造函数</param>
        /// <param name="sourceType">源类型</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByCommonCtor<TResult>(IEnumerable<ConstructorStoreItem> storeItem, Type sourceType, Type conversionType)
        {
            Expression bodyExp = null;

            var nullCst = Constant(null);

            var thisCst = Constant(this);

            var defaultCst = Default(conversionType);

            var resultVar = Variable(typeof(object), "result");

            var paramterExp = Parameter(typeof(object), "source");

            var castToMethod = GetMethodInfo(CastTo); //typeof(ObjectExtentions).GetMethod("CastTo", new Type[] { typeof(object), typeof(Type) });

            if (sourceType.IsValueType)
            {
                storeItem.Where(x => x.ParameterStores.All(y => y.IsOptional || y.ParameterType.IsValueType || y.ParameterType == typeof(string) || y.ParameterType == typeof(Version))).ForEach(info =>
                {
                    var paramterStore = info.ParameterStores.First();

                    if (paramterStore.IsOptional && info.ParameterStores.Count > 1) return;

                    var parameterType = paramterStore.ParameterType;

                    var list = new List<Expression>() { Convert(resultVar, parameterType) };

                    info.ParameterStores.Skip(1).ForEach(y =>
                    {
                        list.Add(Convert(Constant(y.DefaultValue), y.ParameterType));
                    });

                    var methodCallExp = Call(thisCst, castToMethod, paramterExp, Constant(parameterType));

                    var newExp = New(info.Member, list);

                    var conditionExp = Condition(Equal(resultVar, nullCst), bodyExp ?? defaultCst, newExp);

                    bodyExp = Block(Assign(resultVar, methodCallExp), conditionExp);
                });
            }

            storeItem.Where(x => x.ParameterStores.All(y => y.IsOptional || y.ParameterType.IsGenericType && !y.ParameterType.IsValueType)).ForEach(info =>
            {
                var paramterStore = info.ParameterStores.FirstOrDefault();

                if (paramterStore is null || paramterStore.IsOptional && (sourceType.IsValueType || info.ParameterStores.Count > 1)) return;

                var parameterType = paramterStore.ParameterType;

                var generArgs = parameterType.GetGenericArguments();

                if (generArgs.Length > 1) return;

                var genericType = generArgs.First();

                if (!(genericType.IsValueType || genericType == typeof(string) || genericType == typeof(Version))) return;

                var methodCallExp = Call(thisCst, castToMethod, paramterExp, Constant(parameterType));

                var list = new List<Expression>() { Convert(resultVar, parameterType) };

                info.ParameterStores.Skip(1).ForEach(y =>
                {
                    list.Add(Convert(Constant(y.DefaultValue), y.ParameterType));
                });

                var newExp = New(info.Member, list);

                var conditionExp = Condition(Equal(resultVar, nullCst), bodyExp ?? defaultCst, newExp);

                bodyExp = Block(Assign(resultVar, methodCallExp), conditionExp);
            });

            if (bodyExp == null)
                return source => (TResult)source;

            var lamda = Lambda<Func<object, TResult>>(Block(new[] { resultVar }, bodyExp), paramterExp);

            return lamda.Compile();
        }
    }
}
