using System;
using System.Text.RegularExpressions;

namespace CodeArts
{
    /// <summary>
    /// JSON 属性设置(非数字格式的内容会加双引号)。
    /// </summary>
    public class JsonSettings : DefaultSettings
    {
        private static readonly Regex NumberPattern = new Regex("^[-]?(0|[1-9][0-9]*)(\\.[0-9]+)?$", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="namingCase">命名规则。</param>
        public JsonSettings(NamingType namingCase) : base(namingCase)
        {
        }

        /// <summary>
        /// ‘null’值处理。
        /// </summary>
        /// <returns></returns>
        public override string NullValue => "null";

        /// <summary>
        /// 打包数据。
        /// </summary>
        /// <param name="value">数据。</param>
        /// <param name="typeToConvert">源数据类型。</param>
        /// <returns></returns>
        protected override string ValuePackaging(string value, Type typeToConvert)
        {
            if (typeToConvert.IsValueType && NumberPattern.IsMatch(value))
            {
                return value;
            }

            return string.Concat("\"", value, "\"");
        }
    }
}
