#if NET_CORE
namespace CodeArts.Mvc
{
    /// <summary>
    /// JWT令牌。
    /// </summary>
    public class JwtToken
    {
        /// <summary>
        /// 类型。
        /// </summary>
        public string Type { get; set; } = "Bearer";
        /// <summary>
        /// 令牌。
        /// </summary>
        public string Token { get; set; }
    }
}
#endif