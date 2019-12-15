using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace System
{
    /// <summary>
    /// 编码扩展
    /// </summary>
    public static class EncodingExtentions
    {
        /// <summary>
        /// Url编码
        /// </summary>
        /// <param name="str">编码字符串</param>
        /// <param name="encoding">编码格式，默认：UTF8</param>
        /// <returns></returns>
        public static string UrlEncode(this string str, Encoding encoding = null) => HttpUtility.UrlEncode(str, encoding ?? Encoding.UTF8);

        /// <summary>
        /// URL编码大写
        /// </summary>
        /// <param name="str">编码字符串</param>
        /// <param name="encoding">编码格式，默认：UTF8</param>
        /// <returns></returns>
        public static string UrlEncodeUpper(this string str, Encoding encoding = null)
        => Regex.Replace(str.UrlEncode(encoding), "(?<letter>(%[0-9a-z]{2}))", match =>
        {
            return match.Groups["letter"].Value.ToUpper();
        });


        /// <summary>
        /// BASE64转字符串
        /// </summary>
        /// <param name="str">BASE64文本</param>
        /// <param name="encoding">编码格式，默认：UTF8</param>
        /// <returns></returns>
        public static string FromBase64String(this string str, Encoding encoding = null) => (encoding ?? Encoding.UTF8).GetString(Convert.FromBase64String(str));

        /// <summary>
        /// 编码
        /// </summary>
        /// <param name="str">编码字符串</param>
        /// <param name="encoding">编码格式，默认：UTF8</param>
        /// <returns></returns>
        public static byte[] ToBytes(this string str, Encoding encoding = null) => (encoding ?? Encoding.UTF8).GetBytes(str);

        /// <summary>
        /// 字符串BASE64编码
        /// </summary>
        /// <param name="str">文本</param>
        /// <param name="encoding">编码格式，默认：UTF8</param>
        /// <returns></returns>
        public static string ToBase64String(this string str, Encoding encoding = null) => Convert.ToBase64String(str.ToBytes(encoding ?? Encoding.UTF8));
    }
}