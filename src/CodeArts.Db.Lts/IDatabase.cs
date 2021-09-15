using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 数据库（开启事务后，创建的<see cref="IDbCommand"/>会自动设置事务。）。
    /// </summary>
    public partial interface IDatabase : IDbConnection
    {
        /// <summary> 连接名称。 </summary>
        string Name { get; }

        /// <summary> 数据库驱动名称。 </summary>
        string ProviderName { get; }

        /// <summary>
        /// SQL矫正器。
        /// </summary>
        ISQLCorrectSettings Settings { get; }

        /// <summary>
        /// 按照当前数据库，格式化SQL语句。
        /// </summary>
        /// <param name="sql">语句。</param>
        /// <returns></returns>
        string Format(SQL sql);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        T Read<T>(Expression expression);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        IEnumerable<T> Query<T>(Expression expression);

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        int Execute(Expression expression);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="commandSql">查询语句。</param>
        /// <returns></returns>
        T Read<T>(CommandSql<T> commandSql);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="missingMsg">未找到数据异常。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        T Single<T>(string sql, object param = null, string missingMsg = null, int? commandTimeout = null);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns></returns>
        T SingleOrDefault<T>(string sql, object param = null, int? commandTimeout = null, T defaultValue = default);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="missingMsg">未找到数据异常。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        T First<T>(string sql, object param = null, string missingMsg = null, int? commandTimeout = null);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns></returns>
        T FirstOrDefault<T>(string sql, object param = null, int? commandTimeout = null, T defaultValue = default);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="commandSql">查询语句。</param>
        /// <returns></returns>
        IEnumerable<T> Query<T>(CommandSql commandSql);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        IEnumerable<T> Query<T>(string sql, object param = null, int? commandTimeout = null);

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="commandSql">执行语句。</param>
        /// <returns></returns>
        int Execute(CommandSql commandSql);

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        int Execute(string sql, object param = null, int? commandTimeout = null);
    }
}
