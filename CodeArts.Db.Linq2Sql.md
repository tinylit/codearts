![CodeArts](http://oss.jschar.com/codearts.png 'Logo')

### "CodeArts.Db.Linq2Sql"是什么？

CodeArts.Db.Linq2Sql 是基于Linq分析实现的、支持分表和读写分离的轻量级SQL转换器框架。

#### 使用方式：

* 定义数据库矫正配置。

  - 生成指定数据库支持的SQL语句。
  - 详情请查阅：[ISQLCorrectSettings](./CodeArts.Db.md)

* 定义表达式访问器。

  ```c#
  /// <summary>
  /// 自定义的访问器。
  /// </summary>
  public interface ICustomVisitor : IVisitor
  {
      /// <summary>
      /// 表达式分析。
      /// </summary>
      /// <param name="visitor">访问器。</param>
      /// <param name="writer">SQL写入器。</param>
      /// <param name="node">表达式。</param>
      void Visit(ExpressionVisitor visitor, Writer writer, MethodCallExpression node);
  }
  ```

  自定义单字段处理的SQL分析规则。

  例如：

  实现`System.Convert`转为`MySQL`数据库的类型转换函数。

  ```c#
  /// <summary>
  /// Convert扩展支持。
  /// </summary>
  public class ConvertVisitter : ICustomVisitor
  {
      /// <summary>
      /// 是否能解决。
      /// </summary>
      /// <param name="node">表达式。</param>
      /// <returns></returns>
      public bool CanResolve(MethodCallExpression node) => node.Arguments.Count == 1 && node.Method.DeclaringType == typeof(Convert);
  
      /// <summary>
      /// 分析。
      /// </summary>
      /// <param name="visitor">分析器。</param>
      /// <param name="writer">写入器。</param>
      /// <param name="node">表达式。</param>
      /// <returns></returns>
      public void Visit(ExpressionVisitor visitor, Writer writer, MethodCallExpression node)
      {
          if (node.Method.Name == "IsDBNull")
          {
              visitor.Visit(node.Arguments[0]);
  
              writer.IsNull();
  
              return;
          }
  
          writer.Write("CONVERT");
          writer.OpenBrace();
          visitor.Visit(node.Arguments[0]);
          writer.Delimiter();
  
          switch (node.Method.Name)
          {
              case "ToBoolean":
              case "ToByte":
              case "ToSByte":
              case "ToSingle":
              case "ToInt16":
              case "ToInt32":
              case "ToInt64":
                  writer.Write("SIGNED");
                  break;
              case "ToDouble":
              case "ToDecimal":
                  writer.Write("DECIMAL");
                  break;
              case "ToUInt16":
              case "ToUInt32":
              case "ToUInt64":
                  writer.Write("UNSIGNED");
                  break;
              case "ToChar":
                  writer.Write("CHAR(1)");
                  break;
              case "ToString":
                  writer.Write("CHAR");
                  break;
              case "ToDateTime":
                  writer.Write("DATETIME");
                  break;
              default:
                  throw new NotSupportedException();
          }
          writer.CloseBrace();
      }
  }
  ```

* 访问器。

  ```c#
  /// <summary>
  /// 启动访问器。
  /// </summary>
  public interface IStartupVisitor : IVisitor, IDisposable
  {
      /// <summary>
      /// 启动。
      /// </summary>
      /// <param name="node">分析表达式。</param>
      /// <returns></returns>
      void Startup(Expression node);
  
      /// <summary>
      /// 参数。
      /// </summary>
      Dictionary<string, object> Parameters { get; }
  
      /// <summary>
      /// SQL语句。
      /// </summary>
      /// <returns></returns>
      string ToSQL();
  }
  ```

  - 查询器。

    ```c#
    /// <summary>
    /// 查询访问器。
    /// </summary>
    public interface IQueryVisitor : IStartupVisitor
    {
        /// <summary>
        /// 是否必须有查询或执行结果。
        /// </summary>
        bool Required { get; }
    
        /// <summary>
        /// 包含默认值。
        /// </summary>
        bool HasDefaultValue { get; }
    
        /// <summary>
        /// 默认值。
        /// </summary>
        object DefaultValue { get; }
    
        /// <summary>
        /// 未查询到数据异常。
        /// </summary>
        string MissingDataError { get; }
    
        /// <summary>
        /// 获取或设置在终止尝试执行命令并生成错误之前的等待时间<see cref="System.Data.IDbCommand.CommandTimeout"/>。
        /// </summary>
        int? TimeOut { get; }
    }
    ```

    例如：

    ```c#
    IQueryVisitor visitor = new QueryVisitor({ISQLCorrectSettings}settings, {ICustomVisitorList}visitors);
    
    visitor.Startup({Expression}node); // 需要分析的表达式。
    
    Dictionary<string, object> parameters = visitor.Parameters; // 表达式中的参数。
    
    string sql = visitor.ToSQL(); // SQL。
    ```

  - 执行器。

    ```c#
    /// <summary>
    /// 执行能力访问器。
    /// </summary>
    public interface IExecuteVisitor : IStartupVisitor
    {
        /// <summary>
        /// 指令行为:Update/Insert/Delete。
        /// </summary>
        ActionBehavior Behavior { get; }
    
        /// <summary>
        /// 获取或设置在终止尝试执行命令并生成错误之前的等待时间。<see cref="System.Data.IDbCommand.CommandTimeout"/>
        /// </summary>
        int? TimeOut { get; }
    }
    ```

    例如：

    ```c#
    IExecuteVisitor visitor = new ExecuteVisitor({ISQLCorrectSettings}settings, {ICustomVisitorList}visitors);
    
    visitor.Startup({Expression}node); // 需要分析的表达式。
    
    Dictionary<string, object> parameters = visitor.Parameters; // 表达式中的参数。
    
    string sql = visitor.ToSQL(); // SQL。
    
    switch (visitor.Behavior)
    {
        case ActionBehavior.Update:
            // 更新语句。
            break;
        case ActionBehavior.Delete:
            // 删除语句。
            break;
        case ActionBehavior.Insert:
            // 插入语句。
            break;
        default:
            throw new NotSupportedException();
    }
    ```

#### 说明：

* 专注SQL语句优化，会损失极少量的站点服务器性能。
* 函数扩展。
  - `QueryableMethods.From`：可以指定操作表，数据库分表时或跨库操作时使用。
  - `QueryableMethods.TimeOut`：指令执行超时时间。
  - `QueryableMethods.NoResultError`：指定未查询到数据时的异常消息。
  - `QueryableMethods.Insert`：生成插入语句。
  - `QueryableMethods.Update`：生成更新语句。
  - `QueryableMethods.Delete`：生成删除语句。
  - `QueryableMethods.DeleteWithPredicate`：生成删除语句，与`QueryableMethods.Delete`作用相同。

* 由于“Take”和“Skip”参数的特殊性，在生成SQL语句时，将直接生成到SQL语句中，不会添加到参数字典中。

* 支持Linq表达式。

* 支持几乎所有的Linq使用场景(可能会有函数顺序上的优化，目的是让翻译出来的SQL更加简单高效)。

* 支持特殊函数扩展（自定义“ICustomVisitor”接口，添加到适配器中即可）。

* 易读性：参数名称、表别名，会优先使用客户定义的名称。

* 参数空值推测。

  - 当条件两端，一端为可空类型，且另一端为非可空类型时，若可空参数为`null`，则忽略当前条件。

* 布尔推测。

  - 三目运算：A ? B : C
    - 当条件 A 中不包含数据库字段时，A 为`true`，解析B，否则解析C。

  - 且运算：A && B
    - 当条件 A 中不包含数据库字段时，A 为`true`，解析B，否则忽略B表达式。

  - 或运算：A || B
    - 当条件 A 中不包含数据库字段时，A 为`false`，解析B，否则忽略B表达式。

* 条件中，符号扁平化处理，使其容易命中索引。

  - 可空合并。
    + WHERE A ?? B => WHERE (A IS NOT NULL AND A = 1) OR (A IS NULL AND B = 1).
    + WHERE A==(B??C) => WHERE (B IS NOT NULL AND A = B) OR (B IS NULL AND A = C)
  - 三目运算。
    - WHERE A ? B : C => WHERE (A = 1 AND B = 1) OR (A <> 1 AND C = 1)
    - WHERE A ==(B?C:D) => WHERE (B = 1 AND A = C) OR (B <> 1 AND A = D)

* 支持大多数常见的字符串属性和函数，以及可空的类型支持。有关详细信息，请参阅单元测试。

  - Unit testing.
    + [SqlServer](https://github.com/tinylit/codearts/blob/master/Tests/CodeArts.Db.Tests/SqlServerTest.cs)
    + [MySQL](https://github.com/tinylit/codearts/blob/master/Tests/CodeArts.Db.Tests/MySqlTest.cs)