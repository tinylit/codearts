using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.Db.EntityFramework
{
    /// <summary>
    /// 数据库链接。
    /// </summary>
    public class SqlServerTaskConnection : SqlServerConnection
    {
        private readonly ConcurrentDictionary<int, DbConnection> taskConnections = new ConcurrentDictionary<int, DbConnection>();

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public SqlServerTaskConnection(RelationalConnectionDependencies dependencies) : base(dependencies)
        {

        }

        /// <summary>
        /// 根据Task分配链接。
        /// </summary>
        public override DbConnection DbConnection
        {
            get
            {
                var taskId = Task.CurrentId;

                if (taskId.HasValue)
                {
                    return taskConnections.GetOrAdd(taskId.Value, _ => CreateDbConnection());
                }

                var thread = Thread.CurrentThread;

                if (thread is null)
                {
                    return base.DbConnection;
                }

                return taskConnections.GetOrAdd(Thread.CurrentThread.ManagedThreadId, _ => CreateDbConnection());
            }
        }

        /// <summary>
        /// 释放连接。
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            foreach (var kv in taskConnections)
            {
                kv.Value.Dispose();
            }

            taskConnections.Clear();
        }
    }
}
