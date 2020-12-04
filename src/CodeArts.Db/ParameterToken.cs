using System.Collections.Generic;
using System.Diagnostics;
#if NET40
using System.Collections.ObjectModel;
#endif

namespace CodeArts.Db
{
    /// <summary>
    /// 参数令牌。
    /// </summary>
    [DebuggerDisplay("{Name}")]
    public struct ParameterToken
    {
        /// <summary>
        /// 令牌。
        /// </summary>
        public string Token { get; }

        /// <summary>
        /// 参数名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="token">令牌。</param>
        /// <param name="name">名称。</param>
        public ParameterToken(string token, string name)
        {
            Token = token ?? throw new System.ArgumentNullException(nameof(token));
            Name = name ?? throw new System.ArgumentNullException(nameof(name));
        }
        /// <summary>
        /// 返回 参数名称。
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Name;

#if NET40
        /// <summary>
        /// 空集合。
        /// </summary>
        public static readonly ReadOnlyCollection<ParameterToken> None = new ReadOnlyCollection<ParameterToken>(new List<ParameterToken>());
#else
        /// <summary>
        /// 空集合。
        /// </summary>
        public static readonly IReadOnlyCollection<ParameterToken> None = new List<ParameterToken>();
#endif
    }
}
