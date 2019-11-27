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

        /// <summary>
        /// 构造函数。
        /// </summary>
        public ModuleScope()
        {

        }

#if NET45 || NET451 || NET452 || NET461
        private readonly bool saveAssembly = false;
        private readonly List<AssemblyBuilder> builders;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="savePhysicalAssembly">保存程序集物理文件</param>
        public ModuleScope(bool savePhysicalAssembly)
        {
            if (saveAssembly = savePhysicalAssembly)
            {
                builders = new List<AssemblyBuilder>();
            }
        }
#endif
        /// <summary>
        /// 创建模块。
        /// </summary>
        /// <returns></returns>
        public ModuleBuilder Create(Type type) => ModuleCache.GetOrAdd(type.Assembly, assembly =>
        {
            var name = assembly.GetName().Name;

            var assemblyName = new AssemblyName(name + ".Proxies");

#if NET40
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
#elif NET45 || NET451 || NET452 || NET461
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, saveAssembly ? AssemblyBuilderAccess.RunAndSave : AssemblyBuilderAccess.Run);

            if (saveAssembly)
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
        public void SaveAssembly()
        {
            if (saveAssembly)
            {
                builders.ForEach(builder =>
                {
                    builder.Save(builder.GetName().Name + ".dll");
                });
            }
        }
#endif
    }
}
