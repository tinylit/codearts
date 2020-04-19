using System;

namespace CodeArts.DbAnnotations
{
    /// <summary>
    /// 目标。
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public abstract class ObjectiveAttribute : Attribute
    {
        /// <summary>
        /// 名称（未指定是默认为参数名称）。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 命名规范
        /// </summary>
        public NamingType NamingType { get; set; }
    }
}
