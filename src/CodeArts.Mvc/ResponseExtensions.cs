#if NETCOREAPP2_0_OR_GREATER
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
#else
using System.Web;
#endif
using CodeArts.Serialize.Json;
using System.Text;

namespace CodeArts.Mvc
{
    /// <summary>
    /// 响应拓展。
    /// </summary>
    public static class ResponseExtensions
    {
#if NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// 返回json。
        /// </summary>
        /// <param name="httpResponse">响应。</param>
        /// <param name="bytes">媒体内容。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public static Task WriteImageAsync(this HttpResponse httpResponse, byte[] bytes, CancellationToken cancellationToken = default)
        {
            var stream = new MemoryStream(bytes);
            httpResponse.ContentType = "image/png";

            return StreamCopyOperation.CopyToAsync(stream, httpResponse.Body, null, bytes.Length, cancellationToken);
        }

#else
        /// <summary>
        /// 返回json。
        /// </summary>
        /// <param name="httpResponse">响应。</param>
        /// <param name="bytes">媒体内容。</param>
        /// <returns></returns>
        public static void WriteImage(this HttpResponse httpResponse, byte[] bytes)
        {
            httpResponse.ContentType = "image/png";
            httpResponse.BinaryWrite(bytes);
        }
#endif

#if NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// 返回json。
        /// </summary>
        /// <param name="httpResponse">响应。</param>
        /// <param name="text">返回内容。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public static Task WriteJsonAsync(this HttpResponse httpResponse, string text, CancellationToken cancellationToken = default)
        {
            httpResponse.ContentType = "application/json;charset=utf-8";
            return httpResponse.WriteAsync(text, Encoding.UTF8, cancellationToken);
        }
#else
        /// <summary>
        /// 返回json。
        /// </summary>
        /// <param name="httpResponse">响应。</param>
        /// <param name="text">返回内容。</param>
        /// <returns></returns>
        public static void WriteJson(this HttpResponse httpResponse, string text)
        {
            httpResponse.ContentType = "application/json;charset=utf-8";
            httpResponse.ContentEncoding = Encoding.UTF8;
            httpResponse.Write(text);
        }
#endif

#if NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// 返回json。
        /// </summary>
        /// <param name="httpResponse">响应。</param>
        /// <param name="value">返回内容。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public static Task WriteJsonAsync<T>(this HttpResponse httpResponse, T value, CancellationToken cancellationToken = default) => httpResponse.WriteJsonAsync(JsonHelper.ToJson(value), cancellationToken);
#else
        /// <summary>
        /// 返回json。
        /// </summary>
        /// <param name="httpResponse">响应。</param>
        /// <param name="value">返回内容。</param>
        /// <returns></returns>
        public static void WriteJson<T>(this HttpResponse httpResponse, T value) => httpResponse.WriteJson(JsonHelper.ToJson(value));
#endif
    }
}