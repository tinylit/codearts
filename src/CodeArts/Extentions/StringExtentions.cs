using CodeArts;
using CodeArts.Config;
using System.Collections;
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
        private static readonly MethodInfo ConvertMethod = SettingsType.GetMethod("Convert");

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

                var sysConvertMethod = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });

                var namingMethod = typeof(StringExtentions).GetMethod(nameof(ToNamingCase), BindingFlags.Public | BindingFlags.Static);

                var enumerCase = typeStore.PropertyStores.Where(x => x.IsPublic && x.CanRead && !x.IsStatic).Select(info =>
                  {
                      var nameCst = Constant(info.Name);

                      var propertyExp = Property(parameterExp, info.Name);

                      var namingCst = Call(settingsExp, ResolvePropertyNameMethod, nameCst);

                      var propSugarAttr = info.GetCustomAttribute<PropSugarAttribute>();

                      if (!(propSugarAttr is null))
                      {
                          var factory = propSugarAttr.ToStringMethod;

                          var method = factory.Method;

                          return SwitchCase(method.IsStatic ? Call(null, method, propertyExp) : Call(Constant(factory.Target), method, propertyExp), namingCst);
                      }

                      Type memberType = info.MemberType;

                      Expression valueExp = Convert(propertyExp, typeof(object));

                      if (info.MemberType.IsEnum)
                      {
                          memberType = Enum.GetUnderlyingType(info.MemberType);

                          valueExp = Call(ChangeTypeMethod, valueExp, Constant(memberType));
                      }

                      return SwitchCase(Call(settingsExp, ConvertMethod, nameCst, Constant(memberType), valueExp), namingCst);
                  });

                var bodyExp = Call(null, ConcatMethod, Constant("{"), nameExp, Constant("}"));

                var switchExp = Switch(Call(settingsExp, ResolvePropertyNameMethod, nameExp), Condition(Equal(preserveUnknownExp, Constant(true)), bodyExp, defaultCst), null, enumerCase);

                var lamda = Lambda<Func<T, string, DefaultSettings, string>>(switchExp, parameterExp, nameExp, settingsExp);

                Invoke = lamda.Compile();
            }

            /// <summary>
            /// 调用
            /// </summary>
            public static readonly Func<T, string, DefaultSettings, string> Invoke;
        }

        /// <summary>
        /// 属性格式化语法糖(支持属性空字符串【空字符串运算符（A?B 或 A??B），当属性A为空或空字符串时，返回B内容，否则返回A内容】、属性内容合并(A+B)，属性非空字符串合并【空字符串试探合并符(A?+B)，当属性A为空或空字符串时，返回A内容，否则返回A和B的内容】，可以组合使用任意多个。如 {x?y?z} 或 {x+y+z} 或 {x+y?z} 等操作)。从左往右依次计算，不支持小括号。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">字符串</param>
        /// <param name="source">资源</param>
        /// <param name="namingType">比较的命名方式</param>
        /// <returns></returns>
        public static string PropSugar<T>(this string value, T source, NamingType namingType = NamingType.Normal) where T : class => PropSugar(value, source, SettingsCache.GetOrAdd(namingType, namingCase => new DefaultSettings(namingCase)));

        /// <summary>
        /// 属性格式化语法糖(支持属性空字符串【空字符串运算符（A?B 或 A??B），当属性A为空或空字符串时，返回B内容，否则返回A内容】、属性内容合并(A+B)，属性非空字符串合并【空字符串试探合并符(A?+B)，当属性A为空或空字符串时，返回A内容，否则返回A和B的内容】，可以组合使用任意多个。如 {x?y?z} 或 {x+y+z} 或 {x+y?z} 等操作)。从左往右依次计算，不支持小括号。
        /// </summary>
        /// <typeparam name="T"></typeparam>
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
                    return Nested<T>.Invoke(source, nameGrp.Value, settings);

                var nameCap = nameGrp.Captures.GetEnumerator();

                var tokenCap = tokenGrp.Captures.GetEnumerator();

                string valueStr = string.Empty;

                while (nameCap.MoveNext())
                {
                    string text = Nested<T>.Invoke(source, ((Capture)nameCap.Current).Value, settings);

                    if (!(text is null))
                    {
                        valueStr += text;
                    }

                    if (!tokenCap.MoveNext())
                        return valueStr;

                    string token = ((Capture)tokenCap.Current).Value;

                    if (valueStr.Length > 0)
                    {
                        if (token == "?" || token == "??")
                            return valueStr;
                    }
                    else if (token == "?+")
                    {
                        return valueStr;
                    }
                }

                return valueStr;
            });
        }
    }
}
