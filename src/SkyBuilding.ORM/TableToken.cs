using System;
using System.Collections.Generic;
#if NET40
using System.Collections.ObjectModel;
#endif
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 参数令牌
    /// </summary>
    [DebuggerDisplay("{Name}")]
    public struct TableToken
    {
        private readonly static Regex Pattern = new Regex("^[a-zA-Z]+$", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// 令牌
        /// </summary>
        public string Token { get; }

        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 命令
        /// </summary>
        public UppercaseString CommandType { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="token">令牌</param>
        /// <param name="commandType">命令（仅字母构成）</param>
        /// <param name="name">名称</param>
        /// <exception cref="SyntaxErrorException"> commandType 包含非字母的符号。</exception>
        public TableToken(string token, string commandType, string name)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
            Name = name ?? throw new ArgumentNullException(nameof(name));

            if (!Pattern.IsMatch(commandType ?? throw new ArgumentNullException(nameof(commandType))))
                throw new SyntaxErrorException();

            CommandType = new UppercaseString(commandType);
        }

        public override string ToString() => Name;

#if NET40

        public readonly static ReadOnlyCollection<TableToken> None = new ReadOnlyCollection<TableToken>(new List<TableToken>());
#else
        public readonly static IReadOnlyCollection<TableToken> None = new List<TableToken>();
#endif
    }
}
