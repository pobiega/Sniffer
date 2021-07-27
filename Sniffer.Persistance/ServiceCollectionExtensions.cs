using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Sniffer.Persistance
{
    public static class ServiceCollectionExtensions
    {
        public static void AddSnifferDatabase(this IServiceCollection services, DatabaseSettings databaseSettings, string connectionString)
        {
            services.AddDbContext<SnifferDbContext>(opt
                =>
            {
                opt.UseNpgsql(connectionString);
                if (databaseSettings.ShowSqlParametersInLogs)
                {
                    opt.EnableSensitiveDataLogging();
                }
            });
        }
    }
}
