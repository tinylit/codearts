using Dapper;
using SkyBuilding.ORM;
using System.Collections.Generic;
using System.Data;

namespace SkyBuilding.Dapper
{
    /// <summary>
    /// Dapper供应器
    /// </summary>
    public class DapperProvider : RepositoryProvider, IDbRepositoryExecuter
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="settings"></param>
        public DapperProvider(ISQLCorrectSettings settings) : base(settings)
        {

        }
        /// <summary>
        /// 查询独立实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="conn">数据库链接</param>
        /// <param name="sql">查询语句</param>
        /// <param name="parameters">参数</param>
        /// <returns></returns>
        protected override T Single<T>(IDbConnection conn, string sql, Dictionary<string, object> parameters)
        {
            return conn.QueryFirstOrDefault<T>(sql, parameters);
        }
        /// <summary>
        /// 查询列表集合
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <param name="conn">数据库链接</param>
        /// <param name="sql">查询语句</param>
        /// <param name="parameters">参数</param>
        /// <returns></returns>
        protected override IEnumerable<T> Select<T>(IDbConnection conn, string sql, Dictionary<string, object> parameters)
        {
            return conn.Query<T>(sql, parameters);
        }
        /// <summary>
        /// 执行增删改功能
        /// </summary>
        /// <param name="conn">数据库链接</param>
        /// <param name="sql">执行语句</param>
        /// <param name="parameters">参数</param>
        /// <param name="trans">事务</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <param name="commandType">操作方式</param>
        /// <returns></returns>
        public override int Execute(IDbConnection conn, string sql, Dictionary<string, object> parameters)
        {
            return conn.Execute(sql, parameters);
        }
    }
}
