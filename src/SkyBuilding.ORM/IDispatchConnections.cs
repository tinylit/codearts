using System.Data;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 连接调度
    /// </summary>
    public interface IDispatchConnections
    {
        /// <summary>
        /// 获取数据库连接
        /// </summary>
        /// <param name="connectionString">链接字符串</param>
        /// <param name="adapter">数据库适配器</param>
        /// <param name="useCache">使用缓存</param>
        /// <returns></returns>
        IDbConnection Create(string connectionString, IDbConnectionAdapter adapter, bool useCache = true);
    }
}
