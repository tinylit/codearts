using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyBuilding.Tests.Serialize.Json
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
        /// <param name="camelCase"></param>
        public JsonContractResolver(NamingType camelCase)
        {
            _camelCase = camelCase;
        }

        /// <summary>
        /// 属性名解析
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        protected override string ResolvePropertyName(string propertyName)
        {
            switch (_camelCase)
            {
                case NamingType.CamelCase:
                    return propertyName.ToCamelCase();
                case NamingType.UrlCase:
                    return propertyName.ToUrlCase();
                case NamingType.PascalCase:
                    return propertyName.ToPascalCase();
                default:
                    return base.ResolvePropertyName(propertyName);
            }
        }
    }
}
