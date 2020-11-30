using System;

namespace CodeArts
{
    /// <summary>
    /// 类工厂。
    /// </summary>
    public interface ITypeGen
    {
        /// <summary>
        /// 创建类。
        /// </summary>
        /// <param name="abstractType">抽象类型。</param>
        /// <returns>返回接口实现类。</returns>
        Type Create(Type abstractType);
    }
}
