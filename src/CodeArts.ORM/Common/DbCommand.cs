using System;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
#if NET_NORMAL
using System.Threading;
using System.Threading.Tasks;
#endif

namespace CodeArts.ORM.Common
{
    /// <summary>
    /// 数据库命令。
    /// </summary>
    public class DbCommand : IDbCommand, IDisposable
    {
        private readonly IDbCommand command;
        private readonly ISQLCorrectSimSettings settings;

        // look for ? / @ / :
        private static readonly Regex smellsLikeOleDb = new Regex(@"(?<![\p{L}\p{N}@_])[?:@]([\p{L}\p{N}_][\p{L}\p{N}@_]*)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

#if NET_NORMAL
        private readonly System.Data.Common.DbCommand dbCommand;

        /// <summary>
        /// 构造函数。
        /// </summary>
        internal DbCommand(IDbCommand command, ISQLCorrectSimSettings settings)
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
        internal DbCommand(System.Data.Common.DbCommand dbCommand, ISQLCorrectSimSettings settings)
        {
            this.dbCommand = dbCommand ?? throw new ArgumentNullException(nameof(dbCommand));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            IsAsynchronousSupport = true;
            this.command = dbCommand;
        }

        /// <summary>
        /// 是否支持异步。
        /// </summary>
        public bool IsAsynchronousSupport { get; }
#else
        /// <summary>
        /// 构造函数。
        /// </summary>
        internal DbCommand(IDbCommand command, ISQLCorrectSimSettings settings)
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
        public string CommandText { get => command.CommandText; set => command.CommandText = smellsLikeOleDb.Replace(value, m => settings.ParamterName(m.Groups[1].Value)); }

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
            if (settings.Engine == DatabaseEngine.SQLite && (behavior & CommandBehavior.SingleResult) == CommandBehavior.SingleResult)
            {
                behavior &= ~CommandBehavior.SingleResult;
            }

            return command.ExecuteReader(behavior);
        }
        /// <inheritdoc />
        public object ExecuteScalar() => command.ExecuteScalar();

#if NET_NORMAL
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
        public Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
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
        public Task<DbDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken = default)
        {
            if (settings.Engine == DatabaseEngine.SQLite && (behavior & CommandBehavior.SingleResult) == CommandBehavior.SingleResult)
            {
                behavior &= ~CommandBehavior.SingleResult;
            }

            if (IsAsynchronousSupport)
            {
                return dbCommand.ExecuteReaderAsync(behavior, cancellationToken);
            }

            throw new InvalidOperationException("Async operations require use of a DbConnection or an IDbConnection where .CreateCommand() returns a DbCommand.");
        }
#endif

        /// <inheritdoc />
        public void Prepare() => command.Prepare();

        /// <inheritdoc />
        public void Dispose() => command.Dispose();
    }
}
