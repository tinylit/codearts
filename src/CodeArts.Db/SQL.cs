using System.Collections.Generic;
using System.Collections.Concurrent;
#if NET40
using System.Collections.ObjectModel;
#endif
using System.Diagnostics;

namespace CodeArts.Db
{
    /// <summary>
    /// SQL 默认语法：
    ///     表名称：[yep_users]
    ///     名称：[name]
    ///     参数名称:{name}
    ///     条件移除：DROP TABLE IF EXIXSTS [yep_users];
    ///     条件创建：CREATE TABLE IF NOT EXIXSTS [yep_users] ([Id] int not null,[name] varchar(100));
    /// 说明：会自动去除代码注解和多余的换行符压缩语句。
    /// </summary>
    [DebuggerDisplay("{sql}")]
    public sealed class SQL
    {
        private readonly string sql;
        private readonly string originalSql;
        private static readonly ISqlAdpter adpter;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static SQL() => adpter = RuntimeServPools.Singleton<ISqlAdpter, DefaultSqlAdpter>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="sql">原始SQL语句。</param>
        public SQL(string sql) => this.sql = adpter.Analyze(this.originalSql = sql);

        /// <summary>
        /// 添加语句。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <returns></returns>
        public SQL Add(string sql)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return this;
            }

            bool flag = false;

            for (int i = originalSql.Length - 1; i > 0; i--)
            {
                char c = originalSql[i];

                if (c == '\x20' || c == '\t' || c == '\r' || c == '\n' || c == '\f')
                {
                    continue;
                }

                flag = c == ';';

                break;
            }

            return new SQL(flag
                ? string.Concat(originalSql, sql)
                : string.Concat(originalSql, ";", sql));
        }

        /// <summary>
        /// 添加语句。
        /// </summary>
        /// <param name="sql">SQL。</param>
        public SQL Add(SQL sql)
        {
            if (sql is null)
            {
                return this;
            }

            return Add(sql.originalSql);
        }

        private IReadOnlyList<TableToken> tables;
        private IReadOnlyList<string> parameters;

        /// <summary>
        /// 操作的表。
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER
        public IReadOnlyList<TableToken> Tables => tables ??= adpter.AnalyzeTables(sql);
#else
        public IReadOnlyList<TableToken> Tables => tables ?? (tables = adpter.AnalyzeTables(sql));
#endif

        /// <summary>
        /// 参数。
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER
        public IReadOnlyList<string> Parameters => parameters ??= adpter.AnalyzeParameters(sql);
#else
        public IReadOnlyList<string> Parameters => parameters ?? (parameters = adpter.AnalyzeParameters(sql));
#endif

        /// <summary>
        /// 获取总行数。
        /// </summary>
        /// <returns></returns>
        public SQL ToCountSQL() => new SQL(adpter.ToCountSQL(originalSql));

        /// <summary>
        /// 获取分页数据。
        /// </summary>
        /// <param name="pageIndex">页码（从“0”开始）</param>
        /// <param name="pageSize">分页条数。</param>
        /// <returns></returns>
        public SQL ToSQL(int pageIndex, int pageSize) => new SQL(adpter.ToSQL(originalSql, pageIndex, pageSize));

        /// <summary>
        /// 转为实际数据库的SQL语句。
        /// </summary>
        /// <param name="settings">SQL修正配置。</param>
        /// <returns></returns>
        public string ToString(ISQLCorrectSettings settings) => adpter.Format(sql, settings);

        /// <summary>
        /// 返回分析的SQL结果。
        /// </summary>
        /// <returns></returns>
        public override string ToString() => originalSql;

        /// <summary>
        /// 追加sql。
        /// </summary>
        /// <param name="left">原SQL。</param>
        /// <param name="right">需要追加的SQL。</param>
        /// <returns></returns>
        public static SQL operator +(SQL left, SQL right)
        {
            if (left is null)
            {
                return right;
            }

            if (right is null)
            {
                return left;
            }

            return left.Add(right);
        }

        /// <summary>
        /// 追加sql。
        /// </summary>
        /// <param name="left">原SQL。</param>
        /// <param name="right">需要追加的SQL。</param>
        /// <returns></returns>
        public static SQL operator +(SQL left, string right)
        {
            if (left is null)
            {
                return new SQL(right);
            }

            if (right is null)
            {
                return left;
            }

            return left.Add(right);
        }

        /// <summary>
        /// 隐式转换。
        /// </summary>
        /// <param name="sql">SQL。</param>
        public static implicit operator SQL(string sql) => new SQL(sql);

        /// <summary>
        /// 隐式转换。
        /// </summary>
        /// <param name="sql">SQL。</param>
        public static implicit operator string(SQL sql) => sql?.ToString();
    }
}
