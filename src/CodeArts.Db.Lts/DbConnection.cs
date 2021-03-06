﻿using System;
using System.Data;
using System.Threading;
#if NET_NORMAL || NET_CORE
using System.Threading.Tasks;
#endif

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 数据库(Close和Dispose接口虚拟，通过接口调用时不会真正关闭或释放，只有通过类调用才会真实的执行)
    /// </summary>
    public class DbConnection :
#if NETSTANDARD2_1
        IAsyncDisposable,
#endif
        IDisposable
    {
        private readonly IDbConnection connection; //数据库连接
        private readonly ISQLCorrectSettings settings;
#if NET_NORMAL || NET_CORE
        private readonly System.Data.Common.DbConnection dbConnection;

        /// <inheritdoc />
        internal DbConnection(IDbConnection connection, ISQLCorrectSettings settings)
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
        internal DbConnection(IDbConnection connection, ISQLCorrectSettings settings)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }
#endif

        internal DbConnection(DbConnection connection) : this(connection.connection, connection.settings)
        {

        }

        /// <inheritdoc />
        public ConnectionState State => connection.State;

        /// <inheritdoc />
        public virtual DbTransaction BeginTransaction() => new DbTransaction(connection.BeginTransaction());

        /// <inheritdoc />
        public virtual DbTransaction BeginTransaction(IsolationLevel il) => new DbTransaction(connection.BeginTransaction(il));

#if NETSTANDARD2_1

        /// <inheritdoc />
        public virtual async ValueTask<DbTransactionAsync> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
        {
            if (IsAsynchronousSupport)
            {
                return new DbTransactionAsync(await dbConnection.BeginTransactionAsync(isolationLevel, cancellationToken));
            }

            throw new InvalidOperationException("Async operations require use of a DbConnection.");
        }

        /// <inheritdoc />
        public virtual async ValueTask<DbTransactionAsync> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (IsAsynchronousSupport)
            {
                return new DbTransactionAsync(await dbConnection.BeginTransactionAsync(cancellationToken));
            }

            throw new InvalidOperationException("Async operations require use of a DbConnection.");
        }
#endif

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

#if NET_NORMAL || NET_CORE
        /// <inheritdoc />
        public async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            if (!IsAsynchronousSupport)
            {
                throw new InvalidOperationException("Async operations require use of a DbConnection or an already-open IDbConnection.");
            }

            switch (connection.State)
            {
                case ConnectionState.Closed:
                    await dbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case ConnectionState.Broken:
#if NETSTANDARD2_1
                    await dbConnection.CloseAsync().ConfigureAwait(false);
#else
                    connection.Close();
#endif
                    goto default;
                case ConnectionState.Connecting:
                default:
                    if (connection.State != ConnectionState.Open)
                    {
                        await dbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    }
                    break;
            }
        }
#endif

        /// <inheritdoc />
        public void Close() => connection.Close();

#if NETSTANDARD2_1
        /// <inheritdoc />
        public Task CloseAsync()
        {
            if (IsAsynchronousSupport)
            {
                return dbConnection.CloseAsync();
            }

            throw new InvalidOperationException("Async operations require use of a DbConnection.");
        }
        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (IsAsynchronousSupport)
            {
                await dbConnection.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                Dispose();
            }
        }
#endif

        /// <inheritdoc />
        public virtual DbCommand CreateCommand()
        {
#if NET_NORMAL || NET_CORE
            if (IsAsynchronousSupport)
            {
                return new DbCommand(dbConnection.CreateCommand(), settings);
            }
#endif

            return new DbCommand(connection.CreateCommand(), settings);
        }

        /// <inheritdoc />
        public void Dispose() => Dispose(true);

        /// <inheritdoc />
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                connection?.Dispose();

                GC.SuppressFinalize(this);
            }
        }
    }
}
