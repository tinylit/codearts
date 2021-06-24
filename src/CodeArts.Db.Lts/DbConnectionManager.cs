using CodeArts.Db.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 数据库链接管理器。
    /// </summary>
    public static class DbConnectionManager
    {
        private static readonly Dictionary<string, Func<IDbConnectionLtsAdapter, IDatabaseFor>> DatabaseFor;

        private static readonly Dictionary<string, IDbConnectionLtsAdapter> Adapters;

        private static readonly ConcurrentDictionary<IDbConnectionLtsAdapter, IDatabaseFor> DatabaseForCache;


        private static readonly MethodInfo ProviderNameMethodInfo = typeof(DbConnectionManager).GetMethod(nameof(GetProviderName), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo SettingsMethodInfo = typeof(DbConnectionManager).GetMethod(nameof(GetSettings), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo VisitorsMethodInfo = typeof(DbConnectionManager).GetMethod(nameof(GetVisitors), BindingFlags.NonPublic | BindingFlags.Static);

        private static string GetProviderName(IDbConnectionLtsAdapter adapter) => adapter.ProviderName;
        private static ISQLCorrectSettings GetSettings(IDbConnectionLtsAdapter adapter) => adapter.Settings;
        private static ICustomVisitorList GetVisitors(IDbConnectionLtsAdapter adapter) => adapter.Visitors;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static DbConnectionManager()
        {
            Adapters = new Dictionary<string, IDbConnectionLtsAdapter>();
            DatabaseFor = new Dictionary<string, Func<IDbConnectionLtsAdapter, IDatabaseFor>>();
            DatabaseForCache = new ConcurrentDictionary<IDbConnectionLtsAdapter, IDatabaseFor>();
        }

        /// <summary> 注册适配器。</summary>
        /// <param name="adapter">适配器。</param>
        /// <exception cref="NotSupportedException">适配器已在运作，禁止修改！</exception>
        public static void RegisterAdapter(IDbConnectionLtsAdapter adapter)
        {
            if (adapter is null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }

            if (string.IsNullOrWhiteSpace(adapter.ProviderName))
            {
                throw new DException("数据库适配器名称不能为空!");
            }

            Adapters[adapter.ProviderName.ToLower()] = adapter;
        }

        /// <summary>
        /// 为当前所有为指定仓库供应器的适配器添加此数据库支持。
        /// </summary>
        public static void RegisterDatabaseFor<T>() where T : IDatabaseFor
        {
            foreach (var providerName in Adapters.Keys)
            {
                RegisterDatabaseFor<T>(providerName);
            }
        }

        /// <summary>
        /// 为当前所有为指定仓库供应器的适配器添加此数据库支持。
        /// </summary>
        public static void RegisterDatabaseFor<T>(string providerName) where T : IDatabaseFor
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

            var constructor = constructors.OrderBy(x => x.GetParameters().Length).First();

            var parameters = constructor.GetParameters();

            var adapterEx = Parameter(typeof(IDbConnectionLtsAdapter), "adapter");

            if (parameters.Length == 0)
            {
                var bodyEx = New(constructor);

                var lambdaEx = Lambda<Func<IDbConnectionLtsAdapter, IDatabaseFor>>(Convert(bodyEx, typeof(IDatabaseFor)), adapterEx);

                DatabaseFor[key] = lambdaEx.Compile();
            }
            else
            {
                var argumentsEx = new List<Expression>(parameters.Length);

                parameters.ForEach(item =>
                {
                    if (typeof(ISQLCorrectSettings).IsAssignableFrom(item.ParameterType))
                    {
                        argumentsEx.Add(Call(null, SettingsMethodInfo, adapterEx));
                    }
                    else if (typeof(ICustomVisitorList).IsAssignableFrom(item.ParameterType))
                    {
                        argumentsEx.Add(Call(VisitorsMethodInfo, adapterEx));
                    }
                    else if (typeof(IDbConnectionLtsAdapter).IsAssignableFrom(item.ParameterType))
                    {
                        argumentsEx.Add(adapterEx);
                    }
                    else if (item.ParameterType == typeof(string))
                    {
                        argumentsEx.Add(Call(ProviderNameMethodInfo, adapterEx));
                    }
                    else if (item.IsOptional)
                    {
                        argumentsEx.Add(Constant(item.DefaultValue));
                    }
                    else
                    {
                        throw new NotSupportedException($"数据仓库公共构造函数参数类型（{item.ParameterType.FullName}）不被支持(仅支持【string】、【ISQLCorrectSettings】、【IDbConnectionAdapter】或可选参数)!");
                    }
                });

                var bodyEx = New(constructor, argumentsEx);

                var lambdaEx = Lambda<Func<IDbConnectionLtsAdapter, IDatabaseFor>>(Convert(bodyEx, typeof(IDatabaseFor)), adapterEx);

                DatabaseFor[key] = lambdaEx.Compile();
            }
        }
        
        /// <summary> 创建数据库适配器。</summary>
        /// <param name="providerName">供应商名称。</param>
        /// <returns></returns>
        public static IDbConnectionLtsAdapter Get(string providerName)
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
        public static IDatabaseFor Create(IDbConnectionLtsAdapter adapter)
        {
            if (adapter is null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }

            if (DatabaseForCache.TryGetValue(adapter, out IDatabaseFor databaseFor))
            {
                return databaseFor;
            }

            if (DatabaseFor.TryGetValue(adapter.ProviderName.ToLower(), out Func<IDbConnectionLtsAdapter, IDatabaseFor> factory))
            {
                return DatabaseForCache.GetOrAdd(adapter, factory.Invoke(adapter));
            }

            throw new DException($"不支持的供应商：{adapter.ProviderName}");
        }
    }
}
