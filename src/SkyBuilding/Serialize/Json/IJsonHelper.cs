namespace SkyBuilding.Serialize.Json
{
    /// <summary>
    /// JSON序列化
    /// </summary>
    public interface IJsonHelper
    {
        /// <summary> Json序列化 </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonObj"></param>
        /// <param name="namingType">命名规则</param>
        /// <param name="indented">是否缩进</param>
        /// <returns></returns>
        string ToJson<T>(T jsonObj, NamingType namingType = NamingType.CamelCase, bool indented = false);

        /// <summary> Json反序列化 </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json">JSON字符串</param>
        /// <param name="namingType">命名规则</param>
        /// <returns></returns>
        T Json<T>(string json, NamingType namingType = NamingType.CamelCase);

        /// <summary> 匿名对象反序列化 </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json">JSON字符串</param>
        /// <param name="anonymousTypeObject">匿名对象</param>
        /// <param name="namingType">命名规则</param>
        /// <returns></returns>
        T Json<T>(string json, T anonymousTypeObject, NamingType namingType = NamingType.CamelCase);
    }
}
