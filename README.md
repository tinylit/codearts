![CodeArts](https://raw.githubusercontent.com/tinylit/codearts/master/codearts.png '')

![GitHub](https://img.shields.io/github/license/tinylit/codearts.svg)
![language](https://img.shields.io/github/languages/top/tinylit/codearts.svg)
![codeSize](https://img.shields.io/github/languages/code-size/tinylit/codearts.svg)
![appveyor-ci](https://img.shields.io/appveyor/ci/tinylit/codearts.svg)
![AppVeyor tests (compact)](https://img.shields.io/appveyor/tests/tinylit/codearts.svg?compact_message)

### What is CodeArts?
CodeArts is a lightweight, simple, and efficient infrastructure (including type cast, copy, mapping, and ORM support).

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
First, [install NuGet](http://docs.nuget.org/docs/start-here/installing-nuget). Then, install [CodeArts](https://www.nuget.org/packages/CodeArts/) from the package manager console:

```
PM> Install-Package CodeArts
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
    DbConnectionManager.RegisterAdapter(new SqlServerAdapter());
    DbConnectionManager.RegisterProvider<CodeArtsProvider>();
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
    [SqlServer](https://github.com/tinylit/codearts/blob/master/Tests/CodeArts.ORM.Tests/SqlServerTest.cs)
    [MySQL](https://github.com/tinylit/codearts/blob/master/Tests/CodeArts.ORM.Tests/MySqlTest.cs)

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
        "login": "api/values/login", //登录地址，返回登录信息，并自动生成登录令牌
        "register": "api/values/register", // 注册地址，接口返回用户信息时，会自动生成登录令牌
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

### ORM(UML)
<svg id="SvgjsSvg2046" width="1195" height="851" xmlns="http://www.w3.org/2000/svg" version="1.1" xmlns:xlink="http://www.w3.org/1999/xlink" xmlns:svgjs="http://svgjs.com/svgjs"><defs id="SvgjsDefs2047"><marker id="SvgjsMarker2116" markerWidth="16" markerHeight="12" refX="16" refY="6" viewBox="0 0 16 12" orient="auto" markerUnits="userSpaceOnUse"><path id="SvgjsPath2117" d="M0,2 L14,6 L0,11 L0,2" fill="#ff8000" stroke="#ff8000" stroke-width="2"></path></marker><marker id="SvgjsMarker2234" markerWidth="16" markerHeight="12" refX="16" refY="6" viewBox="0 0 16 12" orient="auto" markerUnits="userSpaceOnUse"><path id="SvgjsPath2235" d="M0,2 L14,6 L0,11 L0,2" fill="#ff66ff" stroke="#ff66ff" stroke-width="2"></path></marker><marker id="SvgjsMarker2242" markerWidth="16" markerHeight="12" refX="16" refY="6" viewBox="0 0 16 12" orient="auto" markerUnits="userSpaceOnUse"><path id="SvgjsPath2243" d="M0,2 L14,6 L0,11 L0,2" fill="#ff3399" stroke="#ff3399" stroke-width="2"></path></marker><marker id="SvgjsMarker2284" markerWidth="16" markerHeight="12" refX="16" refY="6" viewBox="0 0 16 12" orient="auto" markerUnits="userSpaceOnUse"><path id="SvgjsPath2285" d="M0,2 L14,6 L0,11 L0,2" fill="#00cccc" stroke="#00cccc" stroke-width="2"></path></marker><marker id="SvgjsMarker2296" markerWidth="16" markerHeight="12" refX="16" refY="6" viewBox="0 0 16 12" orient="auto" markerUnits="userSpaceOnUse"><path id="SvgjsPath2297" d="M0,2 L14,6 L0,11 L0,2" fill="#009999" stroke="#009999" stroke-width="2"></path></marker><marker id="SvgjsMarker2300" markerWidth="16" markerHeight="12" refX="16" refY="6" viewBox="0 0 16 12" orient="auto" markerUnits="userSpaceOnUse"><path id="SvgjsPath2301" d="M0,2 L14,6 L0,11 L0,2" fill="#ff0080" stroke="#ff0080" stroke-width="2"></path></marker><marker id="SvgjsMarker2316" markerWidth="16" markerHeight="12" refX="16" refY="6" viewBox="0 0 16 12" orient="auto" markerUnits="userSpaceOnUse"><path id="SvgjsPath2317" d="M0,2 L14,6 L0,11 L0,2" fill="#00cccc" stroke="#00cccc" stroke-width="2"></path></marker><marker id="SvgjsMarker2320" markerWidth="16" markerHeight="12" refX="16" refY="6" viewBox="0 0 16 12" orient="auto" markerUnits="userSpaceOnUse"><path id="SvgjsPath2321" d="M0,2 L14,6 L0,11 L0,2" fill="#ff0080" stroke="#ff0080" stroke-width="2"></path></marker><marker id="SvgjsMarker2365" markerWidth="16" markerHeight="12" refX="16" refY="6" viewBox="0 0 16 12" orient="auto" markerUnits="userSpaceOnUse"><path id="SvgjsPath2366" d="M0,2 L14,6 L0,11 L0,2" fill="#ff0080" stroke="#ff0080" stroke-width="2"></path></marker><marker id="SvgjsMarker2369" markerWidth="16" markerHeight="12" refX="16" refY="6" viewBox="0 0 16 12" orient="auto" markerUnits="userSpaceOnUse"><path id="SvgjsPath2370" d="M0,2 L14,6 L0,11 L0,2" fill="#ff99cc" stroke="#ff99cc" stroke-width="2"></path></marker></defs><g id="SvgjsG2048" transform="translate(308.5,64)"><path id="SvgjsPath2049" d="M 0 18Q 0 0 18 0L 75 0Q 93 0 93 18L 93 27Q 93 45 75 45L 18 45Q 0 45 0 27L 0 18Z" stroke="#323232" stroke-width="2" fill-opacity="1" fill="#ffffff"></path><g id="SvgjsG2050"><text id="SvgjsText2051" font-family="微软雅黑" text-anchor="middle" font-size="13px" width="32" fill="#323232" font-weight="400" align="middle" anchor="middle" family="微软雅黑" size="13px" weight="400" font-style="" y="12.55" transform="rotate(0)"><tspan id="SvgjsTspan2052" dy="16" x="46.5"><tspan id="SvgjsTspan2053" style="text-decoration:;">ORM</tspan></tspan></text></g></g><g id="SvgjsG2054" transform="translate(229.5,246)"><path id="SvgjsPath2055" d="M 0 4Q 0 0 4 0L 247 0Q 251 0 251 4L 251 86Q 251 90 247 90L 4 90Q 0 90 0 86Z" stroke="#323232" stroke-width="2" fill-opacity="1" fill="#ffffff"></path><path id="SvgjsPath2056" d="M 0 30L 251 30M 0 60L 251 60" stroke="#323232" stroke-width="2" fill="none"></path><path id="SvgjsPath2057" d="M 0 0L 251 0L 251 90L 0 90Z" stroke="none" fill="none"></path><g id="SvgjsG2058"><text id="SvgjsText2059" font-family="微软雅黑" text-anchor="middle" font-size="13px" width="123" fill="#323232" font-weight="700" align="middle" anchor="middle" family="微软雅黑" size="13px" weight="700" font-style="" y="5.05" transform="rotate(0)"><tspan id="SvgjsTspan2060" dy="16" x="125.5"><tspan id="SvgjsTspan2061" style="text-decoration:;">仓库（Repository）</tspan></tspan></text></g><g id="SvgjsG2062"><text id="SvgjsText2063" font-family="微软雅黑" text-anchor="start" font-size="13px" width="215" fill="#323232" font-weight="400" align="middle" anchor="start" family="微软雅黑" size="13px" weight="400" font-style="" y="35.05" transform="rotate(0)"><tspan id="SvgjsTspan2064" dy="16" x="10"><tspan id="SvgjsTspan2065" style="text-decoration:;">DbAdapter:</tspan><tspan id="SvgjsTspan2066" style="text-decoration:;fill: rgb(0, 204, 102);">IDbConnectionAdapter</tspan></tspan></text></g><g id="SvgjsG2067"><text id="SvgjsText2068" font-family="微软雅黑" text-anchor="start" font-size="13px" width="219" fill="#323232" font-weight="400" align="middle" anchor="start" family="微软雅黑" size="13px" weight="400" font-style="" y="65.05" transform="rotate(0)"><tspan id="SvgjsTspan2069" dy="16" x="10"><tspan id="SvgjsTspan2070" style="text-decoration:;">+ GetDbConfig():</tspan><tspan id="SvgjsTspan2071" style="text-decoration:;fill: rgb(0, 0, 255);">ConnectionConfig</tspan></tspan></text></g></g><g id="SvgjsG2072" transform="translate(25,93.125)"><path id="SvgjsPath2073" d="M 0 4Q 0 0 4 0L 224 0Q 228 0 228 4L 228 136.75Q 228 140.75 224 140.75L 4 140.75Q 0 140.75 0 136.75Z" stroke="#323232" stroke-width="2" fill-opacity="1" fill="#ffff99"></path><path id="SvgjsPath2074" d="M 0 32L 228 32M 0 114L 228 114" stroke="#323232" stroke-width="2" fill="none"></path><path id="SvgjsPath2075" d="M 0 0L 228 0L 228 140.75L 0 140.75Z" stroke="none" fill="none"></path><g id="SvgjsG2076"><text id="SvgjsText2077" font-family="微软雅黑" text-anchor="middle" font-size="13px" width="128" fill="#323232" font-weight="700" align="middle" anchor="middle" family="微软雅黑" size="13px" weight="700" font-style="" y="-1.95" transform="rotate(0)"><tspan id="SvgjsTspan2078" dy="16" x="114"><tspan id="SvgjsTspan2079" style="text-decoration:;">数据库连接配置</tspan></tspan><tspan id="SvgjsTspan2080" dy="16" x="114"><tspan id="SvgjsTspan2081" style="text-decoration:;">(ConnectionConfig)</tspan></tspan></text></g><g id="SvgjsG2082"><text id="SvgjsText2083" font-family="微软雅黑" text-anchor="start" font-size="13px" width="161" fill="#323232" font-weight="400" align="middle" anchor="start" family="微软雅黑" size="13px" weight="400" font-style="" y="30.05" transform="rotate(0)"><tspan id="SvgjsTspan2084" dy="16" x="10"><tspan id="SvgjsTspan2085" style="text-decoration:;"> </tspan></tspan><tspan id="SvgjsTspan2086" dy="16" x="10"><tspan id="SvgjsTspan2087" style="text-decoration:;">+ Name:string</tspan></tspan><tspan id="SvgjsTspan2088" dy="16" x="10"><tspan id="SvgjsTspan2089" style="text-decoration:;">+ ProviderName:string</tspan></tspan><tspan id="SvgjsTspan2090" dy="16" x="10"><tspan id="SvgjsTspan2091" style="text-decoration:;">+ ConnectionString:string</tspan></tspan><tspan id="SvgjsTspan2092" dy="16" x="10"><tspan id="SvgjsTspan2093" style="text-decoration:;"> </tspan></tspan></text></g><g id="SvgjsG2094"><text id="SvgjsText2095" font-family="微软雅黑" text-anchor="start" font-size="13px" width="0" fill="#323232" font-weight="400" align="middle" anchor="start" family="微软雅黑" size="13px" weight="400" font-style="" y="118.425" transform="rotate(0)"></text></g></g><g id="SvgjsG2096" transform="translate(480,154)"><path id="SvgjsPath2097" d="M 0 45C 0 -15 186 -15 186 45Q 182.28 88.2 93 90Q 62 90 31 81L 0 90L 21.762 77.13Q 0 62.99999999999999 0 45" stroke="#323232" stroke-width="2" fill-opacity="1" fill="#ffffff"></path><g id="SvgjsG2098"><text id="SvgjsText2099" font-family="微软雅黑" text-anchor="middle" font-size="13px" width="159" fill="#323232" font-weight="400" align="middle" anchor="middle" family="微软雅黑" size="13px" weight="400" font-style="" y="19.05" transform="rotate(0)"><tspan id="SvgjsTspan2100" dy="16" x="93"><tspan id="SvgjsTspan2101" style="text-decoration:;">实例化仓库时会调用</tspan></tspan><tspan id="SvgjsTspan2102" dy="16" x="93"><tspan id="SvgjsTspan2103" style="text-decoration:;">GetDbConfig方法初始仓库</tspan></tspan><tspan id="SvgjsTspan2104" dy="16" x="93"><tspan id="SvgjsTspan2105" style="text-decoration:;">和数据库的关系。</tspan></tspan></text></g></g><g id="SvgjsG2106" transform="translate(651,311)"><path id="SvgjsPath2107" d="M 0 34C 0 -11.333333333333334 126 -11.333333333333334 126 34Q 123.48 66.64 63 68Q 42 68 21 61.2L 0 68L 14.742 58.275999999999996Q 0 47.599999999999994 0 34" stroke="#323232" stroke-width="2" fill-opacity="1" fill="#ffffff"></path><g id="SvgjsG2108"><text id="SvgjsText2109" font-family="微软雅黑" text-anchor="middle" font-size="13px" width="104" fill="#323232" font-weight="400" align="middle" anchor="middle" family="微软雅黑" size="13px" weight="400" font-style="" y="16.05" transform="rotate(0)"><tspan id="SvgjsTspan2110" dy="16" x="63"><tspan id="SvgjsTspan2111" style="text-decoration:;">仓库操作的数据库</tspan></tspan><tspan id="SvgjsTspan2112" dy="16" x="63"><tspan id="SvgjsTspan2113" style="text-decoration:;">表信息。</tspan></tspan></text></g></g><g id="SvgjsG2114"><path id="SvgjsPath2115" d="M139 233.875L139 291L229.5 291" stroke="#ff8000" stroke-width="2" fill="none" marker-end="url(#SvgjsMarker2116)"></path></g><g id="SvgjsG2118" transform="translate(127.5,400)"><path id="SvgjsPath2119" d="M 0 4Q 0 0 4 0L 453 0Q 457 0 457 4L 457 104Q 457 108 453 108L 4 108Q 0 108 0 104Z" stroke="#323232" stroke-width="2" fill-opacity="1" fill="#ffffff"></path><path id="SvgjsPath2120" d="M 0 30L 457 30M 0 60L 457 60" stroke="#323232" stroke-width="2" fill="none"></path><path id="SvgjsPath2121" d="M 0 0L 457 0L 457 108L 0 108Z" stroke="none" fill="none"></path><g id="SvgjsG2122"><text id="SvgjsText2123" font-family="微软雅黑" text-anchor="middle" font-size="13px" width="189" fill="#323232" font-weight="700" align="middle" anchor="middle" family="微软雅黑" size="13px" weight="700" font-style="" y="5.05" transform="rotate(0)"><tspan id="SvgjsTspan2124" dy="16" x="228.5"><tspan id="SvgjsTspan2125" style="text-decoration:;">只读（Repository&lt;</tspan><tspan id="SvgjsTspan2126" style="text-decoration:;fill: rgb(0, 102, 204);">TEntity</tspan><tspan id="SvgjsTspan2127" style="text-decoration:;">&gt;）</tspan></tspan></text></g><g id="SvgjsG2128"><text id="SvgjsText2129" font-family="微软雅黑" text-anchor="start" font-size="13px" width="214" fill="#323232" font-weight="400" align="middle" anchor="start" family="微软雅黑" size="13px" weight="400" font-style="" y="35.05" transform="rotate(0)"><tspan id="SvgjsTspan2130" dy="16" x="10"><tspan id="SvgjsTspan2131" style="text-decoration:;">DbProvider:</tspan><tspan id="SvgjsTspan2132" style="text-decoration:;fill: rgb(0, 204, 102);">IDbRepositoryProvider</tspan></tspan></text></g><g id="SvgjsG2133"><text id="SvgjsText2134" font-family="微软雅黑" text-anchor="start" font-size="13px" width="385" fill="#323232" font-weight="400" align="middle" anchor="start" family="微软雅黑" size="13px" weight="400" font-style="" y="58.05" transform="rotate(0)"><tspan id="SvgjsTspan2135" dy="16" x="10"><tspan id="SvgjsTspan2136" style="text-decoration:;">+ Query&lt;</tspan><tspan id="SvgjsTspan2137" style="text-decoration:;fill: rgb(0, 127, 255);">TResult</tspan><tspan id="SvgjsTspan2138" style="text-decoration:;">&gt;(SQL,object,int?):</tspan><tspan id="SvgjsTspan2139" style="text-decoration:;fill: rgb(0, 204, 102);">IEnumerable</tspan><tspan id="SvgjsTspan2140" style="text-decoration:;">&lt;</tspan><tspan id="SvgjsTspan2141" style="text-decoration:;fill: rgb(0, 127, 255);">TResult</tspan><tspan id="SvgjsTspan2142" style="text-decoration:;">&gt;</tspan></tspan><tspan id="SvgjsTspan2143" dy="16" x="10"><tspan id="SvgjsTspan2144" style="text-decoration:;">+ QueryFirst&lt;</tspan><tspan id="SvgjsTspan2145" style="text-decoration:;fill: rgb(0, 127, 255);">TResult</tspan><tspan id="SvgjsTspan2146" style="text-decoration:;">&gt;(SQL,object,bool,int?):</tspan><tspan id="SvgjsTspan2147" style="text-decoration:;fill: rgb(0, 127, 255);">TResult</tspan></tspan><tspan id="SvgjsTspan2148" dy="16" x="10"><tspan id="SvgjsTspan2149" style="text-decoration:;">+ QueryFirstOrDefault&lt;</tspan><tspan id="SvgjsTspan2150" style="text-decoration:;fill: rgb(0, 127, 255);">TResult</tspan><tspan id="SvgjsTspan2151" style="text-decoration:;">&gt;(SQL,object,bool,int?):</tspan><tspan id="SvgjsTspan2152" style="text-decoration:;fill: rgb(0, 127, 255);">TResult</tspan></tspan></text></g></g><g id="SvgjsG2153" transform="translate(127.5,576)"><path id="SvgjsPath2154" d="M 0 4Q 0 0 4 0L 453 0Q 457 0 457 4L 457 216Q 457 220 453 220L 4 220Q 0 220 0 216Z" stroke="#323232" stroke-width="2" fill-opacity="1" fill="#ffffff"></path><path id="SvgjsPath2155" d="M 0 30L 457 30M 0 60L 457 60" stroke="#323232" stroke-width="2" fill="none"></path><path id="SvgjsPath2156" d="M 0 0L 457 0L 457 220L 0 220Z" stroke="none" fill="none"></path><g id="SvgjsG2157"><text id="SvgjsText2158" font-family="微软雅黑" text-anchor="middle" font-size="13px" width="208" fill="#323232" font-weight="700" align="middle" anchor="middle" family="微软雅黑" size="13px" weight="700" font-style="" y="5.05" transform="rotate(0)"><tspan id="SvgjsTspan2159" dy="16" x="228.5"><tspan id="SvgjsTspan2160" style="text-decoration:;">读写（DbRepository&lt;</tspan><tspan id="SvgjsTspan2161" style="text-decoration:;fill: rgb(0, 102, 204);">TEntity</tspan><tspan id="SvgjsTspan2162" style="text-decoration:;">&gt;）</tspan></tspan></text></g><g id="SvgjsG2163"><text id="SvgjsText2164" font-family="微软雅黑" text-anchor="start" font-size="13px" width="216" fill="#323232" font-weight="400" align="middle" anchor="start" family="微软雅黑" size="13px" weight="400" font-style="" y="35.05" transform="rotate(0)"><tspan id="SvgjsTspan2165" dy="16" x="10"><tspan id="SvgjsTspan2166" style="text-decoration:;">DbExecuter:</tspan><tspan id="SvgjsTspan2167" style="text-decoration:;fill: rgb(0, 204, 102);">IDbRepositoryExecuter</tspan></tspan></text></g><g id="SvgjsG2168"><text id="SvgjsText2169" font-family="微软雅黑" text-anchor="start" font-size="13px" width="386" fill="#323232" font-weight="400" align="middle" anchor="start" family="微软雅黑" size="13px" weight="400" font-style="" y="58.05" transform="rotate(0)"><tspan id="SvgjsTspan2170" dy="16" x="10"><tspan id="SvgjsTspan2171" style="text-decoration:;">+AsExecuteable():</tspan><tspan id="SvgjsTspan2172" style="text-decoration:;fill: rgb(0, 204, 102);">IExecuteable</tspan><tspan id="SvgjsTspan2173" style="text-decoration:;">&lt;</tspan><tspan id="SvgjsTspan2174" style="text-decoration:;fill: rgb(0, 76, 153);">TEntity</tspan><tspan id="SvgjsTspan2175" style="text-decoration:;">&gt;</tspan></tspan><tspan id="SvgjsTspan2176" dy="16" x="10"><tspan id="SvgjsTspan2177" style="text-decoration:;">+AsInsertable(</tspan><tspan id="SvgjsTspan2178" style="text-decoration:;fill: rgb(0, 76, 153);">TEntity</tspan><tspan id="SvgjsTspan2179" style="text-decoration:;">):</tspan><tspan id="SvgjsTspan2180" style="text-decoration:;fill: rgb(0, 204, 102);">IInsertable</tspan><tspan id="SvgjsTspan2181" style="text-decoration:;">&lt;</tspan><tspan id="SvgjsTspan2182" style="text-decoration:;fill: rgb(0, 76, 153);">TEntity</tspan><tspan id="SvgjsTspan2183" style="text-decoration:;">&gt;</tspan></tspan><tspan id="SvgjsTspan2184" dy="16" x="10"><tspan id="SvgjsTspan2185" style="text-decoration:;">+AsInsertable(</tspan><tspan id="SvgjsTspan2186" style="text-decoration:;fill: rgb(0, 204, 102);">IEnumerable</tspan><tspan id="SvgjsTspan2187" style="text-decoration:;">&lt;</tspan><tspan id="SvgjsTspan2188" style="text-decoration:;fill: rgb(0, 76, 153);">TEntity</tspan><tspan id="SvgjsTspan2189" style="text-decoration:;">&gt;):</tspan><tspan id="SvgjsTspan2190" style="text-decoration:;fill: rgb(0, 204, 102);">IInsertable</tspan><tspan id="SvgjsTspan2191" style="text-decoration:;">&lt;</tspan><tspan id="SvgjsTspan2192" style="text-decoration:;fill: rgb(0, 76, 153);">TEntity</tspan><tspan id="SvgjsTspan2193" style="text-decoration:;">&gt;</tspan></tspan><tspan id="SvgjsTspan2194" dy="16" x="10"><tspan id="SvgjsTspan2195" style="text-decoration:;">+AsUpdateable(</tspan><tspan id="SvgjsTspan2196" style="text-decoration:;fill: rgb(0, 102, 204);">TEntity</tspan><tspan id="SvgjsTspan2197" style="text-decoration:;">):</tspan><tspan id="SvgjsTspan2198" style="text-decoration:;fill: rgb(0, 204, 102);">IUpdateable</tspan><tspan id="SvgjsTspan2199" style="text-decoration:;">&lt;</tspan><tspan id="SvgjsTspan2200" style="text-decoration:;fill: rgb(0, 102, 204);">TEntity</tspan><tspan id="SvgjsTspan2201" style="text-decoration:;">&gt;</tspan></tspan><tspan id="SvgjsTspan2202" dy="16" x="10"><tspan id="SvgjsTspan2203" style="text-decoration:;">+AsUpdateable(</tspan><tspan id="SvgjsTspan2204" style="text-decoration:;fill: rgb(0, 204, 102);">IEnumerable</tspan><tspan id="SvgjsTspan2205" style="text-decoration:;">&lt;</tspan><tspan id="SvgjsTspan2206" style="text-decoration:;fill: rgb(0, 102, 204);">TEntity</tspan><tspan id="SvgjsTspan2207" style="text-decoration:;">&gt;):</tspan><tspan id="SvgjsTspan2208" style="text-decoration:;fill: rgb(0, 204, 102);">IUpdateable</tspan><tspan id="SvgjsTspan2209" style="text-decoration:;">&lt;</tspan><tspan id="SvgjsTspan2210" style="text-decoration:;fill: rgb(0, 102, 204);">TEntity</tspan><tspan id="SvgjsTspan2211" style="text-decoration:;">&gt;</tspan></tspan><tspan id="SvgjsTspan2212" dy="16" x="10"><tspan id="SvgjsTspan2213" style="text-decoration:;">+AsDeleteable(TEntity):</tspan><tspan id="SvgjsTspan2214" style="text-decoration:;fill: rgb(0, 204, 102);">IDeleteable</tspan><tspan id="SvgjsTspan2215" style="text-decoration:;">&lt;TEntity&gt;</tspan></tspan><tspan id="SvgjsTspan2216" dy="16" x="10"><tspan id="SvgjsTspan2217" style="text-decoration:;">+AsDeleteable(</tspan><tspan id="SvgjsTspan2218" style="text-decoration:;fill: rgb(0, 204, 102);">IEnumerable</tspan><tspan id="SvgjsTspan2219" style="text-decoration:;">&lt;</tspan><tspan id="SvgjsTspan2220" style="text-decoration:;fill: rgb(0, 102, 204);">TEntity</tspan><tspan id="SvgjsTspan2221" style="text-decoration:;">&gt;):</tspan><tspan id="SvgjsTspan2222" style="text-decoration:;fill: rgb(0, 204, 102);">IDeleteable</tspan><tspan id="SvgjsTspan2223" style="text-decoration:;">&lt;</tspan><tspan id="SvgjsTspan2224" style="text-decoration:;fill: rgb(0, 102, 204);">TEntity</tspan><tspan id="SvgjsTspan2225" style="text-decoration:;">&gt;</tspan></tspan><tspan id="SvgjsTspan2226" dy="16" x="10"><tspan id="SvgjsTspan2227" style="text-decoration:;">+Insert(SQL,object,int?):int</tspan></tspan><tspan id="SvgjsTspan2228" dy="16" x="10"><tspan id="SvgjsTspan2229" style="text-decoration:;">+Update(SQL,object,int?):int</tspan></tspan><tspan id="SvgjsTspan2230" dy="16" x="10"><tspan id="SvgjsTspan2231" style="text-decoration:;">+Delete(SQL,object,int?):int</tspan></tspan></text></g></g><g id="SvgjsG2232"><path id="SvgjsPath2233" d="M355 400L355 368L355 368L355 336" stroke="#ff66ff" stroke-width="2" fill="none" marker-end="url(#SvgjsMarker2234)"></path><rect id="SvgjsRect2236" width="26" height="16" x="342" y="360" fill="#ffffff"></rect><text id="SvgjsText2237" font-family="微软雅黑" text-anchor="middle" font-size="13px" width="26" fill="#323232" font-weight="400" align="top" anchor="middle" family="微软雅黑" size="13px" weight="400" font-style="" y="358.05" transform="rotate(0)"><tspan id="SvgjsTspan2238" dy="16" x="355"><tspan id="SvgjsTspan2239" style="text-decoration:;">继承</tspan></tspan></text></g><g id="SvgjsG2240"><path id="SvgjsPath2241" d="M355 576L355 547.0204081632653L355 547.0204081632653L355 518.0408163265306" stroke="#ff3399" stroke-width="2" fill="none" marker-end="url(#SvgjsMarker2242)"></path><rect id="SvgjsRect2244" width="26" height="16" x="342" y="539.0204081632653" fill="#ffffff"></rect><text id="SvgjsText2245" font-family="微软雅黑" text-anchor="middle" font-size="13px" width="26" fill="#323232" font-weight="400" align="top" anchor="middle" family="微软雅黑" size="13px" weight="400" font-style="" y="537.0704081632653" transform="rotate(0)"><tspan id="SvgjsTspan2246" dy="16" x="355"><tspan id="SvgjsTspan2247" style="text-decoration:;">继承</tspan></tspan></text></g><g id="SvgjsG2248" transform="translate(713,25)"><path id="SvgjsPath2249" d="M 0 4Q 0 0 4 0L 453 0Q 457 0 457 4L 457 198Q 457 202 453 202L 4 202Q 0 202 0 198Z" stroke="#ff9933" stroke-width="2" fill-opacity="1" fill="#ffffff"></path><path id="SvgjsPath2250" d="M 0 25L 137.1 25L 145.1 17L 145.1 0" stroke="#ff9933" stroke-width="2" fill-opacity="1" fill="#ffffff"></path><path id="SvgjsPath2251" d="M 0 0L 457 0L 457 202L 0 202Z" stroke="none" fill="none"></path><g id="SvgjsG2252"><text id="SvgjsText2253" font-family="微软雅黑" text-anchor="start" font-size="13px" width="420" fill="#323232" font-weight="400" align="top" anchor="start" family="微软雅黑" size="13px" weight="400" font-style="" y="28.05" transform="rotate(0)"><tspan id="SvgjsTspan2254" dy="16" x="10"><tspan id="SvgjsTspan2255" style="text-decoration:;">一、指定仓库数据库关系，方式如下（任意一个即可）：</tspan></tspan><tspan id="SvgjsTspan2256" dy="16" x="10"><tspan id="SvgjsTspan2257" style="text-decoration:;">   1、为仓库标记【</tspan><tspan id="SvgjsTspan2258" style="text-decoration:;fill: rgb(0, 153, 153);">DbConfigAttribute</tspan><tspan id="SvgjsTspan2259" style="text-decoration:;">】属性。</tspan></tspan><tspan id="SvgjsTspan2260" dy="16" x="10"><tspan id="SvgjsTspan2261" style="text-decoration:;">   2、重写【GetDbConfig】方法（优先级更高）。</tspan></tspan><tspan id="SvgjsTspan2262" dy="16" x="10"><tspan id="SvgjsTspan2263" style="text-decoration:;">二、指定数据库的适配器：</tspan></tspan><tspan id="SvgjsTspan2264" dy="16" x="10"><tspan id="SvgjsTspan2265" style="text-decoration:;">    DbConnectionManager.RegisterAdapter(</tspan><tspan id="SvgjsTspan2266" style="text-decoration:;fill: rgb(0, 153, 77);">IDbConnectionAdapter</tspan><tspan id="SvgjsTspan2267" style="text-decoration:;">);</tspan></tspan><tspan id="SvgjsTspan2268" dy="16" x="10"><tspan id="SvgjsTspan2269" style="text-decoration:;">三、指定数据库类型的供应器，方式如下（任意一个即可）：</tspan></tspan><tspan id="SvgjsTspan2270" dy="16" x="10"><tspan id="SvgjsTspan2271" style="text-decoration:;">    1、DbConnectionManager.RegisterProvider&lt;</tspan><tspan id="SvgjsTspan2272" style="text-decoration:;fill: rgb(0, 102, 204);">TProvider</tspan><tspan id="SvgjsTspan2273" style="text-decoration:;">&gt;(string);</tspan></tspan><tspan id="SvgjsTspan2274" dy="16" x="10"><tspan id="SvgjsTspan2275" style="text-decoration:;">    2、DbConnectionManager.RegisterProvider&lt;</tspan><tspan id="SvgjsTspan2276" style="text-decoration:;fill: rgb(0, 127, 255);">TProvider</tspan><tspan id="SvgjsTspan2277" style="text-decoration:;">&gt;();</tspan></tspan></text></g><g id="SvgjsG2278"><text id="SvgjsText2279" font-family="微软雅黑" text-anchor="start" font-size="13px" width="26" fill="#323232" font-weight="400" align="middle" anchor="start" family="微软雅黑" size="13px" weight="400" font-style="" y="2.55" transform="rotate(0)"><tspan id="SvgjsTspan2280" dy="16" x="10"><tspan id="SvgjsTspan2281" style="text-decoration:;fill: rgb(255, 153, 51);">配置</tspan></tspan></text></g></g><g id="SvgjsG2282"><path id="SvgjsPath2283" d="M629 416L605.5 416L605.5 416L584.5 416" stroke="#00cccc" stroke-width="2" fill="none" marker-end="url(#SvgjsMarker2284)"></path></g><g id="SvgjsG2286" transform="translate(864,286)"><path id="SvgjsPath2287" d="M 0 45C 0 -15 178 -15 178 45Q 174.44 88.2 89 90Q 59.333333333333336 90 29.666666666666668 81L 0 90L 20.826 77.13Q 0 62.99999999999999 0 45" stroke="#323232" stroke-width="2" fill-opacity="1" fill="#ffffff"></path><g id="SvgjsG2288"><text id="SvgjsText2289" font-family="微软雅黑" text-anchor="middle" font-size="13px" width="156" fill="#323232" font-weight="400" align="middle" anchor="middle" family="微软雅黑" size="13px" weight="400" font-style="" y="27.05" transform="rotate(0)"><tspan id="SvgjsTspan2290" dy="16" x="89"><tspan id="SvgjsTspan2291" style="text-decoration:;">为仓库提供查询以及修改的</tspan></tspan><tspan id="SvgjsTspan2292" dy="16" x="89"><tspan id="SvgjsTspan2293" style="text-decoration:;">指令操作。</tspan></tspan></text></g></g><g id="SvgjsG2294"><path id="SvgjsPath2295" d="M842 417L812 417L812 257L941.5 257L941.5 227" stroke="#009999" stroke-width="2" fill="none" marker-end="url(#SvgjsMarker2296)"></path></g><g id="SvgjsG2298"><path id="SvgjsPath2299" d="M713 126L674.75 126L674.75 291L480.5 291" stroke="#ff0080" stroke-width="2" fill="none" marker-end="url(#SvgjsMarker2300)"></path></g><g id="SvgjsG2302" transform="translate(713,478)"><path id="SvgjsPath2303" d="M 0 4Q 0 0 4 0L 354 0Q 358 0 358 4L 358 53Q 358 57 354 57L 4 57Q 0 57 0 53Z" stroke="#009900" stroke-width="2" fill-opacity="1" fill="#ffffff"></path><path id="SvgjsPath2304" d="M 0 25L 107.39999999999999 25L 115.39999999999999 17L 115.39999999999999 0" stroke="#009900" stroke-width="2" fill-opacity="1" fill="#ffffff"></path><path id="SvgjsPath2305" d="M 0 0L 358 0L 358 57L 0 57Z" stroke="none" fill="none"></path><g id="SvgjsG2306"><text id="SvgjsText2307" font-family="微软雅黑" text-anchor="start" font-size="13px" width="91" fill="#323232" font-weight="400" align="top" anchor="start" family="微软雅黑" size="13px" weight="400" font-style="" y="28.05" transform="rotate(0)"><tspan id="SvgjsTspan2308" dy="16" x="10"><tspan id="SvgjsTspan2309" style="text-decoration:;fill: rgb(0, 153, 153);">支持Linq语法。</tspan></tspan></text></g><g id="SvgjsG2310"><text id="SvgjsText2311" font-family="微软雅黑" text-anchor="start" font-size="13px" width="26" fill="#323232" font-weight="400" align="middle" anchor="start" family="微软雅黑" size="13px" weight="400" font-style="" y="2.55" transform="rotate(0)"><tspan id="SvgjsTspan2312" dy="16" x="10"><tspan id="SvgjsTspan2313" style="text-decoration:;fill: rgb(0, 153, 77);">语法</tspan></tspan></text></g></g><g id="SvgjsG2314"><path id="SvgjsPath2315" d="M629 417L612.5 417L612.5 595L584.5 595" stroke="#00cccc" stroke-width="2" fill="none" marker-end="url(#SvgjsMarker2316)"></path></g><g id="SvgjsG2318"><path id="SvgjsPath2319" d="M713 506.5L647.75 506.5L647.75 454L584.5 454" stroke="#ff0080" stroke-width="2" fill="none" marker-end="url(#SvgjsMarker2320)"></path></g><g id="SvgjsG2322" transform="translate(713,546)"><path id="SvgjsPath2323" d="M 0 4Q 0 0 4 0L 396 0Q 400 0 400 4L 400 276Q 400 280 396 280L 4 280Q 0 280 0 276Z" stroke="#009900" stroke-width="2" fill-opacity="1" fill="#ffffff"></path><path id="SvgjsPath2324" d="M 0 25L 120 25L 128 17L 128 0" stroke="#009900" stroke-width="2" fill-opacity="1" fill="#ffffff"></path><path id="SvgjsPath2325" d="M 0 0L 400 0L 400 280L 0 280Z" stroke="none" fill="none"></path><g id="SvgjsG2326"><text id="SvgjsText2327" font-family="微软雅黑" text-anchor="start" font-size="13px" width="307" fill="#323232" font-weight="400" align="top" anchor="start" family="微软雅黑" size="13px" weight="400" font-style="" y="28.05" transform="rotate(0)"><tspan id="SvgjsTspan2328" dy="16" x="10"><tspan id="SvgjsTspan2329" style="text-decoration:;">1、使用表达式执行力（根据表达式分析执行命令）：</tspan></tspan><tspan id="SvgjsTspan2330" dy="16" x="10"><tspan id="SvgjsTspan2331" style="text-decoration:;fill: rgb(0, 153, 153);">var user = new UserRepository();</tspan></tspan><tspan id="SvgjsTspan2332" dy="16" x="10"><tspan id="SvgjsTspan2333" style="text-decoration:;fill: rgb(0, 153, 153);">var result =</tspan><tspan id="SvgjsTspan2334" style="text-decoration:;fill: rgb(0, 153, 0);"> user.AsExecuteable()</tspan></tspan><tspan id="SvgjsTspan2335" dy="16" x="10"><tspan id="SvgjsTspan2336" style="text-decoration:;fill: rgb(0, 153, 0);">.Where(x =&gt; x.Account.Contains("admin"))</tspan></tspan><tspan id="SvgjsTspan2337" dy="16" x="10"><tspan id="SvgjsTspan2338" style="text-decoration:;fill: rgb(0, 153, 0);">.Update(x =&gt; new Domain.Entities.User {</tspan></tspan><tspan id="SvgjsTspan2339" dy="16" x="10"><tspan id="SvgjsTspan2340" style="text-decoration:;fill: rgb(0, 153, 0);">    Account = "admin"</tspan></tspan><tspan id="SvgjsTspan2341" dy="16" x="10"><tspan id="SvgjsTspan2342" style="text-decoration:;fill: rgb(0, 153, 0);">});</tspan></tspan><tspan id="SvgjsTspan2343" dy="16" x="10"><tspan id="SvgjsTspan2344" style="text-decoration:;">2、使用数据执行力（根据实体信息分析执行命令）：</tspan></tspan><tspan id="SvgjsTspan2345" dy="16" x="10"><tspan id="SvgjsTspan2346" style="text-decoration:;fill: rgb(0, 153, 153);">var entry = new User{ Id=100,Name="tinylit" };</tspan></tspan><tspan id="SvgjsTspan2347" dy="16" x="10"><tspan id="SvgjsTspan2348" style="text-decoration:;fill: rgb(0, 153, 153);">var user = new UserRepository();</tspan></tspan><tspan id="SvgjsTspan2349" dy="16" x="10"><tspan id="SvgjsTspan2350" style="text-decoration:;fill: rgb(0, 153, 153);">var result =</tspan><tspan id="SvgjsTspan2351" style="text-decoration:;fill: rgb(0, 153, 0);"> user.AsUpdateable(entry)</tspan></tspan><tspan id="SvgjsTspan2352" dy="16" x="10"><tspan id="SvgjsTspan2353" style="text-decoration:;font-size: inherit;fill: rgb(0, 153, 0);">.Where(x =&gt;x.Id)</tspan><tspan id="SvgjsTspan2354" style="text-decoration:;fill: rgb(0, 153, 0);">.Limit(x =&gt; x.Name)</tspan></tspan><tspan id="SvgjsTspan2355" dy="16" x="10"><tspan id="SvgjsTspan2356" style="text-decoration:;fill: rgb(0, 153, 0);">.Except(x =&gt; x.Id)</tspan></tspan><tspan id="SvgjsTspan2357" dy="16" x="10"><tspan id="SvgjsTspan2358" style="text-decoration:;fill: rgb(0, 153, 0);">.ExecuteCommand();</tspan></tspan></text></g><g id="SvgjsG2359"><text id="SvgjsText2360" font-family="微软雅黑" text-anchor="start" font-size="13px" width="26" fill="#323232" font-weight="400" align="middle" anchor="start" family="微软雅黑" size="13px" weight="400" font-style="" y="2.55" transform="rotate(0)"><tspan id="SvgjsTspan2361" dy="16" x="10"><tspan id="SvgjsTspan2362" style="text-decoration:;fill: rgb(0, 153, 77);">语法</tspan></tspan></text></g></g><g id="SvgjsG2363"><path id="SvgjsPath2364" d="M713 686L647.75 686L647.75 686L584.5 686" stroke="#ff0080" stroke-width="2" fill="none" marker-end="url(#SvgjsMarker2365)"></path></g><g id="SvgjsG2367"><path id="SvgjsPath2368" d="M355 246L355 177.5L355 177.5L355 109" stroke="#ff99cc" stroke-width="2" fill="none" marker-end="url(#SvgjsMarker2369)"></path></g><g id="SvgjsG2371" transform="translate(629,383)"><path id="SvgjsPath2372" d="M 18 0Q 9 0 9 6.800000000000001L 9 27.2Q 9 34 0 34Q 9 34 9 40.8L 9 61.2Q 9 68 18 68" stroke="#323232" stroke-width="2" fill="none"></path><path id="SvgjsPath2373" d="M 127 68Q 136 68 136 61.2L 136 40.8Q 136 34 145 34Q 136 34 136 27.2L 136 6.800000000000001Q 136 0 127 0" stroke="#323232" stroke-width="2" fill="none"></path><path id="SvgjsPath2374" d="M 0 0L 145 0L 145 68L 0 68Z" stroke="none" fill="none"></path><g id="SvgjsG2375"><text id="SvgjsText2376" font-family="微软雅黑" text-anchor="middle" font-size="13px" width="99" fill="#323232" font-weight="400" align="middle" anchor="middle" family="微软雅黑" size="13px" weight="400" font-style="" y="24.05" transform="rotate(0)"><tspan id="SvgjsTspan2377" dy="16" x="72.5"><tspan id="SvgjsTspan2378" style="text-decoration:;fill: rgb(0, 76, 153);">TEntity</tspan><tspan id="SvgjsTspan2379" style="text-decoration:;"> is </tspan><tspan id="SvgjsTspan2380" style="text-decoration:;fill: rgb(0, 102, 0);">IEntity</tspan></tspan></text></g></g><g id="SvgjsG2381" transform="translate(842,375)"><path id="SvgjsPath2382" d="M 18 0Q 9 0 9 8.4L 9 33.6Q 9 42 0 42Q 9 42 9 50.4L 9 75.6Q 9 84 18 84" stroke="#323232" stroke-width="2" fill="none"></path><path id="SvgjsPath2383" d="M 260 84Q 269 84 269 75.6L 269 50.4Q 269 42 278 42Q 269 42 269 33.6L 269 8.4Q 269 0 260 0" stroke="#323232" stroke-width="2" fill="none"></path><path id="SvgjsPath2384" d="M 0 0L 278 0L 278 84L 0 84Z" stroke="none" fill="none"></path><g id="SvgjsG2385"><text id="SvgjsText2386" font-family="微软雅黑" text-anchor="middle" font-size="13px" width="200" fill="#323232" font-weight="400" align="middle" anchor="middle" family="微软雅黑" size="13px" weight="400" font-style="" y="32.05" transform="rotate(0)"><tspan id="SvgjsTspan2387" dy="16" x="139"><tspan id="SvgjsTspan2388" style="text-decoration:;fill: rgb(0, 76, 153);">TProvider</tspan><tspan id="SvgjsTspan2389" style="text-decoration:;"> is </tspan><tspan id="SvgjsTspan2390" style="text-decoration:;fill: rgb(0, 51, 0);">RepositoryProvider</tspan><tspan id="SvgjsTspan2391" style="text-decoration:;"> </tspan></tspan></text></g></g></svg>