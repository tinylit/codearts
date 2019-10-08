using System;
using System.Collections.Generic;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 范围
    /// </summary>
    public interface ITableRegions
    {
        /// <summary>
        /// 表实体类型
        /// </summary>
        Type TableType { get; }
        /// <summary>
        /// 数据库表名称
        /// </summary>
        string TableName { get; }
        /// <summary>
        /// 主键
        /// </summary>
        IEnumerable<string> Keys { get; }

        /// <summary>
        /// 只读键
        /// </summary>
        IEnumerable<string> ReadOnlys { get; }

#if NET40
        /// <summary>
        /// 令牌
        /// </summary>
        IDictionary<string, TokenAttribute> Tokens { get; }
        /// <summary>
        /// 可读写的键
        /// </summary>
        IDictionary<string, string> ReadWrites { get; }

        /// <summary>
        /// 属性和字段
        /// </summary>
        IDictionary<string, string> ReadOrWrites { get; }
#else

        /// <summary>
        /// 令牌
        /// </summary>
        IReadOnlyDictionary<string, TokenAttribute> Tokens { get; }

        /// <summary>
        /// 可读写的键
        /// </summary>
        IReadOnlyDictionary<string, string> ReadWrites { get; }

        /// <summary>
        /// 属性和字段
        /// </summary>
        IReadOnlyDictionary<string, string> ReadOrWrites { get; }
#endif
    }
}
