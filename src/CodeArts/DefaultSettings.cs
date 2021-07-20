using CodeArts.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CodeArts
{
    /// <summary>
    /// 属性设置。
    /// </summary>
    public class DefaultSettings
    {
        private readonly NamingType _camelCase;

        /// <summary>
        /// 默认日期格式。
        /// </summary>
        public const string DefaultDateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="namingCase">命名规则。</param>
        public DefaultSettings(NamingType namingCase) => _camelCase = namingCase;

        /// <summary>
        /// 保留未知的属性令牌 (为真时，保留匹配的 {PropertyName} 标识，默认：false)。
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
            get => dateFormatString ?? DefaultDateFormatString;
            set => dateFormatString = value;
        }

        private ICollection<PropConverter> converters;
        /// <summary>
        /// 设置 【PropConverter】 将在内容替换期间使用到它。
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER
        public ICollection<PropConverter> Converters => converters ??= new List<PropConverter>();
#else
        public ICollection<PropConverter> Converters => converters ?? (converters = new List<PropConverter>());
#endif

        /// <summary>
        /// 属性名解析。
        /// </summary>
        /// <param name="propertyName">属性名称。</param>
        /// <returns></returns>
        public string ResolvePropertyName(string propertyName) => propertyName.ToNamingCase(_camelCase);

        /// <summary>
        /// 数据解决。
        /// </summary>
        /// <param name="value">内容。</param>
        /// <param name="packaging">包装数据。</param>
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
        /// 替换内容。
        /// </summary>
        /// <param name="propertyItem">属性。</param>
        /// <param name="value">属性值。</param>
        /// <returns></returns>
        public string Convert(PropertyItem propertyItem, object value)
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
        /// <param name="value">数据。</param>
        /// <param name="typeToConvert">源数据类型。</param>
        /// <returns></returns>
        protected virtual string ValuePackaging(string value, Type typeToConvert) => value;

        /// <summary>
        /// 结果拼接。
        /// </summary>
        /// <param name="left">左值。</param>
        /// <param name="right">右值。</param>
        /// <returns></returns>
        public virtual object Add(object left, object right)
        {
            if (left is null || right is null)
            {
                return left ?? right;
            }

            Type leftType = left.GetType();
            Type rightType = right.GetType();

            if (leftType.IsValueType && rightType.IsValueType)
            {
                switch (left)
                {
                    case bool b:
                        switch (right)
                        {
                            case bool br:
                                return b || br;
                            case byte br2:
                                return b ? br2 + 1 : br2;
                            case short sr:
                                return b ? sr + 1 : sr;
                            case ushort usr:
                                return b ? usr + 1 : usr;
                            case int ir:
                                return b ? ir + 1 : ir;
                            case uint uir:
                                return b ? uir + 1U : uir;
                            case long lr:
                                return b ? lr + 1L : lr;
                            case ulong ulr:
                                return b ? ulr + 1uL : ulr;
                            case float fr:
                                return b ? fr + 1F : fr;
                            case double dr:
                                return b ? dr + 1D : dr;
                            case decimal dr2:
                                return b ? dr2 + 1M : dr2;
                            default:
                                break;
                        }
                        break;
                    case byte b:
                        switch (right)
                        {
                            case bool br:
                                return br ? b + 1 : b;
                            case byte br2:
                                return b + br2;
                            case short sr:
                                return b + sr;
                            case ushort usr:
                                return b + usr;
                            case int ir:
                                return b + ir;
                            case uint uir:
                                return b + uir;
                            case long lr:
                                return b + lr;
                            case ulong ulr:
                                return b + ulr;
                            case float fr:
                                return b + fr;
                            case double dr:
                                return b + dr;
                            case decimal dr2:
                                return b + dr2;
                            default:
                                break;
                        }
                        break;

                    case sbyte sb:
                        switch (right)
                        {
                            case bool br:
                                return br ? sb + 1 : sb;
                            case byte br2:
                                return sb + br2;
                            case short sr:
                                return sb + sr;
                            case ushort usr:
                                return sb + usr;
                            case int ir:
                                return sb + ir;
                            case uint uir:
                                return sb + uir;
                            case long lr:
                                return sb + lr;
                            case ulong ulr:
                                return (ulong)sb + ulr;
                            case float fr:
                                return sb + fr;
                            case double dr:
                                return sb + dr;
                            case decimal dr2:
                                return sb + dr2;
                            default:
                                break;
                        }
                        break;
                    case ushort usb:
                        switch (right)
                        {
                            case bool br:
                                return br ? usb + 1 : usb;
                            case byte br2:
                                return usb + br2;
                            case short sr:
                                return usb + sr;
                            case ushort usr:
                                return usb + usr;
                            case int ir:
                                return usb + ir;
                            case uint uir:
                                return usb + uir;
                            case long lr:
                                return usb + lr;
                            case ulong ulr:
                                return usb + ulr;
                            case float fr:
                                return usb + fr;
                            case double dr:
                                return usb + dr;
                            case decimal dr2:
                                return usb + dr2;
                            default:
                                break;
                        }
                        break;
                    case int i:
                        switch (right)
                        {
                            case bool br:
                                return br ? i + 1 : i;
                            case byte br2:
                                return i + br2;
                            case short sr:
                                return i + sr;
                            case ushort usr:
                                return i + usr;
                            case int ir:
                                return i + ir;
                            case uint uir:
                                return i + uir;
                            case long lr:
                                return i + lr;
                            case ulong ulr:
                                return (ulong)i + ulr;
                            case float fr:
                                return i + fr;
                            case double dr:
                                return i + dr;
                            case decimal dr2:
                                return i + dr2;
                            default:
                                break;
                        }
                        break;
                    case uint ui:
                        switch (right)
                        {
                            case bool br:
                                return br ? ui + 1u : ui;
                            case byte br2:
                                return ui + br2;
                            case short sr:
                                return ui + sr;
                            case ushort usr:
                                return ui + usr;
                            case int ir:
                                return ui + ir;
                            case uint uir:
                                return ui + uir;
                            case long lr:
                                return ui + lr;
                            case ulong ulr:
                                return ui + ulr;
                            case float fr:
                                return ui + fr;
                            case double dr:
                                return ui + dr;
                            case decimal dr2:
                                return ui + dr2;
                            default:
                                break;
                        }
                        break;
                    case long l:
                        switch (right)
                        {
                            case bool br:
                                return br ? l + 1L : l;
                            case byte br2:
                                return l + br2;
                            case short sr:
                                return l + sr;
                            case ushort usr:
                                return l + usr;
                            case int ir:
                                return l + ir;
                            case uint uir:
                                return l + uir;
                            case long lr:
                                return l + lr;
                            case ulong ulr:
                                return (ulong)l + ulr;
                            case float fr:
                                return l + fr;
                            case double dr:
                                return l + dr;
                            case decimal dr2:
                                return l + dr2;
                            default:
                                break;
                        }
                        break;
                    case ulong ul:
                        switch (right)
                        {
                            case bool br:
                                return br ? ul + 1uL : ul;
                            case byte br2:
                                return ul + br2;
                            case short sr:
                                return ul + (ulong)sr;
                            case ushort usr:
                                return ul + usr;
                            case int ir:
                                return ul + (ulong)ir;
                            case uint uir:
                                return ul + uir;
                            case long lr:
                                return ul + (ulong)lr;
                            case ulong ulr:
                                return ul + ulr;
                            case float fr:
                                return ul + fr;
                            case double dr:
                                return ul + dr;
                            case decimal dr2:
                                return ul + dr2;
                            default:
                                break;
                        }
                        break;
                    case float f:
                        switch (right)
                        {
                            case bool br:
                                return br ? f + 1F : f;
                            case byte br2:
                                return f + br2;
                            case short sr:
                                return f + sr;
                            case ushort usr:
                                return f + usr;
                            case int ir:
                                return f + ir;
                            case uint uir:
                                return f + uir;
                            case long lr:
                                return f + lr;
                            case ulong ulr:
                                return f + ulr;
                            case float fr:
                                return f + fr;
                            case double dr:
                                return f + dr;
                            case decimal dr2:
                                return (decimal)f + dr2;
                            default:
                                break;
                        }
                        break;
                    case double d:
                        switch (right)
                        {
                            case bool br:
                                return br ? d + 1D : d;
                            case byte br2:
                                return d + br2;
                            case short sr:
                                return d + sr;
                            case ushort usr:
                                return d + usr;
                            case int ir:
                                return d + ir;
                            case uint uir:
                                return d + uir;
                            case long lr:
                                return d + lr;
                            case ulong ulr:
                                return d + ulr;
                            case float fr:
                                return d + fr;
                            case double dr:
                                return d + dr;
                            case decimal dr2:
                                return (decimal)d + dr2;
                            default:
                                break;
                        }
                        break;
                    case decimal d2:
                        switch (right)
                        {
                            case bool br:
                                return br ? d2 + 1M : d2;
                            case byte br2:
                                return d2 + br2;
                            case short sr:
                                return d2 + sr;
                            case ushort usr:
                                return d2 + usr;
                            case int ir:
                                return d2 + ir;
                            case uint uir:
                                return d2 + uir;
                            case long lr:
                                return d2 + lr;
                            case ulong ulr:
                                return d2 + ulr;
                            case float fr:
                                return d2 + (decimal)fr;
                            case double dr:
                                return d2 + (decimal)dr;
                            case decimal dr2:
                                return d2 + dr2;
                            default:
                                break;
                        }
                        break;
                }
            }

            return Convert(left, false) + Convert(right, false);
        }
    }
}
