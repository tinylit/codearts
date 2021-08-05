![CodeArts](http://oss.jschar.com/codearts.png 'Logo')

### "CodeArts.Db"是什么？

CodeArts.Db 是包含数据库连接池、事务池、以及SQL分析器的高扩展性轻量级框架。

#### 使用方式：

* 连接池。

  ```c#
      /// <summary>
      /// 数据连接工程。
      /// </summary>
      public interface IDbConnectionFactory
      {
          /// <summary> 创建数据库连接。 </summary>
          /// <returns></returns>
          IDbConnection Create(string connectionString);
  
          /// <summary>
          /// 连接类型。
          /// </summary>
          Type DbConnectionType { get; }
  
          /// <summary>
          /// 供应器名称。
          /// </summary>
          string ProviderName { get; }
  
          /// <summary>
          /// 链接心跳（链接可以在心跳活动时间内被重用不需要重新分配链接，单位：分钟）。
          /// </summary>
          double ConnectionHeartbeat { get; }
  
          /// <summary>
          /// 线程池数量。
          /// </summary>
          int MaxPoolSize { get; }
      }
  ```

  - 获取链接。

    ```c#
    IDbConnection connection = DispatchConnections.GetConnection(string,IDbConnectionFactory[,bool]);
    ```

  - 生命周期：连接最后一次使用时间超过心跳时间，连接会自动关闭和释放。

* 事务池。

  - 获取链接。

    ```c#
    IDbConnection connection = TransactionConnections.GetConnection(string,IDbConnectionFactory);
    ```

  - 生命周期：`Transaction`提交或释放。

* 数据库实体基础接口：`IEntiy`,`IEntiy<TKey>`。

* 数据库实体基础类：`BaseEntity`,`BaseEntity<TKey>`。

* SQL。

  ```c#
   /// <summary>
   /// 格式化。
   /// </summary>
   public interface IFormatter
   {
       /// <summary>
       /// 表达式。
       /// </summary>
       Regex RegularExpression { get; }
  
       /// <summary>
       /// 替换内容。
       /// </summary>
       /// <param name="match">匹配到的内容。</param>
       /// <returns></returns>
       string Evaluator(Match match);
   }
  ```

  * RegularExpression：正则表达式。

  * Evaluator：格式化正则表达式匹配的内容。

  * 建议继承`AdapterFormatter<T>`抽象接口。

    - 抽象类将自动提取`?<parameterName>`作为方法的参数。
      - Match：匹配到的内容（名称不限）。
      - Group：组名称和参数名称相同的内容组（成功匹配）。
      - String：组名称和参数名称相同的内容（成功匹配）。
      - Boolean：组名称和参数名称相同是否成功匹配。

    - 例如：

      ```c#
          /// <summary>
      /// Drop 命令。
      /// </summary>
      public class DropIfFormatter : AdapterFormatter<DropIfFormatter>, IFormatter
      {
          private static readonly Regex PatternDropIf = new Regex(@"\bdrop[\x20\t\r\n\f]+(?<command>table|view|function|procedure|database)[\x20\t\r\n\f]+(?<if>if[\x20\t\r\n\f]+exists[\x20\t\r\n\f]+)([\[\w\]]+\.)*\[(?<name>\w+)\][\x20\t\r\n\f]*;?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
          /// <summary>
          /// 构造函数。
          /// </summary>
          public DropIfFormatter() : base(PatternDropIf)
          {
          }
      
          /// <summary>
          /// 条件删除。
          /// </summary>
          /// <param name="item">匹配内容。</param>
          /// <param name="command">命令。</param>
          /// <param name="if">条件。</param>
          /// <param name="name">表或视图名称。</param>
          /// <returns></returns>
          public string DropIf(Match item, string command, Group @if, string name)
          {
              var value = item.Value;
      
              var sb = new StringBuilder();
      
              switch (command.ToUpper())
              {
                  case "TABLE":
                      sb.AppendFormat("IF EXIXSTS(SELECT * FROM [sysobjects] WHERE [xtype]='U' and [name] ='{0}')", name);
                      break;
                  case "VIEW":
                      sb.AppendFormat("IF EXIXSTS(SELECT * FROM [sysobjects] WHERE [xtype]='V' and [name] ='{0}')", name);
                      break;
                  case "FUNCTION":
                      sb.AppendFormat("IF EXIXSTS(SELECT * FROM [sysobjects] WHERE [xtype] IN('FN', 'IF', 'TF') AND [name] ='{0}')", name);
                      break;
                  case "PROCEDURE":
                      sb.AppendFormat("IF EXIXSTS(SELECT * FROM [sysobjects] WHERE [xtype]='P' AND [name] ='{0}')", name);
                      break;
                  case "DATABASE":
                      sb.AppendFormat("IF EXIXSTS(SELECT * FROM [sys].[databases] WHERE [name]='{0}')", name);
                      break;
                  default:
                      throw new NotSupportedException();
              }
      
              return sb.Append(" BEGIN")
                .AppendLine()
                .Append(value.Substring(0, @if.Index - item.Index))
                .Append(value.Substring(@if.Index - item.Index + @if.Length))
                .AppendLine()
                .Append("END GO")
                .AppendLine()
                .ToString();
          }
      }
      ```

  ##### SQL矫正器。

  ```c#
      /// <summary>
      /// SQL矫正设置。
  /// </summary>
  public interface ISQLCorrectSettings
  {
      /// <summary>
      /// 数据库引擎。
      /// </summary>
      DatabaseEngine Engine { get; }
  
      /// <summary>
      /// 字符串截取。
      /// </summary>
      string Substring { get; }
  
      /// <summary>
      /// 索引位置。
      /// </summary>
      string IndexOf { get; }
  
      /// <summary>
      /// 长度测量器。
      /// </summary>
      string Length { get; }
  
      /// <summary>
      /// 索引交换位置（默认：value.indexOf("x") => IndexOf(value,"x")）。
      /// </summary>
      bool IndexOfSwapPlaces { get; }
  
      /// <summary>
      /// 格式化集合（用作“<see cref="SQL.ToString(ISQLCorrectSettings)"/>”矫正SQL语句使用）。
      /// </summary>
      ICollection<IFormatter> Formatters { get; }
  
      /// <summary>
      /// 名称。
      /// </summary>
      /// <param name="name">名称。</param>
      /// <returns></returns>
      string Name(string name);
  
      /// <summary>
      /// 参数名称。
      /// </summary>
      /// <param name="name">名称。</param>
      /// <returns></returns>
      string ParamterName(string name);
  
      /// <summary>
      /// SQL(分页)。
      /// </summary>
      /// <param name="sql">SQL。</param>
      /// <param name="take">获取“<paramref name="take"/>”条数据。</param>
      /// <param name="skip">跳过“<paramref name="skip"/>”条数据。</param>
      /// <param name="orderBy">排序。</param>
      /// <returns></returns>
      string ToSQL(string sql, int take, int skip, string orderBy);
  }
  ```

  * Engine：数据库引擎（Normal、Oracle、MySQL、SqlServer、PostgreSQL、DB2、Sybase、Access、SQLite）。
  * Substring：字符串截取函数名称。
  * IndexOf：索引位置，`IndexOfSwapPlaces`值和查找字符位置交替。
  * Formatters：自定义SQL格式化器。
  * Name：字段名称、表名称。
  * ParamterName：参数名称。
  * ToSQL：SQL分页。

  ##### SQL 适配器。

  ```c#
  /// <summary>
  /// SQL 适配器。
  /// </summary>
  public interface ISqlAdpter
  {
      /// <summary>
      /// SQL 分析。
      /// </summary>
      /// <param name="sql">语句。</param>
      /// <returns></returns>
      string Analyze(string sql);
  
      /// <summary>
      /// SQL 分析（表名称）。
      /// </summary>
      /// <param name="sql">来源于【<see cref="Analyze(string)"/>】的结果。</param>
      /// <returns></returns>
      IReadOnlyCollection<TableToken> AnalyzeTables(string sql);
  
      /// <summary>
      /// SQL 分析（参数）。
      /// </summary>
      /// <param name="sql">来源于【<see cref="Analyze(string)"/>】的结果。</param>
      /// <returns></returns>
      IReadOnlyCollection<string> AnalyzeParameters(string sql);
  
      /// <summary>
      /// 获取符合条件的条数。
      /// </summary>
      /// <param name="sql">SQL</param>
      /// <example>SELECT * FROM Users WHERE Id > 100 => SELECT COUNT(1) FROM Users WHERE Id > 100</example>
      /// <example>SELECT * FROM Users WHERE Id > 100 ORDER BY Id DESC => SELECT COUNT(1) FROM Users WHERE Id > 100</example>
      /// <returns></returns>
      string ToCountSQL(string sql);
  
      /// <summary>
      /// 生成分页SQL。
      /// </summary>
      /// <param name="sql">SQL</param>
      /// <param name="pageIndex">页码（从“0”开始）</param>
      /// <param name="pageSize">分页条数</param>
      /// <example>SELECT * FROM Users WHERE Id > 100 => PAGING(`SELECT * FROM Users WHERE Id > 100`,<paramref name="pageIndex"/>,<paramref name="pageSize"/>)</example>
      /// <example>SELECT * FROM Users WHERE Id > 100 ORDER BY Id DESC => PAGING(`SELECT * FROM Users WHERE Id > 100`,<paramref name="pageIndex"/>,<paramref name="pageSize"/>,`ORDER BY Id DESC`)</example>
      /// <returns></returns>
      string ToSQL(string sql, int pageIndex, int pageSize);
  
      /// <summary>
      /// SQL 格式化（格式化为数据库可执行的语句）。
      /// </summary>
      /// <param name="sql">语句。</param>
      /// <returns></returns>
      string Format(string sql);
  
      /// <summary>
      /// SQL 格式化（格式化为数据库可执行的语句）。
      /// </summary>
      /// <param name="sql">语句。</param>
      /// <param name="settings">配置。</param>
      /// <returns></returns>
      string Format(string sql, ISQLCorrectSettings settings);
  }
  ```

  - Analyze
    * 分析表以及表的操作指令（如：SELECT、INSERT、UPDATE、DELETE等）。
    * 分析字段名称或字段别名。
    * 分析参数。

  * AnalyzeTables：分析表信息。
  * AnalyzeParameters：分析参数信息。
  * ToCountSQL：将SQL转为获取条数的SQL语句。
  * ToSQL：将SQL转为分页SQL语句。
  * Format：将SQL转成指定数据库支持的语句，默认：SqlServer。

  ##### SQL 核心。

  ```c#
  /// <summary>
  /// SQL 默认语法：
  ///     表名称：[yep_users]
  ///     名称：[name]
  ///     参数名称:{name}
  ///     条件移除：DROP TABLE IF EXIXSTS [yep_users];
  ///     条件创建：CREATE TABLE IF NOT EXIXSTS [yep_users] ([Id] int not null,[name] varchar(100));
  /// 说明：会自动去除代码注解和多余的换行符压缩语句。
  /// </summary>
  [DebuggerDisplay("{ToString()}")]
  public sealed class SQL
  {
      private readonly string sql;
      private static readonly ISqlAdpter adpter;
  
      /// <summary>
      /// 静态构造函数。
      /// </summary>
      static SQL() => adpter = RuntimeServPools.Singleton<ISqlAdpter, DefaultSqlAdpter>();
  
      /// <summary>
      /// 构造函数。
      /// </summary>
      /// <param name="sql">原始SQL语句。</param>
      public SQL(string sql) => this.sql = adpter.Analyze(sql);
  
      /// <summary>
      /// 添加语句。
      /// </summary>
      /// <param name="sql">SQL。</param>
      /// <returns></returns>
      public SQL Add(string sql) => new SQL(string.Concat(ToString(), ";", sql));
  
      /// <summary>
      /// 添加语句。
      /// </summary>
      /// <param name="sql">SQL。</param>
      public SQL Add(SQL sql) => new SQL(string.Concat(ToString(), ";", sql.ToString()));
  
      private IReadOnlyCollection<TableToken> tables;
      private IReadOnlyCollection<string> parameters;
  
      /// <summary>
      /// 操作的表。
      /// </summary>
      public IReadOnlyCollection<TableToken> Tables => tables ??= adpter.AnalyzeTables(sql);
  
      /// <summary>
      /// 参数。
      /// </summary>
      public IReadOnlyCollection<string> Parameters => parameters ??= adpter.AnalyzeParameters(sql);
  
      /// <summary>
      /// 获取总行数。
      /// </summary>
      /// <returns></returns>
      public SQL ToCountSQL() => new SQL(adpter.ToCountSQL(sql));
  
      /// <summary>
      /// 获取分页数据。
      /// </summary>
      /// <param name="pageIndex">页码（从“0”开始）</param>
      /// <param name="pageSize">分页条数。</param>
      /// <returns></returns>
      public SQL ToSQL(int pageIndex, int pageSize) => new SQL(adpter.ToSQL(sql, pageIndex, pageSize));
  
      /// <summary>
      /// 转为实际数据库的SQL语句。
      /// </summary>
      /// <param name="settings">SQL修正配置。</param>
      /// <returns></returns>
      public string ToString(ISQLCorrectSettings settings) => adpter.Format(sql, settings);
  
      /// <summary>
      /// 返回分析的SQL结果。
      /// </summary>
      /// <returns></returns>
      public override string ToString() => adpter.Format(sql);
  
      /// <summary>
      /// 追加sql。
      /// </summary>
      /// <param name="left">原SQL。</param>
      /// <param name="right">需要追加的SQL。</param>
      /// <returns></returns>
      public static SQL operator +(SQL left, SQL right)
      {
          if (left is null || right is null)
          {
              return right ?? left;
          }
  
          return left.Add(right);
      }
  
      /// <summary>
      /// 运算符。
      /// </summary>
      /// <param name="sql">SQL。</param>
      public static implicit operator SQL(string sql) => new SQL(sql);
  }
  ```

#### 说明：

* 自动清除多余的空格或换行。
* 自动清除注解。
* 不会格式化字符串内容的格式。
* 单个分页语句支持重复分页，不支持多个分页语句重复分页。
* 单条语句可转为统计条数的语句。
* 参数标准名称：{name}，同时还支持：
  - @name：SqlServer。
  - ?name：MySQL。
  - :name：Oracle。