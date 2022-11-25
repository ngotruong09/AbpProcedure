using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.EntityFrameworkCore;

namespace AbpProcedure.SqlServer
{
    public static class DbContextProviderExtension
    {
        public static async Task<bool> ExecuteStoreNonQueryAsync<TDbContext>(
           this IDbContextProvider<TDbContext> dbContextProvider, string nameSp, object param = null)
         where TDbContext : IEfCoreDbContext
        {
            var db = await dbContextProvider.GetDbContextAsync();
            var conn = db.Database.GetDbConnection().ConnectionString;

            var helper = new SqlServerHelper();
            await helper.ExecuteStoreNonQuery(nameSp, "", param);
            return true;
        }
    }
}
