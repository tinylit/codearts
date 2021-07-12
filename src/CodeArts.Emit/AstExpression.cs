using CodeArts.Emit.Expressions;
using System;
using System.Linq.Expressions;
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
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        public virtual void Assign(ILGenerator ilg, AstExpression value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var returnType = RuntimeType;

            if (returnType == typeof(void))
            {
                throw new AstException("不能对无返回值类型进行赋值运算!");
            }

            if (value is ThisAst)
            {
                goto label_core;
            }

            var valueType = value.RuntimeType;

            if (valueType == typeof(void))
            {
                throw new AstException("无返回值类型赋值不能用于赋值运算!");
            }

            if (valueType != returnType && !returnType.IsAssignableFrom(valueType) && (valueType.IsByRef ? valueType.GetElementType() : valueType) != (returnType.IsByRef ? returnType.GetElementType() : returnType))
            {
                throw new AstException("值表达式类型和当前表达式类型不相同!");
            }

        label_core:

            if (CanWrite)
            {
                AssignCore(ilg, value);
            }
            else
            {
                throw new AstException("当前表达式不可写!");
            }
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected virtual void AssignCore(ILGenerator ilg, AstExpression value) => throw new NotImplementedException();

        /// <summary>
        /// 空表达式数组。
        /// </summary>
        public static readonly AstExpression[] EmptyAsts = new AstExpression[0];

        /// <summary>
        /// 类型。
        /// </summary>
        public Type RuntimeType { get; private set; }

        /// <summary>
        /// 当前上下文。
        /// </summary>
        public static ThisAst This => ThisAst.Instance;

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
        public static AssignAst Assign(AstExpression left, AstExpression right) => new AssignAst(left, right);

        /// <summary>
        /// 空合并运算符。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static CoalesceAst Coalesce(AstExpression left, AstExpression right) => new CoalesceAst(left, right);

        /// <summary>
        /// 加法。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst Add(AstExpression left, AstExpression right) => new BinaryAst(left, ExpressionType.Add, right);

        /// <summary>
        /// 减法。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst Subtract(AstExpression left, AstExpression right) => new BinaryAst(left, ExpressionType.Subtract, right);

        /// <summary>
        /// 乘法。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst Multiply(AstExpression left, AstExpression right) => new BinaryAst(left, ExpressionType.Multiply, right);

        /// <summary>
        /// 除法。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst Divide(AstExpression left, AstExpression right) => new BinaryAst(left, ExpressionType.Divide, right);

        /// <summary>
        /// 小于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst LessThan(AstExpression left, AstExpression right) => new BinaryAst(left, ExpressionType.LessThan, right);

        /// <summary>
        /// 小于等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst LessThanOrEqual(AstExpression left, AstExpression right) => new BinaryAst(left, ExpressionType.LessThanOrEqual, right);

        /// <summary>
        /// 等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst Equal(AstExpression left, AstExpression right) => new BinaryAst(left, ExpressionType.Equal, right);

        /// <summary>
        /// 大于等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst GreaterThanOrEqual(AstExpression left, AstExpression right) => new BinaryAst(left, ExpressionType.GreaterThanOrEqual, right);

        /// <summary>
        /// 大于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst GreaterThan(AstExpression left, AstExpression right) => new BinaryAst(left, ExpressionType.GreaterThan, right);

        /// <summary>
        /// 不等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns></returns>
        public static BinaryAst NotEqual(AstExpression left, AstExpression right) => new BinaryAst(left, ExpressionType.NotEqual, right);

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
        /// <param name="method">方法。</param>
        /// <returns></returns>
        public static MethodCallAst Call(MethodInfo method) => new MethodCallAst(method);

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="method">方法。</param>
        /// <param name="arguments">方法参数。</param>
        /// <returns></returns>
        public static MethodCallAst Call(MethodInfo method, params AstExpression[] arguments) => new MethodCallAst(method, arguments);

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="method">方法。</param>
        /// <returns></returns>
        public static MethodCallAst Call(AstExpression instanceAst, MethodInfo method) => new MethodCallAst(instanceAst, method);

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="method">方法。</param>
        /// <param name="arguments">方法参数。</param>
        /// <returns></returns>
        public static MethodCallAst Call(AstExpression instanceAst, MethodInfo method, params AstExpression[] arguments) => new MethodCallAst(instanceAst, method, arguments);

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="overrideAst">重写方法。</param>
        /// <returns></returns>
        public static MethodCallAst Call(OverrideAst overrideAst) => new MethodCallAst(overrideAst);

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="overrideAst">重写方法。</param>
        /// <param name="arguments">方法参数。</param>
        /// <returns></returns>
        public static MethodCallAst Call(OverrideAst overrideAst, params AstExpression[] arguments) => new MethodCallAst(overrideAst, arguments);

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="overrideAst">重写方法。</param>
        /// <returns></returns>
        public static MethodCallAst Call(AstExpression instanceAst, OverrideAst overrideAst) => new MethodCallAst(instanceAst, overrideAst);

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="overrideAst">重写方法。</param>
        /// <param name="arguments">方法参数。</param>
        /// <returns></returns>
        public static MethodCallAst Call(AstExpression instanceAst, OverrideAst overrideAst, params AstExpression[] arguments) => new MethodCallAst(instanceAst, overrideAst, arguments);

        /// <summary>
        /// 调用静态方法。<see cref="MethodBase.Invoke(object, object[])"/>
        /// </summary>
        /// <param name="method">方法。</param>
        /// <param name="arguments">参数<see cref="object"/>[]。</param>
        /// <returns></returns>
        public static InvocationAst Invoke(MethodInfo method, AstExpression arguments) => new InvocationAst(method, arguments);

        /// <summary>
        /// 调用方法。<see cref="MethodBase.Invoke(object, object[])"/>
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="method">方法。</param>
        /// <param name="arguments">参数<see cref="object"/>[]。</param>
        /// <returns></returns>
        public static InvocationAst Invoke(AstExpression instanceAst, MethodInfo method, AstExpression arguments) => new InvocationAst(instanceAst, method, arguments);

        /// <summary>
        /// 调用静态方法。<see cref="MethodBase.Invoke(object, object[])"/>
        /// </summary>
        /// <param name="overrideAst">重写方法。</param>
        /// <param name="arguments">参数<see cref="object"/>[]。</param>
        /// <returns></returns>
        public static InvocationAst Invoke(OverrideAst overrideAst, AstExpression arguments) => new InvocationAst(overrideAst, arguments);

        /// <summary>
        /// 调用方法。<see cref="MethodBase.Invoke(object, object[])"/>
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="overrideAst">重写方法。</param>
        /// <param name="arguments">参数<see cref="object"/>[]。</param>
        /// <returns></returns>
        public static InvocationAst Invoke(AstExpression instanceAst, OverrideAst overrideAst, AstExpression arguments) => new InvocationAst(instanceAst, overrideAst, arguments);

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
        /// 捕获任意异常。
        /// </summary>
        /// <param name="body">异常处理。</param>
        /// <returns></returns>
        public static CatchAst Catch(AstExpression body) => new CatchAst(body);

        /// <summary>
        /// 捕获任意异常，并将异常赋值给指定变量。
        /// </summary>
        /// <param name="body">异常处理。</param>
        /// <param name="variable">变量。</param>
        /// <returns></returns>
        public static CatchAst Catch(AstExpression body, VariableAst variable) => new CatchAst(body, variable);

        /// <summary>
        /// 捕获指定类型的异常。
        /// </summary>
        /// <param name="body">异常处理。</param>
        /// <param name="exceptionType">异常类型。</param>
        /// <returns></returns>
        public static CatchAst Catch(AstExpression body, Type exceptionType) => new CatchAst(body, exceptionType);

        /// <summary>
        /// 捕获指定类型的异常，并将异常赋值给指定变量。
        /// </summary>
        /// <param name="body">异常处理。</param>
        /// <param name="exceptionType">异常类型。</param>
        /// <param name="variable">变量。</param>
        /// <returns></returns>
        public static CatchAst Catch(AstExpression body, Type exceptionType, VariableAst variable) => new CatchAst(body, exceptionType, variable);

        /// <summary>
        /// 始终执行的代码。
        /// </summary>
        /// <param name="body">代码。</param>
        /// <returns></returns>
        public static FinallyAst Finally(AstExpression body) => new FinallyAst(body);

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
        /// 清除当前堆载顶部的数据。
        /// </summary>
        /// <returns></returns>
        public static VoidAst Void() => new VoidAst();

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
    }
}
