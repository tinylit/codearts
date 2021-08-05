using CodeArts.Serialize.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.Tests
{
    [TestClass]
    public class UriExtentions
    {
        [TestMethod]
        public void Get()
        {
            RuntimeServPools.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();

            var value = "http://www.baidu.com/asasad/dsdfgf/ssasa?x=1".AsRequestable()
                 .AppendQueryString(new
                 {
                     wd = "sql",
                     rsv_spt = 1,
                     rsv_iqid = "0x822dd2a900206e39",
                     issp = 1,
                     rsv_bp = 1,
                     rsv_idx = 2,
                     ie = "utf8"
                 })
                 .JsonCast(new Dictionary<string, string>())
                 .JsonCatch((s, e) =>
                 {
                     return new Dictionary<string, string>();
                 })
                 .WebCatch(e =>
                 {
                     return new Dictionary<string, string>();
                 })
                 .Get();
        }

        [TestMethod]
        public void GetJson()
        {
            RuntimeServPools.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();

            var entry = new
            {
                data = new { type = string.Empty, token = string.Empty },
                status = true,
                code = 0,
                message = string.Empty,
                timestamp = DateTime.Now
            };

            var token = "http://localhost:56324/login".AsRequestable()
                .AppendQueryString("?account=ljl&password=liujialin&debug=true")
                .JsonCast(entry, NamingType.CamelCase)
                .WebCatch(e => entry)
                .DataVerify(x => x == entry)
                .ResendCount(1)
                .Get();

            //var values = "http://localhost:56324/api/values".AsRequestable()
            //    .AppendHeader("Authorization", token.data.type + " " + token.data.token)
            //    .ByJson<List<string>>()
            //    .Get();
        }

        [TestMethod]
        public async Task GetJsonAsync()
        {
            RuntimeServPools.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();

            var entry = new
            {
                data = new { type = string.Empty, token = string.Empty },
                status = true,
                code = 0,
                message = string.Empty,
                timestamp = DateTime.Now
            };


            var token = await "http://localhost:56324/login".AsRequestable()
                .AppendQueryString("?account=ljl&password=liujialin&debug=true")
                .TryThen((requestable, e) =>
                {
                    requestable.AppendQueryString("debug=false");
                    //对请求的参数或Headers进行调整。如：令牌认证。 requestable.AppendHeader("Authorization", "{token}");
                })
                .If(e => true) // 当认真过期时，才会执行上一个TryThen。
                .And(e => true)
                .ThenAsync((requestable, e) =>
                {
                    return Task.Delay(1000);
                })
                .If(e => true)
                .TryIf(e => e.Status == WebExceptionStatus.Timeout)
                .Or(e => true)
                .RetryCount(2) // 设置重试次数
                .RetryInterval(500)//重试间隔时长。
                .WebCatch(e => { })
                .WebCatch(e => { })
                .Finally(() =>
                {

                })
                .JsonCast(entry, NamingType.CamelCase)
                .WebCatch(e => entry)
                .Finally(() =>
                {

                })
                .GetAsync();

            //var values = "http://localhost:56324/api/values".AsRequestable()
            //    .AppendHeader("Authorization", token.data.type + " " + token.data.token)
            //    .ByJson<List<string>>()
            //    .Get();
        }

        [TestMethod]
        public async Task GetJsonThenAsync()
        {
            RuntimeServPools.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();

            var entry = new
            {
                data = new { type = string.Empty, token = string.Empty },
                status = true,
                code = 0,
                message = string.Empty,
                timestamp = DateTime.Now
            };


            var token = await "http://localhost:56324/login".AsRequestable()
                .AppendQueryString("?account=ljl&password=liujialin&debug=true")
                .AssignHeader("Authorization", "Bearer 3506555d8a256b82211a62305b6dx317")
                .TryThenAsync((requestable, e) =>
                {
                    requestable.AppendQueryString("debug=false");
                    //对请求的参数或Headers进行调整。如：令牌认证。 requestable.AppendHeader("Authorization", "{token}");

                    return Task.CompletedTask;
                })
                .If(e => e.Status == WebExceptionStatus.ProtocolError) // 当认真过期时，才会执行上一个TryThen。
                .And(e => e.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.Unauthorized)
                .TryIf(e => e.Status == WebExceptionStatus.Timeout)
                .Or(e => e.Status == WebExceptionStatus.UnknownError)
                .RetryCount(2) // 设置重试次数
                .RetryInterval(500)//重试间隔时长。
                .WebCatch(e => { })
                .WebCatch(e => { })
                .Finally(() =>
                {

                })
                .JsonCast(entry, NamingType.CamelCase)
                .WebCatch(e => entry)
                .Finally(() =>
                {

                })
                .GetAsync();

            //var values = "http://localhost:56324/api/values".AsRequestable()
            //    .AppendHeader("Authorization", token.data.type + " " + token.data.token)
            //    .ByJson<List<string>>()
            //    .Get();
        }

        [TestMethod]
        public async Task DownloadAsync()
        {
            string fileName = "C:\\dotnet-sdk-3.0.100-win-x64.exe";

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            try
            {
                await "https://download.visualstudio.microsoft.com/download/pr/53f250a1-318f-4350-8bda-3c6e49f40e76/e8cbbd98b08edd6222125268166cfc43/dotnet-sdk-3.0.100-win-x64.exe".AsRequestable()
                    .TryThen((r, e) =>
                    {

                    })
                    .If(e => true)
                    .TryIf(e => true)
                    .WebCatch(e => { })
                    .Finally(() =>
                    {
                        if (File.Exists(fileName))
                        {
                            File.Delete(fileName);
                        }
                    })
                    .DownloadFileAsync(fileName);
            }
            catch (WebException) { }
        }

        [TestMethod]
        public async Task EncodingTest()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var html = await "http://www.stats.gov.cn/tjsj/tjbz/tjyqhdmhcxhfdm/2020/13/1301.html"
                .AsRequestable()
                .UseEncoding(Encoding.GetEncoding("gb2312"))
                .GetAsync();
        }

        //[TestMethod]
        public void PostJson()
        {
            RuntimeServPools.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();

            //var token = "http://localhost:56324/login".AsRequestable()
            //    .ToQueryString("?account=ljl&password=liujialin&debug=true")
            //    .Json(new
            //    {
            //        data = new { type = string.Empty, token = string.Empty },
            //        status = true,
            //        code = 0,
            //        message = string.Empty,
            //        timestamp = DateTime.Now
            //    }, NamingType.CamelCase)
            //    .Catch(e =>
            //    {
            //        return new
            //        {
            //            data = new { type = string.Empty, token = string.Empty },
            //            status = true,
            //            code = 0,
            //            message = string.Empty,
            //            timestamp = DateTime.Now
            //        };
            //    })
            //    .Get();

            //var json = new
            //{
            //    page = 1,
            //    size = 10,
            //    shopId = 1000000000000,
            //    lsh = string.Empty,
            //    gmfmc = "优易票",
            //    kpr = "何远利",
            //    startTime = DateTime.Now
            //};

            //var value = "http://localhost:56324/api/values/invoice".AsRequestable()
            //    .Header("Authorization", token.data.type + " " + token.data.token)
            //    .Json(json)
            //    .ByJson(json)
            //    .Post();
        }

        //[TestMethod]
        public async Task PostForm()
        {
            RuntimeServPools.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();

            var value = await "http://localhost:49683/weatherforecast".AsRequestable()
             .Json(new
             {
                 Date = DateTime.Now,
                 TemperatureC = 1,
                 Summary = 50
             })
             .JsonCast<ServResult>()
             .DataVerify(r => r.Success)
             .ResendCount(2)
             .ResendInterval((e, i) => i * i * 500)
             .PostAsync();
        }
    }
}
