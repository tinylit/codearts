using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        }

        /// <summary>
        /// 类型名称。
        /// </summary>
        public override string Name => MemberType.Name;

        /// <summary>
        /// 类型全名。
        /// </summary>
        public string FullName => MemberType.FullName;

        /// <summary>
        /// 静态类。
        /// </summary>
        public bool IsStatic => MemberType.IsAbstract && MemberType.IsSealed;

        /// <summary>
        /// 公共类。
        /// </summary>
        public bool IsPublic => MemberType.IsPublic;

        private static readonly object Lock_FieldObj = new object();
        private static readonly object Lock_PropertyObj = new object();
        private static readonly object Lock_MethodObj = new object();
        private static readonly object Lock_ConstructorObj = new object();


        private IReadOnlyList<PropertyItem> propertyStores;

        /// <summary>
        /// 属性。
        /// </summary>
        public IReadOnlyList<PropertyItem> PropertyStores
        {
            get
            {
                if (propertyStores is null)
                {
                    lock (Lock_PropertyObj)
                    {
                        if (propertyStores is null)
                        {
                            propertyStores = MemberType.GetProperties()
                                .Select(info => PropertyItem.Get(info))
#if NET40
                                .ToReadOnlyList();
#else
                                .ToList();
#endif
                        }
                    }
                }

                return propertyStores;
            }
        }

        private IReadOnlyList<FieldItem> fieldStores;
        /// <summary>
        /// 字段。
        /// </summary>
        public IReadOnlyList<FieldItem> FieldStores
        {
            get
            {
                if (fieldStores is null)
                {
                    lock (Lock_FieldObj)
                    {
                        if (fieldStores is null)
                        {
                            fieldStores = MemberType.GetFields()
                                .Select(info => FieldItem.Get(info))
#if NET40
                                .ToReadOnlyList();
#else
                                .ToList();
#endif
                        }
                    }
                }
                return fieldStores;
            }
        }

        private IReadOnlyList<MethodItem> methodStores;
        /// <summary>
        /// 方法。
        /// </summary>
        public IReadOnlyList<MethodItem> MethodStores
        {
            get
            {
                if (methodStores is null)
                {
                    lock (Lock_MethodObj)
                    {
                        if (methodStores is null)
                        {
                            methodStores = MemberType.GetMethods()
                                .Select(info => MethodItem.Get(info))
#if NET40
                                .ToReadOnlyList();
#else
                                .ToList();
#endif
                        }
                    }
                }
                return methodStores;
            }
        }

        private IReadOnlyList<ConstructorItem> constructorStores;
        /// <summary>
        /// 构造函数。
        /// </summary>
        public IReadOnlyList<ConstructorItem> ConstructorStores
        {
            get
            {
                if (constructorStores is null)
                {
                    lock (Lock_ConstructorObj)
                    {
                        if (constructorStores is null)
                        {
                            constructorStores = MemberType.GetConstructors()
                                .Select(info => ConstructorItem.Get(info))
#if NET40
                                .ToReadOnlyList();
#else
                                .ToList();
#endif
                        }
                    }
                }
                return constructorStores;
            }
        }

        /// <summary>
        /// 获取仓储项目。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <returns></returns>
        public static TypeItem Get<T>() => ItemCache.GetOrAdd(typeof(T), conversionType => new TypeItem(conversionType));

        /// <summary>
        /// 获取仓储项目。
        /// </summary>
        /// <param name="type">类型。</param>
        /// <returns></returns>
        public static TypeItem Get(Type type) => ItemCache.GetOrAdd(type ?? throw new ArgumentNullException(nameof(type)), conversionType => new TypeItem(conversionType));
    }
}
