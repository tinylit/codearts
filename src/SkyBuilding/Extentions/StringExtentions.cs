using SkyBuilding;
using SkyBuilding.Config;
using System.Collections;
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

        private static readonly Regex PatternProperty = new Regex("\\{(?<name>(\\w+))\\}", RegexOptions.Multiline);

        private static readonly Regex PatternCamelCase = new Regex("_(?<letter>([a-z]))", RegexOptions.Singleline & RegexOptions.Compiled);

        private static readonly Regex PatternPascalCase = new Regex("(^|_)(?<letter>([a-z]))", RegexOptions.Singleline & RegexOptions.Compiled);

        private static readonly Regex PatternUrlCamelCase = new Regex("(?<letter>([A-Z]))", RegexOptions.Singleline & RegexOptions.Compiled);

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

        /// <summary>
        /// 内嵌的
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        private static class Nested<T>
        {
            private static MethodInfo GetJoinMethodInfo(Func<IEnumerable, string, string> func) => func.Method;
            private static MethodInfo GetMethodInfo(Func<string, string, string, string> func) => func.Method;

            /// <summary>
            /// 静态构造函数
            /// </summary>
            static Nested()
            {
                var type = typeof(T);

                var typeStore = RuntimeTypeCache.Instance.GetCache(type);

                var defaultCst = Constant(string.Empty);

                var parameterExp = Parameter(type, "source");

                var parameterNameExp = Parameter(typeof(string), "name");

                var parameterLostMatchExp = Parameter(typeof(bool), "lostMacth");

                var concatExp = GetMethodInfo(string.Concat);

                var enumerCase = typeStore.PropertyStores.Select(info =>
                {
                    var nameCst = Constant(info.Name);

                    var propertyExp = Property(parameterExp, info.Name);

                    var propSugarAttr = info.GetCustomAttribute<PropSugarAttribute>();

                    if (!(propSugarAttr is null))
                    {
                        var factory = propSugarAttr.ToStringMethod;

                        var method = factory.Method;

                        return SwitchCase(method.IsStatic ? Call(null, method, propertyExp) : Call(Constant(factory.Target), method, propertyExp), nameCst);
                    }

                    if (info.MemberType == typeof(string))
                    {
                        return SwitchCase(Coalesce(propertyExp, defaultCst), nameCst);
                    }

                    if (info.MemberType.IsArray || typeof(IEnumerable).IsAssignableFrom(info.MemberType))
                    {
                        var joinMethod = GetJoinMethodInfo(IEnumerableExtentions.Join);

                        var coreExp = Call(null, joinMethod, Convert(propertyExp, typeof(IEnumerable)), Constant(","));

                        return SwitchCase(Call(null, concatExp, Constant("["), coreExp, Constant("]")), nameCst);
                    }

                    var toStringMethod = info.MemberType.GetMethod("ToString", new Type[] { });

                    var body = Call(propertyExp, toStringMethod);

                    if (info.MemberType.IsClass || info.MemberType.IsNullable())
                    {
                        var testExp = Equal(propertyExp, Constant(null, info.MemberType));

                        return SwitchCase(Condition(testExp, defaultCst, body), nameCst);
                    }

                    return SwitchCase(body, nameCst);
                });


                var bodyExp = Call(null, concatExp, Constant("{"), parameterNameExp, Constant("}"));

                var switchExp = Switch(parameterNameExp, Condition(Equal(parameterLostMatchExp, Constant(true)), bodyExp, defaultCst), null, enumerCase);

                var lamda = Lambda<Func<T, string, bool, string>>(switchExp, parameterExp, parameterNameExp, parameterLostMatchExp);

                Invoke = lamda.Compile();
            }

            /// <summary>
            /// 调用
            /// </summary>
            public static readonly Func<T, string, bool, string> Invoke;
        }

        /// <summary>
        /// 属性格式化语法糖
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">字符串</param>
        /// <param name="source">资源</param>
        /// <param name="keepLostMatch">保留迷失的匹配</param>
        /// <returns></returns>
        public static string PropSugar<T>(this string value, T source, bool keepLostMatch = false) where T : class
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return PatternProperty.Replace(value, match =>
            {
                return Nested<T>.Invoke(source, match.Groups["name"].Value, keepLostMatch);
            });
        }
    }
}
