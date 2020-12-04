using CodeArts.Db.Common;
using CodeArts.Db.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.Db
{
    /// <summary>
    /// 代码艺术。
    /// </summary>
    public class CodeArtsProvider : RepositoryProvider
    {
        private readonly ISQLCorrectSettings settings;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="settings">SQL矫正配置。</param>
        public CodeArtsProvider(ISQLCorrectSettings settings) : base(settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
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
                                defaultValue = Mapper.Map<T>(dr);

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

                if (settings.Engine == DatabaseEngine.SQLite)
                {
                    behavior &= ~CommandBehavior.SingleResult;
                }

                if (isClosedConnection)
                {
                    connection.Open();

                    behavior |= CommandBehavior.CloseConnection;
                }

                try
                {
                    using (var command = connection.CreateCommand())
                    {
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
                                yield return Mapper.Map<T>(dr);
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

#if NET_NORMAL

        private sealed class AsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly IDbContext context;
            private readonly Action<DbCommand, object> readyParam;
            private readonly CommandSql commandSql;

            private IAsyncEnumerator<T> enumerator;

            public AsyncEnumerable(IDbContext context, Action<DbCommand, object> readyParam, CommandSql commandSql)
            {
                this.context = context;
                this.readyParam = readyParam;
                this.commandSql = commandSql;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator() => enumerator ?? (enumerator = new AsyncEnumerator<T>(context, readyParam, commandSql));
        }

        private sealed class AsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IDbContext context;
            private readonly Action<DbCommand, object> readyParam;
            private readonly CommandSql commandSql;
            private bool isClosedConnection = false;
            private bool isReadyConnection = false;
            private bool isReadyCommand = false;
            private bool isReadyDataReader = false;

            private DbConnection dbConnection;
            private DbCommand dbCommand;
            private System.Data.Common.DbDataReader dbDataReader;

            public AsyncEnumerator(IDbContext context, Action<DbCommand, object> readyParam, CommandSql commandSql)
            {
                this.context = context;
                this.readyParam = readyParam;
                this.commandSql = commandSql;
            }

            public T Current => Mapper.Map<T>(dbDataReader);

            public void Dispose()
            {
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

            public async Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                if (isReadyDataReader)
                {
                    return await dbDataReader.ReadAsync(cancellationToken);
                }

                CommandBehavior behavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult;

                dbConnection = context.CreateDb();

                isReadyConnection = true;

                isClosedConnection = dbConnection.State == ConnectionState.Closed;

                if (isClosedConnection)
                {
                    await dbConnection.OpenAsync(cancellationToken);

                    behavior |= CommandBehavior.CloseConnection;
                }

                dbCommand = dbConnection.CreateCommand();

                dbCommand.CommandText = commandSql.Sql;

                if (commandSql.CommandTimeout.HasValue)
                {
                    dbCommand.CommandTimeout = commandSql.CommandTimeout.Value;
                }

                isReadyCommand = true;

                readyParam.Invoke(dbCommand, commandSql.Parameters);

                dbDataReader = await dbCommand.ExecuteReaderAsync(behavior, cancellationToken);

                isReadyDataReader = true;

                isClosedConnection = false;

                return await dbDataReader.ReadAsync(cancellationToken);
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
                    await connection.OpenAsync(cancellationToken);

                    behavior |= CommandBehavior.CloseConnection;
                }

                T defaultValue = commandSql.DefaultValue;

                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = commandSql.Sql;

                        if (commandSql.CommandTimeout.HasValue)
                        {
                            command.CommandTimeout = commandSql.CommandTimeout.Value;
                        }

                        AddParameterAuto(command, commandSql.Parameters);

                        using (var dr = await command.ExecuteReaderAsync(behavior, cancellationToken))
                        {
                            isClosedConnection = false;

                            if (await dr.ReadAsync(cancellationToken))
                            {
                                defaultValue = Mapper.Map<T>(dr);

                                while (await dr.ReadAsync(cancellationToken)) { /* ignore subsequent rows */ }
                            }
                            else if (!commandSql.HasDefaultValue)
                            {
                                throw new DRequiredException(commandSql.MissingMsg);
                            }

                            while (await dr.NextResultAsync(cancellationToken)) { /* ignore subsequent result sets */ }
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
                    await connection.OpenAsync(cancellationToken);
                }

                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = commandSql.Sql;

                        if (commandSql.CommandTimeout.HasValue)
                        {
                            command.CommandTimeout = commandSql.CommandTimeout.Value;
                        }

                        AddParameterAuto(command, commandSql.Parameters);

                        return await command.ExecuteNonQueryAsyc(cancellationToken);
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
