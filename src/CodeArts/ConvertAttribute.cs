using System;

namespace CodeArts
{
    /// <summary>
    /// 转换。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public abstract class ConvertAttribute : Attribute
    {
        /// <summary>
        /// 类型转换。
        /// </summary>
        /// <param name="value">原来的值。</param>
        /// <returns></returns>
        public abstract object Convert(object value);
    }
}
