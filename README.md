![SkyBuilding](https://raw.githubusercontent.com/tinylit/skybuilding/master/skybuilding.png '')

![GitHub](https://img.shields.io/github/license/tinylit/skybuilding.svg)
![language](https://img.shields.io/github/languages/top/tinylit/skybuilding.svg)
![codeSize](https://img.shields.io/github/languages/code-size/tinylit/skybuilding.svg)
![appveyor-ci](https://ci.appveyor.com/api/projects/status/hojhf5erylaap05b?svg=true)
![AppVeyor tests (compact)](https://img.shields.io/appveyor/tests/tinylit/skybuilding.svg?compact_message)

### What is SkyBuilding?
SkyBuilding is a lightweight, simple, and efficient infrastructure (including type cast, copy, mapping, and ORM support).

### How do I use it?
An extension method that has been embedded into an object. For simple use, you can use it without any configuration.

* CastTo
    + The source type can be implicitly or explicitly converted to the target type, or any one of the public constructors of the target type satisfies that the first argument is a source type, or that the source type can be converted, and that only one argument or other argument is optional.
    + When the target type is a collection. Either conversion fails, and the result of the target type is the default value of the target type.
        - Attempts to convert a source type to an element of a collection of target types.
        - When the source type is a collection, an attempt is made to convert the elements in the collection to the collection of the target type. 

```csharp
    var guid = "0bbd0503-4879-42de-8cf0-666537b642e2".CastTo<Guid?>();// success

    var list = new List<string> { "11111", "2111", "3111" };

    var stack = list.CastTo<Stack<string>>();// success

    var listInt = list.CastTo<List<int>>();// success

    var quene = list.CastTo<Queue<int>>();// success

    var queneGuid = list.CastTo<Queue<Guid>>(); // fail => null
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

* CopyTo
    + The source type is the same as the target type.
    + The source type is a subclass of the target type.

```csharp
    var value = new CopyToTest
    {
        Id = 1000,
        Name = "test",
        Date = DateTime.Now
    };

    var copy1 = value.CopyTo(); // success
    var copy2 = value.CopyTo<CopyTest>(); // success
```

* MapTo
    + Any two types of mappings.

```csharp
    var value = new CopyToTest
    {
        Id = 1000,
        Name = "test",
        Date = DateTime.Now
    };

    var map1 = value.MapTo<CopyTest>(); // success

    var map2 = value.MapTo<MapToTest>(); // success

    var map3 = value.MapTo<IEnumerable<KeyValuePair<string, object>>>(); // success

    var map4 = value.MapTo<ICollection<KeyValuePair<string, object>>>(); // success

    var map5 = value.MapTo<Dictionary<string, object>>(); // success
```

### How do I customize it?
The cast, copy, and mapping are customized in the same way.
* Declaration definition, used only for project initialization, specifying a proxy for any target   type, globally unique for each type, subject to the last designation.

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

    RuntimeServicePools.TryAddSingleton<ICopyToExpression>(() => copyTo);
```

* Specifies a proxy for the source type to the target type.

```csharp
    var mapTo = RuntimeServicePools.Singleton<IMapToExpression, MapToExpression>();

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

* Custom proxy for any type to the target type.
```csharp
    var castTo = RuntimeServicePools.Singleton<ICastToExpression, CastToExpression>();

    mapTo.Map<string>(sourceType => sourceType.IsValueType ,source => source.ToString());
```

### Where can I get it?
First, [install NuGet](http://docs.nuget.org/docs/start-here/installing-nuget). Then, install [SkyBuilding](https://www.nuget.org/packages/SkyBuilding/) from the package manager console:

```
PM> Install-Package SkyBuilding
```

### How to use Proxy?
##### The method proxies with "out" or "ref" arguments are not supported!
* Using the interface proxy.
``` csharp
	var of = ProxyGenerator.Of<IEmitTest>(new ProxyOptions(new NonIsByRefMethodsHook())); // Generate interface proxy.
	
	var instance = new EmitTest(); // Interface implementation class.
	
	var interceptor = new Interceptor(); // The interceptor.

	var proxy = of.Of(instance, interceptor); //Gets the interface proxy instance for the specified instance.
```

* Use a proxy class with a default constructor.
``` csharp
	var of = ProxyGenerator.New<EmitTest>(new ProxyOptions(new NonIsByRefMethodsHook())); // Generate class proxy.
	
	var interceptor = new Interceptor(); // The interceptor.

	var proxy = of.New(interceptor); // Gets an instance of a proxy.
```

* Using the class proxy.
``` csharp
	var of = ProxyGenerator.CreateInstance<EmitTest>(new ProxyOptions(new NonIsByRefMethodsHook())); // Generate class proxy.

	var interceptor = new Interceptor(); // The interceptor.

	var proxy = of.CreateInstance(interceptor, ...args/* The constructor parameter of the proscribed class. */); // Gets an instance of a proxy.
```

### How to use ORM?
* Entities defined.
``` csharp
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
* Repositories defined.
    + Provide query-only Repositories.
    ``` csharp
    [SqlServerConnection]
    public class UserRepository : Repository<User/* 表映射实体 */>
    {
        protected override ConnectionConfig GetDbConfig() => base.GetDbConfig();
    }
    ```

    + Provide a repository for queries and execution.
    ``` csharp
    public class UserRepository : DbRepository<User/* 表映射实体 */>
    {
        protected override ConnectionConfig GetDbConfig() => new ConnectionConfig
        {
            Name = "yep.v3.invoice",
            ProviderName = "MySql",
            ConnectionString = ""
        };
    }
    ```
* Set up the database adapter.
``` csharp
    DbConnectionManager.AddAdapter(new SqlServerAdapter());
    DbConnectionManager.AddProvider<SkyProvider>();
```

* For example,
``` csharp
    var y1 = 100;
    var str = "1";
    Prepare();
    var user = new UserRepository();
    var details = new UserDetailsRepository();
    var userWx = new UserWeChatRepository();
    var result = from x in user
                    join y in details on x.Id equals y.Id
                    join z in userWx on x.Id equals z.Uid
                    where x.Id > 0 && y.Id < y1 && x.Username.Contains(str)
                    orderby x.Id, y.Registertime descending
                    select new { x.Id, OldId = x.Id + 1, z.Openid };

    var list = result.ToList();
```
##### The query statement is as follows.
``` SQL
SELECT [x].[uid] AS [Id],
([x].[uid]+@__variable_2) AS [OldId],
[z].[openid] AS [Openid] 
FROM [fei_users] [x] 
LEFT JOIN [fei_userdetails] [y] ON [x].[uid]=[y].[uid] 
LEFT JOIN [fei_user_wx_account_info] [z] ON [x].[uid]=[z].[uid] 
WHERE ((([x].[uid]>@__variable_1) AND ([y].[uid]<@y1)) AND [x].[username] LIKE @str) -- @str is '%1%'
ORDER BY [x].[uid],[y].[registertime] DESC
```

* Introduce.
> - Parameter anti-injection.
> - Because the paging 'take' and 'skip' parameters are special parameters, they are not converted to anti-injection parameters for the convenience of viewing the SQL paging situation.
> - Support for linq continuations.
> - More than 200 scenarios are supported, and the singleton test only tests 70 scenarios, most of which can be used in combination.
> - Support for most common string properties and functions as well as Nullable<T> type support. See unit testing for details.

* Unit testing.
    [SqlServer](https://github.com/tinylit/skybuilding/blob/master/Tests/SkyBuilding.ORM.Tests/SqlServerTest.cs)
    [MySQL](https://github.com/tinylit/skybuilding/blob/master/Tests/SkyBuilding.ORM.Tests/MySqlTest.cs)

### How to use Mvc?
* .NETCore | .NET
    + Using normal(Support dependency injection, SwaggerUi, exception capture and other features).
    ``` csharp
    public class Startup : DStartup {

    }
    ```
    + Using JWT(Supports JWT authentication, login and authCode, and includes all normal features).
    ``` csharp
    public class Startup : JwtStartup {

    }
    ```

* .NET40(Reflection-based implementation)!
* Starting from 3.2, .NET Web use the Startup class to startup, giving up the traditional Global.asax startup mode, with more powerful functions.
* NETCore 3.1 temporarily drops support for SwaggerUi due to the instability of SwaggerUi.

### How to configure MVC?
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
        "login": "api/values/login", //登录地址，必填
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
            <!-- 登录地址：必须的 -->
            <add key="login" value="api/values/login" />
        </appSettings>
    </configuration>
    ```


### Performance comparison.
Function|Third-party libraries|Performance improvement|Explain
:--:|---|:--:|---
MapTo|AutoMapper|~20%↑|The framework is designed based on the target type. Only for simple mapping relationship, need to establish a complex mapping relationship, please use AutoMapper.
ORM|SqlSugar|~15%↑|The framework supports native linq queries, dual mode batch processing is supported.
ORM|Dapper|~6%↑|Only for this ORM project customized, if you need to use a particularly complex operation, please use Dapper.