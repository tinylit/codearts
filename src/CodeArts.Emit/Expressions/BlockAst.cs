using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 代码块。
    /// </summary>
    public class BlockAst : AstExpression
    {
        private readonly List<AstExpression> codes;

        private bool isReadOnly = false;
        private bool CanConverted2Void = true;

        /// <summary>
        /// 代码块。
        /// </summary>
        /// <param name="blockAst"></param>
        protected BlockAst(BlockAst blockAst) : base(blockAst?.RuntimeType ?? throw new ArgumentNullException(nameof(blockAst)))
        {
            codes = blockAst.codes;
            isReadOnly = blockAst.isReadOnly;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回值类型。</param>
        public BlockAst(Type returnType) : base(returnType)
        {
            codes = new List<AstExpression>();
        }

        /// <summary>
        /// 是否为空。
        /// </summary>
        public bool IsEmpty => codes.Count == 0;

        /// <summary>
        /// 添加代码。
        /// </summary>
        /// <param name="code">代码。</param>
        /// <returns></returns>
        public virtual BlockAst Append(AstExpression code)
        {
            if (code is null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            if (isReadOnly)
            {
                throw new AstException("当前代码块已作为其它代码块的一部分，不能进行修改!");
            }

            if (code is ReturnAst returnAst)
            {
                if (returnAst.IsEmpty)
                {
                    if (RuntimeType == typeof(void))
                    {
                        //? 无返回值（模块原本就是无返回值，避免多次清除栈顶数据）。
                        if (code.RuntimeType == typeof(void))
                        {
                            code = new ReturnAst();
                        }

                        goto label_core;
                    }

                    if (IsEmpty)
                    {
                        throw new AstException("栈顶部无任何数据!");
                    }

                    CanConverted2Void = false;

#if NETSTANDARD2_1_OR_GREATER
                    AstExpression lastCode = codes[^1];
#else
                    AstExpression lastCode = codes[codes.Count - 1];
#endif

                    if (lastCode is ReturnAst)
                    {
                        return this;
                    }

                    if (EmitUtils.EqualSignatureTypes(lastCode.RuntimeType, RuntimeType) || lastCode.RuntimeType.IsAssignableFrom(RuntimeType))
                    {
                        goto label_core;
                    }

                    throw new AstException($"栈顶部的数据类型“{lastCode.RuntimeType}”和预期的返回类型“{RuntimeType}”不相同!");
                }
                else
                {
                    CanConverted2Void = false;

                    if (EmitUtils.EqualSignatureTypes(code.RuntimeType, RuntimeType) || code.RuntimeType.IsAssignableFrom(RuntimeType))
                    {
                        goto label_core;
                    }

                    throw new AstException($"返回类型“{code.RuntimeType}”和预期的返回类型“{RuntimeType}”不相同!");
                }
            }
            else if (code is BlockAst blockAst)
            {
                blockAst.isReadOnly = true;

                if (RuntimeType == typeof(void) && blockAst.CanConverted2Void)
                {
                    goto label_core;
                }

                if (EmitUtils.EqualSignatureTypes(code.RuntimeType, RuntimeType) || code.RuntimeType.IsAssignableFrom(RuntimeType))
                {
                    goto label_core;
                }

                throw new AstException($"返回类型“{code.RuntimeType}”和预期的返回类型“{RuntimeType}”不相同!");
            }

        label_core:

            codes.Add(code);

            return this;
        }

        /// <summary>
        /// 发行变量和代码块(无返回值)。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="label">跳转位置。</param>
        protected virtual void EmitVoid(ILGenerator ilg, Label label)
        {
            int i = 0, len = codes.Count - 1;

            for (; i < len;)
            {
                ilg.Emit(OpCodes.Nop);

                var code = codes[i];
                var nextCode = codes[i + 1];

                if (nextCode is ReturnAst returnAst)
                {
                    i += 2; //? 一次处理两条记录。

                    FlowControl(code, ilg, label);

                    ilg.Emit(OpCodes.Leave_S, label);
                }
                else
                {
                    i++;

                    FlowControl(code, ilg, label);
                }
            }

            if (i > len) //? 结尾是返回结果代码。
            {
                return;
            }

#if NETSTANDARD2_1_OR_GREATER
            var codeAst = codes[^1];
#else
            var codeAst = codes[codes.Count - 1];
#endif

            ilg.Emit(OpCodes.Nop);

            FlowControl(codeAst, ilg, label);
        }

        /// <summary>
        /// 发行变量和代码块(无返回值)。
        /// </summary>
        /// <param name="ilg">指令。</param>
        protected virtual void EmitVoid(ILGenerator ilg)
        {
            Label label = ilg.DefineLabel();

            EmitVoid(ilg, label);

            ilg.MarkLabel(label);
        }

        /// <summary>
        /// 发行变量和代码块（有返回值）。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="local">存储结果的变量。</param>
        /// <param name="label">跳转位置。</param>
        protected virtual void Emit(ILGenerator ilg, LocalBuilder local, Label label)
        {
            if (IsEmpty)
            {
                throw new AstException("并非所有代码都有返回值!");
            }

#if NETSTANDARD2_1_OR_GREATER
            var codeAst = codes[^1];
#else
            var codeAst = codes[codes.Count - 1];
#endif

            if (EmitUtils.EqualSignatureTypes(codeAst.RuntimeType, RuntimeType) || codeAst.RuntimeType.IsAssignableFrom(RuntimeType))
            {
                goto label_core;
            }

            if (codes.Any(x => x is ReturnAst))
            {
                goto label_core;
            }

            throw new AstException("并非所有代码都有返回值!");

        label_core:

            int i = 0, len = codes.Count - 1;

            for (; i < len;)
            {
                ilg.Emit(OpCodes.Nop);

                var code = codes[i];
                var nextCode = codes[i + 1];

                if (nextCode is ReturnAst returnAst)
                {
                    i += 2; //? 一次处理两条记录。

                    if (returnAst.IsEmpty)
                    {
                        FlowControl(code, ilg, local, label);
                    }
                    else
                    {
                        FlowControl(code, ilg, label);

                        FlowControl(returnAst.Unbox(), ilg, local, label);
                    }

                    if (len > i)
                    {
                        ilg.Emit(OpCodes.Leave_S, label);
                    }
                }
                else
                {
                    i++;

                    FlowControl(code, ilg, label);
                }
            }

            if (i > len) //? 结尾是返回结果代码。
            {
                return;
            }

            ilg.Emit(OpCodes.Nop);

            FlowControl(codeAst, ilg, local, label);
        }

        /// <summary>
        /// 发行（有返回值）。
        /// </summary>
        /// <param name="ilg">指令。</param>
        protected virtual void Emit(ILGenerator ilg)
        {
            var variable = ilg.DeclareLocal(RuntimeType);

            Label label = ilg.DefineLabel();

            Emit(ilg, variable, label);

            ilg.MarkLabel(label);

            ilg.Emit(OpCodes.Ldloc, variable);
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (isReadOnly || codes.Any(x => x is ReturnAst || x is BlockAst))
            {
                if (RuntimeType == typeof(void))
                {
                    EmitVoid(ilg);
                }
                else
                {
                    Emit(ilg);
                }
            }
            else
            {
                foreach (var code in codes)
                {
                    ilg.Emit(OpCodes.Nop);

                    code.Load(ilg);
                }
            }
        }
    }
}