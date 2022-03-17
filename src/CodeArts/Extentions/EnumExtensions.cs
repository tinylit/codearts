using CodeArts.Runtime;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using static System.Linq.Expressions.Expression;

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

        if (type.IsDefined(typeof(FlagsAttribute), false))
        {
            var sb = new StringBuilder();

            foreach (var info in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                var constantValue = info.GetRawConstantValue();

                if (!Nested<TEnum>.Contains(@enum, (TEnum)constantValue))
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append('|');
                }

#if NET40
                var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(info, typeof(DescriptionAttribute), false);
#else
                var attribute = info.GetCustomAttribute<DescriptionAttribute>();
#endif

                sb.Append(attribute is null ? info.Name : attribute.Description);
            }

            return sb.ToString();
        }

        if (Enum.IsDefined(type, @enum))
        {
            foreach (var info in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                var constantValue = info.GetRawConstantValue();

                if (!Nested<TEnum>.Equals(@enum, (TEnum)constantValue))
                {
                    continue;
                }

#if NET40
            var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(info, typeof(DescriptionAttribute), false);
#else
                var attribute = info.GetCustomAttribute<DescriptionAttribute>();
#endif

                return attribute is null ? info.Name : attribute.Description;
            }
        }

        return "N/A";
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
                return @enum.GetHashCode();
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
                return @enum.GetHashCode();
            case TypeCode.UInt32:
            case TypeCode.Int64:
                return (long)System.Convert.ChangeType(@enum, conversionType);
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

        var value = System.Convert.ChangeType(@enum, conversionType);

        return value.ToString();
    }

    /// <summary>
    /// 获取所有枚举项，标记<see cref="FlagsAttribute"/>的枚举，会返回多个枚举项。
    /// </summary>
    /// <typeparam name="TEnum">枚举类型。</typeparam>
    /// <param name="enum">值。</param>
    /// <returns></returns>
    public static TEnum[] ToValues<TEnum>(this TEnum @enum) where TEnum : struct
    {
        var type = typeof(TEnum);

        if (!type.IsEnum)
        {
            throw new ArgumentException("参数类型不是枚举!");
        }

        if (type.IsDefined(typeof(FlagsAttribute), false))
        {
            var results = new List<TEnum>();

            foreach (TEnum item in Enum.GetValues(type))
            {
                if (Nested<TEnum>.Contains(@enum, item))
                {
                    results.Add(item);
                }
            }

            return results.ToArray();
        }

        if (!Enum.IsDefined(type, @enum))
        {
            return new TEnum[0];
        }

        return new TEnum[1] { @enum };
    }
    private static class Nested<TEnum> where TEnum : struct
    {
        private static readonly Func<TEnum, TEnum, bool> equals;
        private static readonly Func<TEnum, TEnum, bool> contains;
        static Nested()
        {
            var type = typeof(TEnum);

            var leftEx = Parameter(type);
            var rightEx = Parameter(type);

            var underlyingType = Enum.GetUnderlyingType(type);

            var variableLeftEx = Variable(underlyingType);
            var variableRightEx = Variable(underlyingType);

            var bodyEx = OrElse(Equal(variableLeftEx, variableRightEx), AndAlso(NotEqual(variableLeftEx, Default(underlyingType)), Equal(And(variableLeftEx, variableRightEx), variableRightEx)));

            var lambdaEx = Lambda<Func<TEnum, TEnum, bool>>(Block(typeof(bool), new[] { variableLeftEx, variableRightEx }, Assign(variableLeftEx, Convert(leftEx, underlyingType)), Assign(variableRightEx, Convert(rightEx, underlyingType)), bodyEx), leftEx, rightEx);

            contains = lambdaEx.Compile();

            var lambdaEqualEx = Lambda<Func<TEnum, TEnum, bool>>(Equal(leftEx, rightEx), leftEx, rightEx);

            equals = lambdaEqualEx.Compile();
        }

        public static bool Equals(TEnum left, TEnum right) => equals.Invoke(left, right);
        public static bool Contains(TEnum left, TEnum right) => contains.Invoke(left, right);
    }
}

