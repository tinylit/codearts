using System;
using System.Collections.Concurrent;
#if NET40
using System.Collections.ObjectModel;
#else
using System.Collections.Generic;
#endif
using System.Linq;

namespace CodeArts.Runtime
{
    /// <summary>
    /// 类型项目。
    /// </summary>
    public class TypeStoreItem : StoreItem
    {
        private static readonly ConcurrentDictionary<Type, TypeStoreItem> ItemCache = new ConcurrentDictionary<Type, TypeStoreItem>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="type">类型。</param>
        private TypeStoreItem(Type type) : base(type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        /// <summary>
        /// 类型名称。
        /// </summary>
        public override string Name => Type.Name;

        /// <summary>
        /// 类型全名。
        /// </summary>
        public string FullName => Type.FullName;

        /// <summary>
        /// 静态类。
        /// </summary>
        public bool IsStatic => Type.IsAbstract && Type.IsSealed;

        /// <summary>
        /// 公共类。
        /// </summary>
        public bool IsPublic => Type.IsPublic;

        /// <summary>
        /// 类型。
        /// </summary>
        public Type Type { get; }

        private static readonly object Lock_FieldObj = new object();
        private static readonly object Lock_PropertyObj = new object();
        private static readonly object Lock_MethodObj = new object();
        private static readonly object Lock_ConstructorObj = new object();

#if NET40


        private ReadOnlyCollection<PropertyStoreItem> propertyStores;

        /// <summary>
        /// 属性。
        /// </summary>
        public ReadOnlyCollection<PropertyStoreItem> PropertyStores
        {
            get
            {
                if (propertyStores is null)
                {
                    lock (Lock_PropertyObj)
                    {
                        if (propertyStores is null)
                        {
                            propertyStores = Type.GetProperties()
                                .Select(info => PropertyStoreItem.Get(info))
                                .ToList()
                                .AsReadOnly();
                        }
                    }
                }

                return propertyStores;
            }
        }

        private ReadOnlyCollection<FieldStoreItem> fieldStores;
        /// <summary>
        /// 字段。
        /// </summary>
        public ReadOnlyCollection<FieldStoreItem> FieldStores
        {
            get
            {
                if (fieldStores is null)
                {
                    lock (Lock_FieldObj)
                    {
                        if (fieldStores is null)
                        {
                            fieldStores = Type.GetFields()
                                .Select(info => FieldStoreItem.Get(info))
                                .ToList()
                                .AsReadOnly();
                        }
                    }
                }
                return fieldStores;
            }
        }

        private ReadOnlyCollection<MethodStoreItem> methodStores;
        /// <summary>
        /// 方法。
        /// </summary>
        public ReadOnlyCollection<MethodStoreItem> MethodStores
        {
            get
            {
                if (methodStores is null)
                {
                    lock (Lock_MethodObj)
                    {
                        if (methodStores is null)
                        {
                            methodStores = Type.GetMethods()
                                .Select(info => MethodStoreItem.Get(info))
                                .ToList()
                                .AsReadOnly();
                        }
                    }
                }
                return methodStores;
            }
        }

        private ReadOnlyCollection<ConstructorStoreItem> constructorStores;
        /// <summary>
        /// 构造函数。
        /// </summary>
        public ReadOnlyCollection<ConstructorStoreItem> ConstructorStores
        {
            get
            {
                if (constructorStores is null)
                {
                    lock (Lock_ConstructorObj)
                    {
                        if (constructorStores is null)
                        {
                            constructorStores = Type.GetConstructors()
                                .Select(info => ConstructorStoreItem.Get(info))
                                .ToList()
                                .AsReadOnly();
                        }
                    }
                }
                return constructorStores;
            }
        }
#else

        private IReadOnlyCollection<PropertyStoreItem> propertyStores;

        /// <summary>
        /// 属性。
        /// </summary>
        public IReadOnlyCollection<PropertyStoreItem> PropertyStores
        {
            get
            {
                if (propertyStores is null)
                {
                    lock (Lock_PropertyObj)
                    {
                        if (propertyStores is null)
                        {
                            propertyStores = Type.GetProperties()
                                .Select(info => PropertyStoreItem.Get(info))
                                .ToList();
                        }
                    }
                }

                return propertyStores;
            }
        }

        private IReadOnlyCollection<FieldStoreItem> fieldStores;
        /// <summary>
        /// 字段。
        /// </summary>
        public IReadOnlyCollection<FieldStoreItem> FieldStores
        {
            get
            {
                if (fieldStores is null)
                {
                    lock (Lock_FieldObj)
                    {
                        if (fieldStores is null)
                        {
                            fieldStores = Type.GetFields()
                                .Select(info => FieldStoreItem.Get(info))
                                .ToList();
                        }
                    }
                }
                return fieldStores;
            }
        }

        private IReadOnlyCollection<MethodStoreItem> methodStores;
        /// <summary>
        /// 方法。
        /// </summary>
        public IReadOnlyCollection<MethodStoreItem> MethodStores
        {
            get
            {
                if (methodStores is null)
                {
                    lock (Lock_MethodObj)
                    {
                        if (methodStores is null)
                        {
                            methodStores = Type.GetMethods()
                                .Select(info => MethodStoreItem.Get(info))
                                .ToList();
                        }
                    }
                }
                return methodStores;
            }
        }

        private IReadOnlyCollection<ConstructorStoreItem> constructorStores;
        /// <summary>
        /// 构造函数。
        /// </summary>
        public IReadOnlyCollection<ConstructorStoreItem> ConstructorStores
        {
            get
            {
                if (constructorStores is null)
                {
                    lock (Lock_ConstructorObj)
                    {
                        if (constructorStores is null)
                        {
                            constructorStores = Type.GetConstructors()
                                .Select(info => ConstructorStoreItem.Get(info))
                                .ToList();
                        }
                    }
                }
                return constructorStores;
            }
        }
#endif

        /// <summary>
        /// 获取仓储项目。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <returns></returns>
        public static TypeStoreItem Get<T>() => ItemCache.GetOrAdd(typeof(T), conversionType => new TypeStoreItem(conversionType));

        /// <summary>
        /// 获取仓储项目。
        /// </summary>
        /// <param name="type">类型。</param>
        /// <returns></returns>
        public static TypeStoreItem Get(Type type) => ItemCache.GetOrAdd(type ?? throw new ArgumentNullException(nameof(type)), conversionType => new TypeStoreItem(conversionType));
    }
}
