﻿using Dapper;
using CodeArts.ORM;
using System.Collections.Generic;
using System.Data;

namespace CodeArts.Dapper
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
        /// <param name="required">是否必须</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns></returns>
        public override T QueryFirst<T>(IDbConnection conn, string sql, Dictionary<string, object> parameters = null, bool required = false, T defaultValue = default)
        {
            if (required)
                return conn.QueryFirst<T>(sql, parameters);

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
        public override IEnumerable<T> Query<T>(IDbConnection conn, string sql, Dictionary<string, object> parameters = null)
        {
            return conn.Query<T>(sql, parameters);
        }
        /// <summary>
        /// 执行增删改功能
        /// </summary>
        /// <param name="conn">数据库链接</param>
        /// <param name="sql">执行语句</param>
        /// <param name="parameters">参数</param>
        /// <returns></returns>
        public override int Execute(IDbConnection conn, string sql, Dictionary<string, object> parameters = null)
        {
            return conn.Execute(sql, parameters);
        }
    }
}