using System;
using System.Net;

namespace CodeArts.Net
{
    /// <summary>
    /// Web客户端。
    /// </summary>
    public class WebCoreClient : WebClient
    {
        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static WebCoreClient()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
        }

        /// <summary>
        /// 过期时间（单位：毫秒）。
        /// </summary>
        public int Timeout { get; set; } = 5000;

        /// <summary>
        /// 获取 Web 请求实例。
        /// </summary>
        /// <param name="address">请求地址。</param>
        /// <returns></returns>
        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);

            request.Timeout = Timeout;

            return request;
        }
    }
}
