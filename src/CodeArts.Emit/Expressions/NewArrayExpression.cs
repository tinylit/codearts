using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 创建数组。
    /// </summary>
    [DebuggerDisplay("new {ReturnType.Name}[{size}]")]
    public class NewArrayExpression : Expression
    {
        private static readonly MethodInfo methodInfo = typeof(NewArrayExpression).GetMethod(nameof(ArrayType));
        private static readonly ConcurrentDictionary<Type, Type> TypeCache = new ConcurrentDictionary<Type, Type>();

        private readonly int size;

        /// <summary>
        /// 构造函数【生成Object的数组】。
        /// </summary>
        /// <param name="size">数组大小。</param>
        public NewArrayExpression(int size) : this(size, typeof(object))
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="size">数组大小。</param>
        /// <param name="elementType">元素类型。</param>
        public NewArrayExpression(int size, Type elementType) : base(TypeCache.GetOrAdd(elementType, type =>
        {
            var body = System.Linq.Expressions.Expression.Call(null, methodInfo.MakeGenericMethod(type));

            var lambda = System.Linq.Expressions.Expression.Lambda<Func<Type>>(body);

            var invoke = lambda.Compile();

            return invoke.Invoke();
        }))
        {
            this.size = size;
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="iLGen">指令</param>
        public override void Emit(ILGenerator iLGen)
        {
            iLGen.Emit(OpCodes.Ldc_I4, size);
            iLGen.Emit(OpCodes.Newarr, ReturnType);
        }

        private static Type ArrayType<T>() => typeof(T[]);
    }
}
