using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 数组。
    /// </summary>
    public class ArrayAst : AstExpression
    {
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

        public ArrayAst(AstExpression[] expressions) : this(expressions, typeof(object))
        {

        }

        /// <summary>
        /// 元素集合。
        /// </summary>
        /// <param name="expressions">元素。</param>
        /// <param name="elementType">数组类型。</param>

        public ArrayAst(AstExpression[] expressions, Type elementType) : base(elementType.MakeArrayType())
        {
            if (elementType is null)
            {
                throw new ArgumentNullException(nameof(elementType));
            }

            if (!IsValid(expressions ?? throw new ArgumentNullException(nameof(expressions)), elementType))
            {
                throw new AstException($"表达式元素不能转换为数组元素类型!");
            }

            this.expressions = expressions;
            this.elementType = elementType;

        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            bool isObjectElememt = elementType == typeof(object);

            EmitUtils.EmitInt(ilg, expressions.Length);

            ilg.Emit(OpCodes.Newarr, elementType);

            for (int i = 0; i < expressions.Length; i++)
            {
                var expressionAst = expressions[i];

                ilg.Emit(OpCodes.Dup);

                EmitUtils.EmitInt(ilg, i);

                expressionAst.Load(ilg);

                if (isObjectElememt)
                {
                    var type = expressionAst.ReturnType;

                    if (type.IsByRef)
                    {
                        type = type.GetElementType();
                    }

                    if (type.IsEnum)
                    {
                        type = Enum.GetUnderlyingType(type);
                    }

                    if (type.IsValueType || type.IsGenericParameter)
                    {
                        ilg.Emit(OpCodes.Box, type);
                    }
                }

                ilg.Emit(OpCodes.Stelem_Ref);
            }
        }
    }
}
