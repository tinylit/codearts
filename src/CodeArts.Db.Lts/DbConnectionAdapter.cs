using System;
using System.Data;
using System.Data.Common;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 链接适配器。
    /// </summary>
    public class DbConnectionAdapter : DbConnectionFactory, IDbConnectionLtsAdapter
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="providerName">供应商名称。</param>
        /// <param name="settings">SQL矫正配置。</param>
        /// <param name="dbProviderFactory">供应商工厂。</param>
        public DbConnectionAdapter(string providerName, ISQLCorrectSettings settings, DbProviderFactory dbProviderFactory) : base(providerName, dbProviderFactory)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="providerName">供应商名称。</param>
        /// <param name="settings">SQL矫正配置。</param>
        /// <param name="dbProviderFactory">供应商工厂。</param>
        /// <param name="connectionHeartbeat">连接心跳。</param>
        public DbConnectionAdapter(string providerName, ISQLCorrectSettings settings, DbProviderFactory dbProviderFactory, double connectionHeartbeat)
            : base(providerName, dbProviderFactory, connectionHeartbeat)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// SQL 矫正配置。
        /// </summary>
        public ISQLCorrectSettings Settings { get; }


        private CustomVisitorList visitters;

        /// <summary>
        /// 格式化。
        /// </summary>
        #if NETSTANDARD2_1_OR_GREATER
        public ICustomVisitorList Visitors => visitters ??= new CustomVisitorList();
#else
        public ICustomVisitorList Visitors => visitters ?? (visitters = new CustomVisitorList());
#endif
    }
}
