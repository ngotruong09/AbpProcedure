using AbpProcedure.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbpProcedure.Oracle
{
    public class OracleSqlHelper : SqlHelperBase
    {
        public override Task<int> ExecuteStoreNonQuery(string query, string dbContext, object param = null)
        {
            throw new NotImplementedException();
        }
    }
}
