using System.Collections;
using System.Text.RegularExpressions;

namespace CodeArts
{
    /// <summary>
    /// JSON 属性设置(非数字格式的内容会加双引号)
    /// </summary>
    public class JsonSettings : DefaultSettings
    {
        private static readonly Regex NumberPattern = new Regex("^[-]?(0|[1-9][0-9]*)(\\.[0-9]+)?$", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="namingCase">命名规则</param>
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
            string text = base.Convert(value);

            if (text is null || value is null || value is IEnumerable)
                return text;

            var typeToConvert = value.GetType();

            if (typeToConvert.IsValueType && NumberPattern.IsMatch(text))
                return text;

            return string.Concat("\"", text, "\"");
        }
    }
}
