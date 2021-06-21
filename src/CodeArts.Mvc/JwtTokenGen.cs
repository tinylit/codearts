#if NETCOREAPP2_0_OR_GREATER
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
#else
using JWT;
using JWT.Serializers;
#endif
using System;
using System.Collections.Generic;

namespace CodeArts.Mvc
{
    /// <summary>
    /// JWT令牌生成器。
    /// </summary>
    public static class JwtTokenGen
    {
#if NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// 生成令牌。
        /// </summary>
        /// <param name="user">用户信息字典。</param>
        /// <param name="expires">令牌有效期（单位：小时）。</param>
        /// <returns></returns>
        public static JwtToken Create(Dictionary<string, object> user, double expires = 12D) => Create(user.AsIdentity(), expires);

        /// <summary>
        /// 生成令牌。
        /// </summary>
        /// <param name="user">用户信息。</param>
        /// <param name="expires">令牌有效期（单位：小时）。</param>
        /// <returns></returns>
        public static JwtToken Create(ClaimsIdentity user, double expires = 12D)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes("jwt:secret".Config(Consts.JwtSecret));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = user,
                Expires = DateTime.UtcNow.AddHours(expires),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
            return new JwtToken
            {
                Type = "Bearer",
                Token = token
            };
        }
#else
        /// <summary>
        /// 生成令牌。
        /// </summary>
        /// <param name="user">用户信息。</param>
        /// <param name="expires">过期时间（单位：小时）。</param>
        /// <returns></returns>
        public static string Create(Dictionary<string, object> user, double expires = 12D)
        {
            var algorithm = JwtAlgorithmGen.Create();
            var serializer = new JsonNetSerializer();
            var urlEncoder = new JwtBase64UrlEncoder();
            var encoder = new JwtEncoder(algorithm, serializer, urlEncoder);

            if (!user.ContainsKey("exp"))
            {
                user.Add("exp", (DateTime.UtcNow.AddHours(expires) - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
            }

            return encoder.Encode(user, "jwt-secret".Config(Consts.JwtSecret));
        }
#endif
    }
}
