using CodeArts.Runtime;
using System;

namespace CodeArts
{
    /// <summary>
    /// 属性转换器。
    /// </summary>
    public abstract class PropConverter
    {
        /// <summary>
        /// 是否支持此类型的转换。
        /// </summary>
        /// <param name="typeToConvert">解决类型。</param>
        /// <returns></returns>
        public abstract bool CanConvert(Type typeToConvert);

        /// <summary>
        /// 替换内容（解决对象类型）。
        /// </summary>
        /// <param name="propertyItem">属性。</param>
        /// <param name="value">属性值。</param>
        /// <returns></returns>
        public abstract string Convert(PropertyItem propertyItem, object value);
    }

    /// <summary>
    /// 属性转换器。
    /// </summary>
    /// <typeparam name="T">数据类型。</typeparam>
    public abstract class PropConverter<T> : PropConverter
    {
        /// <summary>
        /// 是否支持此类型的转换。
        /// </summary>
        /// <param name="typeToConvert">解决类型。</param>
        /// <returns></returns>
        public sealed override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(T);

        /// <summary>
        /// 替换内容。
        /// </summary>
        /// <param name="propertyItem">属性。</param>
        /// <param name="value">属性值。</param>
        /// <returns></returns>
        public sealed override string Convert(PropertyItem propertyItem, object value)
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
