using CodeArts.Emit.Expressions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit
{
    /// <summary>
    /// 构造函数。
    /// </summary>
    public class ConstructorEmitter : BlockAst
    {
        private readonly List<ParamterEmitter> parameters = new List<ParamterEmitter>();
        private ConstructorBuilder builder;
        private int parameterIndex = 0;

        private class ConstructorExpression : AstExpression
        {
            private readonly ConstructorInfo constructor;
            private readonly ParamterEmitter[] paramters;

            public ConstructorExpression(ConstructorInfo constructor) : base(constructor.DeclaringType)
            {
                this.constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
            }
            public ConstructorExpression(ConstructorInfo constructor, ParamterEmitter[] paramters) : this(constructor)
            {
                this.paramters = paramters;
            }

            public override void Load(ILGenerator ilg)
            {
                ilg.Emit(OpCodes.Ldarg_0);

                if (paramters != null)
                {
                    foreach (var exp in paramters)
                    {
                        exp.Load(ilg);
                    }
                }

                ilg.Emit(OpCodes.Call, constructor);
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="baseType">父类型。</param>
        /// <param name="attributes">属性。</param>
        public ConstructorEmitter(Type baseType, MethodAttributes attributes) : this(baseType, attributes, CallingConventions.Standard)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="baseType">父类型。</param>
        /// <param name="attributes">属性。</param>
        /// <param name="conventions">调用约定。</param>
        public ConstructorEmitter(Type baseType, MethodAttributes attributes, CallingConventions conventions) : base(baseType)
        {
            Attributes = attributes;
            Conventions = conventions;
        }

        /// <summary>
        /// 成员。
        /// </summary>
        public ConstructorBuilder Value => builder ?? throw new NotImplementedException();

        /// <summary>
        /// 方法的名称。
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 方法的属性。
        /// </summary>
        public MethodAttributes Attributes { get; }

        /// <summary>
        /// 调用约定。
        /// </summary>
        public CallingConventions Conventions { get; }

        /// <summary>
        /// 参数。
        /// </summary>
        public ParamterEmitter[] Parameters => parameters.ToArray();

        /// <summary>
        /// 声明参数。
        /// </summary>
        /// <param name="parameterType">参数类型</param>
        /// <param name="attributes">属性</param>
        /// <param name="strParamName">名称</param>
        /// <returns></returns>
        public ParamterEmitter DefineParameter(Type parameterType, ParameterAttributes attributes, string strParamName)
        {
            var parameter = new ParamterEmitter(parameterType, ++parameterIndex, attributes, strParamName);
            parameters.Add(parameter);
            return parameter;
        }

        private bool ImplementedByRuntime
        {
            get
            {
#if NET40
                var attributes = builder.GetMethodImplementationFlags();
#else
                var attributes = builder.MethodImplementationFlags;
#endif
                return (attributes & MethodImplAttributes.Runtime) != MethodImplAttributes.IL;
            }
        }
        /// <summary>
        /// 调用父类构造函数。
        /// </summary>
        public void InvokeBaseConstructor()
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var type = ReturnType;

            if (type.IsGenericParameter)
            {
                type = type.GetGenericTypeDefinition();
            }

            InvokeBaseConstructor(type.GetConstructor(flags, null, Type.EmptyTypes, null));
        }

        /// <summary>
        /// 调用父类构造函数。
        /// </summary>
        /// <param name="constructor">构造函数。</param>
        public void InvokeBaseConstructor(ConstructorInfo constructor)
        {
            Append(new ConstructorExpression(constructor));
        }

        /// <summary>
        /// 调用父类构造函数。
        /// </summary>
        /// <param name="constructor">构造函数。</param>
        /// <param name="paramters">参数。</param>
        public void InvokeBaseConstructor(ConstructorInfo constructor, params ParamterEmitter[] paramters)
        {
            Append(new ConstructorExpression(constructor, paramters));
        }

        /// <summary>
        /// 发行。
        /// </summary>
        public void Emit(ConstructorBuilder builder)
        {
            this.builder = builder;

            if (ImplementedByRuntime)
            {
                return;
            }

            if (IsEmpty)
            {
                InvokeBaseConstructor();

                Append(new ReturnAst());
            }

            foreach (var item in parameters)
            {
                item.Emit(builder.DefineParameter(item.Position, item.Attributes, item.ParameterName));
            }

            base.Load(builder.GetILGenerator());
        }
    }
}
