using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 创建数组。
    /// </summary>
    [DebuggerDisplay("new {ReturnType.Name}[{size}]")]
    public class NewArrayAst : AstExpression
    {
        private readonly int size;
        private readonly Type elementType;

        /// <summary>
        /// 构造函数【生成object[]】。
        /// </summary>
        /// <param name="size">数组大小。</param>
        public NewArrayAst(int size) : this(size, typeof(object))
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="size">数组大小。</param>
        /// <param name="elementType">数组元素类型。</param>
        public NewArrayAst(int size, Type elementType) : base(elementType.MakeArrayType(1))
        {
            this.size = size;
            this.elementType = elementType;
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令</param>
        public override void Load(ILGenerator ilg)
        {
            ilg.Emit(OpCodes.Ldc_I4, size);
            ilg.Emit(OpCodes.Newarr, elementType);
        }
    }
}
