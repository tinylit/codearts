using System;
using System.Collections;
using System.Text.RegularExpressions;

namespace SkyBuilding
{
    /// <summary>
    /// JSON 属性设置
    /// </summary>
    public class JsonSettings : DefaultSettings
    {
        private readonly static Regex Pattern = new Regex("^[-]?(0|[1-9][0-9]*)(\\.[0-9]+)?$", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="namingCase">命名风格</param>
        public JsonSettings(NamingType namingCase) : base(namingCase)
        {
        }

        /// <summary>
        /// 数据解决
        /// </summary>
        /// <param name="value">值</param>
        /// <returns></returns>
        protected override string Convert(object value)
        {
            if (value is string text)
                return string.Concat("\"", text, "\"");

            if (value is DateTime date)
            {
                return string.Concat("\"", date.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFFK"), "\"");
            }

            if (value is IEnumerable enumerable)
            {
                return base.Convert(enumerable);
            }

            text = value.ToString();

            if (Pattern.IsMatch(text))
                return text;

            return string.Concat("\"", text, "\"");
        }

        /// <summary>
        /// 替换内容
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        /// <param name="typeToConvert">属性类型</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public override string Convert(string propertyName, Type typeToConvert, object value)
        {
            if (value is null) return null;

            foreach (PropConverter converter in Converters)
            {
                if (converter.CanConvert(typeToConvert))
                {
                    string text = converter.Convert(propertyName, typeToConvert, value);

                    if (text is null || Pattern.IsMatch(text))
                        return text;

                    return string.Concat("\"", text, "\"");
                }
            }

            try
            {
                return Convert(value);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
