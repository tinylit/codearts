using System;
using System.Collections.Generic;
using System.Reflection;

namespace CodeArts.Proxies.Hooks
{
    /// <summary>
    /// 代理所有方法。
    /// </summary>
    public class AllMethodsHook : IProxyMethodHook
    {
        private static readonly ICollection<Type> SkippedTypes = new Type[]
        {
            typeof(object),
#if NET40 || NET45 || NET451 ||NET452 || NET461
			typeof(MarshalByRefObject),
            typeof(ContextBoundObject)
#endif
		};

        /// <summary>
        /// 获取一个值，该值标识元数据元素。
        /// </summary>
        /// <value>该值能够唯一标识元数据元素。</value>
        public virtual int MetadataToken => GetType().MetadataToken;

        /// <summary>
        /// 是否启用指定方法的代理。
        /// </summary>
        /// <param name="info">方法信息</param>
        /// <returns></returns>
        public virtual bool IsEnabledFor(MethodInfo info) => !SkippedTypes.Contains(info.DeclaringType);
    }
}
