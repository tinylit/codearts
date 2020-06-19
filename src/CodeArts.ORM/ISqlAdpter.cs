#if NET40
using System.Collections.ObjectModel;
#else
using System.Collections.Generic;
#endif

namespace CodeArts.ORM
{
    /// <summary>
    /// SQL 适配器
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
#if NET40
        ReadOnlyCollection<TableToken> AnalyzeTables(string sql);
#else
        IReadOnlyCollection<TableToken> AnalyzeTables(string sql);
#endif

        /// <summary>
        /// SQL 分析（参数）。
        /// </summary>
        /// <param name="sql">来源于【<see cref="Analyze(string)"/>】的结果。</param>
        /// <returns></returns>
#if NET40
        ReadOnlyCollection<ParameterToken> AnalyzeParameters(string sql);
#else
        IReadOnlyCollection<ParameterToken> AnalyzeParameters(string sql);
#endif

        /// <summary>
        /// SQL 格式化（格式化为数据库可执行的语句）。
        /// </summary>
        /// <param name="sql">语句。</param>
        /// <returns></returns>
        string Format(string sql);

        /// <summary>
        /// SQL 格式化（格式化为数据库可执行的语句）。
        /// </summary>
        /// <param name="sql">语句</param>
        /// <param name="settings">配置。</param>
        /// <returns></returns>
        string Format(string sql, ISQLCorrectSimSettings settings);
    }
}
