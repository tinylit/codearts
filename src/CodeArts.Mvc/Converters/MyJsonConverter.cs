#if NETCOREAPP3_1
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeArts.Mvc.Converters
{
    /// <summary>
    /// 天空之城JSON转换器（修复大数字前端数据丢失的问题）。
    /// </summary>
    public class MyJsonConverterFactory : JsonConverterFactory
    {
        /// <summary>
        /// 是否可以调整。
        /// </summary>
        /// <param name="typeToConvert">数据类型。</param>
        /// <returns></returns>
        public override bool CanConvert(Type typeToConvert)
        {
            if (typeToConvert.IsArray || typeToConvert.IsNullable())
            {
                return true;
            }

            if (!typeToConvert.IsGenericType || !typeToConvert.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                return false;
            }

            var typeArguments = typeToConvert.GetGenericArguments();

            if (typeArguments.Length > 1)
            {
                return false;
            }

            return typeArguments.All(x => !x.IsGenericParameter);
        }

        /// <summary>
        /// 创建转换器。
        /// </summary>
        /// <param name="typeToConvert">转换的目标类型。</param>
        /// <param name="options">配置。</param>
        /// <returns></returns>
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            => typeToConvert.IsArray
            ? (JsonConverter)Activator.CreateInstance(typeof(JsonConverterForArrayOfT<>).MakeGenericType(new Type[] { typeToConvert.GetElementType() }))
            : typeToConvert.IsNullable()
            ? (JsonConverter)Activator.CreateInstance(typeof(MyNullableJsonConverter<>).MakeGenericType(new Type[] { Nullable.GetUnderlyingType(typeToConvert) }))
            : (JsonConverter)Activator.CreateInstance(typeof(JsonConverterIEnumerableOfT<>).MakeGenericType(typeToConvert.GetGenericArguments()));

        private class MyNullableJsonConverter<T> : JsonConverter<T?> where T : struct
        {
            public override bool CanConvert(Type typeToConvert) => true;

            private static bool TryRead(Utf8JsonReader reader, out object value)
            {
                bool flag = true;

                try
                {
                    switch (Type.GetTypeCode(typeof(T)))
                    {
                        case TypeCode.DBNull:
                            value = null;
                            break;
                        case TypeCode.Boolean:
                            value = reader.GetBoolean();
                            break;
                        case TypeCode.SByte:
                            value = reader.GetSByte();
                            break;
                        case TypeCode.Byte:
                            value = reader.GetByte();
                            break;
                        case TypeCode.Int16:
                            value = reader.GetInt16();
                            break;
                        case TypeCode.UInt16:
                            value = reader.GetUInt16();
                            break;
                        case TypeCode.Int32:
                            value = reader.GetInt32();
                            break;
                        case TypeCode.UInt32:
                            value = reader.GetUInt32();
                            break;
                        case TypeCode.Int64:
                            value = reader.GetInt64();
                            break;
                        case TypeCode.UInt64:
                            value = reader.GetUInt64();
                            break;
                        case TypeCode.Single:
                            value = reader.GetSingle();
                            break;
                        case TypeCode.Double:
                            value = reader.GetDouble();
                            break;
                        case TypeCode.Decimal:
                            value = reader.GetDecimal();
                            break;
                        case TypeCode.DateTime:
                            value = reader.GetDateTime();
                            break;
                        case TypeCode.String:
                            value = reader.GetString();
                            break;
                        case TypeCode.Char:
                        default:
                            value = null;
                            flag = false;
                            break;
                    }
                }
                catch
                {
                    value = null;
                    flag = false;
                }

                return flag;
            }

            public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return null;
                }

                if (TryRead(reader, out object value))
                {
                    if (value is null)
                    {
                        return null;
                    }

                    return (T)value;
                }

                string text = Encoding.UTF8.GetString(reader.ValueSpan.ToArray());

                try
                {
                    return (T)Convert.ChangeType(text, typeof(T));
                }
                catch
                {
                    return null;
                }
            }

            public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
            {
                if (!value.HasValue)
                {
                    writer.WriteNullValue();
                    return;
                }

                T result = value.Value;

                switch (result)
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
        private class JsonConverterForArrayOfT<T> : JsonConverter<T[]>
        {
            public override bool CanConvert(Type typeToConvert) => true;

            public override T[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var elements = new List<T>();

                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    return elements.ToArray();
                }

                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    elements.Add(JsonSerializer.Deserialize<T>(ref reader, options));
                }

                return elements.ToArray();
            }

            public override void Write(Utf8JsonWriter writer, T[] value, JsonSerializerOptions options)
            {
                writer.WriteStartArray();

                foreach (T item in value)
                {
                    JsonSerializer.Serialize(writer, item, options);
                }

                writer.WriteEndArray();
            }
        }
        private class JsonConverterIEnumerableOfT<T> : JsonConverter<object>
        {
            public override bool CanConvert(Type typeToConvert) => true;

            public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var elements = new List<T>();

                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    return Mapper.Map(elements, typeToConvert);
                }

                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    elements.Add(JsonSerializer.Deserialize<T>(ref reader, options));
                }

                return Mapper.Map(elements, typeToConvert);
            }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                if (value is IEnumerable<T> enumerable)
                {
                    writer.WriteStartArray();

                    foreach (T item in enumerable)
                    {
                        JsonSerializer.Serialize(writer, item, options);
                    }

                    writer.WriteEndArray();
                }
            }
        }
    }

    /// <summary>
    /// 天空之城JSON转换器（修复大数字前端数据丢失的问题）。
    /// </summary>
    public class MyJsonConverter : JsonConverter<object>
    {
        /// <summary>
        /// 是否可以调整。
        /// </summary>
        /// <param name="typeToConvert">数据类型。</param>
        /// <returns></returns>
        public override bool CanConvert(Type typeToConvert) => typeToConvert.IsValueType || typeof(string) == typeToConvert;

        private static bool TryRead(Utf8JsonReader reader, Type typeToConvert, out object value)
        {
            bool flag = true;

            try
            {
                switch (Type.GetTypeCode(typeToConvert))
                {
                    case TypeCode.DBNull:
                        value = null;
                        break;
                    case TypeCode.Boolean:
                        value = reader.GetBoolean();
                        break;
                    case TypeCode.SByte:
                        value = reader.GetSByte();
                        break;
                    case TypeCode.Byte:
                        value = reader.GetByte();
                        break;
                    case TypeCode.Int16:
                        value = reader.GetInt16();
                        break;
                    case TypeCode.UInt16:
                        value = reader.GetUInt16();
                        break;
                    case TypeCode.Int32:
                        value = reader.GetInt32();
                        break;
                    case TypeCode.UInt32:
                        value = reader.GetUInt32();
                        break;
                    case TypeCode.Int64:
                        value = reader.GetInt64();
                        break;
                    case TypeCode.UInt64:
                        value = reader.GetUInt64();
                        break;
                    case TypeCode.Single:
                        value = reader.GetSingle();
                        break;
                    case TypeCode.Double:
                        value = reader.GetDouble();
                        break;
                    case TypeCode.Decimal:
                        value = reader.GetDecimal();
                        break;
                    case TypeCode.DateTime:
                        value = reader.GetDateTime();
                        break;
                    case TypeCode.String:
                        value = reader.GetString();
                        break;
                    case TypeCode.Char:
                    default:
                        value = null;
                        flag = false;
                        break;
                }
            }
            catch
            {
                value = null;
                flag = false;
            }

            return flag;
        }

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <param name="reader">读取器。</param>
        /// <param name="typeToConvert">目标类型。</param>
        /// <param name="options">配置。</param>
        /// <returns></returns>
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (typeToConvert.IsEnum)
            {
                return Enum.Parse(typeToConvert, Encoding.UTF8.GetString(reader.ValueSpan.ToArray()), true);
            }

            if (typeToConvert.IsNullable())
            {
                typeToConvert = Nullable.GetUnderlyingType(typeToConvert);
            }

            if (TryRead(reader, typeToConvert, out object value))
            {
                return value;
            }

            string text = Encoding.UTF8.GetString(reader.ValueSpan.ToArray());

            try
            {
                return Convert.ChangeType(text, typeToConvert);
            }
            catch (FormatException)
            {
                if (typeToConvert == typeof(bool) && int.TryParse(text, out int i))
                {
                    return i > 1;
                }
            }

            return Mapper.Cast(value, typeToConvert);
        }
        /// <summary>
        /// 写入。
        /// </summary>
        /// <param name="writer">写入器。</param>
        /// <param name="value">数据。</param>
        /// <param name="options">配置。</param>
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value is null)
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

namespace CodeArts.Mvc.Converters
{
    /// <summary>
    /// 天空之城JSON转换器（修复大数字前端数据丢失的问题）。
    /// </summary>
    public class MyJsonConverter : JsonConverter
    {
        /// <summary>
        /// 是否可以调整。
        /// </summary>
        /// <param name="objectType">数据类型。</param>
        /// <returns></returns>
        public override bool CanConvert(Type objectType) => objectType.IsValueType || typeof(string) == objectType;

        /// <summary>
        /// 改变数据。
        /// </summary>
        /// <param name="source">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        private object ChangeType(object source, Type conversionType)
        {
            if (source is null) return source;

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

            return Mapper.Cast(source, conversionType);
        }

        /// <summary>
        /// 读取JSON。
        /// </summary>
        /// <param name="reader">阅读器。</param>
        /// <param name="objectType">数据类型。</param>
        /// <param name="existingValue">值。</param>
        /// <param name="serializer">解析器。</param>
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
        /// 写入JSON。
        /// </summary>
        /// <param name="writer">写入器。</param>
        /// <param name="value">值。</param>
        /// <param name="serializer">解析器。</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is null)
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