using System.Collections.Generic;
#if NET40
using System.Linq;
using System.Security.Principal;
#else
using System;
using System.Security.Claims;
#endif

namespace CodeArts.Mvc
{
    /// <summary>
    /// 将字段内容转换为登录认证信息实体。
    /// </summary>
    internal static class DictionaryExtentions
    {
#if NET40
        /// <summary>
        /// 作为身份认证
        /// </summary>
        /// <param name="userData">用户数据</param>
        /// <returns></returns>
        public static IIdentity AsIdentity(this IDictionary<string, object> userData)
        {
            foreach (var kv in userData)
            {
                var value = kv.Value;

                if (value is null) continue;

                var key = kv.Key.ToLower();

                if (key == "id" || key == "name" || key == "account")
                {
                    return new GenericIdentity(value.ToString(), "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
                }
            }

            return new GenericIdentity("JwtBearer", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
        }

        /// <summary>
        /// 作为身份认证
        /// </summary>
        /// <param name="userData">用户数据</param>
        /// <returns></returns>
        public static GenericPrincipal AsPrincipal(this IDictionary<string, object> userData)
        {
            return new GenericPrincipal(userData.AsIdentity(), userData.Where(x => x.Key.ToLower() == "role").Select(x => x.Value.ToString()).ToArray());
        }
#else
        private readonly static Dictionary<string, string> ClaimTypes = new Dictionary<string, string>
        {
            ["id"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
            ["name"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
            ["account"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
            ["role"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
            ["tel"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone",
            ["phone"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone",
            ["homephone"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/homephone",
            ["otherphone"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/otherphone",
            ["mobile"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone",
            ["mobilephone"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone",
            ["email"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress",
            ["emailaddress"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress",
            ["expired"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/expired",
            ["expiration"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/expiration",
            ["actor"] = "http://schemas.xmlsoap.org/ws/2009/09/identity/claims/actor",
            ["postalCode"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/postalcode",
            ["primaryGroupSid"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/primarygroupsid",
            ["primarySid"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/primarysid",
            ["rsa"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/rsa",
            ["serialnumber"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/serialnumber",
            ["sid"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid",
            ["spn"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/spn",
            ["stateorprovince"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/stateorprovince",
            ["streetaddress"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/streetaddress",
            ["surname"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname",
            ["system"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/system",
            ["thumbprint"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/thumbprint",
            ["upn"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn",
            ["uri"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/uri",
            ["userdata"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/userdata",
            ["version"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/version",
            ["webpage"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/webpage",
            ["windowsaccountname"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsaccountname",
            ["windowsdeviceclaim"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsdeviceclaim",
            ["windowsdevicegroup"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsdevicegroup",
            ["windowsfqbnversion"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsfqbnversion",
            ["windowssubauthority"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority",
            ["anonymous"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/anonymous",
            ["authentication"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication",
            ["authenticationinstant"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/authenticationinstant",
            ["authenticationmethod"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/authenticationmethod",
            ["authorizationdecision"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authorizationdecision",
            ["cookiepath"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/cookiepath",
            ["country"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/country",
            ["dateofbirth"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/dateofbirth",
            ["denyonlyprimarygroupsid"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlyprimarygroupsid",
            ["denyonlyprimarysid"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlyprimarysid",
            ["denyonlysid"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/denyonlysid",
            ["windowsuserclaim"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsuserclaim",
            ["denyonlywindowsdevicegroup"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlywindowsdevicegroup",
            ["dsa"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/dsa",
            ["gender"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/gender",
            ["givenname"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname",
            ["groupsid"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid",
            ["hash"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/hash",
            ["ispersistent"] = "http://schemas.microsoft.com/ws/2008/06/identity/claims/ispersistent",
            ["locality"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/locality",
            ["dns"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/dns",
            ["x500distinguishedname"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/x500distinguishedname"
        };

        private readonly static Dictionary<Type, string> ClaimValueTypes = new Dictionary<Type, string>
        {
            [typeof(bool)] = "http://www.w3.org/2001/XMLSchema#boolean",
            [typeof(int)] = "http://www.w3.org/2001/XMLSchema#integer32",
            [typeof(long)] = "http://www.w3.org/2001/XMLSchema#integer64",
            [typeof(uint)] = "http://www.w3.org/2001/XMLSchema#uinteger32",
            [typeof(ulong)] = "http://www.w3.org/2001/XMLSchema#uinteger64",
            [typeof(byte)] = "http://www.w3.org/2001/XMLSchema#integer",
            [typeof(short)] = "http://www.w3.org/2001/XMLSchema#integer",
            [typeof(ushort)] = "http://www.w3.org/2001/XMLSchema#integer",
            [typeof(DateTime)] = "http://www.w3.org/2001/XMLSchema#dateTime",
            [typeof(TimeSpan)] = "http://www.w3.org/2001/XMLSchema#time",
            [typeof(float)] = "http://www.w3.org/2001/XMLSchema#double",
            [typeof(double)] = "http://www.w3.org/2001/XMLSchema#double",
            [typeof(decimal)] = "http://www.w3.org/2001/XMLSchema#double"
        };

        /// <summary>
        /// 作为身份认证
        /// </summary>
        /// <param name="userData">用户数据</param>
        /// <returns></returns>
        public static ClaimsIdentity AsIdentity(this IDictionary<string, object> userData)
        {
            var identity = new ClaimsIdentity("JwtBearer", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");

#if NETSTANDARD2_0 || NETCOREAPP3_1

            identity.AddClaim(new Claim("aud", "jwt:audience".Config(Consts.JwtAudience)));
            identity.AddClaim(new Claim("iss", "jwt:issuer".Config(Consts.JwtIssuer)));
#endif

            foreach (var kv in userData)
            {
                var value = kv.Value;

                if (value is null) continue;

                var key = kv.Key.ToLower();

                if (ClaimTypes.TryGetValue(key, out string type))
                {
                    identity.AddClaim(new Claim(type, value.ToString(), "http://www.w3.org/2001/XMLSchema#string"));
                }

                var dataType = value.GetType();

                if (dataType.IsValueType || dataType == typeof(string))
                {
                    if (value is DateTime date)
                    {
                        identity.AddClaim(new Claim(key, date.ToString("yyyy-MM-dd HH:mm:ss"), "http://www.w3.org/2001/XMLSchema#dateTime"));
                    }
                    else if (ClaimValueTypes.TryGetValue(dataType, out string valueType))
                    {
                        identity.AddClaim(new Claim(key, value.ToString(), valueType));
                    }
                    else
                    {
                        identity.AddClaim(new Claim(key, value.ToString(), "http://www.w3.org/2001/XMLSchema#string"));
                    }
                }
            }

            return identity;
        }
#endif
    }
}
