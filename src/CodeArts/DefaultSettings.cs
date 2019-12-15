using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CodeArts
{
    /// <summary>
    /// 属性设置
    /// </summary>
    public class DefaultSettings
    {
        private readonly NamingType _camelCase;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="namingCase">命名规则</param>
        public DefaultSettings(NamingType namingCase) => _camelCase = namingCase;

        /// <summary>
        /// 保留未知的属性令牌 (为真时，保留匹配的 {PropertyName} 标识，默认：false)
        /// </summary>
        public bool PreserveUnknownPropertyToken { get; set; }

        private string dateFormatString;

        /// <summary>
        /// 获取或设置如何系统。DateTime和系统。格式化DateTimeOffset值,写入JSON文本时，以及读取JSON文本时的期望日期格式。默认值是"yyyy'-'MM'-'dd'T'hh ':' MM':'ss.FFFFFFFK"。
        /// </summary>
        public string DateFormatString
        {
            get => dateFormatString ?? "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";
            set => dateFormatString = value;
        }

        private ICollection<PropConverter> converters;
        /// <summary>
        /// 设置 【PropConverter】 将在内容替换期间使用到它。
        /// </summary>
        public ICollection<PropConverter> Converters => converters ?? (converters = new List<PropConverter>());

        /// <summary>
        /// 属性名解析
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        /// <returns></returns>
        public string ResolvePropertyName(string propertyName) => propertyName.ToNamingCase(_camelCase);

        /// <summary>
        /// 数据解决
        /// </summary>
        /// <param name="value">内容</param>
        /// <returns></returns>
        protected virtual string Convert(object value)
        {
            if (value is string text)
                return text;

            if (value is DateTime date)
            {
                return date.ToString(DateFormatString);
            }

            if (value is IEnumerable enumerable)
            {
                var enumerator = enumerable.GetEnumerator();

                if (!enumerator.MoveNext())
                    return string.Empty;

                while (enumerator.Current is null)
                {
                    if (!enumerator.MoveNext())
                        return string.Empty;
                }

                var sb = new StringBuilder();

                sb.Append("[")
                    .Append(Convert(enumerator.Current));

                while (enumerator.MoveNext())
                {
                    if (enumerator.Current is null)
                        continue;

                    sb.Append(",")
                        .Append(Convert(enumerator.Current));
                }

                return sb.Append("]")
                    .ToString();
            }

            return value.ToString();
        }

        /// <summary>
        /// 替换内容
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        /// <param name="typeToConvert">属性类型</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public virtual string Convert(string propertyName, Type typeToConvert, object value)
        {
            if (value is null) return null;

            foreach (PropConverter converter in Converters)
            {
                if (converter.CanConvert(typeToConvert))
                {
                    return converter.Convert(propertyName, typeToConvert, value);
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
