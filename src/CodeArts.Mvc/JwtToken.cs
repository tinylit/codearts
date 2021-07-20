#if NETCOREAPP2_0_OR_GREATER
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