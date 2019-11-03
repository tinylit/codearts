using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkyBuilding.Net
{
    /// <summary>
    /// 请求能力
    /// </summary>
    /// <typeparam name="T">结果数据</typeparam>
    public interface IRequestable<T>
    {
        /// <summary>
        /// GET 请求
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        /// <returns></returns>
        T Get(int timeout = 5000);

        /// <summary>
        /// GET 请求
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        /// <returns></returns>
        Task<T> GetAsync(int timeout = 5000);

        /// <summary>
        /// DELETE 请求
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        /// <returns></returns>
        T Delete(int timeout = 5000);

        /// <summary>
        /// DELETE 请求
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        /// <returns></returns>
        Task<T> DeleteAsync(int timeout = 5000);

        /// <summary>
        /// POST 请求
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        /// <returns></returns>
        T Post(int timeout = 5000);

        /// <summary>
        /// POST 请求
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        /// <returns></returns>
        Task<T> PostAsync(int timeout = 5000);

        /// <summary>
        /// POST 请求
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        /// <returns></returns>
        T Put(int timeout = 5000);

        /// <summary>
        /// POST 请求
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        /// <returns></returns>
        Task<T> PutAsync(int timeout = 5000);

        /// <summary>
        /// HEAD 请求
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        /// <returns></returns>
        T Head(int timeout = 5000);

        /// <summary>
        /// HEAD 请求
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        /// <returns></returns>
        Task<T> HeadAsync(int timeout = 5000);

        /// <summary>
        /// PATCH 请求
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        /// <returns></returns>
        T Patch(int timeout = 5000);

        /// <summary>
        /// PATCH 请求
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        /// <returns></returns>
        Task<T> PatchAsync(int timeout = 5000);

        /// <summary>
        /// 数据返回XML格式的结果，将转为指定类型
        /// </summary>
        /// <param name="method">求取方式</param>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        /// <returns></returns>
        T Request(string method, int timeout = 5000);
        /// <summary>
        /// 数据返回XML格式的结果，将转为指定类型
        /// </summary>
        /// <param name="method">求取方式</param>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        /// <returns></returns>
        Task<T> RequestAsync(string method, int timeout = 5000);
    }

    /// <summary>
    /// 请求
    /// </summary>
    public interface IRequestable : IRequestable<string>
    {
        /// <summary>
        /// 添加包含与请求或响应相关联的协议头。
        /// </summary>
        /// <param name="header">协议头</param>
        /// <param name="value">内容</param>
        /// <returns></returns>
        IRequestable Header(string header, string value);

        /// <summary>
        /// 添加包含与请求或响应相关联的协议头。
        /// </summary>
        /// <param name="headers">协议头</param>
        /// <returns></returns>
        IRequestable Headers(IEnumerable<KeyValuePair<string, string>> headers);

        /// <summary>
        /// content-type = "application/json"
        /// </summary>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable Json(string param);
        /// <summary>
        ///  content-type = "application/json"
        /// </summary>
        /// <param name="param">参数</param>
        /// <param name="namingType">命名规则</param>
        /// <returns></returns>
        IRequestable Json<T>(T param, NamingType namingType = NamingType.CamelCase) where T : class;
        /// <summary>
        /// content-type = "application/xml";
        /// </summary>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable Xml(string param);
        /// <summary>
        /// content-type = "application/xml";
        /// </summary>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable Xml<T>(T param) where T : class;
        /// <summary>
        /// content-type = "application/x-www-form-urlencoded";
        /// </summary>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable Form(string param);
        /// <summary>
        /// content-type = "application/x-www-form-urlencoded";
        /// </summary>
        /// <param name="param">参数</param>
        /// <param name="namingType">命名规则</param>
        /// <returns></returns>
        IRequestable Form<T>(T param, NamingType namingType = NamingType.CamelCase) where T : class;

        /// <summary>
        /// 请求参数。
        /// </summary>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable Query(string param);

        /// <summary>
        /// 请求参数。?id=1&amp;name="yep"
        /// </summary>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable Query(IEnumerable<string> param);

        /// <summary>
        /// 请求参数。?id=1&amp;name="yep"
        /// </summary>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable Query(IEnumerable<KeyValuePair<string, string>> param);

        /// <summary>
        /// 请求参数。?id=1&amp;name="yep"
        /// </summary>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable Query(IEnumerable<KeyValuePair<string, DateTime>> param);

        /// <summary>
        /// 请求参数。?id=1&amp;name="yep"
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable Query<T>(IEnumerable<KeyValuePair<string, T>> param);

        /// <summary>
        /// 请求参数。?id=1&amp;name="yep"
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="param">参数</param>
        /// <param name="namingType">命名规则</param>
        /// <returns></returns>
        IRequestable Query<T>(T param, NamingType namingType = NamingType.UrlCase) where T : class;

        /// <summary>
        /// 数据返回JSON格式的结果，将转为指定类型
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="namingType">命名规则</param>
        /// <returns></returns>
        IJsonRequestable<T> ByJson<T>(NamingType namingType = NamingType.CamelCase);
        /// <summary>
        /// 数据返回XML格式的结果，将转为指定类型
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <returns></returns>
        IXmlRequestable<T> ByXml<T>();

        /// <summary>
        /// 数据返回JSON格式的结果，将转为指定类型(匿名对象)
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="anonymousTypeObject">匿名对象</param>
        /// <param name="namingType">命名规范</param>
        /// <returns></returns>
        IJsonRequestable<T> ByJson<T>(T anonymousTypeObject, NamingType namingType = NamingType.CamelCase) where T : class;
        /// <summary>
        /// 数据返回XML格式的结果，将转为指定类型(匿名对象)
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="anonymousTypeObject">匿名对象</param>
        /// <returns></returns>
        IXmlRequestable<T> ByXml<T>(T anonymousTypeObject) where T : class;
    }

    /// <summary>
    /// 请求能力
    /// </summary>
    /// <typeparam name="T">结果数据</typeparam>
    public interface IJsonRequestable<T> : IRequestable<T>
    {
        /// <summary>
        /// 命名规则
        /// </summary>
        NamingType NamingType { get; }
    }

    /// <summary>
    /// 请求能力
    /// </summary>
    /// <typeparam name="T">结果数据</typeparam>
    public interface IXmlRequestable<T> : IRequestable<T>
    {

    }
}
