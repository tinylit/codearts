using System;

namespace CodeArts
{
    /// <summary>
    /// 实体属性语法糖
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class PropSugarAttribute : Attribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="toStringMethod">转成字符串的函数</param>
        public PropSugarAttribute(Func<object, string> toStringMethod)
        {
            ToStringMethod = toStringMethod ?? throw new ArgumentNullException(nameof(toStringMethod));
        }

        /// <summary>
        /// 转成字符串的函数
        /// </summary>
        public Func<object, string> ToStringMethod { get; }
    }
}
