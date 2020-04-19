using System;

namespace CodeArts
{
    /// <summary>
    /// 类工厂
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class TypeGenAttribute : Attribute
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="typeGenType">类型工厂类型:<see cref="ITypeGen"/></param>
        public TypeGenAttribute(Type typeGenType)
        {
            if (typeof(ITypeGen).IsAssignableFrom(typeGenType))
            {
                TypeGen = (ITypeGen)Activator.CreateInstance(typeGenType);
            }
            else
            {
                throw new ArgumentException();
            }
        }

        /// <summary>
        /// 类型工厂
        /// </summary>
        public ITypeGen TypeGen { get; }
    }
}
