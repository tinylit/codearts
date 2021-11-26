using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CodeArts.Runtime
{
    /// <summary>
    /// 方法仓库。
    /// </summary>
    public class MethodItem : StoreItem<MethodInfo>
    {
        private static readonly ConcurrentDictionary<MethodInfo, MethodItem> ItemCache = new ConcurrentDictionary<MethodInfo, MethodItem>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="info">方法。</param>
        private MethodItem(MethodInfo info) : base(info)
        {
        }

        /// <summary>
        /// 静态方法。
        /// </summary>
        public override bool IsStatic => Member.IsStatic;

        /// <summary>
        /// 公共方法。
        /// </summary>
        public override bool IsPublic => Member.IsPublic;

        /// <summary>
        /// 可以调用。
        /// </summary>
        public override bool CanRead => Member.IsPublic;

        /// <summary>
        /// 可修改。
        /// </summary>
        public override bool CanWrite => false;

        private static readonly object Lock_ParameterObj = new object();


        private IReadOnlyList<ParameterItem> parameterStores;
        /// <summary>
        /// 参数信息。
        /// </summary>
        public IReadOnlyList<ParameterItem> ParameterStores
        {
            get
            {
                if (parameterStores is null)
                {
                    lock (Lock_ParameterObj)
                    {
                        if (parameterStores is null)
                        {
#if NET40
                            parameterStores = Member.GetParameters()
                                .Select(info => ParameterItem.Get(info))
                                .ToReadOnlyList();
#else
                            parameterStores = Member.GetParameters()
                                .Select(info => ParameterItem.Get(info))
                                .ToList();
#endif
                        }
                    }
                }
                return parameterStores;
            }
        }


        /// <summary>
        /// 获取仓储项目。
        /// </summary>
        /// <param name="info">信息。</param>
        /// <returns></returns>
        public static MethodItem Get(MethodInfo info) => ItemCache.GetOrAdd(info, methodInfo => new MethodItem(methodInfo));
    }
}
