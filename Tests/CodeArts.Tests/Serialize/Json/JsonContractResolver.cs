using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeArts.Tests.Serialize.Json
{
    /// <summary>
    ///JSON序列化解析协议
    /// </summary>
    public class JsonContractResolver : DefaultContractResolver
    {
        private readonly NamingType _camelCase;

        /// <summary>
        /// 构造定义命名解析风格
        /// </summary>
        /// <param name="camelCase">命名规则</param>
        public JsonContractResolver(NamingType camelCase)
        {
            _camelCase = camelCase;
        }

        /// <summary>
        /// 属性名解析
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        /// <returns></returns>
        protected override string ResolvePropertyName(string propertyName)
        {
            switch (_camelCase)
            {
                case NamingType.CamelCase:
                case NamingType.UrlCase:
                case NamingType.PascalCase:
                    return propertyName.ToNamingCase(_camelCase);
                default:
                    return base.ResolvePropertyName(propertyName);
            }
        }
    }
}
