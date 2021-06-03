using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 数组。
    /// </summary>
    public class ArrayAst : AstExpression
    {
        private readonly AstExpression[] expressions;

        private static Type GetReturnType(AstExpression[] expressions)
        {
            if (expressions is null)
            {
                throw new ArgumentNullException(nameof(expressions));
            }

            if (expressions.Length == 0)
            {
                return typeof(object[]);
            }

            return typeof(object[]);
        }

        public ArrayAst(AstExpression[] expressions) : base(GetReturnType(expressions))
        {
            this.expressions = expressions;
        }
    }
}
