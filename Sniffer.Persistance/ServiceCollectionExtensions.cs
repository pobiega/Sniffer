using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Sniffer.Persistance
{
    public static class ServiceCollectionExtensions
    {
        public static void AddSnifferDatabase(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<SnifferDbContext>(opt
                => opt.UseNpgsql(connectionString));
        }
    }
}
