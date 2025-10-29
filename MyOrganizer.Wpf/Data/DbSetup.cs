using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOrganizer.Wpf.Data
{
    public static class DbSetup
    {
        public static void AddDatabase(IServiceCollection services, IConfiguration configuration)
        {
            var provider = configuration["Database:Provider"] ?? "SqlServer";

            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                var connection = configuration["Database:SqlServer:ConnectionString"];
                 

                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(connection).ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));
            }
            else if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                var connection = configuration["Database:Sqlite:ConnectionString"];
                 

                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite(connection).ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));
            }
        }
    }
}
