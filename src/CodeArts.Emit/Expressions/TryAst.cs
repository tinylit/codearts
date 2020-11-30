using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 捕获异常。
    /// </summary>
    [DebuggerDisplay("try { {body} }")]
    public class TryAst : AstExpression
    {
        private readonly AstExpression body;
        private readonly CatchAst[] catchs;
        private readonly FinallyAst @finally;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">内容。</param>
        /// <param name="catchs">异常捕获。</param>
        public TryAst(AstExpression body, params CatchAst[] catchs) : this(body, catchs, null)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">内容。</param>
        /// <param name="finally">结束。</param>
        public TryAst(AstExpression body, FinallyAst @finally) : this(body, null, @finally)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">内容。</param>
        /// <param name="catchs">异常捕获。</param>
        /// <param name="finally">结束。</param>
        public TryAst(AstExpression body, CatchAst[] catchs, FinallyAst @finally) : base(body.ReturnType)
        {
            if ((catchs is null || catchs.Length == 0) && @finally is null)
            {
                throw new AstException($"参数{nameof(catchs)}和{nameof(@finally)}不能同时为空!");
            }

            this.body = body ?? throw new System.ArgumentNullException(nameof(body));

            var bodyType = body.ReturnType;

            if (catchs is null || catchs.Length == 0 || bodyType == typeof(void) || catchs.All(x => x.ReturnType == bodyType || x.ReturnType.IsSubclassOf(bodyType)))
            {
                this.@finally = @finally;
                this.catchs = catchs ?? new CatchAst[0];
            }

            throw new AstException($"异常代码块的返回类型和主代码块的返回类型不相同!");
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            ilg.BeginExceptionBlock();

            body.Load(ilg);

            if (ReturnType == typeof(void))
            {
                foreach (var item in catchs)
                {
                    item.Load(ilg);
                }

                @finally?.Load(ilg);

                ilg.EndExceptionBlock();
            }
            else
            {
                var variable = ilg.DeclareLocal(ReturnType);

                ilg.Emit(OpCodes.Stloc, variable);

                foreach (var item in catchs)
                {
                    item.Load(ilg);

                    ilg.Emit(OpCodes.Stloc, variable);
                }

                @finally?.Load(ilg);

                ilg.EndExceptionBlock();

                ilg.Emit(OpCodes.Ldloc, variable);
            }
        }
    }
}
