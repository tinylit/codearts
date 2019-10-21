using SkyBuilding.Mvc.Filters;
using System.Collections.Generic;
using System;
#if NETSTANDARD2_0 || NETCOREAPP3_0
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
#else
using System.Web.Http;
using JWT;
using JWT.Serializers;
#endif

namespace SkyBuilding.Mvc
{
    [ValidateModel]
#if NETSTANDARD2_0 || NETCOREAPP3_0
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
        public DResult Error(string errorMsg, int statusCode = 500) => DResult.Error(errorMsg, statusCode);
    }

    /// <summary>
    /// 在 JWT 认证的方法中，可以使用用户信息
    /// </summary>
    /// <typeparam name="TUser">用户实体</typeparam>

    public abstract class BaseController<TUser> : BaseController where TUser : class
    {
        private TUser _profile;
        /// <summary>
        /// 用户信息
        /// </summary>
        public TUser Profile
        {
            get
            {
                if (User is null || User.Identity is null || !User.Identity.IsAuthenticated)
                    return null;

                if (_profile == null)
                {
#if NETSTANDARD2_0 || NETCOREAPP3_0
                    var authorize = HttpContext.Request.Headers[HeaderNames.Authorization];
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var token = tokenHandler.ReadJwtToken(authorize.ToString());
                    _profile = token.Payload.MapTo<TUser>();
#else
                    var serializer = new JsonNetSerializer();
                    var provider = new UtcDateTimeProvider();
                    var validator = new JwtValidator(serializer, provider);
                    var urlEncoder = new JwtBase64UrlEncoder();
                    var decoder = new JwtDecoder(serializer, validator, urlEncoder);

                    _profile = decoder.DecodeToObject<TUser>(Request.Headers.Authorization.Scheme, "jwt-secret".Config(Consts.Secret), false);
#endif
                }

                return _profile;
            }
        }
    }
}
