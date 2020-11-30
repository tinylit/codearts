using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CodeArts
{
    /// <summary>
    /// -1 基础异常。
    /// 0 执行成功。
    /// 1-5000 请求状态码。
    /// 5001-10000 系统错误码。
    /// 10001-40000 业务错误码。
    /// 40001-60000 服务异常。
    /// 60001-70000 数据库异常。
    /// --可扩展。
    /// </summary>
    public abstract class StatusCodes
    {
        /// <summary> 默认错误码。 </summary>
        [Description("基础异常")]
        public const int Error = -1;

        /// <summary> 执行成功。</summary>
        [Description("执行成功!")]
        public const int OK = 0;

        /// <summary>
        ///     等效于 HTTP 状态 100。 System.Net.HttpStatusCode.Continue 指示客户端可能继续其请求。
        /// </summary>
        [Description("指示客户端可能继续其请求.")]
        public const int RequestContinue = 100;
        /// <summary>
        ///     等效于 HTTP 状态 101。 System.Net.HttpStatusCode.SwitchingProtocols 指示正在更改协议版本或协议。
        /// </summary>
        [Description("指示正在更改协议版本或协议.")]
        public const int RequestSwitchingProtocols = 101;
        /// <summary>
        ///     等效于 HTTP 状态 200。 System.Net.HttpStatusCode.OK 指示请求成功，且请求的信息包含在响应中。 这是最常接收的状态代码。
        /// </summary>
        [Description("请求成功!")]
        public const int RequestOK = 200;
        /// <summary>
        ///     等效于 HTTP 状态 201。 System.Net.HttpStatusCode.Created 指示请求导致在响应被发送前创建新资源。
        /// </summary>
        [Description("指示请求导致在响应被发送前创建新资源.")]
        public const int RequestCreated = 201;
        /// <summary>
        ///     等效于 HTTP 状态 202。 System.Net.HttpStatusCode.Accepted 指示已接受请求做进一步处理。
        /// </summary>
        [Description("指示已接受请求做进一步处理.")]
        public const int RequestAccepted = 202;
        /// <summary>
        ///     等效于 HTTP 状态 203。 System.Net.HttpStatusCode.NonAuthoritativeInformation 指示返回的元信息来自缓存副本而不是原始服务器，因此可能不正确。
        /// </summary>
        [Description("指示返回的元信息来自缓存副本而不是原始服务器，因此可能不正确.")]
        public const int RequestNonAuthoritativeInformation = 203;
        /// <summary>
        ///     等效于 HTTP 状态 204。 System.Net.HttpStatusCode.NoContent 指示已成功处理请求并且响应已被设定为无内容。
        /// </summary>
        [Description("指示已成功处理请求并且响应已被设定为无内容.")]
        public const int RequestNoContent = 204;
        /// <summary>
        ///     等效于 HTTP 状态 205。 System.Net.HttpStatusCode.ResetContent 指示客户端应重置（而非重新加载）当前资源。
        /// </summary>
        [Description("指示客户端应重置（而非重新加载）当前资源.")]
        public const int RequestResetContent = 205;
        /// <summary>
        ///     等效于 HTTP 状态 206。 System.Net.HttpStatusCode.PartialContent 指示响应是包括字节范围的 GET 请求所请求的部分响应。
        /// </summary>
        [Description("指示客户端应重置（而非重新加载）当前资源.")]
        public const int RequestPartialContent = 206;
        /// <summary>
        ///     等效于 HTTP 状态 300。 System.Net.HttpStatusCode.Ambiguous 指示请求的信息有多种表示形式。 默认操作是将此状态视为重定向，并遵循与此响应关联的
        ///     Location 标头的内容。
        /// </summary>
        [Description("指示请求的信息有多种表示形式.")]
        public const int RequestAmbiguous = 300;
        /// <summary>
        ///     等效于 HTTP 状态 301。 System.Net.HttpStatusCode.Moved 指示请求的信息已移到 Location 头中指定的 URI
        ///     处。 接收到此状态时的默认操作为遵循与响应关联的 Location 标头。 原始请求方法为 POST 时，重定向的请求将使用 GET 方法。
        /// </summary>
        [Description("指示请求的信息已移到 Location 头中指定的 URI 处.")]
        public const int RequestMoved = 301;
        /// <summary>
        ///     等效于 HTTP 状态 302。 System.Net.HttpStatusCode.Redirect 指示请求的信息位于 Location 标头中指定的
        ///     URI 处。 接收到此状态时的默认操作为遵循与响应关联的 Location 标头。 原始请求方法为 POST 时，重定向的请求将使用 GET 方法。
        /// </summary>
        [Description("指示请求的信息位于 Location 标头中指定的 URI 处.")]
        public const int RequestRedirect = 302;
        /// <summary>
        ///     等效于 HTTP 状态 303。 作为 POST 的结果，System.Net.HttpStatusCode.RedirectMethod 将客户端自动重定向到
        ///     Location 标头中指定的 URI。 用 GET 生成对 Location 标头所指定的资源的请求。
        /// </summary>
        [Description("将客户端自动重定向到 Location 标头中指定的 URI.")]
        public const int RequestRedirectMethod = 303;
        /// <summary>
        ///     等效于 HTTP 状态 304。 System.Net.HttpStatusCode.NotModified 指示客户端的缓存副本是最新的。 未传输此资源的内容。
        /// </summary>
        [Description("指示客户端的缓存副本是最新的.")]
        public const int RequestNotModified = 304;
        /// <summary>
        ///     等效于 HTTP 状态 305。 System.Net.HttpStatusCode.UseProxy 指示请求应使用位于 Location 标头中指定的
        ///     URI 的代理服务器。
        /// </summary>
        [Description("指示请求应使用位于 Location 标头中指定的 URI 的代理服务器.")]
        public const int RequestUseProxy = 305;
        /// <summary>
        ///     等效于 HTTP 状态 306。 System.Net.HttpStatusCode.Unused 是未完全指定的 HTTP/1.1 规范的建议扩展。
        /// </summary>
        [Description("是未完全指定的 HTTP/1.1 规范的建议扩展.")]
        public const int RequestUnused = 306;
        /// <summary>
        ///     等效于 HTTP 状态 307。 System.Net.HttpStatusCode.RedirectKeepVerb 指示请求信息位于 Location
        ///     标头中指定的 URI 处。 接收到此状态时的默认操作为遵循与响应关联的 Location 标头。 原始请求方法为 POST 时，重定向的请求还将使用 POST
        ///     方法。
        /// </summary>
        [Description("指示请求信息位于 Location 标头中指定的 URI 处.")]
        public const int RequestRedirectKeepVerb = 307;
        /// <summary>
        ///     等效于 HTTP 状态 400。 System.Net.HttpStatusCode.BadRequest 指示服务器未能识别请求。 如果没有其他适用的错误，或者不知道准确的错误或错误没有自己的错误代码，则发送
        ///     System.Net.HttpStatusCode.BadRequest。
        /// </summary>
        [Description("指示服务器未能识别请求.")]
        public const int RequestBadRequest = 400;
        /// <summary>
        ///     等效于 HTTP 状态 401。 System.Net.HttpStatusCode.Unauthorized 指示请求的资源要求身份验证。 WWW-Authenticate
        ///     标头包含如何执行身份验证的详细信息。
        /// </summary>
        [Description("指示请求的资源要求身份验证.")]
        public const int RequestUnauthorized = 401;
        /// <summary>
        ///     等效于 HTTP 状态 402。 保留 System.Net.HttpStatusCode.PaymentRequired 以供将来使用。
        /// </summary>
        [Description("以供将来使用.")]
        public const int RequestPaymentRequired = 402;
        /// <summary>
        ///     等效于 HTTP 状态 403。 System.Net.HttpStatusCode.Forbidden 指示服务器拒绝满足请求。
        /// </summary>
        [Description("指示服务器拒绝满足请求.")]
        public const int RequestForbidden = 403;
        /// <summary>
        ///     等效于 HTTP 状态 404。 System.Net.HttpStatusCode.NotFound 指示请求的资源不在服务器上。
        /// </summary>
        [Description("指示请求的资源不在服务器上.")]
        public const int RequestNotFound = 404;
        /// <summary>
        ///     等效于 HTTP 状态 405。 System.Net.HttpStatusCode.MethodNotAllowed 指示请求的资源上不允许请求方法（POST
        ///     或 GET）。
        /// </summary>
        [Description("指示请求的资源上不允许请求方法（POST 或 GET）.")]
        public const int RequestMethodNotAllowed = 405;
        /// <summary>
        ///     等效于 HTTP 状态 406。 System.Net.HttpStatusCode.NotAcceptable 指示客户端已用 Accept 标头指示将不接受资源的任何可用表示形式。
        /// </summary>
        [Description("指示客户端已用 Accept 标头指示将不接受资源的任何可用表示形式.")]
        public const int RequestNotAcceptable = 406;
        /// <summary>
        ///     等效于 HTTP 状态 407。 System.Net.HttpStatusCode.ProxyAuthenticationRequired 指示请求的代理要求身份验证。
        ///     Proxy-authenticate 标头包含如何执行身份验证的详细信息。
        /// </summary>
        [Description("指示请求的代理要求身份验证.")]
        public const int RequestProxyAuthenticationRequired = 407;
        /// <summary>
        ///     等效于 HTTP 状态 408。 System.Net.HttpStatusCode.RequestTimeout 指示客户端没有在服务器期望请求的时间内发送请求。
        /// </summary>
        [Description("指示客户端没有在服务器期望请求的时间内发送请求.")]
        public const int RequestTimeout = 408;
        /// <summary>
        ///     等效于 HTTP 状态 409。 System.Net.HttpStatusCode.Conflict 指示由于服务器上的冲突而未能执行请求。
        /// </summary>
        [Description("指示由于服务器上的冲突而未能执行请求.")]
        public const int RequestConflict = 409;
        /// <summary>
        ///     等效于 HTTP 状态 410。 System.Net.HttpStatusCode.Gone 指示请求的资源不再可用。
        /// </summary>
        [Description("指示请求的资源不再可用.")]
        public const int RequestGone = 410;
        /// <summary>
        ///     等效于 HTTP 状态 411。 System.Net.HttpStatusCode.LengthRequired 指示缺少必需的 Content-length
        ///     标头。
        /// </summary>
        [Description("指示缺少必需的 Content-length 标头.")]
        public const int RequestLengthRequired = 411;
        /// <summary>
        ///     等效于 HTTP 状态 412。 System.Net.HttpStatusCode.PreconditionFailed 指示为此请求设置的条件失败，且无法执行此请求。条件是用条件请求标头（如
        ///     If-Match、If-None-Match 或 If-Unmodified-Since）设置的。
        /// </summary>
        [Description("指示为此请求设置的条件失败，且无法执行此请求.")]
        public const int RequestPreconditionFailed = 412;
        /// <summary>
        ///     等效于 HTTP 状态 413。 System.Net.HttpStatusCode.RequestEntityTooLarge 指示请求太大，服务器无法处理。
        /// </summary>
        [Description("指示请求太大，服务器无法处理.")]
        public const int RequestEntityTooLarge = 413;
        /// <summary>
        ///     等效于 HTTP 状态 414。 System.Net.HttpStatusCode.RequestUriTooLong 指示 URI 太长。
        /// </summary>
        [Description("指示 URI 太长.")]
        public const int RequestUriTooLong = 414;
        /// <summary>
        ///     等效于 HTTP 状态 415。 System.Net.HttpStatusCode.UnsupportedMediaType 指示请求是不受支持的类型。
        /// </summary>
        [Description("指示请求是不受支持的类型.")]
        public const int RequestUnsupportedMediaType = 415;
        /// <summary>
        ///     等效于 HTTP 状态 416。 System.Net.HttpStatusCode.RequestedRangeNotSatisfiable 指示无法返回从资源请求的数据范围，因为范围的开头在资源的开头之前，或因为范围的结尾在资源的结尾之后。
        /// </summary>
        [Description("指示无法返回从资源请求的数据范围，因为范围的开头在资源的开头之前，或因为范围的结尾在资源的结尾之后.")]
        public const int RequestedRangeNotSatisfiable = 416;
        /// <summary>
        ///     等效于 HTTP 状态 417。 System.Net.HttpStatusCode.ExpectationFailed 指示服务器未能符合 Expect
        ///     标头中给定的预期值。
        /// </summary>
        [Description("指示服务器未能符合 Expect 标头中给定的预期值.")]
        public const int RequestExpectationFailed = 417;
        /// <summary>
        ///     等效于 HTTP 状态 500。 System.Net.HttpStatusCode.InternalServerError 指示服务器上发生了一般错误。
        /// </summary>
        [Description("指示服务器上发生了一般错误.")]
        public const int RequestInternalServerError = 500;
        /// <summary>
        ///     等效于 HTTP 状态 501。 System.Net.HttpStatusCode.NotImplemented 指示服务器不支持请求的函数。
        /// </summary>
        [Description("指示服务器不支持请求的函数.")]
        public const int RequestNotImplemented = 501;
        /// <summary>
        ///     等效于 HTTP 状态 502。 System.Net.HttpStatusCode.BadGateway 指示中间代理服务器从另一代理或原始服务器接收到错误响应。
        /// </summary>
        [Description("指示中间代理服务器从另一代理或原始服务器接收到错误响应.")]
        public const int RequestBadGateway = 502;
        /// <summary>
        ///     等效于 HTTP 状态 503。 System.Net.HttpStatusCode.ServiceUnavailable 指示服务器暂时不可用，通常是由于过多加载或维护。
        /// </summary>
        [Description("指示服务器暂时不可用，通常是由于过多加载或维护.")]
        public const int RequestServiceUnavailable = 503;
        /// <summary>
        ///     等效于 HTTP 状态 504。 System.Net.HttpStatusCode.GatewayTimeout 指示中间代理服务器在等待来自另一个代理或原始服务器的响应时已超时。
        /// </summary>
        [Description("指示中间代理服务器在等待来自另一个代理或原始服务器的响应时已超时.")]
        public const int RequestGatewayTimeout = 504;
        /// <summary>
        ///     等效于 HTTP 状态 505。 System.Net.HttpStatusCode.HttpVersionNotSupported 指示服务器不支持请求的
        ///     HTTP 版本。
        /// </summary>
        [Description("指示服务器不支持请求的 HTTP 版本.")]
        public const int RequestHttpVersionNotSupported = 505;

        #region 系统异常
        /// <summary> 服务器心情不好，请稍后重试~ </summary>
        [Description("服务器心情不好，请稍后重试~")]
        public const int SystemError = 5001;

        /// <summary> 参数错误。 </summary>
        [Description("参数错误")]
        public const int ParamaterError = 5002;

        /// <summary> 空引用异常。 </summary>
        [Description("空引用异常")]
        public const int NullError = 5003;

        /// <summary> 语法异常。 </summary>
        [Description("语法异常")]
        public const int SyntaxError = 5004;

        /// <summary> 除零异常。 </summary>
        [Description("除零异常")]
        public const int DivideByZeroError = 5006;

        /// <summary> 不支持异常。 </summary>
        [Description("不支持异常")]
        public const int NotSupportedError = 5007;

        /// <summary> 未实现异常。 </summary>
        [Description("未实现异常")]
        public const int NotImplementedError = 5008;

        /// <summary> 参数异常。 </summary>
        [Description("参数异常")]
        public const int ArgumentError = 5009;

        /// <summary> 参数空指针异常。 </summary>
        [Description("参数空指针异常")]
        public const int ArgumentNullError = 5010;

        /// <summary> 参数值越界异常。 </summary>
        [Description("参数值越界异常")]
        public const int ArgumentOutOfRangeError = 5011;

        /// <summary> 无效枚举参数异常。 </summary>
        [Description("无效枚举参数异常")]
        public const int InvalidEnumArgumentError = 5012;

        /// <summary> 索引越界异常。 </summary>
        [Description("索引越界异常")]
        public const int IndexOutOfRangeError = 5013;

        /// <summary> 索引越界异常。 </summary>
        [Description("索引越界异常")]
        public const int KeyNotFoundError = 5014;

        /// <summary> 类型转换异常。 </summary>
        [Description("类型转换异常")]
        public const int InvalidCastError = 5015;
        #endregion

        #region 业务层异常

        /// <summary> 业务异常。 </summary>
        [Description("业务异常")]
        public const int BusiError = 10001;

        #endregion

        #region 服务层异常
        /// <summary> 服务异常。 </summary>
        [Description("服务异常")]
        public const int ServError = 40001;

        /// <summary> 该请求调用受限。 </summary>
        [Description("该请求调用受限")]
        public const int ClientError = 40002;

        /// <summary> 请求超时。 </summary>
        [Description("请求超时")]
        public const int TimeOutError = 40003;

        /// <summary> 服务不标准。 </summary>
        [Description("服务不标准")]
        public const int NonstandardServerError = 4004;

        /// <summary> 没有可用的服务。 </summary>
        [Description("没有可用的服务")]
        public const int NoServiceError = 40005;
        #endregion

        #region 数据层异常
        /// <summary> 数据库异常。 </summary>
        [Description("数据库异常")]
        public const int DbError = 60001;
        /// <summary> 执行语句异常。 </summary>
        [Description("执行语句异常")]
        public const int SqlError = 60002;
        /// <summary> 未找到数据异常。 </summary>
        [Description("未找到数据异常")]
        public const int DbNotFoundError = 60003;
        /// <summary> 执行语句语法异常。 </summary>
        [Description("执行语句语法异常")]
        public const int SqlSyntaxError = 60004;
        #endregion
    }
}
