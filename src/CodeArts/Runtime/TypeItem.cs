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
    public class TypeItem : StoreItem
    {
        private static readonly ConcurrentDictionary<Type, TypeItem> ItemCache = new ConcurrentDictionary<Type, TypeItem>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="type">类型。</param>
        private TypeItem(Type type) : base(type)
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


        private ReadOnlyCollection<PropertyItem> propertyStores;

        /// <summary>
        /// 属性。
        /// </summary>
        public ReadOnlyCollection<PropertyItem> PropertyStores
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
                                .Select(info => PropertyItem.Get(info))
                                .ToList()
                                .AsReadOnly();
                        }
                    }
                }

                return propertyStores;
            }
        }

        private ReadOnlyCollection<FieldItem> fieldStores;
        /// <summary>
        /// 字段。
        /// </summary>
        public ReadOnlyCollection<FieldItem> FieldStores
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
                                .Select(info => FieldItem.Get(info))
                                .ToList()
                                .AsReadOnly();
                        }
                    }
                }
                return fieldStores;
            }
        }

        private ReadOnlyCollection<MethodItem> methodStores;
        /// <summary>
        /// 方法。
        /// </summary>
        public ReadOnlyCollection<MethodItem> MethodStores
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
                                .Select(info => MethodItem.Get(info))
                                .ToList()
                                .AsReadOnly();
                        }
                    }
                }
                return methodStores;
            }
        }

        private ReadOnlyCollection<ConstructorItem> constructorStores;
        /// <summary>
        /// 构造函数。
        /// </summary>
        public ReadOnlyCollection<ConstructorItem> ConstructorStores
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
                                .Select(info => ConstructorItem.Get(info))
                                .ToList()
                                .AsReadOnly();
                        }
                    }
                }
                return constructorStores;
            }
        }
#else

        private IReadOnlyCollection<PropertyItem> propertyStores;

        /// <summary>
        /// 属性。
        /// </summary>
        public IReadOnlyCollection<PropertyItem> PropertyStores
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
                                .Select(info => PropertyItem.Get(info))
                                .ToList();
                        }
                    }
                }

                return propertyStores;
            }
        }

        private IReadOnlyCollection<FieldItem> fieldStores;
        /// <summary>
        /// 字段。
        /// </summary>
        public IReadOnlyCollection<FieldItem> FieldStores
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
                                .Select(info => FieldItem.Get(info))
                                .ToList();
                        }
                    }
                }
                return fieldStores;
            }
        }

        private IReadOnlyCollection<MethodItem> methodStores;
        /// <summary>
        /// 方法。
        /// </summary>
        public IReadOnlyCollection<MethodItem> MethodStores
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
                                .Select(info => MethodItem.Get(info))
                                .ToList();
                        }
                    }
                }
                return methodStores;
            }
        }

        private IReadOnlyCollection<ConstructorItem> constructorStores;
        /// <summary>
        /// 构造函数。
        /// </summary>
        public IReadOnlyCollection<ConstructorItem> ConstructorStores
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
                                .Select(info => ConstructorItem.Get(info))
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

/* 项目“CodeArts (net45)”的未合并的更改
在此之前:
        public static TypeStoreItem Get<T>() => ItemCache.GetOrAdd(typeof(T), conversionType => new TypeStoreItem(conversionType));
在此之后:
        public static TypeStoreItem Get<T>() => ItemCache.GetOrAdd(typeof(T), conversionType => new Runtime.TypeStoreItem(conversionType));
*/

/* 项目“CodeArts (net40)”的未合并的更改
在此之前:
        public static TypeStoreItem Get<T>() => ItemCache.GetOrAdd(typeof(T), conversionType => new TypeStoreItem(conversionType));
在此之后:
        public static TypeStoreItem Get<T>() => ItemCache.GetOrAdd(typeof(T), conversionType => new Runtime.TypeStoreItem(conversionType));
*/

/* 项目“CodeArts (netstandard2.0)”的未合并的更改
在此之前:
        public static TypeStoreItem Get<T>() => ItemCache.GetOrAdd(typeof(T), conversionType => new TypeStoreItem(conversionType));
在此之后:
        public static TypeStoreItem Get<T>() => ItemCache.GetOrAdd(typeof(T), conversionType => new Runtime.TypeStoreItem(conversionType));
*/

/* 项目“CodeArts (netstandard2.1)”的未合并的更改
在此之前:
        public static TypeStoreItem Get<T>() => ItemCache.GetOrAdd(typeof(T), conversionType => new TypeStoreItem(conversionType));
在此之后:
        public static TypeStoreItem Get<T>() => ItemCache.GetOrAdd(typeof(T), conversionType => new Runtime.TypeStoreItem(conversionType));
*/
        public static TypeItem Get<T>() => ItemCache.GetOrAdd(typeof(T), (Func<Type, TypeItem>)(conversionType => (TypeItem)new TypeItem(conversionType)));

        /// <summary>
        /// 获取仓储项目。
        /// </summary>
        /// <param name="type">类型。</param>
        /// <returns></returns>

/* 项目“CodeArts (net45)”的未合并的更改
在此之前:
        public static TypeStoreItem Get(Type type) => ItemCache.GetOrAdd(type ?? throw new ArgumentNullException(nameof(type)), conversionType => new TypeStoreItem(conversionType));
在此之后:
        public static TypeStoreItem Get(Type type) => ItemCache.GetOrAdd(type ?? throw new ArgumentNullException(nameof(type)), conversionType => new Runtime.TypeStoreItem(conversionType));
*/

/* 项目“CodeArts (net40)”的未合并的更改
在此之前:
        public static TypeStoreItem Get(Type type) => ItemCache.GetOrAdd(type ?? throw new ArgumentNullException(nameof(type)), conversionType => new TypeStoreItem(conversionType));
在此之后:
        public static TypeStoreItem Get(Type type) => ItemCache.GetOrAdd(type ?? throw new ArgumentNullException(nameof(type)), conversionType => new Runtime.TypeStoreItem(conversionType));
*/

/* 项目“CodeArts (netstandard2.0)”的未合并的更改
在此之前:
        public static TypeStoreItem Get(Type type) => ItemCache.GetOrAdd(type ?? throw new ArgumentNullException(nameof(type)), conversionType => new TypeStoreItem(conversionType));
在此之后:
        public static TypeStoreItem Get(Type type) => ItemCache.GetOrAdd(type ?? throw new ArgumentNullException(nameof(type)), conversionType => new Runtime.TypeStoreItem(conversionType));
*/

/* 项目“CodeArts (netstandard2.1)”的未合并的更改
在此之前:
        public static TypeStoreItem Get(Type type) => ItemCache.GetOrAdd(type ?? throw new ArgumentNullException(nameof(type)), conversionType => new TypeStoreItem(conversionType));
在此之后:
        public static TypeStoreItem Get(Type type) => ItemCache.GetOrAdd(type ?? throw new ArgumentNullException(nameof(type)), conversionType => new Runtime.TypeStoreItem(conversionType));
*/
        public static TypeItem Get(Type type) => ItemCache.GetOrAdd(type ?? throw new ArgumentNullException(nameof(type)), (Func<Type, TypeItem>)(conversionType => (TypeItem)new TypeItem(conversionType)));
    }
}
