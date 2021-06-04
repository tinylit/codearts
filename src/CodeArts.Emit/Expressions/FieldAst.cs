using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 字段。
    /// </summary>
    [DebuggerDisplay("{ReturnType.Name} {field.Name}")]
    public class FieldAst : MemberAst
    {
        private readonly FieldInfo field;
        private readonly bool isStatic;

        /// <summary>
        /// 是否可写。
        /// </summary>
        public override bool CanWrite => !(field.IsStatic || field.IsInitOnly);

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="field">字段。</param>
        public FieldAst(FieldInfo field) : this(field, (field.Attributes & FieldAttributes.Static) == FieldAttributes.Static)
        {

        }

        private FieldAst(FieldInfo field, bool isStatic) : base(field.FieldType, isStatic)
        {
            this.field = field;
            this.isStatic = isStatic;
        }

        /// <summary>
        /// 获取成员数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        protected override void LoadCore(ILGenerator ilg)
        {
            if (isStatic)
            {
                ilg.Emit(OpCodes.Ldsfld, field);
            }
            else
            {
                ilg.Emit(OpCodes.Ldfld, field);
            }
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected override void AssignCore(ILGenerator ilg, AstExpression value)
        {
            if (isStatic)
            {
                ilg.Emit(OpCodes.Stsfld, field);
            }
            else
            {
                ilg.Emit(OpCodes.Stfld, field);
            }

            value.Load(ilg);
        }
    }
}
