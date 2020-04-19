using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 捕获异常。
    /// </summary>
    [DebuggerDisplay("try { {body} }")]
    public class TryExpression : Expression
    {
        private readonly Expression body;
        private readonly CatchExpression[] catchs;
        private readonly FinallyExpression @finally;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">内容</param>
        /// <param name="catchs">异常捕获</param>
        public TryExpression(Expression body, params CatchExpression[] catchs) : this(body, catchs, null)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">内容</param>
        /// <param name="finally">结束</param>
        public TryExpression(Expression body, FinallyExpression @finally) : this(body, null, @finally)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">内容</param>
        /// <param name="catchs">异常捕获</param>
        /// <param name="finally">结束</param>
        public TryExpression(Expression body, CatchExpression[] catchs, FinallyExpression @finally) : base(body.ReturnType)
        {
            if ((catchs is null || catchs.Length == 0) && @finally is null)
            {
                throw new EmitException($"参数{nameof(catchs)}和{nameof(@finally)}不能同时为空!");
            }

            this.body = body ?? throw new System.ArgumentNullException(nameof(body));

            var bodyType = body.ReturnType;

            if (catchs is null || catchs.Length == 0 || bodyType == typeof(void) || catchs.All(x => x.ReturnType == bodyType || x.ReturnType.IsSubclassOf(bodyType)))
            {
                this.@finally = @finally;
                this.catchs = catchs ?? new CatchExpression[0];
            }

            throw new EmitException($"异常代码块的返回类型和主代码块的返回类型不相同!");
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令</param>
        public override void Emit(ILGenerator ilg)
        {
            ilg.BeginExceptionBlock();

            body.Emit(ilg);

            if (ReturnType == typeof(void))
            {
                foreach (var item in catchs)
                {
                    item.Emit(ilg);
                }

                @finally?.Emit(ilg);

                ilg.EndExceptionBlock();
            }
            else
            {
                var variable = ilg.DeclareLocal(ReturnType);

                ilg.Emit(OpCodes.Stloc, variable);

                foreach (var item in catchs)
                {
                    item.Emit(ilg);

                    ilg.Emit(OpCodes.Stloc, variable);
                }

                @finally?.Emit(ilg);

                ilg.EndExceptionBlock();

                ilg.Emit(OpCodes.Ldloc, variable);
            }
        }
    }
}
