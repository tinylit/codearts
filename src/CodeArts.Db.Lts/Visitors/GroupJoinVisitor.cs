using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// <see cref="Queryable.GroupJoin{TOuter, TInner, TKey, TResult}(IQueryable{TOuter}, IEnumerable{TInner}, Expression{Func{TOuter, TKey}}, Expression{Func{TInner, TKey}}, Expression{Func{TOuter, IEnumerable{TInner}, TResult}})"/>
    /// </summary>
    public class GroupJoinVisitor : JoinVisitor
    {
        private readonly bool useLeftJoin;

        /// <inheritdoc />
        public GroupJoinVisitor(SelectVisitor visitor, ParameterExpression parameter, bool useLeftJoin) : base(visitor)
        {
            AnalysisAlias(parameter);

            this.useLeftJoin = useLeftJoin;
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node)
            => node.Method.DeclaringType == Types.Queryable && node.Method.Name == MethodCall.GroupJoin && node.Arguments.Count == 5;

        /// <inheritdoc />
        protected override void JoinMode()
        {
            if (useLeftJoin)
            {
                writer.GroupJoin();
            }
            else
            {
                base.JoinMode();
            }
        }
    }
}
