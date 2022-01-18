using CodeArts.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeArts.Db
{
    /// <summary>
    /// 区域。
    /// </summary>
    public static class TableRegions
    {
        private static readonly ConcurrentDictionary<Type, TableInfo> TableCache = new ConcurrentDictionary<Type, TableInfo>();

        /// <summary>
        /// 表。
        /// </summary>
        private class TableInfo : ITableInfo
        {
            public TableInfo(Type tableType, string tableName, List<string> keys, List<string> readOnlys, Dictionary<string, TokenAttribute> tokens, Dictionary<string, string> readWrites, Dictionary<string, string> readOrWrites)
            {
                TableType = tableType;
                TableName = tableName;
#if NET40
                Keys = keys.ToReadOnlyList();
                ReadOnlys = readOnlys.ToReadOnlyList();
                Tokens = tokens.ToReadOnlyDictionary();
                ReadWrites = readWrites.ToReadOnlyDictionary();
                ReadOrWrites = readOrWrites.ToReadOnlyDictionary();
#else
                Keys = keys;
                ReadOnlys = readOnlys;
                Tokens = tokens;
                ReadWrites = readWrites;
                ReadOrWrites = readOrWrites;
#endif
            }
            public Type TableType { get; }
            public string TableName { get; }
            public IReadOnlyCollection<string> Keys { get; }
            public IReadOnlyCollection<string> ReadOnlys { get; }
            public IReadOnlyDictionary<string, TokenAttribute> Tokens { get; }
            public IReadOnlyDictionary<string, string> ReadWrites { get; }
            public IReadOnlyDictionary<string, string> ReadOrWrites { get; }
        }

        private static TableInfo Aw_Resolve(Type type) => TableCache.GetOrAdd(type ?? throw new ArgumentNullException(nameof(type)), tableType =>
         {
             var keys = new List<string>();
             var tokens = new Dictionary<string, TokenAttribute>();
             var readOnlys = new List<string>();
             var readWrites = new Dictionary<string, string>();
             var readOrWrites = new Dictionary<string, string>();

             var typeStore = TypeItem.Get(tableType);

             foreach (var item in typeStore.PropertyStores)
             {
                 if (item.Ignore)
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

             return new TableInfo(tableType, typeStore.Naming, keys, readOnlys, tokens, readWrites, readOrWrites);
         });

        /// <summary>
        /// 获取指定类型的表结构。
        /// </summary>
        /// <param name="type">类型。</param>
        /// <returns></returns>
        public static ITableInfo Resolve(Type type)
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
        /// 获取指定泛型类型的表结构。
        /// </summary>
        /// <typeparam name="T">泛型参数。</typeparam>
        /// <returns></returns>
        public static ITableInfo Resolve<T>() where T : class => Aw_Resolve(typeof(T));
    }
}
