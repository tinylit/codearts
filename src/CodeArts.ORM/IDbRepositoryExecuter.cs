using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;

namespace CodeArts.ORM
{
    /// <summary>
    /// 仓储执行器
    /// </summary>
    public interface IDbRepositoryExecuter
    {
        /// <summary>
        /// 创建执行器
        /// </summary>
        /// <returns></returns>
        IExecuteVisitor CreateExe();

        /// <summary>
        /// 执行增删改功能
        /// </summary>
        /// <param name="conn">数据库链接</param>
        /// <param name="expression">表达式</param>
        /// <returns>执行影响行</returns>
        int Execute(IDbConnection conn, Expression expression);

        /// <summary>
        /// 执行增删改功能
        /// </summary>
        /// <param name="conn">数据库链接</param>
        /// <param name="sql">执行语句</param>
        /// <param name="parameters">参数</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns>执行影响行</returns>
        int Execute(IDbConnection conn, string sql, Dictionary<string, object> parameters = null, int? commandTimeout = null);
    }
}
