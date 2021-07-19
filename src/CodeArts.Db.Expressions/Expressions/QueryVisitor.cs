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
                    if (HasDefaultValue)
                    {
                        throw new NotSupportedException($"函数“{node.Method.Name}”仅在表达式链最多只能出现一次！");
                    }

                    if (node.Arguments.Count > 1)
                    {
                        DefaultValue = node.Arguments[1].GetValueFromExpression();
                    }

                    HasDefaultValue = true;

                    Visit(node.Arguments[0]);

                    break;
                case MethodCall.Min:
                case MethodCall.Max:
                case MethodCall.Average:
                    Required = true;

                    base.VisitCore(node);

                    if (node.Arguments.Count == 1)
                    {
                        break;
                    }

                    if (HasDefaultValue)
                    {
                        if (DefaultValue is null)
                        {
                            if (node.Type.IsValueType && !node.Type.IsNullable())
                            {
                                throw new NotSupportedException($"表达式“{node}”不能从默认值“null”中获取数据!");
                            }
                        }
                        else
                        {
                            var argType = node.Arguments[0].Type.GetGenericArguments()[0];//? 获取泛型参数。
                            var defaultType = DefaultValue.GetType();

                            var parameterExp = Expression.Parameter(argType);

                            //? 获取表达式值。
                            var expression = new MyExpressionVisitor(parameterExp)
                                        .Visit(node.Arguments[1]);

                            if (argType == defaultType || defaultType.IsAssignableFrom(argType))
                            {
                                DefaultValue = expression.GetValueFromExpression(DefaultValue);
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
                case MethodCall.Single:
                case MethodCall.ElementAt:
                    Required = true;
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

                    if (TimeOut.HasValue)
                    {
                        timeOut += TimeOut.Value;
                    }

                    TimeOut = new int?(timeOut);

                    Visit(node.Arguments[0]);

                    break;

                case MethodCall.NoResultError:

                    if (!Required)
                    {
                        throw new NotSupportedException($"函数“{node.Method.Name}”仅在表达式链以“Min”、“Max”、“Average”、“Last”、“First”、“Single”或“ElementAt”结尾时，可用！");
                    }

                    var valueObj = node.Arguments[1].GetValueFromExpression();

                    if (valueObj is string text)
                    {
                        MissingDataError = text;
                    }

                    Visit(node.Arguments[0]);

                    break;
                default:
                    base.VisitOfLts(node);

                    break;
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
        /// 执行超时时间。
        /// </summary>
        public int? TimeOut { private set; get; }
        /// <summary>
        /// 是否必须。
        /// </summary>
        public bool Required { private set; get; }
        /// <summary>
        /// 有默认值。
        /// </summary>
        public bool HasDefaultValue { private set; get; }
        /// <summary>
        /// 默认值。
        /// </summary>
        public object DefaultValue { private set; get; }
        /// <summary>
        /// 未找到数据异常。
        /// </summary>
        public string MissingDataError { private set; get; }
    }
}
