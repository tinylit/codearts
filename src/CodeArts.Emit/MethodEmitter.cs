using CodeArts.Emit.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit
{
    /// <summary>
    /// 方法。
    /// </summary>
    public class MethodEmitter : BlockExpression
    {
        private readonly List<ParameterExpression> parameters = new List<ParameterExpression>();
        private MethodBuilder builder;
        private int parameterIndex = 0;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="name">方法的名称。</param>
        /// <param name="attributes">方法的属性。</param>
        /// <param name="returnType">方法的返回类型。</param>
        public MethodEmitter(string name, MethodAttributes attributes, Type returnType) : base(returnType)
        {
            Name = name;
            Attributes = attributes;
        }

        /// <summary>
        /// 成员。
        /// </summary>
        public MethodInfo Value => builder ?? throw new NotImplementedException();
        /// <summary>
        /// 方法的名称。
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 方法的属性。
        /// </summary>
        public MethodAttributes Attributes { get; }

        /// <summary>
        /// 参数。
        /// </summary>
        public ParameterExpression[] Parameters => parameters.ToArray();

        /// <summary>
        /// 声明参数。
        /// </summary>
        /// <param name="parameterType">参数类型</param>
        /// <param name="attributes">属性</param>
        /// <param name="strParamName">名称</param>
        /// <returns></returns>
        public ParameterExpression DefineParameter(Type parameterType, ParameterAttributes attributes, string strParamName)
        {
            var parameter = new ParameterExpression(parameterType, ++parameterIndex, attributes, strParamName);
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
        /// 发行。
        /// </summary>
        public void Emit(MethodBuilder builder)
        {
            this.builder = builder;

            foreach (var item in parameters)
            {
                builder.DefineParameter(item.Position, item.Attributes, item.ParameterName);
            }

            if (!ImplementedByRuntime && IsEmpty)
            {
                Append(new NopExpression());
                Append(new ReturnExpression());
            }

            base.Emit(builder.GetILGenerator());
        }
    }
}
