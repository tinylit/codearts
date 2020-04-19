using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 字段成员。
    /// </summary>
	[DebuggerDisplay("{FieldInfo.Name}")]
    public class FieldExpression : MemberExpression
    {
        private readonly FieldInfo fieldInfo;
        private readonly bool isStatic;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fieldInfo"></param>
        public FieldExpression(FieldInfo fieldInfo) : this(fieldInfo, (fieldInfo.Attributes & FieldAttributes.Static) == 0)
        {
        }

        private FieldExpression(FieldInfo fieldInfo, bool isStatic) : base(fieldInfo.FieldType, isStatic)
        {
            this.fieldInfo = fieldInfo;
            this.isStatic = isStatic;
        }

        /// <summary>
        /// 取值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Emit(ILGenerator ilg)
        {
            if (isStatic)
            {
                ilg.Emit(OpCodes.Ldsfld, fieldInfo);
            }
            else
            {
                ilg.Emit(OpCodes.Ldfld, fieldInfo);
            }
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值</param>
        protected override void AssignCore(ILGenerator ilg, Expression value)
        {
            if (isStatic)
            {
                ilg.Emit(OpCodes.Stsfld, fieldInfo);
            }
            else
            {
                ilg.Emit(OpCodes.Stfld, fieldInfo);
            }
        }
    }
}
