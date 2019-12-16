using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeArts.Net
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
        IRequestable AppendHeader(string header, string value);

        /// <summary>
        /// 添加包含与请求或响应相关联的协议头。
        /// </summary>
        /// <param name="headers">协议头</param>
        /// <returns></returns>
        IRequestable AppendHeaders(IEnumerable<KeyValuePair<string, string>> headers);

        /// <summary>
        /// content-type = "application/json"
        /// </summary>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable ToJson(string param);
        /// <summary>
        ///  content-type = "application/json"
        /// </summary>
        /// <param name="param">参数</param>
        /// <param name="namingType">命名规则</param>
        /// <returns></returns>
        IRequestable ToJson<T>(T param, NamingType namingType = NamingType.CamelCase) where T : class;
        /// <summary>
        /// content-type = "application/xml";
        /// </summary>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable ToXml(string param);
        /// <summary>
        /// content-type = "application/xml";
        /// </summary>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable ToXml<T>(T param) where T : class;
        /// <summary>
        /// content-type = "application/x-www-form-urlencoded";
        /// </summary>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable ToForm(string param);
        /// <summary>
        /// content-type = "application/x-www-form-urlencoded";
        /// </summary>
        /// <param name="param">参数</param>
        /// <param name="namingType">命名规则</param>
        /// <returns></returns>
        IRequestable ToForm(IEnumerable<KeyValuePair<string, string>> param, NamingType namingType = NamingType.Normal);
        /// <summary>
        /// content-type = "application/x-www-form-urlencoded";
        /// </summary>
        /// <param name="param">参数</param>
        /// <param name="namingType">命名规则</param>
        /// <returns></returns>
        IRequestable ToForm(IEnumerable<KeyValuePair<string, DateTime>> param, NamingType namingType = NamingType.Normal);
        /// <summary>
        /// content-type = "application/x-www-form-urlencoded";
        /// </summary>
        /// <param name="param">参数</param>
        /// <param name="namingType">命名规则</param>
        /// <returns></returns>
        IRequestable ToForm<T>(IEnumerable<KeyValuePair<string, T>> param, NamingType namingType = NamingType.Normal);
        /// <summary>
        /// content-type = "application/x-www-form-urlencoded";
        /// </summary>
        /// <param name="param">参数</param>
        /// <param name="namingType">命名规则</param>
        /// <returns></returns>
        IRequestable ToForm<T>(T param, NamingType namingType = NamingType.Normal) where T : class;

        /// <summary>
        /// 请求参数。
        /// </summary>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable ToQueryString(string param);

        /// <summary>
        /// 请求参数。?id=1&amp;name="yep"
        /// </summary>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable ToQueryString(IEnumerable<string> param);

        /// <summary>
        /// 请求参数。?id=1&amp;name="yep"
        /// </summary>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable ToQueryString(IEnumerable<KeyValuePair<string, string>> param);

        /// <summary>
        /// 请求参数。?id=1&amp;name="yep"
        /// </summary>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable ToQueryString(IEnumerable<KeyValuePair<string, DateTime>> param);

        /// <summary>
        /// 请求参数。?id=1&amp;name="yep"
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable ToQueryString<T>(IEnumerable<KeyValuePair<string, T>> param);

        /// <summary>
        /// 请求参数。?id=1&amp;name="yep"
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="param">参数</param>
        /// <param name="namingType">命名规则</param>
        /// <returns></returns>
        IRequestable ToQueryString<T>(T param, NamingType namingType = NamingType.UrlCase) where T : class;

        /// <summary>
        /// 数据返回JSON格式的结果，将转为指定类型
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="namingType">命名规则</param>
        /// <returns></returns>
        IJsonRequestable<T> Json<T>(NamingType namingType = NamingType.CamelCase) where T : class;
        /// <summary>
        /// 数据返回XML格式的结果，将转为指定类型
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <returns></returns>
        IXmlRequestable<T> Xml<T>() where T : class;

        /// <summary>
        /// 数据返回JSON格式的结果，将转为指定类型(匿名对象)
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="anonymousTypeObject">匿名对象</param>
        /// <param name="namingType">命名规范</param>
        /// <returns></returns>
        IJsonRequestable<T> Json<T>(T anonymousTypeObject, NamingType namingType = NamingType.CamelCase) where T : class;
        /// <summary>
        /// 数据返回XML格式的结果，将转为指定类型(匿名对象)
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="anonymousTypeObject">匿名对象</param>
        /// <returns></returns>
        IXmlRequestable<T> Xml<T>(T anonymousTypeObject) where T : class;
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
    public interface IXmlRequestable<T> : IRequestable<T> { }
}
