using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 捕获异常。
    /// </summary>
    [DebuggerDisplay("try { {body} }")]
    public class TryAst : BlockAst
    {
        private readonly List<CatchAst> catchAsts = new List<CatchAst>();
        private readonly List<FinallyAst> finallyAsts = new List<FinallyAst>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回结果。</param>
        public TryAst(Type returnType) : base(returnType)
        {
        }

        /// <summary>
        /// 添加代码。
        /// </summary>
        /// <param name="code">代码。</param>
        /// <returns></returns>
        public override BlockAst Append(AstExpression code)
        {
            if (code is CatchAst catchAst)
            {
                catchAsts.Add(catchAst);

                return this;
            }

            if (code is FinallyAst finallyAst)
            {
                finallyAsts.Add(finallyAst);

                return this;
            }

            return base.Append(code);
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            ilg.BeginExceptionBlock();

            base.Load(ilg);

            if (HasReturn)
            {
                throw new AstException("表达式会将结果推到堆上，不能写返回！");
            }

            if (ReturnType == typeof(void))
            {
                foreach (var item in catchAsts)
                {
                    item.Load(ilg);
                }

                if (finallyAsts.Count > 0)
                {
                    ilg.BeginFinallyBlock();

                    foreach (var item in finallyAsts)
                    {
                        item.Load(ilg);
                    }
                }

                ilg.EndExceptionBlock();
            }
            else
            {
                var variable = ilg.DeclareLocal(ReturnType);

                ilg.Emit(OpCodes.Stloc, variable);

                foreach (var item in catchAsts)
                {
                    item.Load(ilg);
                }

                if (finallyAsts.Count > 0)
                {
                    ilg.BeginFinallyBlock();

                    foreach (var item in finallyAsts)
                    {
                        item.Load(ilg);
                    }
                }

                ilg.EndExceptionBlock();

                ilg.Emit(OpCodes.Ldloc, variable);
            }
        }
    }
}
