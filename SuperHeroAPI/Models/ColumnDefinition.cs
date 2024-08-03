using Npgsql;
using NpgsqlTypes;
using System.Data;

namespace SuperHeroAPI.Models
{
    public class ColumnDefinition
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public DbType DbType
        {
            get
            {
                NpgsqlParameter parameter = new NpgsqlParameter();
                // Assuming 'DataType' holds the PostgreSQL data type name
                switch (DataType.ToLower())
                {
                    case "integer":
                    case "serial":
                        parameter.NpgsqlDbType = NpgsqlDbType.Integer;
                        break;
                    case "varchar":
                    case "text":
                        parameter.NpgsqlDbType = NpgsqlDbType.Varchar;
                        break;
                    case "numeric":
                        parameter.NpgsqlDbType = NpgsqlDbType.Numeric;
                        break;
                    case "date":
                        parameter.NpgsqlDbType = NpgsqlDbType.Date;
                        break;
                    case "character varying":
                        parameter.NpgsqlDbType = NpgsqlDbType.Varchar;
                        break;
                    default:
                        throw new ArgumentException($"Unsupported data type: {DataType}");
                }
                return parameter.DbType;
            }
        }
    }
}
