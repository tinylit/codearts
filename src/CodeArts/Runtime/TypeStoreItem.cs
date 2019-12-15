using System;
using System.Collections.Generic;
#if NET40
using System.Collections.ObjectModel;
#endif
using System.Linq;

namespace CodeArts.Runtime
{
    /// <summary>
    /// 类型地图
    /// </summary>
    public class TypeStoreItem : StoreItem
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="type">类型</param>
        public TypeStoreItem(Type type) : base(Attribute.GetCustomAttributes(type))
        {
            Type = type;
        }

        /// <summary>
        /// 类型名称
        /// </summary>
        public override string Name => Type.Name;

        /// <summary>
        /// 类型全名
        /// </summary>
        public string FullName => Type.FullName;

        /// <summary>
        /// 静态类
        /// </summary>
        public bool IsStatic => Type.IsAbstract && Type.IsSealed;

        /// <summary>
        /// 公共类
        /// </summary>
        public bool IsPublic => Type.IsPublic;

        /// <summary>
        /// 类型
        /// </summary>
        public Type Type { get; }

        private readonly static object Lock_FieldObj = new object();
        private readonly static object Lock_PropertyObj = new object();
        private readonly static object Lock_MethodObj = new object();
        private readonly static object Lock_ConstructorObj = new object();

#if NET40


        private ReadOnlyCollection<PropertyStoreItem> propertyStores;

        /// <summary>
        /// 属性
        /// </summary>
        public ReadOnlyCollection<PropertyStoreItem> PropertyStores
        {
            get
            {
                if (propertyStores == null)
                {
                    lock (Lock_PropertyObj)
                    {
                        if (propertyStores == null)
                        {
                            propertyStores = Type.GetProperties()
                                .Select(info => new PropertyStoreItem(info))
                                .ToList().AsReadOnly();
                        }
                    }
                }

                return propertyStores;
            }
        }

        private ReadOnlyCollection<FieldStoreItem> fieldStores;
        /// <summary>
        /// 字段
        /// </summary>
        public ReadOnlyCollection<FieldStoreItem> FieldStores
        {
            get
            {
                if (fieldStores == null)
                {
                    lock (Lock_FieldObj)
                    {
                        if (fieldStores == null)
                        {
                            fieldStores = Type.GetFields()
                                .Select(info => new FieldStoreItem(info))
                                .ToList().AsReadOnly();
                        }
                    }
                }
                return fieldStores;
            }
        }

        private ReadOnlyCollection<MethodStoreItem> methodStores;
        /// <summary>
        /// 方法
        /// </summary>
        public ReadOnlyCollection<MethodStoreItem> MethodStores
        {
            get
            {
                if (methodStores == null)
                {
                    lock (Lock_MethodObj)
                    {
                        if (methodStores == null)
                        {
                            methodStores = Type.GetMethods()
                                .Select(info => new MethodStoreItem(info))
                                .ToList().AsReadOnly();
                        }
                    }
                }
                return methodStores;
            }
        }

        private ReadOnlyCollection<ConstructorStoreItem> constructorStores;
        /// <summary>
        /// 构造函数
        /// </summary>
        public ReadOnlyCollection<ConstructorStoreItem> ConstructorStores
        {
            get
            {
                if (constructorStores == null)
                {
                    lock (Lock_ConstructorObj)
                    {
                        if (constructorStores == null)
                        {
                            constructorStores = Type.GetConstructors()
                                .Select(info => new ConstructorStoreItem(info))
                                .ToList().AsReadOnly();
                        }
                    }
                }
                return constructorStores;
            }
        }
#else

        private IReadOnlyCollection<PropertyStoreItem> propertyStores;

        /// <summary>
        /// 属性
        /// </summary>
        public IReadOnlyCollection<PropertyStoreItem> PropertyStores
        {
            get
            {
                if (propertyStores == null)
                {
                    lock (Lock_PropertyObj)
                    {
                        if (propertyStores == null)
                        {
                            propertyStores = Type.GetProperties()
                                .Select(info => new PropertyStoreItem(info))
                                .ToList();
                        }
                    }
                }

                return propertyStores;
            }
        }

        private IReadOnlyCollection<FieldStoreItem> fieldStores;
        /// <summary>
        /// 字段
        /// </summary>
        public IReadOnlyCollection<FieldStoreItem> FieldStores
        {
            get
            {
                if (fieldStores == null)
                {
                    lock (Lock_FieldObj)
                    {
                        if (fieldStores == null)
                        {
                            fieldStores = Type.GetFields()
                                .Select(info => new FieldStoreItem(info))
                                .ToList();
                        }
                    }
                }
                return fieldStores;
            }
        }

        private IReadOnlyCollection<MethodStoreItem> methodStores;
        /// <summary>
        /// 方法
        /// </summary>
        public IReadOnlyCollection<MethodStoreItem> MethodStores
        {
            get
            {
                if (methodStores == null)
                {
                    lock (Lock_MethodObj)
                    {
                        if (methodStores == null)
                        {
                            methodStores = Type.GetMethods()
                                .Select(info => new MethodStoreItem(info))
                                .ToList();
                        }
                    }
                }
                return methodStores;
            }
        }

        private IReadOnlyCollection<ConstructorStoreItem> constructorStores;
        /// <summary>
        /// 构造函数
        /// </summary>
        public IReadOnlyCollection<ConstructorStoreItem> ConstructorStores
        {
            get
            {
                if (constructorStores == null)
                {
                    lock (Lock_ConstructorObj)
                    {
                        if (constructorStores == null)
                        {
                            constructorStores = Type.GetConstructors()
                                .Select(info => new ConstructorStoreItem(info))
                                .ToList();
                        }
                    }
                }
                return constructorStores;
            }
        }
#endif
    }
}
