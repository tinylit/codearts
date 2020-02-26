#if NETSTANDARD2_0 || NETCOREAPP3_1
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using CodeArts;
using CodeArts.Mvc;
#elif !NET40
using System.Threading.Tasks;
#endif
using CodeArts.Cache;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using CodeArts.Exceptions;
using System.Net;
#if NETSTANDARD2_0 || NETCOREAPP3_1
namespace Microsoft.AspNetCore.Builder
#else
namespace CodeArts.Mvc.Builder
#endif
{
    /// <summary>
    /// 登录
    /// </summary>
    public static class AuthApplicationBuilderExtentions
    {
        private static readonly char[] CharArray = "0123456789ABCDEabcdefghigklmnopqrFGHIGKLMNOPQRSTUVWXYZstuvwxyz".ToCharArray();

        /// <summary>
        /// 验证码
        /// </summary>
        public static ICache AuthCode => CacheManager.GetCache("auth-code", CacheLevel.Second);

        /// <summary>
        /// 登录配置。
        /// 通过“/login”登录。
        /// 通过“/authcode”获取验证码。
        /// 请在配置文件中配置“login”项(可以是相对地址或绝对地址)。
        /// 添加请求参数“debug”为真时，不进行验证码验证。
        /// 自定义请参考<see cref="Consts"/>。
        /// </summary>
        /// <param name="app">配置</param>
        /// <returns></returns>
#if NETSTANDARD2_0 || NETCOREAPP3_1
        public static IApplicationBuilder UseJwtAuth(this IApplicationBuilder app)
        {
            return app.Map("/authCode", builder => builder.Run(async context =>
            {
                string code = CreateRandomCode("captcha:length".Config(4)); //验证码的字符为4个
                byte[] bytes = CreateValidateGraphic(code);

                string id = context.GetRemoteMacAddress() ?? context.GetRemoteIpAddress();
                string url = context.GetRefererUrlStrings();

                string md5 = $"{id}-{url}".Md5();

                AuthCode.Set(md5, code, TimeSpan.FromMinutes(2D));

                await context.Response.WriteImageAsync(bytes);

            })).Map("/login", builder => builder.Run(async context =>
            {
                if (!context.Request.Query.TryGetValue("debug", out StringValues debug) || !bool.TryParse(debug, out bool isDebug) || !isDebug)
                {
                    if (!context.Request.Query.TryGetValue("authCode", out StringValues value) || value == StringValues.Empty)
                    {
                        await context.Response.WriteJsonAsync(DResult.Error("验证码不能为空!"));
                        return;
                    }

                    string id = context.GetRemoteMacAddress() ?? context.GetRemoteIpAddress();
                    string url = context.GetRefererUrlStrings();

                    string md5 = $"{id}-{url}".Md5();

                    string authCache = AuthCode.Get<string>(md5);

                    if (string.IsNullOrEmpty(authCache))
                    {
                        await context.Response.WriteJsonAsync(DResult.Error("验证码已过期!"));
                        return;
                    }

                    string authCode = value.ToString();

                    if (authCode.Trim().ToLower() != authCache.Trim().ToLower())
                    {
                        await context.Response.WriteJsonAsync(DResult.Error("验证码错误!"));
                        return;
                    }
                }

                var loginUrl = "login".Config<string>();

                if (string.IsNullOrEmpty(loginUrl))
                {
                    await context.Response.WriteJsonAsync(DResult.Error("未配置登录接口!", CodeArts.StatusCodes.ServError));
                    return;
                }

                if (loginUrl.IsUrl() ? !Uri.TryCreate(loginUrl, UriKind.Absolute, out Uri loginUri) : !Uri.TryCreate($"{context.Request.Scheme}://{context.Request.Host}/{loginUrl.TrimStart('/')}", UriKind.Absolute, out loginUri))
                {
                    await context.Response.WriteJsonAsync(DResult.Error("不规范的登录接口!", CodeArts.StatusCodes.NonstandardServerError));
                    return;
                }


                var request = loginUri
                    .AsRequestable()
                    .ToQueryString(context.Request.QueryString.ToString());

                string contentType = context.Request.ContentType?.ToLower() ?? "application/json";

                if (contentType.Contains("application/x-www-form-urlencoded"))
                {
                    request.ToForm(context.Request.Form);
                }
                else if (context.Request.Body.Length > 0)
                {
                    using (var reader = new StreamReader(context.Request.Body))
                    {
                        if (contentType.Contains("application/json"))
                        {
                            request.ToJson(reader.ReadToEnd());
                        }
                        else if (contentType.Contains("application/xml"))
                        {
                            request.ToXml(reader.ReadToEnd());
                        }
                        else
                        {
                            await context.Response.WriteJsonAsync(DResult.Error($"未实现({contentType})类型传输!"));
                            return;
                        }
                    }
                }
                try
                {
                    var result = await request
                        .Json<ServResult<Dictionary<string, object>>>()
                        .RequestAsync(context.Request.Method ?? "GET", "map:timeout".Config(10000));

                    if (result.Success)
                    {
                        await context.Response.WriteJsonAsync(DResult.Ok(JwtTokenGen.Create(result.Data)));
                    }
                    else
                    {
                        await context.Response.WriteJsonAsync(result);
                    }
                }
                catch (Exception e)
                {
                    await context.Response.WriteJsonAsync(ExceptionHandler.Handler(e));
                }


            })).Map("/register", builder => builder.Run(async context =>
                {
                    if (!context.Request.Query.TryGetValue("debug", out StringValues debug) || !bool.TryParse(debug, out bool isDebug) || !isDebug)
                    {
                        if (!context.Request.Query.TryGetValue("authCode", out StringValues value) || value == StringValues.Empty)
                        {
                            await context.Response.WriteJsonAsync(DResult.Error("验证码不能为空!"));
                            return;
                        }

                        string id = context.GetRemoteMacAddress() ?? context.GetRemoteIpAddress();
                        string url = context.GetRefererUrlStrings();

                        string md5 = $"{id}-{url}".Md5();

                        string authCache = AuthCode.Get<string>(md5);

                        if (string.IsNullOrEmpty(authCache))
                        {
                            await context.Response.WriteJsonAsync(DResult.Error("验证码已过期!"));
                            return;
                        }

                        string authCode = value.ToString();

                        if (authCode.Trim().ToLower() != authCache.Trim().ToLower())
                        {
                            await context.Response.WriteJsonAsync(DResult.Error("验证码错误!"));
                            return;
                        }
                    }

                    var registerUrl = "register".Config<string>();

                    if (string.IsNullOrEmpty(registerUrl))
                    {
                        await context.Response.WriteJsonAsync(DResult.Error("未配置注册接口!", CodeArts.StatusCodes.ServError));
                        return;
                    }

                    if (registerUrl.IsUrl() ? !Uri.TryCreate(registerUrl, UriKind.Absolute, out Uri registerUri) : !Uri.TryCreate($"{context.Request.Scheme}://{context.Request.Host}/{registerUrl.TrimStart('/')}", UriKind.Absolute, out registerUri))
                    {
                        await context.Response.WriteJsonAsync(DResult.Error("不规范的注册接口!", CodeArts.StatusCodes.NonstandardServerError));
                        return;
                    }

                    var request = registerUrl
                        .AsRequestable()
                        .ToQueryString(context.Request.QueryString.ToString());

                    string contentType = context.Request.ContentType?.ToLower() ?? "application/json";

                    if (contentType.Contains("application/x-www-form-urlencoded"))
                    {
                        request.ToForm(context.Request.Form);
                    }
                    else if (context.Request.Body.Length > 0)
                    {
                        using (var reader = new StreamReader(context.Request.Body))
                        {
                            if (contentType.Contains("application/json"))
                            {
                                request.ToJson(reader.ReadToEnd());
                            }
                            else if (contentType.Contains("application/xml"))
                            {
                                request.ToXml(reader.ReadToEnd());
                            }
                            else
                            {
                                await context.Response.WriteJsonAsync(DResult.Error($"未实现({contentType})类型传输!"));
                                return;
                            }
                        }
                    }

                    try
                    {
                        var result = await request
                                .Json<ServResult<Dictionary<string, object>>>()
                                .RequestAsync(context.Request.Method ?? "GET", "map:timeout".Config(10000));

                        if (result.Success)
                        {
                            if (result.Data is null || result.Data.Count == 0)
                            {
                                await context.Response.WriteJsonAsync(DResult.Ok());
                            }
                            else
                            {
                                await context.Response.WriteJsonAsync(DResult.Ok(JwtTokenGen.Create(result.Data)));
                            }
                        }
                        else
                        {
                            await context.Response.WriteJsonAsync(result);
                        }
                    }
                    catch (Exception e)
                    {
                        await context.Response.WriteJsonAsync(ExceptionHandler.Handler(e));
                    }
                }));
        }
#else
        public static IApplicationBuilder UseJwtAuth(this IApplicationBuilder app)
        {
            return app.Map("/authCode", context =>
            {
                string code = CreateRandomCode("captcha:length".Config(4)); //验证码的字符为4个
                byte[] bytes = CreateValidateGraphic(code);

                string id = context.GetRemoteMacAddress() ?? context.GetRemoteIpAddress();
                string url = context.GetRefererUrlStrings();

                string md5 = $"{id}-{url}".Md5();

                AuthCode.Set(md5, code, TimeSpan.FromMinutes(2D));

#if NET40
                context.Response.WriteImage(bytes);

            }).Map("/login", context =>
#else
                return Task.Run(() => context.Response.WriteImage(bytes));

            }).Map("/login", async context =>
#endif
            {
                var debug = context.Request.QueryString.Get("debug");

                if (string.IsNullOrEmpty(debug) || !bool.TryParse(debug, out bool isDebug) || !isDebug)
                {
                    var value = context.Request.QueryString.Get("authCode");

                    if (string.IsNullOrEmpty(value))
                    {
                        context.Response.WriteJson(DResult.Error("验证码不能为空!"));
                        return;
                    }

                    string id = context.GetRemoteMacAddress() ?? context.GetRemoteIpAddress();
                    string url = context.GetRefererUrlStrings();

                    string md5 = $"{id}-{url}".Md5();

                    string authCache = AuthCode.Get<string>(md5);

                    if (string.IsNullOrEmpty(authCache))
                    {
                        context.Response.WriteJson(DResult.Error("验证码已过期!"));
                        return;
                    }

                    string authCode = value.ToString();

                    if (authCode.Trim().ToLower() != authCache.Trim().ToLower())
                    {
                        context.Response.WriteJson(DResult.Error("验证码错误!"));
                        return;
                    }
                }

                var loginUrl = "login".Config<string>();

                if (string.IsNullOrEmpty(loginUrl))
                {
                    context.Response.WriteJson(DResult.Error("未配置登录接口!", StatusCodes.ServError));
                    return;
                }

                if (loginUrl.IsUrl() ? !Uri.TryCreate(loginUrl, UriKind.Absolute, out Uri loginUri) : !Uri.TryCreate($"{context.Request.Url.Scheme}://{context.Request.Url.Authority}/{loginUrl.TrimStart('/')}", UriKind.Absolute, out loginUri))
                {
                    context.Response.WriteJson(DResult.Error("不规范的登录接口!", StatusCodes.NonstandardServerError));
                    return;
                }

                var request = loginUri
                    .AsRequestable()
                    .ToQueryString(context.Request.QueryString.ToString());

                string contentType = context.Request.ContentType?.ToLower() ?? "application/json";

                if (contentType.Contains("application/x-www-form-urlencoded"))
                {
                    request.ToForm(context.Request.Form);
                }

                var body = context.Request.GetBufferlessInputStream();

                if (body.Length > 0)
                {
                    using (var reader = new StreamReader(body))
                    {
                        if (contentType.Contains("application/json"))
                        {
                            request.ToJson(reader.ReadToEnd());
                        }
                        else if (contentType.Contains("application/xml"))
                        {
                            request.ToXml(reader.ReadToEnd());
                        }
                        else
                        {
                            context.Response.WriteJson(DResult.Error($"未实现({contentType})类型传输!"));
                            return;
                        }
                    }
                }

                try
                {
#if NET40
                    var result = request
                            .Json<ServResult<Dictionary<string, object>>>()
                            .Request(context.Request.HttpMethod ?? "GET", "map:timeout".Config(10000));
#else
                    var result = await request
                            .Json<ServResult<Dictionary<string, object>>>()
                            .RequestAsync(context.Request.HttpMethod ?? "GET", "map:timeout".Config(10000));
#endif
                    if (result.Success)
                    {
                        if (result.Data is null || result.Data.Count == 0)
                        {
                            context.Response.WriteJson(DResult.Ok());
                        }
                        else
                        {
                            context.Response.WriteJson(DResult.Ok(JwtTokenGen.Create(result.Data)));
                        }
                    }
                    else
                    {
                        context.Response.WriteJson(result);
                    }
                }
                catch (Exception e)
                {
                    context.Response.WriteJson(ExceptionHandler.Handler(e));
                }
            })
#if NET40
            .Map("/register", context =>
#else
            .Map("/register", async context =>
#endif
            {
                var debug = context.Request.QueryString.Get("debug");

                if (string.IsNullOrEmpty(debug) || !bool.TryParse(debug, out bool isDebug) || !isDebug)
                {
                    var value = context.Request.QueryString.Get("authCode");

                    if (string.IsNullOrEmpty(value))
                    {
                        context.Response.WriteJson(DResult.Error("验证码不能为空!"));
                        return;
                    }

                    string id = context.GetRemoteMacAddress() ?? context.GetRemoteIpAddress();
                    string url = context.GetRefererUrlStrings();

                    string md5 = $"{id}-{url}".Md5();

                    string authCache = AuthCode.Get<string>(md5);

                    if (string.IsNullOrEmpty(authCache))
                    {
                        context.Response.WriteJson(DResult.Error("验证码已过期!"));
                        return;
                    }

                    string authCode = value.ToString();

                    if (authCode.Trim().ToLower() != authCache.Trim().ToLower())
                    {
                        context.Response.WriteJson(DResult.Error("验证码错误!"));
                        return;
                    }
                }

                var registerUrl = "register".Config<string>();

                if (string.IsNullOrEmpty(registerUrl))
                {
                    context.Response.WriteJson(DResult.Error("未配置注册接口!", StatusCodes.ServError));
                    return;
                }

                if (registerUrl.IsUrl() ? !Uri.TryCreate(registerUrl, UriKind.Absolute, out Uri registerUri) : !Uri.TryCreate($"{context.Request.Url.Scheme}://{context.Request.Url.Authority}/{registerUrl.TrimStart('/')}", UriKind.Absolute, out registerUri))
                {
                    context.Response.WriteJson(DResult.Error("不规范的注册接口!", StatusCodes.NonstandardServerError));
                    return;
                }

                try
                {
#if NET40
                    var result = registerUri.AsRequestable()
                        .ToQueryString(context.Request.QueryString.ToString())
                        .Json<ServResult<Dictionary<string, object>>>()
                        .Get();
#else
                    var result = await registerUri.AsRequestable()
                        .ToQueryString(context.Request.QueryString.ToString())
                        .Json<ServResult<Dictionary<string, object>>>()
                        .GetAsync();
#endif


                    if (result.Success)
                    {
                        if (result.Data is null || result.Data.Count == 0)
                        {
                            context.Response.WriteJson(DResult.Ok());
                        }
                        else
                        {
                            context.Response.WriteJson(DResult.Ok(JwtTokenGen.Create(result.Data)));
                        }
                    }
                    else
                    {
                        context.Response.WriteJson(result);
                    }
                }
                catch (Exception e)
                {
                    context.Response.WriteJson(ExceptionHandler.Handler(e));
                }
            });
        }
#endif
        #region Private
        /// <summary>
        /// 生成随机的字符串
        /// </summary>
        /// <param name="codeCount">验证码长度</param>
        /// <returns></returns>
        private static string CreateRandomCode(int codeCount)
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
        /// <param name="validateCode">验证码</param>
        /// <returns></returns>
        private static byte[] CreateValidateGraphic(string validateCode)
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
}