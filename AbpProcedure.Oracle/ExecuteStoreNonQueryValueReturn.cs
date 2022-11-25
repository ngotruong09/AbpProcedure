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
        public static OracleDbType getDataTypeProc(string DATA_TYPE)
        {
            OracleDbType res = OracleDbType.Varchar2;
            switch (DATA_TYPE.ToUpper())
            {
                case "BFILE":
                    res = OracleDbType.BFile;
                    break;
                case "BLOB":
                    res = OracleDbType.Blob;
                    break;
                case "BYTE":
                    res = OracleDbType.Byte;
                    break;
                case "CHAR":
                    res = OracleDbType.Char;
                    break;
                case "CLOB":
                    res = OracleDbType.Clob;
                    break;
                case "DATE":
                    res = OracleDbType.Date;
                    break;
                case "NUMBER":
                    res = OracleDbType.Decimal;
                    break;
                case "FLOAT":
                    res = OracleDbType.Double;
                    break;
                case "INTEGER":
                    res = OracleDbType.Int64;
                    break;
                case "INTERVAL DAY TO SECOND":
                    res = OracleDbType.IntervalDS;
                    break;
                case "INTERVAL YEAR TO MONTH":
                    res = OracleDbType.IntervalYM;
                    break;
                case "LONG":
                    res = OracleDbType.Long;
                    break;
                case "LONG RAW":
                    res = OracleDbType.LongRaw;
                    break;
                case "NCHAR":
                    res = OracleDbType.NChar;
                    break;
                case "NCLOB":
                    res = OracleDbType.NClob;
                    break;
                case "NVARCHAR2":
                    res = OracleDbType.NVarchar2;
                    break;
                case "RAW":
                    res = OracleDbType.Raw;
                    break;
                case "REF CURSOR":
                    res = OracleDbType.RefCursor;
                    break;
                case "TIMESTAMP":
                    res = OracleDbType.TimeStamp;
                    break;
                case "TIMESTAMP WITH LOCAL TIME ZONE":
                    res = OracleDbType.TimeStampLTZ;
                    break;
                case "TIMESTAMP WITH TIME ZONE":
                    res = OracleDbType.TimeStampTZ;
                    break;
                case "VARCHAR2":
                    res = OracleDbType.Varchar2;
                    break;
                case "XMLType":
                    res = OracleDbType.XmlType;
                    break;
            }

            return res;
        }

        public static async Task<Tuple<string, object[]>> GetExecutionParams2(string query, object param, int? type, string dbContext)
        {
            if (param == null)
            {
                param = new { };
            }

            var queryScript = string.Empty;

            Tuple<string, object[]> @params;
            if (type == 0)
            {
                @params = await GetStoreExecutionParams2(query, param, dbContext);
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

        public static async Task<Tuple<string, object[]>> GetStoreExecutionParams2(string storeName, object param, string dbContext)
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

                if (para.InOut == "OUT")
                {
                    sqlParams.Add(new OracleParameter(sParam, getDataTypeProc(para.DataType), ParameterDirection.Output));
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

        public static async Task<string> ExecuteStoreNonQuery2(string query, string nameParamOutput, string dbContext, object param = null)
        {
            string rs = string.Empty;

            // Get store param.
            var storeInfo = await GetExecutionParams2(query, param, 0, dbContext);

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
                    await objCmd.ExecuteNonQueryAsync();

                    rs = objCmd.Parameters[nameParamOutput].Value.ToString();
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
