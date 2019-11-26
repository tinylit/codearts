using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace SkyBuilding.Proxies
{
    /// <summary>
    /// 模块范围。
    /// </summary>
    public class ModuleScope
    {
        private static readonly ConcurrentDictionary<Assembly, ModuleBuilder> ModuleCache = new ConcurrentDictionary<Assembly, ModuleBuilder>();

        /// <summary>
        /// 创建模块。
        /// </summary>
        /// <returns></returns>
        public virtual ModuleBuilder Create(Type type) => ModuleCache.GetOrAdd(type.Assembly, assembly =>
        {
            var name = assembly.GetName().Name;

            var assemblyName = new AssemblyName(name + ".Proxies");

#if NET40
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
#else
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
#endif
            return assemblyBuilder.DefineDynamicModule(name + ".Dynamic");
        });
    }
}
