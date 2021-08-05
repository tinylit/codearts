namespace CodeArts.Emit
{
    /// <summary>
    /// 一元运算符。
    /// </summary>
    public enum UnaryExpressionType
    {
        /// <summary>
        /// A unary increment operation, such as (a + 1) in C# and Visual Basic. The object a should not be modified in place.
        /// </summary>
        Increment = 1,
        /// <summary>
        /// A unary increment operation, such as (a += 1) in C# and Visual Basic. The object a should not be modified in place.
        /// </summary>
        IncrementAssign = 2,
        /// <summary>
        /// A unary decrement operation, such as (a - 1) in C# and Visual Basic. The object a should not be modified in place.
        /// </summary>
        Decrement = 3,
        /// <summary>
        /// A unary decrement operation, such as (a -= 1) in C# and Visual Basic. The object a should not be modified in place.
        /// </summary>
        DecrementAssign = 4,
        /// <summary>
        /// A node that represents a unary plus operation. The result of a predefined unary plus operation is simply the value of the operand, but user-defined implementations may have non-trivial results.
        /// </summary>
        UnaryPlus = 5,
        /// <summary>
        /// An arithmetic negation operation, such as (-a). The object a should not be modified in place.
        /// </summary>
        Negate = 6,
        /// <summary>
        /// A bitwise complement or logical negation operation. In C#, it is equivalent to (~a) for integral types and to (!a) for Boolean values. In Visual Basic, it is equivalent to (Not a). The object a should not be modified in place.
        /// </summary>
        Not = 7,
        /// <summary>
        /// A false condition value.
        /// </summary>
        IsFalse = 8
    }
}
