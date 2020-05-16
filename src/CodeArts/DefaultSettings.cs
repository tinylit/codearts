using CodeArts.Runtime;
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

        /// <summary>
        /// ‘null’值处理。
        /// </summary>
        /// <returns></returns>
        public virtual string NullValue => string.Empty;

        /// <summary>
        /// 命名规范后，属性名称比较忽略分大小写。默认：否。
        /// </summary>
        public virtual bool IgnoreCase { get; set; }

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
        /// <param name="packaging">包装数据</param>
        /// <returns></returns>
        public string Convert(object value, bool packaging = true)
        {
            if (value is null)
            {
                if (packaging)
                {
                    return NullValue;
                }

                return null;
            }

            switch (value)
            {
                case string text:
                    return packaging ? ValuePackaging(text, typeof(string)) : text;
                case DateTime date:
                    return packaging ? ValuePackaging(date.ToString(DateFormatString), typeof(DateTime)) : date.ToString(DateFormatString);
                case IEnumerable enumerable:
                    {
                        var enumerator = enumerable.GetEnumerator();

                        if (!enumerator.MoveNext())
                        {
                            if (packaging)
                            {
                                return NullValue;
                            }

                            return null;
                        }

                        while (enumerator.Current is null)
                        {
                            if (!enumerator.MoveNext())
                            {
                                if (packaging)
                                {
                                    return NullValue;
                                }

                                return null;
                            }
                        }

                        var sb = new StringBuilder();

                        sb.Append("[")
                            .Append(Convert(enumerator.Current, true));

                        while (enumerator.MoveNext())
                        {
                            if (enumerator.Current is null)
                            {
                                continue;
                            }

                            sb.Append(",")
                                .Append(Convert(enumerator.Current, true));
                        }

                        return sb.Append("]")
                            .ToString();
                    }
                default:
                    return packaging ? ValuePackaging(value.ToString(), value.GetType()) : value.ToString();
            }
        }

        /// <summary>
        /// 替换内容
        /// </summary>
        /// <param name="propertyItem">属性</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public string Convert(PropertyStoreItem propertyItem, object value)
        {
            foreach (PropConverter converter in Converters)
            {
                if (converter.CanConvert(propertyItem.MemberType))
                {
                    return ValuePackaging(converter.Convert(propertyItem, value), propertyItem.MemberType);
                }
            }

            try
            {
                return Convert(value);
            }
            catch (Exception)
            {
                return NullValue;
            }
        }

        /// <summary>
        /// 打包数据。
        /// </summary>
        /// <param name="value">数据</param>
        /// <param name="typeToConvert">源数据类型</param>
        /// <returns></returns>
        protected virtual string ValuePackaging(string value, Type typeToConvert) => value;
    }
}
