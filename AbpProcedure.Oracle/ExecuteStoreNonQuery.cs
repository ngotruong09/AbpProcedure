using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Threading.Tasks;

namespace AbpProcedure.Oracle
{
    public static partial class OracleSqlHelper1
    {
        public static async Task<int> ExecuteStoreNonQuery(string query, string dbContext, object param = null)
        {
            int rs = 0;

            // Get store param.
            var storeInfo = await GetExecutionParams(query, param, 0, dbContext);

            string connStr = GetConnection(dbContext);
            using (var objConn = new OracleConnection(connStr))
            {
                var objCmd = new OracleCommand
                {
                    Connection = objConn,
                    CommandText = query,//storeInfo.Item1,
                    CommandType = CommandType.StoredProcedure
                };

                objCmd.Parameters.AddRange(storeInfo.Item2);

                try
                {
                    // Open oracle connection.
                    await objConn.OpenAsync();
                    // Execute query.
                    rs = await objCmd.ExecuteNonQueryAsync();
                }
                finally
                {
                    await objConn.CloseAsync();
                }
            }

            return rs;
        }

        public static async Task<int> ExecuteStoreNonQuery(string query, string package, string dbContext, object param = null)
        {
            int rs = 0;

            // Get store param.
            var storeInfo = await GetExecutionParamsWithPackage(query, package, param, 0, dbContext);

            string connStr = GetConnection(dbContext);
            using (var objConn = new OracleConnection(connStr))
            {
                var objCmd = new OracleCommand
                {
                    Connection = objConn,
                    CommandText = package + "." + query,
                    CommandType = CommandType.StoredProcedure
                };

                objCmd.Parameters.AddRange(storeInfo.Item2);

                try
                {
                    // Open oracle connection.
                    await objConn.OpenAsync();
                    // Execute query.
                    rs = await objCmd.ExecuteNonQueryAsync();
                }
                finally
                {
                    await objConn.CloseAsync();
                }
            }

            return rs;
        }
    }
}
