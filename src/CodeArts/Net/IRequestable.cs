using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Xml;

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
        /// DELETE 请求
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        /// <returns></returns>
        T Delete(int timeout = 5000);

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
        T Put(int timeout = 5000);

        /// <summary>
        /// HEAD 请求
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        /// <returns></returns>
        T Head(int timeout = 5000);

        /// <summary>
        /// PATCH 请求
        /// </summary>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        /// <returns></returns>
        T Patch(int timeout = 5000);

        /// <summary>
        /// 数据返回XML格式的结果，将转为指定类型
        /// </summary>
        /// <param name="method">求取方式</param>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        /// <returns></returns>
        T Request(string method, int timeout = 5000);

#if !NET40
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
        Task<T> DeleteAsync(int timeout = 5000);

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
        Task<T> PutAsync(int timeout = 5000);

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
        Task<T> PatchAsync(int timeout = 5000);

        /// <summary>
        /// 数据返回XML格式的结果，将转为指定类型
        /// </summary>
        /// <param name="method">求取方式</param>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        /// <returns></returns>
        Task<T> RequestAsync(string method, int timeout = 5000);
#endif

    }

    /// <summary>
    /// 转换能力
    /// </summary>
    public interface ICastRequestable
    {
        /// <summary>
        /// 数据返回JSON格式的结果，将转为指定类型
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="namingType">命名规则</param>
        /// <returns></returns>
        IJsonRequestable<T> Json<T>(NamingType namingType = NamingType.CamelCase) where T : class;
        /// <summary>
        /// 数据返回XML格式的结果，将转为指定类型
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <returns></returns>
        IXmlRequestable<T> Xml<T>() where T : class;

        /// <summary>
        /// 数据返回JSON格式的结果，将转为指定类型(匿名对象)
        /// </summary>
        /// <typeparam name="T">匿名类型</typeparam>
        /// <param name="anonymousTypeObject">匿名对象</param>
        /// <param name="namingType">命名规范</param>
        /// <returns></returns>
        IJsonRequestable<T> Json<T>(T anonymousTypeObject, NamingType namingType = NamingType.CamelCase) where T : class;
        /// <summary>
        /// 数据返回XML格式的结果，将转为指定类型(匿名对象)
        /// </summary>
        /// <typeparam name="T">匿名类型</typeparam>
        /// <param name="anonymousTypeObject">匿名对象</param>
        /// <returns></returns>
        IXmlRequestable<T> Xml<T>(T anonymousTypeObject) where T : class;
    }

    /// <summary>
    /// 文件下载地址
    /// </summary>
    public interface IFileRequestable
    {
        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="fileName">本地文件地址：指定文件将被上传到服务地址。</param>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        byte[] UploadFile(string fileName, int timeout = 5000);

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="method">求取方式</param>
        /// <param name="fileName">本地文件地址：指定文件将被上传到服务地址。</param>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        byte[] UploadFile(string method, string fileName, int timeout = 5000);

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="fileName">本地文件地址：文件将下载到这个路径下。</param>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        void DownloadFile(string fileName, int timeout = 5000);

#if !NET40
        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="fileName">本地文件地址：指定文件将被上传到服务地址。</param>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        Task<byte[]> UploadFileAsync(string fileName, int timeout = 5000);

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="method">求取方式</param>
        /// <param name="fileName">本地文件地址：指定文件将被上传到服务地址。</param>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        Task<byte[]> UploadFileAsync(string method, string fileName, int timeout = 5000);

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="fileName">本地文件地址：文件将下载到这个路径下。</param>
        /// <param name="timeout">超时时间，单位：毫秒</param>
        Task DownloadFileAsync(string fileName, int timeout = 5000);
#endif
    }

    /// <summary>
    /// 请求基础
    /// </summary>
    public interface IRequestableBase
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
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable ToQueryString<T>(IEnumerable<KeyValuePair<string, T>> param);

        /// <summary>
        /// 请求参数。?id=1&amp;name="yep"
        /// </summary>
        /// <param name="param">参数</param>
        /// <param name="namingType">命名规则</param>
        /// <returns></returns>
        IRequestable ToQueryString(object param, NamingType namingType = NamingType.UrlCase);
    }

    /// <summary>
    /// 请求
    /// </summary>
    public interface IRequestable : IRequestable<string>, IRequestableBase, IFileRequestable, ICastRequestable
    {
        /// <summary>
        /// content-type = "application/json"
        /// </summary>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable ToJson(string param);
        /// <summary>
        ///  content-type = "application/json"
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
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
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="param">参数</param>
        /// <returns></returns>
        IRequestable ToXml<T>(T param) where T : class;
        /// <summary>
        /// content-type = "application/x-www-form-urlencoded";
        /// </summary>
        /// <param name="param">参数</param>
        /// <param name="namingType">命名规则</param>
        /// <returns></returns>
        IRequestable ToForm(string param, NamingType namingType = NamingType.Normal);
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
        /// <typeparam name="T">值类型</typeparam>
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
        IRequestable ToForm(object param, NamingType namingType = NamingType.Normal);

        /// <summary>
        /// 如果请求异常，会调用【<paramref name="match"/>】，判断是否重试请求。
        /// 多个条件是或的关系。
        /// </summary>
        /// <param name="match">判断是否重试请求</param>
        /// <returns></returns>
        IThenRequestable TryWhen(Predicate<WebException> match);

        /// <summary>
        /// 如果请求异常，会调用【<paramref name="then"/>】，并重试一次请求。
        /// </summary>
        /// <param name="then">异常处理事件</param>
        /// <returns></returns>
        IThenConditionRequestable TryThen(Action<IRequestableBase, WebException> then);

        /// <summary>
        /// 捕获Web异常
        /// </summary>
        /// <param name="catchError">异常捕获</param>
        /// <returns></returns>
        ICatchRequestable Catch(Action<WebException> catchError);

        /// <summary>
        /// 捕获Web异常，并返回结果（返回最后一次的结果）。
        /// </summary>
        /// <param name="catchError">异常捕获,并返回异常情况下的结果</param>
        /// <returns></returns>
        ICatchRequestable Catch(Func<WebException, string> catchError);

        /// <summary>
        /// 始终执行的动作
        /// </summary>
        /// <param name="always">请求始终会执行的方法</param>
        /// <returns></returns>
        IFinallyRequestable Finally(Action always);
    }

    /// <summary>
    /// 异常处理的请求
    /// </summary>
    public interface ICatchRequestable : IRequestable<string>, ICastRequestable
    {
        /// <summary>
        /// 始终执行的动作
        /// </summary>
        /// <param name="always">请求始终会执行的方法</param>
        /// <returns></returns>
        IFinallyRequestable Finally(Action always);
    }

    /// <summary>
    /// 结束
    /// </summary>
    public interface IFinallyRequestable : IRequestable<string>, ICastRequestable
    {

    }

    /// <summary>
    /// 结束
    /// </summary>
    public interface IFinallyRequestable<T> : IRequestable<T>
    {

    }

    /// <summary>
    /// 异常处理能力
    /// </summary>
    public interface ICatchRequestable<T> : IRequestable<T>
    {
        /// <summary>
        /// 始终执行的动作
        /// </summary>
        /// <param name="always">请求始终会执行的方法</param>
        /// <returns></returns>
        IFinallyRequestable<T> Finally(Action always);
    }

    /// <summary>
    /// 延续能力的请求
    /// </summary>
    public interface IThenRequestable : ICatchRequestable, IFileRequestable
    {
        /// <summary>
        /// 如果请求异常，会调用【<paramref name="match"/>】，判断是否重试请求。
        /// 多个条件之间是或的关系。
        /// </summary>
        /// <param name="match">判断是否重试请求</param>
        /// <returns></returns>
        IThenRequestable Or(Predicate<WebException> match);

        /// <summary>
        /// 设置重试次数：默认：设置<see cref="Or(Predicate{WebException})"/>或<seealso cref="IRequestable.TryWhen(Predicate{WebException})"/>或<seealso cref="IThenConditionRequestable.TryWhen(Predicate{WebException})"/>的总次数。
        /// 多次设置时，取最大值。
        /// </summary>
        /// <param name="retryCount">最大重试次数。</param>
        /// <returns></returns>
        IThenRequestable MaxRetries(int retryCount);

        /// <summary>
        /// 捕获Web异常
        /// </summary>
        /// <param name="catchError">异常捕获</param>
        /// <returns></returns>
        ICatchRequestable Catch(Action<WebException> catchError);

        /// <summary>
        /// 捕获Web异常，并返回结果（返回最后一次的结果）。
        /// </summary>
        /// <param name="catchError">异常捕获,并返回异常情况下的结果</param>
        /// <returns></returns>
        ICatchRequestable Catch(Func<WebException, string> catchError);
    }

    /// <summary>
    /// 带有条件的延续能力请求
    /// </summary>
    public interface IThenConditionRequestable : ICatchRequestable, IFileRequestable
    {
        /// <summary>
        /// 所有条件都满足时，重试一次请求。
        /// 多个条件之间是且的关系。
        /// </summary>
        /// <param name="predicate">条件</param>
        /// <returns></returns>
        IThenAndConditionRequestable If(Predicate<WebException> predicate);

        /// <summary>
        /// 如果请求异常，会调用【<paramref name="match"/>】，判断是否重试请求。
        /// 多个条件是或的关系。
        /// </summary>
        /// <param name="match">判断是否重试请求</param>
        /// <returns></returns>
        IThenRequestable TryWhen(Predicate<WebException> match);

        /// <summary>
        /// 新开一个重试机制，如果请求异常，会调用【<paramref name="then"/>】，并重试一次请求。
        /// </summary>
        /// <param name="then">异常处理事件</param>
        /// <returns></returns>
        IThenConditionRequestable Then(Action<IRequestableBase, WebException> then);

        /// <summary>
        /// 捕获Web异常
        /// </summary>
        /// <param name="catchError">异常捕获</param>
        /// <returns></returns>
        ICatchRequestable Catch(Action<WebException> catchError);

        /// <summary>
        /// 捕获Web异常，并返回结果（返回最后一次的结果）。
        /// </summary>
        /// <param name="catchError">异常捕获,并返回异常情况下的结果</param>
        /// <returns></returns>
        ICatchRequestable Catch(Func<WebException, string> catchError);
    }

    /// <summary>
    /// 带有条件的延续能力请求
    /// </summary>
    public interface IThenAndConditionRequestable : ICatchRequestable, IFileRequestable
    {
        /// <summary>
        /// 所有条件都满足时，重试一次请求。
        /// 多个条件之间是且的关系。
        /// </summary>
        /// <param name="predicate">条件</param>
        /// <returns></returns>
        IThenAndConditionRequestable And(Predicate<WebException> predicate);

        /// <summary>
        /// 如果请求异常，会调用【<paramref name="match"/>】，判断是否重试请求。
        /// 多个条件是或的关系。
        /// </summary>
        /// <param name="match">判断是否重试请求</param>
        /// <returns></returns>
        IThenRequestable TryWhen(Predicate<WebException> match);

        /// <summary>
        /// 新开一个重试机制，如果请求异常，会调用【<paramref name="then"/>】，并重试一次请求。
        /// </summary>
        /// <param name="then">异常处理事件</param>
        /// <returns></returns>
        IThenConditionRequestable Then(Action<IRequestableBase, WebException> then);

        /// <summary>
        /// 捕获Web异常
        /// </summary>
        /// <param name="catchError">异常捕获</param>
        /// <returns></returns>
        ICatchRequestable Catch(Action<WebException> catchError);

        /// <summary>
        /// 捕获Web异常，并返回结果（返回最后一次的结果）。
        /// </summary>
        /// <param name="catchError">异常捕获,并返回异常情况下的结果</param>
        /// <returns></returns>
        ICatchRequestable Catch(Func<WebException, string> catchError);
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

        /// <summary>
        /// 捕获Web异常
        /// </summary>
        /// <param name="catchError">异常捕获</param>
        /// <returns></returns>
        ICatchRequestable<T> Catch(Action<string, Exception> catchError);

        /// <summary>
        /// 捕获Web异常，并返回结果（返回最后一次的结果）。
        /// </summary>
        /// <param name="catchError">异常捕获,并返回异常情况下的结果</param>
        /// <returns></returns>
        ICatchRequestable<T> Catch(Func<string, Exception, T> catchError);

        /// <summary>
        /// 捕获Web异常，并返回结果（返回最后一次的结果）。
        /// </summary>
        /// <param name="catchError">异常捕获,并返回异常情况下的结果</param>
        /// <returns></returns>
        ICatchRequestable<T> Catch(Func<WebException, T> catchError);

        /// <summary>
        /// 始终执行的动作
        /// </summary>
        /// <param name="always">请求始终会执行的方法</param>
        /// <returns></returns>
        IFinallyRequestable<T> Finally(Action always);
    }

    /// <summary>
    /// 请求能力
    /// </summary>
    /// <typeparam name="T">结果数据</typeparam>
    public interface IXmlRequestable<T> : IRequestable<T>
    {
        /// <summary>
        /// 捕获Web异常
        /// </summary>
        /// <param name="catchError">异常捕获</param>
        /// <returns></returns>
        ICatchRequestable<T> Catch(Action<string, XmlException> catchError);

        /// <summary>
        /// 捕获Web异常，并返回结果（返回最后一次的结果）。
        /// </summary>
        /// <param name="catchError">异常捕获,并返回异常情况下的结果</param>
        /// <returns></returns>
        ICatchRequestable<T> Catch(Func<string, XmlException, T> catchError);

        /// <summary>
        /// 捕获Web异常，并返回结果（返回最后一次的结果）。
        /// </summary>
        /// <param name="catchError">异常捕获,并返回异常情况下的结果</param>
        /// <returns></returns>
        ICatchRequestable<T> Catch(Func<WebException, T> catchError);

        /// <summary>
        /// 始终执行的动作
        /// </summary>
        /// <param name="always">请求始终会执行的方法</param>
        /// <returns></returns>
        IFinallyRequestable<T> Finally(Action always);
    }
}
