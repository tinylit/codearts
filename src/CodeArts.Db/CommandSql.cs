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
    /// 查询行。
    /// </summary>
    public enum RowStyle
    {
        /// <summary>
        /// 不限。
        /// </summary>
        None = -1,
        /// <summary>
        /// 取第一条。
        /// </summary>
        First = 0,
        /// <summary>
        /// 取第一条或默认。
        /// </summary>
        FirstOrDefault = 1,
        /// <summary>
        /// 有且仅有一条。
        /// </summary>
        Single = 2,
        /// <summary>
        /// 只有一条，或没有。
        /// </summary>
        SingleOrDefault = 3
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
        /// <param name="rowStyle">数据风格。</param>
        /// <param name="missingMsg">异常信息。</param>
        /// <param name="commandTimeout">超时时间。</param>
        public CommandSql(string sql, object param, RowStyle rowStyle = RowStyle.None, string missingMsg = null, int? commandTimeout = null)
            : this(sql, param, rowStyle, false, default, commandTimeout, missingMsg)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="rowStyle">数据风格。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <param name="commandTimeout">超时时间。</param>
        public CommandSql(string sql, object param, RowStyle rowStyle, T defaultValue, int? commandTimeout = null)
            : this(sql, param, rowStyle, true, defaultValue, commandTimeout, null)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="hasDefaultValue">有指定默认值。</param>
        /// <param name="rowStyle">数据风格。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <param name="missingMsg">未找到数据异常消息。</param>
        public CommandSql(string sql, object param, RowStyle rowStyle, bool hasDefaultValue, T defaultValue = default, int? commandTimeout = null, string missingMsg = null) : base(sql, param, commandTimeout)
        {
            HasDefaultValue = hasDefaultValue;
            DefaultValue = defaultValue;
            MissingMsg = missingMsg;
            RowStyle = rowStyle;
        }

        /// <summary>
        /// 数据风格。
        /// </summary>
        public RowStyle RowStyle { get; }

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
