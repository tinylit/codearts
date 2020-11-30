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
        /// 构造函数。
        /// </summary>
        /// <param name="field">字段。</param>
        public FieldAst(FieldInfo field) : base(field.FieldType, (field.Attributes & FieldAttributes.Static) == FieldAttributes.Static)
        {
            this.field = field;
            isStatic = (field.Attributes & FieldAttributes.Static) == FieldAttributes.Static;
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
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
        public override void Assign(ILGenerator ilg)
        {
            if (isStatic)
            {
                ilg.Emit(OpCodes.Stsfld, field);
            }
            else
            {
                ilg.Emit(OpCodes.Stfld, field);
            }
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected override void AssignCore(ILGenerator ilg, AstExpression value)
        {
            Assign(ilg);

            value.Load(ilg);
        }
    }
}
