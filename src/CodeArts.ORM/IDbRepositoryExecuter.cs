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
        /// <typeparam name="T">实体</typeparam>
        /// <returns></returns>
        IBuilder<T> Create<T>();

        /// <summary>
        /// 执行增删改功能
        /// </summary>
        /// <param name="conn">数据库链接</param>
        /// <param name="expression">表达式</param>
        /// <returns></returns>
        int Execute<T>(IDbConnection conn, Expression expression);

        /// <summary>
        /// 执行增删改功能
        /// </summary>
        /// <param name="conn">数据库链接</param>
        /// <param name="sql">执行语句</param>
        /// <param name="parameters">参数</param>
        int Execute(IDbConnection conn, string sql, Dictionary<string, object> parameters = null);
    }
}
