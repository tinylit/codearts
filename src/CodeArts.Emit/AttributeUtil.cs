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
    /// 属性工具包。
    /// </summary>
    internal static class AttributeUtil
    {
        /// <summary>
        /// 创建自定义属性构造器。
        /// </summary>
        /// <param name="attribute">属性。</param>
        /// <returns></returns>
        internal static CustomAttributeBuilder Create(CustomAttributeData attribute)
        {
            if (attribute is null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            var constructorArguments = GetArguments(attribute.ConstructorArguments);

#if NET40            
            var namedArguments = GetSettersAndFields(attribute.Constructor.DeclaringType, attribute.NamedArguments);

            return new CustomAttributeBuilder(attribute.Constructor.DeclaringType.GetConstructor(constructorArguments.Item1), constructorArguments.Item2, namedArguments.Item1, namedArguments.Item2, namedArguments.Item3, namedArguments.Item4);
#else
            var namedArguments = GetSettersAndFields(attribute.AttributeType, attribute.NamedArguments);

            return new CustomAttributeBuilder(attribute.AttributeType.GetConstructor(constructorArguments.Item1), constructorArguments.Item2, namedArguments.Item1, namedArguments.Item2, namedArguments.Item3, namedArguments.Item4);
#endif
        }

        private static Tuple<Type[], object[]> GetArguments(IList<CustomAttributeTypedArgument> constructorArguments)
        {
            var constructorArgTypes = new Type[constructorArguments.Count];
            var constructorArgs = new object[constructorArguments.Count];
            for (var i = 0; i < constructorArguments.Count; i++)
            {
                constructorArgTypes[i] = constructorArguments[i].ArgumentType;
                constructorArgs[i] = ReadAttributeValue(constructorArguments[i]);
            }

            return Tuple.Create(constructorArgTypes, constructorArgs);
        }

        private static Tuple<PropertyInfo[], object[], FieldInfo[], object[]> GetSettersAndFields(Type attributeType, IEnumerable<CustomAttributeNamedArgument> namedArguments)
        {
            var propertyList = new List<PropertyInfo>();
            var propertyValuesList = new List<object>();
            var fieldList = new List<FieldInfo>();
            var fieldValuesList = new List<object>();
            foreach (var argument in namedArguments)
            {
#if NET40
                if (argument.MemberInfo is FieldInfo)
                {
                    fieldList.Add(attributeType.GetField(argument.MemberInfo.Name));
                    fieldValuesList.Add(ReadAttributeValue(argument.TypedValue));
                }
                else
                {
                    propertyList.Add(attributeType.GetProperty(argument.MemberInfo.Name));
                    propertyValuesList.Add(ReadAttributeValue(argument.TypedValue));
                }
#else
                if (argument.IsField)
                {
                    fieldList.Add(attributeType.GetField(argument.MemberName));
                    fieldValuesList.Add(ReadAttributeValue(argument.TypedValue));
                }
                else
                {
                    propertyList.Add(attributeType.GetProperty(argument.MemberName));
                    propertyValuesList.Add(ReadAttributeValue(argument.TypedValue));
                }
#endif
            }

            return Tuple.Create(propertyList.ToArray(), propertyValuesList.ToArray(), fieldList.ToArray(), fieldValuesList.ToArray());
        }

        private static object ReadAttributeValue(CustomAttributeTypedArgument argument)
        {
            var value = argument.Value;
            if (argument.ArgumentType.IsArray && value is IList<CustomAttributeTypedArgument> arrays)
            {
                //special case for handling arrays in attributes
                return GetNestedArguments(arrays);
            }

            return value;
        }

        private static object[] GetNestedArguments(IList<CustomAttributeTypedArgument> constructorArguments)
        {
            var arguments = new object[constructorArguments.Count];

            for (var i = 0; i < constructorArguments.Count; i++)
            {
                arguments[i] = ReadAttributeValue(constructorArguments[i]);
            }

            return arguments;
        }
    }
}
