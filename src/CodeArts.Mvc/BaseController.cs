using CodeArts.Mvc.Filters;
using System.Collections.Generic;
using System;
#if NETSTANDARD2_0 || NETCOREAPP3_1
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
#else
using System.Web.Http;
using JWT;
using JWT.Serializers;
#endif

namespace CodeArts.Mvc
{
    /// <summary>
    /// 控制器基类（数据注解验证）
    /// </summary>
    [ValidateModel]
#if NETSTANDARD2_0 || NETCOREAPP3_1
    public abstract class BaseController : ControllerBase

#else
    
    public abstract class BaseController : ApiController
#endif
    {
#if NET40
        /// <summary>
        /// 成功
        /// </summary>
        /// <returns></returns>
        [NonAction]
        public DResult Ok() => DResult.Ok();

#else
        /// <summary>
        /// 成功
        /// </summary>
        /// <returns></returns>
        [NonAction]
        public new DResult Ok() => DResult.Ok();
#endif
        /// <summary>
        /// 成功
        /// </summary>
        /// <param name="total">总数</param>
        /// <param name="data">数据</param>
        /// <returns></returns>
        [NonAction]
        public DResults<T> Ok<T>(int total, List<T> data) => DResult.Ok(total, data);

        /// <summary>
        /// 失败
        /// </summary>
        /// <param name="errorMsg">错误信息</param>
        /// <param name="statusCode">状态码</param>
        /// <returns></returns>
        [NonAction]
        public DResult Error(string errorMsg, int statusCode = StatusCodes.Error) => DResult.Error(errorMsg, statusCode);
    }

    /// <summary>
    /// 在 JWT 认证的方法中，可以使用用户信息
    /// </summary>
    /// <typeparam name="TUser">用户实体</typeparam>

    public abstract class BaseController<TUser> : BaseController where TUser : class
    {
        private TUser _user;
        /// <summary>
        /// 用户信息
        /// </summary>
        public TUser MyUser
        {
            get
            {
                if (User is null || User.Identity is null || !User.Identity.IsAuthenticated)
                    return null;

                if (_user == null)
                {
#if NETSTANDARD2_0 || NETCOREAPP3_1
                    var authorize = HttpContext.Request.Headers[HeaderNames.Authorization];
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var value = authorize.ToString();
                    var values = value.Split(' ');
                    var token = tokenHandler.ReadJwtToken(values[values.Length - 1]);
                    _user = token.Payload.MapTo<TUser>();
#else
                    var serializer = new JsonNetSerializer();
                    var provider = new UtcDateTimeProvider();
                    var validator = new JwtValidator(serializer, provider);
                    var urlEncoder = new JwtBase64UrlEncoder();
                    var decoder = new JwtDecoder(serializer, validator, urlEncoder);

                    _user = decoder.DecodeToObject<TUser>(Request.Headers.Authorization.Scheme, "jwt-secret".Config(Consts.JwtSecret), false);
#endif
                }

                return _user;
            }
        }
    }
}
