using System;
using System.Data;
using System.Data.Common;
#if NET_NORMAL || NETSTANDARD2_0
using System.Threading;
using System.Threading.Tasks;
#endif

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 数据库命令。
    /// </summary>
    public class DbCommand : IDbCommand, IDisposable
    {
        private readonly IDbCommand command;
        private readonly ISQLCorrectSettings settings;

#if NET_NORMAL || NETSTANDARD2_0
        private readonly System.Data.Common.DbCommand dbCommand;

        /// <summary>
        /// 构造函数。
        /// </summary>
        internal DbCommand(IDbCommand command, ISQLCorrectSettings settings)
        {
            this.command = command ?? throw new ArgumentNullException(nameof(command));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            if (command is System.Data.Common.DbCommand dbCommand)
            {
                IsAsynchronousSupport = true;

                this.dbCommand = dbCommand;
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        internal DbCommand(System.Data.Common.DbCommand dbCommand, ISQLCorrectSettings settings)
        {
            this.dbCommand = dbCommand ?? throw new ArgumentNullException(nameof(dbCommand));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.command = dbCommand;
            IsAsynchronousSupport = true;
        }

        /// <summary>
        /// 是否支持异步。
        /// </summary>
        public bool IsAsynchronousSupport { get; }
#else
        /// <summary>
        /// 构造函数。
        /// </summary>
        internal DbCommand(IDbCommand command, ISQLCorrectSettings settings)
        {
            this.command = command ?? throw new ArgumentNullException(nameof(command));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }
#endif


        private DbTransaction transaction;

        /// <inheritdoc />
        public DbTransaction Transaction
        {
            get => transaction;
            set
            {
                if (value is null)
                {
                    command.Transaction = null;
                }
                else
                {
                    command.Transaction = value.Transaction;
                }

                transaction = value;
            }
        }

        /// <inheritdoc />
        string IDbCommand.CommandText { get => command.CommandText; set => command.CommandText = value; }

        /// <inheritdoc />
        public string CommandText
        {
            get => command.CommandText;
            set => command.CommandText = new SQL(value).ToString(settings);
        }

        /// <inheritdoc />
        public int CommandTimeout { get => command.CommandTimeout; set => command.CommandTimeout = value; }

        /// <inheritdoc />
        public CommandType CommandType { get => command.CommandType; set => command.CommandType = value; }

        private DbParameterCollection parameters;

        /// <inheritdoc />
        public IDataParameterCollection Parameters => parameters ?? (parameters = new DbParameterCollection(command.Parameters, settings));

        /// <inheritdoc />
        public UpdateRowSource UpdatedRowSource { get => command.UpdatedRowSource; set => command.UpdatedRowSource = value; }

        /// <inheritdoc />
        IDbConnection IDbCommand.Connection { get => command.Connection; set => command.Connection = value; }

        /// <inheritdoc />
        IDbTransaction IDbCommand.Transaction { get => command.Transaction; set => command.Transaction = value; }

        /// <inheritdoc />
        public void Cancel() => command.Cancel();

        /// <inheritdoc />
        public IDbDataParameter CreateParameter() => command.CreateParameter();

        /// <inheritdoc />
        public int ExecuteNonQuery() => command.ExecuteNonQuery();

        /// <inheritdoc />
        public IDataReader ExecuteReader() => command.ExecuteReader();

        /// <inheritdoc />
        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            if (settings.Engine == DatabaseEngine.SQLite)
            {
                try
                {
                    return command.ExecuteReader(GetBehavior(behavior));
                }
                catch (ArgumentException ex)
                {
                    if (DisableCommandBehaviorOptimizations(behavior, ex))
                    {
                        return command.ExecuteReader(GetBehavior(behavior));
                    }

                    throw;
                }
            }

            return command.ExecuteReader(behavior);
        }

        /// <inheritdoc />
        public object ExecuteScalar() => command.ExecuteScalar();

#if NET_NORMAL || NETSTANDARD2_0
        /// <inheritdoc />
        public Task<int> ExecuteNonQueryAsyc(CancellationToken cancellationToken = default)
        {
            if (IsAsynchronousSupport)
            {
                return dbCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            throw new InvalidOperationException("Async operations require use of a DbConnection or an IDbConnection where .CreateCommand() returns a DbCommand.");
        }

        /// <inheritdoc />
        public Task<object> ExecuteScalarAsync(CancellationToken cancellationToken = default)
        {
            if (IsAsynchronousSupport)
            {
                return dbCommand.ExecuteScalarAsync(cancellationToken);
            }

            throw new InvalidOperationException("Async operations require use of a DbConnection or an IDbConnection where .CreateCommand() returns a DbCommand.");
        }

        /// <inheritdoc />
        public Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default)
        {
            if (IsAsynchronousSupport)
            {
                return dbCommand.ExecuteReaderAsync(cancellationToken);
            }

            throw new InvalidOperationException("Async operations require use of a DbConnection or an IDbConnection where .CreateCommand() returns a DbCommand.");
        }

        /// <inheritdoc />
        public async Task<DbDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken = default)
        {
            if (!IsAsynchronousSupport)
            {
                throw new InvalidOperationException("Async operations require use of a DbConnection or an IDbConnection where .CreateCommand() returns a DbCommand.");
            }

            if (settings.Engine == DatabaseEngine.SQLite)
            {
                try
                {
                    return await dbCommand.ExecuteReaderAsync(GetBehavior(behavior), cancellationToken);
                }
                catch (ArgumentException ex)
                {
                    if (DisableCommandBehaviorOptimizations(behavior, ex))
                    {
                        return await dbCommand.ExecuteReaderAsync(GetBehavior(behavior), cancellationToken);
                    }

                    throw;
                }
            }

            return await dbCommand.ExecuteReaderAsync(behavior, cancellationToken);
        }
#endif

        /// <inheritdoc />
        public void Prepare() => command.Prepare();

        /// <inheritdoc />
        public void Dispose() => command.Dispose();

        // disable single result by default; prevents errors AFTER the select being detected properly
        private const CommandBehavior DefaultAllowedCommandBehaviors = ~CommandBehavior.SingleResult;
        private static CommandBehavior AllowedCommandBehaviors = DefaultAllowedCommandBehaviors;

        private static CommandBehavior GetBehavior(CommandBehavior behavior)
        {
            return behavior & AllowedCommandBehaviors;
        }

        private static bool DisableCommandBehaviorOptimizations(CommandBehavior behavior, ArgumentException ex)
        {
            if (AllowedCommandBehaviors == DefaultAllowedCommandBehaviors
                && (behavior & (CommandBehavior.SingleResult | CommandBehavior.SingleRow)) != 0)
            {
                if (ex.Message.Contains(nameof(CommandBehavior.SingleResult))
                    || ex.Message.Contains(nameof(CommandBehavior.SingleRow)))
                {
                    AllowedCommandBehaviors &= ~(CommandBehavior.SingleResult | CommandBehavior.SingleRow);

                    return true;
                }
            }

            return false;
        }
    }
}
