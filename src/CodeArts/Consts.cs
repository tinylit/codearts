namespace CodeArts
{
    /// <summary>
    /// 全局常量。
    /// </summary>
    public static class Consts
    {
#if NETSTANDARD2_0_OR_GREATER

        /// <summary>
        /// 映射接口响应超时时间（推荐配置：map:timeout）， 单位：毫秒。
        /// </summary>
        public const int MapTimeout = 10000;

        /// <summary>
        /// 密钥(推荐配置：jwt:secret)
        /// </summary>
        public const string JwtSecret = "Ai_1024~#$`@%^2048";

        /// <summary>
        /// Jwt Audience（推荐配置: jwt:audience）
        /// </summary>
        public const string JwtAudience = "api";

        /// <summary>
        /// Jwt Issuer(推荐配置: jwt:issuer）
        /// </summary>
        public const string JwtIssuer = "ai";

        /// <summary>
        /// SwaggerUI 版本 (推荐配置：swagger:version)
        /// </summary>
        public const string SwaggerVersion = "v1";

        /// <summary>
        /// SwaggerUI 标题 (推荐配置：swagger:title)
        /// </summary>
        public const string SwaggerTitle = "API接口文档";

        /// <summary>
        /// Swagger Description（推荐:swagger:description）。
        /// </summary>
        public const string SwaggerDescription = "请输入【Bearer {token}】，注意中间有一个空格!";

#else

        /// <summary>
        /// 映射接口响应超时时间（推荐配置：map-timeout）， 单位：毫秒。
        /// </summary>
        public const int MapTimeout = 10000;

        /// <summary>
        /// 密钥(推荐配置：jwt-secret)。
        /// </summary>
        public const string JwtSecret = "Ai_1024~#$`@%^2048";

        /// <summary>
        /// SwaggerUI 版本 (推荐配置：swagger-version)。
        /// </summary>
        public const string SwaggerVersion = "v1";

        /// <summary>
        /// SwaggerUI 标题 (推荐配置：swagger-title)。
        /// </summary>
        public const string SwaggerTitle = "API接口文档";

        /// <summary>
        /// Swagger Description（推荐:swagger-description）。
        /// </summary>
        public const string SwaggerDescription = "API Key Authentication";
#endif

        /// <summary>
        /// 日期格式化。
        /// </summary>
        public const string DateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK";
    }
}
