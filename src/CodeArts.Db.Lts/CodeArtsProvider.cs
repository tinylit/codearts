using CodeArts.Db.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 代码艺术。
    /// </summary>
    public class CodeArtsProvider : RepositoryProvider
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="settings">SQL矫正配置。</param>
        /// <param name="visitors">自定义访问器集合。</param>
        public CodeArtsProvider(ISQLCorrectSettings settings, ICustomVisitorList visitors) : base(settings, visitors)
        {
        }

        /// <summary>
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="context">数据库上下文。</param>
        /// <param name="commandSql">命令SQL。</param>
        /// <returns></returns>
        public override T Read<T>(IDbContext context, CommandSql<T> commandSql)
        {
            using (var connection = context.CreateDb())
            {
                bool isClosedConnection = connection.State == ConnectionState.Closed;

                CommandBehavior behavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow;

                if (isClosedConnection)
                {
                    connection.Open();

                    behavior |= CommandBehavior.CloseConnection;
                }

                T defaultValue = commandSql.DefaultValue;

                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.AllowSkippingFormattingSql = true;

                        command.CommandText = commandSql.Sql;

                        if (commandSql.CommandTimeout.HasValue)
                        {
                            command.CommandTimeout = commandSql.CommandTimeout.Value;
                        }

                        AddParameterAuto(command, commandSql.Parameters);

                        using (var dr = command.ExecuteReader(behavior))
                        {
                            isClosedConnection = false;

                            if (dr.Read())
                            {
                                defaultValue = Mapper.ThrowsMap<T>(dr);

                                while (dr.Read()) { /* ignore subsequent rows */ }
                            }
                            else if (!commandSql.HasDefaultValue)
                            {
                                throw new DRequiredException(commandSql.MissingMsg);
                            }

                            while (dr.NextResult()) { /* ignore subsequent result sets */ }
                        }
                    }
                }
                finally
                {
                    if (isClosedConnection)
                    {
                        connection.Close();
                    }
                }

                return defaultValue;
            }
        }

        /// <summary>
        /// 查询列表集合。
        /// </summary>
        /// <typeparam name="T">集合元素类型。</typeparam>
        /// <param name="context">数据库上下文。</param>
        /// <param name="commandSql">命令SQL。</param>
        /// <returns></returns>
        public override IEnumerable<T> Query<T>(IDbContext context, CommandSql commandSql)
        {
            using (var connection = context.CreateDb())
            {
                bool isClosedConnection = connection.State == ConnectionState.Closed;

                CommandBehavior behavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult;

                if (isClosedConnection)
                {
                    connection.Open();

                    behavior |= CommandBehavior.CloseConnection;
                }

                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.AllowSkippingFormattingSql = true;

                        command.CommandText = commandSql.Sql;

                        if (commandSql.CommandTimeout.HasValue)
                        {
                            command.CommandTimeout = commandSql.CommandTimeout.Value;
                        }

                        AddParameterAuto(command, commandSql.Parameters);

                        using (var dr = command.ExecuteReader(behavior))
                        {
                            isClosedConnection = false;

                            while (dr.Read())
                            {
                                yield return Mapper.ThrowsMap<T>(dr);
                            }

                            while (dr.NextResult()) { /* ignore subsequent result sets */ }
                        }
                    }
                }
                finally
                {
                    if (isClosedConnection)
                    {
                        connection.Close();
                    }
                }
            }
        }

        /// <summary>
        /// 执行增删改功能。
        /// </summary>
        /// <param name="context">数据库上下文。</param>
        /// <param name="commandSql">命令SQL。</param>
        /// <returns>执行影响行。</returns>
        public override int Execute(IDbContext context, CommandSql commandSql)
        {
            using (var connection = context.CreateDb())
            {
                bool isClosedConnection = connection.State == ConnectionState.Closed;

                if (isClosedConnection)
                {
                    connection.Open();
                }

                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.AllowSkippingFormattingSql = true;

                        command.CommandText = commandSql.Sql;

                        if (commandSql.CommandTimeout.HasValue)
                        {
                            command.CommandTimeout = commandSql.CommandTimeout.Value;
                        }

                        AddParameterAuto(command, commandSql.Parameters);

                        return command.ExecuteNonQuery();
                    }
                }
                finally
                {
                    if (isClosedConnection)
                    {
                        connection.Close();
                    }
                }
            }
        }

#if NET_NORMAL || NET_CORE
        private sealed class AsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly IDbContext context;
            private readonly Action<IDbCommand, object> readyParam;
            private readonly CommandSql commandSql;

            private IAsyncEnumerator<T> enumerator;

            public AsyncEnumerable(IDbContext context, Action<IDbCommand, object> readyParam, CommandSql commandSql)
            {
                this.context = context;
                this.readyParam = readyParam;
                this.commandSql = commandSql;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
#if NETSTANDARD2_1
                => enumerator ??= new AsyncEnumerator<T>(context, readyParam, commandSql, cancellationToken);
#else
                => enumerator ?? (enumerator = new AsyncEnumerator<T>(context, readyParam, commandSql, cancellationToken));
#endif
        }

        private sealed class AsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IDbContext context;
            private readonly Action<IDbCommand, object> readyParam;
            private readonly CommandSql commandSql;
            private bool isClosedConnection = false;
            private bool isReadyConnection = false;
            private bool isReadyCommand = false;
            private bool isReadyDataReader = false;
            private bool isAsyncEnumeratorDisposed = false;

            private DbConnection dbConnection;
            private DbCommand dbCommand;
            private System.Data.Common.DbDataReader dbDataReader;

            private readonly CancellationToken cancellationToken;

            public AsyncEnumerator(IDbContext context, Action<IDbCommand, object> readyParam, CommandSql commandSql, CancellationToken cancellationToken)
            {
                this.context = context;
                this.readyParam = readyParam;
                this.commandSql = commandSql;
                this.cancellationToken = cancellationToken;
            }

            public T Current => Mapper.ThrowsMap<T>(dbDataReader);

#if NETSTANDARD2_1
            public async ValueTask<bool> MoveNextAsync()
#else
            public async Task<bool> MoveNextAsync()
#endif
            {
                if (isAsyncEnumeratorDisposed)
                {
                    return false;
                }

                if (isReadyDataReader)
                {
                    return await dbDataReader.ReadAsync(cancellationToken).ConfigureAwait(false);
                }

                CommandBehavior behavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult;

                dbConnection = context.CreateDb();

                isReadyConnection = true;

                isClosedConnection = dbConnection.State == ConnectionState.Closed;

                if (isClosedConnection)
                {
                    await dbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

                    behavior |= CommandBehavior.CloseConnection;
                }

                dbCommand = dbConnection.CreateCommand();

                dbCommand.AllowSkippingFormattingSql = true;

                dbCommand.CommandText = commandSql.Sql;

                if (commandSql.CommandTimeout.HasValue)
                {
                    dbCommand.CommandTimeout = commandSql.CommandTimeout.Value;
                }

                isReadyCommand = true;

                readyParam.Invoke(dbCommand, commandSql.Parameters);

                dbDataReader = await dbCommand.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);

                isReadyDataReader = true;

                isClosedConnection = false;

                return await dbDataReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }

#if NETSTANDARD2_1
            public async ValueTask DisposeAsync()
            {
                if (isAsyncEnumeratorDisposed)
                {
                    return;
                }

                isAsyncEnumeratorDisposed = true;

                if (isReadyDataReader)
                {
                    await dbDataReader.CloseAsync().ConfigureAwait(false);
                    await dbDataReader.DisposeAsync().ConfigureAwait(false);
                }

                if (isReadyCommand)
                {
                    await dbCommand.DisposeAsync().ConfigureAwait(false);
                }

                if (isClosedConnection)
                {
                    await dbConnection.CloseAsync().ConfigureAwait(false);
                }

                if (isReadyConnection)
                {
                    await dbConnection.DisposeAsync().ConfigureAwait(false);
                }
            }
#endif

            public void Dispose()
            {
                if (isAsyncEnumeratorDisposed)
                {
                    return;
                }

                isAsyncEnumeratorDisposed = true;

                if (isReadyDataReader)
                {
                    dbDataReader.Close();
                    dbDataReader.Dispose();
                }

                if (isReadyCommand)
                {
                    dbCommand.Dispose();
                }

                if (isClosedConnection)
                {
                    dbConnection.Close();
                }

                if (isReadyConnection)
                {
                    dbConnection.Dispose();
                }
            }
        }

        /// <summary>
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="context">数据库上下文。</param>
        /// <param name="commandSql">命令SQL。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public override async Task<T> ReadAsync<T>(IDbContext context, CommandSql<T> commandSql, CancellationToken cancellationToken = default)
        {
            using (DbConnection connection = context.CreateDb())
            {
                bool isClosedConnection = connection.State == ConnectionState.Closed;

                CommandBehavior behavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow;

                if (isClosedConnection)
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                    behavior |= CommandBehavior.CloseConnection;
                }

                T defaultValue = commandSql.DefaultValue;

                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.AllowSkippingFormattingSql = true;

                        command.CommandText = commandSql.Sql;

                        if (commandSql.CommandTimeout.HasValue)
                        {
                            command.CommandTimeout = commandSql.CommandTimeout.Value;
                        }

                        AddParameterAuto(command, commandSql.Parameters);

                        using (var dr = await command.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false))
                        {
                            isClosedConnection = false;

                            if (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                defaultValue = Mapper.ThrowsMap<T>(dr);

                                while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false)) { /* ignore subsequent rows */ }
                            }
                            else if (!commandSql.HasDefaultValue)
                            {
                                throw new DRequiredException(commandSql.MissingMsg);
                            }

                            while (await dr.NextResultAsync(cancellationToken).ConfigureAwait(false)) { /* ignore subsequent result sets */ }
                        }
                    }
                }
                finally
                {
                    if (isClosedConnection)
                    {
                        connection.Close();
                    }
                }

                return defaultValue;
            }
        }

        /// <summary>
        /// 查询列表集合。
        /// </summary>
        /// <typeparam name="T">集合元素类型。</typeparam>
        /// <param name="context">数据库上下文。</param>
        /// <param name="commandSql">命令SQL。</param>
        /// <returns></returns>
        public override IAsyncEnumerable<T> QueryAsync<T>(IDbContext context, CommandSql commandSql) => new AsyncEnumerable<T>(context, AddParameterAuto, commandSql);

        /// <summary>
        /// 执行增删改功能。
        /// </summary>
        /// <param name="context">数据库上下文。</param>
        /// <param name="commandSql">命令SQL。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns>执行影响行。</returns>
        public override async Task<int> ExecuteAsync(IDbContext context, CommandSql commandSql, CancellationToken cancellationToken = default)
        {
            using (var connection = context.CreateDb())
            {
                bool isClosedConnection = connection.State == ConnectionState.Closed;

                if (isClosedConnection)
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                }

                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.AllowSkippingFormattingSql = true;

                        command.CommandText = commandSql.Sql;

                        if (commandSql.CommandTimeout.HasValue)
                        {
                            command.CommandTimeout = commandSql.CommandTimeout.Value;
                        }

                        AddParameterAuto(command, commandSql.Parameters);

                        return await command.ExecuteNonQueryAsyc(cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    if (isClosedConnection)
                    {
                        connection.Close();
                    }
                }
            }
        }
#endif
    }
}
