using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace EntityFrameworkStoredProcedure
{
    public interface ISqlProcedure<T> : IDisposable
    {
        string procedure { get; set; }

        IList<object> parameters { get; set; }


        T ExecuteProcedure<T>(Func<DbDataReader, T> entity);

        IEnumerable<SqlParameter> GetParameterStoredProcedure();

        void AddParametersByArray(object[] values);

        void AddParameters(SqlParameter parameter, ParameterDirection direction);











    }
}
