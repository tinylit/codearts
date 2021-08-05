![CodeArts](http://oss.jschar.com/codearts.png 'Logo')

### "CodeArts.Caching"是什么？

CodeArts.Caching 是缓存操作框架，包含：设置、移除、存在判定、设置过期、清空槽以及事务锁相关功能。

#### 使用方式：

* 定义缓存槽实现。

  ```c#
  public class MemoryCaching : BaseCaching {
      private readonly string _region;
      public MemoryCaching(string name = "default") { // 槽。
          _region = name;
      }
      /// <summary> 缓存区域。 </summary>
      public override string Region => _region;
      // 实现抽象方法。
  }
  ```

  

* 定义缓存提供者。

  ```c#
  public class MemoryCachingProvider : ICachingProvider {
       public ICaching GetCache(string regionName) => new MemoryCaching(regionName);
  }
  ```

  

* 注入不同缓存级别的缓存提供者。

  ``` c#
  CachingManager.RegisterProvider(new MemoryCachingProvider())
  ```

  

* 获取缓存槽。

  ```c#
  ICaching caching = CachingManager.GetCache("solt");
  ```

#### 默认实现的缓存提供者：

* 内存缓存：[CodeArts.MemoryCaching](https://www.nuget.org/packages/CodeArts.MemoryCaching/)
* 分布式缓存：[CodeArts.RedisCaching](https://www.nuget.org/packages/CodeArts.RedisCaching/)

---

##### 说明：

* 设置供应器时，缓存级别只能是单个缓存级别。
* 获取缓存槽时：
  - 优先找支持的缓存级别，若指定缓存级别不存在，会依次找更低支持的缓存级别。如：需要二级缓存，若未设置二级缓存支持，会自动降级，获取一级缓存支持。
  - 可获取多个缓存级别的缓存槽。获取操作，任意级别缓存槽有数据即可；设置操作，所有级别的缓存槽都会被设置。