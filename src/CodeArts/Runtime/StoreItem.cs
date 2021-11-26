using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CodeArts.Runtime
{
    /// <summary>
    /// 仓库项目。
    /// </summary>
    public interface IStoreItem
    {
        /// <summary>
        /// 名称。
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 命名。
        /// </summary>
        string Naming { get; }

        /// <summary>
        /// 成员类型（或方法返回值类型）。
        /// </summary>
        Type MemberType { get; }

        /// <summary>
        /// 属性。
        /// </summary>
        IEnumerable<Attribute> Attributes { get; }

        /// <summary>
        /// 命名。
        /// </summary>
        NamingAttribute NamingAttribute { get; }

        /// <summary>
        /// 是否定义了指定特性。
        /// </summary>
        /// <param name="attributeType">特性。</param>
        /// <returns></returns>
        bool IsDefined(Type attributeType);

        /// <summary>
        /// 是否定义了指定特性。
        /// </summary>
        /// <typeparam name="T">特性。</typeparam>
        /// <returns></returns>
        bool IsDefined<T>() where T : Attribute;

        /// <summary>
        /// 获取指定特性。
        /// </summary>
        /// <param name="attributeType">特性。</param>
        /// <returns></returns>
        Attribute GetCustomAttribute(Type attributeType);

        /// <summary>
        /// 获取指定特性。
        /// </summary>
        /// <typeparam name="T">特性。</typeparam>
        /// <returns></returns>
        T GetCustomAttribute<T>() where T : Attribute;
    }
    /// <summary>
    /// 仓库项目。
    /// </summary>
    /// <typeparam name="T">成员。</typeparam>
    public interface IStoreItem<T> : IStoreItem where T : MemberInfo
    {
        /// <summary>
        /// 成员信息。
        /// </summary>
        T Member { get; }

        /// <summary>
        /// 静态成员。
        /// </summary>
        bool IsStatic { get; }

        /// <summary>
        /// 公共成员。
        /// </summary>
        bool IsPublic { get; }

        /// <summary>
        /// 可读。
        /// </summary>
        bool CanRead { get; }

        /// <summary>
        /// 可写。
        /// </summary>
        bool CanWrite { get; }

        /// <summary>
        /// 是否定义了指定特性。
        /// </summary>
        /// <param name="attributeType">特性。</param>
        /// <param name="inherit">如果为 true，则指定还在 element 的祖先中搜索自定义属性。</param>
        /// <returns></returns>
        bool IsDefined(Type attributeType, bool inherit);

        /// <summary>
        /// 是否定义了指定特性。
        /// </summary>
        /// <typeparam name="TAttribute">特性。</typeparam>
        /// <param name="inherit">如果为 true，则指定还在 element 的祖先中搜索自定义属性。</param>
        /// <returns></returns>
        bool IsDefined<TAttribute>(bool inherit) where TAttribute : Attribute;

        /// <summary>
        /// 获取指定特性。
        /// </summary>
        /// <param name="attributeType">特性。</param>
        /// <param name="inherit">如果为 true，则指定还在 element 的祖先中搜索自定义属性。</param>
        /// <returns></returns>
        Attribute GetCustomAttribute(Type attributeType, bool inherit);

        /// <summary>
        /// 获取指定特性。
        /// </summary>
        /// <typeparam name="TAttribute">特性。</typeparam>
        /// <param name="inherit">如果为 true，则指定还在 element 的祖先中搜索自定义属性。</param>
        /// <returns></returns>
        TAttribute GetCustomAttribute<TAttribute>(bool inherit) where TAttribute : Attribute;
    }

    /// <summary>
    /// 仓库。
    /// </summary>
    public abstract class StoreItem : IStoreItem
    {
        private readonly Type declaringType;
        private readonly Func<Attribute[]> invokeAttrs;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="info">参数。</param>
        public StoreItem(Type info)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            MemberType = info;

            NamingAttribute = (NamingAttribute)Attribute.GetCustomAttribute(info, typeof(NamingAttribute));

            CustomAttributes = info.GetCustomAttributesData();

            invokeAttrs = () => Attribute.GetCustomAttributes(info);
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="info">参数。</param>
        public StoreItem(ParameterInfo info)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            MemberType = info.ParameterType;

            NamingAttribute = (NamingAttribute)Attribute.GetCustomAttribute(info, typeof(NamingAttribute));

            CustomAttributes = info.GetCustomAttributesData();

            invokeAttrs = () => Attribute.GetCustomAttributes(info);
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="info">参数。</param>
        public StoreItem(MemberInfo info)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            switch (info.MemberType)
            {
                case MemberTypes.Constructor:
                    MemberType = info.DeclaringType;
                    break;
                case MemberTypes.Event:
                    MemberType = ((EventInfo)info).EventHandlerType;
                    break;
                case MemberTypes.Field:
                    MemberType = ((FieldInfo)info).FieldType;
                    break;
                case MemberTypes.Method:
                    MemberType = ((MethodInfo)info).ReturnType;
                    break;
                case MemberTypes.Property:
                    MemberType = ((PropertyInfo)info).PropertyType;
                    break;
                case MemberTypes.TypeInfo:
                case MemberTypes.NestedType:
#if NET40
                    MemberType = (Type)info;
#else
                    if (info is TypeInfo typeInfo)
                    {
                        MemberType = typeInfo.AsType();
                    }
                    else
                    {
                        MemberType = (Type)info;
                    }
#endif
                    break;
                case MemberTypes.Custom:
                case MemberTypes.All:
                default:
                    break;
            }

            declaringType = info.DeclaringType;

            CustomAttributes = info.GetCustomAttributesData();

#if NET40
            NamingAttribute = (NamingAttribute)Attribute.GetCustomAttribute(info, typeof(NamingAttribute));
#else
            NamingAttribute = info.GetCustomAttribute<NamingAttribute>();
#endif

            invokeAttrs = () => Attribute.GetCustomAttributes(info);
        }

        private IEnumerable<Attribute> attributes;
        /// <summary>
        /// 属性标记集合。
        /// </summary>

#if NETSTANDARD2_1_OR_GREATER
        public IEnumerable<Attribute> Attributes => attributes ??= invokeAttrs.Invoke();
#else
        public IEnumerable<Attribute> Attributes => attributes ?? (attributes = invokeAttrs.Invoke());
#endif

        /// <summary>
        /// 自定义属性数据。
        /// </summary>
        public IEnumerable<CustomAttributeData> CustomAttributes { get; }

        /// <summary>
        /// 命名标记。
        /// </summary>
        public NamingAttribute NamingAttribute { get; }

        /// <summary>
        /// 名称。
        /// </summary>
        public abstract string Name { get; }

        private string _Naming = string.Empty;

        /// <summary>
        /// 命名名称。
        /// </summary>
        public virtual string Naming
        {
            get
            {
                if (_Naming.Length > 0)
                {
                    return _Naming;
                }

                var namingAttr = NamingAttribute;

                if (namingAttr is null)
                {
                    if (declaringType is null)
                    {
                        return _Naming = Name;
                    }

                    namingAttr = (NamingAttribute)Attribute.GetCustomAttribute(declaringType, typeof(NamingAttribute));

                    if (namingAttr is null)
                    {
                        return _Naming = Name;
                    }

                    return _Naming = Name.ToNamingCase(namingAttr.NamingType);
                }

                return _Naming = namingAttr.Name ?? Name.ToNamingCase(namingAttr.NamingType);
            }
        }

        /// <summary>
        /// 成员类型。
        /// </summary>
        public virtual Type MemberType { get; }

        /// <summary>
        /// 获取自定义类型属性标记。
        /// </summary>
        /// <param name="attributeType">属性类型。</param>
        /// <returns></returns>
        public Attribute GetCustomAttribute(Type attributeType)
        {
            bool flag = false;

            Attribute attribute = null;

            foreach (var attr in Attributes)
            {
                var type = attr.GetType();

                if (type == attributeType || type.IsSubclassOf(attributeType))
                {
                    if (flag)
                    {
                        throw new AmbiguousMatchException();
                    }

                    flag = true;

                    attribute = attr;
                }
            }

            return attribute;
        }

        /// <summary>
        /// 获取自定义类型属性标记。
        /// </summary>
        /// <typeparam name="TAttribute">属性类型。</typeparam>
        /// <returns></returns>
        public TAttribute GetCustomAttribute<TAttribute>() where TAttribute : Attribute => (TAttribute)GetCustomAttribute(typeof(TAttribute));

        /// <summary>
        /// 是否定义了指定类型属性标记。
        /// </summary>
        /// <param name="attributeType">属性类型。</param>
        /// <returns></returns>
#if NET40
        public bool IsDefined(Type attributeType) => CustomAttributes.Any(attr => attr.Constructor.DeclaringType == attributeType) || CustomAttributes.Any(attr => attr.Constructor.DeclaringType.IsSubclassOf(attributeType));
#else
        public bool IsDefined(Type attributeType) => CustomAttributes.Any(attr => attr.AttributeType == attributeType) || CustomAttributes.Any(attr => attr.AttributeType.IsSubclassOf(attributeType));
#endif
        /// <summary>
        /// 是否定义了指定类型属性标记。
        /// </summary>
        /// <typeparam name="TAttribute">属性类型。</typeparam>
        /// <returns></returns>
        public bool IsDefined<TAttribute>() where TAttribute : Attribute => IsDefined(typeof(TAttribute));
    }

    /// <summary>
    /// 仓库项目。
    /// </summary>
    /// <typeparam name="T">成员。</typeparam>
    public abstract class StoreItem<T> : StoreItem, IStoreItem<T> where T : MemberInfo
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="member">成员。</param>
        public StoreItem(T member) : base(member)
        {
            Member = member ?? throw new ArgumentNullException(nameof(member));
        }

        /// <summary>
        /// 名称。
        /// </summary>
        public override string Name => Member.Name;

        /// <summary>
        /// 成员。
        /// </summary>
        public T Member { get; }

        /// <summary>
        /// 忽略。
        /// </summary>
        public bool Ignore => IsDefined<IgnoreAttribute>();

        /// <summary>
        /// 静态成员。
        /// </summary>
        public abstract bool IsStatic { get; }

        /// <summary>
        /// 公共成员。
        /// </summary>
        public abstract bool IsPublic { get; }

        /// <summary>
        /// 是否可读。
        /// </summary>
        public abstract bool CanRead { get; }

        /// <summary>
        /// 是否可写。
        /// </summary>
        public abstract bool CanWrite { get; }

        /// <summary>
        /// 获取指定属性类型的属性标记。
        /// </summary>
        /// <param name="attributeType">属性类型。</param>
        /// <param name="inherit">如果为 true，则指定还在 element 的祖先中搜索自定义属性。</param>
        /// <returns></returns>
        public Attribute GetCustomAttribute(Type attributeType, bool inherit) => Attribute.GetCustomAttribute(Member, attributeType, inherit);

        /// <summary>
        /// 获取指定属性类型的属性标记。
        /// </summary>
        /// <typeparam name="TAttribute">属性类型。</typeparam>
        /// <param name="inherit">如果为 true，则指定还在 element 的祖先中搜索自定义属性。</param>
        /// <returns></returns>
        public TAttribute GetCustomAttribute<TAttribute>(bool inherit) where TAttribute : Attribute => (TAttribute)GetCustomAttribute(typeof(TAttribute), inherit);

        /// <summary>
        /// 是否声明了指定属性类型标记。
        /// </summary>
        /// <param name="attributeType">属性类型。</param>
        /// <param name="inherit">如果为 true，则指定还在 element 的祖先中搜索自定义属性。</param>
        /// <returns></returns>
        public bool IsDefined(Type attributeType, bool inherit) => Attribute.IsDefined(Member, attributeType, inherit);

        /// <summary>
        /// 是否声明了指定属性类型标记。
        /// </summary>
        /// <typeparam name="TAttribute">属性类型。</typeparam>
        /// <param name="inherit">如果为 true，则指定还在 element 的祖先中搜索自定义属性。</param>
        /// <returns></returns>
        public bool IsDefined<TAttribute>(bool inherit) where TAttribute : Attribute => IsDefined(typeof(TAttribute), inherit);
    }
}
