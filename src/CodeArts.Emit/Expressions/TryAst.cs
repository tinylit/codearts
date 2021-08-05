using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 捕获异常。
    /// </summary>
    [DebuggerDisplay("try \\{ //TODO:somethings \\}")]
    public class TryAst : BlockAst
    {
        private readonly AstExpression finallyAst;
        private readonly List<CatchAst> catchAsts;

        /// <summary>
        /// 异常处理。
        /// </summary>
        public interface IErrorHandler
        {
            /// <summary>
            /// 添加表达式。
            /// </summary>
            /// <param name="code">表达式。</param>
            IErrorHandler Append(AstExpression code);
        }

        /// <summary>
        /// 捕获异常。
        /// </summary>
        [DebuggerDisplay("catch({variable}){ {body} }")]
        private class CatchAst : BlockAst, IErrorHandler
        {
            private class CatchBlockAst : AstExpression
            {
                public CatchBlockAst(Type returnType) : base(returnType)
                {
                }

                public override void Load(ILGenerator ilg)
                {
                    ilg.BeginCatchBlock(RuntimeType);
                }
            }

            private readonly Type exceptionType;
            private readonly VariableAst variable;

            protected CatchAst(CatchAst catchAst) : base(catchAst?.RuntimeType)
            {
                if (catchAst is null)
                {
                    throw new ArgumentNullException(nameof(catchAst));
                }
                variable = catchAst.variable;
                exceptionType = catchAst.exceptionType;
            }

            public CatchAst(Type returnType, Type exceptionType) : base(returnType)
            {
                if (exceptionType is null)
                {
                    throw new ArgumentNullException(nameof(exceptionType));
                }

                if (exceptionType == typeof(Exception) || exceptionType.IsAssignableFrom(typeof(Exception)))
                {
                    this.exceptionType = exceptionType;
                }
                else
                {
                    throw new AstException($"变量类型“{exceptionType}”未继承“{typeof(Exception)}”异常基类!");
                }
            }

            public CatchAst(Type returnType, VariableAst variable) : base(returnType)
            {
                if (variable is null)
                {
                    throw new ArgumentNullException(nameof(variable));
                }

                this.exceptionType = variable.RuntimeType;

                if (exceptionType == typeof(Exception) || exceptionType.IsAssignableFrom(typeof(Exception)))
                {
                    this.variable = variable;
                }
                else
                {
                    throw new AstException($"变量类型“{exceptionType}”未继承“{typeof(Exception)}”异常基类!");
                }
            }

            /// <summary>
            /// 生成。
            /// </summary>
            /// <param name="ilg">指令。</param>
            public override void Load(ILGenerator ilg)
            {
                if (variable is null)
                {
                    ilg.BeginCatchBlock(exceptionType);
                }
                else
                {
                    Assign(variable, new CatchBlockAst(exceptionType))
                        .Load(ilg);
                }

                ilg.Emit(OpCodes.Nop);

                base.Load(ilg);
            }

            IErrorHandler IErrorHandler.Append(AstExpression code)
            {
                Append(code);

                return this;
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="tryAst">异常捕获。</param>
        protected TryAst(TryAst tryAst) : base(tryAst)
        {
            catchAsts = tryAst.catchAsts;
            finallyAst = tryAst.finallyAst;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回结果。</param>
        public TryAst(Type returnType) : base(returnType)
        {
            catchAsts = new List<CatchAst>();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回结果。</param>
        /// <param name="finallyAst">一定会执行的代码。</param>
        public TryAst(Type returnType, AstExpression finallyAst) : base(returnType)
        {
            this.finallyAst = finallyAst ?? throw new ArgumentNullException(nameof(finallyAst));

            catchAsts = new List<CatchAst>();
        }

        /// <summary>
        /// 捕获任意异常。
        /// </summary>
        /// <returns></returns>
        public IErrorHandler Catch() => Catch(typeof(Exception));

        /// <summary>
        /// 捕获“<paramref name="variable"/>.RuntimeType”异常，并将异常赋值给指定变量。
        /// </summary>
        /// <returns></returns>
        public IErrorHandler Catch(VariableAst variable) => Catch(RuntimeType, variable);

        /// <summary>
        /// 捕获指定类型异常。
        /// </summary>
        /// <param name="exceptionType">异常类型。</param>
        /// <returns></returns>
        public IErrorHandler Catch(Type exceptionType) => Catch(RuntimeType, exceptionType);

        /// <summary>
        /// 捕获指定类型的异常。
        /// </summary>
        /// <param name="returnType">返回类型。</param>
        /// <param name="exceptionType">异常类型。</param>
        /// <returns></returns>
        public IErrorHandler Catch(Type returnType, Type exceptionType)
        {
            if (exceptionType is null)
            {
                throw new ArgumentNullException(nameof(exceptionType));
            }

            var catchAst = new CatchAst(returnType, exceptionType);

            catchAsts.Add(catchAst);

            return catchAst;
        }

        /// <summary>
        /// 捕获“<paramref name="variable"/>.RuntimeType”的异常，并将异常赋值给指定变量。
        /// </summary>
        /// <param name="returnType">返回类型。</param>
        /// <param name="variable">变量。</param>
        /// <returns></returns>
        public IErrorHandler Catch(Type returnType, VariableAst variable)
        {
            if (variable is null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            var catchAst = new CatchAst(returnType, variable);

            catchAsts.Add(catchAst);

            return catchAst;
        }

        /// <summary>
        /// 发行变量和代码块(无返回值)。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="label">跳转位置。</param>
        protected override void EmitVoid(ILGenerator ilg, Label label)
        {
            ilg.BeginExceptionBlock();

            base.EmitVoid(ilg, label);

            if (catchAsts.Count > 0)
            {
                foreach (var catchAst in catchAsts)
                {
                    FlowControl(catchAst, ilg, label);
                }

                ilg.Emit(OpCodes.Nop);
            }

            if (finallyAst != null)
            {
                ilg.BeginFinallyBlock();

                FlowControl(finallyAst, ilg, label);

                ilg.Emit(OpCodes.Nop);
            }

            ilg.EndExceptionBlock();
        }

        /// <summary>
        /// 发行变量和代码块（有返回值）。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="variable">存储结果的变量。</param>
        /// <param name="label">跳转位置。</param>
        protected override void Emit(ILGenerator ilg, LocalBuilder variable, Label label)
        {
            ilg.BeginExceptionBlock();

            base.Emit(ilg, variable, label);

            if (catchAsts.Count > 0)
            {
                foreach (var catchAst in catchAsts)
                {
                    FlowControl(catchAst, ilg, variable, label);
                }

                ilg.Emit(OpCodes.Nop);
            }

            if (finallyAst != null)
            {
                ilg.BeginFinallyBlock();

                FlowControl(finallyAst, ilg, label);

                ilg.Emit(OpCodes.Nop);
            }

            ilg.EndExceptionBlock();
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (catchAsts.Count == 0 && finallyAst is null)
            {
                throw new AstException("表达式残缺，未设置“catch”代码块和“finally”代码块至少设置其一！");
            }

            base.Load(ilg);
        }
    }
}
