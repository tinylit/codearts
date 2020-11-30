using CodeArts.ORM.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CodeArts.ORM
{
    /// <summary>
    /// 数据库链接管理器。
    /// </summary>
    public static class DbConnectionManager
    {
        private static readonly Dictionary<string, Func<IDbConnectionAdapter, RepositoryProvider>> FactoryProviders;

        private static readonly ConcurrentDictionary<string, IDbConnectionAdapter> Adapters;

        private static readonly ConcurrentDictionary<IDbConnectionAdapter, RepositoryProvider> Providers;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static DbConnectionManager()
        {
            Providers = new ConcurrentDictionary<IDbConnectionAdapter, RepositoryProvider>();
            Adapters = new ConcurrentDictionary<string, IDbConnectionAdapter>();
            FactoryProviders = new Dictionary<string, Func<IDbConnectionAdapter, RepositoryProvider>>();
        }

        /// <summary> 注册适配器。</summary>
        /// <param name="adapter">适配器。</param>
        public static void RegisterAdapter(IDbConnectionAdapter adapter)
        {
            if (adapter is null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }

            if (string.IsNullOrWhiteSpace(adapter.ProviderName))
            {
                throw new DException("数据库适配器名称不能为空!");
            }

            Adapters.AddOrUpdate(adapter.ProviderName.ToLower(), adapter, (_, _2) => adapter);
        }

        /// <summary>
        /// 为当前所有为指定仓库供应器的适配器添加此仓库供应器。
        /// </summary>
        public static void RegisterProvider<T>() where T : RepositoryProvider
        {
            foreach (string key in Adapters.Keys)
            {
                RegisterProvider<T>(key);
            }
        }

        /// <summary>
        /// 为数据库适配器指定仓库提供者。
        /// </summary>
        /// <param name="providerName">供应商名称。</param>
        /// <exception cref="ArgumentException">providerName不能为Null或空字符串！</exception>
        /// <exception cref="NotImplementedException">供应器类型必须包含公共构造函数！</exception>
        public static void RegisterProvider<T>(string providerName) where T : RepositoryProvider
        {
            if (string.IsNullOrEmpty(providerName))
            {
                throw new ArgumentException("数据库适配器名称不能为空", nameof(providerName));
            }

            var instanceType = typeof(T);
            var constructors = instanceType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);

            if (constructors.Length == 0)
            {
                throw new NotImplementedException("数据仓促必须提供公共构造函数!");
            }

            string key = providerName.ToLower();

            if (constructors.Any(x => x.GetParameters().Length == 0))
            {
                FactoryProviders[key] = adapter => Activator.CreateInstance<T>();

                return;
            }

            var constructor = constructors.OrderBy(x => x.GetParameters().Length).First();

            var parameters = constructor.GetParameters();

            var list = new List<Func<IDbConnectionAdapter, object>>();

            parameters.ForEach(item =>
            {
                if (typeof(ISQLCorrectSimSettings).IsAssignableFrom(item.ParameterType))
                {
                    list.Add(adapter => adapter.Settings);
                }
                else if (typeof(IDbConnectionAdapter).IsAssignableFrom(item.ParameterType))
                {
                    list.Add(adapter => adapter);
                }
                else if (item.ParameterType == typeof(string))
                {
                    list.Add(adapter => adapter.ProviderName);
                }
                else if (item.IsOptional)
                {
                    list.Add(adapter => item.DefaultValue);
                }
                else
                {
                    throw new NotSupportedException($"数据仓库公共构造函数参数类型（{item.ParameterType.FullName}）不被支持(仅支持【string】、【ISQLCorrectSettings】、【IDbConnectionAdapter】或可选参数)!");
                }
            });

            FactoryProviders[key] = adapter => (RepositoryProvider)constructor.Invoke(list.ConvertAll(factoty => factoty(adapter)).ToArray());
        }

        /// <summary> 创建数据库适配器。</summary>
        /// <param name="providerName">供应商名称。</param>
        /// <returns></returns>
        public static IDbConnectionAdapter Get(string providerName)
        {
            if (providerName is null)
            {
                throw new ArgumentNullException(nameof(providerName));
            }

            if (string.IsNullOrEmpty(providerName))
            {
                throw new ArgumentException("数据库适配器名称不能为空", nameof(providerName));
            }

            if (Adapters.TryGetValue(providerName.ToLower(), out var adapter))
            {
                return adapter;
            }

            throw new DException($"不支持的适配器：{providerName}");
        }

        /// <summary>
        /// 获取数据仓促供应器。
        /// </summary>
        /// <param name="adapter">适配器。</param>
        /// <returns></returns>
        public static RepositoryProvider Create(IDbConnectionAdapter adapter)
        {
            if (adapter is null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }

            string key = adapter.ProviderName.ToLower();

            if (Providers.TryGetValue(adapter, out RepositoryProvider provider))
            {
                return provider;
            }

            if (FactoryProviders.TryGetValue(key, out Func<IDbConnectionAdapter, RepositoryProvider> factory))
            {
                return Providers.GetOrAdd(adapter, factory.Invoke(adapter));
            }

            throw new DException($"不支持的供应商：{adapter.ProviderName}");
        }
    }
}
