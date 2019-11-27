using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

#if NET45 || NET451 || NET452 || NET461
        private static readonly List<AssemblyBuilder> builders = new List<AssemblyBuilder>();

        /// <summary>
        /// 是否保存程序集。
        /// </summary>
        public bool SaveAssembly { get; set; }
#endif

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
#elif NET45 || NET451 || NET452 || NET461
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, SaveAssembly ? AssemblyBuilderAccess.RunAndSave : AssemblyBuilderAccess.Run);

            if (SaveAssembly)
            {
                builders.Add(assemblyBuilder);

                return assemblyBuilder.DefineDynamicModule(name + ".Dynamic", name + ".Proxies.dll");
            }

#else
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
#endif
            return assemblyBuilder.DefineDynamicModule(name + ".Dynamic");
        });

#if NET45 || NET451 || NET452 || NET461
        /// <summary>
        /// 保存程序集。
        /// </summary>
        public void Save()
        {
            builders.ForEach(builder => builder.Save(builder.GetName().Name + ".dll"));
        }
#endif
    }
}
