![CodeArts](http://oss.jschar.com/codearts.png 'Logo')

### "CodeArts.Mvc"是什么？

CodeArts.Mvc 基于.NET/.NETCore MVC的封装框架，支持懒人依赖、SwaggerUI、异常捕获、JWT认证、跨域处理等功能。

#### 使用方式：

* .NETCore | .NET。

  - 普通模式（支持依赖注入、SwaggerUi、异常捕获以及其他功能）。

    ```c#
    public class Startup : DStartup {
        public Startup()
        {
            using (var startup = new XStartup()) //? 引包即用。
            {
                startup.DoStartup();
            }
        }
        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
    
            services
                .UseDependencyInjection() //? 自动注入控制器实现。
                .UseMiddleware(); //? IOC 中间件。
        }
    }
    ```

  - JWT认证模式(支持JWT认证，以及普通模式的所有功能)。

    ```c#
    public class Startup : JwtStartup {
        public JwtStartup()
        {
            using (var startup = new XStartup()) //? 引包即用。
            {
                startup.DoStartup();
            }
        }
        public override void ConfigureServices(IServiceCollection services)
        {
            services
                .UseDependencyInjection() //? 自动注入控制器实现。
                .UseMiddleware(); //? IOC 中间件。
        }
    }
    ```

* .NET40(基于反射的实现)！

* .NET MVC 使用Startup类启动，放弃了传统的“Global.asax”启动模式，具有更强大的功能（使用与.NETCore MVC类似）。

* 生成 JWT Token。

  ```c#
  JwtTokenGen.Create(new Dictionary<string, object>
          {
              ["id"] = 1011,
              ["name"] = "何远利",
              ["role"] = "Administrator,Developer"
          }, 120D);
  ```

#### 如何定义全局的 `System.ComponentModel.DataAnnotations.ValidationAttribute` 异常消息？

```c#
ModelValidator.CustomValidate<RequiredAttribute>((attr, context) =>
{
    if (attr.AllowEmptyStrings)
    {
        return $"{context.DisplayName}不能为空!";
  
    return $"{context.DisplayName}不能为null!";
});
```

#### 如何自定义异常捕获适配器？

* 定义适配器。

  ```c#
  public class DivideByZeroExceptionAdapter : ExceptionAdapter<DivideByZeroException> {
      // 定义异常返回的结果，接口调用抛出 DivideByZeroException 异常时，会触发该函数。
      protected override DResult GetResult(DivideByZeroException error){
          return DResult.Error($"除零异常:{error.Message}");
      }
  }
  ```

* 注册适配器。

  ```c#
  ExceptionHandler.Add(new DivideByZeroExceptionAdapter());
  ```

##### 说明：

* 修复`Javascript`【大数字】精度丢失问题（大数字传到前端自动转成字符串，后端接受自动转为指定类型）。
* 返回结果结果字段名称自动转为小驼峰。
* `System.Text.Json`常用类型反序列化补充。
* 接口转接（支持外部站点转接）。
  - 显示支持：GET、DELETE、POST、PUT。
  - 隐式支持：使用`Map`指定`HttpVerbs`请求方式。
    - HttpVerbs：GET|POST|PUT|DELETE|HEAD|PATCH|OPTIONS
  - 多请求方式支持：`HttpVerbs`支持`|`运算。

```c#
public class Startup : JwtStartup
{
    public Startup()
    {
        using (var startup = new XStartup())
        {
            startup.DoStartup();
        }
    }
    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.Map("api", HttpVerbs.GET | HttpVerbs.POST | HttpVerbs.PUT | HttpVerbs.DELETE, "destinationApi"); //? 接口转接。
        
        base.Configure(app, env);
    }
}
```

* 数据验证。

  - 基础接口。

    ```c#
    public class ValuesController: BaseController {//? 自动验证参数。    
    }
    ```

  - 用户接口。

    ```c#
    public class ValuesController: BaseController<User> {//? 自动将Jwt数据映射到User实例。 
        /// <summary>
        /// 获取当前登录用户信息。
        /// </summary>
        /// <returns></returns>
        [NonAction]
        protected override TUser GetUser() => base.GetUser();
    }
    ```

  - 用户数据接口。

    ```c#
    public class ValuesController: BaseController<User, UserData> {//? 自动将Jwt数据映射到User实例。  
        /// <summary>
        /// 获取当前登录用户信息。
        /// </summary>
        /// <returns></returns>
        [NonAction]
        protected override TUser GetUser() => base.GetUser();
        
        /// <summary>
        /// 获取用户信息。
        /// </summary>
        /// <param name="user">简易用户信息。</param>
        /// <returns></returns>
        [NonAction]
        protected override TUserData GetUserData(TUser user){
            // TODO: 获取用户数据。
        }
    }
    ```

    