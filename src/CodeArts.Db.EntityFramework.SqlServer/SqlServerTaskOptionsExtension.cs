using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace CodeArts.Db.EntityFramework
{
    /// <summary>
    /// Task 线程支持。
    /// </summary>
    public class SqlServerTaskOptionsExtension : SqlServerOptionsExtension
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public SqlServerTaskOptionsExtension()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="copyFrom">拷贝。</param>
        protected SqlServerTaskOptionsExtension(SqlServerOptionsExtension copyFrom) : base(copyFrom)
        {

        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override RelationalOptionsExtension Clone()
            => new SqlServerTaskOptionsExtension(this);

        /// <summary>
        /// 配置服务。
        /// </summary>
        /// <param name="services">服务集合。</param>
        public override void ApplyServices(IServiceCollection services)
        {
            base.ApplyServices(services);

            var sqlServerType = typeof(ISqlServerConnection);

            for (int i = 0; i < services.Count; i++)
            {
                var serviceDescriptor = services[i];

                if (serviceDescriptor.ServiceType == sqlServerType)
                {
                    services[i] = new ServiceDescriptor(sqlServerType, typeof(SqlServerTaskConnection), serviceDescriptor.Lifetime);

                    break;
                }
            }
        }
    }
}
