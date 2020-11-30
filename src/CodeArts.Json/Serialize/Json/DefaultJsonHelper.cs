using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace CodeArts.Serialize.Json
{
    /// <summary>
    /// 牛顿 JSON 序列化帮助类。
    /// </summary>
    public class DefaultJsonHelper : IJsonHelper
    {
        /// <summary>
        ///JSON序列化解析协议。
        /// </summary>
        private class JsonContractResolver : DefaultContractResolver
        {
            private readonly NamingType _camelCase;
            /// <summary>
            /// 构造定义命名解析风格。
            /// </summary>
            /// <param name="namingCase">命名规则。</param>
            public JsonContractResolver(NamingType namingCase) => _camelCase = namingCase;

            /// <summary>
            /// 属性名解析。
            /// </summary>
            /// <param name="propertyName">属性名称。</param>
            /// <returns></returns>
            protected override string ResolvePropertyName(string propertyName)
            {
                switch (_camelCase)
                {
                    case NamingType.CamelCase:
                    case NamingType.UrlCase:
                    case NamingType.PascalCase:
                        return propertyName.ToNamingCase(_camelCase);
                    default:
                        return base.ResolvePropertyName(propertyName);
                }
            }
        }

        private const string DateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK";
        private readonly JsonSerializerSettings settings;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public DefaultJsonHelper()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="settings">配置。</param>
        public DefaultJsonHelper(JsonSerializerSettings settings) => this.settings = settings;

        /// <summary>
        /// JSON序列化设置。
        /// </summary>
        /// <param name="namingType">命名方式。</param>
        /// <param name="indented">是否缩进。</param>
        /// <returns></returns>
        private static JsonSerializerSettings LoadSetting(NamingType namingType, bool indented = false)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new JsonContractResolver(namingType)
            };

            if (indented)
            {
                settings.Formatting = Formatting.Indented;
            }

            settings.DateFormatHandling = DateFormatHandling.MicrosoftDateFormat;
            settings.DateFormatString = DateFormatString;
            settings.NullValueHandling = NullValueHandling.Ignore;
            return settings;
        }

        /// <summary>
        /// JSON序列化设置。
        /// </summary>
        /// <param name="settings">配置。</param>
        /// <param name="namingType">命名方式。</param>
        /// <param name="indented">是否缩进。</param>
        /// <returns></returns>
        private static JsonSerializerSettings LoadSetting(JsonSerializerSettings settings, NamingType namingType, bool indented = false)
        {
            if (settings is null)
            {
                return LoadSetting(namingType, indented);
            }

            if (settings.ContractResolver is null)
            {
                settings.ContractResolver = new JsonContractResolver(namingType);
            }

            if (indented)
            {
                settings.Formatting = Formatting.Indented;
            }

            return settings;
        }

        /// <summary>
        /// 将JSON反序列化为对象。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="json">JSON字符串。</param>
        /// <param name="namingType">命名规则。</param>
        /// <returns></returns>
        public T Json<T>(string json, NamingType namingType = NamingType.Normal)
        {
            return JsonConvert.DeserializeObject<T>(json, LoadSetting(settings, namingType));
        }

        /// <summary> 将JSON反序列化到匿名对象。 </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="json">JSON字符串。</param>
        /// <param name="anonymousTypeObject">匿名对象。</param>
        /// <param name="namingType">命名规则。</param>
        /// <returns></returns>
        public T Json<T>(string json, T anonymousTypeObject, NamingType namingType = NamingType.Normal)
        {
            return JsonConvert.DeserializeAnonymousType(json, anonymousTypeObject, LoadSetting(settings, namingType));
        }

        /// <summary>
        /// 对象序列化为JSON。
        /// </summary>
        /// <typeparam name="T">数据类型。</typeparam>
        /// <param name="jsonObj">对象。</param>
        /// <param name="namingType">命名规则。</param>
        /// <param name="indented">是否缩进。</param>
        /// <returns></returns>
        public string ToJson<T>(T jsonObj, NamingType namingType = NamingType.Normal, bool indented = false)
        {
            return JsonConvert.SerializeObject(jsonObj, LoadSetting(settings, namingType, indented));
        }
    }
}