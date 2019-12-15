using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace CodeArts.Runtime
{
    /// <summary>
    /// 方法仓库
    /// </summary>
    public class MethodStoreItem : StoreItem<MethodInfo>
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="info">方法</param>
        public MethodStoreItem(MethodInfo info) : base(info)
        {
        }

        /// <summary>
        /// 放回值类型
        /// </summary>
        public override Type MemberType => Member.ReturnType;

        /// <summary>
        /// 静态方法
        /// </summary>
        public override bool IsStatic => Member.IsStatic;

        /// <summary>
        /// 公共方法
        /// </summary>
        public override bool IsPublic => Member.IsPublic;

        /// <summary>
        /// 可以调用
        /// </summary>
        public override bool CanRead => Member.IsPublic;

        /// <summary>
        /// 可修改
        /// </summary>
        public override bool CanWrite => false;

        private ReadOnlyCollection<ParameterStoreItem> parameterStores;
        private readonly static object Lock_ParameterObj = new object();
        /// <summary>
        /// 参数信息
        /// </summary>
        public ReadOnlyCollection<ParameterStoreItem> ParameterStores
        {
            get
            {
                if (parameterStores == null)
                {
                    lock (Lock_ParameterObj)
                    {
                        if (parameterStores == null)
                        {
                            parameterStores = Member.GetParameters()
                                .Select(info => new ParameterStoreItem(info))
                                .ToList().AsReadOnly();
                        }
                    }
                }
                return parameterStores;
            }
        }
    }
}
