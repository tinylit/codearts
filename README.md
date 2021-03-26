![CodeArts](https://raw.githubusercontent.com/tinylit/codearts/master/codearts.png '')

![GitHub](https://img.shields.io/github/license/tinylit/codearts.svg)
![language](https://img.shields.io/github/languages/top/tinylit/codearts.svg)
![codeSize](https://img.shields.io/github/languages/code-size/tinylit/codearts.svg)
![appveyor-ci](https://img.shields.io/appveyor/ci/tinylit/codearts.svg)
![AppVeyor tests (compact)](https://img.shields.io/appveyor/tests/tinylit/codearts.svg?compact_message)

### “CodeArts”是什么？
CodeArts 是一套简单、高效的轻量级框架（涵盖了类型转换、实体复制、实体映射，以及基于Linq分析实现的、支持分表和读写分离的ORM框架）。

### 如何使用？
在Object类型上做了扩展函数。使用非常简便，可以不做任何配置就能使用。

* Mapper.Cast
    + 源类型可以隐式或显式地转换为目标类型,或任何一个公共构造函数的第一个参数的目标类型满足源类型,或者可以将源类型转换,这只有一个参数或其他参数是可选的。
    + 当目标类型为集合时。任何一种转换失败时，目标类型的结果是目标类型的默认值。
        - 尝试将源类型转换为目标类型集合的元素。
        - 当源类型为集合时，将尝试将集合中的元素转换为目标类型的集合。

```csharp
    var guid = Mapper.Cast<Guid?>("0bbd0503-4879-42de-8cf0-666537b642e2"); // success

    var list = new List<string> { "11111", "2111", "3111" };

    var stack = Mapper.Cast<Stack<string>>(list); // success

    var listInt = Mapper.Cast<List<int>>(list); // success

    var quene = Mapper.Cast<Queue<int>>(list); // success

    var queneGuid = Mapper.Cast<Queue<Guid>>(list); // fail => null
```

```csharp
    public class CopyTest
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class CopyToTest : CopyTest
    {
        public DateTime Date { get; set; }
    }

    public class MapToTest
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime Date { get; set; }
    }
```

* Mapper.Copy
    + 源类型和目标类型相同.
    + 源类型是目标类型的后代类型（继承关系）。

```csharp
    var value = new CopyToTest
    {
        Id = 1000,
        Name = "test",
        Date = DateTime.Now
    };

    var copy1 = Mapper.Copy(value); // success
    var copy2 = Mapper.Copy<CopyTest>(value); // success
```

* Mapper.Map
    + 任意两种类型之间的映射。

```csharp
    var value = new CopyToTest
    {
        Id = 1000,
        Name = "test",
        Date = DateTime.Now
    };

    var map1 = Mapper.Map<CopyTest>(value); // success

    var map2 = Mapper.Map<MapToTest>(value); // success

    var map3 = Mapper.Map<IEnumerable<KeyValuePair<string, object>>>(value); // success

    var map4 = Mapper.Map<ICollection<KeyValuePair<string, object>>>(value); // success

    var map5 = Mapper.Map<Dictionary<string, object>>(value); // success
```

### 如何自定义数据关系？
数据转换、复制、映射的自定义方式相同。
* 声明定义，仅用于项目初始化，为任何目标类型指定代理，对每种类型全局唯一，以最后一次指定为准。

```csharp
    var copyTo = new CopyToExpression();

    //? Specify the proxy for the target type of "CopyTest".
    copyTo.Use((profile, type) =>
    {
        if (type == typeof(CopyToTest))
        {
            return source =>
            {
                var copy = (CopyTest)source;

                return new CopyTest
                {
                    Id = copy.Id,
                    Name = copy.Name
                };
            };
        }
        return profile.Create<CopyTest>(type);
    });

    RuntimeServPools.TryAddSingleton<ICopyToExpression>(() => copyTo);
```

* 为源类型指定到目标类型的代理。

```csharp
    var mapTo = RuntimeServPools.Singleton<IMapToExpression, MapToExpression>();

    //? Specify an agent of type "CopyToTest" to type "CopyTest".
    mapTo.Absolute<CopyToTest, CopyTest>(source =>
    {
        return new CopyTest
        {
            Id = source.Id,
            Name = source.Name
        };
    });

    //? Type "CopyTest" by specifying an agent of type "CopyToTest" or a subclass of "CopyToTest".
    mapTo.Run<CopyToTest, MapToTest>(source =>
    {
        return new MapToTest
        {
            Id = source.Id,
            Name = source.Name,
            Date = source.Date
        };
    });
```

* 为目标类型定制任何类型的代理。

```csharp
    var castTo = RuntimeServPools.Singleton<ICastToExpression, CastToExpression>();

    castTo.Map<string>(sourceType => sourceType.IsValueType, source => source.ToString());
```

### 如何安装？
First, [install NuGet](http://docs.nuget.org/docs/start-here/installing-nuget). Then, install [CodeArts](https://www.nuget.org/packages/CodeArts/) from the package manager console:

```
PM> Install-Package CodeArts
```

### 如何使用ORM？
* 定义实体。
``` csharp
    [DbConfig] // 设置数据库连接。
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
    }
```
* 定义仓库。
    + 只读仓库。
    ``` csharp
    [DbConfig] // 设置数据库连接。
    public class UserRepository : Repository<User/* 表映射实体 */>
    {
        protected override ConnectionConfig GetDbConfig() => base.GetDbConfig();
    }
    ```

    ```
	+ 可读写的仓库。
    ``` csharp
    [DbConfig] // （1）设置数据库连接。
    public class UserRepository : DbRepository<User/* 表映射实体 */>
    {
        protected override ConnectionConfig GetDbConfig() => new ConnectionConfig // （2）设置数据库连接（会忽略（1）的配置）。
        {
            Name = "yep.v3.invoice",
            ProviderName = "MySql",
            ConnectionString = ""
        };
    }
    ```

* 设置数据库适配器。
``` csharp
    DbConnectionManager.RegisterAdapter(new SqlServerLtsAdapter());
    DbConnectionManager.RegisterProvider<CodeArtsProvider>();
```

* 使用方式如下：
	+ 查询：
``` csharp
    var y1 = 100;
    var str = "1";
    Prepare();
    var user = new UserRepository();
    var details = new UserDetailsRepository();
    var userWx = new UserWeChatRepository();
    var results = from x in user.From(x => x.TableName) // 指定查询表（数据分表）。
                    join y in details on x.Id equals y.Id
                    join z in userWx on x.Id equals z.Uid
                    where x.Id > 0 && y.Id < y1 && x.Username.Contains(str)
                    orderby x.Id, y.Registertime descending
                    select new { x.Id, OldId = x.Id + 1, z.Openid };

    var list = results.ToList();
```
##### SQL：
``` SQL
SELECT 
    [x].[uid] AS [Id],
    ([x].[uid] + @__variable_2) AS [OldId],
    [z].[openid] AS [Openid] 
FROM [fei_users] [x] 
    LEFT JOIN [fei_userdetails] [y] ON [x].[uid] = [y].[uid] 
    LEFT JOIN [fei_user_wx_account_info] [z] ON [x].[uid] = [z].[uid] 
WHERE ((([x].[uid] > @__variable_1) 
    AND ([y].[uid] < @y1)) 
    AND [x].[username] LIKE @str) 
ORDER BY [x].[uid],[y].[registertime] DESC
```
	+更新(方式一)：
``` csharp
	var user = new UserRepository();
	var lines = user
		.From(x => x.TableName)
		.Where(x => x.Username == "admin")
		.Update(x => new FeiUsers
		{
			Mallagid = 2,
			Username = x.Username.Substring(0, 4) // 指定需要更新的字段。
		});
```
##### SQL：
``` SQL
UPDATE [x] 
	SET [mallagid]=@__variable_2,
	[username]=CASE 
		WHEN [x].[username] IS NULL OR (LEN([x].[username]) - @__variable_3) < 1 
		THEN @__variable_4 
		ELSE SUBSTRING([x].[username],@__variable_5,@__variable_6) 
		END,
	[modified_time]=@__variable_7 
FROM [fei_users] [x] 
WHERE ([x].[username] = @__variable_1)
```
	+更新（方式二）
``` csharp
	var user = new UserRepository();	
	var entry = new FeiUsers
	{
		Bcid = 0,
		Userstatus = 1,
		Mobile = "18980861011",
		Email = "tinylit@foxmail.com",
		Password = "123456",
		Salt = string.Empty,
		CreatedTime = DateTime.Now,
		ModifiedTime = DateTime.Now
	};
	var lines = user.AsUpdateable(entry)
			.Limit(x => x.Password) // 指定更新字段，可以有多个。
			.Where(x => x.Username ?? x.Mobile) // 指定更新条件，用户名称为空时，使用手机号作为条件，否则使用用户名称作为条件。
			.ExecuteCommand();
```
##### SQL：
``` SQL
UPDATE [fei_users] 
	SET [password]=@password,
	[modified_time]=@__token_modified_time 
WHERE [mobile]=@mobile AND [modified_time]=@modified_time
```

* 介绍：
> - 参数分析，当实体属性为值类型，且不为Nullable类型时，比较的参数为NULL时，自动忽略该条件。
> - 由于“Take”和“Skip”参数的特殊性，在生成SQL语句时，将直接生成到SQL语句中，不会向参数字典中添加KeyValue。
> - 支持Linq表达式。
> - 支持几乎所有的Linq使用场景(可能会有函数顺序上的优化，目的是让翻译出来的SQL更加简单高效)。
> - 支持特殊函数扩展（自定义“ICustomVisitor”接口，添加到适配器中即可）。
> - 使用“SqlCapture”可捕获范围内分析的SQL语句以及参数。
> - 支持大多数常见的字符串属性和函数，以及可空的类型支持。有关详细信息，请参阅单元测试。

##### UML
* [ORM Lts UML](https://www.processon.com/view/link/5ea8d6171e085346f728333a '框架模型')

* Unit testing.
    [SqlServer](https://github.com/tinylit/codearts/blob/master/Tests/CodeArts.Db.Tests/SqlServerTest.cs)
    [MySQL](https://github.com/tinylit/codearts/blob/master/Tests/CodeArts.Db.Tests/MySqlTest.cs)

### 如何使用Mvc？
* .NETCore | .NET
    + 普通模式（支持依赖注入、SwaggerUi、异常捕获以及其他功能）。
    ``` csharp
    public class Startup : DStartup {

    }
    ```
    + JWT认证模式(支持JWT认证，以及普通模式的所有功能)。
    ``` csharp
    public class Startup : JwtStartup {

    }
    ```

* .NET40(基于反射的实现)！
* .NET Web使用Startup类启动，放弃了传统的“Global.asax”启动模式，具有更强大的功能。

### 如何配置MVC？
* .NETCore
    ``` json
    {
        "Logging": {
            "LogLevel": {
                "Default": "Information",
                "Microsoft": "Warning",
                "Microsoft.Hosting.Lifetime": "Information"
            }
        },
        "jwt": {
			"authority": "" // 使用JwtStartup时必须。
            //"secret": "",  //密钥
            //"audience":"",  // 添加 identity.AddClaim(new Claim("aud", "jwt:audience".Config("api")));
            //"issuer":""  // 添加 identity.AddClaim(new Claim("iss", "jwt:issuer".Config("yep")));
        },
        "swagger": { // 可选
            //"title": "mvc.core2.2", // swagger 标题
            //"version": "v1" // swagger 版本号
        },
        "AllowedHosts": "*"
    }

    ```
* .NET
    ``` xml
    <?xml version="1.0" encoding="utf-8"?>

    <!--
    有关如何配置 ASP.NET 应用程序的详细信息，请访问
    https://go.microsoft.com/fwlink/?LinkId=169433
    -->

    <configuration>
        <appSettings>
            <!--jwt 密钥-->
            <!--<add key="jwt-secret" value="">-->
        </appSettings>
    </configuration>
    ```


### 性能比较。
功能|框架|性能|说明
:--:|---|:--:|---
MapTo|AutoMapper|~20%↑|框架是基于目标类型设计的。仅适用于简单的映射关系，需要建立复杂的映射关系，请使用AutoMapper。
ORM|SqlSugar|~15%↑|框架支持本地linq查询，支持双模式批处理。
ORM|Dapper|~6%↑|仅针对此ORM项目定制，如果需要使用特别复杂的操作，请使用Dapper。