using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AbpProcedure.Base
{
    public abstract class SqlHelperBase
    {
        public abstract Task<int> ExecuteStoreNonQuery(string query, string dbContext, object param = null);
    }
}
