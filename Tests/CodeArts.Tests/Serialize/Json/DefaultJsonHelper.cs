using Newtonsoft.Json;
using CodeArts.Serialize.Json;
using System.Collections.Generic;
using System.IO;

namespace CodeArts.Tests.Serialize.Json
{
    /// <summary>
    /// 默认JSON序列化帮助类
    /// </summary>
    public class DefaultJsonHelper : IJsonHelper
    {
        #region 序列化规则配置

        /// <summary>
        /// JSON序列化设置
        /// </summary>
        /// <param name="namingType">命名方式</param>
        /// <param name="indented">是否缩进</param>
        /// <returns></returns>
        private static JsonSerializerSettings LoadSetting(NamingType namingType, bool indented)
        {
            var setting = new JsonSerializerSettings
            {
                ContractResolver = new JsonContractResolver(namingType)
            };
            if (indented)
                setting.Formatting = Formatting.Indented;
            setting.DateFormatHandling = DateFormatHandling.MicrosoftDateFormat;
            setting.DateFormatString = "yyyy-MM-dd HH:mm:ss";
            setting.NullValueHandling = NullValueHandling.Ignore;
            return setting;
        }

        #endregion

        #region JSON反序列化到对象

        /// <summary>
        /// 将JSON反序列化为对象
        /// </summary>
        /// <typeparam name="T">结果类型</typeparam>
        /// <param name="json">JSON字符串</param>
        /// <param name="namingType">命名风格</param>
        /// <returns></returns>
        public T Json<T>(string json, NamingType namingType = NamingType.Normal)
        {
            return JsonConvert.DeserializeObject<T>(json, LoadSetting(namingType, false));
        }

        /// <summary> 将JSON反序列化到匿名对象 </summary>
        /// <typeparam name="T">结果类型</typeparam>
        /// <param name="json">JSON字符串</param>
        /// <param name="anonymousTypeObject">匿名对象</param>
        /// <param name="namingType">命名规则</param>
        /// <returns></returns>
        public T Json<T>(string json, T anonymousTypeObject, NamingType namingType = NamingType.Normal)
        {
            return JsonConvert.DeserializeAnonymousType(json, anonymousTypeObject, LoadSetting(namingType, false));
        }

        /// <summary> 将JSON反序列化json为列表 </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="json">JSON字符串</param>
        /// <param name="namingType">命名规则</param>
        /// <returns></returns>
        public IEnumerable<T> JsonList<T>(string json, NamingType namingType = NamingType.Normal)
        {
            var serializer = new JsonSerializer();
            if (namingType != NamingType.Normal)
                serializer.ContractResolver = new JsonContractResolver(namingType);
            using (var sr = new StringReader(json))
            {
                using (var jsonReader = new JsonTextReader(sr))
                {
                    var obj = serializer.Deserialize(jsonReader, typeof(IEnumerable<T>));
                    return obj as IEnumerable<T>;
                }
            }
        }

        #endregion

        #region 对象序列化到JSON

        /// <summary>
        /// 对象序列化为JSON
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="jsonObj">对象</param>
        /// <param name="namingType">命名风格</param>
        /// <param name="indented">是否缩进</param>
        /// <returns></returns>
        public string ToJson<T>(T jsonObj, NamingType namingType = NamingType.Normal, bool indented = false)
        {
            return JsonConvert.SerializeObject(jsonObj, LoadSetting(namingType, indented));
        }

        #endregion
    }
}
