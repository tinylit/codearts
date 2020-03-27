using CodeArts;
using CodeArts.Net;
using CodeArts.Serialize.Json;
using CodeArts.Serialize.Xml;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace System
{
    /// <summary>
    /// 地址拓展
    /// </summary>
    public static class UriExtentions
    {
        private static readonly Regex UriPattern = new Regex(@"\w+://.+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private abstract class Requestable<T> : IRequestable<T>
        {
            public T Get(int timeout = 5000) => Request("GET", timeout);
            public T Delete(int timeout = 5000) => Request("DELETE", timeout);
            public T Post(int timeout = 5000) => Request("POST", timeout);
            public T Put(int timeout = 5000) => Request("PUT", timeout);
            public T Head(int timeout = 5000) => Request("HEAD", timeout);
            public T Patch(int timeout = 5000) => Request("PATCH", timeout);
            public abstract T Request(string method, int timeout = 5000);

#if !NET40
            public Task<T> GetAsync(int timeout = 5000) => RequestAsync("GET", timeout);


            public Task<T> DeleteAsync(int timeout = 5000) => RequestAsync("DELETE", timeout);


            public Task<T> PostAsync(int timeout = 5000) => RequestAsync("POST", timeout);


            public Task<T> PutAsync(int timeout = 5000) => RequestAsync("PUT", timeout);


            public Task<T> HeadAsync(int timeout = 5000) => RequestAsync("HEAD", timeout);


            public Task<T> PatchAsync(int timeout = 5000) => RequestAsync("PATCH", timeout);

            public abstract Task<T> RequestAsync(string method, int timeout = 5000);
#endif
        }

        #region 补充
        private class CatchRequestable : Requestable<string>, ICatchRequestable
        {
            private readonly IRequestable<string> requestable;
            private readonly Action<WebException> action;

            public CatchRequestable(IRequestable<string> requestable, Action<WebException> action)
            {
                this.requestable = requestable;
                this.action = action ?? throw new ArgumentNullException(nameof(action));
            }

            public IFinallyRequestable Finally(Action always) => new FinallyRequestable(this, always);

            public IJsonRequestable<T> Json<T>(NamingType namingType = NamingType.CamelCase) where T : class => new JsonRequestable<T>(this, namingType);

            public IJsonRequestable<T> Json<T>(T anonymousTypeObject, NamingType namingType = NamingType.CamelCase) where T : class => new JsonRequestable<T>(this, namingType);

            public override string Request(string method, int timeout = 5000)
            {
                try
                {
                    return requestable.Request(method, timeout);
                }
                catch (WebException e)
                {
                    action.Invoke(e);

                    throw;
                }
            }
#if !NET40
            public override Task<string> RequestAsync(string method, int timeout = 5000)
            {
                try
                {
                    return requestable.RequestAsync(method, timeout);
                }
                catch (WebException e)
                {
                    action.Invoke(e);

                    throw;
                }
            }
#endif

            public IXmlRequestable<T> Xml<T>() where T : class => new XmlRequestable<T>(this);

            public IXmlRequestable<T> Xml<T>(T _) where T : class => new XmlRequestable<T>(this);
        }

        private class ResultCatchRequestable : Requestable<string>, ICatchRequestable
        {
            private readonly IRequestable<string> requestable;
            private readonly Func<WebException, string> action;

            public ResultCatchRequestable(IRequestable<string> requestable, Func<WebException, string> action)
            {
                this.requestable = requestable;
                this.action = action ?? throw new ArgumentNullException(nameof(action));
            }

            public IFinallyRequestable Finally(Action always) => new FinallyRequestable(this, always);

            public IJsonRequestable<T> Json<T>(NamingType namingType = NamingType.CamelCase) where T : class => new JsonRequestable<T>(this, namingType);

            public IJsonRequestable<T> Json<T>(T anonymousTypeObject, NamingType namingType = NamingType.CamelCase) where T : class => new JsonRequestable<T>(this, namingType);

            public override string Request(string method, int timeout = 5000)
            {
                try
                {
                    return requestable.Request(method, timeout);
                }
                catch (WebException e)
                {
                    return action.Invoke(e);
                }
            }
#if !NET40
            public override async Task<string> RequestAsync(string method, int timeout = 5000)
            {
                try
                {
                    return await requestable.RequestAsync(method, timeout);
                }
                catch (WebException e)
                {
                    return action.Invoke(e);
                }
            }
#endif

            public IXmlRequestable<T> Xml<T>() where T : class => new XmlRequestable<T>(this);

            public IXmlRequestable<T> Xml<T>(T anonymousTypeObject) where T : class => new XmlRequestable<T>(this);
        }

        private class FinallyRequestable : Requestable<string>, IFinallyRequestable
        {
            private readonly Action action;
            private readonly IRequestable<string> requestable;

            public FinallyRequestable(IRequestable<string> requestable, Action action)
            {
                this.requestable = requestable;
                this.action = action ?? throw new ArgumentNullException(nameof(action));
            }

            public IJsonRequestable<T> Json<T>(NamingType namingType = NamingType.CamelCase) where T : class => new JsonRequestable<T>(this, namingType);

            public IJsonRequestable<T> Json<T>(T anonymousTypeObject, NamingType namingType = NamingType.CamelCase) where T : class => new JsonRequestable<T>(this, namingType);

            public override string Request(string method, int timeout = 5000)
            {
                try
                {
                    return requestable.Request(method, timeout);
                }
                finally
                {
                    action.Invoke();
                }
            }
#if !NET40
            public override Task<string> RequestAsync(string method, int timeout = 5000)
            {
                try
                {
                    return requestable.RequestAsync(method, timeout);
                }
                finally
                {
                    action.Invoke();
                }
            }
#endif

            public IXmlRequestable<T> Xml<T>() where T : class => new XmlRequestable<T>(this);

            public IXmlRequestable<T> Xml<T>(T anonymousTypeObject) where T : class => new XmlRequestable<T>(this);
        }

        private class IIFThenRequestable : Requestable<string>, IThenRequestable
        {
            private readonly IRequestableBase requestable;
            private readonly IRequestable<string> request;
            private readonly IFileRequestable fileRequestable;
            private readonly Predicate<WebException> predicate;

            public IIFThenRequestable(IRequestable<string> request, IFileRequestable fileRequestable, IRequestableBase requestable, Predicate<WebException> predicate)
            {
                this.request = request;
                this.requestable = requestable;
                this.fileRequestable = fileRequestable;
                this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            }

            public IIFThenRequestable(IRequestable requestable, Predicate<WebException> predicate) : this(requestable, requestable, requestable, predicate)
            {
            }

            public IFinallyRequestable Finally(Action always) => new FinallyRequestable(this, always);

            public IThenRequestable Then(Predicate<WebException> match) => new IIFThenRequestable(this, this, requestable, match);

            public IThenRequestable Then(Action<IRequestableBase, WebException> then) => new ThenRequestable(this, this, requestable, then);

            public override string Request(string method, int timeout = 5000)
            {
                try
                {
                    return request.Request(method, timeout);
                }
                catch (WebException e)
                {
                    if (predicate.Invoke(e))
                    {
                        return request.Request(method, timeout);
                    }

                    throw;
                }
            }

            public ICatchRequestable Catch(Action<WebException> catchError) => new CatchRequestable(this, catchError);

            public ICatchRequestable Catch(Func<WebException, string> catchError) => new ResultCatchRequestable(this, catchError);

            public IJsonRequestable<T> Json<T>(NamingType namingType = NamingType.CamelCase) where T : class => new JsonRequestable<T>(this, namingType);

            public IXmlRequestable<T> Xml<T>() where T : class => new XmlRequestable<T>(this);

            public IJsonRequestable<T> Json<T>(T _, NamingType namingType = NamingType.CamelCase) where T : class => Json<T>(namingType);

            public IXmlRequestable<T> Xml<T>(T _) where T : class => Xml<T>();

            public byte[] UploadFile(string fileName, int timeout = 5000) => UploadFile(null, fileName, timeout);

            public byte[] UploadFile(string method, string fileName, int timeout = 5000)
            {
                try
                {
                    return fileRequestable.UploadFile(method, fileName, timeout);
                }
                catch (WebException e)
                {
                    if (predicate.Invoke(e))
                    {
                        return fileRequestable.UploadFile(method, fileName, timeout);
                    }

                    throw;
                }
            }

            public void DownloadFile(string fileName, int timeout = 5000)
            {
                try
                {
                    fileRequestable.DownloadFile(fileName, timeout);
                }
                catch (WebException e)
                {
                    if (predicate.Invoke(e))
                    {
                        fileRequestable.DownloadFile(fileName, timeout);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

#if !NET40
            public override async Task<string> RequestAsync(string method, int timeout = 5000)
            {
                try
                {
                    return await request.RequestAsync(method, timeout);
                }
                catch (WebException e)
                {
                    if (predicate.Invoke(e))
                    {
                        return await request.RequestAsync(method, timeout);
                    }

                    throw;
                }
            }

            public Task<byte[]> UploadFileAsync(string fileName, int timeout = 5000) => UploadFileAsync(null, fileName, timeout);

            public async Task<byte[]> UploadFileAsync(string method, string fileName, int timeout = 5000)
            {
                try
                {
                    return await fileRequestable.UploadFileAsync(method, fileName, timeout);
                }
                catch (WebException e)
                {
                    if (predicate.Invoke(e))
                    {
                        return await fileRequestable.UploadFileAsync(method, fileName, timeout);
                    }

                    throw;
                }
            }

            public async Task DownloadFileAsync(string fileName, int timeout = 5000)
            {
                try
                {
                    await fileRequestable.DownloadFileAsync(fileName, timeout);
                }
                catch (WebException e)
                {
                    if (predicate.Invoke(e))
                    {
                        await fileRequestable.DownloadFileAsync(fileName, timeout);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
#endif
        }
        private class ThenRequestable : Requestable<string>, IThenRequestable
        {
            private readonly IRequestableBase requestable;
            private readonly IRequestable<string> request;
            private readonly IFileRequestable fileRequestable;
            private readonly Action<IRequestableBase, WebException> then;

            public ThenRequestable(IRequestable<string> request, IFileRequestable fileRequestable, IRequestableBase requestable, Action<IRequestableBase, WebException> then)
            {
                this.request = request;
                this.requestable = requestable;
                this.fileRequestable = fileRequestable;
                this.then = then ?? throw new ArgumentNullException(nameof(then));
            }

            public ThenRequestable(IRequestable requestable, Action<IRequestableBase, WebException> then) : this(requestable, requestable, requestable, then)
            {
            }

            public IFinallyRequestable Finally(Action always) => new FinallyRequestable(this, always);

            public IThenRequestable Then(Predicate<WebException> match) => new IIFThenRequestable(this, this, requestable, match);

            public IThenRequestable Then(Action<IRequestableBase, WebException> then) => new ThenRequestable(this, this, requestable, then);

            public override string Request(string method, int timeout = 5000)
            {
                try
                {
                    return request.Request(method, timeout);
                }
                catch (WebException e)
                {
                    then.Invoke(requestable, e);

                    return request.Request(method, timeout);
                }
            }

            public ICatchRequestable Catch(Action<WebException> catchError) => new CatchRequestable(this, catchError);

            public ICatchRequestable Catch(Func<WebException, string> catchError) => new ResultCatchRequestable(this, catchError);

            public IJsonRequestable<T> Json<T>(NamingType namingType = NamingType.CamelCase) where T : class => new JsonRequestable<T>(this, namingType);

            public IXmlRequestable<T> Xml<T>() where T : class => new XmlRequestable<T>(this);

            public IJsonRequestable<T> Json<T>(T _, NamingType namingType = NamingType.CamelCase) where T : class => Json<T>(namingType);

            public IXmlRequestable<T> Xml<T>(T _) where T : class => Xml<T>();

            public byte[] UploadFile(string fileName, int timeout = 5000) => UploadFile(null, fileName, timeout);

            public byte[] UploadFile(string method, string fileName, int timeout = 5000)
            {
                try
                {
                    return fileRequestable.UploadFile(method, fileName, timeout);
                }
                catch (WebException e)
                {
                    then.Invoke(requestable, e);

                    return fileRequestable.UploadFile(method, fileName, timeout);
                }
            }

            public void DownloadFile(string fileName, int timeout = 5000)
            {
                try
                {
                    fileRequestable.DownloadFile(fileName, timeout);
                }
                catch (WebException e)
                {
                    then.Invoke(requestable, e);

                    fileRequestable.DownloadFile(fileName, timeout);
                }
            }

#if !NET40
            public override async Task<string> RequestAsync(string method, int timeout = 5000)
            {
                try
                {
                    return await request.RequestAsync(method, timeout);
                }
                catch (WebException e)
                {
                    then.Invoke(requestable, e);

                    return await request.RequestAsync(method, timeout);
                }
            }

            public Task<byte[]> UploadFileAsync(string fileName, int timeout = 5000) => UploadFileAsync(null, fileName, timeout);

            public async Task<byte[]> UploadFileAsync(string method, string fileName, int timeout = 5000)
            {
                try
                {
                    return await fileRequestable.UploadFileAsync(method, fileName, timeout);
                }
                catch (WebException e)
                {
                    then.Invoke(requestable, e);

                    return await fileRequestable.UploadFileAsync(method, fileName, timeout);
                }
            }

            public async Task DownloadFileAsync(string fileName, int timeout = 5000)
            {
                try
                {
                    await fileRequestable.DownloadFileAsync(fileName, timeout);
                }
                catch (WebException e)
                {
                    then.Invoke(requestable, e);

                    await fileRequestable.DownloadFileAsync(fileName, timeout);
                }
            }
#endif
        }

        private class JsonCatchRequestable<T> : Requestable<T>, ICatchRequestable<T>
        {
            private readonly NamingType namingType;
            private readonly IRequestable<string> requestable;
            private readonly Action<string, Exception> action;

            public JsonCatchRequestable(IRequestable<string> requestable, Action<string, Exception> action, NamingType namingType)
            {
                this.requestable = requestable;
                this.action = action ?? throw new ArgumentNullException(nameof(action));
                this.namingType = namingType;
            }

            public IFinallyRequestable<T> Finally(Action always) => new FinallyRequestable<T>(this, always);

            public override T Request(string method, int timeout = 5000)
            {
                string value = requestable.Request(method, timeout);

                try
                {
                    return JsonHelper.Json<T>(value, namingType);
                }
                catch (Exception e) when (IsJsonError(e))
                {
                    action.Invoke(value, e);

                    throw;
                }
            }

            private static bool IsJsonError(Exception e)
            {
                for (Type type = e.GetType(), destinationType = typeof(Exception); type != destinationType; type = type.BaseType ?? destinationType)
                {
                    if (type.Name == "JsonException")
                    {
                        return true;
                    }
                }

                return false;
            }

#if !NET40
            public override async Task<T> RequestAsync(string method, int timeout = 5000)
            {
                string value = await requestable.RequestAsync(method, timeout);

                try
                {
                    return JsonHelper.Json<T>(value, namingType);
                }
                catch (Exception e) when (IsJsonError(e))
                {
                    action.Invoke(value, e);

                    throw;
                }
            }
#endif

        }

        private class JsonResultCatchRequestable<T> : Requestable<T>, ICatchRequestable<T>
        {
            private readonly IRequestable<string> requestable;
            private readonly Func<string, Exception, T> action;
            private readonly NamingType namingType;

            public JsonResultCatchRequestable(IRequestable<string> requestable, Func<string, Exception, T> action, NamingType namingType)
            {
                this.requestable = requestable;
                this.action = action ?? throw new ArgumentNullException(nameof(action));
                this.namingType = namingType;
            }

            public IFinallyRequestable<T> Finally(Action always) => new FinallyRequestable<T>(this, always);

            public override T Request(string method, int timeout = 5000)
            {
                string value = requestable.Request(method, timeout);

                try
                {
                    return JsonHelper.Json<T>(value, namingType);
                }
                catch (Exception e) when (IsJsonError(e))
                {
                    return action.Invoke(value, e);
                }
            }

            private static bool IsJsonError(Exception e)
            {
                for (Type type = e.GetType(), destinationType = typeof(Exception); type != destinationType; type = type.BaseType ?? destinationType)
                {
                    if (type.Name == "JsonException")
                    {
                        return true;
                    }
                }

                return false;
            }

#if !NET40
            public override async Task<T> RequestAsync(string method, int timeout = 5000)
            {
                string value = await requestable.RequestAsync(method, timeout);

                try
                {
                    return JsonHelper.Json<T>(value, namingType);
                }
                catch (Exception e) when (IsJsonError(e))
                {
                    return action.Invoke(value, e);
                }
            }
#endif

        }

        private class XmlCatchRequestable<T> : Requestable<T>, ICatchRequestable<T>
        {
            private readonly IRequestable<string> requestable;
            private readonly Action<string, XmlException> action;

            public XmlCatchRequestable(IRequestable<string> requestable, Action<string, XmlException> action)
            {
                this.requestable = requestable;
                this.action = action ?? throw new ArgumentNullException(nameof(action));
            }

            public IFinallyRequestable<T> Finally(Action always) => new FinallyRequestable<T>(this, always);

            public override T Request(string method, int timeout = 5000)
            {
                string value = requestable.Request(method, timeout);

                try
                {
                    return JsonHelper.Json<T>(value);
                }
                catch (XmlException e)
                {
                    action.Invoke(value, e);

                    throw;
                }
            }

#if !NET40
            public override async Task<T> RequestAsync(string method, int timeout = 5000)
            {
                string value = await requestable.RequestAsync(method, timeout);

                try
                {
                    return JsonHelper.Json<T>(value);
                }
                catch (XmlException e)
                {
                    action.Invoke(value, e);

                    throw;
                }
            }
#endif

        }

        private class XmlResultCatchRequestable<T> : Requestable<T>, ICatchRequestable<T>
        {
            private readonly IRequestable<string> requestable;
            private readonly Func<string, XmlException, T> action;

            public XmlResultCatchRequestable(IRequestable<string> requestable, Func<string, XmlException, T> action)
            {
                this.requestable = requestable;
                this.action = action ?? throw new ArgumentNullException(nameof(action));
            }

            public IFinallyRequestable<T> Finally(Action always) => new FinallyRequestable<T>(this, always);

            public override T Request(string method, int timeout = 5000)
            {
                string value = requestable.Request(method, timeout);

                try
                {
                    return JsonHelper.Json<T>(value);
                }
                catch (XmlException e)
                {
                    return action.Invoke(value, e);
                }
            }

#if !NET40
            public override async Task<T> RequestAsync(string method, int timeout = 5000)
            {
                string value = await requestable.RequestAsync(method, timeout);

                try
                {
                    return JsonHelper.Json<T>(value);
                }
                catch (XmlException e)
                {
                    return action.Invoke(value, e);
                }
            }
#endif
        }

        private class ResultCatchRequestable<T> : Requestable<T>, ICatchRequestable<T>
        {
            private readonly IRequestable<T> requestable;
            private readonly Func<WebException, T> action;

            public ResultCatchRequestable(IRequestable<T> requestable, Func<WebException, T> action)
            {
                this.requestable = requestable;
                this.action = action ?? throw new ArgumentNullException(nameof(action));
            }

            public IFinallyRequestable<T> Finally(Action always) => new FinallyRequestable<T>(this, always);

            public override T Request(string method, int timeout = 5000)
            {
                try
                {
                    return requestable.Request(method, timeout);
                }
                catch (WebException e)
                {
                    return action.Invoke(e);
                }
            }
#if !NET40
            public override async Task<T> RequestAsync(string method, int timeout = 5000)
            {
                try
                {
                    return await requestable.RequestAsync(method, timeout);
                }
                catch (WebException e)
                {
                    return action.Invoke(e);
                }
            }
#endif
        }

        private class FinallyRequestable<T> : Requestable<T>, IFinallyRequestable<T>
        {
            private readonly IRequestable<T> requestable;
            private readonly Action action;

            public FinallyRequestable(IRequestable<T> requestable, Action action)
            {
                this.requestable = requestable;
                this.action = action ?? throw new ArgumentNullException(nameof(action));
            }

            public override T Request(string method, int timeout = 5000)
            {
                try
                {
                    return requestable.Request(method, timeout);
                }
                finally
                {
                    action.Invoke();
                }
            }
#if !NET40
            public override Task<T> RequestAsync(string method, int timeout = 5000)
            {
                try
                {
                    return requestable.RequestAsync(method, timeout);
                }
                finally
                {
                    action.Invoke();
                }
            }
#endif

        }
        #endregion

        private class Requestable : Requestable<string>, IRequestable
        {
            private Uri __uri;
            private string __data;
            private NameValueCollection __form;
            private readonly Dictionary<string, string> __headers;

            public Requestable(string uriString) : this(new Uri(uriString)) { }

            public Requestable(Uri uri)
            {
                __uri = uri ?? throw new ArgumentNullException(nameof(uri));
                __headers = new Dictionary<string, string>();
            }

            public IRequestable AppendHeader(string header, string value)
            {
                __headers.Add(header, value);

                return this;
            }

            public IRequestable AppendHeaders(IEnumerable<KeyValuePair<string, string>> headers)
            {
                foreach (var kv in headers)
                {
                    __headers.Add(kv.Key, kv.Value);
                }

                return this;
            }

            public IRequestable ToForm(string param, NamingType namingType = NamingType.Normal)
                => ToForm(JsonHelper.Json<Dictionary<string, string>>(param, namingType));

            public IRequestable ToForm(IEnumerable<KeyValuePair<string, string>> param, NamingType namingType = NamingType.Normal)
            {
                __form = __form ?? new NameValueCollection();

                foreach (var kv in param)
                {
                    __form.Add(kv.Key.ToNamingCase(namingType), kv.Value);
                }

                return AppendHeader("Content-Type", "application/x-www-form-urlencoded");
            }

            public IRequestable ToForm(IEnumerable<KeyValuePair<string, DateTime>> param, NamingType namingType = NamingType.Normal)
                => ToForm(param.Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFFK"))), namingType);

            public IRequestable ToForm<T>(IEnumerable<KeyValuePair<string, T>> param, NamingType namingType = NamingType.Normal)
                => ToForm(param.Select(x => new KeyValuePair<string, string>(x.Key, x.Value?.ToString())), namingType);

            public IRequestable ToForm(object param, NamingType namingType = NamingType.Normal)
            {
                if (param is null)
                {
                    return this;
                }

                var typeStore = RuntimeTypeCache.Instance.GetCache(param.GetType());

                return ToForm(typeStore.PropertyStores
                    .Where(x => x.CanRead)
                    .Select(info =>
                    {
                        var item = info.Member.GetValue(param, null);

                        if (item is null) return new KeyValuePair<string, string>(info.Name, null);

                        if (item is DateTime date)
                        {
                            return new KeyValuePair<string, string>(info.Name, date.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFFK"));
                        }

                        return new KeyValuePair<string, string>(info.Name, item.ToString());

                    }), namingType);
            }

            public IRequestable ToJson(string param)
            {
                __data = param;

                return AppendHeader("Content-Type", "application/json");
            }

            public IRequestable ToJson<T>(T param, NamingType namingType = NamingType.CamelCase) where T : class
                => ToJson(JsonHelper.ToJson(param, namingType));

            public IRequestable ToQueryString(string param)
            {
                if (string.IsNullOrEmpty(param))
                {
                    return this;
                }

                string uriString = __uri.ToString();

                string query = param.TrimStart('?', '&');

                __uri = new Uri(uriString + (string.IsNullOrEmpty(__uri.Query) ? "?" : "&") + query);

                return this;
            }

            public IRequestable ToQueryString(IEnumerable<string> param)
              => ToQueryString(string.Join("&", param));

            public IRequestable ToQueryString(IEnumerable<KeyValuePair<string, string>> param)
                 => ToQueryString(string.Join("&", param.Select(kv => string.Concat(kv.Key, "=", kv.Value))));

            public IRequestable ToQueryString(IEnumerable<KeyValuePair<string, DateTime>> param)
                  => ToQueryString(string.Join("&", param.Select(x => string.Concat(x.Key, "=", x.Value.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFFK")))));

            public IRequestable ToQueryString<T>(IEnumerable<KeyValuePair<string, T>> param)
                  => ToQueryString(string.Join("&", param.Select(x => string.Concat(x.Key, "=", x.Value?.ToString() ?? string.Empty))));

            public IRequestable ToQueryString(object param, NamingType namingType = NamingType.UrlCase)
            {
                if (param is null)
                {
                    return this;
                }

                var typeStore = RuntimeTypeCache.Instance.GetCache(param.GetType());

                return ToQueryString(string.Join("&", typeStore.PropertyStores
                    .Where(x => x.CanRead)
                    .Select(info =>
                    {
                        var item = info.Member.GetValue(param, null);

                        if (item is null) return string.Concat(info.Name.ToNamingCase(namingType), "=", string.Empty);

                        if (item is DateTime date)
                        {
                            return string.Concat(info.Name.ToNamingCase(namingType), "=", date.ToString("yyyy-MM-dd HH:mm:ss"));
                        }
                        return string.Concat(info.Name.ToNamingCase(namingType), "=", item.ToString());
                    })));
            }

            public override string Request(string method, int timeout = 5000)
            {
                using (var client = new WebCoreClient
                {
                    Timeout = timeout,
                    Encoding = Encoding.UTF8
                })
                {
                    foreach (var kv in __headers)
                    {
                        client.Headers.Add(kv.Key, kv.Value);
                    }

                    if (method.ToUpper() == "GET")
                    {
                        return client.DownloadString(__uri);
                    }

                    if (__form is null)
                    {
                        return client.UploadString(__uri, method.ToUpper(), __data ?? string.Empty);
                    }

                    return Encoding.UTF8.GetString(client.UploadValues(__uri, method.ToUpper(), __form));
                }
            }

            public IRequestable ToXml(string param)
            {
                __data = param;

                return AppendHeader("Content-Type", "application/xml");
            }

            public IRequestable ToXml<T>(T param) where T : class => ToXml(XmlHelper.XmlSerialize(param));

            public IJsonRequestable<T> Json<T>(NamingType namingType = NamingType.CamelCase) where T : class => new JsonRequestable<T>(this, namingType);

            public IXmlRequestable<T> Xml<T>() where T : class => new XmlRequestable<T>(this);

            public IJsonRequestable<T> Json<T>(T _, NamingType namingType = NamingType.CamelCase) where T : class => new JsonRequestable<T>(this, namingType);

            public IXmlRequestable<T> Xml<T>(T _) where T : class => new XmlRequestable<T>(this);

            public IThenRequestable TryThen(Predicate<WebException> match) => new IIFThenRequestable(this, match);

            public IThenRequestable TryThen(Action<IRequestableBase, WebException> then) => new ThenRequestable(this, then);

            public ICatchRequestable Catch(Func<WebException, string> catchError) => new ResultCatchRequestable(this, catchError);

            public ICatchRequestable Catch(Action<WebException> catchError) => new CatchRequestable(this, catchError);

            public IFinallyRequestable Finally(Action always) => new FinallyRequestable(this, always);

            public byte[] UploadFile(string fileName, int timeout = 5000) => UploadFile(null, fileName, timeout);

            public byte[] UploadFile(string method, string fileName, int timeout = 5000)
            {
                using (var client = new WebCoreClient
                {
                    Timeout = timeout,
                    Encoding = Encoding.UTF8
                })
                {
                    foreach (var kv in __headers)
                    {
                        client.Headers.Add(kv.Key, kv.Value);
                    }

                    return client.UploadFile(__uri, method, fileName);
                }
            }

            public void DownloadFile(string fileName, int timeout = 5000)
            {
                using (var client = new WebCoreClient
                {
                    Timeout = timeout,
                    Encoding = Encoding.UTF8
                })
                {
                    foreach (var kv in __headers)
                    {
                        client.Headers.Add(kv.Key, kv.Value);
                    }

                    client.DownloadFile(__uri, fileName);
                }
            }

#if !NET40
            public override async Task<string> RequestAsync(string method, int timeout = 5000)
            {
                using (var client = new WebCoreClient
                {
                    Timeout = timeout,
                    Encoding = Encoding.UTF8
                })
                {
                    foreach (var kv in __headers)
                    {
                        client.Headers.Add(kv.Key, kv.Value);
                    }

                    if (method.ToUpper() == "GET")
                    {
                        return await client.DownloadStringTaskAsync(__uri);
                    }

                    if (__form is null)
                    {
                        return await client.UploadStringTaskAsync(__uri, method.ToUpper(), __data ?? string.Empty);
                    }

                    return Encoding.UTF8.GetString(await client.UploadValuesTaskAsync(__uri, method.ToUpper(), __form));
                }
            }

            public Task<byte[]> UploadFileAsync(string fileName, int timeout = 5000) => UploadFileAsync(null, fileName, timeout);

            public Task<byte[]> UploadFileAsync(string method, string fileName, int timeout = 5000)
            {
                using (var client = new WebCoreClient
                {
                    Timeout = timeout,
                    Encoding = Encoding.UTF8
                })
                {
                    foreach (var kv in __headers)
                    {
                        client.Headers.Add(kv.Key, kv.Value);
                    }

                    return client.UploadFileTaskAsync(__uri, method, fileName);
                }
            }

            public Task DownloadFileAsync(string fileName, int timeout = 5000)
            {
                using (var client = new WebCoreClient
                {
                    Timeout = timeout,
                    Encoding = Encoding.UTF8
                })
                {
                    foreach (var kv in __headers)
                    {
                        client.Headers.Add(kv.Key, kv.Value);
                    }

                    return client.DownloadFileTaskAsync(__uri, fileName);
                }
            }
#endif
        }

        private class JsonRequestable<T> : Requestable<T>, IJsonRequestable<T>
        {
            private readonly IRequestable<string> requestable;

            public JsonRequestable(IRequestable<string> requestable, NamingType namingType)
            {
                NamingType = namingType;
                this.requestable = requestable;
            }

            public NamingType NamingType { get; }

            public override T Request(string method, int timeout = 5000)
            {
                return JsonHelper.Json<T>(requestable.Request(method, timeout), NamingType);
            }

#if !NET40

            public override async Task<T> RequestAsync(string method, int timeout = 5000)
            {
                return JsonHelper.Json<T>(await requestable.RequestAsync(method, timeout), NamingType);
            }
#endif
            public ICatchRequestable<T> Catch(Action<string, Exception> catchError) => new JsonCatchRequestable<T>(requestable, catchError, NamingType);

            public ICatchRequestable<T> Catch(Func<string, Exception, T> catchError) => new JsonResultCatchRequestable<T>(requestable, catchError, NamingType);

            public ICatchRequestable<T> Catch(Func<WebException, T> catchError) => new ResultCatchRequestable<T>(this, catchError);

            public IFinallyRequestable<T> Finally(Action always) => new FinallyRequestable<T>(this, always);
        }

        private class XmlRequestable<T> : Requestable<T>, IXmlRequestable<T>
        {
            private readonly IRequestable<string> requestable;

            public XmlRequestable(IRequestable<string> requestable)
            {
                this.requestable = requestable;
            }

            public override T Request(string method, int timeout = 5000)
            {
                return XmlHelper.XmlDeserialize<T>(requestable.Request(method, timeout));
            }

#if !NET40
            public override async Task<T> RequestAsync(string method, int timeout = 5000)
            {
                return XmlHelper.XmlDeserialize<T>(await requestable.RequestAsync(method, timeout));
            }
#endif

            public ICatchRequestable<T> Catch(Action<string, XmlException> catchError) => new XmlCatchRequestable<T>(requestable, catchError);

            public ICatchRequestable<T> Catch(Func<string, XmlException, T> catchError) => new XmlResultCatchRequestable<T>(requestable, catchError);

            public ICatchRequestable<T> Catch(Func<WebException, T> catchError) => new ResultCatchRequestable<T>(this, catchError);

            public IFinallyRequestable<T> Finally(Action always) => new FinallyRequestable<T>(this, always);
        }

        /// <summary>
        /// 字符串是否为有效链接
        /// </summary>
        /// <param name="uriString">链接地址</param>
        /// <returns></returns>
        public static bool IsUrl(this string uriString) => UriPattern.IsMatch(uriString);

        /// <summary>
        /// 提供远程请求能力
        /// </summary>
        /// <param name="uriString">请求地址</param>
        /// <returns></returns>
        public static IRequestable AsRequestable(this string uriString)
            => new Requestable(uriString);

        /// <summary>
        /// 提供远程请求能力
        /// </summary>
        /// <param name="uri">请求地址</param>
        /// <returns></returns>
        public static IRequestable AsRequestable(this Uri uri)
            => new Requestable(uri);
    }
}
