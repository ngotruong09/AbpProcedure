using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AbpProcedure.Oracle
{
    public static partial class OracleSqlHelper1
    {
        public static async Task<string> ExecuteFunctionAsync(string query, string package, string dbContext, object param = null)
        {
            string rs = string.Empty;

            // Get store param.
            var storeInfo = await GetExecutionParamFnc(query, package, param, 0, dbContext);

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
                    await objCmd.ExecuteNonQueryAsync();
                    rs = objCmd.Parameters["RESULT"].Value.ToString();
                }
                finally
                {
                    await objConn.CloseAsync();
                }
            }

            return rs;
        }

        public async static Task<Tuple<string, object[]>> GetExecutionParamFnc(string query, string package, object param, int? type, string dbContext)
        {
            var queryScript = package + "." + query;
            if (param == null)
            {
                param = new { };
            }

            Tuple<string, object[]> @params;
            @params = await GetFncExecutionParams(query, param, dbContext);
            queryScript = $"SELECT {queryScript}({@params.Item1}) FROM DUAL";

            return new Tuple<string, object[]>(queryScript, @params.Item2);
        }

        public async static Task<Tuple<string, object[]>> GetFncExecutionParams(string storeName, object param, string dbContext)
        {
            var sqlParams = new List<object>();
            var paramStr = "";

            // Get input param dictionary.
            var inputParamDic = param.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                                            .ToDictionary(item => item.Name.ToUpper(), item => item);

            // Get store procedure parameters.
            var storeParams = await GetStoreParams(storeName, dbContext);

            // Get store params.
            foreach (var para in storeParams)
            {
                var sParam = para.Name.ToUpper();
                sParam = string.IsNullOrEmpty(sParam) ? "RESULT" : sParam;

                paramStr += string.IsNullOrWhiteSpace(paramStr)
                    ? $":{sParam}"
                    : $",:{sParam}";

                if (para.InOut == "OUT")
                {
                    sqlParams.Add(new OracleParameter(sParam, OracleDbType.Double, ParameterDirection.ReturnValue));
                }
                else
                {
                    object value = null;
                    if (inputParamDic.ContainsKey(sParam))
                    {
                        var pro = inputParamDic[sParam];
                        value = pro.GetValue(param);
                    }

                    // Incase normal property.
                    sqlParams.Add(value == null
                        ? new OracleParameter(sParam, DBNull.Value)
                        : new OracleParameter(sParam, value));
                }
            }

            return new Tuple<string, object[]>(paramStr, sqlParams.ToArray());
        }
    }
}
