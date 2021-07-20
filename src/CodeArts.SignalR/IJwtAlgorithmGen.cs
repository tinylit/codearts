#if NET40_OR_GREATER
#if NET461_OR_GREATER
using JWT.Algorithms;
#else
using JWT;
#endif

namespace CodeArts.SignalR
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