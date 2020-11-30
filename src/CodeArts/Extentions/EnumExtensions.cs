using CodeArts;
using CodeArts.Runtime;
using System.ComponentModel;
using System.Linq;

namespace System
{
    /// <summary>
    /// 枚举扩展。
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// 获取枚举描述。
        /// </summary>
        /// <typeparam name="TEnum">枚举类型。</typeparam>
        /// <param name="enum">枚举值。</param>
        /// <exception cref="ArgumentException">参数不是枚举类型。</exception>
        /// <returns></returns>
        public static string GetText<TEnum>(this TEnum @enum) where TEnum : struct
        {
            var type = typeof(TEnum);

            if (!type.IsEnum)
            {
                throw new ArgumentException("参数类型不是枚举!");
            }

            var typeStore = TypeStoreItem.Get<TEnum>();

            string enumStr = @enum.ToString();

            if (typeStore.IsDefined<FlagsAttribute>())
            {
                var values = enumStr.Split(',').Select(x => x.Trim());

                if (!values.All(x => typeStore.FieldStores.Any(y => string.Equals(y.Name, x, StringComparison.InvariantCultureIgnoreCase))))
                {
                    return "N/A";
                }

                return string.Join("|", typeStore.FieldStores
                    .Where(x => values.Any(y => string.Equals(y, x.Name, StringComparison.InvariantCultureIgnoreCase)))
                    .Select(x =>
                    {
                        var desc2 = x.GetCustomAttribute<DescriptionAttribute>();

                        return desc2 is null ? x.Name : desc2.Description;
                    }));
            }

            var field = typeStore.FieldStores.FirstOrDefault(x => x.Name == enumStr);

            if (field is null)
            {
                return "N/A";
            }

            var desc = field.GetCustomAttribute<DescriptionAttribute>();

            return desc is null ? field.Name : desc.Description;
        }

        /// <summary>
        /// 转换为<see cref="int"/>。
        /// </summary>
        /// <typeparam name="TEnum">枚举类型。</typeparam>
        /// <param name="enum">枚举值。</param>
        /// <exception cref="ArgumentException">参数不是枚举类型。</exception>
        /// <exception cref="InvalidCastException">枚举基类型不能隐式转化为<see cref="int"/>。</exception>
        /// <returns></returns>
        public static int ToInt32<TEnum>(this TEnum @enum) where TEnum : struct
        {
            var type = typeof(TEnum);

            if (!type.IsEnum)
            {
                throw new ArgumentException("参数类型不是枚举!");
            }

            var conversionType = Enum.GetUnderlyingType(type);

            switch (Type.GetTypeCode(conversionType))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                    return (int)Convert.ChangeType(@enum, conversionType);
                default:
                    throw new InvalidCastException($"{@enum}的基础数据类型为“{conversionType.Name}”，不能安全转换为Int32！");
            }
        }

        /// <summary>
        /// 转换为<see cref="long"/>。
        /// </summary>
        /// <typeparam name="TEnum">枚举类型。</typeparam>
        /// <param name="enum">枚举值。</param>
        /// <exception cref="ArgumentException">参数不是枚举类型。</exception>
        /// <exception cref="InvalidCastException">枚举基类型不能隐式转化为<see cref="long"/>。</exception>
        /// <returns></returns>
        public static long ToInt64<TEnum>(this TEnum @enum) where TEnum : struct
        {
            var type = typeof(TEnum);

            if (!type.IsEnum)
            {
                throw new ArgumentException("参数类型不是枚举!");
            }

            var conversionType = Enum.GetUnderlyingType(type);

            switch (Type.GetTypeCode(conversionType))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                    return (long)Convert.ChangeType(@enum, conversionType);
                default:
                    throw new InvalidCastException($"{@enum}的基础数据类型为“{conversionType.Name}”，不能安全转换为Int64！");
            }
        }

        /// <summary>
        /// 获取枚举的数据类型字符串。
        /// </summary>
        /// <typeparam name="TEnum">枚举类型。</typeparam>
        /// <param name="enum">枚举值。</param>
        /// <exception cref="ArgumentException">参数不是枚举类型。</exception>
        /// <returns></returns>
        public static string ToValueString<TEnum>(this TEnum @enum) where TEnum : struct
        {
            var type = typeof(TEnum);

            if (!type.IsEnum)
            {
                throw new ArgumentException("参数类型不是枚举!");
            }

            var conversionType = Enum.GetUnderlyingType(type);

            var value = Convert.ChangeType(@enum, conversionType);

            return value.ToString();
        }
    }
}
