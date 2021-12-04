#if NET40
using System.Collections.Generic;
using System.Collections.ObjectModel;
#else
using System.Collections.Generic;
#endif

namespace CodeArts.Db
{
    /// <summary>
    /// SQL 适配器。
    /// </summary>
    public interface ISqlAdpter
    {
        /// <summary>
        /// SQL 分析。
        /// </summary>
        /// <param name="sql">语句。</param>
        /// <returns></returns>
        string Analyze(string sql);

        /// <summary>
        /// SQL 分析（表名称）。
        /// </summary>
        /// <param name="sql">来源于【<see cref="Analyze(string)"/>】的结果。</param>
        /// <returns></returns>
        IReadOnlyList<TableToken> AnalyzeTables(string sql);

        /// <summary>
        /// SQL 分析（参数）。
        /// </summary>
        /// <param name="sql">来源于【<see cref="Analyze(string)"/>】的结果。</param>
        /// <returns></returns>
        IReadOnlyList<string> AnalyzeParameters(string sql);

        /// <summary>
        /// 获取符合条件的条数。
        /// </summary>
        /// <param name="sql">SQL</param>
        /// <example>SELECT * FROM Users WHERE Id > 100 => SELECT COUNT(1) FROM Users WHERE Id > 100</example>
        /// <example>SELECT * FROM Users WHERE Id > 100 ORDER BY Id DESC => SELECT COUNT(1) FROM Users WHERE Id > 100</example>
        /// <returns></returns>
        string ToCountSQL(string sql);

        /// <summary>
        /// 生成分页SQL。
        /// </summary>
        /// <param name="sql">SQL</param>
        /// <param name="pageIndex">页码（从“0”开始）</param>
        /// <param name="pageSize">分页条数</param>
        /// <example>SELECT * FROM Users WHERE Id > 100 => PAGING(`SELECT * FROM Users WHERE Id > 100`,<paramref name="pageIndex"/>,<paramref name="pageSize"/>)</example>
        /// <example>SELECT * FROM Users WHERE Id > 100 ORDER BY Id DESC => PAGING(`SELECT * FROM Users WHERE Id > 100`,<paramref name="pageIndex"/>,<paramref name="pageSize"/>,`ORDER BY Id DESC`)</example>
        /// <returns></returns>
        string ToSQL(string sql, int pageIndex, int pageSize);

        /// <summary>
        /// SQL 格式化（格式化为数据库可执行的语句）。
        /// </summary>
        /// <param name="sql">语句。</param>
        /// <param name="settings">配置。</param>
        /// <returns></returns>
        string Format(string sql, ISQLCorrectSettings settings);
    }
}
