using System;
using System.Data;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 数据库链接适配器
    /// </summary>
    public interface IDbConnectionAdapter
    {
        /// <summary> 创建数据库连接 </summary>
        /// <returns></returns>
        IDbConnection Create(string connectionString);

        /// <summary>
        /// 供应器名称
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// 链接心跳（链接可以在心跳活动时间内被重用不需要重新分配链接，单位：分钟，最小5分钟，最大60分钟）
        /// </summary>
        double ConnectionHeartbeat { get; }

        /// <summary>
        /// SQL矫正设置
        /// </summary>
        ISQLCorrectSettings Settings { get; }
    }
}
