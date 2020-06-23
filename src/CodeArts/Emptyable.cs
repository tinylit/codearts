using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace CodeArts
{
    /// <summary>
    /// 可空能力
    /// </summary>
    public static class Emptyable
    {
        private static readonly Dictionary<Type, Type> ImplementCache = new Dictionary<Type, Type>();

        private static readonly ConcurrentDictionary<Type, Func<object>> EmptyCache = new ConcurrentDictionary<Type, Func<object>>();

        /// <summary>
        /// 注册类型解决方案
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="valueFactory">生成值的工厂</param>
        public static void Register<T>(Func<T> valueFactory)
        {
            if (valueFactory is null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }

            EmptyCache.TryAdd(typeof(T), () => valueFactory.Invoke());
        }

        /// <summary>
        /// 注册类型解决方案
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <typeparam name="TImplement">实现类型</typeparam>
        public static void Register<T, TImplement>() where T : class where TImplement : T
        {
            var type = typeof(T);

            if (type.IsGenericType && type.IsGenericTypeDefinition)
            {
                ImplementCache.TryAdd(type, typeof(TImplement));
            }

            EmptyCache.TryAdd(type, () => Empty<TImplement>());
        }

        private static Type MakeGenericType(Type interfaceType)
        {
            var typeDefinition = interfaceType.GetGenericTypeDefinition();

            if (ImplementCache.TryGetValue(interfaceType, out Type conversionType))
            {
                return conversionType.MakeGenericType(interfaceType.GetGenericArguments());
            }

            if (typeDefinition == typeof(IEnumerable<>) || typeDefinition == typeof(ICollection<>) || typeDefinition == typeof(IList<>)
#if !NET40
                        || typeDefinition == typeof(IReadOnlyCollection<>) || typeDefinition == typeof(IReadOnlyList<>)
#endif
                        )
            {
                return typeof(List<>).MakeGenericType(interfaceType.GetGenericArguments());
            }

            if (typeDefinition == typeof(IDictionary<,>)
#if !NET40
                        || typeDefinition == typeof(IReadOnlyDictionary<,>)
#endif
                        )
            {
                return typeof(Dictionary<,>).MakeGenericType(interfaceType.GetGenericArguments());
            }

            throw new NotImplementedException($"指定类型({interfaceType.FullName})不被支持!");
        }

        /// <summary>
        /// 空对象
        /// </summary>
        /// <param name="typeEmpty">类型</param>
        /// <returns></returns>
        public static object Empty(Type typeEmpty)
        {
            if (typeEmpty.IsValueType)
            {
                return Activator.CreateInstance(typeEmpty);
            }

            if (typeEmpty == typeof(string))
            {
                return string.Empty;
            }

            if (EmptyCache.TryGetValue(typeEmpty, out Func<object> valueFactory))
            {
                return valueFactory.Invoke();
            }

            if (typeEmpty.IsInterface)
            {
                if (typeEmpty.IsGenericType)
                {
                    var conversionType = MakeGenericType(typeEmpty);

                    if (EmptyCache.TryGetValue(conversionType, out valueFactory))
                    {
                        return valueFactory.Invoke();
                    }

                    return Activator.CreateInstance(conversionType);
                }

                throw new NotImplementedException($"指定类型({typeEmpty.FullName})不被支持!");
            }

            try
            {
                return Activator.CreateInstance(typeEmpty, true);
            }
            catch { }

            valueFactory = EmptyCache.GetOrAdd(typeEmpty, type =>
            {
                var typeCache = RuntimeTypeCache.Instance.GetCache(type);

                var itemCtor = typeCache.ConstructorStores
                      .OrderBy(x => x.ParameterStores.Count)
                      .First();

                return () => itemCtor.Member.Invoke(itemCtor.ParameterStores.Select(x =>
                {
                    if (x.IsOptional)
                    {
                        return x.DefaultValue;
                    }

                    return Empty(x.ParameterType);
                }).ToArray());
            });

            return valueFactory.Invoke();
        }

        /// <summary>
        /// 空对象
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
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
