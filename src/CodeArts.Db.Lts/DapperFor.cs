using CodeArts.Db.Exceptions;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// Dapper for Lts。
    /// </summary>
    public class DapperFor : DatabaseFor
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public DapperFor(ISQLCorrectSettings settings, ICustomVisitorList visitors) : base(settings, visitors)
        {
        }

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <returns></returns>
        protected override int Execute(IDbConnection connection, CommandSql commandSql)
        => connection.Execute(commandSql.Sql, commandSql.Parameters, null, commandSql.CommandTimeout);

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        protected override Task<int> ExecuteAsync(IDbConnection connection, CommandSql commandSql, CancellationToken cancellationToken)
        => connection.ExecuteAsync(commandSql.Sql, commandSql.Parameters, null, commandSql.CommandTimeout);
#endif
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <returns></returns>
        protected override IEnumerable<T> Query<T>(IDbConnection connection, CommandSql commandSql)
        => connection.Query<T>(commandSql.Sql, commandSql.Parameters, null, true, commandSql.CommandTimeout);

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <returns></returns>
        protected override IAsyncEnumerable<T> QueryAsync<T>(IDbConnection connection, CommandSql commandSql)
        => new AsyncEnumerable<T>(connection, commandSql);

        private sealed class AsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly IDbConnection connection;
            private readonly CommandSql commandSql;

            private IAsyncEnumerator<T> enumerator;

            public AsyncEnumerable(IDbConnection connection, CommandSql commandSql)
            {
                this.connection = connection;
                this.commandSql = commandSql;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
#if NETSTANDARD2_1_OR_GREATER
                => enumerator ??= new AsyncEnumerator<T>(connection, commandSql, cancellationToken);
#else
                => enumerator ?? (enumerator = new AsyncEnumerator<T>(connection, commandSql, cancellationToken));
#endif
        }

        private sealed class AsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private IEnumerator<T> enumerator;

            private readonly IDbConnection connection;
            private readonly CommandSql commandSql;

            private readonly CancellationToken cancellationToken;

            public AsyncEnumerator(IDbConnection connection, CommandSql commandSql, CancellationToken cancellationToken)
            {
                this.connection = connection;
                this.commandSql = commandSql;
                this.cancellationToken = cancellationToken;
            }

            public T Current => enumerator.Current;

#if NETSTANDARD2_1_OR_GREATER
            public async ValueTask<bool> MoveNextAsync()
#else
            public async Task<bool> MoveNextAsync()
#endif
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (enumerator is null)
                {
                    var results = await connection.QueryAsync<T>(new CommandDefinition(commandSql.Sql, commandSql.Parameters, null, commandSql.CommandTimeout, null, CommandFlags.Buffered, cancellationToken));

                    enumerator = results.GetEnumerator();
                }

                return enumerator.MoveNext();
            }

#if NETSTANDARD2_1_OR_GREATER
            public ValueTask DisposeAsync() => new ValueTask(Task.Run(enumerator.Dispose));
#endif

            public void Dispose() => enumerator.Dispose();
        }
#endif
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <returns></returns>
        protected override T Read<T>(IDbConnection connection, CommandSql<T> commandSql)
        {
            var type = typeof(T);

            var value = connection.QuerySingleOrDefault(type.IsValueType && !type.IsNullable() ? typeof(Nullable<>).MakeGenericType(type) : type, commandSql.Sql, commandSql.Parameters, null, commandSql.CommandTimeout);

            if (value is null)
            {
                return commandSql.HasDefaultValue ? commandSql.DefaultValue : throw new DRequiredException(commandSql.MissingMsg);
            }

            return (T)value;
        }

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        protected override async Task<T> ReadAsync<T>(IDbConnection connection, CommandSql<T> commandSql, CancellationToken cancellationToken)
        {
            var type = typeof(T);

            var value = await connection.QuerySingleOrDefaultAsync(type.IsValueType && !type.IsNullable() ? typeof(Nullable<>).MakeGenericType(type) : type, commandSql.Sql, commandSql.Parameters, null, commandSql.CommandTimeout);

            if (value is null)
            {
                return commandSql.HasDefaultValue ? commandSql.DefaultValue : throw new DRequiredException(commandSql.MissingMsg);
            }

            return (T)value;
        }
#endif
    }
}
