using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CodeArts.ORM
{
    /// <summary>
    /// SQL 语句
    /// </summary>
    public interface ISQL
    {
#if NET40
        /// <summary>
        /// 操作的表
        /// </summary>
        ReadOnlyCollection<TableToken> Tables { get; }

        /// <summary>
        /// 参数
        /// </summary>
        ReadOnlyCollection<ParameterToken> Parameters { get; }
#else
        /// <summary>
        /// 操作的表
        /// </summary>
        IReadOnlyCollection<TableToken> Tables { get; }
        /// <summary>
        /// 参数
        /// </summary>
        IReadOnlyCollection<ParameterToken> Parameters { get; }
#endif
        /// <summary>
        /// 转为实际数据库的SQL语句
        /// </summary>
        /// <param name="settings">SQL修正配置</param>
        /// <returns></returns>
        string ToString(ISQLCorrectSimSettings settings);
    }
}
