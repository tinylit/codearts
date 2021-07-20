using System;
using System.Reflection;

namespace CodeArts.Emit
{
    /// <summary>
    /// 匿名类。
    /// </summary>
    public class NestedClassEmitter : AbstractTypeEmitter
    {

        /// <summary>
        /// 创建匿名类型的构造函数。
        /// </summary>
        /// <param name="typeEmitter">匿名类型的所属类型。</param>
        /// <param name="name">匿名类型名称。</param>
        public NestedClassEmitter(AbstractTypeEmitter typeEmitter, string name) : base(typeEmitter, name)
        {

        }

        /// <summary>
        /// 创建匿名类型的构造函数。
        /// </summary>
        /// <param name="typeEmitter">匿名类型的所属类型。</param>
        /// <param name="name">匿名类型名称。</param>
        /// <param name="attributes">匿名函数类型。</param>
        public NestedClassEmitter(AbstractTypeEmitter typeEmitter, string name, TypeAttributes attributes) : base(typeEmitter, name, attributes)
        {

        }

        /// <summary>
        /// 创建匿名类型的构造函数。
        /// </summary>
        /// <param name="typeEmitter">匿名类型的所属类型。</param>
        /// <param name="name">匿名类型名称。</param>
        /// <param name="attributes">匿名函数类型。</param>
        /// <param name="baseType">匿名函数基类。</param>
        public NestedClassEmitter(AbstractTypeEmitter typeEmitter, string name, TypeAttributes attributes, Type baseType) : base(typeEmitter, name, attributes, baseType)
        {

        }

        /// <summary>
        /// 创建匿名类型的构造函数。
        /// </summary>
        /// <param name="typeEmitter">匿名类型的所属类型。</param>
        /// <param name="name">匿名类型名称。</param>
        /// <param name="attributes">匿名函数类型。</param>
        /// <param name="baseType">匿名函数基类。</param>
        /// <param name="interfaces">匿名函数实现接口。</param>
        public NestedClassEmitter(AbstractTypeEmitter typeEmitter, string name, TypeAttributes attributes, Type baseType, Type[] interfaces) : base(typeEmitter, name, attributes, baseType, interfaces)
        {

        }
    }
}
