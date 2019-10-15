#if NETSTANDARD2_0 || NETSTANDARD2_1
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using SkyBuilding.Exceptions;
using System.Linq;
using System.Net.NetworkInformation;

namespace SkyBuilding.Mvc
{
    /// <summary>
    /// 请求上下文扩展
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// 获取客户端IP地址
        /// </summary>
        /// <returns></returns>
        public static string GetRemoteIpAddress(this HttpContext context)
        {
            string ipAddress = context.Connection.RemoteIpAddress.ToString();

            if (ipAddress == "::1")
                return "127.0.0.1";

            return ipAddress;
        }

        /// <summary>
        /// 获取客户端Mac地址
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GetRemoteMacAddress(this HttpContext context)
        {
            var networks = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var network in networks.Where(x => x.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
            {
                var physicalAddress = network.GetPhysicalAddress();

                return string.Join("-", physicalAddress.GetAddressBytes().Select(b => b.ToString("X2")));
            }

            return null;
        }

        /// <summary>
        /// 获取请求方的地址
        /// </summary>
        /// <returns></returns>
        public static string GetRefererUrlStrings(this HttpContext context)
        {
            StringValues origin = context.Request.Headers[HeaderNames.Referer];
            if (origin == StringValues.Empty)
            {
                return string.Empty;
            }
            return origin.ToString();
        }
    }
}
#endif