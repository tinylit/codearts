namespace CodeArts
{
    /// <summary>
    /// 全局常量
    /// </summary>
    public static class Consts
    {
#if NETSTANDARD2_0 || NETSTANDARD2_1

        /// <summary>
        /// 映射接口响应超时时间（推荐配置：map:timeout）， 单位：毫秒。
        /// </summary>
        public const int MapTimeout = 10000;

        /// <summary>
        /// 验证码长度（推荐配置：captcha:length）
        /// </summary>
        public const int CaptchaLength = 4;

        /// <summary>
        /// 验证码过期时间（推荐配置：captcha:timeout），单位：秒。
        /// </summary>
        public const double CaptchaTimeout = 120D;

        /// <summary>
        /// 密钥(推荐配置：jwt:secret)
        /// </summary>
        public const string JwtSecret = "Sky_1024~#$`@%^2048";

        /// <summary>
        /// Jwt Audience（推荐配置: jwt:audience）
        /// </summary>
        public const string JwtAudience = "api";

        /// <summary>
        /// Jwt Issuer(推荐配置: jwt:issuer）
        /// </summary>
        public const string JwtIssuer = "yep";

        /// <summary>
        /// SwaggerUI 版本 (推荐配置：swagger:version)
        /// </summary>
        public const string SwaggerVersion = "v1";

        /// <summary>
        /// SwaggerUI 标题 (推荐配置：swagger:title)
        /// </summary>
        public const string SwaggerTitle = "API接口文档";

#else

        /// <summary>
        /// 映射接口响应超时时间（推荐配置：map-timeout）， 单位：毫秒。
        /// </summary>
        public const int MapTimeout = 10000;

        /// <summary>
        /// 验证码长度（推荐配置：captcha-length）。
        /// </summary>
        public const int CaptchaLength = 4;

        /// <summary>
        /// 验证码过期时间（推荐配置：captcha-timeout），单位：秒。
        /// </summary>
        public const double CaptchaTimeout = 120D;

        /// <summary>
        /// 密钥(推荐配置：jwt-secret)。
        /// </summary>
        public const string JwtSecret = "Sky_1024~#$`@%^2048";

        /// <summary>
        /// SwaggerUI 版本 (推荐配置：swagger-version)。
        /// </summary>
        public const string SwaggerVersion = "v1";

        /// <summary>
        /// SwaggerUI 标题 (推荐配置：swagger-title)。
        /// </summary>
        public const string SwaggerTitle = "API接口文档";
#endif

        /// <summary>
        /// 日期格式化。
        /// </summary>
        public const string DateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK";
    }
}
