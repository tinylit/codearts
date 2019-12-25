using CodeArts.Serialize.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeArts.Tests
{
    [TestClass]
    public class UriExtentions
    {
        [TestMethod]
        public void Get()
        {
            var value = "http://www.baidu.com".AsRequestable()
                 .ToQueryString(new
                 {
                     wd = "sql",
                     rsv_spt = 1,
                     rsv_iqid = "0x822dd2a900206e39",
                     issp = 1,
                     rsv_bp = 1,
                     rsv_idx = 2,
                     ie = "utf8"
                 })
                 .Json(new Dictionary<string, string>())
                 .Get();
        }

        [TestMethod]
        public void GetJson()
        {
            RuntimeServManager.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();

            //var token = "http://localhost:56324/login".AsRequestable()
            //    .Query("?account=ljl&password=liujialin&debug=true")
            //    .ByJson(new
            //    {
            //        data = new { type = string.Empty, token = string.Empty },
            //        status = true,
            //        code = 0,
            //        message = string.Empty,
            //        timestamp = DateTime.Now
            //    }, NamingType.CamelCase)
            //    .Get();

            //var values = "http://localhost:56324/api/values".AsRequestable()
            //    .Header("Authorization", token.data.type + " " + token.data.token)
            //    .ByJson<List<string>>()
            //    .Get();
        }

        [TestMethod]
        public void PostJson()
        {
            RuntimeServManager.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();

            //var token = "http://localhost:56324/login".AsRequestable()
            //    .Query("?account=ljl&password=liujialin&debug=true")
            //    .ByJson(new
            //    {
            //        data = new { type = string.Empty, token = string.Empty },
            //        status = true,
            //        code = 0,
            //        message = string.Empty,
            //        timestamp = DateTime.Now
            //    }, NamingType.CamelCase)
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

        [TestMethod]
        public async Task PostForm()
        {
            RuntimeServManager.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();

            var value = await "http://localhost:49683/weatherforecast".AsRequestable()
             .ToForm(new
             {
                 Date = DateTime.Now,
                 TemperatureC = 1,
                 Summary = 50
             })
             .Json(new Dictionary<string, string>())
             .PostAsync();
        }
    }
}
