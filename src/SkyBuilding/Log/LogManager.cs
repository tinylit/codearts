using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SkyBuilding.Log
{
    /// <summary>
    /// 日志管理
    /// </summary>
    public static class LogManager
    {
        /// <summary> 日志对象集合 </summary>
        private static readonly ConcurrentDictionary<string, Logger> LoggerDictionary;

        /// <summary> 日志适配器集合 </summary>
        private static readonly ConcurrentDictionary<ILoggerAdapter, LogLevel> LoggerAdapters;

        /// <summary> 默认日志等级 </summary>
        private static LogLevel _logLevel = LogLevel.All;

        /// <summary> 静态构造 </summary>
        static LogManager()
        {
            LoggerDictionary = new ConcurrentDictionary<string, Logger>();
            LoggerAdapters = new ConcurrentDictionary<ILoggerAdapter, LogLevel>();
        }

        /// <summary> 日志等级，默认： LogLevel.All </summary>
        public static LogLevel Level
        {
            get => _logLevel;
            set
            {
                if (_logLevel == value)
                    return;

                _logLevel = value;

                if (value == LogLevel.Off)
                {
                    foreach (var adapter in LoggerAdapters.Where(x => x.Value != LogLevel.Off))
                    {
                        LoggerAdapters[adapter.Key] = value;
                    }
                }
                else
                {
                    foreach (var adapter in LoggerAdapters.Where(x => x.Value == LogLevel.Off || !IsEnableLevel(x.Value, value)))
                    {
                        LoggerAdapters[adapter.Key] = value;
                    }
                }
            }
        }

        /// <summary> 是否可启用目标日志等级 </summary>
        /// <param name="level">当前日志等级</param>
        /// <param name="targetLevel">目标日志等级</param>
        /// <returns></returns>
        private static bool IsEnableLevel(LogLevel level, LogLevel logLevel)
        {
            if (logLevel == LogLevel.Off)
                return false;

            return logLevel == LogLevel.All || (level & logLevel) == level;
        }

        /// <summary> 添加适配器 </summary>
        /// <param name="adapter">适配器</param>
        public static void AddAdapter(ILoggerAdapter adapter) => LoggerAdapters.TryAdd(adapter, _logLevel);

        /// <summary> 添加适配器 </summary>
        /// <param name="adapter">适配器</param>
        /// <param name="level">日志等级(可多位与)</param>
        public static void AddAdapter(ILoggerAdapter adapter, LogLevel level) => LoggerAdapters.AddOrUpdate(adapter, level, (_, _2) => level);

        /// <summary> 移除适配器 </summary>
        /// <param name="adapter"></param>
        public static void RemoveAdapter(ILoggerAdapter adapter) => RemoveAdapter(adapter.GetType());

        /// <summary> 移除适配器 </summary>
        /// <param name="adapterType"></param>
        public static void RemoveAdapter(Type adapterType) => LoggerAdapters.Where(t => t.Key.GetType() == adapterType).ForEach(t => LoggerAdapters.TryRemove(t.Key, out _));

        /// <summary> 清空适配器 </summary>
        public static void ClearAdapter() => LoggerAdapters.Clear();

        /// <summary>
        /// 获取日志记录实例
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Logger Logger(string name) => LoggerDictionary.GetOrAdd(name, _ => new Logger(name));

        /// <summary>
        /// 获取日志记录实例
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Logger Logger(Type type) => Logger(type.FullName);

        /// <summary>
        /// 获取日志记录实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Logger Logger<T>() => Logger(typeof(T));

        /// <summary>
        /// 递归执行日志适配器
        /// </summary>
        /// <param name="loggerName">适配器名称</param>
        /// <param name="level">日志等级</param>
        /// <param name="action">操作</param>
        internal static void EachAdapter(this string loggerName, LogLevel level, Action<ILog> action)
        {
            if (string.IsNullOrWhiteSpace(loggerName))
                return;

            LoggerAdapters
                .Where(t => IsEnableLevel(level, t.Value))
                .Select(t => t.Key.GetLogger(loggerName))
                .ForEach(action);
        }
    }
}
