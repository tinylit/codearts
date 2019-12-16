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
using System.Threading.Tasks;

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

            public Task<T> GetAsync(int timeout = 5000) => RequestAsync("GET", timeout);

            public T Delete(int timeout = 5000) => Request("DELETE", timeout);

            public Task<T> DeleteAsync(int timeout = 5000) => RequestAsync("DELETE", timeout);

            public T Post(int timeout = 5000) => Request("POST", timeout);

            public Task<T> PostAsync(int timeout = 5000) => RequestAsync("POST", timeout);

            public T Put(int timeout = 5000) => Request("PUT", timeout);

            public Task<T> PutAsync(int timeout = 5000) => RequestAsync("PUT", timeout);

            public T Head(int timeout = 5000) => Request("HEAD", timeout);

            public Task<T> HeadAsync(int timeout = 5000) => RequestAsync("HEAD", timeout);

            public T Patch(int timeout = 5000) => Request("PATCH", timeout);

            public Task<T> PatchAsync(int timeout = 5000) => RequestAsync("PATCH", timeout);

            public abstract T Request(string method, int timeout = 5000);

#if NET40
            public Task<T> RequestAsync(string method, int timeout = 5000)
                => Task.Factory.StartNew(() => Request(method, timeout));
#else

            public abstract Task<T> RequestAsync(string method, int timeout = 5000);
#endif
        }

        /// <summary>
        /// 默认请求
        /// </summary>
        private class Requestable : Requestable<string>, IRequestable
        {
            private string __data;
            private Uri __uri;
            private NameValueCollection __form;
            private readonly Dictionary<string, string> __headers;

            /// <summary>
            /// 建立请求
            /// </summary>
            /// <param name="uriString">请求连接</param>
            public Requestable(string uriString) : this(new Uri(uriString)) { }

            /// <summary>
            /// 建立请求
            /// </summary>
            /// <param name="uri">请求连接</param>
            public Requestable(Uri uri)
            {
                __uri = uri ?? throw new ArgumentNullException(nameof(uri));
                __headers = new Dictionary<string, string>();
            }

            /// <summary>
            /// 添加请求头
            /// </summary>
            /// <param name="header">头</param>
            /// <param name="value">头信息</param>
            /// <returns></returns>
            public IRequestable AppendHeader(string header, string value)
            {
                __headers.Add(header, value);

                return this;
            }

            /// <summary>
            /// 批量添加请求头
            /// </summary>
            /// <param name="headers">请求头集合</param>
            /// <returns></returns>
            public IRequestable AppendHeaders(IEnumerable<KeyValuePair<string, string>> headers)
            {
                foreach (var kv in headers)
                {
                    __headers.Add(kv.Key, kv.Value);
                }

                return this;
            }

            /// <summary>
            /// content-type = "application/x-www-form-urlencoded";
            /// </summary>
            /// <param name="param">参数</param>
            /// <returns></returns>
            public IRequestable ToForm(string param)
                => ToForm(JsonHelper.Json<Dictionary<string, string>>(param, NamingType.Normal));

            /// <summary>
            /// content-type = "application/x-www-form-urlencoded";
            /// </summary>
            /// <param name="param">参数</param>
            /// <param name="namingType">命名规则</param>
            /// <returns></returns>
            public IRequestable ToForm(IEnumerable<KeyValuePair<string, string>> param, NamingType namingType = NamingType.Normal)
            {
                __form = __form ?? new NameValueCollection();

                foreach (var kv in param)
                {
                    __form.Add(kv.Key.ToNamingCase(namingType), kv.Value);
                }

                return AppendHeader("Content-Type", "application/x-www-form-urlencoded");
            }

            /// <summary>
            /// content-type = "application/x-www-form-urlencoded";
            /// </summary>
            /// <param name="param">参数</param>
            /// <param name="namingType">命名规则</param>
            /// <returns></returns>
            public IRequestable ToForm(IEnumerable<KeyValuePair<string, DateTime>> param, NamingType namingType = NamingType.Normal)
                => ToForm(param.Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString("yyyy-MM-dd HH:mm:ss"))), namingType);

            /// <summary>
            /// content-type = "application/x-www-form-urlencoded";
            /// </summary>
            /// <param name="param">参数</param>
            /// <param name="namingType">命名规则</param>
            /// <returns></returns>
            public IRequestable ToForm<T>(IEnumerable<KeyValuePair<string, T>> param, NamingType namingType = NamingType.Normal)
                => ToForm(param.Select(x =>
                {
                    if (x.Value is DateTime date)
                    {
                        return new KeyValuePair<string, string>(x.Key, date.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFFK"));
                    }
                    return new KeyValuePair<string, string>(x.Key, x.Value?.ToString());
                }), namingType);

            /// <summary>
            /// content-type = "application/x-www-form-urlencoded";
            /// </summary>
            /// <param name="param">参数</param>
            /// <param name="namingType">命名规则</param>
            /// <returns></returns>
            public IRequestable ToForm(object param, NamingType namingType = NamingType.Normal)
            {
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

            /// <summary>
            /// content-type = "application/json"
            /// </summary>
            /// <param name="param">参数</param>
            /// <returns></returns>
            public IRequestable ToJson(string param)
            {
                __data = param;

                return AppendHeader("Content-Type", "application/json");
            }

            /// <summary>
            /// content-type = "application/json"
            /// </summary>
            /// <param name="param">参数</param>
            /// <param name="namingType">命名规则</param>
            /// <returns></returns>
            public IRequestable ToJson<T>(T param, NamingType namingType = NamingType.CamelCase) where T : class
                => ToJson(JsonHelper.ToJson(param, namingType));

            /// <summary>
            /// 请求参数。?id=1&amp;name="yep"
            /// </summary>
            /// <param name="param">参数</param>
            /// <returns></returns>
            public IRequestable ToQueryString(string param)
            {
                if (string.IsNullOrEmpty(param)) return this;

                string uriString = __uri.ToString();

                string query = param.TrimStart('?', '&');

                __uri = new Uri(uriString + (string.IsNullOrEmpty(__uri.Query) ? "?" : "&") + query);

                return this;
            }

            /// <summary>
            /// 请求参数。?id=1&amp;name="yep"
            /// </summary>
            /// <param name="param">参数</param>
            /// <returns></returns>
            public IRequestable ToQueryString(IEnumerable<string> param)
              => ToQueryString(string.Join("&", param));

            /// <summary>
            /// 请求参数。?id=1&amp;name="yep"
            /// </summary>
            /// <param name="param">参数</param>
            /// <returns></returns>
            public IRequestable ToQueryString(IEnumerable<KeyValuePair<string, string>> param)
                 => ToQueryString(string.Join("&", param.Select(kv => string.Concat(kv.Key, "=", kv.Value))));

            /// <summary>
            /// 请求参数。?id=1&amp;name="yep"
            /// </summary>
            /// <typeparam name="T">类型</typeparam>
            /// <param name="param">参数</param>
            /// <returns></returns>
            public IRequestable ToQueryString<T>(IEnumerable<KeyValuePair<string, T>> param)
                  => ToQueryString(string.Join("&", param.Select(kv =>
                  {
                      if (kv.Value is DateTime date)
                      {
                          return string.Concat(kv.Key, "=", date.ToString("yyyy-MM-dd HH:mm:ss"));
                      }

                      return string.Concat(kv.Key, "=", kv.Value?.ToString() ?? string.Empty);
                  })));

            /// <summary>
            /// 请求参数。?id=1&amp;name="yep"
            /// </summary>
            /// <param name="param">参数</param>
            /// <param name="namingType">命名规则</param>
            /// <returns></returns>
            public IRequestable ToQueryString(object param, NamingType namingType = NamingType.UrlCase)
            {
                var typeStore = RuntimeTypeCache.Instance.GetCache(param.GetType());

                return ToQueryString(string.Join("&", typeStore.PropertyStores
                    .Where(x => x.CanRead)
                    .Select(info =>
                    {
                        var item = info.Member.GetValue(param, null);

                        if (item is null) return null;

                        if (item is DateTime date)
                        {
                            return string.Concat(info.Name.ToNamingCase(namingType), "=", date.ToString("yyyy-MM-dd HH:mm:ss"));
                        }
                        return string.Concat(info.Name.ToNamingCase(namingType), "=", item.ToString());
                    })
                    .Where(x => !(x is null))));
            }

            /// <summary>
            /// 请求
            /// </summary>
            /// <param name="method">请求方式</param>
            /// <param name="timeout">超时 时间</param>
            /// <returns></returns>
            public override string Request(string method, int timeout = 5)
            {
                using (var client = new SkyWebClient
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
                        return Encoding.UTF8.GetString(client.DownloadData(__uri));
                    }

                    if (__form is null)
                    {
                        client.UploadString(__uri, method.ToUpper(), __data ?? string.Empty);
                    }

                    return Encoding.UTF8.GetString(client.UploadValues(__uri, method.ToUpper(), __form));
                }
            }

#if !NET40
            /// <summary>
            /// 请求
            /// </summary>
            /// <param name="method">请求方式</param>
            /// <param name="timeout">超时时间</param>
            /// <returns></returns>
            public override async Task<string> RequestAsync(string method, int timeout = 5)
            {
                using (var client = new SkyWebClient
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
                        return Encoding.UTF8.GetString(await client.DownloadDataTaskAsync(__uri));
                    }

                    if (__form is null)
                    {
                        return await client.UploadStringTaskAsync(__uri, method.ToUpper(), __data ?? string.Empty);
                    }

                    return Encoding.UTF8.GetString(await client.UploadValuesTaskAsync(__uri, method.ToUpper(), __form));
                }
            }
#endif
            public IRequestable ToXml(string param)
            {
                __data = param;

                return AppendHeader("Content-Type", "application/xml");
            }

            public IRequestable ToXml<T>(T param) where T : class => ToXml(XmlHelper.XmlSerialize(param));

            public IJsonRequestable<T> Json<T>(NamingType namingType = NamingType.CamelCase) where T : class => new JsonRequestable<T>(this, namingType);

            public IXmlRequestable<T> Xml<T>() where T : class => new XmlRequestable<T>(this);

            /// <summary>
            /// 数据返回JSON格式的结果，将转为指定类型
            /// </summary>
            /// <typeparam name="T">类型</typeparam>
            /// <param name="_">匿名对象</param>
            /// <param name="namingType">命名规则</param>
            /// <returns></returns>
            public IJsonRequestable<T> Json<T>(T _, NamingType namingType = NamingType.CamelCase) where T : class => new JsonRequestable<T>(this, namingType);
            /// <summary>
            /// 数据返回XML格式的结果，将转为指定类型
            /// </summary>
            /// <typeparam name="T">类型</typeparam>
            /// <param name="_">匿名对象</param>
            /// <returns></returns>
            public IXmlRequestable<T> Xml<T>(T _) where T : class => new XmlRequestable<T>(this);
        }

        private class JsonRequestable<T> : Requestable<T>, IJsonRequestable<T>
        {
            private readonly IRequestable requestable;

            public JsonRequestable(IRequestable requestable, NamingType namingType)
            {
                this.requestable = requestable;
                NamingType = namingType;
            }

            public NamingType NamingType { get; }

            public override T Request(string method, int timeout = 5000)
            {
                string value = requestable.Request(method, timeout);

                if (string.IsNullOrEmpty(value))
                    return default;
                try
                {
                    return JsonHelper.Json<T>(value, NamingType);
                }
                catch
                {
                    return default;
                }
            }

#if !NET40

            public override async Task<T> RequestAsync(string method, int timeout = 5000)
            {
                string value = await requestable.RequestAsync(method, timeout);

                if (string.IsNullOrEmpty(value))
                    return default;

                try
                {
                    return JsonHelper.Json<T>(value, NamingType);
                }
                catch
                {
                    return default;
                }
            }
#endif
        }

        private class XmlRequestable<T> : Requestable<T>, IXmlRequestable<T>
        {
            private readonly IRequestable requestable;

            public XmlRequestable(IRequestable requestable)
            {
                this.requestable = requestable;
            }

            public override T Request(string method, int timeout = 5000)
            {
                string value = requestable.Request(method, timeout);

                if (string.IsNullOrEmpty(value))
                    return default;
                try
                {
                    return XmlHelper.XmlDeserialize<T>(value);
                }
                catch
                {
                    return default;
                }
            }

#if !NET40
            public override async Task<T> RequestAsync(string method, int timeout = 5000)
            {
                string value = await requestable.RequestAsync(method, timeout);

                if (string.IsNullOrEmpty(value))
                    return default;

                try
                {
                    return XmlHelper.XmlDeserialize<T>(value);
                }
                catch
                {
                    return default;
                }
            }
#endif
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
