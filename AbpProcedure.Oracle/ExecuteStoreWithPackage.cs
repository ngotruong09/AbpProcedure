using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Threading.Tasks;

namespace AbpProcedure.Oracle
{
    public static partial class OracleSqlHelper1
    {
        public static async Task<DataSet> ExecuteStoreWithPackageAsync(string query, string package, string dbContext, object param)
        {
            var dataset = new DataSet();

            // Get store param.
            var storeInfo = await GetExecutionParamsWithPackage(query, package, param, 0, dbContext);

            string connStr = GetConnection(dbContext);
            using (var objConn = new OracleConnection(connStr))
            {
                var objCmd = new OracleCommand
                {
                    Connection = objConn,
                    CommandText = package + "." + query,//storeInfo.Item1,
                    CommandType = CommandType.StoredProcedure
                };

                objCmd.Parameters.AddRange(storeInfo.Item2);

                try
                {
                    // Open oracle connection.
                    await objConn.OpenAsync();
                    // Get data set.
                    var da = new OracleDataAdapter(objCmd);
                    da.Fill(dataset);
                }
                finally
                {
                    await objConn.CloseAsync();
                }
            }

            return dataset;
        }
    }
}
