﻿#if NET45 || NET451 || NET452 ||NET461
using JWT;
using JWT.Serializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Web;

namespace SkyBuilding.Mvc.Authentication
{
    /// <summary>
    /// 结果上下文
    /// </summary>
    public class ResultContext
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context"></param>
        public ResultContext(HttpContext context)
        {
            Context = context;
        }

        /// <summary>
        /// 上下文
        /// </summary>
        public HttpContext Context { get; }


        /// <summary>
        /// 请求
        /// </summary>
        public HttpRequest Request => Context.Request;

        /// <summary>
        /// 响应
        /// </summary>
        public HttpResponse Response => Context.Response;
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
        public MessageReceivedContext(HttpContext context) : base(context)
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
        public TokenValidateContext(MessageReceivedContext context) : base(context.Context) => Token = context.Token;

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
                    var decoder = new JwtDecoder(serializer, validator, urlEncoder);

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
        public TokenValidatedContext(TokenValidateContext context) : base(context.Context) => UserData = context.UserData;

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
                    user = new ClaimsPrincipal(UserData.AsIdentity());
                }
                return user;
            }
            set => user = value;
        }
    }
}
#endif