#if NETSTANDARD2_0 || NETCOREAPP3_0
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using SkyBuilding.Serialize.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SkyBuilding.Mvc
{
    /// <summary>
    /// 响应拓展
    /// </summary>
    public static class ResponseExtensions
    {
        /// <summary>
        /// 返回json
        /// </summary>
        /// <param name="httpResponse">响应</param>
        /// <param name="text">返回内容</param>
        /// <returns></returns>
        public static Task WriteImageAsync(this HttpResponse httpResponse, byte[] bytes)
        {
            var sream = new MemoryStream(bytes);
            httpResponse.ContentType = "image/png";
            return StreamCopyOperation.CopyToAsync(sream, httpResponse.Body, null, bytes.Length, default);
        }
        /// <summary>
        /// 返回json
        /// </summary>
        /// <param name="httpResponse">响应</param>
        /// <param name="text">返回内容</param>
        /// <returns></returns>
        public static Task WriteJsonAsync(this HttpResponse httpResponse, string text)
        {
            httpResponse.ContentType = "application/json;charset=utf-8";
            return httpResponse.WriteAsync(text, Encoding.UTF8);
        }
        /// <summary>
        /// 返回json
        /// </summary>
        /// <param name="httpResponse">响应</param>
        /// <param name="text">返回内容</param>
        /// <returns></returns>
        public static Task WriteJsonAsync<T>(this HttpResponse httpResponse, T value)
        {
            httpResponse.ContentType = "application/json;charset=utf-8";
            return httpResponse.WriteAsync(JsonHelper.ToJson(value), Encoding.UTF8);
        }
    }
}
#endif