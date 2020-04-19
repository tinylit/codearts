using System;
using System.Reflection;

namespace CodeArts.Emit
{
    /// <summary>
    /// 成员。
    /// </summary>
    public interface IMemberEmitter
    {
        /// <summary>
        /// 成员。
        /// </summary>
        MemberInfo Member { get; }

        /// <summary>
        /// 返回类型。
        /// </summary>
        Type ReturnType { get; }

        /// <summary>
        /// 发行。
        /// </summary>
        void Emit();
    }
}
