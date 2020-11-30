using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace CodeArts.Runtime
{
    /// <summary>
    /// 字段仓储。
    /// </summary>
    public class FieldStoreItem : StoreItem<FieldInfo>
    {
        private static readonly ConcurrentDictionary<FieldInfo, FieldStoreItem> ItemCache = new ConcurrentDictionary<FieldInfo, FieldStoreItem>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="info">字段信息。</param>
        private FieldStoreItem(FieldInfo info) : base(info)
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
        public static FieldStoreItem Get(FieldInfo info) => ItemCache.GetOrAdd(info, fieldInfo => new FieldStoreItem(fieldInfo));
    }
}
