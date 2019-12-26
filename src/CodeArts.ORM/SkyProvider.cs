using CodeArts.ORM.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;

namespace CodeArts.ORM
{
    /// <summary>
    /// 天空供应
    /// </summary>
    public class SkyProvider : RepositoryProvider
    {
        private readonly ISQLCorrectSettings settings;
        private static readonly Dictionary<Type, DbType> typeMap;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="settings">SQL矫正配置</param>
        public SkyProvider(ISQLCorrectSettings settings) : base(settings)
        {
            this.settings = settings;
        }

        static SkyProvider()
        {
            typeMap = new Dictionary<Type, DbType>
            {
                [typeof(byte)] = DbType.Byte,
                [typeof(sbyte)] = DbType.SByte,
                [typeof(short)] = DbType.Int16,
                [typeof(ushort)] = DbType.UInt16,
                [typeof(int)] = DbType.Int32,
                [typeof(uint)] = DbType.UInt32,
                [typeof(long)] = DbType.Int64,
                [typeof(ulong)] = DbType.UInt64,
                [typeof(float)] = DbType.Single,
                [typeof(double)] = DbType.Double,
                [typeof(decimal)] = DbType.Decimal,
                [typeof(bool)] = DbType.Boolean,
                [typeof(string)] = DbType.String,
                [typeof(char)] = DbType.StringFixedLength,
                [typeof(Guid)] = DbType.Guid,
                [typeof(DateTime)] = DbType.DateTime,
                [typeof(DateTimeOffset)] = DbType.DateTimeOffset,
                [typeof(TimeSpan)] = DbType.Time,
                [typeof(byte[])] = DbType.Binary,
                [typeof(object)] = DbType.Object
            };
        }

        private static DbType LookupDbType(Type dataType)
        {
            if (dataType.IsEnum)
            {
                dataType = Enum.GetUnderlyingType(dataType);
            }
            else if (dataType.IsNullable())
            {
                dataType = Nullable.GetUnderlyingType(dataType);
            }

            if (typeMap.TryGetValue(dataType, out DbType dbType))
                return dbType;

            if (dataType.FullName == "System.Data.Linq.Binary")
            {
                return DbType.Binary;
            }

            return DbType.Object;
        }

        private void AddParameterAuto(IDbCommand command, Dictionary<string, object> parameters)
        {
            if (parameters is null || parameters.Count == 0)
                return;

            foreach (var kv in parameters)
            {
                AddParameterAuto(command, kv.Key, kv.Value);
            }
        }

        private void AddParameterAuto(IDbCommand command, string key, object value)
        {
            if (key[0] == '@' || key[0] == '?' || key[0] == ':')
            {
                key = key.Substring(1);
            }

            var dbParameter = command.CreateParameter();

            dbParameter.Value = value ?? DBNull.Value;
            dbParameter.ParameterName = settings.ParamterName(key);
            dbParameter.Direction = ParameterDirection.Input;
            dbParameter.DbType = value == null ? DbType.Object : LookupDbType(value.GetType());

            command.Parameters.Add(dbParameter);
        }

        /// <summary>
        /// 执行SQL。
        /// </summary>
        /// <param name="conn">数据库连接</param>
        /// <param name="sql">SQL</param>
        /// <param name="parameters">参数</param>
        /// <returns></returns>
        public override int Execute(IDbConnection conn, string sql, Dictionary<string, object> parameters = null)
        {
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            try
            {
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = sql;

                    AddParameterAuto(command, parameters);

                    return command.ExecuteNonQuery();
                }
            }
            finally
            {
                conn.Close();
            }
        }

        /// <summary>
        /// 查询。
        /// </summary>
        /// <typeparam name="T">结果类型</typeparam>
        /// <param name="conn">数据库连接</param>
        /// <param name="sql">SQL</param>
        /// <param name="parameters">参数</param>
        /// <returns></returns>
        public override IEnumerable<T> Query<T>(IDbConnection conn, string sql, Dictionary<string, object> parameters = null)
        {
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            try
            {
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = sql;

                    AddParameterAuto(command, parameters);

                    using (var dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            yield return dr.MapTo<T>();
                        }

                        while (dr.NextResult()) { /* ignore subsequent result sets */ }

                        dr.Close();
                    }
                }
            }
            finally
            {
                conn.Close();
            }
        }

        /// <summary>
        /// 查询。
        /// </summary>
        /// <typeparam name="T">结果类型</typeparam>
        /// <param name="conn">数据库连接</param>
        /// <param name="sql">SQL</param>
        /// <param name="parameters">参数</param>
        /// <param name="reqiured">是否必须。</param>
        /// <param name="defaultValue">默认</param>
        /// <exception cref="DRequiredException">必须且数据库未查询到数据</exception>
        /// <returns></returns>
        public override T QueryFirst<T>(IDbConnection conn, string sql, Dictionary<string, object> parameters = null, bool reqiured = false, T defaultValue = default)
        {
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            var value = defaultValue;

            try
            {
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = sql;

                    AddParameterAuto(command, parameters);

                    using (var dr = command.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            value = dr.MapTo<T>();
                        }
                        else if (reqiured)
                        {
                            throw new DRequiredException();
                        }

                        while (dr.NextResult()) { /* ignore subsequent result sets */ }

                        dr.Close();
                    }
                }
            }
            finally
            {
                conn.Close();
            }

            return value;
        }
    }
}
