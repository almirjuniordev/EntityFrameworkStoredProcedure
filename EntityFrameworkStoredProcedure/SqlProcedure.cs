using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace EntityFrameworkStoredProcedure
{
    public class SqlProcedure<T1, T2> : ISqlProcedure<T2> where T1 : DbContext where T2 : class
    {
        public string procedure { get; set; }

        public IList<object> parameters { get; set; }

        protected T1 context;

        public SqlProcedure()
        {
            context = (T1)Activator.CreateInstance(typeof(T1));
            this.parameters = new List<object>();
        }

        public SqlProcedure(string procedure, List<object> parameters) : base()
        {
            this.procedure = procedure;
            this.parameters = parameters;
        }


        public T2 ExecuteProcedure<T2>(Func<DbDataReader, T2> entity)
        {
            using (var conn = new SqlConnection(context.Database.Connection.ConnectionString))
            {
                using (var command = new SqlCommand(this.procedure, conn))
                {
                    conn.Open();
                    command.Parameters.AddRange(this.parameters.ToArray());
                    command.CommandType = CommandType.StoredProcedure;
                    try
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            T2 data = entity(reader);
                            return data;
                        }
                    }
                    finally
                    {
                        conn.Close();
                        conn.Dispose();
                    }
                }
            }
        }

        public IEnumerable<SqlParameter> GetParameterStoredProcedure()
        {
            var con = new SqlConnection(context.Database.Connection.ConnectionString);
            var cmd = new SqlCommand();
            var stbQuery = new StringBuilder();
            var sqlParameters = new List<SqlParameter>();
            var dt = new DataTable();
            try
            {

                stbQuery.Append("SELECT SP.Name, type_name(sp.user_type_id) AS type ");
                stbQuery.Append($"FROM {context.Database.Connection.Database.ToString()}..sysobjects SO ");
                stbQuery.Append($"INNER join {context.Database.Connection.Database.ToString()}.sys.all_parameters SP ON ");
                stbQuery.Append("SO.id = SP.object_id and SO.xtype = 'P' ");
                stbQuery.Append($"WHERE SO.name = '{procedure}'");

                cmd.Connection = con;
                cmd.CommandText = stbQuery.ToString();
                cmd.CommandType = CommandType.Text;

                con.Open();
                new SqlDataAdapter(cmd).Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    if (Convert.ToString(row).Length != 0)
                        sqlParameters.Add(new SqlParameter(row[0].ToString(), GetTypeDb(row[1].ToString().ToLower())));
                    else
                        sqlParameters.Add(null);
                }
                return sqlParameters;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            finally
            {
                con.Close();
                con.Dispose();
                cmd.Dispose();
                dt.Dispose();
                stbQuery = null;
                sqlParameters = null;

            }

        }

        private SqlDbType GetTypeDb(string value)
        {
            if (value != string.Empty)
            {
                switch (value)
                {
                    case "bigbnt":
                        return SqlDbType.BigInt;
                    case "binary":
                        return SqlDbType.Binary;
                    case "image":
                        return SqlDbType.Image;
                    case "timestamp":
                        return SqlDbType.Timestamp;
                    case "varbinary":
                        return SqlDbType.VarBinary;
                    case "bit":
                        return SqlDbType.Bit;
                    case "char":
                        return SqlDbType.Char;
                    case "nchar":
                        return SqlDbType.NChar;
                    case "ntext":
                        return SqlDbType.NText;
                    case "nvarchar":
                        return SqlDbType.NVarChar;
                    case "text":
                        return SqlDbType.Text;
                    case "varchar":
                        return SqlDbType.VarChar;
                    case "xml":
                        return SqlDbType.Xml;
                    case "datetime":
                        return SqlDbType.DateTime;
                    case "smalldatetime":
                        return SqlDbType.SmallDateTime;
                    case "date":
                        return SqlDbType.Date;
                    case "time":
                        return SqlDbType.Time;
                    case "datetime2":
                        return SqlDbType.DateTime2;
                    case "decimal":
                        return SqlDbType.Decimal;
                    case "money":
                        return SqlDbType.Money;
                    case "smallmoney":
                        return SqlDbType.SmallMoney;
                    case "float":
                        return SqlDbType.Float;
                    case "int":
                        return SqlDbType.Int;
                    case "real":
                        return SqlDbType.Real;
                    case "uniqueidentifier":
                        return SqlDbType.UniqueIdentifier;
                    case "smallint":
                        return SqlDbType.SmallInt;
                    case "tinyint":
                        return SqlDbType.TinyInt;
                    case "variant":
                        return SqlDbType.Variant;
                    case "udt":
                        return SqlDbType.Udt;
                    case "structured":
                        return SqlDbType.Structured;
                    case "datetimeoffset":
                        return SqlDbType.DateTimeOffset;
                }
            }

            return new SqlDbType();

        }

        #region AddParameters


        public void AddParametersByArray(object[] values)
        {
            var sqlParameters = GetParameterStoredProcedure();
            for (int i = 0; i < sqlParameters.Count(); i++)
                AddParameters(new SqlParameter(sqlParameters.ElementAt(i).ParameterName, values[i]), ParameterDirection.Input);
        }
        public void AddParameters(SqlParameter parameter, ParameterDirection direction)
        {
            switch (direction)
            {
                case ParameterDirection.Input:
                    this.parameters.Add(CreateParametersInput(parameter));
                    break;
                case ParameterDirection.Output:
                    this.parameters.Add(CreateParametersOutput(parameter));
                    break;
                case ParameterDirection.InputOutput:
                    break;
                case ParameterDirection.ReturnValue:
                    break;
                default:
                    break;
            }
        }


        #endregion


        #region CreateParameters

        private SqlParameter CreateParametersInput(SqlParameter parameter)
        {

            if (parameter == null)
            {
                parameter.SqlValue = DBNull.Value;
                return parameter;
            }
            else
            {
                parameter.Size = parameter.ToString().Length;
                parameter.Direction = ParameterDirection.Input;
                return parameter;
            }

        }

        private SqlParameter CreateParametersOutput(SqlParameter parameter)
        {
            if (parameter == null)
            {
                parameter.SqlValue = DBNull.Value;
                return parameter;
            }
            else
            {
                parameter.Size = parameter.ToString().Length;
                parameter.Direction = ParameterDirection.Output;
                return parameter;
            }
        }

        #endregion


        #region GetObject
        protected IEnumerable<T2> GetAllObject<T2>(DbDataReader dataReader) =>
            ((IObjectContextAdapter)context)
                .ObjectContext
                .Translate<T2>(dataReader)
                .ToList();

        protected T2 GetOneObject<T2>(DbDataReader dataReader) =>
                ((IObjectContextAdapter)context)
                    .ObjectContext
                    .Translate<T2>(dataReader)
                    .FirstOrDefault();
        #endregion


        protected virtual T2 GetResult(DbDataReader dataReader, T2 objeto) =>
            objeto;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
