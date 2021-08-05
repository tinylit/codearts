![CodeArts](http://oss.jschar.com/codearts.png 'Logo')

### "CodeArts.Casting"是什么？

CodeArts.Casting 是运行时类型操作框架，包含：类型转换、实体复制、实体映射相关功能。

#### 使用方式：

* 类型转换（值类型、基元类型、字符串以及系统集合类型的相互装换）。

  ```c#
  Mapper.Cast<Guid>("0bbd0503-4879-42de-8cf0-666537b642e2"); // 安全转换，转换失败时，返回默认值。
  Mapper.ThrowsCast<Guid>("0bbd0503-4879-42de-8cf0-666537b642e2"); // 不安全转换，不能装换时，抛出异常。
  ```

  

* 实体复制（相同类型，或目标类型是源类型的基础接口或父类）。

  ```c#
  public class CopyTest
  {
      public int Id { get; set; }
      public string Name { get; set; }
  }
  public class CopyToTest : CopyTest
  {
      public DateTime Date { get; set; }
  }
  
  var value = new CopyToTest
  {
      Id = 1000,
      Name = "test",
      Date = DateTime.Now
  };
  
  // 同类型复制。
  var copy1 = Mapper.Copy(value);
  // 父类型复制。
  var copy2 = Mapper.Copy<CopyTest>(value);
  ```

  

* 实体映射（任意类型之间的转换）。

  ``` c#
  public class MapTest
  {
      public int Id { get; set; }
      public string Name { get; set; }
  }
  public class MapToTest
  {
      public int Id { get; set; }
      public string Name { get; set; }
      public DateTime Date { get; set; }
  }
  
  var value = new MapToTest
  {
      Id = 1000,
      Name = "test",
      Date = DateTime.Now
  };
  // 不同类型的映射。
  var map1 = Mapper.Map<MapTest>(value);
  // 相同类型映射（复制实体）；
  var map2 = Mapper.Map<MapToTest>(value);
  ```

#### 修正配置：

+ 契约。

  ```c#
  using System;
  
  namespace CodeArts.Casting
  {
      /// <summary>
      /// 拷贝配置。
      /// </summary>
      public interface IProfileConfiguration
      {
          /// <summary>
          /// 类型创建器。
          /// </summary>
          Func<Type, object> ServiceCtor { get; }
  
          /// <summary>
          /// 匹配模式。
          /// </summary>
          PatternKind Kind { get; }
  
          /// <summary>
          /// 深度映射。
          /// </summary>
          bool? IsDepthMapping { get; }
  
          /// <summary>
          /// 允许空目标值。
          /// </summary>
          bool? AllowNullDestinationValues { get; }
  
          /// <summary>
          /// 允许空值传播映射。
          /// </summary>
          bool? AllowNullPropagationMapping { get; }
      }
  }
  ```

+ 说明。

  - ServiceCtor：创建指定类型的实例。
  - Kind：指定匹配模式，映射属性或字段。
  - IsDepthMapping：是否深度映射（仅对自定义引用类型有效）。
  - AllowNullDestinationValues：为false时，源值为null时，不做数据映射。
  - AllowNullPropagationMapping：源值为null时，为目标值设置默认空值（仅对目标类型是string或Version有效）。

#### 扩展支持：

* 指定类型代理（任意源类型装换到指定目标类型的代理，仅最后一次指定有效）。

  ```c#
  RuntimeServPools.Singleton<ICastToExpression, CastToExpression>()
      .Use((profile, type) =>
      {
          if (type == typeof(Guid)) // 需要特殊处理的类型。
          {
              return source =>
              {
                  var copy = (Guid)source;
                  return copy.ToString("N");
              };
          }
          return profile.Create<string>(type); // 使用默认处理方式。
      });
  ```

* 绝对类型代理（指定源类型到目标类型的代理）。

  ```c#
  RuntimeServPools.Singleton<ICastToExpression, CastToExpression>()
      .Absolute<Guid, string>(source => source.ToString("N"));
  ```

* 约束类型代理（指定类型或指定类型的子类到目标类型的代理）。

  ```c#
  RuntimeServPools.Singleton<IMapToExpression, MapToExpression>()
      .Run<IEnumerable<Guid>, List<Guid>>(source =>
      {
          return new List<Guid>(source);
      });
  ```

* 条件约束代理（验证源类型是否可以代理到目标类型，如果能则调用解决方案）。

  ```c#
  RuntimeServPools.Singleton<ICastToExpression, CastToExpression>()
      .Map(sourceType => sourceType.IsEnum, source => source.GetText());
  ```

---

##### 说明：

* 属性映射属性，字段映射字段，不支持交替映射。
* 映射优先级：
  - 相同名称。
  - 相同的别名（`NamingAttribute`标记的名称）。
  - 别名和名称相同。
* 指定类型代理优先于其它代理方式。