#if NET40 || NET45 || NET451 || NET452 || NET461
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using SkyBuilding.Cache;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Management;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace SkyBuilding.Mvc.Controllers
{
    /// <summary>
    /// 登录逻辑
    /// </summary>
#if NET45 || NET451 || NET452 || NET461
    [Route]
    public sealed class __Controller : ApiController
    {
        private readonly char[] CharArray = "0123456789ABCDEabcdefghigklmnopqrFGHIGKLMNOPQRSTUVWXYZstuvwxyz".ToCharArray();

        /// <summary>
        /// 验证码
        /// </summary>
        private ICache AuthCache => CacheManager.GetCache("auth-code", CacheLevel.Second);

        /// <summary>
        /// 获取Jwt认证令牌
        /// </summary>
        /// <param name="userData">用户数据</param>
        /// <returns></returns>
        private static string GetJwtToken(object userData)
        {
            var algorithm = new HMACSHA256Algorithm();
            var serializer = new JsonNetSerializer();
            var urlEncoder = new JwtBase64UrlEncoder();
            var encoder = new JwtEncoder(algorithm, serializer, urlEncoder);

            return encoder.Encode(userData, "jwt-secret".Config(Consts.Secret));
        }

        [HttpGet]
        [Route("login")]
        public DResult Login(string authCode = null, bool debug = false)
        {
            if (!debug)
            {
                if (string.IsNullOrWhiteSpace(authCode))
                {
                    return DResult.Error("验证码不能为空!");
                }

                string id = GetRemoteMacAddress();

                string url = ControllerContext.Request.Headers.Referrer?.ToString();

                string md5 = $"{id}-{url}".Md5();

                string authCache = AuthCache.Get<string>(md5);

                if (string.IsNullOrEmpty(authCache))
                {
                    return DResult.Error("验证码已过期!");
                }

                if (authCode.Trim().ToLower() != authCache.Trim().ToLower())
                {
                    return DResult.Error("验证码错误!");
                }
            }

            var loginUrl = "login".Config<string>();

            if (string.IsNullOrEmpty(loginUrl))
            {
                return DResult.Error("未配置登录接口!", StatusCodes.ServError);
            }

            var reqUri = ControllerContext.Request.RequestUri;

            if (loginUrl.IsUrl() ? !Uri.TryCreate(loginUrl, UriKind.Absolute, out Uri loginUri) : !Uri.TryCreate($"{reqUri.Scheme}://{reqUri.Host}:{reqUri.Port}/{loginUrl.TrimStart('/')}", UriKind.Absolute, out loginUri))
            {
                return DResult.Error("不规范的登录接口!", StatusCodes.NonstandardServerError);
            }

            var result = loginUri.AsRequestable()
            .Query(reqUri.Query)
            .ByJson<DResult<Dictionary<string, object>>>()
            .Get();

            if (result.Success)
            {
                return DResult.Ok(GetJwtToken(result.Data));
            }

            return result;
        }


        [HttpGet]
        [Route("authCode")]
        public string AuthCode()
        {
            string code = CreateRandomCode(4); //验证码的字符为4个
            byte[] bytes = CreateValidateGraphic(code);

            string id = GetRemoteMacAddress();

            string url = ControllerContext.Request.Headers.Referrer?.ToString();

            string md5 = $"{id}-{url}".Md5();

            AuthCache.Set(md5, code, TimeSpan.FromMinutes(2D));

            return string.Concat("data:image/png;base64,", Convert.ToBase64String(bytes));
        }

        private string GetRemoteMacAddress()
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

            return GetRemoteIPAddress();
        }

        private string GetRemoteIPAddress()
        {
            string ipAddress = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];

            if (string.IsNullOrEmpty(ipAddress))
            {
                if (HttpContext.Current.Request.ServerVariables["HTTP_VIA"] != null)
                    ipAddress = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString().Split(',')[0].Trim();
            }

            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = HttpContext.Current.Request.UserHostAddress;
            }

            if (ipAddress == "::1")
                return "127.0.0.1";

            return ipAddress;
        }

    #region Private
        /// <summary>
        /// 生成随机的字符串
        /// </summary>
        /// <param name="codeCount"></param>
        /// <returns></returns>
        private string CreateRandomCode(int codeCount)
        {
            Random rand = new Random();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < codeCount; i++)
            {
                sb.Append(CharArray[rand.Next(35)]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 创建验证码图片
        /// </summary>
        /// <param name="validateCode"></param>
        /// <returns></returns>
        private byte[] CreateValidateGraphic(string validateCode)
        {
            Bitmap image = new Bitmap((int)Math.Ceiling(validateCode.Length * 16.0), 27);
            Graphics g = Graphics.FromImage(image);
            try
            {
                //生成随机生成器
                Random random = new Random();
                //清空图片背景色
                g.Clear(Color.White);
                //画图片的干扰线
                for (int i = 0; i < 25; i++)
                {
                    int x1 = random.Next(image.Width);
                    int x2 = random.Next(image.Width);
                    int y1 = random.Next(image.Height);
                    int y2 = random.Next(image.Height);
                    g.DrawLine(new Pen(Color.Silver), x1, x2, y1, y2);
                }
                Font font = new Font("Arial", 13, (FontStyle.Bold | FontStyle.Italic));
                LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, 0, image.Width, image.Height), Color.Blue, Color.DarkRed, 1.2f, true);
                g.DrawString(validateCode, font, brush, 3, 2);

                //画图片的前景干扰线
                for (int i = 0; i < 100; i++)
                {
                    int x = random.Next(image.Width);
                    int y = random.Next(image.Height);
                    image.SetPixel(x, y, Color.FromArgb(random.Next()));
                }
                //画图片的边框线
                g.DrawRectangle(new Pen(Color.Silver), 0, 0, image.Width - 1, image.Height - 1);

                //保存图片数据
                MemoryStream stream = new MemoryStream();
                image.Save(stream, ImageFormat.Png);

                //输出图片流
                return stream.ToArray();
            }
            finally
            {
                g.Dispose();
                image.Dispose();
            }
        }

    #endregion
    }
#else
    public sealed class LoginController : ApiController
    {
        /// <summary>
        /// 验证码
        /// </summary>
        private ICache AuthCache => CacheManager.GetCache("auth-code", CacheLevel.Second);

        /// <summary>
        /// 获取Jwt认证令牌
        /// </summary>
        /// <param name="userData">用户数据</param>
        /// <returns></returns>
        private static string GetJwtToken(object userData)
        {
            var algorithm = new HMACSHA256Algorithm();
            var serializer = new JsonNetSerializer();
            var urlEncoder = new JwtBase64UrlEncoder();
            var encoder = new JwtEncoder(algorithm, serializer, urlEncoder);

            return encoder.Encode(userData, "jwt-secret".Config(Consts.Secret));
        }

        [HttpGet]
        public DResult Get(string authCode = null, bool debug = false)
        {
            if (!debug)
            {
                if (string.IsNullOrWhiteSpace(authCode))
                {
                    return DResult.Error("验证码不能为空!");
                }

                string id = GetRemoteMacAddress();

                var uri = ControllerContext.Request.Headers.Referrer ?? ControllerContext.Request.RequestUri;

                string md5 = $"{id}-{uri.ToString()}".Md5();

                string authCache = AuthCache.Get<string>(md5);

                if (string.IsNullOrEmpty(authCache))
                {
                    return DResult.Error("验证码已过期!");
                }

                if (authCode.Trim().ToLower() != authCache.Trim().ToLower())
                {
                    return DResult.Error("验证码错误!");
                }
            }

            var loginUrl = "login".Config<string>();

            if (string.IsNullOrEmpty(loginUrl))
            {
                return DResult.Error("未配置登录接口!", StatusCodes.ServError);
            }

            var reqUri = ControllerContext.Request.RequestUri;

            if (loginUrl.IsUrl() ? !Uri.TryCreate(loginUrl, UriKind.Absolute, out Uri loginUri) : !Uri.TryCreate($"{reqUri.Scheme}://{reqUri.Host}:{reqUri.Port}/{loginUrl.TrimStart('/')}", UriKind.Absolute, out loginUri))
            {
                return DResult.Error("不规范的登录接口!", StatusCodes.NonstandardServerError);
            }

            var result = loginUri.AsRequestable()
            .Query(reqUri.Query)
            .ByJson<DResult<Dictionary<string, object>>>()
            .Get();

            if (result.Success)
            {
                return DResult.Ok(GetJwtToken(result.Data));
            }

            return result;
        }

        private string GetRemoteMacAddress()
        {
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");

            ManagementObjectCollection moc2 = mc.GetInstances();

            foreach (ManagementObject mo in moc2)
            {
                if ((bool)mo["IPEnabled"] == true)
                {
                    return mo["MacAddress"].ToString();
                }
            }

            return GetRemoteIPAddress();
        }

        private string GetRemoteIPAddress()
        {
            string ipAddress = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];

            if (string.IsNullOrEmpty(ipAddress))
            {
                if (HttpContext.Current.Request.ServerVariables["HTTP_VIA"] != null)
                    ipAddress = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString().Split(',')[0].Trim();
            }

            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = HttpContext.Current.Request.UserHostAddress;
            }

            if (ipAddress == "::1")
                return "127.0.0.1";

            return ipAddress;
        }

    }

    public sealed class AuthCodeController : ApiController
    {
        private readonly char[] CharArray = "0123456789ABCDEabcdefghigklmnopqrFGHIGKLMNOPQRSTUVWXYZstuvwxyz".ToCharArray();

        /// <summary>
        /// 验证码
        /// </summary>
        private ICache AuthCache => CacheManager.GetCache("auth-code", CacheLevel.Second);

        private string GetRemoteMacAddress()
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

            return GetRemoteIPAddress();
        }

        private string GetRemoteIPAddress()
        {
            string ipAddress = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];

            if (string.IsNullOrEmpty(ipAddress))
            {
                if (HttpContext.Current.Request.ServerVariables["HTTP_VIA"] != null)
                    ipAddress = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString().Split(',')[0].Trim();
            }

            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = HttpContext.Current.Request.UserHostAddress;
            }

            if (ipAddress == "::1")
                return "127.0.0.1";

            return ipAddress;
        }

        public override Task<HttpResponseMessage> ExecuteAsync(HttpControllerContext controllerContext, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                string code = CreateRandomCode(4); //验证码的字符为4个
                byte[] bytes = CreateValidateGraphic(code);

                string id = GetRemoteMacAddress();

                var uri = controllerContext.Request.Headers.Referrer ?? controllerContext.Request.RequestUri;

                string md5 = $"{id}-{uri.ToString()}".Md5();

                AuthCache.Set(md5, code, TimeSpan.FromMinutes(2D));

                string imgUrl = string.Concat("data:image/png;base64,", Convert.ToBase64String(bytes));
                return new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(imgUrl)
                };
            });
        }

        #region Private
        /// <summary>
        /// 生成随机的字符串
        /// </summary>
        /// <param name="codeCount"></param>
        /// <returns></returns>
        private string CreateRandomCode(int codeCount)
        {
            Random rand = new Random();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < codeCount; i++)
            {
                sb.Append(CharArray[rand.Next(35)]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 创建验证码图片
        /// </summary>
        /// <param name="validateCode"></param>
        /// <returns></returns>
        private byte[] CreateValidateGraphic(string validateCode)
        {
            Bitmap image = new Bitmap((int)Math.Ceiling(validateCode.Length * 16.0), 27);
            Graphics g = Graphics.FromImage(image);
            try
            {
                //生成随机生成器
                Random random = new Random();
                //清空图片背景色
                g.Clear(Color.White);
                //画图片的干扰线
                for (int i = 0; i < 25; i++)
                {
                    int x1 = random.Next(image.Width);
                    int x2 = random.Next(image.Width);
                    int y1 = random.Next(image.Height);
                    int y2 = random.Next(image.Height);
                    g.DrawLine(new Pen(Color.Silver), x1, x2, y1, y2);
                }
                Font font = new Font("Arial", 13, (FontStyle.Bold | FontStyle.Italic));
                LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, 0, image.Width, image.Height), Color.Blue, Color.DarkRed, 1.2f, true);
                g.DrawString(validateCode, font, brush, 3, 2);

                //画图片的前景干扰线
                for (int i = 0; i < 100; i++)
                {
                    int x = random.Next(image.Width);
                    int y = random.Next(image.Height);
                    image.SetPixel(x, y, Color.FromArgb(random.Next()));
                }
                //画图片的边框线
                g.DrawRectangle(new Pen(Color.Silver), 0, 0, image.Width - 1, image.Height - 1);

                //保存图片数据
                MemoryStream stream = new MemoryStream();
                image.Save(stream, ImageFormat.Png);

                //输出图片流
                return stream.ToArray();
            }
            finally
            {
                g.Dispose();
                image.Dispose();
            }
        }

        #endregion
    }
#endif
}
#endif