#if NETSTANDARD2_0 || NETCOREAPP3_1
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System.Net.NetworkInformation;
#else
using System.Web;
using System.Management;
#endif

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
        /// <param name="context">请求上下文</param>
        /// <returns></returns>
#if NETSTANDARD2_0 || NETCOREAPP3_1
        public static string GetRemoteIpAddress(this HttpContext context)
        {
            string ipAddress = context.Connection.RemoteIpAddress.ToString();

            if (ipAddress == "::1")
                return "127.0.0.1";

            return ipAddress;
        }
#else
        public static string GetRemoteIpAddress(this HttpContext context)
        {
            string ipAddress = context.Request.ServerVariables["REMOTE_ADDR"];

            if (string.IsNullOrEmpty(ipAddress))
            {
                if (context.Request.ServerVariables["HTTP_VIA"] != null)
                    ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString().Split(',')[0].Trim();
            }

            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Request.UserHostAddress;
            }

            if (ipAddress == "::1")
                return "127.0.0.1";

            return ipAddress;
        }
#endif

        /// <summary>
        /// 获取客户端Mac地址
        /// </summary>
        /// <param name="context">请求上下文</param>
        /// <returns></returns>
#if NETSTANDARD2_0 || NETCOREAPP3_1
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
#else
        public static string GetRemoteMacAddress(this HttpContext context)
        {
            using (ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration"))
            {
                ManagementObjectCollection moc2 = mc.GetInstances();

                foreach (ManagementObject mo in moc2)
                {
                    if ((bool)mo["IPEnabled"] == true)
                    {
                        return mo["MacAddress"].ToString();
                    }
                }
            }
            return null;
        }
#endif

        /// <summary>
        /// 获取请求方的地址
        /// </summary>
        /// <returns></returns>
#if NETSTANDARD2_0 || NETCOREAPP3_1
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
#else
        public static string GetRefererUrlStrings(this HttpContext context) => context.Request.UrlReferrer?.ToString();
    }
#endif
}