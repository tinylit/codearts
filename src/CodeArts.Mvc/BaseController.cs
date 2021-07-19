using CodeArts.Mvc.Filters;
using System.Collections.Generic;
using System;
using CodeArts.Serialize.Json;
#if NETCOREAPP2_0_OR_GREATER
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
#else
using System.Web.Http;
using JWT;
using JWT.Serializers;
#endif

namespace CodeArts.Mvc
{
    /// <summary>
    /// 控制器基类（数据注解验证）。
    /// </summary>
    [ValidateModel]
#if NETCOREAPP2_0_OR_GREATER
    public abstract class BaseController : ControllerBase

#else

    public abstract class BaseController : ApiController
#endif
    {
#if NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// 成功。
        /// </summary>
        /// <returns></returns>
        [NonAction]
        public new DResult Ok() => DResult.Ok();

        /// <summary>
        /// 成功。
        /// </summary>
        /// <param name="data">数据。</param>
        /// <returns></returns>
        [NonAction]
        public DResult<T> Ok<T>(T data) => DResult.Ok(data);
#elif NET40
        /// <summary>
        /// 成功。
        /// </summary>
        /// <returns></returns>
        [NonAction]
        public DResult Ok() => DResult.Ok();

        /// <summary>
        /// 成功。
        /// </summary>
        /// <param name="data">数据。</param>
        /// <returns></returns>
        [NonAction]
        public DResult<T> Ok<T>(T data) => DResult.Ok(data);
#else
        /// <summary>
        /// 成功。
        /// </summary>
        /// <returns></returns>
        [NonAction]
        public new DResult Ok() => DResult.Ok();

        /// <summary>
        /// 成功。
        /// </summary>
        /// <param name="data">数据。</param>
        /// <returns></returns>
        [NonAction]
        public new DResult<T> Ok<T>(T data) => DResult.Ok(data);
#endif

        /// <summary>
        /// 成功。
        /// </summary>
        /// <param name="data">数据。</param>
        /// <returns></returns>
        [NonAction]
        public DResults<T> Ok<T>(PagedList<T> data) => DResult.Ok(data);

        /// <summary>
        /// 成功。
        /// </summary>
        /// <param name="total">总数。</param>
        /// <param name="data">数据。</param>
        /// <returns></returns>
        [NonAction]
        public DResults<T> Ok<T>(int total, List<T> data) => DResult.Ok(total, data);

        /// <summary>
        /// 失败。
        /// </summary>
        /// <param name="errorMsg">错误信息。</param>
        /// <param name="statusCode">状态码。</param>
        /// <returns></returns>
        [NonAction]
        public DResult Error(string errorMsg, int statusCode = StatusCodes.Error) => DResult.Error(errorMsg, statusCode);

        /// <summary>
        /// 失败。
        /// </summary>
        /// <param name="errorMsg">错误信息。</param>
        /// <param name="statusCode">状态码。</param>
        /// <returns></returns>
        [NonAction]
        public DResult<T> Error<T>(string errorMsg, int statusCode = StatusCodes.Error) => DResult.Error<T>(errorMsg, statusCode);

        /// <summary>
        /// 失败。
        /// </summary>
        /// <param name="errorMsg">错误信息。</param>
        /// <param name="statusCode">状态码。</param>
        /// <returns></returns>
        [NonAction]
        public DResults<T> Errors<T>(string errorMsg, int statusCode = StatusCodes.Error) => DResult.Errors<T>(errorMsg, statusCode);
    }

    /// <summary>
    /// 在 JWT 认证的方法中，可以使用用户信息。
    /// </summary>
    /// <typeparam name="TUser">用户实体。</typeparam>
    public abstract class BaseController<TUser> : BaseController where TUser : class
    {
        private TUser _user;
        /// <summary>
        /// 用户信息。
        /// </summary>
        /// <exception cref="NotSupportedException">未找到当前用户信息。</exception>
        public TUser MyUser
        {
            get
            {
                if (User is null || User.Identity is null || !User.Identity.IsAuthenticated)
                {
                    throw new NotSupportedException();
                }

                if (_user is null)
                {
                    _user = GetUser() ?? throw new NotSupportedException();
                }

                return _user;
            }
        }

        /// <summary>
        /// 获取当前登录用户信息。
        /// </summary>
        /// <returns></returns>
        [NonAction]
        protected virtual TUser GetUser()
        {
#if NETCOREAPP2_0_OR_GREATER
            var authorize = HttpContext.Request.Headers[HeaderNames.Authorization];
            var tokenHandler = new JwtSecurityTokenHandler();
            var value = authorize.ToString();
            var values = value.Split(' ');

#if NETCOREAPP3_1
            var token = values[^1];
#else
            var token = values[values.Length - 1];
#endif

            var payload = token.Split('.')[1];

            return JsonHelper.Json<TUser>(Base64UrlEncoder.Decode(payload));
#else
            var serializer = new JsonNetSerializer();
            var provider = new UtcDateTimeProvider();
            var validator = new JwtValidator(serializer, provider);
            var urlEncoder = new JwtBase64UrlEncoder();
#if NET461_OR_GREATER
            var decoder = new JwtDecoder(serializer, validator, urlEncoder, JwtAlgorithmGen.Create());
#else
            var decoder = new JwtDecoder(serializer, validator, urlEncoder);
#endif

            return decoder.DecodeToObject<TUser>(Request.Headers.Authorization.Scheme, "jwt-secret".Config(Consts.JwtSecret), false);
#endif
        }
    }

    /// <summary>
    /// 在 JWT 认证的方法中，可以使用用户信息。
    /// </summary>
    /// <typeparam name="TUser">用户信息。</typeparam>
    /// <typeparam name="TUserData">用户数据。</typeparam>
    public abstract class BaseController<TUser, TUserData> : BaseController<TUser> where TUser : class where TUserData : class
    {
        private TUserData _user;

        /// <summary>
        /// 用户数据。
        /// </summary>
        /// <exception cref="NotSupportedException">未找到当前用户信息。</exception>
        public TUserData MyData
        {
            get
            {
                if (_user is null)
                {
                    _user = GetUserData(MyUser) ?? throw new NotSupportedException();
                }

                return _user;
            }
        }

        /// <summary>
        /// 获取用户信息。
        /// </summary>
        /// <param name="user">简易用户信息。</param>
        /// <returns></returns>
        [NonAction]
        protected abstract TUserData GetUserData(TUser user);
    }
}
