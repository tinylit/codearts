#if NET40 || NET45 || NET451 || NET452 || NET461
using CodeArts.SignalR.Algorithms;
#if NET461
using JWT.Algorithms;
#else
using JWT;
#endif

namespace CodeArts.SignalR
{
    /// <summary>
    /// JWT 算法工厂。
    /// </summary>
    public static class JwtAlgorithmGen
    {
        private static readonly IJwtAlgorithmGen _algorithmGen;
        static JwtAlgorithmGen() => _algorithmGen = RuntimeServPools.Singleton<IJwtAlgorithmGen, HMACSHA256AlgorithmGen>();

        /// <summary>
        /// 创建 JWT 算法。
        /// </summary>
        /// <returns></returns>
        public static IJwtAlgorithm Create() => _algorithmGen.Create();
    }
}
#endif