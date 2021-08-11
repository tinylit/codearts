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

### 特殊语法。

```c#
/// <summary>
/// 用户实体。
/// </summary>
[Naming(NamingType.UrlCase, Name = "fei_users")]
public class FeiUsers : BaseEntity<int>
{
    /// <summary>
    /// 用户ID。
    /// </summary>
    [Key]
    [Naming("uid")] // 真实的数据库字段。
    [ReadOnly(true)]
    public override int Id { get; set; }

    /// <summary>
    /// 公司ID。
    /// </summary>
    public int Bcid { get; set; }

    /// <summary>
    /// 用户名。
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// 邮箱。
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    /// <summary>
    /// 电话。
    /// </summary>
    public string Mobile { get; set; }

    /// <summary>
    /// 密码。
    /// </summary>
    [Required]
    public string Password { get; set; }

    /// <summary>
    /// 角色组。
    /// </summary>
    public short Mallagid { get; set; }

    /// <summary>
    /// 盐。
    /// </summary>
    public string Salt { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public int? Userstatus { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 修改时间。
    /// </summary>
    [DateTimeToken] // Token 在更新时，始终作为更新条件，以达到数据幂等。
    public DateTime ModifiedTime { get; set; }
}
```

- 更新。

  - 数据库路由更新。

    ```c#
    await user
        .From(x => x.TableName)
        .Where(x => x.Username == "admin")
        .UpdateAsync(x => new FeiUsers
        {
              Mallagid = 2,
              Username = x.Username.Substring(0, 4) // 数据库字段幂等更新。
        });
    ```

    ```sql
    UPDATE [x] SET [mallagid]=2,[username]= CASE WHEN [x].[username] IS NULL THEN NULL WHEN LEN([x].[username]) < 4 THEN @__variable_2 ELSE SUBSTRING([x].[username],1,4) END,[modified_time]=@__variable_3 FROM [fei_users] [x] WHERE ([x].[username] = @__variable_1)
    ```

    - @__variable_1："admin"
    - @__variable_2：string.Empty
    - @__variable_3：字段`ModifiedTime`的`DateTimeToken`标记生成的值。

  - 指定更新。

    ```c#
    var entry = new FeiUsers
    {
        Id = 1011,
        Bcid = 0,
        Userstatus = 1,
        Mobile = "18980861011",
        Email = "tinylit@foxmail.com",
        Password = "123456",
        Salt = string.Empty,
        CreatedTime = DateTime.Now,
        ModifiedTime = DateTime.Now
    };
    await user.AsUpdateable(entry) // 主键始终会作为更新条件。
        .Limit(x => x.Password) // 指定更新字段。
        .Where(x => x.Username ?? x.Mobile) // Username 为null时，使用 Mobile 作为条件。
        .ExecuteCommandAsync();
    ```
    
    ```sql
    UPDATE [fei_users] SET [password]=@password,[modified_time]=@__token_modified_time WHERE [uid]=@id AND [mobile]=@mobile AND [modified_time]=@modified_time;
    ```
    
    - 支持批量更新。
    - @id：主键条件始终生效。
    - @password：实体的`Password`属性值。
    - @__token_modified_time：字段`ModifiedTime`的`DateTimeToken`标记生成的值。
    - @username/@mobile：实体的`Username`属性值为`null`时，生成【mobile】字段条件，否则生成【username】字段条件。
    - @modified_time：字段`ModifiedTime`的传入值。

- 插入。

  - 数据库路由插入。

    ```c#
    await user.InsertAsync(details.Take(10).Select(x => new FeiUsers
    {
    	Username = x.Nickname,
    	Mobile = "18980861011",
    	Email = "tinylit@foxmail.com",
    	Password = "123456",
    	Salt = string.Empty
    }))
    ```

    ```sql
    INSERT INTO [fei_users]([username],[mobile],[email],[password],[salt])
    SELECT TOP (10) [x].[nickname],@__variable_1,@__variable_2,@__variable_3,@__variable_empty FROM [fei_userdetails] [x]
    ```

    - 只生成指定的字段。

  - 指定插入。

    ```c#
    var list = new List<FeiUsers>(10);
    
    for (int i = 0; i < 10; i++)
    {
        list.Add(new FeiUsers
        {
            Bcid = 0,
            Username = "admin",
            Userstatus = 1,
            Mobile = "18980861011",
            Email = "tinylit@foxmail.com",
            Password = "123456",
            Salt = string.Empty,
            CreatedTime = DateTime.Now,
            ModifiedTime = DateTime.Now
        });
    }
    await user.AsInsertable(list)
        .UseTransaction() // 启用事务。
        .ExecuteCommandAsync();
    ```

    ```sql
    INSERT INTO [fei_users]([bcid],[username],[email],[mobile],[password],[mallagid],[salt],[userstatus],[created_time],[modified_time]) VALUES (@bcid,@username,@email,@mobile,@password,@mallagid,@salt,@userstatus,@created_time,@modified_time),(@bcid_1,@username_1,@email_1,@mobile_1,@password_1,@mallagid_1,@salt_1,@userstatus_1,@created_time_1,@modified_time_1),(@bcid_2,@username_2,@email_2,@mobile_2,@password_2,@mallagid_2,@salt_2,@userstatus_2,@created_time_2,@modified_time_2),(@bcid_3,@username_3,@email_3,@mobile_3,@password_3,@mallagid_3,@salt_3,@userstatus_3,@created_time_3,@modified_time_3),(@bcid_4,@username_4,@email_4,@mobile_4,@password_4,@mallagid_4,@salt_4,@userstatus_4,@created_time_4,@modified_time_4),(@bcid_5,@username_5,@email_5,@mobile_5,@password_5,@mallagid_5,@salt_5,@userstatus_5,@created_time_5,@modified_time_5),(@bcid_6,@username_6,@email_6,@mobile_6,@password_6,@mallagid_6,@salt_6,@userstatus_6,@created_time_6,@modified_time_6),(@bcid_7,@username_7,@email_7,@mobile_7,@password_7,@mallagid_7,@salt_7,@userstatus_7,@created_time_7,@modified_time_7),(@bcid_8,@username_8,@email_8,@mobile_8,@password_8,@mallagid_8,@salt_8,@userstatus_8,@created_time_8,@modified_time_8),(@bcid_9,@username_9,@email_9,@mobile_9,@password_9,@mallagid_9,@salt_9,@userstatus_9,@created_time_9,@modified_time_9)
    ```

    - 只读主键，代表由数据库生成，不在SQL中体现。

- 批量删除。

  - 数据库路由删除。

    ```c#
    string username = "admi";
    await user.DeleteAsync(x => x.Username == username);
    ```

    ```c#
    DELETE [x] FROM [fei_users] [x] WHERE ([x].[username] = @username)
    ```

    - @username：参数名称优先使用变量名称、参数名称或常量名称。

  - 指定删除。

    ```c#
    var entry = new FeiUsers
    {
        Id = 1011
    };
    await user.AsDeleteable(entry)
        .ExecuteCommandAsync()
    ```

    ```sql
    DELETE FROM [fei_users] WHERE uid=@id
    ```

    - 通过主键删除数据。
    - 支持批量删除。

### 乐观锁。

​		CA、CB客户查阅并修改同一份数据，CA更新数据{A:"123",B:true}，然后CB更新数据{B:true,C:100}，解决CA更新的{A:"123"}被还原的问题。

​		乐观锁的原理，是利用实体某字段。如：long Version，前端更新时，将查询接口返回的`Version`传回后端，更新时，ORM会自动将`Version`作为更新条件，并刷新`Version`值。更新失败时，返回影响行为<kbd>0</kbd>。

​		实体可以有任意多个乐观锁属性，每次更新，属性都会被作为更新条件，在属性前标记特性：`TokenAttribute`的实现类即可。

```c#
/// <summary>
/// 令牌（作为更新数据库的唯一标识，会体现在更新的条件语句中）。
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public abstract class TokenAttribute : Attribute
{
    /// <summary>
    /// 创建新令牌。
    /// </summary>
    /// <returns></returns>
    public abstract object Create();
}

/// <summary>
/// 时间戳令牌。
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class DateTimeTicksAttribute : TokenAttribute
{
    public override object Create() => DateTime.Now.Ticks;
}
```

> 实用于任何更新方式。

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