using System;
using System.Reflection;

namespace SkyBuilding.Runtime
{
    /// <summary>
    /// 属性仓库
    /// </summary>
    public class PropertyStoreItem : StoreItem<PropertyInfo>
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="info">属性</param>
        public PropertyStoreItem(PropertyInfo info) : base(info)
        {
        }

        /// <summary>
        /// 属性类型
        /// </summary>
        public override Type MemberType => Member.PropertyType;

#if NET40

        /// <summary>
        /// 静态属性
        /// </summary>
        public override bool IsStatic => Member.GetGetMethod(true)?.IsStatic ?? Member.GetSetMethod(true)?.IsStatic ?? false;

        /// <summary>
        /// 公共属性
        /// </summary>
        public override bool IsPublic => Member.GetGetMethod(true)?.IsPublic ?? Member.GetSetMethod(true)?.IsPublic ?? false;
#else
        /// <summary>
        /// 静态属性
        /// </summary>
        public override bool IsStatic => Member.GetMethod?.IsStatic ?? Member.SetMethod?.IsStatic ?? false;

        /// <summary>
        /// 公共属性
        /// </summary>
        public override bool IsPublic => Member.GetMethod?.IsPublic ?? Member.SetMethod?.IsPublic ?? false;
#endif
        /// <summary>
        /// 可读
        /// </summary>
        public override bool CanRead => Member.CanRead;

        /// <summary>
        /// 可写
        /// </summary>
        public override bool CanWrite => Member.CanWrite;

    }
}
