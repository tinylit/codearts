![CodeArts](http://oss.jschar.com/codearts.png 'Logo')

### "CodeArts.Net"是什么？

CodeArts.Net 是HTTP/HTTPS请求工具，涵盖了刷新认证、重试、序列化、反序列化、数据验证与重发，文件上传下载等功能。

#### 使用方式：

* 获得请求能力。

  - `String`.AsRequestable()
  - `Uri`.AsRequestable()

* 根据业务需要，按照提示即可下发请求指令。

  - 普通请求。

    ```c#
    string result = await "api".AsRequestable()
        .AppendQueryString("?{params}")
        .GetAsync();
    ```

  - 认证信息刷新请求。

    ```c#
    string result = await "api".AsRequestable()
        .AppendQueryString("?{params}")
        .AssignHeader("Authorization", "Bearer 3506555d8a256b82211a62305b6dx317")
        .TryThen((requestable, e) => { // 仅会执行一次，与其它重试机制无关。
            //TODO:刷新认证信息。
        })
        .If(e => e.Status == WebExceptionStatus.ProtocolError)
        .And(e => e.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.Unauthorized)
        .GetAsync();
    ```

  - 重试。

    ```c#
    string result = await "api".AsRequestable()
        .AppendQueryString("?{params}")
        .TryIf(e => e.Status == WebExceptionStatus.Timeout) // 重试条件。
        .Or(e => e.Status == WebExceptionStatus.UnknownError)
        .RetryCount(2) // 设置重试次数。不设置，默认重试一次。
        .RetryInterval(500)//重试间隔时长。
        .GetAsync();
    ```

  - 序列化、反序列化、验证与重发。

    + 结果实体。

      ```c#
      public class ServResult
      {
          /// <summary>
          /// 状态码。
          /// </summary>
          [XmlElement("code")]
          public int Code { get; set; }
      
          private bool? success = null;
      
          /// <summary>
          /// 是否成功。
          /// </summary>
          [XmlIgnore]
          public bool Success{
          {
              get => success ?? Code == StatusCodes.OK;
              set => success = new bool?(value);
          }
      
          /// <summary>
          /// 错误信息。
          /// </summary>
          [XmlElement("msg")]
          public string Msg { get; set; }
      
          /// <summary>
          /// Utc。
          /// </summary>
          [XmlElement("timestamp")]
          public DateTime Timestamp { get; set; }
      }
      ```

    + 序列化。

      ```c#
      string result = await "api".AsRequestable()
          .Json(new
                {
                    Date = DateTime.Now,
                    TemperatureC = 1,
                    Summary = 50
                })
          .PostAsync();
      ```

    + 反序列化。

      ```c#
      ServResult result = await "api".AsRequestable()
          .Json(new
                {
                    Date = DateTime.Now,
                    TemperatureC = 1,
                    Summary = 50
                })
          .JsonCast<ServResult>()
          .PostAsync();
      ```

    + 验证与重发。

      ```c#
      ServResult result = await "api".AsRequestable()
          .Json(new
                {
                    Date = DateTime.Now,
                    TemperatureC = 1,
                    Summary = 50
                })
          .JsonCast<ServResult>()
          .DataVerify(r => r.Success) // 数据验证。
          .ResendCount(2) // 设置重发次数。不设置，默认重试一次。
          .ResendInterval((e, i) => i * i * 500)
          .PostAsync();
      ```

##### 说明：

* 基础请求配置。
  - `AssignHeader`设置求取头。
  - `AppendQueryString`添加请求参数。
    - 正常情况：多次添加相同的参数名称，不会被覆盖（数组场景）。
    - 刷新认证：`TryThen`函数中，会覆盖相同名称的参数。
* 请求方式。
  - 显示支持：GET、DELETE、POST、PUT、HEAD、PATCH。
  - 隐式支持：使用`Request`/`RequestAsync`方法，第一个参数为请求方式。
  - 文件处理：`UploadFile`/`UploadFileAsync`文件上传，`DownloadFile`/`DownloadFileAsync`文件下载。

* 数据传输。
  - Json：`content-type = "application/json"`。
  - Xml：`content-type = "application/xml"`。
  - Form：`content-type = "application/x-www-form-urlencoded"`。
  - Body：自己序列化数据和指定`content-type`。
* 数据接收。
  - XmlCast&lt;T&gt;：接收Xml格式数据，并自动反序列化为`T`类型。
  - JsonCast&lt;T&gt;：接收JSON格式数据，并自动反序列化为`T`类型，需要提供`IJsonHelper`接口支持，可以使用`CodeArts.Json`包。
  - String：接收任意格式结果。
* 刷新认证。
  - Then/ThenAsync/TryThen/TryThenAsync：请求异常刷新认证(每个设置，最多执行一次)。
  - If/And：需要刷新认证的条件。
* 重试。
  - TryIf/Or：重试条件（返回`true`代表需要重试）。
  - RetryCount：重试次数，默认：1次。
  - RetryInterval：重试时间间隔，默认：异常立即重试。
* 数据验证。
  - DataVerify/And：数据验证（返回`true`代表数据符合预期，不需要重试）。
  - ResendCount：重发次数，默认：1次。
  - ResendInterval：重发时间间隔，默认：异常立即重试。
* 其它。
  - WebCatch：捕获`WebException`异常，会继续抛出`WebException`异常。
  - WebCatch&lt;T&gt;：捕获`WebException`异常，并返回`T`结果，不抛异常。
  - XmlCatch：捕获`XmlException`异常，会继续抛出`XmlException`异常。
  - XmlCatch&lt;T&gt;：捕获`XmlException`异常，并返回`T`结果，不抛异常。
  - JsonCatch：捕获`JsonException`异常，会继续抛出`JsonException`异常。
  - JsonCatch&lt;T&gt;：捕获`JsonException`异常，并返回`T`结果，不抛异常。
  - Finally：请求完成（执行一次）。
  - UseEncoding：数据编码格式，默认：`UTF8`。