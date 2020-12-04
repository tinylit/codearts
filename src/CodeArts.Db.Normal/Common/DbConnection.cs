using System;
using System.Data;
using System.Threading;
#if NET_NORMAL
using System.Threading.Tasks;
#endif

namespace CodeArts.Db.Common
{
    /// <summary>
    /// 数据库(Close和Dispose接口虚拟，通过接口调用时不会真正关闭或释放，只有通过类调用才会真实的执行)
    /// </summary>
    public class DbConnection : IDisposable
    {
        private readonly IDbConnection connection; //数据库连接
        private readonly ISQLCorrectSimSettings settings;
#if NET_NORMAL
        private readonly System.Data.Common.DbConnection dbConnection;

        /// <inheritdoc />
        internal DbConnection(IDbConnection connection, ISQLCorrectSimSettings settings)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (connection is System.Data.Common.DbConnection dbConnection)
            {
                IsAsynchronousSupport = true;

                this.dbConnection = dbConnection;
            }
        }

        /// <inheritdoc />
        public bool IsAsynchronousSupport { get; }
#else
        /// <inheritdoc />
        internal DbConnection(IDbConnection connection, ISQLCorrectSimSettings settings)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }
#endif

        /// <inheritdoc />
        public ConnectionState State => connection.State;

        /// <inheritdoc />
        public DbTransaction BeginTransaction() => new DbTransaction(connection.BeginTransaction());

        /// <inheritdoc />
        public DbTransaction BeginTransaction(IsolationLevel il) => new DbTransaction(connection.BeginTransaction(il));

        /// <inheritdoc />
        public void Open()
        {
            switch (connection.State)
            {
                case ConnectionState.Closed:
                    connection.Open();
                    break;
                case ConnectionState.Connecting:

                    do
                    {
                        Thread.Sleep(5);

                    } while (State == ConnectionState.Connecting);

                    goto default;
                case ConnectionState.Broken:
                    connection.Close();
                    goto default;
                default:
                    if (connection.State == ConnectionState.Closed)
                    {
                        connection.Open();
                    }
                    break;
            }
        }

#if NET_NORMAL

        /// <inheritdoc />
        public Task OpenAsync(CancellationToken cancellationToken = default)
        {
            if (IsAsynchronousSupport)
            {
                switch (connection.State)
                {
                    case ConnectionState.Closed:
                        return dbConnection.OpenAsync(cancellationToken);
                    case ConnectionState.Broken:
                        connection.Close();
                        goto default;
                    case ConnectionState.Connecting:
                    default:
                        if (connection.State != ConnectionState.Open)
                        {
                            return dbConnection.OpenAsync(cancellationToken);
                        }

#if NET45
                        return Task.Delay(0, cancellationToken);
#else
                        return Task.CompletedTask;
#endif

                }
            }

            throw new InvalidOperationException("Async operations require use of a DbConnection or an already-open IDbConnection.");
        }
#endif

        /// <inheritdoc />
        public void Close() => connection.Close();

        /// <inheritdoc />
        public DbCommand CreateCommand()
        {
#if NET_NORMAL
            if (IsAsynchronousSupport)
            {
                return new DbCommand(dbConnection.CreateCommand(), settings);
            }
            else
            {
                return new DbCommand(connection.CreateCommand(), settings);
            }
#else
            return new DbCommand(connection.CreateCommand(), settings);
#endif
        }

        /// <inheritdoc />
        public void Dispose() => Dispose(true);

        /// <inheritdoc />
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                connection?.Close();
                connection?.Dispose();

                GC.SuppressFinalize(this);
            }
        }
    }
}
