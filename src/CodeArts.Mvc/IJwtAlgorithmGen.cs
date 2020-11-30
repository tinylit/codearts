#if NET40 || NET_NORMAL
#if NET461
using JWT.Algorithms;
#else
using JWT;
#endif

namespace CodeArts.Mvc
{
    /// <summary>
    /// JWT 算法工厂。
    /// </summary>
    public interface IJwtAlgorithmGen
    {
        /// <summary>
        /// 创建 JWT 算法。
        /// </summary>
        /// <returns></returns>
        IJwtAlgorithm Create();
    }
}
#endif