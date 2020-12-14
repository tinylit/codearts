﻿using CodeArts.Db.Exceptions;
using System;
using System.Collections.Concurrent;

namespace CodeArts.Db.EntityFramework
{
    /// <summary>
    /// 数据库链接管理器。
    /// </summary>
    public static class LinqConnectionManager
    {
#if NETSTANDARD2_0
        private static readonly ConcurrentDictionary<string, IDbConnectionLinqAdapter> Adapters;
#else
        private static readonly ConcurrentDictionary<string, IDbConnectionAdapter> Adapters;
#endif

        /// <summary>
        /// 静态构造函数。
        /// </summary>
#if NETSTANDARD2_0
        static LinqConnectionManager() => Adapters = new ConcurrentDictionary<string, IDbConnectionLinqAdapter>();
#else
        static LinqConnectionManager() => Adapters = new ConcurrentDictionary<string, IDbConnectionAdapter>();
#endif

        /// <summary> 注册适配器。</summary>
        /// <param name="adapter">适配器。</param>
#if NETSTANDARD2_0
        public static void RegisterAdapter(IDbConnectionLinqAdapter adapter)
#else
        public static void RegisterAdapter(IDbConnectionAdapter adapter)
#endif
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

        /// <summary> 创建数据库适配器。</summary>
        /// <param name="providerName">供应商名称。</param>
        /// <returns></returns>
#if NETSTANDARD2_0
        public static IDbConnectionLinqAdapter Get(string providerName)
#else
        public static IDbConnectionAdapter Get(string providerName)
#endif
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
    }
}