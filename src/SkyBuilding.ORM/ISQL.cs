using System.Collections.ObjectModel;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// SQL 语句
    /// </summary>
    public interface ISQL
    {
        /// <summary>
        /// 操作的表
        /// </summary>
        ReadOnlyCollection<SQLToken> Tables { get; }

        /// <summary>
        /// 参数
        /// </summary>
        ReadOnlyCollection<SQLToken> Parameters { get; }

        /// <summary>
        /// 转为实际数据库的SQL语句
        /// </summary>
        /// <param name="settings">SQL修正配置</param>
        /// <returns></returns>
        string ToString(ISQLCorrectSimSettings settings);
    }
}
