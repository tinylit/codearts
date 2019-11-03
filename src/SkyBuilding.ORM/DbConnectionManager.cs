using SkyBuilding.ORM.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 数据库链接管理器
    /// </summary>
    public static class DbConnectionManager
    {
        private static readonly ConcurrentDictionary<string, Func<IDbConnectionAdapter, RepositoryProvider>> FactoryProviders;

        private static readonly ConcurrentDictionary<string, IDbConnectionAdapter> Adapters;

        private static readonly ConcurrentDictionary<string, RepositoryProvider> Providers;

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static DbConnectionManager()
        {
            Providers = new ConcurrentDictionary<string, RepositoryProvider>();
            Adapters = new ConcurrentDictionary<string, IDbConnectionAdapter>();
            FactoryProviders = new ConcurrentDictionary<string, Func<IDbConnectionAdapter, RepositoryProvider>>();
        }

        /// <summary> 添加适配器 </summary>
        /// <param name="adapter"></param>
        public static void AddAdapter(IDbConnectionAdapter adapter)
        {
            if (adapter == null)
                throw new ArgumentNullException(nameof(adapter));
            if (string.IsNullOrWhiteSpace(adapter.ProviderName))
                throw new DException("数据库适配器名称不能为空!");
            var key = adapter.ProviderName.ToLower();
            if (Adapters.ContainsKey(key))
                return;
            Adapters.TryAdd(key, adapter);
        }

        /// <summary>
        /// 为当前所有为指定仓库供应器的适配器添加此仓库供应器
        /// </summary>
        public static void AddProvider<T>() where T : RepositoryProvider
        {
            foreach (string key in Adapters.Keys)
            {
                AddProvider<T>(key);
            }
        }

        /// <summary>
        /// 为数据库适配器指定仓库提供者
        /// </summary>
        /// <param name="providerName">供应商名称</param>
        /// <exception cref="ArgumentException">providerName不能为Null或空字符串!</exception>
        /// <exception cref="NotImplementedException">供应器类型必须包含公共构造函数!</exception>
        public static void AddProvider<T>(string providerName) where T : RepositoryProvider
        {
            if (string.IsNullOrEmpty(providerName))
            {
                throw new ArgumentException("数据库适配器名称不能为空", nameof(providerName));
            }
            string key = providerName.ToLower();
            if (FactoryProviders.ContainsKey(key))
                return;
            var type = typeof(T);
            var infos = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            if (infos.Length == 0)
            {
                throw new NotImplementedException("数据仓促必须提供公共构造函数!");
            }
            var info = infos.First();
            var parameters = info.GetParameters();
            if (parameters.Length == 0)
            {
                FactoryProviders.TryAdd(key, adapter =>
                {
                    return Activator.CreateInstance<T>();
                });
                return;
            }
            var list = new List<Func<IDbConnectionAdapter, object>>();

            parameters.ForEach(item =>
            {
                if (typeof(ISQLCorrectSettings).IsAssignableFrom(item.ParameterType))
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

            FactoryProviders.TryAdd(key, adapter =>
            {
                var args = list.ConvertAll(factoty => factoty(adapter));

                return (RepositoryProvider)Activator.CreateInstance(type, args.ToArray());
            });
        }

        /// <summary> 创建数据库适配器 </summary>
        /// <param name="providerName"></param>
        /// <returns></returns>
        public static IDbConnectionAdapter Create(string providerName)
        {

            if (providerName == null)
            {
                throw new ArgumentNullException(nameof(providerName));
            }

            if (string.IsNullOrEmpty(providerName))
            {
                throw new ArgumentException("数据库适配器名称不能为空", nameof(providerName));
            }

            if (Adapters.TryGetValue(providerName.ToLower(), out var adapter))
                return adapter;

            throw new DException($"不支持的适配器：{providerName}");
        }

        /// <summary>
        /// 获取数据仓促供应器
        /// </summary>
        /// <param name="adapter"></param>
        /// <returns></returns>
        public static RepositoryProvider Create(IDbConnectionAdapter adapter)
        {
            if (adapter == null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }

            string key = adapter.ProviderName.ToLower();

            if (Providers.TryGetValue(key, out RepositoryProvider provider))
                return provider;

            if (FactoryProviders.TryGetValue(key, out Func<IDbConnectionAdapter, RepositoryProvider> factory))
            {
                return Providers.GetOrAdd(key, _ => factory(adapter));
            }
            throw new DException($"不支持的供应商：{adapter.ProviderName}");
        }
    }
}
