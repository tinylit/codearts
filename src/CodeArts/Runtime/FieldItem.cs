using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace CodeArts.Runtime
{
    /// <summary>
    /// 字段仓储。
    /// </summary>
    public class FieldItem : StoreItem<FieldInfo>
    {
        private static readonly ConcurrentDictionary<FieldInfo, FieldItem> ItemCache = new ConcurrentDictionary<FieldInfo, FieldItem>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="info">字段信息。</param>
        private FieldItem(FieldInfo info) : base(info)
        {
        }

        /// <summary>
        /// 字段类型。
        /// </summary>
        public override Type MemberType => Member.FieldType;

        /// <summary>
        /// 静态字段。
        /// </summary>
        public override bool IsStatic => Member.IsStatic;

        /// <summary>
        /// 公共字段。
        /// </summary>
        public override bool IsPublic => Member.IsPublic;

        /// <summary>
        /// 是否可读。
        /// </summary>
        public override bool CanRead => Member.IsPublic;

        /// <summary>
        /// 是否可写。
        /// </summary>
        public override bool CanWrite => Member.IsPublic && !Member.IsInitOnly;

        /// <summary>
        /// 获取仓储项目。
        /// </summary>
        /// <param name="info">信息。</param>
        /// <returns></returns>

/* 项目“CodeArts (net45)”的未合并的更改
在此之前:
        public static FieldStoreItem Get(FieldInfo info) => ItemCache.GetOrAdd(info, fieldInfo => new FieldStoreItem(fieldInfo));
在此之后:
        public static FieldStoreItem Get(FieldInfo info) => ItemCache.GetOrAdd(info, fieldInfo => new Runtime.FieldStoreItem(fieldInfo));
*/

/* 项目“CodeArts (net40)”的未合并的更改
在此之前:
        public static FieldStoreItem Get(FieldInfo info) => ItemCache.GetOrAdd(info, fieldInfo => new FieldStoreItem(fieldInfo));
在此之后:
        public static FieldStoreItem Get(FieldInfo info) => ItemCache.GetOrAdd(info, fieldInfo => new Runtime.FieldStoreItem(fieldInfo));
*/

/* 项目“CodeArts (netstandard2.0)”的未合并的更改
在此之前:
        public static FieldStoreItem Get(FieldInfo info) => ItemCache.GetOrAdd(info, fieldInfo => new FieldStoreItem(fieldInfo));
在此之后:
        public static FieldStoreItem Get(FieldInfo info) => ItemCache.GetOrAdd(info, fieldInfo => new Runtime.FieldStoreItem(fieldInfo));
*/

/* 项目“CodeArts (netstandard2.1)”的未合并的更改
在此之前:
        public static FieldStoreItem Get(FieldInfo info) => ItemCache.GetOrAdd(info, fieldInfo => new FieldStoreItem(fieldInfo));
在此之后:
        public static FieldStoreItem Get(FieldInfo info) => ItemCache.GetOrAdd(info, fieldInfo => new Runtime.FieldStoreItem(fieldInfo));
*/
        public static FieldItem Get(FieldInfo info) => ItemCache.GetOrAdd(info, (Func<FieldInfo, FieldItem>)(fieldInfo => (FieldItem)new FieldItem(fieldInfo)));
    }
}
