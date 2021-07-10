using System;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit
{
    /// <summary>
    /// 类。
    /// </summary>
    public class ClassEmitter : AbstractTypeEmitter
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="builder">类型构造器。</param>
        public ClassEmitter(TypeBuilder builder) : base(builder, new NamingProvider())
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="name">类名。</param>
        public ClassEmitter(ModuleEmitter module, string name) : base(module.Value.DefineType(module.Naming.GetUniqueName(name)), module.Naming) { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="name">类名。</param>
        /// <param name="attributes">类属性。</param>
        public ClassEmitter(ModuleEmitter module, string name, TypeAttributes attributes) : base(module.Value.DefineType(module.Naming.GetUniqueName(name), attributes), module.Naming) { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="name">类名。</param>
        /// <param name="attributes">类属性。</param>
        /// <param name="baseType">父类型。</param>
        public ClassEmitter(ModuleEmitter module, string name, TypeAttributes attributes, Type baseType) : base(module.Value.DefineType(module.Naming.GetUniqueName(name), attributes, baseType), module.Naming) { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="name">类名。</param>
        /// <param name="attributes">类属性。</param>
        /// <param name="baseType">父类型。</param>
        /// <param name="interfaces">接口。</param>
        public ClassEmitter(ModuleEmitter module, string name, TypeAttributes attributes, Type baseType, Type[] interfaces) : base(module.Value.DefineType(module.Naming.GetUniqueName(name), attributes, baseType, interfaces), module.Naming) { }
    }
}
