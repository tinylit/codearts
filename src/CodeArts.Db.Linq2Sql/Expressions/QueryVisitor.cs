using CodeArts.Db.Exceptions;
using System;
using System.Collections.Generic;
#if NET40
using System.Collections.ObjectModel;
#endif
using System.Linq.Expressions;
using System.Reflection;

namespace CodeArts.Db.Expressions
{
    /// <summary>
    /// 查询访问器。
    /// </summary>
    public class QueryVisitor : SelectVisitor, IQueryVisitor
    {
        private bool buildWatchSql = false;

        private Action<CommandSql> watchSql;

        private int? timeOut;

        private bool hasDefaultValue;

        private object defaultValue;

        private string mssingDataError;

        private RowStyle rowStyle = RowStyle.None;

        private readonly ICustomVisitorList visitors;

        private QueryVisitor(BaseVisitor baseVisitor) : base(baseVisitor)
        {
        }

        /// <inheritdoc />
        public QueryVisitor(ISQLCorrectSettings settings, ICustomVisitorList visitors) : base(settings)
        {
            this.visitors = visitors;
        }

        /// <inheritdoc />
        public override SelectVisitor CreateInstance(BaseVisitor baseVisitor) => new QueryVisitor(baseVisitor);

        /// <inheritdoc />
        public override void Startup(Expression node)
        {
            if (node is ConstantExpression constant)
            {
                VisitConstant(constant);
            }
            else
            {
                base.Startup(node);
            }
        }

        /// <inheritdoc />
        protected override void VisitCore(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.DefaultIfEmpty:
                    if (hasDefaultValue)
                    {
                        throw new NotSupportedException($"函数“{node.Method.Name}”仅在表达式链最多只能出现一次！");
                    }

                    if (node.Arguments.Count > 1)
                    {
                        defaultValue = node.Arguments[1].GetValueFromExpression();
                    }

                    hasDefaultValue = true;

                    Visit(node.Arguments[0]);

                    break;
                case MethodCall.Min:
                case MethodCall.Max:
                case MethodCall.Average:

                    rowStyle = RowStyle.Single;

                    base.VisitCore(node);

                    if (node.Arguments.Count == 1)
                    {
                        break;
                    }

                    if (hasDefaultValue)
                    {
                        if (defaultValue is null)
                        {
                            if (node.Type.IsValueType && !node.Type.IsNullable())
                            {
                                throw new NotSupportedException($"表达式“{node}”不能从默认值“null”中获取数据!");
                            }
                        }
                        else
                        {
                            var argType = node.Arguments[0].Type.GetGenericArguments()[0];//? 获取泛型参数。
                            var defaultType = defaultValue.GetType();

                            var parameterExp = Expression.Parameter(argType);

                            //? 获取表达式值。
                            var expression = new MyExpressionVisitor(parameterExp)
                                        .Visit(node.Arguments[1]);

                            if (argType == defaultType || defaultType.IsAssignableFrom(argType))
                            {
                                defaultValue = expression.GetValueFromExpression(defaultValue);
                            }
                            else
                            {
                                throw new InvalidCastException($"无法从“{argType}”转换为“{defaultType}”类型!");
                            }
                        }
                    }
                    break;
                case MethodCall.Last:
                case MethodCall.First:
                    rowStyle = RowStyle.First;
                    break;
                case MethodCall.LastOrDefault:
                case MethodCall.FirstOrDefault:
                    rowStyle = RowStyle.FirstOrDefault;
                    break;
                case MethodCall.Single:
                case MethodCall.ElementAt:
                    rowStyle = RowStyle.Single;
                    goto default;
                case MethodCall.SingleOrDefault:
                case MethodCall.ElementAtOrDefault:
                    rowStyle = RowStyle.SingleOrDefault;
                    goto default;
                default:
                    base.VisitCore(node);
                    break;
            }
        }

        /// <inheritdoc />
        protected override void VisitOfLts(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.TimeOut:

                    int timeOut = (int)node.Arguments[1].GetValueFromExpression();

                    if (this.timeOut.HasValue)
                    {
                        this.timeOut += timeOut;
                    }
                    else
                    {
                        this.timeOut = new int?(timeOut);
                    }

                    Visit(node.Arguments[0]);

                    break;
                case MethodCall.WatchSql:
                    buildWatchSql = true;

                    Visit(node.Arguments[1]);

                    buildWatchSql = false;

                    Visit(node.Arguments[0]);

                    break;
                case MethodCall.NoResultError:

                    if (rowStyle == RowStyle.None || rowStyle == RowStyle.FirstOrDefault || rowStyle == RowStyle.SingleOrDefault)
                    {
                        throw new NotSupportedException($"函数“{node.Method.Name}”仅在表达式链以“Min”、“Max”、“Average”、“Last”、“First”、“Single”或“ElementAt”结尾时，可用！");
                    }

                    var valueObj = node.Arguments[1].GetValueFromExpression();

                    if (valueObj is string text)
                    {
                        mssingDataError = text;
                    }

                    Visit(node.Arguments[0]);

                    break;
                default:
                    base.VisitOfLts(node);
                    break;
            }
        }

        /// <inheritdoc />
        protected override void Constant(Type conversionType, object value)
        {
            if (buildWatchSql)
            {
                watchSql = (Action<CommandSql>)value;
            }
            else
            {
                base.Constant(conversionType, value);
            }
        }

        /// <inheritdoc />
        protected override void DefMemberAs(string field, string alias)
        {
            if (field != alias)
            {
                writer.As(alias);
            }
        }

        /// <inheritdoc />
        protected override void DefNewMemberAs(MemberInfo memberInfo, Type memberOfHostType)
        {
            writer.As(memberInfo.Name);
        }

        /// <inheritdoc />
        protected override void DefMemberBindingAs(MemberBinding member, Type memberOfHostType)
        {
            writer.As(member.Member.Name);
        }

        /// <summary>
        /// 获取自定义访问器。
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<ICustomVisitor> GetCustomVisitors() => visitors;

        private class MyExpressionVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression parameter;

            public MyExpressionVisitor(ParameterExpression parameter)
            {
                this.parameter = parameter;
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                return Expression.Lambda(new ReplaceExpressionVisitor(node.Parameters[0], parameter).Visit(node.Body), new ParameterExpression[1] { parameter });
            }
        }

        private class ReplaceExpressionVisitor : ExpressionVisitor
        {
            private readonly Expression _oldExpression;
            private readonly Expression _newExpression;

            public ReplaceExpressionVisitor(Expression oldExpression, Expression newExpression)
            {
                _oldExpression = oldExpression;
                _newExpression = newExpression;
            }
            public override Expression Visit(Expression node)
            {
                if (_oldExpression == node)
                {
                    return base.Visit(_newExpression);
                }

                return base.Visit(node);
            }
        }

        /// <summary>
        /// 获取执行语句。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <returns></returns>
        protected virtual CommandSql<T> ToSql<T>()
        {
            T defaultValue = default;

            if (hasDefaultValue)
            {
                if (this.defaultValue is T value)
                {
                    defaultValue = value;
                }
                else if (this.defaultValue != null)
                {
                    throw new DSyntaxErrorException($"查询结果类型({typeof(T)})和指定的默认值类型({this.defaultValue.GetType()})无法进行默认转换!");
                }
            }

            return new CommandSql<T>(writer.ToSQL(), writer.Parameters, rowStyle, hasDefaultValue, defaultValue, timeOut, mssingDataError);
        }

        /// <summary>
        /// 获取执行语句。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <returns></returns>
        public CommandSql<T> ToSQL<T>()
        {
            var sql = ToSql<T>();

            watchSql?.Invoke(sql);

            return sql;
        }
    }
}
