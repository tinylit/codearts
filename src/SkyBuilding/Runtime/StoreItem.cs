using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SkyBuilding.Runtime
{
    /// <summary>
    /// 仓库项目
    /// </summary>
    public interface IStoreItem
    {
        /// <summary>
        /// 名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 命名 
        /// </summary>
        string Naming { get; }

        /// <summary>
        /// 属性
        /// </summary>
        IEnumerable<Attribute> Attributes { get; }

        /// <summary>
        /// 命名 
        /// </summary>
        NamingAttribute NamingAttribute { get; }

        /// <summary>
        /// 是否定义了指定特性。
        /// </summary>
        /// <param name="attributeType">特性</param>
        /// <returns></returns>
        bool IsDefined(Type attributeType);

        /// <summary>
        /// 是否定义了指定特性。
        /// </summary>
        /// <typeparam name="T">特性</typeparam>
        /// <returns></returns>
        bool IsDefined<T>() where T : Attribute;

        /// <summary>
        /// 获取指定特性。
        /// </summary>
        /// <param name="attributeType">特性</param>
        /// <returns></returns>
        Attribute GetCustomAttribute(Type attributeType);

        /// <summary>
        /// 获取指定特性。
        /// </summary>
        /// <typeparam name="T">特性</typeparam>
        /// <returns></returns>
        T GetCustomAttribute<T>() where T : Attribute;
    }
    /// <summary>
    /// 仓库项目
    /// </summary>
    /// <typeparam name="T">成员</typeparam>
    public interface IStoreItem<T> : IStoreItem where T : MemberInfo
    {
        /// <summary>
        /// 成员信息
        /// </summary>
        T Member { get; }

        /// <summary>
        /// 可读
        /// </summary>
        bool CanRead { get; }

        /// <summary>
        /// 可写
        /// </summary>
        bool CanWrite { get; }

        /// <summary>
        /// 成员类型（或方法返回值类型）
        /// </summary>
        Type MemberType { get; }

        /// <summary>
        /// 是否定义了指定特性。
        /// </summary>
        /// <param name="attributeType">特性</param>
        /// <returns></returns>
        bool IsDefined(Type attributeType, bool inherit);

        /// <summary>
        /// 是否定义了指定特性。
        /// </summary>
        /// <typeparam name="TAttribute">特性</typeparam>
        /// <returns></returns>
        bool IsDefined<TAttribute>(bool inherit) where TAttribute : Attribute;

        /// <summary>
        /// 获取指定特性。
        /// </summary>
        /// <param name="attributeType">特性</param>
        /// <returns></returns>
        Attribute GetCustomAttribute(Type attributeType, bool inherit);

        /// <summary>
        /// 获取指定特性。
        /// </summary>
        /// <typeparam name="TAttribute">特性</typeparam>
        /// <returns></returns>
        TAttribute GetCustomAttribute<TAttribute>(bool inherit) where TAttribute : Attribute;
    }

    /// <summary>
    /// 仓库
    /// </summary>
    public abstract class StoreItem : IStoreItem
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="attributes">特性</param>
        public StoreItem(IEnumerable<Attribute> attributes)
        {
            Attributes = attributes;
            NamingAttribute = attributes.OfType<NamingAttribute>().FirstOrDefault();
        }

        public IEnumerable<Attribute> Attributes { get; }

        public NamingAttribute NamingAttribute { get; }

        public abstract string Name { get; }

        private string _Naming = string.Empty;

        public virtual string Naming
        {
            get
            {
                if (_Naming.Length > 0) return _Naming;

                var namingAttr = NamingAttribute;

                string name = namingAttr?.Name ?? Name;

                if (namingAttr is null) return _Naming = name;

                return _Naming = name.ToNamingCase(namingAttr.NamingType);
            }
        }

        public Attribute GetCustomAttribute(Type attributeType) => Attributes.FirstOrDefault(x => x.GetType() == attributeType) ?? Attributes.FirstOrDefault(x => x.GetType().IsSubclassOf(attributeType));

        public T GetCustomAttribute<T>() where T : Attribute => (T)GetCustomAttribute(typeof(T));

        public bool IsDefined(Type attributeType) => Attributes.Any(attr => attr.GetType() == attributeType) || Attributes.Any(attr => attr.GetType().IsSubclassOf(attributeType));

        public bool IsDefined<T>() where T : Attribute => IsDefined(typeof(T));
    }

    /// <summary>
    /// 仓库项目
    /// </summary>
    /// <typeparam name="T">成员</typeparam>
    public abstract class StoreItem<T> : StoreItem, IStoreItem<T> where T : MemberInfo
    {
        private static readonly ConcurrentDictionary<Type, Attribute> AttrCache = new ConcurrentDictionary<Type, Attribute>();
        private static readonly ConcurrentDictionary<Type, bool> BoolCache = new ConcurrentDictionary<Type, bool>();
        public StoreItem(T member) : base(Attribute.GetCustomAttributes(member))
        {
            Member = member;
        }

        public override string Name => Member.Name;

        private string _Naming = string.Empty;

        public override string Naming
        {
            get
            {
                if (_Naming.Length > 0) return _Naming;

                var namingAttr = NamingAttribute;

                string name = namingAttr?.Name ?? Name;

                if (namingAttr is null)
                {
                    var typeStore = RuntimeTypeCache.Instance.GetCache(Member.DeclaringType);

                    namingAttr = typeStore.NamingAttribute;
                }

                if (namingAttr is null) return _Naming = name;

                return _Naming = name.ToNamingCase(namingAttr.NamingType);
            }
        }

        public T Member { get; }

        /// <summary>
        /// 成员类型
        /// </summary>
        public abstract Type MemberType { get; }

        public abstract bool CanRead { get; }

        public abstract bool CanWrite { get; }

        public Attribute GetCustomAttribute(Type attributeType, bool inherit)
        {
            var attr = GetCustomAttribute(attributeType);

            if (!inherit) return attr;

            return attr ?? AttrCache.GetOrAdd(attributeType, type => Attribute.GetCustomAttribute(Member, type, inherit));
        }

        public TAttribute GetCustomAttribute<TAttribute>(bool inherit) where TAttribute : Attribute => (TAttribute)GetCustomAttribute(typeof(TAttribute), inherit);

        public bool IsDefined(Type attributeType, bool inherit) => IsDefined(attributeType) || inherit && BoolCache.GetOrAdd(attributeType, type => Attribute.IsDefined(Member, attributeType, inherit));

        public bool IsDefined<TAttribute>(bool inherit) where TAttribute : Attribute => IsDefined(typeof(TAttribute), inherit);
    }
}
