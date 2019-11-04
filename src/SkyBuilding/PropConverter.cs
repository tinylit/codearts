using System;

namespace SkyBuilding
{
    /// <summary>
    /// 属性转换器
    /// </summary>
    public abstract class PropConverter
    {
        /// <summary>
        /// 是否支持此类型的转换
        /// </summary>
        /// <param name="typeToConvert">解决类型</param>
        /// <returns></returns>
        public abstract bool CanConvert(Type typeToConvert);

        /// <summary>
        /// 替换内容（解决对象类型）
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        /// <param name="typeToConvert">属性类型</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public abstract string Convert(string propertyName, Type typeToConvert, object value);
    }

    /// <summary>
    /// 属性转换器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PropConverter<T> : PropConverter
    {
        /// <summary>
        /// 是否支持此类型的转换
        /// </summary>
        /// <param name="typeToConvert">解决类型</param>
        /// <returns></returns>
        public sealed override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(T);

        /// <summary>
        /// 替换内容
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        /// <param name="typeToConvert">属性类型</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public sealed override string Convert(string propertyName, Type typeToConvert, object value)
        {
            if (value is T typeValue)
            {
                return Convert(propertyName, typeToConvert, typeValue);
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// 替换内容
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        /// <param name="typeToConvert">属性类型</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public abstract string Convert(string propertyName, Type typeToConvert, T value);
    }
}
