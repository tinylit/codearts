using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 变量。
    /// </summary>
    [DebuggerDisplay("{RuntimeType.Name} variable")]
    public sealed class VariableAst : AstExpression
    {
        private LocalBuilder local;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="variableType">类型。</param>
        public VariableAst(Type variableType) : base(variableType)
        {
        }
#if NET40_OR_GREATER
        private readonly string name;
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="variableType">类型。</param>
        /// <param name="name">变量名称。</param>
        public VariableAst(Type variableType, string name) : base(variableType)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"“{nameof(name)}”不能为 null 或空。", nameof(name));
            }

            this.name = name;
        }
#endif

        /// <summary>
        /// 是否可写。
        /// </summary>
        public override bool CanWrite => true;

        /// <summary>
        /// 取值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (local is null)
            {
                local = ilg.DeclareLocal(RuntimeType);
#if NET40_OR_GREATER
                if (name?.Length > 0)
                {
                    local.SetLocalSymInfo(name);
                }
#endif
            }

            ilg.Emit(OpCodes.Ldloc, local);
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected override void Assign(ILGenerator ilg, AstExpression value)
        {
            value.Load(ilg);

            if (local is null)
            {
                local = ilg.DeclareLocal(RuntimeType);

#if NET40_OR_GREATER
                if (name?.Length > 0)
                {
                    local.SetLocalSymInfo(name);
                }
#endif
            }

            ilg.Emit(OpCodes.Stloc, local);
        }
    }
}
