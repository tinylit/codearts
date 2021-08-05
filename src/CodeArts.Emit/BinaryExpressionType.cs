namespace CodeArts.Emit
{
    /// <summary>
    /// 二进制（位赋值运算比位运算大一，如：<see cref="Add"/> + 1 = <seealso cref="AddAssign"/>）。
    /// </summary>
    public enum BinaryExpressionType
    {
        /// <summary>
        /// An addition operation, such as a + b, without overflow checking, for numeric operands.
        /// </summary>
        Add = 1,
        /// <summary>
        /// An addition compound assignment operation, such as (a += b), without overflow checking, for numeric operands.
        /// </summary>
        AddAssign = 2,
        /// <summary>
        /// An addition operation, such as (a + b), with overflow checking, for numeric operands.
        /// </summary>
        AddChecked = 3,
        /// <summary>
        /// An addition compound assignment operation, such as (a += b), with overflow checking, for numeric operands.
        /// </summary>
        AddAssignChecked = 4,
        /// <summary>
        /// A subtraction operation, such as (a - b), without overflow checking, for numeric operands.
        /// </summary>
        Subtract = 5,
        /// <summary>
        /// A subtraction compound assignment operation, such as (a -= b), without overflow checking, for numeric operands.
        /// </summary>
        SubtractAssign = 6,
        /// <summary>
        /// An arithmetic subtraction operation, such as (a - b), that has overflow checking, for numeric operands.
        /// </summary>
        SubtractChecked = 7,
        /// <summary>
        /// A subtraction compound assignment operation, such as (a -= b), that has overflow checking, for numeric operands.
        /// </summary>
        SubtractAssignChecked = 8,
        /// <summary>
        /// A multiplication operation, such as (a * b), without overflow checking, for numeric operands.
        /// </summary>
        Multiply = 9,
        /// <summary>
        /// A multiplication compound assignment operation, such as (a *= b), without overflow checking, for numeric operands.
        /// </summary>
        MultiplyAssign = 10,
        /// <summary>
        /// An multiplication operation, such as (a * b), that has overflow checking, for numeric operands.
        /// </summary>
        MultiplyChecked = 11,
        /// <summary>
        /// A multiplication compound assignment operation, such as (a *= b), that has overflow checking, for numeric operands.
        /// </summary>
        MultiplyAssignChecked = 12,
        /// <summary>
        /// A division operation, such as (a / b), for numeric operands.
        /// </summary>
        Divide = 13,
        /// <summary>
        /// An division compound assignment operation, such as (a /= b), for numeric operands.
        /// </summary>
        DivideAssign = 14,
        /// <summary>
        /// An arithmetic remainder operation, such as (a % b) in C# or (a Mod b) in Visual Basic.
        /// </summary>
        Modulo = 15,
        /// <summary>
        /// An arithmetic remainder compound assignment operation, such as (a %= b) in C#.
        /// </summary>
        ModuloAssign = 16,
        /// <summary>
        /// A bitwise or logical AND operation, such as (a &amp; b) in C# and (a And b) in Visual Basic.
        /// </summary>
        And = 17,
        /// <summary>
        /// A bitwise or logical AND compound assignment operation, such as (a &amp;= b) in C#.
        /// </summary>
        AndAssign = 18,
        /// <summary>
        /// A bitwise or logical OR operation, such as (a | b) in C# or (a Or b) in Visual Basic.
        /// </summary>
        Or = 19,
        /// <summary>
        /// A bitwise or logical OR compound assignment, such as (a |= b) in C#.
        /// </summary>
        OrAssign = 20,
        /// <summary>
        /// A bitwise or logical XOR operation, such as (a ^ b) in C# or (a Xor b) in Visual Basic.
        /// </summary>
        ExclusiveOr = 21,
        /// <summary>
        /// A bitwise or logical XOR compound assignment operation, such as (a ^= b) in C#.
        /// </summary>
        ExclusiveOrAssign = 22,
        /// <summary>
        /// A mathematical operation that raises a number to a power, such as (a ^ b) in Visual Basic.
        /// </summary>
        Power = 23,
        /// <summary>
        /// A compound assignment operation that raises a number to a power, such as (a ^= b) in Visual Basic.
        /// </summary>
        PowerAssign = 24,
        /// <summary>
        /// A bitwise left-shift operation, such as (a &lt;&lt; b).
        /// </summary>
        LeftShift = 25,
        /// <summary>
        /// A bitwise left-shift compound assignment, such as (a &lt;&lt;= b).
        /// </summary>
        LeftShiftAssign = 26,
        /// <summary>
        /// A bitwise right-shift operation, such as (a >> b).
        /// </summary>
        RightShift = 27,
        /// <summary>
        /// A bitwise right-shift compound assignment operation, such as (a >>= b).
        /// </summary>
        RightShiftAssign = 28,

        // --------------------- 以下为布尔指令。 -------------------------

        /// <summary>
        /// A short-circuiting conditional OR operation, such as (a || b) in C# or (a OrElse b) in Visual Basic.
        /// </summary>
        OrElse = 1024,
        /// <summary>
        /// A conditional AND operation that evaluates the second operand only if the first operand evaluates to true. It corresponds to (a &amp;&amp; b) in C# and (a AndAlso b) in Visual Basic.
        /// </summary>
        AndAlso = 1025,
        /// <summary>
        /// A node that represents an equality comparison, such as (a == b) in C# or (a = b) in Visual Basic.
        /// </summary>
        Equal = 1026,
        /// <summary>
        /// A "greater than" comparison, such as (a > b).
        /// </summary>
        GreaterThan = 1027,
        /// <summary>
        /// A "greater than or equal to" comparison, such as (a >= b).
        /// </summary>
        GreaterThanOrEqual = 1028,
        /// <summary>
        /// A "less than" comparison, such as (a &lt; b).
        /// </summary>
        LessThan = 1029,
        /// <summary>
        /// A "less than or equal to" comparison, such as (a &lt;= b).
        /// </summary>
        LessThanOrEqual = 1030,
        /// <summary>
        /// An inequality comparison, such as (a != b) in C# or (a &lt;> b) in Visual Basic.
        /// </summary>
        NotEqual = 1031
    }
}
