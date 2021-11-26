using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CodeArts.Runtime
{
    /// <summary>
    /// 构造函数。
    /// </summary>
    public class ConstructorItem : StoreItem<ConstructorInfo>
    {
        private static readonly ConcurrentDictionary<ConstructorInfo, ConstructorItem> ItemCache = new ConcurrentDictionary<ConstructorInfo, ConstructorItem>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="info">构造函数信息。</param>
        private ConstructorItem(ConstructorInfo info) : base(info)
        {
        }

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        public override bool IsStatic => Member.IsStatic;

        /// <summary>
        /// 公共构造函数。
        /// </summary>
        public override bool IsPublic => Member.IsPublic;

        /// <summary>
        /// 是否可读。
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// 是否可写。
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
                            parameterStores = Member.GetParameters()
                                .Select(info => ParameterItem.Get(info))
#if NET40
                                .ToReadOnlyList();
#else
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
        public static ConstructorItem Get(ConstructorInfo info) => ItemCache.GetOrAdd(info, constructorInfo => new ConstructorItem(constructorInfo));
    }
}
