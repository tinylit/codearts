using CodeArts.Db.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// <see cref="Queryable.GroupBy{TSource, TKey}(IQueryable{TSource}, Expression{Func{TSource, TKey}})"/>
    /// <seealso cref="Enumerable.Count{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
    /// <seealso cref="Enumerable.LongCount{TSource}(IEnumerable{TSource})"/>
    /// <seealso cref="Enumerable.Where{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
    /// </summary>
    public class GroupByLinqVisitor : CoreVisitor
    {
        private static readonly DateTime UtcBase = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        /// <inheritdoc />
        public GroupByLinqVisitor(BaseVisitor visitor) : base(visitor, false, ConditionType.And)
        {
        }

        /// <inheritdoc />
        protected override void StartupCore(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.Count when node.Arguments.Count == 2:
                case MethodCall.LongCount when node.Arguments.Count == 2:
                    writer.Write("COUNT");
                    writer.OpenBrace();
                    writer.Write("CASE WHEN ");

                    VisitCondition(node);

                    writer.Write(" THEN 1 ELSE 0 END");
                    writer.CloseBrace();
                    break;
                case MethodCall.Count when node.Arguments[0].NodeType == ExpressionType.Parameter:
                case MethodCall.LongCount when node.Arguments[0].NodeType == ExpressionType.Parameter:

                    writer.Write("COUNT(1)");

                    break;
                case MethodCall.Count:
                case MethodCall.LongCount:

                    writer.Write("COUNT");
                    writer.OpenBrace();
                    writer.Write("CASE WHEN ");

                    Visit(node.Arguments[0]);

                    writer.Write(" THEN 1 ELSE 0 END");
                    writer.CloseBrace();

                    break;
                case MethodCall.Sum when node.Arguments.Count == 2 && node.Arguments[0].NodeType == ExpressionType.Parameter:
                case MethodCall.Max when node.Arguments.Count == 2 && node.Arguments[0].NodeType == ExpressionType.Parameter:
                case MethodCall.Min when node.Arguments.Count == 2 && node.Arguments[0].NodeType == ExpressionType.Parameter:
                case MethodCall.Average when node.Arguments.Count == 2 && node.Arguments[0].NodeType == ExpressionType.Parameter:

                    writer.Write(node.Method.Name == MethodCall.Average ? "AVG" : node.Method.Name.ToUpper());

                    writer.OpenBrace();

                    Visit(node.Arguments[1]);

                    writer.CloseBrace();

                    break;
                case MethodCall.Sum when node.Arguments.Count == 2:
                case MethodCall.Max when node.Arguments.Count == 2:
                case MethodCall.Min when node.Arguments.Count == 2:
                case MethodCall.Average when node.Arguments.Count == 2:

                    writer.Write(node.Method.Name == MethodCall.Average ? "AVG" : node.Method.Name.ToUpper());

                    writer.OpenBrace();
                    writer.Write("CASE WHEN ");

                    Visit(node.Arguments[0]);

                    writer.Write(" THEN ");

                    Visit(node.Arguments[1]);

                    writer.Write(" ELSE ");

                    if (node.Method.ReturnType.IsNullable())
                    {
                        writer.Null();
                    }
                    else if (node.Method.ReturnType == Types.DateTime)
                    {
                        writer.Parameter("date_utc_base", UtcBase);
                    }
                    else if (node.Method.ReturnType == Types.DateTimeOffset)
                    {
                        writer.Parameter("date_offset_utc_base", new DateTimeOffset(UtcBase));
                    }
                    else
                    {
                        writer.Parameter($"{node.Method.ReturnType.Name.ToUrlCase()}_default", Emptyable.Empty(node.Method.ReturnType));
                    }

                    writer.Write(" END");

                    writer.CloseBrace();

                    break;
                case MethodCall.Any when node.Arguments.Count == 1 && node.Arguments[0].NodeType == ExpressionType.Parameter:
                    writer.BooleanTrue();
                    break;
                case MethodCall.Any when node.Arguments.Count == 1:
                    writer.Write("CASE ");

                    writer.Write("MAX");

                    writer.OpenBrace();
                    writer.Write("CASE WHEN ");

                    Visit(node.Arguments[0]);

                    writer.Write(" THEN 1 ELSE 0 END");

                    writer.CloseBrace();

                    writer.Write(" WHEN 1 THEN ");

                    writer.BooleanTrue();

                    writer.Write(" ELSE ");

                    writer.BooleanFalse();

                    writer.Write(" END");
                    break;

                case MethodCall.Any when node.Arguments.Count == 2:
                case MethodCall.All when node.Arguments.Count == 2:

                    writer.Write("CASE ");

                    writer.Write(node.Method.Name == MethodCall.Any ? "MAX" : "MIN");

                    writer.OpenBrace();
                    writer.Write("CASE WHEN ");

                    VisitCondition(node);

                    writer.Write(" THEN 1 ELSE 0 END");

                    writer.CloseBrace();

                    writer.Write(" WHEN 1 THEN ");

                    writer.BooleanTrue();

                    writer.Write(" ELSE ");

                    writer.BooleanFalse();

                    writer.Write(" END");

                    break;
                case MethodCall.Sum:
                case MethodCall.Max:
                case MethodCall.Min:
                case MethodCall.Average:
                    throw new DSyntaxErrorException($"分组中函数“{node.Method.Name}”需指定计算属性!");
                default:
                    throw new DSyntaxErrorException($"分组中函数“{node.Method.Name}”不被支持!");
            }
        }

        /// <summary>
        /// 条件。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected override internal void VisitCondition(MethodCallExpression node)
        {
            if (node.Arguments[0].NodeType == ExpressionType.Parameter)
            {
                using (var visitor = new WhereVisitor(this))
                {
                    visitor.Startup(node.Arguments[1]);
                }
            }
            else
            {
                base.VisitCondition(node);
            }
        }

        /// <inheritdoc />
        protected override void VisitLinq(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.Where:
                    VisitCondition(node);
                    break;
                default:
                    base.VisitLinq(node);
                    break;
            }
        }
    }
}
