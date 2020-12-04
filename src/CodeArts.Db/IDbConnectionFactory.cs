using System.Data;

namespace CodeArts.Db
{
    /// <summary>
    /// 数据连接工程。
    /// </summary>
    public interface IDbConnectionFactory
    {
        /// <summary> 创建数据库连接。 </summary>
        /// <returns></returns>
        IDbConnection Create(string connectionString);

        /// <summary>
        /// 供应器名称。
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// 链接心跳（链接可以在心跳活动时间内被重用不需要重新分配链接，单位：分钟）。
        /// </summary>
        double ConnectionHeartbeat { get; }

        /// <summary>
        /// 线程池数量。
        /// </summary>
        int MaxPoolSize { get; }
    }
}
