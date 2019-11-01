using SkyBuilding.ORM.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 天空供应
    /// </summary>
    public class SkyProvider : RepositoryProvider
    {
        private static readonly Dictionary<Type, DbType> typeMap;
        public SkyProvider(ISQLCorrectSettings settings) : base(settings)
        {
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

        private static void AddParameterAuto(IDbCommand command, Dictionary<string, object> parameters)
        {
            if (parameters is null)
                return;

            foreach (var kv in parameters)
            {
                AddParameterAuto(command, kv.Key, kv.Value);
            }
        }

        private static void AddParameterAuto(IDbCommand command, string key, object value)
        {
            var dbParameter = command.CreateParameter();

            dbParameter.Value = value ?? DBNull.Value;
            dbParameter.ParameterName = key;
            dbParameter.Direction = ParameterDirection.Input;
            dbParameter.DbType = value == null ? DbType.Object : LookupDbType(value.GetType());

            command.Parameters.Add(dbParameter);
        }

        public override int Execute(IDbConnection conn, string sql, Dictionary<string, object> parameters = null)
        {
            conn.Open();

            using (var command = conn.CreateCommand())
            {
                command.CommandText = sql;

                AddParameterAuto(command, parameters);

                return command.ExecuteNonQuery();
            }
        }
        public override IEnumerable<T> Query<T>(IDbConnection conn, string sql, Dictionary<string, object> parameters = null)
        {
            conn.Open();

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

        public override T One<T>(IDbConnection conn, string sql, Dictionary<string, object> parameters = null, bool reqiured = false, T defaultValue = default)
        {
            conn.Open();

            using (var command = conn.CreateCommand())
            {
                command.CommandText = sql;

                AddParameterAuto(command, parameters);

                var value = defaultValue;

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
                return value;
            }
        }
    }
}
