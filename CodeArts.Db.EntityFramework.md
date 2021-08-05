![CodeArts](http://oss.jschar.com/codearts.png 'Logo')

### "CodeArts.Db.EntityFramework"是什么？

CodeArts.Db.EntityFramework 是基于EF/EF Core易用性封装、事务单元、以及数据库解耦的一个轻量级框架。

#### 使用方式：

* 定义实体：与EF/EF Core无异。


* 定义仓库：无特殊业务操作时，可不用定义仓库，可注入默认实现。

  ```c#
  public class UserRepository : LinqRepository<User/* 表映射实体 */>
  {
  }
  ```

* 定义数据库上下文。

  ```c#
  /// <summary>
  /// 上下文。
  /// </summary>
  [DbConfig("connectionStrings:mssql")] // 数据库连接。
  public class EfContext : DbContext<EfContext>
  {
      /// <summary>
      /// 用户。
      /// </summary>
      public DbSet<User> Users { get; set; }
  }
  ```

* 设置数据库适配器。

  ```c#
  LinqConnectionManager.RegisterAdapter(new SqlServerLinqAdapter());
  ```

* 事务单元。

  ```c#
  /// <summary>
  /// 数据供应器。
  /// </summary>
  public class DbTransactionProvider : IDbTransactionProvider
  {
      /// <summary>
      /// 使用事务。
      /// </summary>
      /// <param name="dbContexts">上下文集合。</param>
      /// <returns></returns>
      public IDbTransaction BeginTransaction(params DbContext[] dbContexts) => new DbTransaction(dbContexts);
  
      /// <summary>
      /// 使用事务。
      /// </summary>
      /// <param name="repositories">仓库集合。</param>
      /// <returns></returns>
      public IDbTransaction BeginTransaction(params ILinqRepository[] repositories) => new DbTransaction(repositories);
  }
  ```

* 数据库上下文和仓库默认实现注入。

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
          LinqConnectionManager.RegisterAdapter(new SqlServerLinqAdapter()); //? 注册数据库适配器。
          
          services.AddSingleton<IDbTransactionProvider, DbTransactionProvider>() //? 注入事务单元。
                  .AddDefaultRepositories<EfContext>(); //? 注入上下文和上下文的仓库默认实现。
          
          base.ConfigureServices(services);
      }
  }
  ```

#### 说明：

* 使用与EF/EF Core 无异。
* 不需要再数据库上下文指定数据库类型，将数据库类型放到启动类中配置。
* 提供事务单元，确保数据提交一致性。