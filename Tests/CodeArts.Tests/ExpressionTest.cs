using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace CodeArts.Tests
{
    [TestClass]
    public class ExpressionTest
    {
        [TestMethod]
        public void Loop()
        {
            ParameterExpression localvar_sequence = Expression.Parameter(typeof(int));
            LabelTarget break_label = Expression.Label(typeof(int));
            LabelTarget continue_label = Expression.Label(typeof(void));

            Func<int> expression = Expression.Lambda<Func<int>>(
                Expression.Block(new ParameterExpression[]
                {
                    localvar_sequence,
                }, new Expression[]
                {
                    Expression.Assign(localvar_sequence, Expression.Constant(0)),
                    Expression.Loop(
                        Expression.IfThenElse(
                            Expression.LessThan(localvar_sequence, Expression.Constant(100)),
                            Expression.Block(
                                Expression.AddAssign(localvar_sequence, Expression.Constant(1)),
                                Expression.Call(typeof(System.Diagnostics.Debug).GetMethod("WriteLine", new Type[]{ typeof(object)}), Expression.Convert(localvar_sequence,typeof(object))),
                                Expression.Continue(continue_label, typeof(void))
                            ),
                            Expression.Break(break_label, localvar_sequence)), // push to eax/rax --> return value
                        break_label, continue_label),
                })).Compile();

            var i = expression.Invoke();
        }
    }
}
