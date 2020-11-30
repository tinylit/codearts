using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CodeArts
{
    /// <summary>
    /// 程序集缓存。
    /// </summary>
    public class AssemblyFinder
    {
        private static readonly string assemblyPath;

        private static readonly ConcurrentDictionary<string, Assembly> AassemblyLoads = new ConcurrentDictionary<string, Assembly>();
        private static readonly ConcurrentDictionary<string, IEnumerable<Assembly>> AssemblyCache = new ConcurrentDictionary<string, IEnumerable<Assembly>>();

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static AssemblyFinder()
        {
            if (!Directory.Exists(assemblyPath = AppDomain.CurrentDomain.RelativeSearchPath))
            {
                assemblyPath = AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        /// <summary>
        /// 所有程序集。
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Assembly> FindAll() => Find("*.dll");

        /// <summary>
        /// 满足指定条件的程序集。
        /// </summary>
        /// <param name="pattern">过滤条件。</param>
        /// <returns></returns>
        public static IEnumerable<Assembly> Find(string pattern)
        {
            if (pattern is null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (!pattern.EndsWith(".dll"))
            {
                pattern += ".dll";
            }

            return AssemblyCache.GetOrAdd(pattern, searchPattern => Directory.GetFiles(assemblyPath, searchPattern).Select(x => AassemblyLoads.GetOrAdd(x, Assembly.LoadFrom)).ToList());
        }
    }
}
