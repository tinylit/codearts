using System;

namespace CodeArts.DbAnnotations
{
    /// <summary>
    /// 语句
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class SqlAttribute : Attribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="sql">SQL</param>
        /// <param name="commandType">命令类型</param>
        public SqlAttribute(string sql, UppercaseString commandType)
        {
            if (sql is null)
            {
                throw new ArgumentNullException(nameof(sql));
            }

            if (sql.Length == 0)
            {
                throw new ArgumentException(nameof(sql));
            }

            Sql = sql;
            CommandType = commandType;
        }

        /// <summary>
        /// SQL
        /// </summary>
        public string Sql { get; }
        /// <summary>
        /// 命令。
        /// </summary>
        public UppercaseString CommandType { get; }
    }
}
