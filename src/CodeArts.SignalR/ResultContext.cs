#if NET45 || NET451 || NET452 ||NET461
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Owin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;

namespace CodeArts.SignalR
{
    /// <summary>
    /// 结果上下文
    /// </summary>
    public class ResultContext : IOwinContext
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">上下文</param>
        public ResultContext(IOwinContext context)
        {
            Context = context;
        }

        /// <summary>
        /// 上下文
        /// </summary>
        public IOwinContext Context { get; }

        /// <summary>
        ///认证
        /// </summary>
        public IAuthenticationManager Authentication => Context.Authentication;

        /// <summary>
        /// 请求
        /// </summary>
        public IOwinRequest Request => Context.Request;

        /// <summary>
        /// 响应
        /// </summary>
        public IOwinResponse Response => Context.Response;

        /// <summary>
        /// 环境变量
        /// </summary>
        public IDictionary<string, object> Environment => Context.Environment;

        /// <summary>
        /// 输入输出
        /// </summary>
        TextWriter IOwinContext.TraceOutput { get => Context.TraceOutput; set => Context.TraceOutput = value; }

        /// <summary>
        /// 获取环境变量的值
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">键</param>
        /// <returns></returns>
        public T Get<T>(string key) => Context.Get<T>(key);

        /// <summary>
        /// 设置环境变量
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public IOwinContext Set<T>(string key, T value) => Context.Set<T>(key, value);
    }

    /// <summary>
    /// 消息接收上下文
    /// </summary>
    public class MessageReceivedContext : ResultContext
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">上下文</param>
        public MessageReceivedContext(IOwinContext context) : base(context)
        {
        }

        /// <summary>
        /// 令牌
        /// </summary>
        public string Token { get; set; }
    }

    /// <summary>
    /// 令牌验证
    /// </summary>
    public class TokenValidateContext : ResultContext
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">上下文</param>
        public TokenValidateContext(MessageReceivedContext context) : base(context) => Token = context.Token;

        /// <summary>
        /// 令牌
        /// </summary>
        public string Token { get; }

        /// <summary>
        /// 令牌过期校验（默认：false）
        /// </summary>
        public bool TokenVerify { get; set; }

        private IDictionary<string, object> userData;

        /// <summary>
        /// 用户数据
        /// </summary>
        public IDictionary<string, object> UserData
        {
            get
            {
                if (userData is null)
                {
                    var serializer = new JsonNetSerializer();
                    var provider = new UtcDateTimeProvider();
                    var validator = new JwtValidator(serializer, provider);
                    var urlEncoder = new JwtBase64UrlEncoder();
#if NET461
                    var decoder = new JwtDecoder(serializer, validator, urlEncoder, new HMACSHA256Algorithm());
#else
                    var decoder = new JwtDecoder(serializer, validator, urlEncoder);
#endif

                    try
                    {
                        userData = decoder.DecodeToObject(Token, "jwt-secret".Config(Consts.JwtSecret), TokenVerify);
                    }
                    catch { }
                }

                return userData;
            }
            set => userData = value;
        }
    }

    /// <summary>
    /// Token 验证通过
    /// </summary>
    public class TokenValidatedContext : ResultContext
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">上下文</param>
        public TokenValidatedContext(TokenValidateContext context) : base(context) => UserData = context.UserData;

        /// <summary>
        /// 用户数据
        /// </summary>
        public IDictionary<string, object> UserData { get; }

        private ClaimsPrincipal user = null;

        /// <summary>
        /// 用户信息
        /// </summary>
        public ClaimsPrincipal User
        {
            get
            {
                if (user is null)
                {
                    user = new ClaimsPrincipal(JwtExtensions.AsIdentity(UserData));
                }
                return user;
            }
            set => user = value;
        }
    }
}
#endif