using System;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// <see cref="Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal}})"/>
    /// <seealso cref="Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal?}})"/>
    /// <seealso cref="Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, double}})"/>
    /// <seealso cref="Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, double?}})"/>
    /// <seealso cref="Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, float}})"/>
    /// <seealso cref="Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, float?}})"/>
    /// <seealso cref="Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, long}})"/>
    /// <seealso cref="Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, long?}})"/>
    /// <seealso cref="Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, int}})"/>
    /// <seealso cref="Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, int?}})"/>
    /// </summary>
    public class SumVisitor : BaseVisitor
    {
        const string FnName = "SUM";

        private readonly SelectVisitor visitor;

        /// <inheritdoc />
        public SumVisitor(SelectVisitor visitor) : base(visitor, false)
        {
            this.visitor = visitor;
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node)
            => node.Method.DeclaringType == Types.Queryable && node.Method.Name == MethodCall.Sum && node.Arguments.Count == 2;

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

            }, () => visitor.Visit(node.Arguments[0]));
        }
    }
}
