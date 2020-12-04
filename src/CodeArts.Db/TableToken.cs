using System;
using System.Collections.Generic;
#if NET40
using System.Collections.ObjectModel;
#endif
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CodeArts.Db
{
    /// <summary>
    /// 参数令牌。
    /// </summary>
    [DebuggerDisplay("{Name}")]
    public struct TableToken : IEquatable<TableToken>
    {
        private static readonly Regex Pattern = new Regex("^[a-zA-Z]+$", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// 令牌。
        /// </summary>
        public string Token { get; }

        /// <summary>
        /// 参数名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 命令。
        /// </summary>
        public UppercaseString CommandType { get; }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="token">令牌。</param>
        /// <param name="commandType">命令（仅字母构成）。</param>
        /// <param name="name">名称。</param>
        /// <exception cref="SyntaxErrorException"> commandType 包含非字母的符号。</exception>
        public TableToken(string token, string commandType, string name)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
            Name = name ?? throw new ArgumentNullException(nameof(name));

            if (!Pattern.IsMatch(commandType ?? throw new ArgumentNullException(nameof(commandType))))
                throw new SyntaxErrorException();

            CommandType = new UppercaseString(commandType);
        }
        /// <summary>
        /// 返回表名称。
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Name;

        /// <summary>
        /// 比较。
        /// </summary>
        /// <param name="other">其它。</param>
        /// <returns></returns>
        public bool Equals(TableToken other) => Token == other.Token && Name == other.Name && CommandType == other.CommandType;

#if NET40
        /// <summary>
        /// 空集合。
        /// </summary>
        public static readonly ReadOnlyCollection<TableToken> None = new ReadOnlyCollection<TableToken>(new List<TableToken>());
#else
        /// <summary>
        /// 空集合。
        /// </summary>
        public static readonly IReadOnlyCollection<TableToken> None = new List<TableToken>();
#endif
    }
}
