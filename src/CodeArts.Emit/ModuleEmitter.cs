using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;

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
        private readonly INamingScope namingScope;

        private readonly string moduleName;
        private readonly string assemblyPath;

        // Used to lock the module builder creation
        private readonly object moduleLocker = new object();
#if NET40_OR_GREATER
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
        public ModuleEmitter(string moduleName)
            : this(moduleName, string.Concat(moduleName, ".dll"))
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="moduleName">程序集名称。</param>
        /// <param name="assemblyPath">程序集地址。</param>
        public ModuleEmitter(string moduleName, string assemblyPath)
            : this(false, new NamingScope(), moduleName, assemblyPath)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="naming">命名规则。</param>
        /// <param name="moduleName">程序集名称。</param>
        /// <param name="assemblyPath">程序集地址。</param>
        public ModuleEmitter(INamingScope naming, string moduleName, string assemblyPath) : this(false, naming, moduleName, assemblyPath)
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
        /// <param name="assemblyPath">程序集地址。</param>
        public ModuleEmitter(bool savePhysicalAssembly, string moduleName, string assemblyPath)
            : this(savePhysicalAssembly, new NamingScope(), moduleName, assemblyPath)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="savePhysicalAssembly">是否保存物理文件。</param>
        /// <param name="namingScope">命名规则。</param>
        /// <param name="moduleName">程序集名称。</param>
        /// <param name="assemblyPath">程序集地址。</param>
        public ModuleEmitter(bool savePhysicalAssembly, INamingScope namingScope,
                           string moduleName, string assemblyPath)
        {
            this.namingScope = namingScope ?? throw new ArgumentNullException(nameof(namingScope));

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
        public ModuleEmitter(string moduleName)
            : this(moduleName, string.Concat(moduleName, ".dll"))
        {
        }
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="moduleName">程序集名称。</param>
        /// <param name="assemblyPath">程序集地址。</param>
        public ModuleEmitter(string moduleName, string assemblyPath)
            : this(new NamingScope(), moduleName, assemblyPath)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="namingScope">命名规则。</param>
        /// <param name="moduleName">程序集名称。</param>
        /// <param name="assemblyPath">程序集地址。</param>
        public ModuleEmitter(INamingScope namingScope,
                           string moduleName, string assemblyPath)
        {
            this.namingScope = namingScope ?? throw new ArgumentNullException(nameof(namingScope));
            this.moduleName = moduleName;
            this.assemblyPath = assemblyPath;
        }
#endif

        /// <summary>
        /// 程序集名称。
        /// </summary>
        public string AssemblyFileName
        {
            get { return Path.GetFileName(assemblyPath); }
        }

#if NET45_OR_GREATER
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
#elif NET45_OR_GREATER
            if (savePhysicalAssembly)
            {
                AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                        assemblyName, AssemblyBuilderAccess.RunAndSave, AssemblyDirectory);

                return assemblyBuilder.DefineDynamicModule(moduleName, moduleName, false);
            }
            else
            {
                var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

                return assemblyBuilder.DefineDynamicModule(moduleName);
            }
#else
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

            return assemblyBuilder.DefineDynamicModule(moduleName);
#endif
        }

        /// <summary>
        /// 在此模块中用指定的名称为私有类型构造 TypeBuilder。
        /// </summary>
        /// <param name="name">类型的完整路径，其中包括命名空间。 name 不能包含嵌入的 null。</param>
        /// <returns>具有指定名称的私有类型。</returns>
        [SecuritySafeCritical]
        public ClassEmitter DefineType(string name) => DefineType(name, TypeAttributes.NotPublic);

        /// <summary>
        /// 在给定类型名称和类型特性的情况下，构造 TypeBuilder。
        /// </summary>
        /// <param name="name">类型的完整路径。 name 不能包含嵌入的 null。</param>
        /// <param name="attr">已定义类型的属性。</param>
        /// <returns>用所有请求的特性创建的 TypeBuilder。</returns>
        [SecuritySafeCritical]
        public ClassEmitter DefineType(string name, TypeAttributes attr) => DefineType(name, attr, typeof(object));

        /// <summary>
        /// 在给定类型名称、类型特性和已定义类型扩展的类型的情况下，构造 TypeBuilder。
        /// </summary>
        /// <param name="name">类型的完整路径。 name 不能包含嵌入的 null。</param>
        /// <param name="attr">与类型关联的属性。</param>
        /// <param name="parent">已定义类型扩展的类型。</param>
        /// <returns>用所有请求的特性创建的 TypeBuilder。</returns>
        [SecuritySafeCritical]
        public ClassEmitter DefineType(string name, TypeAttributes attr, Type parent) => DefineType(name, attr, parent, Type.EmptyTypes);

        /// <summary>
        /// 在给定类型名称、特性、已定义类型扩展的类型和已定义类型实现的接口的情况下，构造 TypeBuilder。
        /// </summary>
        /// <param name="name">类型的完整路径。 name 不能包含嵌入的 null。</param>
        /// <param name="attr">与类型关联的特性。</param>
        /// <param name="parent">已定义类型扩展的类型。</param>
        /// <param name="interfaces">类型实现的接口列表。</param>
        /// <returns>用所有请求的特性创建的 TypeBuilder。</returns>
        [ComVisible(true)]
        [SecuritySafeCritical]
        public ClassEmitter DefineType(string name, TypeAttributes attr, Type parent, Type[] interfaces)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

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

            return new ClassEmitter(DefineTypeBuilder(moduleBuilder, namingScope.GetUniqueName(name), attr, parent, interfaces), namingScope);
        }

        private static readonly Regex NamingPattern = new Regex("[^0-9a-zA-Z]+", RegexOptions.Singleline | RegexOptions.Compiled);

        private static TypeBuilder DefineTypeBuilder(ModuleBuilder moduleBuilder, string name, TypeAttributes attributes, Type baseType, Type[] interfaces)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            name = NamingPattern.Replace(name, string.Empty);

            if (baseType is null && interfaces is null)
            {
                return moduleBuilder.DefineType(name, attributes);
            }
            else if (interfaces is null || interfaces.Length == 0)
            {
                if (!baseType.IsGenericType)
                {
                    return moduleBuilder.DefineType(name, attributes, baseType);
                }

                var genericArguments = baseType.GetGenericArguments();

                if (!Array.Exists(genericArguments, x => x.IsGenericParameter))
                {
                    return moduleBuilder.DefineType(name, attributes, baseType);
                }

                var builder = moduleBuilder.DefineType(name, attributes);

                var names = new List<string>(genericArguments.Length);

                Array.ForEach(genericArguments, x =>
                {
                    if (x.IsGenericParameter)
                    {
                        names.Add(x.Name);
                    }
                });

                var typeParameterBuilders = builder.DefineGenericParameters(names.ToArray());

                foreach (var item in genericArguments.Zip(typeParameterBuilders, (g, t) =>
                {
                    t.SetGenericParameterAttributes(g.GenericParameterAttributes);

                    t.SetInterfaceConstraints(AdjustGenericConstraints(typeParameterBuilders, genericArguments, g.GetGenericParameterConstraints()));

                    t.SetBaseTypeConstraint(g.BaseType);

                    return true;
                })) { }

                int offset = 0;

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    if (genericArguments[i].IsGenericParameter)
                    {
                        genericArguments[i] = typeParameterBuilders[i - offset];
                    }
                    else
                    {
                        offset--;
                    }
                }

                builder.SetParent(baseType.GetGenericTypeDefinition().MakeGenericType(genericArguments));

                return builder;
            }
            else
            {
                //? 父类型是否有泛型参数。
                bool flag = false;

                var names = new Dictionary<Type, string>();

                if (baseType?.IsGenericType ?? false)
                {
                    Array.ForEach(baseType.GetGenericArguments(), x =>
                    {
                        if (x.IsGenericParameter)
                        {
                            flag = true;

                            names.Add(x, x.Name);
                        }
                    });
                }

                Array.ForEach(interfaces, x =>
                {
                    if (x.IsGenericType)
                    {
                        Array.ForEach(x.GetGenericArguments(), y =>
                        {
                            if (y.IsGenericParameter && !names.ContainsKey(y))
                            {
                                names.Add(y, y.Name);
                            }
                        });
                    }
                });

                if (names.Count == 0)
                {
                    return moduleBuilder.DefineType(name, attributes, baseType, interfaces);
                }

                var builder = flag
                    ? moduleBuilder.DefineType(name, attributes)
                    : moduleBuilder.DefineType(name, attributes, baseType);

                var typeParameterBuilders = builder.DefineGenericParameters(names.Values.ToArray());

                var genericTypes = names.Keys.ToArray();

                foreach (var item in genericTypes.Zip(typeParameterBuilders, (g, t) =>
                {
                    t.SetGenericParameterAttributes(g.GenericParameterAttributes);

                    t.SetInterfaceConstraints(AdjustGenericConstraints(typeParameterBuilders, genericTypes, g.GetGenericParameterConstraints()));

                    t.SetBaseTypeConstraint(g.BaseType);

                    return true;
                })) { }

                if (flag)
                {
                    var genericArguments = baseType.GetGenericArguments();

                    for (int i = 0; i < genericArguments.Length; i++)
                    {
                        int index = Array.IndexOf(genericTypes, genericArguments[i]);

                        if (index > -1)
                        {
                            genericArguments[i] = typeParameterBuilders[index];
                        }
                    }

                    builder.SetParent(baseType.GetGenericTypeDefinition().MakeGenericType(genericArguments));
                }

                Array.ForEach(interfaces, x =>
                {
                    if (x.IsGenericType)
                    {
                        var genericArguments = x.GetGenericArguments();

                        for (int i = 0; i < genericArguments.Length; i++)
                        {
                            int index = Array.IndexOf(genericTypes, genericArguments[i]);

                            if (index > -1)
                            {
                                genericArguments[i] = typeParameterBuilders[index];
                            }
                        }

                        builder.AddInterfaceImplementation(x.GetGenericTypeDefinition().MakeGenericType(genericArguments));
                    }
                    else
                    {
                        builder.AddInterfaceImplementation(x);
                    }
                });

                return builder;
            }
        }

        private static Type AdjustConstraintToNewGenericParameters(Type constraint, Type[] originalGenericParameters, GenericTypeParameterBuilder[] newGenericParameters)
        {
            if (constraint.IsGenericType)
            {
                var genericArgumentsOfConstraint = constraint.GetGenericArguments();

                for (var i = 0; i < genericArgumentsOfConstraint.Length; ++i)
                {
                    genericArgumentsOfConstraint[i] =
                        AdjustConstraintToNewGenericParameters(genericArgumentsOfConstraint[i], originalGenericParameters, newGenericParameters);
                }

                return constraint.GetGenericTypeDefinition().MakeGenericType(genericArgumentsOfConstraint);
            }
            else
            {
                return constraint;
            }
        }

        private static Type[] AdjustGenericConstraints(GenericTypeParameterBuilder[] newGenericParameters, Type[] originalGenericArguments, Type[] constraints)
        {
            Type[] adjustedConstraints = new Type[constraints.Length];
            for (var i = 0; i < constraints.Length; i++)
            {
                adjustedConstraints[i] = AdjustConstraintToNewGenericParameters(constraints[i], originalGenericArguments, newGenericParameters);
            }
            return adjustedConstraints;
        }

#if NET40_OR_GREATER
        /// <summary>
        /// 保存程序集。
        /// </summary>
        /// <returns>返回文件地址。</returns>
        public string SaveAssembly()
        {
            if (moduleBuilder is null)
            {
                throw new InvalidOperationException("未生成弱命名程序集。");
            }

            if (!savePhysicalAssembly)
            {
                throw new NotSupportedException("未设置保存为物理文件的支持!");
            }

            var assemblyBuilder = (AssemblyBuilder)moduleBuilder.Assembly;
            var assemblyFileName = AssemblyFileName;
            var assemblyFilePath = moduleBuilder.FullyQualifiedName;

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
