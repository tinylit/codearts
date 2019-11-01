using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 参数令牌
    /// </summary>
    [DebuggerDisplay("{Name}")]
    public struct SQLToken
    {
        /// <summary>
        /// 令牌
        /// </summary>
        public string Token { get; }

        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="token">令牌</param>
        /// <param name="name">名称</param>
        public SQLToken(string token, string name)
        {
            Token = token ?? throw new System.ArgumentNullException(nameof(token));
            Name = name ?? throw new System.ArgumentNullException(nameof(name));
        }

        public override string ToString() => Name;

        public readonly static ReadOnlyCollection<SQLToken> None = new ReadOnlyCollection<SQLToken>(new List<SQLToken>());
    }
}
