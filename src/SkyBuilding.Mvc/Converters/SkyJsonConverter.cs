#if NETCOREAPP3_1
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkyBuilding.Mvc.Converters
{
    /// <summary>
    /// 天空之城JSON转换器（修复长整型前端数据丢失的问题）
    /// </summary>
    public class SkyJsonConverter : JsonConverter<object>
    {
        /// <summary>
        /// 是否可以调整
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type objectType) => objectType.IsValueType || typeof(string) == objectType;

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="reader">读取器</param>
        /// <param name="typeToConvert">目标类型</param>
        /// <param name="options">配置</param>
        /// <returns></returns>
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            string value = Encoding.UTF8.GetString(reader.ValueSpan.ToArray());

            if (typeToConvert.IsEnum)
            {
                return Enum.Parse(typeToConvert, value, true);
            }

            if (typeToConvert.IsNullable())
            {
                typeToConvert = Nullable.GetUnderlyingType(typeToConvert);
            }

            try
            {
                return Convert.ChangeType(value, typeToConvert);
            }
            catch { }

            return value.CastTo(typeToConvert);
        }
        /// <summary>
        /// 写入
        /// </summary>
        /// <param name="writer">写入器</param>
        /// <param name="value">数据</param>
        /// <param name="options">配置</param>
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            var type = value.GetType();

            if (type.IsEnum)
            {
                value = Convert.ChangeType(value, Enum.GetUnderlyingType(type));
            }
            else if (type.IsNullable())
            {
                value = Convert.ChangeType(value, Nullable.GetUnderlyingType(type));
            }            

            switch (value)
            {
                case bool b:
                    writer.WriteBooleanValue(b);
                    break;
                case int i32:
                    writer.WriteNumberValue(i32);
                    break;
                case long i64 when i64 >= -9007199254740991L && i64 <= 9007199254740991L:
                    writer.WriteNumberValue(i64);
                    break;
                case float f when f >= -9007199254740991F && f <= 9007199254740991F:
                    writer.WriteNumberValue(f);
                    break;
                case double d when d >= -9007199254740991D && d <= 9007199254740991D:
                    writer.WriteNumberValue(d);
                    break;
                case decimal m when m >= -9007199254740991M && m <= 9007199254740991M:
                    writer.WriteNumberValue(m);
                    break;
                case ulong u64 when u64 <= 9007199254740991uL:
                    writer.WriteNumberValue(u64);
                    break;
                case uint u32:
                    writer.WriteNumberValue(u32);
                    break;
                case DateTime date:
                    writer.WriteStringValue(date.ToString(Consts.DateFormatString));
                    break;
                default:
                    writer.WriteStringValue(value.ToString());
                    break;
            }
        }
    }
}
#else
using Newtonsoft.Json;
using System;

namespace SkyBuilding.Mvc.Converters
{
    /// <summary>
    /// 天空之城JSON转换器（修复长整型前端数据丢失的问题）
    /// </summary>
    public class SkyJsonConverter : JsonConverter
    {
        /// <summary>
        /// 是否可以调整
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type objectType) => objectType.IsValueType || typeof(string) == objectType;

        /// <summary>
        /// 改变数据
        /// </summary>
        /// <returns></returns>
        private object ChangeType(object source, Type conversionType)
        {
            if (source == null) return source;

            if (source.GetType() == conversionType)
                return source;

            if (conversionType.IsValueType)
            {
                if (conversionType.IsNullable())
                {
                    conversionType = Nullable.GetUnderlyingType(conversionType);
                }

                try
                {
                    return Convert.ChangeType(source, conversionType);
                }
                catch { }

                string value = source.ToString();

                if (conversionType.IsEnum)
                {
                    try
                    {
                        return Enum.Parse(conversionType, value, true);
                    }
                    catch
                    {
                        return null;
                    }
                }

                if (conversionType == typeof(bool))
                {
                    if (bool.TryParse(value, out bool result))
                    {
                        return result;
                    }
                }

                if (conversionType == typeof(int) ||
                    conversionType == typeof(bool) ||
                    conversionType == typeof(byte))
                {
                    if (int.TryParse(value, out int result))
                    {
                        if (conversionType == typeof(bool))
                            return result > 0;
                        return Convert.ChangeType(result, conversionType);
                    }
                    return null;
                }

                if (conversionType == typeof(uint))
                {
                    if (uint.TryParse(value, out uint result))
                    {
                        return result;
                    }
                    return null;
                }

                if (conversionType == typeof(long))
                {
                    if (long.TryParse(value, out long result))
                    {
                        return result;
                    }
                    return null;
                }

                if (conversionType == typeof(ulong))
                {
                    if (ulong.TryParse(value, out ulong result))
                    {
                        return result;
                    }
                    return null;
                }
            }

            if (conversionType == typeof(string))
                return source.ToString();

            return source.CastTo(conversionType);
        }

        /// <summary>
        /// 读取JSON
        /// </summary>
        /// <param name="reader">阅读器</param>
        /// <param name="objectType">数据类型</param>
        /// <param name="existingValue">值</param>
        /// <param name="serializer">解析器</param>
        /// <returns></returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            return ChangeType(reader.Value, objectType);
        }

        /// <summary>
        /// 写入JSON
        /// </summary>
        /// <param name="writer">写入器</param>
        /// <param name="value">值</param>
        /// <param name="serializer">解析器</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var type = value.GetType();

            if (type.IsEnum)
            {
                value = Convert.ChangeType(value, Enum.GetUnderlyingType(type));
            }
            else if (type.IsNullable())
            {
                value = Convert.ChangeType(value, Nullable.GetUnderlyingType(type));
            }

            switch (value)
            {
                case ulong u64 when u64 > 9007199254740991uL:
                case long i64 when i64 < -9007199254740991L || i64 > 9007199254740991L:
                case float f when f < -9007199254740991F || f > 9007199254740991F:
                case double d when d < -9007199254740991D || d > 9007199254740991D:
                case decimal m when m < -9007199254740991M || m > 9007199254740991M:
                    writer.WriteValue(value.ToString());
                    break;
                default:
                    writer.WriteValue(value);
                    break;
            }
        }
    }
}
#endif