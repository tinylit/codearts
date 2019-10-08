using SkyBuilding;
using SkyBuilding.Net;
using SkyBuilding.Serialize.Json;
using SkyBuilding.Serialize.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    /// <summary>
    /// 地址拓展
    /// </summary>
    public static class UriExtentions
    {
        private abstract class Requestable<T> : IRequestable<T>
        {
            public T Get(int timeout = 5) => Request("GET", timeout);

            public Task<T> GetAsync(int timeout = 5) => RequestAsync("GET", timeout);

            public T Delete(int timeout = 5) => Request("DELETE", timeout);

            public Task<T> DeleteAsync(int timeout = 5) => RequestAsync("DELETE", timeout);

            public T Post(int timeout = 5) => Request("POST", timeout);

            public Task<T> PostAsync(int timeout = 5) => RequestAsync("POST", timeout);

            public T Put(int timeout = 5) => Request("PUT", timeout);

            public Task<T> PutAsync(int timeout = 5) => RequestAsync("PUT", timeout);

            public T Head(int timeout = 5) => Request("HEAD", timeout);

            public Task<T> HeadAsync(int timeout) => RequestAsync("HEAD", timeout);

            public T Patch(int timeout = 5) => Request("PATCH", timeout);

            public Task<T> PatchAsync(int timeout) => RequestAsync("PATCH", timeout);

            public abstract T Request(string method, int timeout = 5);

#if NET40
            public Task<T> RequestAsync(string method, int timeout = 5)
                => Task.Factory.StartNew(() => Request(method, timeout));
#else

            public abstract Task<T> RequestAsync(string method, int timeout = 5);
#endif
        }

        /// <summary>
        /// 默认请求
        /// </summary>
        private class Requestable : Requestable<string>, IRequestable
        {
            private string __data;
            private Uri __uri;
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
            public IRequestable Header(string header, string value)
            {
                __headers.Add(header, value);

                return this;
            }

            /// <summary>
            /// 批量添加请求头
            /// </summary>
            /// <param name="headers">请求头集合</param>
            /// <returns></returns>
            public IRequestable Headers(IEnumerable<KeyValuePair<string, string>> headers)
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
            public IRequestable Form(string param)
            {
                __data = param;

                return Header("Content-Type", "application/x-www-form-urlencoded");
            }

            /// <summary>
            /// content-type = "application/x-www-form-urlencoded";
            /// </summary>
            /// <param name="param">参数</param>
            /// <returns></returns>
            public IRequestable Form<T>(T param, NamingType namingType = NamingType.CamelCase) where T : class => Form(JsonHelper.ToJson(param, namingType));

            /// <summary>
            /// content-type = "application/json"
            /// </summary>
            /// <param name="param">参数</param>
            /// <returns></returns>
            public IRequestable Json(string param)
            {
                __data = param;

                return Header("Content-Type", "application/json");
            }

            /// <summary>
            /// content-type = "application/json"
            /// </summary>
            /// <param name="param">参数</param>
            /// <returns></returns>
            public IRequestable Json<T>(T param, NamingType namingType = NamingType.Normal) where T : class
                => Json(JsonHelper.ToJson(param, namingType));

            /// <summary>
            /// 请求参数。?id=1&name="yep"
            /// </summary>
            /// <param name="param">参数</param>
            /// <returns></returns>
            public IRequestable Query(string param)
            {
                if (string.IsNullOrEmpty(param)) return this;

                string uriString = __uri.ToString();

                string query = param.TrimStart('?', '&');

                __uri = new Uri(uriString + (string.IsNullOrEmpty(__uri.Query) ? "?" : "&") + query);

                return this;
            }

            /// <summary>
            /// 请求参数。?id=1&name="yep"
            /// </summary>
            /// <param name="param">参数</param>
            /// <returns></returns>
            public IRequestable Query(IEnumerable<string> param)
              => Query(string.Join("&", param));

            /// <summary>
            /// 请求参数。?id=1&name="yep"
            /// </summary>
            /// <param name="param">参数</param>
            /// <returns></returns>
            public IRequestable Query(IEnumerable<KeyValuePair<string, string>> param)
                 => Query(string.Join("&", param.Select(kv => string.Concat(kv.Key, "=", kv.Value))));

            /// <summary>
            /// 请求参数。?id=1&name="yep"
            /// </summary>
            /// <param name="param">参数</param>
            /// <returns></returns>
            public IRequestable Query(IEnumerable<KeyValuePair<string, DateTime>> param)
                 => Query(string.Join("&", param.Select(kv => string.Concat(kv.Key, "=", kv.Value.ToString("yyyy-MM-dd HH:mm:ss")))));

            /// <summary>
            /// 请求参数。?id=1&name="yep"
            /// </summary>
            /// <typeparam name="T">类型</typeparam>
            /// <param name="param">参数</param>
            /// <returns></returns>
            public IRequestable Query<T>(IEnumerable<KeyValuePair<string, T>> param)
                  => Query(string.Join("&", param.Select(kv =>
                  {
                      if (kv.Value is DateTime date)
                      {
                          return string.Concat(kv.Key, "=", date.ToString("yyyy-MM-dd HH:mm:ss"));
                      }

                      return string.Concat(kv.Key, "=", kv.Value?.ToString());
                  })));

            /// <summary>
            /// 请求参数。?id=1&name="yep"
            /// </summary>
            /// <typeparam name="T">类型</typeparam>
            /// <param name="param">参数</param>
            /// <returns></returns>
            public IRequestable Query<T>(T param, NamingType namingType = NamingType.UrlCase) where T : class
            {
                if (param is null) return this;

                var typeStore = RuntimeTypeCache.Instance.GetCache<T>();

                return Query(string.Join("&", typeStore.PropertyStores
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

                    return client.UploadString(__uri, method.ToUpper(), __data ?? string.Empty);
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

                    return await client.UploadStringTaskAsync(__uri, method.ToUpper(), __data ?? string.Empty);
                }
            }
#endif
            public IRequestable Xml(string param)
            {
                __data = param;
                return Header("Content-Type", "application/xml");
            }

            public IRequestable Xml<T>(T param) where T : class => Xml(XmlHelper.XmlSerialize(param));

            public IJsonRequestable<T> ByJson<T>(NamingType namingType = NamingType.PascalCase) => new JsonRequestable<T>(this, namingType);

            public IXmlRequestable<T> ByXml<T>() => new XmlRequestable<T>(this);

            /// <summary>
            /// 数据返回JSON格式的结果，将转为指定类型
            /// </summary>
            /// <typeparam name="T">类型</typeparam>
            /// <param name="_">匿名对象</param>
            /// <param name="method">求取方式</param>
            /// <param name="timeout">超时时间，单位：秒</param>
            /// <returns></returns>
            public IJsonRequestable<T> ByJson<T>(T _, NamingType namingType = NamingType.PascalCase) where T : class => new JsonRequestable<T>(this, namingType);
            /// <summary>
            /// 数据返回XML格式的结果，将转为指定类型
            /// </summary>
            /// <typeparam name="T">类型</typeparam>
            /// <param name="_">匿名对象</param>
            /// <param name="method">求取方式</param>
            /// <param name="timeout">超时时间，单位：秒</param>
            /// <returns></returns>
            public IXmlRequestable<T> ByXml<T>(T _) where T : class => new XmlRequestable<T>(this);
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

            public override T Request(string method, int timeout = 5)
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

            public override async Task<T> RequestAsync(string method, int timeout = 5)
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

            public override T Request(string method, int timeout = 5)
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
            public override async Task<T> RequestAsync(string method, int timeout = 5)
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
