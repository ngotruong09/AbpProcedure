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
        #region -- fields --
        public static Dictionary<string, OracleStoreParam[]> cacheStoreparamDic = new Dictionary<string, OracleStoreParam[]>();
        public static Dictionary<string, string> ConnectionString = new Dictionary<string, string>();
        public static Dictionary<string,string> SchemaOwner = new Dictionary<string,string>();
        #endregion
       
        #region -- get multiple db context --
        public static void SetConnection(string dbcontext, string connection)
        {
            if (!ConnectionString.ContainsKey(dbcontext))
            {
                ConnectionString.Add(dbcontext, connection);
            }
        }
        public static string GetConnection(string dbcontext)
        {
            if (ConnectionString.ContainsKey(dbcontext))
            {
                return ConnectionString[dbcontext];
            }
            return string.Empty;
        }
        public static bool RemoveConnection(string dbcontext)
        {
            if (ConnectionString != null && ConnectionString.ContainsKey(dbcontext))
            {
                ConnectionString.Remove(dbcontext);
                return true;
            }
            return false;
        }
        public static bool IsEmptyConnectionString()
        {
            return ConnectionString.Count == 0;
        }
        public static bool IsExistConnection(string dbContext)
        {
            return ConnectionString.ContainsKey(dbContext);
        }
        public static void SetSchemaOwner(string dbcontext, string schema)
        {
            if (!SchemaOwner.ContainsKey(dbcontext))
            {
                SchemaOwner.Add(dbcontext, schema);
            }
        }
        public static string GetSchemaOwner(string dbcontext)
        {
            if (SchemaOwner.ContainsKey(dbcontext))
            {
                return SchemaOwner[dbcontext];
            }
            return string.Empty;
        }
        public static bool RemoveSchemaOwner(string dbcontext)
        {
            if (SchemaOwner != null && SchemaOwner.ContainsKey(dbcontext))
            {
                SchemaOwner.Remove(dbcontext);
                return true;
            }
            return false;
        }
        public static bool IsEmptySchema()
        {
            return SchemaOwner.Count == 0;
        }
        public static bool IsExistSchema(string dbContext)
        {
            return SchemaOwner.ContainsKey(dbContext);
        }
        #endregion
       
        #region -- methods --
        public async static Task<OracleStoreParam[]> GetStoreParamsInDb(string storeName, string dbContext)
        {
            var sql = $@"
                        SELECT ua.argument_name as name,
                               ua.position as position,
                               ua.data_type as datatype,
                               ua.IN_OUT as inout
                        FROM all_arguments ua
                        WHERE ua.object_name = '{storeName?.ToUpper()}'
                    ";

            var schemaOwner = GetSchemaOwner(dbContext);
            if (!string.IsNullOrWhiteSpace(schemaOwner))
            {
                sql = $"{sql} and ua.owner ='{schemaOwner}'";
            }
            var dt = new DataTable();
            string connStr = GetConnection(dbContext);
            using (var objConn = new OracleConnection(connStr))
            {
                var objCmd = new OracleCommand
                {
                    Connection = objConn,
                    CommandText = sql,// storeInfo.Item1,
                    CommandType = CommandType.Text
                };
                try
                {
                    // Open oracle connection.
                    await objConn.OpenAsync();
                    // Get data set.
                    var da = new OracleDataAdapter(objCmd);
                    da.Fill(dt);
                }
                finally
                {
                    await objConn.CloseAsync();
                }
            }

            var paramList = new List<OracleStoreParam>();
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    var param = new OracleStoreParam
                    {
                        Name = row["name"].ToString(),
                        Position = int.Parse(row["position"].ToString()),
                        DataType = row["datatype"].ToString(),
                        InOut = row["inout"].ToString(),
                    };

                    paramList.Add(param);
                }
            }

            var res = paramList.OrderBy(item => item.Position).ToArray();
            return res;
        }
        public static async Task<Tuple<string, object[]>> GetExecutionParamsWithPackage(string query, string package, object param, int? type, string dbContext)
        {
            if (param == null)
            {
                param = new { };
            }

            var queryScript = string.Empty;

            Tuple<string, object[]> @params;
            if (type == 0)
            {
                @params = await GetStoreExecutionParams(query, param, dbContext);
            }
            else
            {
                @params = await GetExecutionParams(param);
            }
            var queryNormalize = package + "." + query;
            queryScript = queryNormalize;
            if (type == 0)
            {
                // Incase store proceduce.
                queryScript = $"EXEC {queryNormalize} ({@params.Item1}); END;";
            }
            else if (type == 10)
            {
                // Incase Scalar function
                queryScript = $"SELECT {queryNormalize}({@params.Item1}) FROM DUAL";
            }
            else if (type == 10)
            {
                // Incase Table function
                queryScript = $"Select * {queryNormalize}({@params.Item1}) FROM DUAL";
            }

            return new Tuple<string, object[]>(queryScript, @params.Item2);
        }
        public static async Task<DataSet> ExecuteStoreAsync(string query, string dbContext, object param)
        {
            var dataset = new DataSet();

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
        public static async Task<Tuple<string, object[]>> GetExecutionParams(string query, object param, int? type, string dbContext)
        {
            if (param == null)
            {
                param = new { };
            }

            var queryScript = string.Empty;

            Tuple<string, object[]> @params;
            if (type == 0)
            {
                @params = await GetStoreExecutionParams(query, param, dbContext);
            }
            else
            {
                @params = await GetExecutionParams(param);
            }

            queryScript = query;
            if (type == 0)
            {
                // Incase store proceduce.
                queryScript = $"EXEC {query} ({@params.Item1}); END;";
            }
            else if (type == 10)
            {
                // Incase Scalar function
                queryScript = $"SELECT {query}({@params.Item1}) FROM DUAL";
            }
            else if (type == 10)
            {
                // Incase Table function
                queryScript = $"Select * {query}({@params.Item1}) FROM DUAL";
            }

            return new Tuple<string, object[]>(queryScript, @params.Item2);
        }
        public static Task<Tuple<string, object[]>> GetExecutionParams(object param)
        {
            var sqlParams = new List<object>();
            var paramStr = "";

            // Get sql param.
            var properties = param.GetType().GetProperties();
            foreach (var property in properties)
            {
                paramStr += string.IsNullOrWhiteSpace(paramStr)
                    ? $":{property.Name}"
                    : $",:{property.Name}";

                var value = property.GetValue(param);

                // Incase cursor property.
                if (property.PropertyType == typeof(OracleDbType) &&
                    (OracleDbType)value == OracleDbType.RefCursor)
                {
                    sqlParams.Add(new OracleParameter(property.Name, OracleDbType.RefCursor, ParameterDirection.Output));
                }
                else
                {
                    // Incase normal property.
                    sqlParams.Add(value == null
                        ? new OracleParameter(property.Name, DBNull.Value)
                        : new OracleParameter(property.Name, value));
                }
            }

            var res = new Tuple<string, object[]>(paramStr, sqlParams.ToArray());
            return Task.FromResult(res);
        }
        public static async Task<Tuple<string, object[]>> GetStoreExecutionParams(string storeName, object param, string dbContext)
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

                paramStr += string.IsNullOrWhiteSpace(paramStr)
                    ? $":{sParam}"
                    : $",:{sParam}";

                if (para.DataType == "REF CURSOR")
                {
                    if (para.InOut == "OUT")
                    {
                        sqlParams.Add(new OracleParameter(sParam, OracleDbType.RefCursor, ParameterDirection.Output));
                    }
                    else
                    {
                        throw new Exception("Don't support REF CURSOR IN");
                    }
                }
                else if (para.DataType == "CLOB")
                {
                    object value = null;
                    if (inputParamDic.ContainsKey(sParam))
                    {
                        var pro = inputParamDic[sParam];
                        value = pro.GetValue(param);
                    }
                    sqlParams.Add(new OracleParameter(sParam, OracleDbType.Clob, value, ParameterDirection.Input));
                }
                else if (para.DataType == "NCLOB")
                {
                    object value = null;
                    if (inputParamDic.ContainsKey(sParam))
                    {
                        var pro = inputParamDic[sParam];
                        value = pro.GetValue(param);
                    }
                    sqlParams.Add(new OracleParameter(sParam, OracleDbType.NClob, value, ParameterDirection.Input));
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
        public static async Task<OracleStoreParam[]> GetStoreParams(string storeName, string dbContext)
        {
            if (!cacheStoreparamDic.ContainsKey(storeName))
            {
                var @param = await GetStoreParamsInDb(storeName, dbContext);
                cacheStoreparamDic.Add(storeName, @param);
            }

            return cacheStoreparamDic[storeName];
        }
        #endregion
    }
}
