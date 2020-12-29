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
        /// 获取声明该成员的类。
        /// </summary>
        public override Type MemberType => Member.DeclaringType;

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
        public static ConstructorStoreItem Get(ConstructorInfo info) => ItemCache.GetOrAdd(info, constructorInfo => new ConstructorStoreItem(constructorInfo));
在此之后:
        public static ConstructorStoreItem Get(ConstructorInfo info) => ItemCache.GetOrAdd(info, constructorInfo => new Runtime.ConstructorStoreItem(constructorInfo));
*/

/* 项目“CodeArts (net40)”的未合并的更改
在此之前:
        public static ConstructorStoreItem Get(ConstructorInfo info) => ItemCache.GetOrAdd(info, constructorInfo => new ConstructorStoreItem(constructorInfo));
在此之后:
        public static ConstructorStoreItem Get(ConstructorInfo info) => ItemCache.GetOrAdd(info, constructorInfo => new Runtime.ConstructorStoreItem(constructorInfo));
*/

/* 项目“CodeArts (netstandard2.0)”的未合并的更改
在此之前:
        public static ConstructorStoreItem Get(ConstructorInfo info) => ItemCache.GetOrAdd(info, constructorInfo => new ConstructorStoreItem(constructorInfo));
在此之后:
        public static ConstructorStoreItem Get(ConstructorInfo info) => ItemCache.GetOrAdd(info, constructorInfo => new Runtime.ConstructorStoreItem(constructorInfo));
*/

/* 项目“CodeArts (netstandard2.1)”的未合并的更改
在此之前:
        public static ConstructorStoreItem Get(ConstructorInfo info) => ItemCache.GetOrAdd(info, constructorInfo => new ConstructorStoreItem(constructorInfo));
在此之后:
        public static ConstructorStoreItem Get(ConstructorInfo info) => ItemCache.GetOrAdd(info, constructorInfo => new Runtime.ConstructorStoreItem(constructorInfo));
*/
        public static ConstructorItem Get(ConstructorInfo info) => ItemCache.GetOrAdd(info, (Func<ConstructorInfo, ConstructorItem>)(constructorInfo => (ConstructorItem)new ConstructorItem(constructorInfo)));
    }
}
