using System;

namespace SkyBuilding.Log
{
    /// <summary>
    /// 按名称缓存的日志实现适配器基类，用于创建并管理指定类型的日志实例
    /// </summary>
    public abstract class LoggerAdapter : ILoggerAdapter
    {
        /// <summary>
        /// 由指定类型获取日志实例
        /// </summary>
        /// <param name="type">指定类型</param>
        /// <returns></returns>
        public virtual ILog GetLogger(Type type) => GetLogger(type.FullName);

        /// <summary>
        /// 由指定名称获取日志实例
        /// </summary>
        /// <param name="name">指定名称</param>
        /// <returns></returns>
        public abstract ILog GetLogger(string name);
    }
}
