using System;

namespace CodeArts
{
    /// <summary>
    /// 命名特性。
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public sealed class NamingAttribute : Attribute
    {
        /// <summary>
        /// 名称。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 命名规范。
        /// </summary>
        public NamingType NamingType { get; set; }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="name">名称。</param>
        public NamingAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="namingType">名称风格。</param>
        public NamingAttribute(NamingType namingType)
        {
            NamingType = namingType;
        }
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="namingType">名称风格。</param>
        public NamingAttribute(string name, NamingType namingType) : this(name)
        {
            NamingType = namingType;
        }
    }
}
