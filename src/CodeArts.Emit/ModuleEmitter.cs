using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit
{
    /// <summary>
    /// 模块。
    /// </summary>
    public class ModuleEmitter
    {
        /// <summary>
        ///   默认文件名称。
        /// </summary>
        public static readonly string DEFAULT_FILE_NAME = "CodeArts.Emit.Proxy.dll";

        /// <summary>
        ///   程序集名称。
        /// </summary>
        public static readonly string DEFAULT_ASSEMBLY_NAME = "CodeArts.Emit.Proxy";

        private ModuleBuilder moduleBuilder;

        private readonly string moduleName;
        private readonly string assemblyPath;

        // Used to lock the module builder creation
        private readonly object moduleLocker = new object();
#if NET40 || NET45 || NET451 || NET452 || NET461
        // Specified whether the generated assemblies are intended to be saved
        private readonly bool savePhysicalAssembly;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public ModuleEmitter() : this(false)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="moduleName">程序集名称。</param>
        /// <param name="assemblyPath">程序集地址</param>
        public ModuleEmitter(string moduleName, string assemblyPath)
            : this(false, new NamingProvider(), moduleName, assemblyPath)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="naming">命名规则。</param>
        /// <param name="moduleName">程序集名称。</param>
        /// <param name="assemblyPath">程序集地址</param>
        public ModuleEmitter(INamingProvider naming, string moduleName, string assemblyPath) : this(false, naming, moduleName, assemblyPath)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="savePhysicalAssembly">是否保存物理文件。</param>
        public ModuleEmitter(bool savePhysicalAssembly)
            : this(savePhysicalAssembly, DEFAULT_ASSEMBLY_NAME, DEFAULT_FILE_NAME)
        {
        }
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="savePhysicalAssembly">是否保存物理文件。</param>
        /// <param name="moduleName">程序集名称。</param>
        /// <param name="assemblyPath">程序集地址</param>
        public ModuleEmitter(bool savePhysicalAssembly, string moduleName, string assemblyPath)
            : this(savePhysicalAssembly, new NamingProvider(), moduleName, assemblyPath)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="savePhysicalAssembly">是否保存物理文件。</param>
        /// <param name="naming">命名规则。</param>
        /// <param name="moduleName">程序集名称。</param>
        /// <param name="assemblyPath">程序集地址</param>
        public ModuleEmitter(bool savePhysicalAssembly, INamingProvider naming,
                           string moduleName, string assemblyPath)
        {
            Naming = naming;

            this.savePhysicalAssembly = savePhysicalAssembly;
            this.moduleName = moduleName;
            this.assemblyPath = assemblyPath;
        }
#else
        /// <summary>
        /// 构造函数。
        /// </summary>
        public ModuleEmitter()
            : this(DEFAULT_ASSEMBLY_NAME, DEFAULT_FILE_NAME)
        {
        }
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="moduleName">程序集名称。</param>
        /// <param name="assemblyPath">程序集地址</param>
        public ModuleEmitter(string moduleName, string assemblyPath)
            : this(new NamingProvider(), moduleName, assemblyPath)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="naming">命名规则。</param>
        /// <param name="moduleName">程序集名称。</param>
        /// <param name="assemblyPath">程序集地址</param>
        public ModuleEmitter(INamingProvider naming,
                           string moduleName, string assemblyPath)
        {
            Naming = naming;
            this.moduleName = moduleName;
            this.assemblyPath = assemblyPath;
        }
#endif
        /// <summary>
        /// 命名。
        /// </summary>
        public INamingProvider Naming { get; }

        /// <summary>
        /// 模块构造器。
        /// </summary>
        public ModuleBuilder Value
        {
            get
            {
                if (moduleBuilder is null)
                {
                    lock (moduleLocker)
                    {
                        if (moduleBuilder is null)
                        {
                            moduleBuilder = CreateModule();
                        }
                    }
                }
                return moduleBuilder;
            }
        }

        /// <summary>
        /// 程序集名称。
        /// </summary>
        public string AssemblyFileName
        {
            get { return Path.GetFileName(assemblyPath); }
        }

#if NET45 || NET451 || NET452 || NET461
        /// <summary>
        /// 程序集文件地址。
        /// </summary>
        public string AssemblyDirectory
        {
            get
            {
                var directory = Path.GetDirectoryName(assemblyPath);
                if (directory == string.Empty)
                {
                    return null;
                }
                return directory;
            }
        }
#endif
        private ModuleBuilder CreateModule()
        {
            var assemblyName = new AssemblyName
            {
                Name = this.moduleName
            };
            var moduleName = AssemblyFileName;
#if NET40
            if (savePhysicalAssembly)
            {
                AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
                return assemblyBuilder.DefineDynamicModule(moduleName, moduleName, false);
            }
            else
            {
                AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                return assemblyBuilder.DefineDynamicModule(moduleName);
            }
#elif NET45 || NET451 || NET452 || NET461
            if (savePhysicalAssembly)
            {
                AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                        assemblyName, AssemblyBuilderAccess.RunAndSave, AssemblyDirectory);

                return assemblyBuilder.DefineDynamicModule(moduleName, moduleName, false);
            }
            else
            {
                var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
                return assemblyBuilder.DefineDynamicModule(moduleName);
            }
#else
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

            var module = assemblyBuilder.DefineDynamicModule(moduleName);
            return module;
#endif
        }


#if NET40 || NET45 || NET451 || NET452 || NET461
        /// <summary>
        /// 保存程序集。
        /// </summary>
        /// <returns>返回文件地址。</returns>
        public string SaveAssembly()
        {
            if (Value is null)
            {
                throw new InvalidOperationException("No weak-named assembly has been generated.");
            }

            if (!savePhysicalAssembly)
            {
                throw new NotSupportedException("未设置保存为物理文件的支持!");
            }

            var assemblyBuilder = (AssemblyBuilder)Value.Assembly;
            var assemblyFileName = AssemblyFileName;
            var assemblyFilePath = Value.FullyQualifiedName;

            if (File.Exists(assemblyFilePath))
            {
                File.Delete(assemblyFilePath);
            }

            assemblyBuilder.Save(assemblyFileName);

            return assemblyFilePath;
        }
#endif
    }
}
