using CodeArts.Emit.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.Emit
{
    /// <summary>
    /// 抽象类。
    /// </summary>
    public class AbstractTypeEmitter
    {
        private readonly TypeBuilder builder;
        private readonly List<MethodEmitter> methods = new List<MethodEmitter>();
        private readonly Dictionary<string, FieldExpression> fields = new Dictionary<string, FieldExpression>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="builder">类型构造器</param>
        protected AbstractTypeEmitter(TypeBuilder builder)
        {
            this.builder = builder;
        }

        /// <summary>
        /// 父类型。
        /// </summary>
        public Type BaseType
        {
            get
            {
                if (builder.IsInterface)
                {
                    throw new InvalidOperationException("This emitter represents an interface; interfaces have no base types.");
                }
                return builder.BaseType;
            }
        }


        public FieldExpression CreateField(string name, Type fieldType)
        {
            return CreateField(name, fieldType, true);
        }

        public FieldExpression CreateField(string name, Type fieldType, bool serializable)
        {
            var atts = FieldAttributes.Private;

            if (!serializable)
            {
                atts |= FieldAttributes.NotSerialized;
            }

            return CreateField(name, fieldType, atts);
        }

        public FieldExpression CreateField(string name, Type fieldType, FieldAttributes atts)
        {
            var reference = new FieldExpression(builder.DefineField(name, fieldType, atts));

            fields.Add(name, reference);

            return reference;
        }

        public MethodEmitter CreateMethod(string name, MethodAttributes attrs, Type returnType)
        {
            var member = new MethodEmitter(name, attrs, returnType);
            methods.Add(member);
            return member;
        }

        public PropertyEmitter CreateProperty(string name, PropertyAttributes attributes, Type propertyType, Type[] arguments)
        {
            var propEmitter = new PropertyEmitter(this, name, attributes, propertyType, arguments);
            properties.Add(propEmitter);
            return propEmitter;
        }


        /// <summary>
        /// 创建类型。
        /// </summary>
        /// <returns></returns>
        public Type CreateType()
        {
#if NETSTANDARD2_0
            return builder.CreateTypeInfo().AsType();
#else
            return builder.CreateType();
#endif
        }
    }
}
