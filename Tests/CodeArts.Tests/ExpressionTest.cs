using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using static System.Linq.Expressions.Expression;

namespace CodeArts.Tests
{
    [TestClass]
    public class ExpressionTest
    {
        [TestMethod]
        public void Loop()
        {
            ParameterExpression localvar_sequence = Parameter(typeof(int));
            LabelTarget break_label = Label(typeof(int));
            LabelTarget continue_label = Label(typeof(void));

            Func<int> expression = Lambda<Func<int>>(
                Block(new ParameterExpression[]
                {
                    localvar_sequence,
                }, new Expression[]
                {
                    Assign(localvar_sequence, Constant(0)),
                    Expression.Loop(
                        IfThenElse(
                            LessThan(localvar_sequence, Constant(100)),
                            Block(
                                AddAssign(localvar_sequence, Constant(1)),
                                Call(typeof(System.Diagnostics.Debug).GetMethod("WriteLine", new Type[]{ typeof(object)}), Expression.Convert(localvar_sequence,typeof(object))),
                                Continue(continue_label, typeof(void))
                            ),
                            Break(break_label, localvar_sequence)), // push to eax/rax --> return value
                        break_label, continue_label),
                })).Compile();

            var i = expression.Invoke();
        }

        [TestMethod]
        public void NullableNew()
        {
            var body = New(typeof(Nullable<>).MakeGenericType(typeof(int)).GetConstructors(BindingFlags.Public | BindingFlags.Instance).First(), Default(typeof(int)));

            var lambda = Lambda<Func<int?>>(body);

            var value = lambda.Compile().Invoke();
        }
    }
}
