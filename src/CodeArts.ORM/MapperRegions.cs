using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeArts.ORM
{
    /// <summary>
    /// 区域
    /// </summary>
    public static class MapperRegions
    {
        private static readonly ConcurrentDictionary<Type, TableRegions> MapperCache =
            new ConcurrentDictionary<Type, TableRegions>();

        /// <summary>
        /// 表
        /// </summary>
        private sealed class TableRegions : ITableRegions
        {
            public TableRegions(Type tableType, string tableName, List<string> keys, List<string> readOnlys, Dictionary<string, TokenAttribute> tokens, Dictionary<string, string> readWrites, Dictionary<string, string> readOrWrites)
            {
                TableType = tableType ?? throw new ArgumentNullException(nameof(tableType));
                TableName = tableName;
                Keys = keys;
                ReadOnlys = readOnlys;
                Tokens = tokens;
                ReadWrites = readWrites;
                ReadOrWrites = readOrWrites;
            }
            public Type TableType { get; }
            public string TableName { get; }
            public IEnumerable<string> Keys { get; }
            public IEnumerable<string> ReadOnlys { get; }
#if NET40
        public IDictionary<string, TokenAttribute> Tokens { get; }

        public IDictionary<string, string> ReadWrites { get; }

        public IDictionary<string, string> ReadOrWrites { get; }
#else

            public IReadOnlyDictionary<string, TokenAttribute> Tokens { get; }

            public IReadOnlyDictionary<string, string> ReadWrites { get; }

            public IReadOnlyDictionary<string, string> ReadOrWrites { get; }
#endif
        }

        private static TableRegions Aw_Resolve(Type type) => MapperCache.GetOrAdd(type ?? throw new ArgumentNullException(nameof(type)), tableType =>
         {
             var keys = new List<string>();
             var tokens = new Dictionary<string, TokenAttribute>();
             var readOnlys = new List<string>();
             var readWrites = new Dictionary<string, string>();
             var readOrWrites = new Dictionary<string, string>();

             var typeStore = RuntimeTypeCache.Instance.GetCache(tableType);

             foreach (var item in typeStore.PropertyStores)
             {
                 if (item.IsDefined<IgnoreAttribute>())
                 {
                     continue;
                 }

                 if (item.IsDefined<KeyAttribute>())
                 {
                     keys.Add(item.Name);
                 }

                 var token = item.GetCustomAttribute<TokenAttribute>();

                 if (token != null)
                 {
                     tokens.Add(item.Name, token);
                 }

                 var readOnly = item.GetCustomAttribute<ReadOnlyAttribute>();

                 if (readOnly?.IsReadOnly ?? false)
                 {
                     readOnlys.Add(item.Name);
                 }
                 else
                 {
                     readWrites.Add(item.Name, item.Naming);
                 }

                 readOrWrites.Add(item.Name, item.Naming);
             }

             return new TableRegions(tableType, typeStore.Naming, keys, readOnlys, tokens, readWrites, readOrWrites);
         });

        /// <summary>
        /// 获取指定类型的表结构
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static ITableRegions Resolve(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (type.IsValueType)
            {
                throw new NotSupportedException("不支持值类型!");
            }

            return Aw_Resolve(type);
        }
        /// <summary>
        /// 获取指定泛型类型的表结构
        /// </summary>
        /// <typeparam name="T">泛型参数</typeparam>
        /// <returns></returns>
        public static ITableRegions Resolve<T>() where T : class => Aw_Resolve(typeof(T));
    }
}
