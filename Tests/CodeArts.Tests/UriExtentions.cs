using CodeArts.Serialize.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace CodeArts.Tests
{
    [TestClass]
    public class UriExtentions
    {
        [TestMethod]
        public void Get()
        {
            RuntimeServManager.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();

            var value = "http://www.baidu.com".AsRequestable()
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
                 .Get();
        }

        [TestMethod]
        public void GetJson()
        {
            RuntimeServManager.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();

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
                .AgainCount(1)
                .Get();

            //var values = "http://localhost:56324/api/values".AsRequestable()
            //    .AppendHeader("Authorization", token.data.type + " " + token.data.token)
            //    .ByJson<List<string>>()
            //    .Get();
        }

        [TestMethod]
        public async Task GetJsonAsync()
        {
            RuntimeServManager.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();

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
                    //对请求的参数或Headers进行调整。如：令牌认证。 requestable.AppendHeader("Authorization", "{token}");
                })
                .If(e => e.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.Unauthorized) // 当认真过期时，才会执行上一个TryThen。
                .And(e => true)
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

        //[TestMethod]
        public void PostJson()
        {
            RuntimeServManager.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();

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
            RuntimeServManager.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();

            var value = await "http://localhost:49683/weatherforecast".AsRequestable()
             .Form(new
             {
                 Date = DateTime.Now,
                 TemperatureC = 1,
                 Summary = 50
             })
             .JsonCast(new Dictionary<string, string>())
             .PostAsync();
        }
    }
}
