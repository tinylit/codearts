using CodeArts.Db.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 数据库工厂。
    /// </summary>
    public class DatabaseFactory
    {
        /// <summary>
        /// 创建库。
        /// </summary>
        /// <param name="connectionConfig"></param>
        /// <returns></returns>
        public static IDatabase Create(IReadOnlyConnectionConfig connectionConfig)
        {
            if (connectionConfig is null)
            {
                throw new ArgumentNullException(nameof(connectionConfig));
            }

            return new Database(connectionConfig, DbConnectionManager.Get(connectionConfig.ProviderName));
        }

        private class Database : IDatabase
        {
            private IDatabaseFor databaseFor;
            private readonly IDbConnectionLtsAdapter adapter;
            private readonly IReadOnlyConnectionConfig connectionConfig;

            public Database(IReadOnlyConnectionConfig connectionConfig, IDbConnectionLtsAdapter adapter)
            {
                this.connectionConfig = connectionConfig;
                this.adapter = adapter;
            }

            public IDatabaseFor DatabaseFor
            {
                get
                {
                    if (databaseFor is null)
                    {
                        databaseFor = DbConnectionManager.GetOrCreate(adapter);
                    }

                    return databaseFor;
                }
            }

            public string Name => connectionConfig.Name;

            public string ProviderName => adapter.ProviderName;

            public string ConnectionString => connectionConfig.ConnectionString;

            public T Read<T>(Expression expression) => DatabaseFor.Read<T>(ConnectionString, expression);

            public IEnumerable<T> Query<T>(Expression expression) => DatabaseFor.Query<T>(ConnectionString, expression);

            public int Execute(Expression expression) => DatabaseFor.Execute(ConnectionString, expression);

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
            public Task<T> ReadAsync<T>(Expression expression, CancellationToken cancellationToken = default) => DatabaseFor.ReadAsync<T>(ConnectionString, expression, cancellationToken);
            public IAsyncEnumerable<T> QueryAsync<T>(Expression expression) => DatabaseFor.QueryAsync<T>(ConnectionString, expression);
            public Task<int> ExecuteAsync(Expression expression, CancellationToken cancellationToken = default) => DatabaseFor.ExecuteAsync(ConnectionString, expression, cancellationToken);
#endif

            #region IDisposable Support
            private bool disposedValue = false; // 要检测冗余调用

            /// <inheritdoc />
            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        GC.SuppressFinalize(this);
                    }

                    disposedValue = true;
                }
            }

            /// <inheritdoc />
            public void Dispose()
            {
                // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
                Dispose(true);
            }
            #endregion
        }
    }
}
