using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 流程。
    /// </summary>
    public class SwitchAst : AstExpression
    {
        private readonly AstExpression defaultAst;
        private readonly AstExpression switchValue;
        private readonly List<IPrivateCaseHandler> switchCases;

        private readonly Type switchValueType;
        private readonly MySwitchValueKind switchValueKind;

        private enum MySwitchValueKind
        {
            Arithmetic,
            RuntimeType,
            Equality
        }

        /// <summary>
        /// 案例处理。
        /// </summary>
        public interface ICaseHandler
        {
            /// <summary>
            /// 添加表达式。
            /// </summary>
            /// <param name="code">表达式。</param>
            /// <returns></returns>
            ICaseHandler Append(AstExpression code);
        }

        private interface IPrivateCaseHandler : ICaseHandler
        {
            OpCode Equal_S { get; }

            void EmitEqual(ILGenerator ilg);

            void EmitVoid(ILGenerator ilg, MyVariableAst variable, Label label);

            void Emit(ILGenerator ilg, MyVariableAst variable, LocalBuilder local, Label label);
        }

        private class MyVariableAst : AstExpression
        {
            private readonly LocalBuilder local;

            public MyVariableAst(LocalBuilder local) : base(local.LocalType)
            {
                this.local = local;
            }

            public override void Load(ILGenerator ilg)
            {
                ilg.Emit(OpCodes.Ldloc, local);
            }
        }

        private class SwitchCaseArithmeticAst : BlockAst, IPrivateCaseHandler
        {
            private readonly ConstantAst constant;

            public SwitchCaseArithmeticAst(ConstantAst constant, Type returnType) : base(returnType)
            {

                this.constant = constant;
            }

            public void EmitEqual(ILGenerator ilg)
            {
                constant.Load(ilg);
            }

            public OpCode Equal_S => OpCodes.Beq_S;

            ICaseHandler ICaseHandler.Append(AstExpression code)
            {
                Append(code);

                return this;
            }

            void IPrivateCaseHandler.Emit(ILGenerator ilg, MyVariableAst variable, LocalBuilder local, Label label) => Emit(ilg, local, label);

            void IPrivateCaseHandler.EmitVoid(ILGenerator ilg, MyVariableAst variable, Label label) => EmitVoid(ilg, label);
        }

        private class SwitchCaseRuntimeTypeAst : BlockAst, IPrivateCaseHandler
        {
            private readonly VariableAst variableAst;

            public SwitchCaseRuntimeTypeAst(VariableAst variableAst, Type returnType) : base(returnType)
            {
                this.variableAst = variableAst;
            }

            public void EmitEqual(ILGenerator ilg)
            {
                if (variableAst.RuntimeType.IsNullable())
                {
                    ilg.Emit(OpCodes.Isinst, Nullable.GetUnderlyingType(variableAst.RuntimeType));
                }
                else
                {
                    ilg.Emit(OpCodes.Isinst, variableAst.RuntimeType);
                }
            }

            public OpCode Equal_S => OpCodes.Brtrue_S;

            ICaseHandler ICaseHandler.Append(AstExpression code)
            {
                Append(code);

                return this;
            }
            void IPrivateCaseHandler.Emit(ILGenerator ilg, MyVariableAst variable, LocalBuilder local, Label label)
            {
                Assign(variableAst, TypeAs(variable, variableAst.RuntimeType))
                    .Load(ilg);

                Emit(ilg, local, label);
            }

            void IPrivateCaseHandler.EmitVoid(ILGenerator ilg, MyVariableAst variable, Label label)
            {
                Assign(variableAst, TypeAs(variable, variableAst.RuntimeType))
                    .Load(ilg);

                EmitVoid(ilg, label);
            }
        }

        private class SwitchCaseEqualityAst : BlockAst, IPrivateCaseHandler
        {
            private readonly ConstantAst constant;
            private readonly MethodInfo comparison;

            public SwitchCaseEqualityAst(ConstantAst constant, MethodInfo comparison, Type returnType) : base(returnType)
            {
                this.constant = constant;
                this.comparison = comparison;
            }

            public void EmitEqual(ILGenerator ilg)
            {
                constant.Load(ilg);

                if (comparison.IsStatic || comparison.DeclaringType.IsValueType)
                {
                    ilg.Emit(OpCodes.Call, comparison);
                }
                else
                {
                    ilg.Emit(OpCodes.Callvirt, comparison);
                }
            }

            public OpCode Equal_S => OpCodes.Brtrue_S;

            ICaseHandler ICaseHandler.Append(AstExpression code)
            {
                Append(code);

                return this;
            }

            void IPrivateCaseHandler.Emit(ILGenerator ilg, MyVariableAst variable, LocalBuilder local, Label label) => Emit(ilg, local, label);

            void IPrivateCaseHandler.EmitVoid(ILGenerator ilg, MyVariableAst variable, Label label) => EmitVoid(ilg, label);
        }

        private class SwitchCase
        {
            public SwitchCase(ConstantAst value, AstExpression body)
            {
                Value = value;
                Body = body;
            }

            public ConstantAst Value { get; }
            public AstExpression Body { get; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="switchAst"></param>
        protected SwitchAst(SwitchAst switchAst) : base(switchAst.RuntimeType)
        {
            defaultAst = switchAst.defaultAst;
            switchValue = switchAst.switchValue;
            switchCases = switchAst.switchCases;
            switchValueType = switchAst.switchValueType;
            switchValueKind = switchAst.switchValueKind;
        }

        private SwitchAst(AstExpression switchValue, Type returnType) : base(returnType)
        {
            if (switchValue is null)
            {
                throw new ArgumentNullException(nameof(switchValue));
            }

            switchValueType = switchValue.RuntimeType;

            if (switchValueType == typeof(object))
            {
                switchValueType = typeof(Type);

                switchValueKind = MySwitchValueKind.RuntimeType;
            }
            else if (IsArithmetic(switchValueType))
            {
                switchValueKind = MySwitchValueKind.Arithmetic;
            }
            else
            {
                switchValueKind = MySwitchValueKind.Equality;
            }

            switchCases = new List<IPrivateCaseHandler>();

            this.switchValue = switchValue;
        }

        /// <summary>
        /// 流程（无返回值）。
        /// </summary>
        public SwitchAst(AstExpression switchValue) : this(switchValue, typeof(void))
        {

        }

        /// <summary>
        /// 流程（无返回值）。
        /// </summary>
        public SwitchAst(AstExpression switchValue, AstExpression defaultAst) : this(switchValue, typeof(void))
        {
            this.defaultAst = defaultAst ?? throw new ArgumentNullException(nameof(defaultAst));
        }

        /// <summary>
        /// 流程。
        /// </summary>
        public SwitchAst(AstExpression switchValue, AstExpression defaultAst, Type returnType) : this(switchValue, returnType)
        {
            if (defaultAst is null)
            {
                throw new ArgumentNullException(nameof(defaultAst));
            }

            if (returnType == typeof(void) || EmitUtils.EqualSignatureTypes(defaultAst.RuntimeType, returnType) || defaultAst.RuntimeType.IsAssignableFrom(RuntimeType))
            {
                this.defaultAst = defaultAst;
            }
            else
            {
                throw new NotSupportedException($"默认模块“{defaultAst.RuntimeType}”和返回“{returnType}”类型无法默认转换!");
            }
        }

        /// <summary>
        /// 实例。
        /// </summary>
        /// <param name="constant">常量。</param>
        public ICaseHandler Case(ConstantAst constant)
        {
            if (constant is null)
            {
                throw new ArgumentNullException(nameof(constant));
            }

            IPrivateCaseHandler handler;

            switch (switchValueKind)
            {
                case MySwitchValueKind.Arithmetic when IsArithmetic(constant.RuntimeType):
                    handler = new SwitchCaseArithmeticAst(constant, RuntimeType);
                    break;
                case MySwitchValueKind.RuntimeType:
                    throw new AstException("当前流程控制为类型转换，请使用“{Case(VariableAst variable)}”方法处理！");
                case MySwitchValueKind.Equality:
                    var types = new Type[2] { switchValueType, constant.RuntimeType };
                    MethodInfo comparison = switchValueType.GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);

                    if (comparison is null && !EmitUtils.AreEquivalent(switchValueType, constant.RuntimeType))
                    {
                        comparison = constant.RuntimeType.GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
                    }

                    if (comparison is null && switchValueType.IsAssignableFrom(typeof(IEquatable<>).MakeGenericType(constant.RuntimeType)))
                    {
                        comparison = switchValueType.GetMethod("Equals", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { constant.RuntimeType }, null);
                    }

                    if (comparison is null)
                    {
                        throw new InvalidOperationException($"未找到“{constant.RuntimeType}”和“{switchValueType}”有效的比较函数!");
                    }

                    handler = new SwitchCaseEqualityAst(constant, comparison, RuntimeType);

                    break;
                default:
                    throw new NotSupportedException();
            }

            switchCases.Add(handler);

            return handler;
        }

        /// <summary>
        /// 实例（转换成功会自动为变量赋值）。
        /// </summary>
        /// <param name="variable">变量。</param>
        public ICaseHandler Case(VariableAst variable)
        {
            if (variable is null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            IPrivateCaseHandler handler;

            switch (switchValueKind)
            {
                case MySwitchValueKind.RuntimeType:
                    handler = new SwitchCaseRuntimeTypeAst(variable, RuntimeType);
                    break;
                case MySwitchValueKind.Arithmetic:
                case MySwitchValueKind.Equality:
                    throw new AstException("当前流程控制为值比较转换，请使用“{Case(ConstantAst constant)}”方法处理！");
                default:
                    throw new NotSupportedException();
            }

            switchCases.Add(handler);

            return handler;
        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (switchCases.Count == 0)
            {
                defaultAst?.Load(ilg);
            }
            else if (RuntimeType == typeof(void))
            {
                EmitVoid(ilg);
            }
            else
            {
                Emit(ilg);
            }
        }

        /// <summary>
        /// 发行变量和代码块(无返回值)。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="label">跳转位置。</param>
        protected virtual void EmitVoid(ILGenerator ilg, Label label)
        {
            LocalBuilder variable = ilg.DeclareLocal(switchValue.RuntimeType);

            switchValue.Load(ilg);

            ilg.Emit(OpCodes.Stloc, variable);

            int i = 0, len = switchCases.Count;

            var labels = new Label[len];

            for (; i < len; i++)
            {
                labels[i] = ilg.DefineLabel();
            }

            for (i = 0; i < len; i++)
            {
                var switchCase = switchCases[i];

                ilg.Emit(OpCodes.Ldloc, variable);

                switchCase.EmitEqual(ilg);

                var caseLabel = ilg.DefineLabel();

                ilg.Emit(switchCase.Equal_S, caseLabel);

                ilg.Emit(OpCodes.Br_S, labels[i]);

                ilg.MarkLabel(caseLabel);

                switchCase.EmitVoid(ilg, new MyVariableAst(variable), label);

                ilg.Emit(OpCodes.Br_S, label);

                ilg.MarkLabel(labels[i]);
            }

            if (defaultAst is null)
            {
                return;
            }

            FlowControl(defaultAst, ilg, label);
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
            LocalBuilder variable = ilg.DeclareLocal(switchValue.RuntimeType);

            switchValue.Load(ilg);

            ilg.Emit(OpCodes.Stloc, variable);

            int i = 0, len = switchCases.Count;

            var labels = new Label[len];

            for (; i < len; i++)
            {
                labels[i] = ilg.DefineLabel();
            }

            for (i = 0; i < len; i++)
            {
                var switchCase = switchCases[i];

                ilg.Emit(OpCodes.Ldloc, variable);

                switchCase.EmitEqual(ilg);

                var caseLabel = ilg.DefineLabel();

                ilg.Emit(switchCase.Equal_S, caseLabel);

                ilg.Emit(OpCodes.Br_S, labels[i]);

                ilg.MarkLabel(caseLabel);

                switchCase.Emit(ilg, new MyVariableAst(variable), local, label);

                ilg.Emit(OpCodes.Br_S, label);

                ilg.MarkLabel(labels[i]);
            }

            if (defaultAst is null)
            {
                return;
            }

            FlowControl(defaultAst, ilg, local, label);
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

        private static bool IsArithmetic(Type type)
        {
            if (type.IsEnum || type.IsNullable())
            {
                return false;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Double:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }
    }
}
