using AbpProcedure.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbpProcedure.SqlServer
{
    public class SqlServerHelper : SqlHelperBase
    {
        public override Task<int> ExecuteStoreNonQuery(string query, string dbContext, object param = null)
        {
            throw new NotImplementedException();
        }
    }
}
