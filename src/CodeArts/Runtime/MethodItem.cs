using System;
using System.Collections.Concurrent;
#if NET40
using System.Collections.ObjectModel;
#else
using System.Collections.Generic;
#endif
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
        /// 放回值类型。
        /// </summary>
        public override Type MemberType => Member.ReturnType;

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

#if NET40

        private ReadOnlyCollection<ParameterItem> parameterStores;
        /// <summary>
        /// 参数信息。
        /// </summary>
        public ReadOnlyCollection<ParameterItem> ParameterStores
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
                                .ToList()
                                .AsReadOnly();
                        }
                    }
                }
                return parameterStores;
            }
        }
#else
        private IReadOnlyCollection<ParameterItem> parameterStores;
        /// <summary>
        /// 参数信息。
        /// </summary>
        public IReadOnlyCollection<ParameterItem> ParameterStores
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
                                .ToList();
                        }
                    }
                }
                return parameterStores;
            }
        }
#endif


        /// <summary>
        /// 获取仓储项目。
        /// </summary>
        /// <param name="info">信息。</param>
        /// <returns></returns>

/* 项目“CodeArts (net45)”的未合并的更改
在此之前:
        public static MethodStoreItem Get(MethodInfo info) => ItemCache.GetOrAdd(info, methodInfo => new MethodStoreItem(methodInfo));
在此之后:
        public static MethodStoreItem Get(MethodInfo info) => ItemCache.GetOrAdd(info, methodInfo => new Runtime.MethodStoreItem(methodInfo));
*/

/* 项目“CodeArts (net40)”的未合并的更改
在此之前:
        public static MethodStoreItem Get(MethodInfo info) => ItemCache.GetOrAdd(info, methodInfo => new MethodStoreItem(methodInfo));
在此之后:
        public static MethodStoreItem Get(MethodInfo info) => ItemCache.GetOrAdd(info, methodInfo => new Runtime.MethodStoreItem(methodInfo));
*/

/* 项目“CodeArts (netstandard2.0)”的未合并的更改
在此之前:
        public static MethodStoreItem Get(MethodInfo info) => ItemCache.GetOrAdd(info, methodInfo => new MethodStoreItem(methodInfo));
在此之后:
        public static MethodStoreItem Get(MethodInfo info) => ItemCache.GetOrAdd(info, methodInfo => new Runtime.MethodStoreItem(methodInfo));
*/

/* 项目“CodeArts (netstandard2.1)”的未合并的更改
在此之前:
        public static MethodStoreItem Get(MethodInfo info) => ItemCache.GetOrAdd(info, methodInfo => new MethodStoreItem(methodInfo));
在此之后:
        public static MethodStoreItem Get(MethodInfo info) => ItemCache.GetOrAdd(info, methodInfo => new Runtime.MethodStoreItem(methodInfo));
*/
        public static MethodItem Get(MethodInfo info) => ItemCache.GetOrAdd(info, (Func<MethodInfo, MethodItem>)(methodInfo => (MethodItem)new MethodItem(methodInfo)));
    }
}
