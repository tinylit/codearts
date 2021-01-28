using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace CodeArts.Runtime
{
    /// <summary>
    /// 参数项目。
    /// </summary>
    public class ParameterItem : StoreItem
    {
        private static readonly ConcurrentDictionary<ParameterInfo, ParameterItem> ItemCache = new ConcurrentDictionary<ParameterInfo, ParameterItem>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="info">参数。</param>
        private ParameterItem(ParameterInfo info) : base(info)
        {
            Info = info;
        }

        /// <summary>
        /// 参数名称。
        /// </summary>
        public override string Name => Info.Name;

        /// <summary>
        /// 参数信息。
        /// </summary>
        public ParameterInfo Info { get; }

        /// <summary>
        /// 可选参数。
        /// </summary>
        public bool IsOptional => Info.IsOptional;

#if !NET40
        /// <summary>
        /// 是否有默认值。
        /// </summary>
        public bool HasDefaultValue => Info.HasDefaultValue;
#endif

        /// <summary>
        /// 默认值。
        /// </summary>
        public object DefaultValue => Info.DefaultValue;

        /// <summary>
        /// 参数类型。
        /// </summary>
        public Type ParameterType => Info.ParameterType;

        /// <summary>
        /// 获取仓储项目。
        /// </summary>
        /// <param name="info">信息。</param>
        /// <returns></returns>
        public static ParameterItem Get(ParameterInfo info) => ItemCache.GetOrAdd(info, parameterInfo => new ParameterItem(parameterInfo));
    }
}
