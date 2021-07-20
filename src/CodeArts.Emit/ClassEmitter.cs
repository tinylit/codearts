using System;
using System.Reflection.Emit;

namespace CodeArts.Emit
{
    /// <summary>
    /// 类。
    /// </summary>
    public sealed class ClassEmitter : AbstractTypeEmitter
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="builder">类型构造器。</param>
        /// <param name="namingScope">命名。</param>
        public ClassEmitter(TypeBuilder builder, INamingScope namingScope) : base(builder, namingScope)
        {
        }

        /// <summary>
        /// 创建类型。
        /// </summary>
        /// <returns></returns>
        public Type CreateType() => Emit();
    }
}
