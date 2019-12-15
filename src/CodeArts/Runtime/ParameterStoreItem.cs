using System;
using System.Linq;
using System.Reflection;

namespace CodeArts.Runtime
{
    /// <summary>
    /// 参数项目
    /// </summary>
    public class ParameterStoreItem : StoreItem
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="info">参数信息</param>
        public ParameterStoreItem(ParameterInfo info) : base(info.GetCustomAttributes(true).Select(attr => (Attribute)attr))
        {
            Info = info;
        }

        /// <summary>
        /// 参数名称
        /// </summary>
        public override string Name => Info.Name;

        /// <summary>
        /// 参数信息
        /// </summary>
        public ParameterInfo Info { get; }

        /// <summary>
        /// 可选参数
        /// </summary>
        public bool IsOptional => Info.IsOptional;

        /// <summary>
        /// 默认值
        /// </summary>
        public object DefaultValue => Info.DefaultValue;

        /// <summary>
        /// 参数类型
        /// </summary>
        public Type ParameterType => Info.ParameterType;
    }
}
