#if NET_CORE
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.Linq;
using System.Reflection;
using static System.Linq.Expressions.Expression;
using System.Threading.Tasks;
using System.Threading;
using CodeArts.Runtime;
using System.Linq.Expressions;

namespace CodeArts.Db.EntityFramework
{
    /// <summary>
    /// 数据指令。
    /// </summary>
    public class DbRelationalCommand : RelationalCommand
    {
        private readonly RelationalCommandBuilderDependencies dependencies;
        private static readonly MethodInfo methodInfo = typeof(AccessorExtensions).GetMethod(nameof(AccessorExtensions.GetService));
        private static readonly ConcurrentDictionary<Type, Func<IInfrastructure<IServiceProvider>, IRelationalConnection>> Connections = new ConcurrentDictionary<Type, Func<IInfrastructure<IServiceProvider>, IRelationalConnection>>();

        /// <summary>
        /// inheritdoc.
        /// </summary>
        public DbRelationalCommand(RelationalCommandBuilderDependencies dependencies, string commandText, IReadOnlyList<IRelationalParameter> parameters) : base(dependencies, commandText, parameters)
        {
            this.dependencies = dependencies;
        }

        private static RelationalCommandParameterObject GetOrClone(RelationalCommandParameterObject parameterObject)
        {
            if (parameterObject.Connection.CurrentTransaction is null)
            {
                var connectionFactory = Connections.GetOrAdd(parameterObject.Connection.GetType(), connectionType =>
                {
                    var contextArg = Parameter(typeof(IInfrastructure<IServiceProvider>), "context");

                    var typeItem = TypeItem.Get(connectionType);

                    var ctorItem = typeItem.ConstructorStores
                     .OrderBy(x => x.ParameterStores.Count)
                     .First();

                    var list = new List<Expression>(ctorItem.ParameterStores.Count);

                    ctorItem.ParameterStores.ForEach(x =>
                    {
                        list.Add(Call(methodInfo.MakeGenericMethod(x.ParameterType), contextArg));
                    });

                    var bodyEx = New(ctorItem.Member, list);

                    var lambdaEx = Lambda<Func<IInfrastructure<IServiceProvider>, IRelationalConnection>>(bodyEx, contextArg);

                    return lambdaEx.Compile();
                });

                var connection = connectionFactory.Invoke(parameterObject.Context);

#if NETSTANDARD2_1
                return new RelationalCommandParameterObject(connection, parameterObject.ParameterValues, parameterObject.ReaderColumns, parameterObject.Context, parameterObject.Logger, parameterObject.DetailedErrorsEnabled);
#else
                return new RelationalCommandParameterObject(connection, parameterObject.ParameterValues, parameterObject.ReaderColumns, parameterObject.Context, parameterObject.Logger);
#endif
            }

            return parameterObject;
        }

        /// <summary>
        /// inheritdoc.
        /// </summary>
        public override object ExecuteScalar(RelationalCommandParameterObject parameterObject) => base.ExecuteScalar(GetOrClone(parameterObject));

        /// <summary>
        /// inheritdoc.
        /// </summary>
        public override Task<object> ExecuteScalarAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken = default) => base.ExecuteScalarAsync(GetOrClone(parameterObject), cancellationToken);

        /// <summary>
        /// inheritdoc.
        /// </summary>
        public override RelationalDataReader ExecuteReader(RelationalCommandParameterObject parameterObject)
            => base.ExecuteReader(GetOrClone(parameterObject));

        /// <summary>
        /// inheritdoc.
        /// </summary>
        public override Task<RelationalDataReader> ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken = default)
            => base.ExecuteReaderAsync(GetOrClone(parameterObject), cancellationToken);
    }
}
#endif