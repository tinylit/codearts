using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SkyBuilding.Net
{
    /// <summary>
    /// Web客户端
    /// </summary>
    public class SkyWebClient : WebClient
    {
        /// <summary>
        /// 静态构造函数
        /// </summary>
        static SkyWebClient()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
        }

        /// <summary>
        /// 过期时间（单位：秒）
        /// </summary>
        public int Timeout { get; set; } = 5;

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);

            request.Timeout = 1000 * Timeout;

            return request;
        }
    }
}
