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
    /// 配置。
    /// </summary>
    public class CastToExpression : ProfileExpression<CastToExpression>, ICastToExpression, IProfile
    {
        private static readonly Type typeSelf = typeof(CastToExpression);
        /// <summary>
        /// 对象转换。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="obj">源数据。</param>
        /// <param name="def">默认值。</param>
        /// <returns></returns>
        public T Cast<T>(object obj, T def = default)
        {
            if (obj is null)
            {
                return def;
            }

            try
            {
                var invoke = Create<T>(obj.GetType());

                return invoke.Invoke(obj);
            }
            catch (InvalidCastException)
            {
                return def;
            }
            catch (ApplicationException)
            {
                return def;
            }
        }

        /// <summary>
        /// 对象转换（不安全）。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="obj">数据源。</param>
        /// <returns></returns>
        public T ThrowsCast<T>(object obj)
        {
            if (obj is null)
            {
                if (typeof(T).IsValueType)
                {
                    throw new NullReferenceException();
                }

                return default;
            }

            var invoke = Create<T>(obj.GetType());

            return invoke.Invoke(obj);
        }

        /// <summary>
        /// 对象转换。
        /// </summary>
        /// <param name="obj">源数据。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public object Cast(object obj, Type conversionType)
        {
            if (conversionType is null)
            {
                throw new ArgumentNullException(nameof(conversionType));
            }

            if (obj is null)
            {
                if (conversionType.IsValueType)
                {
                    return Emptyable.Empty(conversionType);
                }

                return null;
            }

            try
            {
                return ThrowsCast(obj, conversionType);
            }
            catch (Exception)
            {
                if (conversionType.IsValueType)
                {
                    return Emptyable.Empty(conversionType);
                }

                return null;
            }
        }

        /// <summary>
        /// 对象转换。
        /// </summary>
        /// <param name="obj">源数据。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public object ThrowsCast(object obj, Type conversionType)
        {
            if (conversionType is null)
            {
                throw new ArgumentNullException(nameof(conversionType));
            }

            if (obj is null)
            {
                if (conversionType.IsValueType)
                {
                    throw new NullReferenceException();
                }

                return null;
            }

            var invoke = Create(obj.GetType(), conversionType);

            return invoke.Invoke(obj);
        }

        #region 反射使用

        private static MethodInfo GetMethodInfo<T>(Func<object, T> func) => func.Method;

        private static MethodInfo GetMethodInfo(Func<object, Type, object> func) => func.Method;

        private static IEnumerable ByObjectToEnumerable(object source)
        {
            yield return source;
        }

        private static IEnumerable<T> ByEnumarableToEnumarable<T>(IEnumerable source, CastToExpression castTo)
        {
            foreach (var item in source)
            {
                yield return castTo.ThrowsCast<T>(item);
            }
        }

        private static List<T> ByEnumarableToList<T>(IEnumerable source, CastToExpression castTo)
        => ByEnumarableToCollectionLike<T, List<T>>(source, castTo);

        private static TResult ByEnumarableToCollectionLike<T, TResult>(IEnumerable source, CastToExpression castTo) where TResult : ICollection<T>
        {
            var results = Activator.CreateInstance<TResult>();

            foreach (var item in source)
            {
                results.Add(castTo.ThrowsCast<T>(item));
            }

            return results;
        }

        private static List<T> ByObjectToList<T>(object source, CastToExpression castTo)
        => ByObjectToCollectionLike<T, List<T>>(source, castTo);

        private static TResult ByObjectToCollectionLike<T, TResult>(object source, CastToExpression castTo) where TResult : ICollection<T>
        {
            var value = castTo.ThrowsCast<T>(source);

            var results = Activator.CreateInstance<TResult>();

            results.Add(value);

            return results;
        }

        #endregion

        private static Func<object, object> CancelClosure(Func<object, object> invoke) => source => invoke.Invoke(source);

        /// <summary>
        /// 解决 任意类型 到 可空类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型。</typeparam>
        /// <param name="sourceType">源数据类型。</param>
        /// <param name="conversionType">目标数据类型。</param>
        /// <param name="typeArgument">泛型约束。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ToNullable<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            if (sourceType == typeArgument)
            {
                var parameterExp = Parameter(typeof(object), "source");

                var typeStore = TypeItem.Get(conversionType);

                var ctorInfo = typeStore.ConstructorStores.First(x => x.ParameterStores.Count == 1);

                var bodyExp = New(ctorInfo.Member, Convert(parameterExp, typeArgument));

                var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

                return lamdaExp.Compile();
            }
            else
            {
                Func<object, object> invoke = CancelClosure(Create(sourceType, typeArgument));

                var parameterExp = Parameter(typeof(object), "source");

                var typeStore = TypeItem.Get(conversionType);

                var ctorInfo = typeStore.ConstructorStores.First(x => x.ParameterStores.Count == 1);

                var coreExp = invoke.Method.IsStatic ?
                    Call(null, invoke.Method, parameterExp)
                    :
                    Call(Constant(invoke.Target), invoke.Method, parameterExp);

                var bodyExp = New(ctorInfo.Member, Convert(coreExp, typeArgument));

                var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

                return lamdaExp.Compile();
            }
        }

        /// <summary>
        /// 解决 值类型 到 任意类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByValueType<TResult>(Type sourceType, Type conversionType)
        {
            if (conversionType.IsValueType)
            {
                if (typeof(IConvertible).IsAssignableFrom(sourceType))
                {
                    if (conversionType.IsEnum)
                    {
                        return source => (TResult)Enum.ToObject(conversionType, source);
                    }

                    return source => (TResult)System.Convert.ChangeType(source, conversionType);
                }
            }

            return ByObjectToCommon<TResult>(sourceType, conversionType);
        }

        /// <summary>
        /// 解决 任意类型 到 值类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型。</typeparam>
        /// <param name="sourceType">源数据类型。</param>
        /// <param name="conversionType">目标数据类型。</param>
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

            return ByObjectToCommon<TResult>(sourceType, conversionType);
        }

        /// <summary>
        /// 解决 对象 到类似 IEnumerable&lt;T&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型。</typeparam>
        /// <param name="sourceType">源数据类型。</param>
        /// <param name="conversionType">目标数据类型。</param>
        /// <param name="typeArgument">泛型【T】约束。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToEnumerableLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = GetMethodInfo(ByObjectToEnumerable);

            var bodyExp = Call(method, parameterExp);

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 类 到类似 IEnumarable&lt;T&gt; 类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型。</typeparam>
        /// <param name="sourceType">源数据类型。</param>
        /// <param name="conversionType">目标数据类型。</param>
        /// <param name="typeArgument">泛型【T】约束。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToIEnumarableLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(ByObjectToList), BindingFlags.NonPublic | BindingFlags.Static);

            var genericMethod = method.MakeGenericMethod(typeArgument);

            var bodyExp = Call(genericMethod, parameterExp, Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(Convert(bodyExp, conversionType), parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 类 到类似 ICollection&lt;T&gt; 类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型。</typeparam>
        /// <param name="sourceType">源数据类型。</param>
        /// <param name="conversionType">目标数据类型。</param>
        /// <param name="typeArgument">泛型【T】约束。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToICollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(ByObjectToList), BindingFlags.NonPublic | BindingFlags.Static);

            var genericMethod = method.MakeGenericMethod(typeArgument, conversionType);

            var bodyExp = Call(genericMethod, parameterExp, Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T1&gt; 到类似 IEnumarable&lt;T2&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型。</typeparam>
        /// <param name="sourceType">源数据类型。</param>
        /// <param name="conversionType">目标数据类型。</param>
        /// <param name="typeArgument">泛型【T2】约束。</param>
        /// <returns></returns>

        protected override Func<object, TResult> ByIEnumarableLikeToIEnumarableLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(ByEnumarableToEnumarable), BindingFlags.NonPublic | BindingFlags.Static);

            var genericMethod = method.MakeGenericMethod(typeArgument);

            var bodyExp = Call(genericMethod, Convert(parameterExp, typeof(IEnumerable)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(bodyExp, parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T1&gt; 到类似 ICollection&lt;T2&gt; 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型。</typeparam>
        /// <param name="sourceType">源数据类型。</param>
        /// <param name="conversionType">目标数据类型。</param>
        /// <param name="typeArgument">泛型【T2】约束。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByIEnumarableLikeToICollectionLike<TResult>(Type sourceType, Type conversionType, Type typeArgument)
        {
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(ByEnumarableToList), BindingFlags.NonPublic | BindingFlags.Static);

            var genericMethod = method.MakeGenericMethod(typeArgument);

            var bodyExp = Call(genericMethod, Convert(parameterExp, typeof(IEnumerable)), Constant(this));

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
            var parameterExp = Parameter(typeof(object), "source");

            var method = typeSelf.GetMethod(nameof(ByEnumarableToCollectionLike), BindingFlags.NonPublic | BindingFlags.Static);

            var genericMethod = method.MakeGenericMethod(typeArgument, conversionType);

            var bodyExp = Call(genericMethod, Convert(parameterExp, typeof(IEnumerable)), Constant(this));

            var lamdaExp = Lambda<Func<object, TResult>>(Convert(bodyExp, conversionType), parameterExp);

            return lamdaExp.Compile();
        }

        /// <summary>
        /// 解决 IEnumarable&lt;T&gt; 到 未知泛型接口的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="typeArguments">泛型约束。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByIEnumarableLikeToUnknownInterface<TResult>(Type sourceType, Type conversionType, Type[] typeArguments) => ByIEnumarableLikeToCommon<TResult>(sourceType, conversionType);

        /// <summary>
        /// 解决 类似 IEnumarable&lt;T&gt; 到 类 的转换。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型。</typeparam>
        /// <param name="sourceType">源数据类型。</param>
        /// <param name="conversionType">目标数据类型。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByIEnumarableLikeToCommon<TResult>(Type sourceType, Type conversionType) => ByObjectToCommon<TResult>(sourceType, conversionType);

        /// <summary>
        /// 解决 对象 到 任意对象 的操作。
        /// </summary>
        /// <typeparam name="TResult">目标数据类型。</typeparam>
        /// <param name="sourceType">源数据类型。</param>
        /// <param name="conversionType">目标数据类型。</param>
        /// <returns></returns>
        protected override Func<object, TResult> ByObjectToCommon<TResult>(Type sourceType, Type conversionType)
        {
            var typeStore = TypeItem.Get(conversionType);

            //! 绝对匹配
            foreach (var item in typeStore.ConstructorStores)
            {
                if (item.ParameterStores.All(x => x.ParameterType == sourceType || x.IsOptional))
                {
                    return ByLikeCtor<TResult>(item, sourceType, conversionType);
                }
            }

            //? 有继承关系
            foreach (var item in typeStore.ConstructorStores)
            {
                if (item.ParameterStores.All(x => x.ParameterType.IsAssignableFrom(sourceType) || x.IsOptional))
                {
                    return ByLikeCtor<TResult>(item, sourceType, conversionType);
                }
            }

            return ByObjectToCommon<TResult>(typeStore.ConstructorStores, sourceType, conversionType);
        }

        /// <summary>
        /// 解决 对象 到 包含 源类型或源类型父类 的公共无参构造函数的目标类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="storeItem">构造函数。</param>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByLikeCtor<TResult>(ConstructorItem storeItem, Type sourceType, Type conversionType)
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
        /// 解决 对象 到 任意类型的转换。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="storeItem">构造函数。</param>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected virtual Func<object, TResult> ByObjectToCommon<TResult>(IEnumerable<ConstructorItem> storeItem, Type sourceType, Type conversionType)
        {
            Expression bodyExp = null;

            var nullCst = Constant(null);

            var thisCst = Constant(this);

            var defaultCst = Default(conversionType);

            var resultVar = Variable(typeof(object), "result");

            var parameterExp = Parameter(typeof(object), "source");

            var castToMethod = GetMethodInfo(Cast); //typeof(ObjectExtentions).GetMethod("CastTo", new Type[] { typeof(object), typeof(Type) });

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
                        if (y.IsOptional)
                        {
                            list.Add(Convert(Constant(y.DefaultValue), y.ParameterType));
                        }
                        else
                        {
                            list.Add(Convert(Default(y.ParameterType), y.ParameterType));
                        }
                    });

                    var methodCallExp = Call(thisCst, castToMethod, parameterExp, Constant(parameterType));

                    var newExp = New(info.Member, list);

                    var conditionExp = Condition(Equal(resultVar, nullCst), bodyExp ?? defaultCst, newExp);

                    bodyExp = Block(Assign(resultVar, methodCallExp), conditionExp);
                });
            }
            else
            {
                storeItem.Where(x => x.ParameterStores.All(y => y.IsOptional || y.ParameterType.IsGenericType && !y.ParameterType.IsValueType)).ForEach(info =>
                {
                    var paramterStore = info.ParameterStores.FirstOrDefault();

                    if (paramterStore is null || paramterStore.IsOptional && (sourceType.IsValueType || info.ParameterStores.Count > 1)) return;

                    var parameterType = paramterStore.ParameterType;

                    var generArgs = parameterType.GetGenericArguments();

                    if (generArgs.Length > 1) return;

                    var genericType = generArgs.First();

                    if (!(genericType.IsValueType || genericType == typeof(string) || genericType == typeof(Version))) return;

                    var methodCallExp = Call(thisCst, castToMethod, parameterExp, Constant(parameterType));

                    var list = new List<Expression>() { Convert(resultVar, parameterType) };

                    info.ParameterStores.Skip(1).ForEach(y =>
                    {
                        if (y.IsOptional)
                        {
                            list.Add(Convert(Constant(y.DefaultValue), y.ParameterType));
                        }
                        else
                        {
                            list.Add(Convert(Default(y.ParameterType), y.ParameterType));
                        }
                    });

                    var newExp = New(info.Member, list);

                    var conditionExp = Condition(Equal(resultVar, nullCst), bodyExp ?? defaultCst, newExp);

                    bodyExp = Block(Assign(resultVar, methodCallExp), conditionExp);
                });
            }

            if (bodyExp is null)
            {
                return source => (TResult)source;
            }

            var lamda = Lambda<Func<object, TResult>>(Block(new[] { resultVar }, bodyExp), parameterExp);

            return lamda.Compile();
        }
    }
}
