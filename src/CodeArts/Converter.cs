using CodeArts.Runtime;
using System;

namespace CodeArts
{
    /// <summary>
    /// 属性转换器。
    /// </summary>
    public interface IConverter
    {
        /// <summary>
        /// 是否支持此类型的转换。
        /// </summary>
        /// <param name="propertyItem">属性。</param>
        /// <returns></returns>
        bool CanConvert(PropertyItem propertyItem);

        /// <summary>
        /// 替换内容（解决对象类型）。
        /// </summary>
        /// <param name="propertyItem">属性。</param>
        /// <param name="value">属性值。</param>
        /// <returns></returns>
        string Convert(PropertyItem propertyItem, object value);
    }

    /// <summary>
    /// 属性转换器。
    /// </summary>
    /// <typeparam name="T">数据类型。</typeparam>
    public abstract class Converter<T> : IConverter
    {
        /// <summary>
        /// 是否支持此类型的转换。
        /// </summary>
        /// <param name="propertyItem">属性。</param>
        /// <returns></returns>
        public bool CanConvert(PropertyItem propertyItem) => propertyItem.MemberType == typeof(T);

        /// <summary>
        /// 替换内容。
        /// </summary>
        /// <param name="propertyItem">属性。</param>
        /// <param name="value">属性值。</param>
        /// <returns></returns>
        public string Convert(PropertyItem propertyItem, object value)
        {
            if (value is T typeValue)
            {
                return Convert(propertyItem, typeValue);
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// 替换内容。
        /// </summary>
        /// <param name="propertyItem">属性。</param>
        /// <param name="value">属性值。</param>
        /// <returns></returns>
        protected abstract string Convert(PropertyItem propertyItem, T value);
    }
}
