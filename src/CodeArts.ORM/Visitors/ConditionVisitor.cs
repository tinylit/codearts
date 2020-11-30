using CodeArts.ORM.Exceptions;
using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.ORM.Visitors
{
    /// <summary>
    /// 条件访问器。
    /// </summary>
    public class ConditionVisitor : BaseVisitor
    {
        #region 匿名内部类

        /// <summary>
        /// 智能开关。
        /// </summary>
        private class SmartSwitch
        {
            public bool IsFirst { get; private set; }

            public Action FirstAction { get; }

            public Action UnFirstAction { get; }

            public SmartSwitch(Action firstAction, Action unFirstAction)
            {
                IsFirst = true;
                FirstAction = firstAction;
                UnFirstAction = unFirstAction;
            }

            public void UnWrap(Action action)
            {
                bool isFirst = IsFirst;

                IsFirst = true;

                action?.Invoke();

                IsFirst = isFirst;
            }

            public void Execute()
            {
                if (IsFirst)
                {
                    IsFirst = false;
                    FirstAction?.Invoke();
                    return;
                }
                UnFirstAction?.Invoke();
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
        #endregion

        /// <summary>
        /// 条件。
        /// </summary>
        private readonly SmartSwitch whereSwitch;

        /// <summary>
        /// 在条件中。
        /// </summary>
        private volatile bool isInTheCondition = false;

        /// <summary>
        /// 条件的两端。
        /// </summary>
        private bool isConditionBalance = false;

        /// <summary>
        /// 忽略可空类型。
        /// </summary>
        private bool ignoreNullable = false;

        private readonly bool hasVisitor = false;

        /// <inheritdoc />
        public ConditionVisitor(ISQLCorrectSettings settings) : base(settings)
        {
            whereSwitch = new SmartSwitch(this.writer.Where, this.writer.And);
        }

        /// <inheritdoc />
        public ConditionVisitor(BaseVisitor visitor, bool isNewWriter = false) : base(visitor, isNewWriter)
        {
            hasVisitor = true;

            whereSwitch = new SmartSwitch(this.writer.Where, this.writer.And);
        }

        /// <inheritdoc />
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var declaringType = node.Method.DeclaringType;

            if (declaringType == typeof(Queryable))
            {
                return VisitOfQueryable(node);
            }

            if (declaringType == typeof(RepositoryExtentions))
            {
                return VisitOfSelect(node);
            }

            if (declaringType == typeof(Enumerable))
            {
                return VisitOfEnumerable(node);
            }

            if (declaringType == typeof(string))
            {
                return VisitOfString(node);
            }

            if (typeof(IEnumerable).IsAssignableFrom(declaringType))
            {
                return VisitOfIEnumerable(node);
            }

            return base.VisitMethodCall(node);
        }

        /// <summary>
        ///  System.Linq.Queryable 的函数。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        protected override Expression VisitOfQueryable(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.Where:
                case MethodCall.TakeWhile:
                    return VisitCondition(node);
                case MethodCall.SkipWhile:
                    return writer.ReverseCondition(() => VisitCondition(node));
                default:
                    return base.VisitOfQueryable(node);
            }
        }

        /// <summary>
        /// System.Linq.Enumerable 的函数。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected virtual Expression VisitOfEnumerable(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.Any:
                    return VisitOfEnumerableAny(node);
                case MethodCall.All:
                    goto default;
                case MethodCall.Contains:
                    return VisitOfEnumerableContains(node);
                case "get_Item":
                case MethodCall.First:
                case MethodCall.FirstOrDefault:
                case MethodCall.Last:
                case MethodCall.LastOrDefault:
                case MethodCall.Single:
                case MethodCall.SingleOrDefault:
                case MethodCall.ElementAt:
                case MethodCall.ElementAtOrDefault:
                case MethodCall.Count:
                case MethodCall.LongCount:
                case MethodCall.Max:
                case MethodCall.Min:
                case MethodCall.Average:
                    writer.Parameter(node.GetValueFromExpression());
                    return node;
                default:
                    return VisitByCustom(node);
            }
        }

        /// <summary>
        /// System.Collections.IEnumerable 的函数。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected virtual Expression VisitOfIEnumerable(MethodCallExpression node) => VisitOfEnumerable(node);

        /// <summary>
        /// System.String 的函数。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected virtual Expression VisitOfString(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.Contains:
                case MethodCall.EndsWith:
                case MethodCall.StartsWith:
                    return VisitLike(node);
                case MethodCall.IsNullOrEmpty:
                    return VisitIsEmpty(node);
                case MethodCall.Replace:
                    return VisitToReplace(node);
                case MethodCall.Substring:
                    return VisitToSubstring(node);
                case MethodCall.ToUpper:
                case MethodCall.ToLower:
                    return VisitToCaseConversion(node);
                case MethodCall.Trim:
                case MethodCall.TrimEnd:
                case MethodCall.TrimStart:
                    return VisitToTrim(node);
                case MethodCall.IndexOf when node.Arguments.Count > 1:
                    return VisitByIndexOfWithLimit(node);
                case MethodCall.IndexOf:
                    return VisitByIndexOf(node);
                default:
                    return VisitByCustom(node);
            }
        }

        /// <summary>
        /// System.Linq.Enumerable 的Any函数。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected virtual Expression VisitOfEnumerableAny(MethodCallExpression node)
        {
            var enumerable = (IEnumerable)(node.Object ?? node.Arguments[0]).GetValueFromExpression();

            var enumerator = enumerable.GetEnumerator();

            int index = node.Object is null ? 1 : 0;

            if (node.Arguments.Count == index)
            {
                if (enumerator.MoveNext() ^ writer.IsReverseCondition)
                {
                    return node;
                }

                BooleanFalse(true);

                return node;
            }

            var lambda = node.Arguments[index] as LambdaExpression;

            var parameterExp = lambda.Parameters[0];

            void VisitObject(object value)
            {
                var constantExp = Expression.Constant(value, parameterExp.Type);

                base.Visit(new ReplaceExpressionVisitor(parameterExp, constantExp)
                    .Visit(lambda.Body));
            }

            if (enumerator.MoveNext())
            {
                if (enumerator.Current is null)
                {
                    throw new ArgumentNullException(parameterExp.Name);
                }

                writer.OpenBrace();

                VisitObject(enumerator.Current);

                while (enumerator.MoveNext())
                {
                    if (enumerator.Current is null)
                    {
                        throw new ArgumentNullException(parameterExp.Name);
                    }

                    writer.Or();

                    VisitObject(enumerator.Current);
                }

                writer.CloseBrace();

                return node;
            }

            BooleanFalse(false);

            return node;
        }

        /// <summary>
        /// System.Linq.Enumerable 的Contains函数。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected virtual Expression VisitOfEnumerableContains(MethodCallExpression node)
        {
            int index = node.Object is null ? 1 : 0;

            Workflow(whereIsNotEmpty =>
            {
                if (whereIsNotEmpty)
                {
                    Visit(node.Arguments[index]);
                }
                else
                {
                    BooleanFalse(false);
                }

            }, () =>
              {
                  var enumerable = (IEnumerable)(node.Object ?? node.Arguments[0]).GetValueFromExpression();
                  var enumerator = enumerable.GetEnumerator();

                  if (enumerator.MoveNext())
                  {
                      int parameterCount = 0;

                      writer.Contains();
                      writer.OpenBrace();
                      writer.Parameter(enumerator.Current);

                      while (enumerator.MoveNext())
                      {
                          if (parameterCount < 256)
                          {
                              writer.Delimiter();
                          }
                          else
                          {
                              parameterCount = 0;

                              writer.CloseBrace();
                              writer.WhiteSpace();
                              writer.Write("OR");
                              writer.WhiteSpace();

                              base.Visit(node.Arguments[index]);

                              writer.Contains();
                              writer.OpenBrace();
                          }

                          writer.Parameter(enumerator.Current);
                      }

                      writer.CloseBrace();
                  }
              });

            return node;
        }

        /// <summary>
        /// System.String 的 Contains、StartsWith、EndsWith函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression VisitLike(MethodCallExpression node)
        {
            if (node.Arguments.Count > 1)
            {
                throw new DSyntaxErrorException($"仅支持参数类型为“System.String”的({node.Method.Name}(String))方法。");
            }

            if (IsPlainVariable(node.Arguments[0]))
            {
                return VisitLikeByVariable(node);
            }

            return VisitLikeByExpression(node);
        }

        /// <summary>
        /// System.String 的 Contains、StartsWith、EndsWith函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression VisitLikeByVariable(MethodCallExpression node)
        {
            var objExp = node.Arguments[0];

            var value = objExp.GetValueFromExpression();

            if (value is null)
            {
                return node;
            }

            if (!(value is string text))
            {
                throw new DSyntaxErrorException($"仅支持参数类型为“System.String”的({node.Method.Name})方法。");
            }

            base.Visit(node.Object);

            if (text.Length == 0)
            {
                writer.ReverseCondition(writer.IsNull);
            }
            else
            {
                writer.Like();

                if (node.Method.Name == MethodCall.EndsWith || node.Method.Name == MethodCall.Contains)
                {
                    text = "%" + text;
                }

                if (node.Method.Name == MethodCall.StartsWith || node.Method.Name == MethodCall.Contains)
                {
                    text += "%";
                }

                if (objExp is MemberExpression member)
                {
                    writer.Parameter(member.Member.Name, text);
                }
                else
                {
                    writer.Parameter(text);
                }
            }

            return node;
        }

        /// <summary>
        /// System.String 的 Contains、StartsWith、EndsWith函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression VisitLikeByExpression(MethodCallExpression node)
        {
            base.Visit(node.Object);

            writer.Like();

            if (settings.Engine == DatabaseEngine.MySQL)
            {
                writer.Write("CONCAT");
                writer.OpenBrace();

                if (node.Method.Name == MethodCall.StartsWith || node.Method.Name == MethodCall.Contains)
                {
                    writer.Write("'%'");
                    writer.Delimiter();
                }
            }

            base.Visit(node.Arguments[0]);

            if (settings.Engine == DatabaseEngine.MySQL)
            {
                if (node.Method.Name == MethodCall.EndsWith || node.Method.Name == MethodCall.Contains)
                {
                    writer.Delimiter();
                    writer.Write("'%'");
                }

                writer.CloseBrace();
            }

            return node;
        }

        /// <summary>
        /// System.String 的 IsNullOrEmpty 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression VisitIsEmpty(MethodCallExpression node)
        {
            var objExp = node.Arguments.Count > 0 ? node.Arguments[0] : node.Object;

            writer.OpenBrace();

            base.Visit(objExp);

            writer.IsNull();

            writer.Or();

            base.Visit(objExp);

            writer.Equal();
            writer.EmptyString();
            writer.CloseBrace();

            return node;
        }

        /// <summary>
        /// System.String 的 Replace 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression VisitToReplace(MethodCallExpression node)
        {
            writer.Write(node.Method.Name);
            writer.OpenBrace();

            base.Visit(node.Object);

            foreach (Expression item in node.Arguments)
            {
                writer.Delimiter();

                base.Visit(item);
            }

            writer.CloseBrace();

            return node;
        }

        /// <summary>
        /// System.String 的 Substring 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression VisitToSubstring(MethodCallExpression node)
        {
            writer.Write("CASE WHEN ");

            base.Visit(node.Object);

            writer.Write(" IS NULL OR ");

            writer.OpenBrace();
            writer.LengthMethod();
            writer.OpenBrace();

            base.Visit(node.Object);

            writer.CloseBrace();
            writer.Write(" - ");

            base.Visit(node.Arguments[0]);

            writer.CloseBrace();

            writer.Write(" < 1");

            writer.Write(" THEN ");
            writer.Parameter(string.Empty);
            writer.Write(" ELSE ");

            writer.SubstringMethod();
            writer.OpenBrace();

            base.Visit(node.Object);

            writer.Delimiter();

            if (IsPlainVariable(node.Arguments[0]))
            {
                writer.Parameter((int)node.Arguments[0].GetValueFromExpression() + 1);
            }
            else
            {
                base.Visit(node.Arguments[0]);

                writer.Write(" + 1");
            }

            writer.Delimiter();

            if (node.Arguments.Count > 1)
            {
                base.Visit(node.Arguments[1]);
            }
            else
            {
                writer.LengthMethod();
                writer.OpenBrace();

                base.Visit(node.Object);

                writer.CloseBrace();
                writer.Write(" - ");

                base.Visit(node.Arguments[0]);
            }

            writer.CloseBrace();

            writer.Write(" END");

            return node;
        }

        /// <summary>
        /// System.String 的 ToUpper、ToLower 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression VisitToCaseConversion(MethodCallExpression node)
        {
            writer.Write(node.Method.Name.Substring(2));
            writer.OpenBrace();

            base.Visit(node.Object);

            writer.CloseBrace();

            return node;
        }

        /// <summary>
        /// System.String 的 Trim、TrimStart、TrimEnd 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression VisitToTrim(MethodCallExpression node)
        {
            if (node.Method.Name == MethodCall.TrimStart || node.Method.Name == MethodCall.Trim)
            {
                writer.Write("LTRIM");
                writer.OpenBrace();
            }

            if (node.Method.Name == MethodCall.TrimEnd || node.Method.Name == MethodCall.Trim)
            {
                writer.Write("RTRIM");
                writer.OpenBrace();
            }

            base.Visit(node.Object);

            if (node.Method.Name == MethodCall.TrimStart || node.Method.Name == MethodCall.Trim)
            {
                writer.CloseBrace();
            }

            if (node.Method.Name == MethodCall.TrimEnd || node.Method.Name == MethodCall.Trim)
            {
                writer.CloseBrace();
            }

            return node;
        }

        /// <summary>
        /// System.String 的 IndexOf(int) 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression VisitByIndexOf(MethodCallExpression node)
        {
            var indexOfExp = node.Arguments[0];

            if (IsPlainVariable(indexOfExp, true))
            {
                var value = indexOfExp.GetValueFromExpression();

                if (value is null)
                {
                    writer.Parameter(-1);

                    return node;
                }

                writer.OpenBrace();

                writer.Write("CASE WHEN ");

                base.Visit(node.Object);
            }
            else
            {
                writer.OpenBrace();

                writer.Write("CASE WHEN ");

                base.Visit(node.Object);

                writer.Write(" IS NULL OR ");

                base.Visit(node.Arguments[0]);
            }

            writer.Write(" IS NULL THEN -1 ELSE ");

            writer.IndexOfMethod();
            writer.OpenBrace();

            base.Visit(settings.IndexOfSwapPlaces ? indexOfExp : node.Object);

            writer.Delimiter();

            base.Visit(settings.IndexOfSwapPlaces ? node.Object : indexOfExp);

            writer.CloseBrace();

            writer.Write(" - 1 END");

            writer.CloseBrace();

            return node;
        }

        /// <summary>
        /// System.String 的 IndexOf(int,int) 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression VisitByIndexOfWithLimit(MethodCallExpression node)
        {
            var objExp = node.Arguments[1];

            var isVariable = IsPlainVariable(objExp);

            var indexStart = isVariable ? (int)objExp.GetValueFromExpression() : -1;

            writer.Write("CASE WHEN ");

            base.Visit(settings.IndexOfSwapPlaces ? node.Arguments[0] : node.Object);

            writer.Write(" IS NULL OR ");

            base.Visit(settings.IndexOfSwapPlaces ? node.Object : node.Arguments[0]);

            writer.Write(" IS NULL ");

            if (node.Arguments.Count > 2)
            {
                writer.Write(" OR ");
                writer.IndexOfMethod();
                writer.OpenBrace();

                base.Visit(settings.IndexOfSwapPlaces ? node.Arguments[0] : node.Object);

                writer.Delimiter();

                base.Visit(settings.IndexOfSwapPlaces ? node.Object : node.Arguments[0]);

                writer.Delimiter();

                if (isVariable)
                {
                    writer.Parameter(indexStart + 1);
                }
                else
                {
                    base.Visit(objExp);

                    writer.Write(" + 1");
                }

                writer.CloseBrace();

                writer.Write(" > ");

                if (isVariable)
                {
                    writer.Parameter(indexStart);
                }
                else
                {
                    base.Visit(objExp);
                }

                writer.Write(" + ");

                base.Visit(node.Arguments[2]);
            }

            writer.Write(" THEN -1 ELSE ");

            writer.IndexOfMethod();
            writer.OpenBrace();

            base.Visit(settings.IndexOfSwapPlaces ? node.Arguments[0] : node.Object);

            writer.Delimiter();

            base.Visit(settings.IndexOfSwapPlaces ? node.Object : node.Arguments[0]);

            writer.Delimiter();

            if (isVariable)
            {
                writer.Parameter(indexStart + 1);
            }
            else
            {
                base.Visit(objExp);

                writer.Write(" + 1");
            }

            writer.CloseBrace();

            writer.Write(" - 1 END");

            return node;
        }

        /// <summary>
        /// 条件。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected virtual Expression VisitCondition(MethodCallExpression node)
        {
            base.Visit(node.Arguments[0]);

            Workflow(whereIsNotEmpty =>
            {
                if (whereIsNotEmpty)
                {
                    whereSwitch.Execute();
                }

            }, () =>
            {
                isInTheCondition = true;

                base.Visit(node.Arguments[1]);

                isInTheCondition = false;
            });

            return node;
        }

        /// <summary>
        /// 条件。
        /// </summary>
        /// <param name="whereCore">条件表达式分析部分。</param>
        /// <param name="whereIf">表达式分析结果是否不为空。</param>
        /// <param name="autoConditionRelation">自动建立条件关系。</param>
        /// <returns></returns>
        protected Expression UsingCondition(Func<Expression> whereCore, Action<bool> whereIf = null, bool autoConditionRelation = false)
        {
            Expression node = null;

            Workflow(whereIsNotEmpty =>
            {
                whereIf?.Invoke(whereIsNotEmpty);

                if (whereIsNotEmpty && autoConditionRelation)
                {
                    whereSwitch.Execute();
                }

            }, () =>
            {
                isInTheCondition = true;

                node = whereCore.Invoke();

                isInTheCondition = false;
            });

            return node;
        }

        /// <summary>
        /// 变量成员。
        /// </summary>
        /// <returns></returns>
        protected override Expression VisitMemberIsVariable(MemberExpression node)
        {
            if (ignoreNullable || isConditionBalance)
            {
                var value = node.GetValueFromExpression();

                if (ignoreNullable && value is null)
                {
                    return node;
                }

                if (isConditionBalance)
                {
                    if (value.Equals(!writer.IsReverseCondition))
                    {
                        return node;
                    }

                    try
                    {
                        return base.VisitMemberIsVariable(node);
                    }
                    finally
                    {
                        if (node.IsBoolean())
                        {
                            writer.Equal();

                            writer.BooleanFalse();
                        }
                    }
                }
            }

            return base.VisitMemberIsVariable(node);
        }

        /// <summary>
        /// 成员依赖于参数成员（参数类型是值类型或字符串类型）。
        /// </summary>
        /// <returns></returns>
        protected override Expression VisitMemberIsDependOnParameterTypeIsPlain(MemberExpression node)
        {
            try
            {
                return base.VisitMemberIsDependOnParameterTypeIsPlain(node);
            }
            finally
            {
                if (isConditionBalance && node.IsBoolean())
                {
                    writer.Equal();

                    writer.BooleanTrue();
                }
            }
        }

        /// <summary>
        ///  System.Linq.Queryable 的Any函数。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        protected override Expression VisitOfQueryableAny(MethodCallExpression node)
        {
            if (isInTheCondition || hasVisitor)
            {
                return base.VisitOfQueryableAny(node);
            }

            writer.Write("CASE WHEN ");

            try
            {
                return base.VisitOfQueryableAny(node);
            }
            finally
            {
                writer.Write(" THEN ");
                writer.Parameter("__variable_true", true);
                writer.Write(" ELSE ");
                writer.Parameter("__variable_false", false);
                writer.Write(" END");
            }
        }

        /// <summary>
        ///  System.Linq.Queryable 的All函数。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        protected override Expression VisitOfQueryableAll(MethodCallExpression node)
        {
            if (isInTheCondition || hasVisitor)
            {
                return base.VisitOfQueryableAll(node);
            }

            writer.Write("CASE WHEN ");

            try
            {
                return base.VisitOfQueryableAll(node);
            }
            finally
            {
                writer.Write(" THEN ");
                writer.Parameter("__variable_true", true);
                writer.Write(" ELSE ");
                writer.Parameter("__variable_false", false);
                writer.Write(" END");
            }
        }

        /// <summary>
        /// System.Linq.Enumerable 的Union/Concat/Except/Intersect函数。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected override Expression VisitCombination(MethodCallExpression node)
        {
            if (isInTheCondition)
            {
                writer.OpenBrace();

                try
                {
                    return base.VisitCombination(node);
                }
                finally
                {
                    writer.CloseBrace();
                }
            }

            return base.VisitCombination(node);
        }

        #region 运算

        /// <summary>
        /// 是条件(condition1 And condition2)。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitBinaryIsCondition(BinaryExpression node, bool isAndLike)
        {
            int indexBefore = writer.Length;

            VisitBinaryIsConditionToVisit(node.Left);

            int indexStep = writer.Length;

            VisitBinaryIsConditionToVisit(node.Right);

            if (indexStep > indexBefore && writer.Length > indexStep)
            {
                int index = writer.Length;

                int length = writer.Length;

                int appendAt = writer.AppendAt;

                if (appendAt > -1)
                {
                    indexStep -= (index - appendAt);

                    indexBefore -= (index - appendAt);
                }

                writer.AppendAt = indexBefore;

                writer.OpenBrace();

                writer.AppendAt = indexStep + 1;

                if (isAndLike)
                {
                    writer.And();
                }
                else
                {
                    writer.Or();
                }

                if (appendAt > -1)
                {
                    appendAt += writer.Length - length;
                }

                writer.AppendAt = appendAt;

                writer.CloseBrace();
            }

            return node;
        }

        /// <summary>
        /// 空适配符。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitBinaryIsCoalesce(BinaryExpression node)
        {
            if (IsPlainVariable(node.Left, true))
            {
                var nodeValue = node.Left.GetValueFromExpression();

                if (nodeValue is null)
                {
                    return VisitCheckIfSubconnection(node.Right);
                }

                if (node.Left is MemberExpression memberExp)
                {
                    writer.Parameter(memberExp.Member.Name, nodeValue);
                }
                else
                {
                    writer.Parameter(nodeValue);
                }

                return node;
            }

            writer.OpenBrace();

            writer.Write("CASE WHEN ");

            VisitCheckIfSubconnection(node.Left);

            writer.IsNull();
            writer.Write(" THEN ");

            VisitCheckIfSubconnection(node.Right);

            writer.Write(" ELSE ");

            VisitCheckIfSubconnection(node.Left);

            writer.Write(" END");

            writer.CloseBrace();

            return node;
        }

        /// <summary>
        /// 拼接。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitBinaryIsAdd(BinaryExpression node)
        {
            bool useConcat = (settings.Engine == DatabaseEngine.MySQL || settings.Engine == DatabaseEngine.Oracle) && node.Type == typeof(string);

            int indexBefore = writer.Length;

            VisitCheckIfSubconnection(node.Right);

            int indexStep = writer.Length;

            if (indexStep == indexBefore)
            {
                return node;
            }

            int index = writer.Length;

            int length = writer.Length;

            int appendAt = writer.AppendAt;

            if (appendAt > -1)
            {
                indexBefore -= index - appendAt;
            }

            writer.AppendAt = indexBefore;

            if (useConcat)
            {
                writer.Write("CONCAT");
            }

            writer.OpenBrace();

            int indexNext = writer.Length;

            VisitCheckIfSubconnection(node.Left);

            if (writer.Length == indexNext)
            {
                writer.Remove(indexBefore, indexNext - indexBefore);

                writer.AppendAt = appendAt;

                return node;
            }

            if (useConcat)
            {
                writer.Delimiter();
            }
            else
            {
                writer.Write(node.NodeType);
            }

            if (appendAt > -1)
            {
                appendAt += writer.Length - length;
            }

            writer.AppendAt = appendAt;

            writer.CloseBrace();

            return node;
        }

        /// <summary>
        /// 单个条件(field=1)。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitBinaryIsBoolean(BinaryExpression node)
        {
            int indexBefore = writer.Length;

            ignoreNullable = node.Left.Type.IsNullable();

            VisitCheckIfSubconnection(node.Right);

            ignoreNullable = false;

            int indexStep = writer.Length;

            if (indexStep == indexBefore)
            {
                return node;
            }

            int index = writer.Length;

            int length = writer.Length;

            int appendAt = writer.AppendAt;

            if (appendAt > -1)
            {
                indexBefore -= index - appendAt;
            }

            writer.AppendAt = indexBefore;

            writer.OpenBrace();

            int indexNext = writer.Length;

            ignoreNullable = node.Left.Type.IsNullable();

            VisitCheckIfSubconnection(node.Left);

            ignoreNullable = false;

            if (writer.Length == indexNext)
            {
                writer.Remove(indexBefore, indexNext - indexBefore);

                writer.AppendAt = appendAt;

                return node;
            }

            writer.Write(node.NodeType);

            if (appendAt > -1)
            {
                appendAt += writer.Length - length;
            }

            writer.AppendAt = appendAt;

            writer.CloseBrace();

            return node;
        }

        /// <summary>
        /// 位运算(field&amp;1)。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitBinaryIsBit(BinaryExpression node)
        {
            int indexBefore = writer.Length;

            ignoreNullable = true;

            Visit(node.Right);

            ignoreNullable = false;

            int indexStep = writer.Length;

            if (indexStep == indexBefore)
            {
                return node;
            }

            int index = writer.Length;

            int length = writer.Length;

            int appendAt = writer.AppendAt;

            if (appendAt > -1)
            {
                indexBefore -= index - appendAt;
            }

            writer.AppendAt = indexBefore;

            writer.OpenBrace();

            int indexNext = writer.Length;

            ignoreNullable = true;

            Visit(node.Left);

            ignoreNullable = false;

            if (writer.Length == indexNext)
            {
                writer.Remove(indexBefore, indexNext - indexBefore);

                writer.AppendAt = appendAt;

                return node;
            }

            writer.Write(node.NodeType);

            if (appendAt > -1)
            {
                appendAt += writer.Length - length;
            }

            writer.AppendAt = appendAt;

            writer.CloseBrace();

            return node;
        }

        /// <summary>
        /// 条件运算（a&amp;&amp;b）。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitBinaryIsConditionToVisit(Expression node)
        {
            if (!node.Type.IsBoolean())
            {
                return VisitCheckIfSubconnection(node);
            }

            if (node.NodeType == ExpressionType.MemberAccess || IsPlainVariable(node))
            {
                try
                {
                    isConditionBalance = true;

                    return VisitCheckIfSubconnection(node);
                }
                finally
                {
                    isConditionBalance = false;
                }
            }

            return VisitCheckIfSubconnection(node);
        }

        /// <inheritdoc />
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Coalesce)
            {
                return VisitBinaryIsCoalesce(node);
            }

            Expression left = node.Left;
            Expression right = node.Right;

            var nodeType = node.NodeType;

            bool isAndLike = nodeType == ExpressionType.AndAlso;
            bool isOrLike = nodeType == ExpressionType.OrElse;

            bool isAndOrLike = isAndLike || isOrLike;

            if (isAndOrLike && left.Type.IsBoolean() && IsPlainVariable(left, true))
            {
                if (writer.IsReverseCondition)
                {
                    isAndLike ^= isOrLike;
                    isOrLike ^= isAndLike;
                    isAndLike ^= isOrLike;
                }

                var value = left.GetValueFromExpression();

                if (value is null || value.Equals(false))
                {
                    if (isOrLike)
                    {
                        return VisitBinaryIsConditionToVisit(right);
                    }
                }
                else if (isAndLike)
                {
                    return VisitBinaryIsConditionToVisit(right);
                }

                return node;
            }

            if (isAndOrLike)
            {
                return VisitBinaryIsCondition(node, isAndLike);
            }

            if (nodeType == ExpressionType.Add || nodeType == ExpressionType.AddChecked)
            {
                return VisitBinaryIsAdd(node);
            }

            if (node.Type.IsBoolean())
            {
                return VisitBinaryIsBoolean(node);
            }

            return VisitBinaryIsBit(node);
        }

        /// <summary>
        /// 条件是变量。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitConditionalIsVariable(ConditionalExpression node)
        {
            if (Equals(node.Test.GetValueFromExpression(), true))
            {
                return VisitCheckIfSubconnection(node.IfTrue);
            }
            else
            {
                return VisitCheckIfSubconnection(node.IfFalse);
            }
        }

        /// <inheritdoc />
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            if (IsPlainVariable(node.Test, true))
            {
                return VisitConditionalIsVariable(node);
            }

            writer.OpenBrace();
            writer.Write("CASE WHEN ");

            VisitCheckIfSubconnection(node.Test);

            writer.Write(" THEN ");

            VisitCheckIfSubconnection(node.IfTrue);

            writer.Write(" ELSE ");

            VisitCheckIfSubconnection(node.IfFalse);

            writer.Write(" END");
            writer.CloseBrace();

            return node;
        }

        /// <inheritdoc />
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            VisitCheckIfSubconnection(node.Expression);

            return node;
        }

        /// <summary>
        /// 判断是否为子连接。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitCheckIfSubconnection(Expression node)
        {
            if (node.NodeType == ExpressionType.Call && (node is MethodCallExpression callExpression))
            {
                if (callExpression.Method.Name == MethodCall.Any || callExpression.Method.Name == MethodCall.All || callExpression.Method.Name == MethodCall.Contains)
                {
                    return Visit(node);
                }

                if (callExpression.Method.DeclaringType == typeof(Queryable))
                {
                    writer.OpenBrace();

                    using (var visitor = new SelectVisitor(this))
                    {
                        visitor.Startup(node);
                    }

                    writer.CloseBrace();

                    return node;
                }
            }

            return Visit(node);
        }
        #endregion

        private void BooleanFalse(bool allwaysFalse)
        {
            if (allwaysFalse || !writer.IsReverseCondition)
            {
                writer.BooleanTrue();

                writer.Equal();

                writer.BooleanFalse();
            }
        }
    }
}
