using System;

namespace CodeArts.Db
{
    /// <summary>
    /// 命令SQL。
    /// </summary>
    public class CommandSql
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="parameters">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        public CommandSql(string sql, object parameters = null, int? commandTimeout = null)
        {
            if (string.IsNullOrEmpty(sql))
            {
                throw new ArgumentException($"“{nameof(sql)}”不能是 Null 或为空。", nameof(sql));
            }

            Sql = sql;
            Parameters = parameters;
            CommandTimeout = commandTimeout;
        }

        /// <summary>
        /// SQL。
        /// </summary>
        public string Sql { get; }

        /// <summary>
        /// 参数。
        /// </summary>
        public object Parameters { get; }

        /// <summary>
        /// 超时时间。
        /// </summary>
        public int? CommandTimeout { get; }
    }

    /// <summary>
    /// 命令SQL。
    /// </summary>
    public class CommandSql<T> : CommandSql
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="defaultValue">默认值。</param>
        public CommandSql(string sql, object param = null, int? commandTimeout = null, T defaultValue = default) : this(sql, param, commandTimeout, true, defaultValue)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="hasDefaultValue">是否包含默认值。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <param name="missingMsg">未找到数据异常消息。</param>
        public CommandSql(string sql, object param, int? commandTimeout, bool hasDefaultValue, T defaultValue = default, string missingMsg = null) : base(sql, param, commandTimeout)
        {
            HasDefaultValue = hasDefaultValue;
            DefaultValue = defaultValue;
            MissingMsg = missingMsg;
        }

        /// <summary>
        /// 是否包含默认值。
        /// </summary>
        public bool HasDefaultValue { get; }

        /// <summary>
        /// 默认值。
        /// </summary>
        public T DefaultValue { get; }

        /// <summary>
        /// 未找到数据异常消息。
        /// </summary>
        public string MissingMsg { get; }
    }
}
