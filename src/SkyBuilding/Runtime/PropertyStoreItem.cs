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
