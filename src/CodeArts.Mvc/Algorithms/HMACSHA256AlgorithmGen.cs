#if NET40_OR_GREATER
#if !NET461_OR_GREATER
using JWT;
#endif
using JWT.Algorithms;

namespace CodeArts.Mvc.Algorithms
{
    /// <summary>
    /// HMACSHA256 加密。
    /// </summary>
    public class HMACSHA256AlgorithmGen : IJwtAlgorithmGen
    {
        /// <summary>
        /// 创建 JWT 算法。
        /// </summary>
        /// <returns></returns>
        public IJwtAlgorithm Create() => new HMACSHA256Algorithm();
    }
}
#endif