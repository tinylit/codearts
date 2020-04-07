#if NETSTANDARD2_0 || NETCOREAPP3_1
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using CodeArts;
using CodeArts.Mvc;
#endif
#if !NET40
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
using System.Web;

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
        /// 通过“/authcode”获取验证码；通过“/authcode{pathString}”会自动调用当前项目的“{pathString}”(<see cref="PathString"/>)接口，并将随机验证码作为【authCode】参数传递给接口，接口可选择是否返回新的验证码。
        /// 请在配置文件中配置“login”项(可以是相对地址或绝对地址)。
        /// 添加请求参数“debug”为真时，不进行验证码验证。
        /// 自定义请参考<see cref="Consts"/>。
        /// </summary>
        /// <param name="app">配置</param>
        /// <param name="basePath">登录、注册、验证码的基础路径。</param>
        /// <returns></returns>
#if NETSTANDARD2_0 || NETCOREAPP3_1
        public static IApplicationBuilder UseJwtAuth(this IApplicationBuilder app, PathString basePath)
        {
            return app.Map(basePath.Add("/authCode"), builder => builder.Run(async context =>
            {
                string code = CreateRandomCode("captcha:length".Config(4)); //验证码的字符为4个

                string id = context.GetRemoteMacAddress() ?? context.GetRemoteIpAddress();
                string url = context.GetRefererUrlStrings();

                string md5 = $"{id}-{url}".Md5();

                if (context.Request.Path.HasValue)
                {
                    var result = await $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path.Value}".AsRequestable()
                      .ToQueryString(context.Request.QueryString.ToString())
                      .ToQueryString($"authCode={code}")
                      .Json<ServResult<string>>()
                      .Catch(e =>
                      {
                          if (e.Response is HttpWebResponse response)
                          {
                              var statusCode = (int)response.StatusCode;

                              return ServResult.Error<string>(statusCode.Message(), statusCode);
                          }

                          return ServResult.Error<string>(e.Message, CodeArts.StatusCodes.NotFound);
                      })
                      .GetAsync();

                    if (result.Success)
                    {
                        AuthCode.Set(md5, result.Data ?? code, TimeSpan.FromMinutes(2D));

                        await context.Response.WriteJsonAsync(DResult.Ok());
                    }
                    else
                    {
                        await context.Response.WriteJsonAsync(DResult.Error(result.Msg, result.Code));
                    }
                }
                else
                {
                    byte[] bytes = CreateValidateGraphic(code);

                    AuthCode.Set(md5, code, TimeSpan.FromMinutes(2D));

                    await context.Response.WriteImageAsync(bytes);
                }

            })).Map(basePath.Add("/login"), builder => builder.Run(async context =>
            {
                var result = VerifyAuthCode(context);

                if (result.Success)
                {
                    var loginUrl = "login".Config<string>();

                    if (string.IsNullOrEmpty(loginUrl))
                    {
                        await context.Response.WriteJsonAsync(DResult.Error("未配置登录接口!", CodeArts.StatusCodes.ServError));
                    }
                    else if (loginUrl.IsUrl() ? !Uri.TryCreate(loginUrl, UriKind.Absolute, out Uri loginUri) : !Uri.TryCreate($"{context.Request.Scheme}://{context.Request.Host}/{loginUrl.TrimStart('/')}", UriKind.Absolute, out loginUri))
                    {
                        await context.Response.WriteJsonAsync(DResult.Error("不规范的登录接口!", CodeArts.StatusCodes.NonstandardServerError));
                    }
                    else
                    {
                        await context.Response.WriteJsonAsync(await RequestAsync(loginUri, context));
                    }
                }
                else
                {
                    await context.Response.WriteJsonAsync(result);
                }

            })).Map(basePath.Add("/register"), builder => builder.Run(async context =>
            {
                var result = VerifyAuthCode(context);

                if (result.Success)
                {
                    var registerUrl = "register".Config<string>();

                    if (string.IsNullOrEmpty(registerUrl))
                    {
                        await context.Response.WriteJsonAsync(DResult.Error("未配置注册接口!", CodeArts.StatusCodes.ServError));
                    }
                    else if (registerUrl.IsUrl() ? !Uri.TryCreate(registerUrl, UriKind.Absolute, out Uri registerUri) : !Uri.TryCreate($"{context.Request.Scheme}://{context.Request.Host}/{registerUrl.TrimStart('/')}", UriKind.Absolute, out registerUri))
                    {
                        await context.Response.WriteJsonAsync(DResult.Error("不规范的注册接口!", CodeArts.StatusCodes.NonstandardServerError));
                    }
                    else
                    {
                        await context.Response.WriteJsonAsync(await RequestAsync(registerUri, context));
                    }
                }
                else
                {
                    await context.Response.WriteJsonAsync(result);
                }
            }));
        }
#else
        public static IApplicationBuilder UseJwtAuth(this IApplicationBuilder app, PathString basePath)
        {
            PathString authCode = basePath.Add("/authCode");
#if NET40
            return app.Map(authCode, context =>
#else
            return app.Map(authCode, async context =>
#endif
            {
                string code = CreateRandomCode("captcha:length".Config(4)); //验证码的字符为4个
                string id = context.GetRemoteMacAddress() ?? context.GetRemoteIpAddress();
                string url = context.GetRefererUrlStrings();

                string md5 = $"{id}-{url}".Md5();

                PathString absolutePath = new PathString(context.Request.Url.AbsolutePath);
                if (absolutePath.StartsWithSegments(authCode, StringComparison.OrdinalIgnoreCase, out PathString path) && path.HasValue)
                {
                    string api = $"{context.Request.Url.Scheme}://{context.Request.Url.Authority}{path.Value}";
#if NET40
                    var result = api.AsRequestable()
#else
                    var result = await api.AsRequestable()
#endif
                        .ToQueryString(context.Request.QueryString.ToString())
                        .ToQueryString($"authCode={code}")
                        .Json<ServResult<string>>()
                        .Catch(e =>
                        {
                            if (e.Response is HttpWebResponse response)
                            {
                                var statusCode = (int)response.StatusCode;

                                return ServResult.Error<string>(statusCode.Message(), statusCode);
                            }

                            return ServResult.Error<string>(e.Message, CodeArts.StatusCodes.NotFound);
                        })
#if NET40
                        .Get();
#else
                        .GetAsync();
#endif

                    if (result.Success)
                    {
                        AuthCode.Set(md5, result.Data ?? code, TimeSpan.FromMinutes(2D));

                        context.Response.WriteJson(DResult.Ok());
                    }
                    else
                    {
                        context.Response.WriteJson(DResult.Error(result.Msg, result.Code));
                    }
                }
                else
                {
                    byte[] bytes = CreateValidateGraphic(code);

                    AuthCode.Set(md5, code, TimeSpan.FromMinutes(2D));

                    context.Response.WriteImage(bytes);
                }
#if NET40
            }).Map(basePath.Add("/login"), context =>
#else
            }).Map(basePath.Add("/login"), async context =>
#endif
            {
                var result = VerifyAuthCode(context);

                if (result.Success)
                {
                    var loginUrl = "login".Config<string>();

                    if (string.IsNullOrEmpty(loginUrl))
                    {
                        context.Response.WriteJson(DResult.Error("未配置登录接口!", StatusCodes.ServError));
                    }
                    else if (loginUrl.IsUrl() ? !Uri.TryCreate(loginUrl, UriKind.Absolute, out Uri loginUri) : !Uri.TryCreate($"{context.Request.Url.Scheme}://{context.Request.Url.Authority}/{loginUrl.TrimStart('/')}", UriKind.Absolute, out loginUri))
                    {
                        context.Response.WriteJson(DResult.Error("不规范的登录接口!", StatusCodes.NonstandardServerError));
                    }
                    else
                    {
#if NET40
                        context.Response.WriteJson(Request(loginUri, context));
#else
                        context.Response.WriteJson(await RequestAsync(loginUri, context));
#endif
                    }
                }
            })
#if NET40
            .Map(basePath.Add("/register"), context =>
#else
            .Map(basePath.Add("/register"), async context =>
#endif
            {
                var result = VerifyAuthCode(context);

                if (result.Success)
                {
                    var registerUrl = "register".Config<string>();

                    if (string.IsNullOrEmpty(registerUrl))
                    {
                        context.Response.WriteJson(DResult.Error("未配置注册接口!", StatusCodes.ServError));
                    }
                    else if (registerUrl.IsUrl() ? !Uri.TryCreate(registerUrl, UriKind.Absolute, out Uri registerUri) : !Uri.TryCreate($"{context.Request.Url.Scheme}://{context.Request.Url.Authority}/{registerUrl.TrimStart('/')}", UriKind.Absolute, out registerUri))
                    {
                        context.Response.WriteJson(DResult.Error("不规范的注册接口!", StatusCodes.NonstandardServerError));
                    }
                    else
                    {
#if NET40
                        context.Response.WriteJson(Request(registerUri, context));
#else
                        context.Response.WriteJson(await RequestAsync(registerUri, context));
#endif
                    }
                }
                else
                {
                    context.Response.WriteJson(result);
                }
            });
        }
#endif
        #region Private

        /// <summary>
        /// 请求
        /// </summary>
        /// <param name="uri">请求地址</param>
        /// <param name="context">当前上下文</param>
        /// <returns></returns>
#if NET40
        private static IResult Request(Uri uri, HttpContext context)
#else
        private static async Task<IResult> RequestAsync(Uri uri, HttpContext context)
#endif
        {
            var request = uri
                .AsRequestable()
                .ToQueryString(context.Request.QueryString.ToString());

            string contentType = context.Request.ContentType?.ToLower() ?? string.Empty;

            if (contentType.Contains("application/x-www-form-urlencoded"))
            {
                request.ToForm(context.Request.Form);
            }
            else if (contentType.Contains("application/json") || contentType.Contains("application/xml"))
            {
#if NETSTANDARD2_0 || NETCOREAPP3_1
                if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > 0)
                {
                    var length = context.Request.ContentLength.Value;
                    var buffer = new byte[length];
                    await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);

                    var body = Encoding.UTF8.GetString(buffer);

                    if (contentType.Contains("application/json"))
                    {
                        request.ToJson(body);
                    }
                    else
                    {
                        request.ToXml(body);
                    }
                }
#else
                if (context.Request.InputStream.Length > 0)
                {
                    using (var reader = new StreamReader(context.Request.InputStream))
                    {
                        if (contentType.Contains("application/json"))
                        {
                            request.ToJson(reader.ReadToEnd());
                        }
                        else
                        {
                            request.ToXml(reader.ReadToEnd());
                        }
                    }
                }
#endif
            }
            else if (!string.IsNullOrEmpty(contentType))
            {
                return DResult.Error($"未实现({contentType})类型传输!");
            }

            try
            {

#if NETSTANDARD2_0 || NETCOREAPP3_1
                var result = await request
                        .Json<ServResult<Dictionary<string, object>>>()
                        .RequestAsync(context.Request.Method ?? "GET", "map:timeout".Config(10000));
#elif NET40
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
                        return DResult.Ok();
                    }

                    return DResult.Ok(JwtTokenGen.Create(result.Data));
                }

                return result;
            }
            catch (Exception e)
            {
                return ExceptionHandler.Handler(e);
            }
        }

        /// <summary>
        /// 验证验证码
        /// </summary>
        /// <param name="context">请求上下文</param>
        /// <returns></returns>
        private static DResult VerifyAuthCode(HttpContext context)
        {
#if NETSTANDARD2_0 || NETCOREAPP3_1
            if (!context.Request.Query.TryGetValue("debug", out StringValues debug) || !bool.TryParse(debug, out bool isDebug) || !isDebug)
            {
                if (!context.Request.Query.TryGetValue("authCode", out StringValues value) || value == StringValues.Empty)
                {
                    return DResult.Error("验证码不能为空!");
                }
#else
            var debug = context.Request.QueryString.Get("debug");

            if (string.IsNullOrEmpty(debug) || !bool.TryParse(debug, out bool isDebug) || !isDebug)
            {
                var value = context.Request.QueryString.Get("authCode");
                if (string.IsNullOrEmpty(value))
                {
                    return DResult.Error("验证码不能为空!");
                }
#endif
                string id = context.GetRemoteMacAddress() ?? context.GetRemoteIpAddress();
                string url = context.GetRefererUrlStrings();

                string md5 = $"{id}-{url}".Md5();

                string authCache = AuthCode.Get<string>(md5);

                if (string.IsNullOrEmpty(authCache))
                {
                    return DResult.Error("验证码已过期!");
                }

                string authCode = value.ToString();

                if (authCode.Trim().ToLower() != authCache.Trim().ToLower())
                {
                    return DResult.Error("验证码错误!");
                }

                AuthCode.Remove(md5);
            }

            return DResult.Ok();
        }

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