![SkyBuilding](https://raw.githubusercontent.com/tinylit/skybuilding/master/skybuilding.png '')
【天空之城】一款轻量高效的基础框架（包含类型转换、复制、映射，以及ORM支持）。

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

### Performance comparison.
Function|Third-party libraries|Performance improvement|Explain
:--:|---|:--:|---
MapTo|AutoMapper|25%↑|The framework is designed based on the target type. Only for simple mapping relationship, need to establish a complex mapping relationship, please use AutoMapper.
ORM|SqlSugar|15%↑|The framework supports native linq queries, dual mode batch processing is supported.
ORM|Dapper|15%↑|Only for this ORM project customized, if you need to use a particularly complex operation, please use Dapper.