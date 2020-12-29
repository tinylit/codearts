using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace CodeArts.Runtime
{
    /// <summary>
    /// 属性仓库。
    /// </summary>
    public class PropertyItem : StoreItem<PropertyInfo>
    {
        private static readonly ConcurrentDictionary<PropertyInfo, PropertyItem> ItemCache = new ConcurrentDictionary<PropertyInfo, PropertyItem>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="info">属性。</param>
        private PropertyItem(PropertyInfo info) : base(info)
        {
        }

        /// <summary>
        /// 属性类型。
        /// </summary>
        public override Type MemberType => Member.PropertyType;

#if NET40

        /// <summary>
        /// 静态属性。
        /// </summary>
        public override bool IsStatic => Member.GetGetMethod(true)?.IsStatic ?? Member.GetSetMethod(true)?.IsStatic ?? false;

        /// <summary>
        /// 公共属性。
        /// </summary>
        public override bool IsPublic => Member.GetGetMethod(true)?.IsPublic ?? Member.GetSetMethod(true)?.IsPublic ?? false;
#else
        /// <summary>
        /// 静态属性。
        /// </summary>
        public override bool IsStatic => Member.GetMethod?.IsStatic ?? Member.SetMethod?.IsStatic ?? false;

        /// <summary>
        /// 公共属性。
        /// </summary>
        public override bool IsPublic => Member.GetMethod?.IsPublic ?? Member.SetMethod?.IsPublic ?? false;
#endif
        /// <summary>
        /// 是否可读。
        /// </summary>
        public override bool CanRead => Member.CanRead;

        /// <summary>
        /// 是否可写。
        /// </summary>
        public override bool CanWrite => Member.CanWrite;

        /// <summary>
        /// 获取仓储项目。
        /// </summary>
        /// <param name="info">信息。</param>
        /// <returns></returns>

/* 项目“CodeArts (net45)”的未合并的更改
在此之前:
        public static PropertyStoreItem Get(PropertyInfo info) => ItemCache.GetOrAdd(info, propertyInfo => new PropertyStoreItem(propertyInfo));
在此之后:
        public static PropertyStoreItem Get(PropertyInfo info) => ItemCache.GetOrAdd(info, propertyInfo => new Runtime.PropertyStoreItem(propertyInfo));
*/

/* 项目“CodeArts (net40)”的未合并的更改
在此之前:
        public static PropertyStoreItem Get(PropertyInfo info) => ItemCache.GetOrAdd(info, propertyInfo => new PropertyStoreItem(propertyInfo));
在此之后:
        public static PropertyStoreItem Get(PropertyInfo info) => ItemCache.GetOrAdd(info, propertyInfo => new Runtime.PropertyStoreItem(propertyInfo));
*/

/* 项目“CodeArts (netstandard2.0)”的未合并的更改
在此之前:
        public static PropertyStoreItem Get(PropertyInfo info) => ItemCache.GetOrAdd(info, propertyInfo => new PropertyStoreItem(propertyInfo));
在此之后:
        public static PropertyStoreItem Get(PropertyInfo info) => ItemCache.GetOrAdd(info, propertyInfo => new Runtime.PropertyStoreItem(propertyInfo));
*/

/* 项目“CodeArts (netstandard2.1)”的未合并的更改
在此之前:
        public static PropertyStoreItem Get(PropertyInfo info) => ItemCache.GetOrAdd(info, propertyInfo => new PropertyStoreItem(propertyInfo));
在此之后:
        public static PropertyStoreItem Get(PropertyInfo info) => ItemCache.GetOrAdd(info, propertyInfo => new Runtime.PropertyStoreItem(propertyInfo));
*/
        public static PropertyItem Get(PropertyInfo info) => ItemCache.GetOrAdd(info, (Func<PropertyInfo, PropertyItem>)(propertyInfo => (PropertyItem)new PropertyItem(propertyInfo)));
    }
}
