using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 数组。
    /// </summary>
    public class ArrayAst : AstExpression
    {
        private class ParatemerRefAst : MemberAst
        {
            public ParatemerRefAst(AstExpression paramterAst) : base(paramterAst.ReturnType.GetElementType(), paramterAst)
            {
            }

            public override void Assign(ILGenerator ilg)
            {
                EmitUtils.EmitAssignToType(ilg, ReturnType);
            }

            protected override void MemberLoad(ILGenerator ilg)
            {
                EmitUtils.EmitLoadToType(ilg, ReturnType);
            }

            protected override void AssignCore(ILGenerator ilg, AstExpression value)
            {
                value.Load(ilg);

                Assign(ilg);
            }
        }

        private readonly Type elementType;
        private readonly AstExpression[] expressions;

        private static bool IsValid(AstExpression[] expressions, Type elementType)
        {
            return expressions.Length == 0 || elementType == typeof(object) || expressions.All(x => elementType.IsAssignableFrom(x.ReturnType));
        }

        /// <summary>
        /// 元素集合。
        /// </summary>
        /// <param name="expressions">元素。</param>

        public ArrayAst(AstExpression[] expressions) : this(expressions, typeof(object[]))
        {

        }

        /// <summary>
        /// 元素集合。
        /// </summary>
        /// <param name="expressions">元素。</param>
        /// <param name="arrayType">数组类型。</param>

        public ArrayAst(AstExpression[] expressions, Type arrayType) : base(arrayType)
        {
            if (arrayType is null)
            {
                throw new ArgumentNullException(nameof(arrayType));
            }

            if (!arrayType.IsArray)
            {
                throw new ArgumentException($"“{arrayType}”不是数组类型!", nameof(arrayType));
            }

            var elementType = arrayType.GetElementType();

            if (!IsValid(expressions ?? throw new ArgumentNullException(nameof(expressions)), elementType))
            {
                throw new AstException($"表达式元素不能转换为数组元素类型!");
            }

            this.expressions = expressions;
            this.elementType = arrayType.GetElementType();

        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            throw new NotImplementedException();
        }
    }
}
