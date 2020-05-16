using CodeArts;
using CodeArts.Config;
using CodeArts.Runtime;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using static System.Linq.Expressions.Expression;

namespace System
{
    /// <summary>
    /// 字符串扩展
    /// </summary>
    public static class StringExtentions
    {
        private static readonly Regex Whitespace = new Regex("[\\x20\\t\\r\\n\\f]", RegexOptions.Compiled);

        private static readonly Regex Pattern = new Regex("\\{(?<name>\\w+)([\\x20\\t\\r\\n\\f]*(?<token>(\\??[?+]))[\\x20\\t\\r\\n\\f]*(?<name>\\w+))*\\}", RegexOptions.Multiline);

        private static readonly Regex PatternCamelCase = new Regex("_(?<letter>([a-z]))", RegexOptions.Singleline & RegexOptions.Compiled);

        private static readonly Regex PatternPascalCase = new Regex("(^|_)(?<letter>([a-z]))", RegexOptions.Singleline & RegexOptions.Compiled);

        private static readonly Regex PatternUrlCamelCase = new Regex("(?<letter>([A-Z]))", RegexOptions.Singleline & RegexOptions.Compiled);

        private static readonly Regex PatternMail = new Regex(@"\w[-\w.+]*@([A-Za-z0-9][-A-Za-z0-9]+\.)+[A-Za-z]{2,14}", RegexOptions.Compiled);

        private static readonly ConcurrentDictionary<NamingType, DefaultSettings> SettingsCache = new ConcurrentDictionary<NamingType, DefaultSettings>();

        /// <summary>
        /// 命名
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="namingType">命名规则</param>
        /// <returns></returns>
        public static string ToNamingCase(this string name, NamingType namingType)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("名称不能为空！", nameof(name));
            }

            if (Whitespace.IsMatch(name))
                throw new ArgumentException($"“{name}”不是一个有效的名称。", nameof(name));

            switch (namingType)
            {
                case NamingType.Normal:
                    return name;
                case NamingType.CamelCase:

                    if (char.IsUpper(name[0]))
                    {
                        return name.Substring(0, 1).ToLower() + PatternCamelCase.Replace(name.Substring(1), x =>
                        {
                            return x.Groups["letter"].Value.ToUpper();
                        });
                    }

                    return PatternCamelCase.Replace(name, x =>
                        {
                            return x.Groups["letter"].Value.ToUpper();
                        });
                case NamingType.UrlCase:
                    return PatternUrlCamelCase.Replace(name, x =>
                    {
                        var match = x.Groups["letter"];
                        var value = match.Value.ToLower();
                        if (match.Index > 0)
                        {
                            value = string.Concat("_", value);
                        }
                        return value;
                    });
                case NamingType.PascalCase:
                    return PatternPascalCase.Replace(name, x =>
                    {
                        return x.Groups["letter"].Value.ToUpper();
                    });
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary> 帕斯卡命名法 </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static string ToPascalCase(this string name) => ToNamingCase(name, NamingType.PascalCase);

        /// <summary>
        /// 驼峰命名
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static string ToCamelCase(this string name) => ToNamingCase(name, NamingType.CamelCase);

        /// <summary> url命名法 </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static string ToUrlCase(this string name) => ToNamingCase(name, NamingType.UrlCase);

        /// <summary>
        /// 是否为NULL。
        /// </summary>
        /// <param name="value">字符串</param>
        /// <returns></returns>
        public static bool IsNull(this string value) => value is null;

        /// <summary>
        /// 指示指定的字符串是 null 或是 空字符串 ("")。
        /// </summary>
        /// <param name="value">字符串</param>
        /// <returns></returns>
        public static bool IsEmpty(this string value) => value is null || value.Length == 0;

        /// <summary>
        /// 原内容不为null时，返回原字符，否则返回空字符串（""）。
        /// </summary>
        /// <param name="value">字符串</param>
        /// <returns></returns>
        public static string OrEmpty(this string value) => value ?? string.Empty;

        /// <summary>
        /// 内容是邮箱。
        /// </summary>
        /// <param name="value">字符串</param>
        /// <returns></returns>
        public static bool IsMail(this string value) => PatternMail.IsMatch(value);

        /// <summary>
        /// 配置文件读取
        /// </summary>
        /// <typeparam name="T">读取数据类型</typeparam>
        /// <param name="configName">健</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns></returns>
        public static T Config<T>(this string configName, T defaultValue = default) => ConfigHelper.Instance.Get(configName, defaultValue);

        private static MethodInfo GetMethodInfo(Func<string, string, string, string> func) => func.Method;

        private static readonly MethodInfo ChangeTypeMethod = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });
        private static readonly MethodInfo ConcatMethod = GetMethodInfo(string.Concat);
        private static readonly Type SettingsType = typeof(DefaultSettings);
        private static readonly MethodInfo ResolvePropertyNameMethod = SettingsType.GetMethod("ResolvePropertyName");
        private static readonly MethodInfo ConvertMethod = SettingsType.GetMethod("Convert", new Type[] { typeof(PropertyStoreItem), typeof(object) });

        /// <summary>
        /// 内嵌的
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        private static class Nested<T>
        {
            /// <summary>
            /// 静态构造函数
            /// </summary>
            static Nested()
            {
                var type = typeof(T);

                var typeStore = RuntimeTypeCache.Instance.GetCache(type);

                var defaultCst = Constant(string.Empty);

                var parameterExp = Parameter(type, "source");

                var nameExp = Parameter(typeof(string), "name");

                var settingsExp = Parameter(SettingsType, "settings");

                var preserveUnknownExp = Property(settingsExp, "PreserveUnknownPropertyToken");

                var nullValueExp = Property(settingsExp, "NullValue");

                var sysConvertMethod = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });

                var namingMethod = typeof(StringExtentions).GetMethod(nameof(ToNamingCase), BindingFlags.Public | BindingFlags.Static);

                var propertyStores = typeStore.PropertyStores.Where(x => x.IsPublic && x.CanRead && !x.IsStatic).ToList();

                var enumerCase = propertyStores.Select(info =>
                  {
                      Type memberType = info.MemberType;

                      ConstantExpression nameCst = Constant(info.Name);

                      MemberExpression propertyExp = Property(parameterExp, info.Name);

                      var namingCst = Call(settingsExp, ResolvePropertyNameMethod, nameCst);

                      if (memberType.IsValueType)
                      {
                          Expression valueExp = Expression.Convert(propertyExp, typeof(object));

                          if (memberType.IsNullable())
                          {
                              return SwitchCase(Condition(Equal(valueExp, Constant(null, memberType)), nullValueExp, Call(settingsExp, ConvertMethod, Constant(info), valueExp)), namingCst);
                          }

                          if (memberType.IsEnum)
                          {
                              return SwitchCase(Call(settingsExp, ConvertMethod, Constant(info), Call(ChangeTypeMethod, valueExp, Constant(Enum.GetUnderlyingType(memberType)))), namingCst);
                          }

                          return SwitchCase(Call(settingsExp, ConvertMethod, Constant(info), valueExp), namingCst);
                      }

                      return SwitchCase(Condition(Equal(propertyExp, Constant(null, memberType)), nullValueExp, Call(settingsExp, ConvertMethod, Constant(info), propertyExp)), namingCst);
                  });

                var bodyExp = Call(null, ConcatMethod, Constant("{"), nameExp, Constant("}"));

                var switchExp = Switch(Call(settingsExp, ResolvePropertyNameMethod, nameExp), Condition(preserveUnknownExp, bodyExp, defaultCst), null, enumerCase);

                var lamda = Lambda<Func<T, string, DefaultSettings, string>>(switchExp, parameterExp, nameExp, settingsExp);

                Convert = lamda.Compile();

                var enumerCase2 = propertyStores.Select(info =>
                {
                    Type memberType = info.MemberType;

                    var nameCst = Constant(info.Name);

                    var propertyExp = Property(parameterExp, info.Name);

                    var namingCst = Call(settingsExp, ResolvePropertyNameMethod, nameCst);

                    var valueExp = Expression.Convert(propertyExp, typeof(object));

                    if (memberType.IsEnum)
                    {
                        return SwitchCase(Call(ChangeTypeMethod, valueExp, Constant(Enum.GetUnderlyingType(memberType))), namingCst);
                    }

                    return SwitchCase(valueExp, namingCst);
                });

                var switchExp2 = Switch(Call(settingsExp, ResolvePropertyNameMethod, nameExp), Constant(null, typeof(object)), null, enumerCase2);

                var lamda2 = Lambda<Func<T, string, DefaultSettings, object>>(switchExp2, parameterExp, nameExp, settingsExp);

                GetPropertyValue = lamda2.Compile();
            }

            /// <summary>
            /// 调用
            /// </summary>
            public static readonly Func<T, string, DefaultSettings, string> Convert;

            /// <summary>
            /// 获取属性值。
            /// </summary>
            public static readonly Func<T, string, DefaultSettings, object> GetPropertyValue;
        }

        private static class IgnoreCaseNested<T>
        {
            private static bool Compare(string arg1, string arg2)
            {
                return string.Equals(arg1, arg2, StringComparison.OrdinalIgnoreCase);
            }
            /// <summary>
            /// 静态构造函数
            /// </summary>
            static IgnoreCaseNested()
            {
                var type = typeof(T);

                var typeStore = RuntimeTypeCache.Instance.GetCache(type);

                var defaultCst = Constant(string.Empty);

                var parameterExp = Parameter(type, "source");

                var nameExp = Parameter(typeof(string), "name");

                var settingsExp = Parameter(SettingsType, "settings");

                var preserveUnknownExp = Property(settingsExp, "PreserveUnknownPropertyToken");

                var nullValueExp = Property(settingsExp, "NullValue");

                var sysConvertMethod = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });

                var namingMethod = typeof(StringExtentions).GetMethod(nameof(ToNamingCase), BindingFlags.Public | BindingFlags.Static);

                MethodInfo comparison = typeof(IgnoreCaseNested<T>).GetMethod(nameof(Compare), BindingFlags.NonPublic | BindingFlags.Static);

                var propertyStores = typeStore.PropertyStores.Where(x => x.IsPublic && x.CanRead && !x.IsStatic).ToList();

                var enumerCase = propertyStores.Select(info =>
                  {
                      Type memberType = info.MemberType;

                      ConstantExpression nameCst = Constant(info.Name);

                      MemberExpression propertyExp = Property(parameterExp, info.Name);

                      var namingCst = Call(settingsExp, ResolvePropertyNameMethod, nameCst);

                      if (memberType.IsValueType)
                      {
                          Expression valueExp = Expression.Convert(propertyExp, typeof(object));

                          if (memberType.IsNullable())
                          {
                              return SwitchCase(Condition(Equal(valueExp, Constant(null, memberType)), nullValueExp, Call(settingsExp, ConvertMethod, Constant(info), valueExp)), namingCst);
                          }

                          if (memberType.IsEnum)
                          {
                              return SwitchCase(Call(settingsExp, ConvertMethod, Constant(info), Call(ChangeTypeMethod, valueExp, Constant(Enum.GetUnderlyingType(memberType)))), namingCst);
                          }

                          return SwitchCase(Call(settingsExp, ConvertMethod, Constant(info), valueExp), namingCst);
                      }

                      return SwitchCase(Condition(Equal(propertyExp, Constant(null, memberType)), nullValueExp, Call(settingsExp, ConvertMethod, Constant(info), propertyExp)), namingCst);
                  });

                var bodyExp = Call(null, ConcatMethod, Constant("{"), nameExp, Constant("}"));

                var switchExp = Switch(Call(settingsExp, ResolvePropertyNameMethod, nameExp), Condition(preserveUnknownExp, bodyExp, defaultCst), comparison, enumerCase);

                var lamda = Lambda<Func<T, string, DefaultSettings, string>>(switchExp, parameterExp, nameExp, settingsExp);

                Convert = lamda.Compile();

                var enumerCase2 = propertyStores.Select(info =>
                {
                    Type memberType = info.MemberType;

                    var nameCst = Constant(info.Name);

                    var propertyExp = Property(parameterExp, info.Name);

                    var namingCst = Call(settingsExp, ResolvePropertyNameMethod, nameCst);

                    var valueExp = Expression.Convert(propertyExp, typeof(object));

                    if (memberType.IsEnum)
                    {
                        return SwitchCase(Call(ChangeTypeMethod, valueExp, Constant(Enum.GetUnderlyingType(memberType))), namingCst);
                    }

                    return SwitchCase(valueExp, namingCst);
                });

                var switchExp2 = Switch(Call(settingsExp, ResolvePropertyNameMethod, nameExp), Constant(null, typeof(object)), null, enumerCase2);

                var lamda2 = Lambda<Func<T, string, DefaultSettings, object>>(switchExp2, parameterExp, nameExp, settingsExp);

                GetPropertyValue = lamda2.Compile();
            }

            /// <summary>
            /// 调用
            /// </summary>
            public static readonly Func<T, string, DefaultSettings, string> Convert;

            /// <summary>
            /// 获取属性值。
            /// </summary>
            public static readonly Func<T, string, DefaultSettings, object> GetPropertyValue;
        }

        private static object Add(DefaultSettings settings, object left, object right)
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

            if (leftType.IsEnum)
            {
                leftType = Enum.GetUnderlyingType(leftType);

                left = Convert.ChangeType(left, leftType);

                left = string.Concat("[", left.ToString(), "]");
            }

            if (rightType.IsEnum)
            {
                rightType = Enum.GetUnderlyingType(rightType);

                right = Convert.ChangeType(right, rightType);

                right = string.Concat("[", right.ToString(), "]");
            }

            return settings.Convert(left, false) + settings.Convert(right, false);
        }

        /// <summary>
        /// 属性格式化语法糖(支持属性空字符串【空字符串运算符（A?B 或 A??B），当属性A为“null”时，返回B内容，否则返回A内容】、属性内容合并(A+B)，属性非“null”合并【空试探合并符(A?+B)，当属性A为“null”时，返回A内容，否则返回A和B的内容】，可以组合使用任意多个。如 {x?y?z} 或 {x+y+z} 或 {x+y?z} 等操作)。从左往右依次计算，不支持小括号。
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="value">字符串</param>
        /// <param name="source">资源</param>
        /// <param name="namingType">比较的命名方式</param>
        /// <returns></returns>
        public static string PropSugar<T>(this string value, T source, NamingType namingType = NamingType.Normal) where T : class => PropSugar(value, source, SettingsCache.GetOrAdd(namingType, namingCase => new DefaultSettings(namingCase)));

        /// <summary>
        /// 属性格式化语法糖(支持属性空字符串【空字符串运算符（A?B 或 A??B），当属性A为“null”时，返回B内容，否则返回A内容】、属性内容合并(A+B)，属性非“null”合并【空试探合并符(A?+B)，当属性A为“null”时，返回A内容，否则返回B的内容】，可以组合使用任意多个。如 {x?y?z} 或 {x+y+z} 或 {x+y?z} 等操作)。从左往右依次计算，不支持小括号。
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="value">字符串</param>
        /// <param name="source">资源</param>
        /// <param name="settings">属性配置</param>
        /// <returns></returns>
        public static string PropSugar<T>(this string value, T source, DefaultSettings settings) where T : class
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            return Pattern.Replace(value, match =>
            {
                var nameGrp = match.Groups["name"];

                var tokenGrp = match.Groups["token"];

                if (!tokenGrp.Success)
                {
                    if (settings.IgnoreCase)
                    {
                        return IgnoreCaseNested<T>.Convert(source, nameGrp.Value, settings);
                    }

                    return Nested<T>.Convert(source, nameGrp.Value, settings);
                }

                object result = null;

                var tokenCap = tokenGrp.Captures.GetEnumerator();

                foreach (Capture item in nameGrp.Captures)
                {
                    result = Add(settings, result, settings.IgnoreCase ? IgnoreCaseNested<T>.GetPropertyValue(source, item.Value, settings) : Nested<T>.GetPropertyValue(source, item.Value, settings));

                    if (!tokenCap.MoveNext())
                    {
                        break;
                    }

                    string token = ((Capture)tokenCap.Current).Value;

                    if (result is null)
                    {
                        if (token == "?+")
                        {
                            return settings.NullValue;
                        }

                        continue;
                    }

                    if (token == "?" || token == "??")
                    {
                        return settings.Convert(result);
                    }
                }

                return settings.Convert(result);
            });
        }
    }
}
