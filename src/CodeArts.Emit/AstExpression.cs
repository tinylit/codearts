using CodeArts.Emit.Expressions;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit
{
    /// <summary>
    /// 表达式。
    /// </summary>
    public abstract class AstExpression
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回类型。</param>
        protected AstExpression(Type returnType) => RuntimeType = returnType ?? throw new ArgumentNullException(nameof(returnType));

        /// <summary>
        /// 是否可写。
        /// </summary>
        public virtual bool CanWrite => false;

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public abstract void Load(ILGenerator ilg);

        /// <summary>
        /// 检查是否可以进行赋值运算。
        /// </summary>
        /// <param name="value">值。</param>
        protected virtual bool AssignChecked(AstExpression value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!CanWrite)
            {
                return false;
            }

            var returnType = RuntimeType;

            if (returnType == typeof(void))
            {
                throw new AstException("不能对无返回值类型进行赋值运算!");
            }

            if (value is ThisAst)
            {
                return true;
            }

            var valueType = value.RuntimeType;

            if (valueType == typeof(void))
            {
                throw new AstException("无返回值类型赋值不能用于赋值运算!");
            }

            if (valueType == returnType || returnType.IsAssignableFrom(valueType))
            {
                return true;
            }

            if (valueType.IsByRef || returnType.IsByRef)
            {
                if (valueType.IsByRef)
                {
                    valueType = valueType.GetElementType();
                }

                if (returnType.IsByRef)
                {
                    returnType = returnType.GetElementType();
                }

                if (valueType == returnType || returnType.IsAssignableFrom(valueType))
                {
                    return true;
                }
            }

            if (valueType.IsEnum || returnType.IsEnum)
            {
                if (valueType.IsEnum)
                {
                    valueType = Enum.GetUnderlyingType(valueType);
                }

                if (returnType.IsEnum)
                {
                    returnType = Enum.GetUnderlyingType(returnType);
                }

                if (valueType == returnType || returnType.IsAssignableFrom(valueType))
                {
                    return true;
                }
            }

            if (returnType.IsNullable())
            {
                return Enum.GetUnderlyingType(returnType) == valueType;
            }

            throw new AstException($"“{value.RuntimeType}”无法对类型“{RuntimeType}”赋值!");
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected virtual void Assign(ILGenerator ilg, AstExpression value) => throw new NotSupportedException();

        /// <summary>
        /// 空表达式数组。
        /// </summary>
        public static readonly AstExpression[] EmptyAsts = new AstExpression[0];

        #region 表达式模块

        /// <summary>
        /// 成员本身。
        /// </summary>
        [DebuggerDisplay("this")]
        private class ThisAst : AstExpression
        {
            /// <summary>
            /// 单例。
            /// </summary>
            public static ThisAst Instance = new ThisAst();

            /// <summary>
            /// 构造函数。
            /// </summary>
            private ThisAst() : base(typeof(object))
            {
            }

            /// <summary>
            /// 加载。
            /// </summary>
            /// <param name="ilg">指令。</param>
            public override void Load(ILGenerator ilg)
            {
                ilg.Emit(OpCodes.Ldarg_0);
            }
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        [DebuggerDisplay("{left} = {right}")]
        private class AssignAst : AstExpression
        {
            private readonly AstExpression left;
            private readonly AstExpression right;
            public AssignAst(AstExpression left, AstExpression right) : base(right.RuntimeType)
            {
                this.left = left ?? throw new ArgumentNullException(nameof(left));

                if (left.AssignChecked(right))
                {
                    this.right = right ?? throw new ArgumentNullException(nameof(right));
                }
                else
                {
                    throw new ArgumentException("表达式左侧只读!");
                }
            }

            public override void Load(ILGenerator ilg) => left.Assign(ilg, Convert(right, RuntimeType));
        }
        private class VBlockAst : BlockAst
        {
            private readonly LocalBuilder variable;
            private readonly Label label;
            public VBlockAst(BlockAst blockAst, LocalBuilder variable, Label label) : base(blockAst)
            {
                this.variable = variable;
                this.label = label;
            }
            protected override void Emit(ILGenerator ilg) => Emit(ilg, variable, label);
            protected override void EmitVoid(ILGenerator ilg) => EmitVoid(ilg, label);
        }

        private class VTryAst : TryAst
        {
            private readonly LocalBuilder variable;
            private readonly Label label;
            public VTryAst(TryAst tryAst, LocalBuilder variable, Label label) : base(tryAst)
            {
                this.variable = variable;
                this.label = label;
            }
            protected override void Emit(ILGenerator ilg) => Emit(ilg, variable, label);
            protected override void EmitVoid(ILGenerator ilg) => EmitVoid(ilg, label);
        }

        private class VSwitchAst : SwitchAst
        {
            private readonly LocalBuilder variable;
            private readonly Label label;
            public VSwitchAst(SwitchAst switchAst, LocalBuilder variable, Label label) : base(switchAst)
            {
                this.variable = variable;
                this.label = label;
            }
            protected override void Emit(ILGenerator ilg)
            {
                var label = ilg.DefineLabel();

                Emit(ilg, variable, label, this.label);

                ilg.MarkLabel(label);
            }
            protected override void EmitVoid(ILGenerator ilg)
            {
                var label = ilg.DefineLabel();

                EmitVoid(ilg, label, this.label);

                ilg.MarkLabel(label);
            }
        }

        private class VoidSwitchAst : SwitchAst
        {
            private readonly Label label;

            public VoidSwitchAst(SwitchAst blockAst, Label label) : base(blockAst)
            {
                this.label = label;
            }
            
            protected override void Emit(ILGenerator ilg)
            {
                var label = ilg.DefineLabel();

                EmitVoid(ilg, label, this.label);

                ilg.MarkLabel(label);
            }
            protected override void EmitVoid(ILGenerator ilg)
            {
                var label = ilg.DefineLabel();

                EmitVoid(ilg, label, this.label);

                ilg.MarkLabel(label);
            }
        }


        private class VoidBlockAst : BlockAst
        {
            private readonly Label label;

            public VoidBlockAst(BlockAst blockAst, Label label) : base(blockAst)
            {
                this.label = label;
            }
            protected override void Emit(ILGenerator ilg) => EmitVoid(ilg, label);
            protected override void EmitVoid(ILGenerator ilg) => EmitVoid(ilg, label);
        }

        private class VoidTryAst : TryAst
        {
            private readonly Label label;
            public VoidTryAst(TryAst tryAst, Label label) : base(tryAst)
            {
                this.label = label;
            }

            protected override void Emit(ILGenerator ilg) => EmitVoid(ilg);
            protected override void EmitVoid(ILGenerator ilg) => EmitVoid(ilg, label);
        }
        #endregion

        /// <summary>
        /// 流程控制模块使用：发行忽略返回值的表达式。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <param name="ilg">指令。</param>
        /// <param name="label">跳转目标。</param>
        public static void FlowControl(AstExpression node, ILGenerator ilg, Label label)
        {
            switch (node)
            {
                case TryAst tryAst:
                    new VoidTryAst(tryAst, label)
                        .Load(ilg);
                    break;
                case BlockAst blockAst:
                    new VoidBlockAst(blockAst, label)
                        .Load(ilg);
                    break;
                case SwitchAst switchAst:
                    new VoidSwitchAst(switchAst, label)
                     .Load(ilg);
                    break;
                default:
                    node?.Load(ilg);
                    break;
            }
        }

        /// <summary>
        /// 流程控制模块使用：发行表达式。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <param name="ilg">指令。</param>
        /// <param name="local">返回值存放的变量。</param>
        /// <param name="label">跳转目标。</param>
        public static void FlowControl(AstExpression node, ILGenerator ilg, LocalBuilder local, Label label)
        {
            switch (node)
            {
                case TryAst tryAst:
                    new VTryAst(tryAst, local, label)
                    .Load(ilg);
                    break;
                case BlockAst blockAst:
                    new VBlockAst(blockAst, local, label)
                     .Load(ilg);
                    break;
                case SwitchAst switchAst:
                    new VSwitchAst(switchAst, local, label)
                     .Load(ilg);
                    break;
                default:
                    if (node is null)
                    {
                        break;
                    }

                    if (node.RuntimeType == typeof(void))
                    {
                        node.Load(ilg);
                    }
                    else
                    {
                        node.Load(ilg);

                        if (EmitUtils.EqualSignatureTypes(node.RuntimeType, local.LocalType) || node.RuntimeType.IsAssignableFrom(local.LocalType))
                        {
                            EmitUtils.EmitConvertToType(ilg, node.RuntimeType, local.LocalType, true);

                            ilg.Emit(OpCodes.Stloc, local);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 类型。
        /// </summary>
        public Type RuntimeType { get; private set; }

        /// <summary>
        /// 当前上下文。
        /// </summary>
        public static AstExpression This => ThisAst.Instance;

        /// <summary>
        /// 类型转换。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <param name="convertToType">转换类型。</param>
        /// <returns></returns>
        public static ConvertAst Convert(AstExpression body, Type convertToType) => new ConvertAst(body, convertToType);

        /// <summary>
        /// 默认值。
        /// </summary>
        /// <param name="defaultType">默认值。</param>
        /// <returns></returns>
        public static DefaultAst Default(Type defaultType) => new DefaultAst(defaultType);

        /// <summary>
        /// 常量。
        /// </summary>
        /// <param name="value">值。</param>
        /// <returns></returns>
        public static ConstantAst Constant(object value) => new ConstantAst(value);

        /// <summary>
        /// 常量。
        /// </summary>
        /// <param name="value">值。</param>
        /// <param name="constantType">常量类型。</param>
        /// <returns></returns>
        public static ConstantAst Constant(object value, Type constantType) => new ConstantAst(value, constantType);

        /// <summary>
        /// 变量。
        /// </summary>
        /// <param name="variableType">变量类型。</param>
        /// <returns></returns>
        public static VariableAst Variable(Type variableType) => new VariableAst(variableType);

#if NET40_OR_GREATER
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="variableType">类型。</param>
        /// <param name="name">变量名称。</param>
        public static VariableAst Variable(Type variableType, string name) => new VariableAst(variableType, name);
#endif

        /// <summary>
        /// 类型是。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <param name="bodyIsType">类型。</param>
        /// <returns></returns>
        public static TypeIsAst TypeIs(AstExpression body, Type bodyIsType) => new TypeIsAst(body, bodyIsType);

        /// <summary>
        /// 类型转为。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <param name="bodyAsType">类型。</param>
        /// <returns></returns>
        public static TypeAsAst TypeAs(AstExpression body, Type bodyAsType) => new TypeAsAst(body, bodyAsType);

        /// <summary>
        /// 创建实例。
        /// </summary>
        /// <param name="instanceType">实例类型。</param>
        /// <returns></returns>
        public static NewInstanceAst New(Type instanceType) => new NewInstanceAst(instanceType);

        /// <summary>
        /// 创建实例。
        /// </summary>
        /// <param name="constructor">构造函数。</param>
        /// <returns></returns>
        public static NewInstanceAst New(ConstructorInfo constructor) => new NewInstanceAst(constructor);

        /// <summary>
        /// 创建实例。
        /// </summary>
        /// <param name="instanceType">实例类型。</param>
        /// <param name="parameters">参数。</param>
        /// <returns></returns>
        public static NewInstanceAst New(Type instanceType, params AstExpression[] parameters) => new NewInstanceAst(instanceType, parameters);

        /// <summary>
        /// 创建实例。
        /// </summary>
        /// <param name="constructor">构造函数。</param>
        /// <param name="parameters">参数。</param>
        /// <returns></returns>
        public static NewInstanceAst New(ConstructorInfo constructor, params AstExpression[] parameters) => new NewInstanceAst(constructor, parameters);

        /// <summary>
        /// 创建 object[]。
        /// </summary>
        /// <param name="size">数组大小。</param>
        /// <returns></returns>
        public static NewArrayAst NewArray(int size) => new NewArrayAst(size);

        /// <summary>
        /// 创建 <paramref name="elementType"/>[]。
        /// </summary>
        /// <param name="size">数组大小。</param>
        /// <param name="elementType">数组元素类型。</param>
        /// <returns></returns>
        public static NewArrayAst NewArray(int size, Type elementType) => new NewArrayAst(size, elementType);

        /// <summary>
        /// 创建 object[]。
        /// </summary>
        /// <param name="arguments">元素。</param>
        /// <returns></returns>
        public static ArrayAst Array(params AstExpression[] arguments) => new ArrayAst(arguments);

        /// <summary>
        /// 创建 object[]。
        /// </summary>
        /// <param name="elementType">元素类型。</param>
        /// <param name="arguments">元素。</param>
        /// <returns></returns>
        public static ArrayAst Array(Type elementType, params AstExpression[] arguments) => new ArrayAst(arguments, elementType);

        /// <summary>
        /// 数组索引。
        /// </summary>
        /// <param name="array">数组。</param>
        /// <param name="index">索引。</param>
        /// <returns></returns>
        public static ArrayIndexAst ArrayIndex(AstExpression array, int index) => new ArrayIndexAst(array, index);

        /// <summary>
        /// 数组索引。
        /// </summary>
        /// <param name="array">数组。</param>
        /// <param name="index">索引。</param>
        /// <returns></returns>
        public static ArrayIndexAst ArrayIndex(AstExpression array, AstExpression index) => new ArrayIndexAst(array, index);

        /// <summary>
        /// 数组长度。
        /// </summary>
        /// <param name="array">数组。</param>
        /// <returns></returns>
        public static ArrayLengthAst ArrayLength(AstExpression array) => new ArrayLengthAst(array);

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static AstExpression Assign(AstExpression left, AstExpression right) => new AssignAst(left, right);

        /// <summary>
        /// 空合并运算符。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static CoalesceAst Coalesce(AstExpression left, AstExpression right) => new CoalesceAst(left, right);

        /// <summary>
        /// 加。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst Add(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.Add, right);

        /// <summary>
        /// 加(检查溢出)。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst AddChecked(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.AddChecked, right);

        /// <summary>
        /// 加等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst AddAssign(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.AddAssign, right);

        /// <summary>
        /// 加等于(检查溢出)。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst AddAssignChecked(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.AddAssignChecked, right);

        /// <summary>
        /// 减。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst Subtract(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.Subtract, right);

        /// <summary>
        /// 减(检查溢出)。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst SubtractChecked(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.SubtractChecked, right);

        /// <summary>
        /// 减等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst SubtractAssign(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.SubtractAssign, right);

        /// <summary>
        /// 减等于(检查溢出)。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst SubtractAssignChecked(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.SubtractAssignChecked, right);

        /// <summary>
        /// 乘。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst Multiply(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.Multiply, right);

        /// <summary>
        /// 乘（检查溢出）。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst MultiplyChecked(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.MultiplyChecked, right);

        /// <summary>
        /// 乘等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst MultiplyAssign(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.MultiplyAssign, right);

        /// <summary>
        /// 乘等于（检查溢出）。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst MultiplyAssignChecked(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.MultiplyAssignChecked, right);

        /// <summary>
        /// 除。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst Divide(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.Divide, right);

        /// <summary>
        /// 除等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst DivideAssign(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.DivideAssign, right);

        /// <summary>
        /// 取模。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst Modulo(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.Modulo, right);

        /// <summary>
        /// 取模等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst ModuloAssign(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.ModuloAssign, right);

        /// <summary>
        /// 小于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst LessThan(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.LessThan, right);

        /// <summary>
        /// 小于等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst LessThanOrEqual(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.LessThanOrEqual, right);

        /// <summary>
        /// 位运算：或。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst Or(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.Or, right);

        /// <summary>
        /// 位运算：或等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst OrAssign(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.OrAssign, right);

        /// <summary>
        /// 位运算：且。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst And(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.And, right);

        /// <summary>
        /// 位运算：且等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst AndAssign(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.AndAssign, right);

        /// <summary>
        /// 位运算：异或。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst ExclusiveOr(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.ExclusiveOr, right);

        /// <summary>
        /// 位运算：异或等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst ExclusiveOrAssign(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.ExclusiveOrAssign, right);

        /// <summary>
        /// 等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst Equal(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.Equal, right);

        /// <summary>
        /// 大于等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst GreaterThanOrEqual(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.GreaterThanOrEqual, right);

        /// <summary>
        /// 大于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst GreaterThan(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.GreaterThan, right);

        /// <summary>
        /// 不等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst NotEqual(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.NotEqual, right);

        /// <summary>
        /// 或。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst OrElse(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.OrElse, right);

        /// <summary>
        /// 且。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst AndAlso(AstExpression left, AstExpression right) => new BinaryAst(left, BinaryExpressionType.AndAlso, right);

        /// <summary>
        /// 按位补运算或逻辑反运算。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <returns></returns>
        public static UnaryAst Not(AstExpression body) => new UnaryAst(body, UnaryExpressionType.Not);

        /// <summary>
        /// 是否为假。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <returns></returns>
        public static UnaryAst IsFalse(AstExpression body) => new UnaryAst(body, UnaryExpressionType.IsFalse);

        /// <summary>
        /// 增量(i + 1)。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <returns></returns>
        public static UnaryAst Increment(AstExpression body) => new UnaryAst(body, UnaryExpressionType.Increment);

        /// <summary>
        /// 减量(i - 1)。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <returns></returns>
        public static UnaryAst Decrement(AstExpression body) => new UnaryAst(body, UnaryExpressionType.Decrement);

        /// <summary>
        /// 递增(i += 1)。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <returns></returns>
        public static UnaryAst IncrementAssign(AstExpression body) => new UnaryAst(body, UnaryExpressionType.IncrementAssign);

        /// <summary>
        /// 递减(i -= 1)。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <returns></returns>
        public static UnaryAst DecrementAssign(AstExpression body) => new UnaryAst(body, UnaryExpressionType.DecrementAssign);

        /// <summary>
        /// 正负反转。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <returns></returns>
        public static UnaryAst Negate(AstExpression body) => new UnaryAst(body, UnaryExpressionType.Negate);

        /// <summary>
        /// 流程（无返回值）。
        /// </summary>
        /// <param name="switchValue">判断依据。</param>
        /// <returns><see cref="void"/></returns>
        public static SwitchAst Switch(AstExpression switchValue) => new SwitchAst(switchValue);

        /// <summary>
        /// 流程(无返回值)。
        /// </summary>
        /// <param name="switchValue">判断依据。</param>
        /// <param name="defaultAst">默认流程。</param>
        /// <returns></returns>
        public static SwitchAst Switch(AstExpression switchValue, AstExpression defaultAst) => new SwitchAst(switchValue, defaultAst);

        /// <summary>
        /// 流程(返回“<paramref name="returnType"/>”类型)。
        /// </summary>
        /// <param name="switchValue">判断依据。</param>
        /// <param name="defaultAst">默认流程。</param>
        /// <param name="returnType">流程返回值。</param>
        /// <returns></returns>
        public static SwitchAst Switch(AstExpression switchValue, AstExpression defaultAst, Type returnType) => new SwitchAst(switchValue, defaultAst, returnType);

        /// <summary>
        /// 条件判断。
        /// </summary>
        /// <param name="test">条件。</param>
        /// <param name="ifTrue">为真时，执行的代码。</param>
        /// <returns></returns>
        public static IfThenAst IfThen(AstExpression test, AstExpression ifTrue) => new IfThenAst(test, ifTrue);

        /// <summary>
        /// 条件判断。
        /// </summary>
        /// <param name="test">条件。</param>
        /// <param name="ifTrue">为真时，执行的代码。</param>
        /// <param name="ifFalse">为假时，执行的代码。</param>
        /// <returns></returns>
        public static IfThenElseAst IfThenElse(AstExpression test, AstExpression ifTrue, AstExpression ifFalse) => new IfThenElseAst(test, ifTrue, ifFalse);

        /// <summary>
        /// 三目运算。
        /// </summary>
        /// <param name="test">条件。</param>
        /// <param name="ifTrue">为真时，执行的代码。</param>
        /// <param name="ifFalse">为假时，执行的代码。</param>
        /// <returns></returns>
        public static ConditionAst Condition(AstExpression test, AstExpression ifTrue, AstExpression ifFalse) => new ConditionAst(test, ifTrue, ifFalse);

        /// <summary>
        /// 三目运算。
        /// </summary>
        /// <param name="test">条件。</param>
        /// <param name="ifTrue">为真时，执行的代码。</param>
        /// <param name="ifFalse">为假时，执行的代码。</param>
        /// <param name="returnType">返回类型。</param>
        /// <returns></returns>
        public static ConditionAst Condition(AstExpression test, AstExpression ifTrue, AstExpression ifFalse, Type returnType) => new ConditionAst(test, ifTrue, ifFalse, returnType);

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="methodInfo">方法。</param>
        /// <returns><paramref name="methodInfo"/>.ReturnType</returns>
        public static MethodCallAst Call(MethodInfo methodInfo) => new MethodCallAst(methodInfo);

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="methodInfo">方法。</param>
        /// <param name="arguments">方法参数。</param>
        /// <returns><paramref name="methodInfo"/>.ReturnType</returns>
        public static MethodCallAst Call(MethodInfo methodInfo, params AstExpression[] arguments) => new MethodCallAst(methodInfo, arguments);

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="methodInfo">方法。</param>
        /// <returns><paramref name="methodInfo"/>.ReturnType</returns>
        public static MethodCallAst Call(AstExpression instanceAst, MethodInfo methodInfo) => new MethodCallAst(instanceAst, methodInfo);

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="methodInfo">方法。</param>
        /// <param name="arguments">方法参数。</param>
        /// <returns><paramref name="methodInfo"/>.ReturnType</returns>
        public static MethodCallAst Call(AstExpression instanceAst, MethodInfo methodInfo, params AstExpression[] arguments) => new MethodCallAst(instanceAst, methodInfo, arguments);

        /// <summary>
        /// 调用方法：返回“<paramref name="methodInfo"/>.ReturnType”。。<see cref="MethodBase.Invoke(object, object[])"/>
        /// </summary>
        /// <param name="methodInfo">方法。</param>
        /// <param name="arguments">参数<see cref="object"/>[]。</param>
        /// <returns><paramref name="methodInfo"/>.ReturnType</returns>
        public static InvocationAst Invoke(MethodInfo methodInfo, AstExpression arguments) => new InvocationAst(methodInfo, arguments);

        /// <summary>
        /// 调用方法：返回“<paramref name="methodInfo"/>.ReturnType”。<see cref="MethodBase.Invoke(object, object[])"/>
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="methodInfo">方法。</param>
        /// <param name="arguments">参数<see cref="object"/>[]。</param>
        /// <returns><paramref name="methodInfo"/>.ReturnType</returns>
        public static InvocationAst Invoke(AstExpression instanceAst, MethodInfo methodInfo, AstExpression arguments) => new InvocationAst(instanceAst, methodInfo, arguments);

        /// <summary>
        /// 调用静态方法。<see cref="MethodBase.Invoke(object, object[])"/>
        /// </summary>
        /// <param name="methodAst">方法表达式。</param>
        /// <param name="arguments">参数<see cref="object"/>[]。</param>
        /// <returns></returns>
        public static InvocationAst Invoke(AstExpression methodAst, AstExpression arguments) => new InvocationAst(methodAst, arguments);

        /// <summary>
        /// 调用方法。<see cref="MethodBase.Invoke(object, object[])"/>
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="methodAst">方法表达式。</param>
        /// <param name="arguments">参数<see cref="object"/>[]。</param>
        /// <returns></returns>
        public static InvocationAst Invoke(AstExpression instanceAst, AstExpression methodAst, AstExpression arguments) => new InvocationAst(instanceAst, methodAst, arguments);

        /// <summary>
        /// 代码块。
        /// </summary>
        /// <param name="returnType">返回值。</param>
        /// <returns></returns>
        public static BlockAst Block(Type returnType) => new BlockAst(returnType);

        /// <summary>
        /// 抛出异常。
        /// </summary>
        /// <param name="exceptionType">异常类型。</param>
        /// <returns></returns>
        public static ThrowAst Throw(Type exceptionType) => new ThrowAst(exceptionType);

        /// <summary>
        /// 抛出异常。
        /// </summary>
        /// <param name="exceptionType">异常类型。</param>
        /// <param name="errorMsg">异常消息。</param>
        /// <returns></returns>
        public static ThrowAst Throw(Type exceptionType, string errorMsg) => new ThrowAst(exceptionType, errorMsg);

        /// <summary>
        /// 抛出异常。
        /// </summary>
        /// <param name="expression">异常表达式。</param>
        /// <returns></returns>
        public static ThrowAst Throw(AstExpression expression) => new ThrowAst(expression);

        /// <summary>
        /// 异常处理。
        /// </summary>
        /// <param name="returnType">返回值。</param>
        /// <returns></returns>
        public static TryAst Try(Type returnType) => new TryAst(returnType);

        /// <summary>
        /// 异常处理。
        /// </summary>
        /// <param name="returnType">返回值。</param>
        /// <param name="finallyAst">一定会执行的代码。</param>
        /// <returns></returns>
        public static TryAst Try(Type returnType, AstExpression finallyAst) => new TryAst(returnType, finallyAst);

        /// <summary>
        /// 字段。
        /// </summary>
        /// <param name="field">字段。</param>
        /// <returns></returns>
        public static FieldAst Field(FieldInfo field) => new FieldAst(field);

        /// <summary>
        /// 属性。
        /// </summary>
        /// <param name="property">属性。</param>
        /// <returns></returns>
        public static PropertyAst Property(PropertyInfo property) => new PropertyAst(property);

        /// <summary>
        /// 参数。
        /// </summary>
        /// <param name="parameter">参数。</param>
        /// <returns></returns>
        public static ParameterAst Paramter(ParameterInfo parameter) => new ParameterAst(parameter);

        /// <summary>
        /// 参数。
        /// </summary>
        /// <param name="paramterType">参数类型。</param>
        /// <param name="position">参数位置。</param>
        /// <returns></returns>
        public static ParameterAst Paramter(Type paramterType, int position) => new ParameterAst(paramterType, position);

        /// <summary>
        /// 将当前堆载顶部的数据返回。
        /// </summary>
        /// <returns></returns>
        public static ReturnAst Return() => new ReturnAst();

        /// <summary>
        /// 执行指定代码，并返回其数据。
        /// </summary>
        /// <param name="body">代码块。</param>
        /// <returns></returns>
        public static ReturnAst Return(AstExpression body) => new ReturnAst(body);

        /// <summary>
        /// 无返回值（自动清除栈顶数据）。
        /// </summary>
        /// <returns></returns>
        public static ReturnAst ReturnVoid() => ReturnAst.Void;
    }
}
