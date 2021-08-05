![CodeArts](http://oss.jschar.com/codearts.png 'Logo')

### "CodeArts.Middleware"是什么？

CodeArts.Middleware 是基于 Emit （`CodeArts.Emit`）实现代理的高效轻量级中间件框架。

#### 使用方式：

* 定义拦截器。

  ```c#
  /// <summary>
  /// 拦截上下文。
  /// </summary>
  public class InterceptContext
  {
      /// <summary>
      /// 构造函数。
      /// </summary>
      /// <param name="target">上下文。</param>
      /// <param name="main">调用函数。</param>
      /// <param name="inputs">函数参数。</param>
      public InterceptContext(object target, MethodInfo main, object[] inputs)
      {
          Target = target ?? throw new ArgumentNullException(nameof(target));
          Main = main ?? throw new ArgumentNullException(nameof(main));
          Inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
      }
  
      /// <summary>
      /// 上下文。
      /// </summary>
      public object Target { get; }
  
      /// <summary>
      /// 方法主体。
      /// </summary>
      public MethodInfo Main { get; }
  
      /// <summary>
      /// 输入参数。
      /// </summary>
      public object[] Inputs { get; }
  }
  // 拦截器。
  public class DependencyInterceptAttribute : InterceptAttribute
  {
      // 无返回值。
      public override void Run(InterceptContext context, Intercept intercept)
      {
          intercept.Run(context); // 继续执行。
      }
  	// 有返回值。
      public override T Run<T>(InterceptContext context, Intercept<T> intercept)
      {
          return intercept.Run(context); // 继续执行。
      }
      // 异步无返回值。
      public override Task RunAsync(InterceptContext context, InterceptAsync intercept)
      {
          return intercept.RunAsync(context); // 继续执行。
      }
      // 异步有返回值。
      public override Task<T> RunAsync<T>(InterceptContext context, InterceptAsync<T> intercept)
      {
          return intercept.RunAsync(context); // 继续执行。
      }
  }
  ```

* 标记接口/类/方法。
  - 接口/类：方法实现拦截（仅拦截声明方法）。
  - 方法：拦截指定方法。
  - 类的继承类或实现接口、继承类或实现接口的方法标记拦截，均会生效。

* 中间件注入。

  ```c#
  public class Startup : JwtStartup {
      public Startup()
      {
          using (var startup = new XStartup())
          {
              startup.DoStartup();
          }
      }    
      public override void ConfigureServices(IServiceCollection services)
      {        
          base.ConfigureServices(services);
          
          services.UseMiddleware(); // 代理实现，方法中间件。
      }
  }
  ```

#### 说明：

* 拦截类不能是值类型、密封类或抽象类。
* 支持代理泛型类、泛型接口、泛型方法、以及包含`out`或`ref`参数的方法。
* 拦截顺序:
  - 接口。
  - 类。
  - 继承类。
  - 方法。
  - 被重写的方法。

* 拦截器标记每次调用不会生成新实例。