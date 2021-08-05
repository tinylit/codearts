![CodeArts](http://oss.jschar.com/codearts.png 'Logo')

### "CodeArts.Json"是什么？

CodeArts.Json 是实体JSON序列化和反序列化工具。

#### 使用方式：

* 序列化。

  ```c#
  var value = JsonHelper.ToJson(new { Id = Guid.NewGuid(), Timestamp = DateTime.Now });
  ```

* 反序列化。

  ```c#
  namespace CodeArts
  {
      /// <summary>
      /// 配置变更监听能力。
      /// </summary>
      public interface IConfigable<TSelf> where TSelf : class, IConfigable<TSelf>
      {
          /// <summary>
          /// 监听到变更后的新数据。
          /// </summary>
          /// <param name="changedValue">变更后的数据。</param>
          void SaveChanges(TSelf changedValue);
      }
  }
  
  using System;
  
  namespace CodeArts.Db
  {
      /// <summary>
      /// 数据库连接字符配置。
      /// </summary>
      public class ConnectionConfig : IConfigable<ConnectionConfig>
      {
          /// <summary> 连接名称。 </summary>
          public string Name { get; set; }
  
          /// <summary> 数据库驱动名称。 </summary>
          public string ProviderName { get; set; }
  
          /// <summary> 连接字符串。 </summary>
          public string ConnectionString { get; set; }
  
          /// <summary>
          /// 监听到变更后的新数据。
          /// </summary>
          /// <param name="changedValue">变更后的数据。</param>
          public void SaveChanges(ConnectionConfig changedValue)
          {
              if (changedValue is null)
              {
                  return;
              }
  
              Name = changedValue.Name;
              ProviderName = changedValue.ProviderName;
              ConnectionString = changedValue.ConnectionString;
          }
      }
  }
  
  var value = "connectionStrings:default".Config<ConnectionConfig>();
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

#### 接口契约：

```c#
    /// <summary>
    /// 配置文件帮助类。
    /// </summary>
    public interface IConfigHelper
    {
        ///<summary> 配置文件变更事件。</summary>
        event Action<object> OnConfigChanged;

        /// <summary>
        /// 配置文件读取。
        /// </summary>
        /// <typeparam name="T">读取数据类型。</typeparam>
        /// <param name="key">健。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns></returns>
        T Get<T>(string key, T defaultValue = default);
    }
```

##### 说明：

* .NET Framework。
  - 运行环境包括：Web、Form、Service。
  - 运行环境默认使用`Web`。
  - 层级分隔符：`/`。
  - 默认读取`appStrings`下的键值。
  - 读取数据库连接:`connectionStrings/key`。
  - 读取数据库连接的连接字符：`connectionStrings/key/connectionString`。
  - 读取自定义`ConfigurationSectionGroup`请提供准确的类型，否则强转失败，返回默认值。
* .NET Standard：
  - 层级分隔符：`:`。
  - 读取规则与`Microsoft.Extensions.Configuration`保持一致。