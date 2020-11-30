namespace CodeArts.Serialize.Json
{
    /// <summary>
    /// JSON 助手。
    /// </summary>
    public static class JsonHelper
    {
        private static readonly IJsonHelper _jsonHelper;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static JsonHelper() => _jsonHelper = RuntimeServPools.Singleton<IJsonHelper>();

        /// <summary> Json序列化。 </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="jsonObj">对象。</param>
        /// <param name="namingType">命名规则。</param>
        /// <param name="indented">是否缩进。</param>
        /// <returns></returns>
        public static string ToJson<T>(T jsonObj, NamingType namingType = NamingType.CamelCase, bool indented = false)
            => _jsonHelper.ToJson(jsonObj, namingType, indented);

        /// <summary> Json反序列化。 </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="json">JSON 字符串。</param>
        /// <param name="namingType">命名规则。</param>
        /// <returns></returns>
        public static T Json<T>(string json, NamingType namingType = NamingType.CamelCase)
            => _jsonHelper.Json<T>(json, namingType);

        /// <summary> 匿名对象反序列化。 </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="json">JSON 字符串。</param>
        /// <param name="anonymousTypeObject">匿名对象类型。</param>
        /// <param name="namingType">命名规则。</param>
        /// <returns></returns>
        public static T Json<T>(string json, T anonymousTypeObject, NamingType namingType = NamingType.CamelCase)
            => _jsonHelper.Json(json, anonymousTypeObject, namingType);
    }
}
