using System;
using System.Reflection.Emit;

namespace CodeArts.Emit
{
    /// <summary>
    /// 表达式
    /// </summary>
    public abstract class Expression
    {
        private static class AssignableVoid { }

        /// <summary>
        /// 暂不确定的类型。
        /// </summary>
        public static readonly Type AssignableVoidType = typeof(AssignableVoid);

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回类型。</param>
        protected Expression(Type returnType) => ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="iLGen">指令</param>
        public abstract void Emit(ILGenerator iLGen);

        /// <summary>
        /// 类型。
        /// </summary>
        public Type ReturnType { get; private set; }
    }
}
