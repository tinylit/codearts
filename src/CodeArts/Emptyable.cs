using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace CodeArts
{
    /// <summary>
    /// 可空能力。
    /// </summary>
    public static class Emptyable
    {
        private static readonly ConcurrentDictionary<Type, Type> ImplementCache = new ConcurrentDictionary<Type, Type>();

        private static readonly ConcurrentDictionary<Type, Type> TypeDefinitionCache = new ConcurrentDictionary<Type, Type>();

        private static readonly ConcurrentDictionary<Type, object> DefaultCache = new ConcurrentDictionary<Type, object>();

        private static readonly ConcurrentDictionary<Type, Func<object>> EmptyCache = new ConcurrentDictionary<Type, Func<object>>();

        private static readonly MethodInfo EmptyMethodInfo = typeof(Emptyable).GetMethod(nameof(Empty), new Type[] { typeof(Type) });

        /// <summary>
        /// 注册类型解决方案。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <param name="valueFactory">生成值的工厂。</param>
        public static void Register<T>(Func<T> valueFactory) where T : class
        {
            if (valueFactory is null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }

            EmptyCache.TryAdd(typeof(T), () => valueFactory.Invoke());
        }

        /// <summary>
        /// 注册类型解决方案。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <typeparam name="TImplement">实现类型。</typeparam>
        public static void Register<T, TImplement>() where T : class where TImplement : T => ImplementCache.TryAdd(typeof(T), typeof(TImplement));

        /// <summary>
        /// 注册解决方案。
        /// </summary>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">实现类型。</param>
        public static void Register(Type sourceType, Type conversionType)
        {
            if (sourceType is null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            if (conversionType is null)
            {
                throw new ArgumentNullException(nameof(conversionType));
            }

            if (sourceType.IsGenericType && sourceType.IsGenericTypeDefinition)
            {
                if (!IsIsAssignableFrom(sourceType, conversionType))
                {
                    throw new InvalidCastException($"{conversionType.FullName}不是{sourceType.FullName}的派生类！");
                }

                TypeDefinitionCache.TryAdd(sourceType, conversionType);
            }
            else if (!sourceType.IsAssignableFrom(conversionType))
            {
                throw new InvalidCastException($"{conversionType.FullName}不是{sourceType.FullName}的派生类！");
            }
            else
            {
                ImplementCache.TryAdd(sourceType, conversionType);
            }
        }

        private static bool IsIsAssignableFrom(Type sourceType, Type conversionType)
        {
            if (!conversionType.IsGenericType || !conversionType.IsGenericTypeDefinition)
            {
                return false;
            }

            if (sourceType.IsInterface)
            {
                foreach (var item in conversionType.GetInterfaces())
                {
                    if (item.GetGenericTypeDefinition() == sourceType)
                    {
                        return true;
                    }
                }
                return false;
            }

            do
            {
                conversionType = conversionType.BaseType;

                if (conversionType is null)
                {
                    return false;
                }

                conversionType = conversionType.GetGenericTypeDefinition();

            } while (conversionType != sourceType);

            return true;
        }

        private static Type MakeGenericType(Type interfaceType)
        {
            var typeDefinition = interfaceType.GetGenericTypeDefinition();

            if (TypeDefinitionCache.TryGetValue(typeDefinition, out Type conversionType))
            {
                return conversionType.MakeGenericType(interfaceType.GetGenericArguments());
            }

            if (typeDefinition == typeof(IEnumerable<>) || typeDefinition == typeof(ICollection<>) || typeDefinition == typeof(IList<>)
#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
                        || typeDefinition == typeof(IReadOnlyCollection<>) || typeDefinition == typeof(IReadOnlyList<>)
#endif
                        )
            {
                return typeof(List<>).MakeGenericType(interfaceType.GetGenericArguments());
            }

            if (typeDefinition == typeof(IDictionary<,>)
#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
                        || typeDefinition == typeof(IReadOnlyDictionary<,>)
#endif
                        )
            {
                return typeof(Dictionary<,>).MakeGenericType(interfaceType.GetGenericArguments());
            }

            throw new NotImplementedException($"指定类型({interfaceType.FullName})不被支持!");
        }

        /// <summary>
        /// 空对象。
        /// </summary>
        /// <param name="typeEmpty">类型。</param>
        /// <returns></returns>
        public static object Empty(Type typeEmpty)
        {
            if (typeEmpty.IsValueType)
            {
                return DefaultCache.GetOrAdd(typeEmpty, Activator.CreateInstance);
            }

            if (typeEmpty == typeof(string))
            {
                return string.Empty;
            }

            if (EmptyCache.TryGetValue(typeEmpty, out Func<object> valueFactory))
            {
                return valueFactory.Invoke();
            }

            if (ImplementCache.TryGetValue(typeEmpty, out Type implementType))
            {
                if (EmptyCache.TryGetValue(implementType, out valueFactory))
                {
                    return valueFactory.Invoke();
                }

                typeEmpty = implementType;
            }

            if (typeEmpty.IsInterface || typeEmpty.IsAbstract)
            {
                if (typeEmpty.IsGenericType)
                {
                    return Empty(ImplementCache.GetOrAdd(typeEmpty, conversionType => MakeGenericType(conversionType)));
                }

                throw new NotImplementedException($"指定类型({typeEmpty.FullName})不被支持!");
            }

            valueFactory = EmptyCache.GetOrAdd(typeEmpty, type =>
            {
                foreach (var constructorInfo in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
                {
                    return DoneCreateInstance(constructorInfo);
                }

                foreach (var constructorInfo in type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    return DoneCreateInstance(constructorInfo);
                }

                throw new NotSupportedException($"未找到“{type.FullName}”的任何有效构造函数！");
            });

            return valueFactory.Invoke();
        }

        private static Func<object> DoneCreateInstance(ConstructorInfo constructorInfo)
        {
            var parameters = constructorInfo.GetParameters();

            if (parameters.Length == 0)
            {
                var lambda = Lambda<Func<object>>(New(constructorInfo));

                return lambda.Compile();
            }
            else
            {
                var expressions = new Expression[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameterInfo = parameters[i];

                    if (parameterInfo.IsOptional)
                    {
                        expressions[i] = Constant(parameterInfo.DefaultValue);
                    }
                    else if (parameterInfo.ParameterType.IsValueType)
                    {
                        expressions[i] = Default(parameterInfo.ParameterType);
                    }
                    else
                    {
                        expressions[i] = Call(null, EmptyMethodInfo, Constant(parameterInfo.ParameterType));
                    }
                }

                var lambda = Lambda<Func<object>>(New(constructorInfo, expressions));

                return lambda.Compile();
            }
        }

        /// <summary>
        /// 空对象。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <returns></returns>
        public static T Empty<T>()
        {
            var type = typeof(T);

            if (type.IsValueType)
            {
                return default;
            }

            return (T)Empty(type);
        }
    }
}
