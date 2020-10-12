using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.ORM
{
    /// <summary>
    /// 路由执行能力。
    /// </summary>
    public interface IRouteExecuter
    {
        /// <summary>
        /// SQL矫正
        /// </summary>
        ISQLCorrectSimSettings Settings { get; }

        /// <summary>
        /// 执行。
        /// </summary>
        /// <param name="sql">SQL语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        int ExecuteCommand(string sql, Dictionary<string, object> param = null, int? commandTimeout = null);
    }

    /// <summary>
    /// 路由执行能力。
    /// </summary>
    public interface IRouteExecuter<T> : IRouteExecuter
    {
        /// <summary>
        /// 创建路由供应器。
        /// </summary>
        /// <param name="behavior"></param>
        /// <returns></returns>
        IRouteProvider<T> CreateRouteProvider(CommandBehavior behavior);
    }
}
