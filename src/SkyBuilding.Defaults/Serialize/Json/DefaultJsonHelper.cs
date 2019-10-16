#if NETSTANDARD2_1
using System;
using System.Text.Json;
using SkyBuilding.Serialize.Json;

namespace SkyBuilding.Serialize.Json
{
    /// <summary>
    /// 牛顿 JSON 序列化帮助类
    /// </summary>
    public class DefaultJsonHelper : IJsonHelper
    {
        /// <summary>
        /// 天空之城 json 命名
        /// </summary>
        private class SkyJsonNamingPolicy : JsonNamingPolicy
        {
            private readonly NamingType namingType;

            public SkyJsonNamingPolicy(NamingType namingType)
            {
                this.namingType = namingType;
            }

            public override string ConvertName(string name) => name.ToNamingCase(namingType);
        }

        /// <summary>
        /// JSON序列化设置
        /// </summary>
        /// <param name="namingType">命名方式</param>
        /// <param name="indented">是否缩进</param>
        /// <returns></returns>
        private static JsonSerializerOptions LoadSetting(NamingType namingType, bool indented = false)
        {
            var policy = new SkyJsonNamingPolicy(namingType);

            return new JsonSerializerOptions
            {
                WriteIndented = indented,
                DictionaryKeyPolicy = policy,
                PropertyNamingPolicy = policy
            };
        }

        /// <summary>
        /// 将JSON反序列化为对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json">JSON字符串</param>
        /// <param name="namingType">命名风格</param>
        /// <returns></returns>
        public T Json<T>(string json, NamingType namingType = NamingType.CamelCase)
        {
            return JsonSerializer.Deserialize<T>(json, LoadSetting(namingType));
        }

        /// <summary> 将JSON反序列化到匿名对象 </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <param name="anonymousTypeObject"></param>
        /// <param name="namingType"></param>
        /// <returns></returns>
        public T Json<T>(string json, T anonymousTypeObject, NamingType namingType = NamingType.CamelCase)
        {
            return (T)JsonSerializer.Deserialize(json, typeof(T), LoadSetting(namingType));
        }

        /// <summary>
        /// 对象序列化为JSON
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonObj">对象</param>
        /// <param name="namingType">命名风格</param>
        /// <param name="indented">是否缩进</param>
        /// <returns></returns>
        public string ToJson<T>(T jsonObj, NamingType namingType = NamingType.CamelCase, bool indented = false)
        {
            return JsonSerializer.Serialize(jsonObj, LoadSetting(namingType, indented));
        }
    }
}
#else
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SkyBuilding.Serialize.Json;
using System;

namespace SkyBuilding.Serialize.Json
{
    /// <summary>
    /// 牛顿 JSON 序列化帮助类
    /// </summary>
    public class DefaultJsonHelper : IJsonHelper
    {
        /// <summary>
        ///JSON序列化解析协议
        /// </summary>
        private class JsonContractResolver : DefaultContractResolver
        {
            private readonly NamingType _camelCase;
            /// <summary>
            /// 构造定义命名解析风格
            /// </summary>
            /// <param name="camelCase"></param>
            public JsonContractResolver(NamingType camelCase) => _camelCase = camelCase;

            /// <summary>
            /// 属性名解析
            /// </summary>
            /// <param name="propertyName"></param>
            /// <returns></returns>
            protected override string ResolvePropertyName(string propertyName)
            {
                switch (_camelCase)
                {
                    case NamingType.Normal:
                    case NamingType.CamelCase:
                    case NamingType.UrlCase:
                    case NamingType.PascalCase:
                        return propertyName.ToNamingCase(_camelCase);
                    default:
                        return base.ResolvePropertyName(propertyName);
                }
            }
        }

        private const string DateFormatString = "yyyy-MM-dd HH:mm:ss";


        /// <summary>
        /// JSON序列化设置
        /// </summary>
        /// <param name="namingType">命名方式</param>
        /// <param name="indented">是否缩进</param>
        /// <returns></returns>
        private static JsonSerializerSettings LoadSetting(NamingType namingType, bool indented = false)
        {
            var setting = new JsonSerializerSettings
            {
                ContractResolver = new JsonContractResolver(namingType)
            };
            if (indented)
                setting.Formatting = Formatting.Indented;
            setting.DateFormatHandling = DateFormatHandling.MicrosoftDateFormat;
            setting.DateFormatString = DateFormatString;
            setting.NullValueHandling = NullValueHandling.Ignore;
            return setting;
        }


        /// <summary>
        /// 将JSON反序列化为对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json">JSON字符串</param>
        /// <param name="namingType">命名风格</param>
        /// <returns></returns>
        public T Json<T>(string json, NamingType namingType = NamingType.CamelCase)
        {
            return JsonConvert.DeserializeObject<T>(json, LoadSetting(namingType));
        }

        /// <summary> 将JSON反序列化到匿名对象 </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <param name="anonymousTypeObject"></param>
        /// <param name="namingType"></param>
        /// <returns></returns>
        public T Json<T>(string json, T anonymousTypeObject, NamingType namingType = NamingType.CamelCase)
        {
            return JsonConvert.DeserializeAnonymousType(json, anonymousTypeObject, LoadSetting(namingType));
        }

        /// <summary>
        /// 对象序列化为JSON
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonObj">对象</param>
        /// <param name="namingType">命名风格</param>
        /// <param name="indented">是否缩进</param>
        /// <returns></returns>
        public string ToJson<T>(T jsonObj, NamingType namingType = NamingType.CamelCase, bool indented = false)
        {
            return JsonConvert.SerializeObject(jsonObj, LoadSetting(namingType, indented));
        }
    }
}
#endif