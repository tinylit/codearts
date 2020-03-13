using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace CodeArts.Runtime
{
    /// <summary>
    /// 构造函数项目
    /// </summary>
    public class ConstructorStoreItem : StoreItem<ConstructorInfo>
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="info">构造函数信息</param>
        public ConstructorStoreItem(ConstructorInfo info) : base(info)
        {
        }

        /// <summary>
        /// 获取声明该成员的类。
        /// </summary>
        public override Type MemberType => Member.DeclaringType;

        /// <summary>
        /// 静态构造函数
        /// </summary>
        public override bool IsStatic => Member.IsStatic;

        /// <summary>
        /// 公共构造函数
        /// </summary>
        public override bool IsPublic => Member.IsPublic;

        /// <summary>
        /// 是否可读
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// 是否可写
        /// </summary>
        public override bool CanWrite => false;


        private ReadOnlyCollection<ParameterStoreItem> parameterStores;
        private static readonly object Lock_ParameterObj = new object();
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
