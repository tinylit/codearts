using System;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// <see cref="Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal}})"/>
    /// <seealso cref="Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal?}})"/>
    /// <seealso cref="Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, double}})"/>
    /// <seealso cref="Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, double?}})"/>
    /// <seealso cref="Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, float}})"/>
    /// <seealso cref="Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, float?}})"/>
    /// <seealso cref="Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, long}})"/>
    /// <seealso cref="Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, long?}})"/>
    /// <seealso cref="Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, int}})"/>
    /// <seealso cref="Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, int?}})"/>
    /// </summary>
    public class AverageVisitor : BaseVisitor
    {
        const string FnName = "AVG";

        private readonly SelectVisitor visitor;

        /// <inheritdoc />
        public AverageVisitor(SelectVisitor visitor) : base(visitor, false)
        {
            this.visitor = visitor;
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node)
            => node.Method.DeclaringType == Types.Queryable && node.Method.Name == MethodCall.Average && node.Arguments.Count == 2;

        /// <inheritdoc />
        protected override void StartupCore(MethodCallExpression node)
        {
            writer.Select();

            writer.Write(FnName);
            writer.OpenBrace();

            Workflow(() =>
            {
                visitor.Visit(node.Arguments[1]);

                writer.CloseBrace();

                writer.From();

                var tableInfo = MakeTableInfo(node.Arguments[0].Type);

                var prefix = GetEntryAlias(tableInfo.TableType, string.Empty);

                writer.NameWhiteSpace(tableInfo.TableName, prefix);

            }, () => visitor.Visit(node.Arguments[0]));
        }
    }
}
