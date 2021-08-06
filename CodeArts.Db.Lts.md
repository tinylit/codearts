![CodeArts](http://oss.jschar.com/codearts.png 'Logo')

### "CodeArts.Db.Lts"是什么？

CodeArts.Db.Lts 是基于Linq分析实现的、支持分表和读写分离的轻量级ORM框架。

#### 使用方式：

* 定义实体。

  ```c#
  [DbConfig] // 设置数据库连接（不分读写）。
  // [DbReadConfig] // 设置只读连接。
  // [DbWriteConfig] // 设置读写连接。
  [Naming(NamingType.UrlCase, Name = "yep_users")] // 指定整个实体的字段格式，指定当前实体映射表名称。
  public class User : BaseEntity<int> // 必须继承IEntity接口或实现了IEntity接口的类。
  {
      [Key] // 指定主键（用户操作更新和删除）
      [Naming("uid")] // 字段映射，将字段Id和数据库user字段添加映射关系。
      [ReadOnly(true)] //指定字段只读，字段值由数据库生成，不被插入或更新。
      public override int Id { get => base.Id; set => base.Id = value; }
  
      [Required] //设置字段必填
      [Display(Name = "用户名")] // 遵循 System.ComponentModel.DataAnnotations.ValidationAttribute 约束
      public string Username { get; set; }
  
      [Naming(NamingType.CamelCase)] // 如果字段特殊，可单独为字段设置字段命名规则。
      [DateTimeToken] // 设置Token键（在更新时，将属性数据作为更新条件，并为属性创建新值；在插入时，如若属性值为类型默认值，会自动获取新值）
      public DateTime PasswordLastChanged { get; set; }
  
      [Ignore]// 忽略，不会被生成SQL操作的字段。
      public bool HasValue => Id > 0;
  }
  ```

* 定义仓库：无特殊业务操作时，可不用定义仓库，可注入默认实现。

  - 只读仓库。

    ```c#
    [DbReadConfig] // （1）设置数据库连接。。
    // [DbConfig] // 设置数据库连接（不限读写）。
    public class UserRepository : Repository<User/* 表映射实体 */>
    {
        protected override ConnectionConfig GetDbConfig() => base.GetDbConfig(); // （2）设置数据库连接（会忽略（1）的配置）。
    }
    ```

  - 读写仓储。

    ```c#
    [DbWriteConfig] // （1）设置数据库连接。
    // [DbConfig] // 设置数据库连接（不限读写）。
    public class UserRepository : DbRepository<User/* 表映射实体 */>
    {
        protected override ConnectionConfig GetDbConfig() => new ConnectionConfig // （2）设置数据库连接（会忽略（1）的配置）。
        {
            Name = "yep.v3.invoice",
            ProviderName = "MySql",
            ConnectionString = "connectionString"
        };
    }
    ```

* 设置数据库适配器。

  ```c#
  DbConnectionManager.RegisterAdapter(new SqlServerLtsAdapter());
  DbConnectionManager.RegisterDatabaseFor<DapperFor>();
  ```

* UML

  ![UML](http://oss.jschar.com/ORM_UML.svg 'UML')

### 如何定义全局的 `System.ComponentModel.DataAnnotations.ValidationAttribute` 异常消息？

```c#
DbValidator.CustomValidate<RequiredAttribute>((attr, context) =>
{
    if (attr.AllowEmptyStrings)
    {
        return $"{context.DisplayName}不能为空!";
    }

    return $"{context.DisplayName}不能为null!";
});
```

#### 说明：

* 参数分析，当实体属性为值类型，且不为Nullable类型时，比较的参数为NULL时，自动忽略该条件。
* 由于“Take”和“Skip”参数的特殊性，在生成SQL语句时，将直接生成到SQL语句中，不会向参数字典中添加KeyValue。
* 支持Linq表达式。
* 支持几乎所有的Linq使用场景(可能会有函数顺序上的优化，目的是让翻译出来的SQL更加简单高效)。
* 支持特殊函数扩展（自定义“ICustomVisitor”接口，添加到适配器中即可）。
* 使用“SqlCapture”可捕获范围内分析的SQL语句以及参数。
* 支持大多数常见的字符串属性和函数，以及可空的类型支持。有关详细信息，请参阅单元测试。
* 详情请查阅：["CodeArts.Db.Linq2Sql"是什么？](./CodeArts.Db.Linq2Sql.md)
  - Unit testing.
    + [SqlServer](https://github.com/tinylit/codearts/blob/master/Tests/CodeArts.Db.Tests/SqlServerTest.cs)
    + [MySQL](https://github.com/tinylit/codearts/blob/master/Tests/CodeArts.Db.Tests/MySqlTest.cs)