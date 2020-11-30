using System;

namespace CodeArts.ORM
{
    /// <summary>
    /// 仓库。
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class RepositoryAttribute : Attribute
    {
        /// <summary>
        /// 仓库。
        /// </summary>
        /// <param name="repositoryType">仓库类型。</param>
        public RepositoryAttribute(Type repositoryType)
        {
            if (typeof(Repository).IsAssignableFrom(repositoryType))
            {
                RepositoryType = repositoryType;
            }
            else
            {
                throw new ArgumentException("不是有效的仓库类型!");
            }
        }
        /// <summary>
        /// 仓库类型。
        /// </summary>
        public Type RepositoryType { get; }
    }
}
