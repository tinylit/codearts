using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 二进制。
    /// </summary>
    [DebuggerDisplay("{left} {expressionType} {right}")]
    public class BinaryAst : AstExpression
    {
        private readonly AstExpression left;
        private readonly ExpressionType expressionType;
        private readonly AstExpression right;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="expressionType">计算方式。</param>
        /// <param name="right">右表达式。</param>
        public BinaryAst(AstExpression left, ExpressionType expressionType, AstExpression right) : base(AnalysisType(left, expressionType, right))
        {
            this.left = left;
            this.expressionType = expressionType;
            this.right = right;
        }

        private static Type AnalysisType(AstExpression left, ExpressionType expressionType, AstExpression right)
        {
            if (left.ReturnType != right.ReturnType)
            {
                throw new AstException("左右表达式类型不相同!");
            }

            switch (expressionType)
            {
                case ExpressionType.Add:
                case ExpressionType.Subtract:
                case ExpressionType.Multiply:
                case ExpressionType.Divide:
                    if (left.ReturnType.IsValueType && left.ReturnType.IsPrimitive)
                    {
                        return left.ReturnType;
                    }
                    throw new AstException($"{left.ReturnType}类型不支持“{expressionType}”运算!");
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.NotEqual:
                    return typeof(bool);
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="ilg">命令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (left is ConstantAst constantAst && constantAst.IsNull)
            {
                ilg.Emit(OpCodes.Ldnull);
            }
            else
            {
                left.Load(ilg);
            }

            if (right is ConstantAst constantAst2 && constantAst2.IsNull)
            {
                ilg.Emit(OpCodes.Ldnull);
            }
            else
            {
                right.Load(ilg);
            }

            switch (expressionType)
            {
                case ExpressionType.Add:
                    ilg.Emit(OpCodes.Add_Ovf);
                    break;
                case ExpressionType.Subtract:
                    ilg.Emit(OpCodes.Sub_Ovf);
                    break;
                case ExpressionType.Multiply:
                    ilg.Emit(OpCodes.Mul_Ovf);
                    break;
                case ExpressionType.Divide:
                    ilg.Emit(OpCodes.Div);
                    break;
                case ExpressionType.LessThan:
                    ilg.Emit(OpCodes.Clt);
                    break;
                case ExpressionType.Equal:
                    ilg.Emit(OpCodes.Ceq);
                    break;
                case ExpressionType.GreaterThan:
                    ilg.Emit(OpCodes.Cgt);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.NotEqual:
                    if (expressionType == ExpressionType.GreaterThanOrEqual)
                    {
                        ilg.Emit(OpCodes.Clt);
                    }
                    else if (expressionType == ExpressionType.LessThanOrEqual)
                    {
                        ilg.Emit(OpCodes.Cgt);
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Ceq);
                    }
                    ilg.Emit(OpCodes.Ldc_I4_0);
                    ilg.Emit(OpCodes.Ceq);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
