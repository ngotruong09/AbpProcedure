using AbpProcedure.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Volo.Abp.EntityFrameworkCore;

namespace AbpProcedure.Oracle
{
    public static class DbContextProviderExtension
    {
        private static Task<string> InitValue<TDbContext>(TDbContext dbContextProvider, string conn)
            where TDbContext : IEfCoreDbContext
        {
            var dbContext = dbContextProvider.GetType().Name.Replace("DbContext", "");
            if (!OracleSqlHelper1.IsExistSchema(dbContext))
            {
                var config = dbContextProvider.GetService<IConfiguration>();
                var schemaOwner = config[$"SchemaOwnerDb:{dbContext}"];
                if (!string.IsNullOrEmpty(schemaOwner))
                {
                    OracleSqlHelper1.SetSchemaOwner(dbContext, schemaOwner);
                }
            }
            if (!OracleSqlHelper1.IsExistConnection(dbContext))
            {
                OracleSqlHelper1.SetConnection(dbContext, conn);
            }

            return Task.FromResult(dbContext);
        }

        public static async Task<bool> ExecuteStoreNonQueryAsync<TDbContext>(
            this IDbContextProvider<TDbContext> dbContextProvider, string nameSp, object param = null)
          where TDbContext : IEfCoreDbContext
        {
            var db = await dbContextProvider.GetDbContextAsync();
            var conn = db.Database.GetDbConnection().ConnectionString;
            var dbContext = await InitValue(db, conn);

            var helper = new OracleSqlHelper();
            await helper.ExecuteStoreNonQuery(nameSp, dbContext, param);
            return true;
        }
    }
}
