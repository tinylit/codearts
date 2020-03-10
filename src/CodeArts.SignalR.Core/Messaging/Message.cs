#if NET45 || NET451 || NET452 ||NET461
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace CodeArts.SignalR.Messaging
{
    /// <summary>
    /// 消息
    /// </summary>
    public class Message
    {
        /// <summary>
        /// 消息中心
        /// </summary>
        [JsonProperty("H")]
        public string Hub { get; set; }
        /// <summary>
        /// 调用方法
        /// </summary>
        [JsonProperty("M")]
        public string Method { get; set; }
        /// <summary>
        /// 参数
        /// </summary>
        [JsonProperty("A")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Type is used for serialization.")]
        public object[] Args { get; set; }
    }
}
#endif