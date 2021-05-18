using Microsoft.Extensions.DependencyInjection;

namespace Sniffer.KillBoard.ZKill
{
    public static class DependencyInjectionExtensions
    {
        public static void AddZKillService(this IServiceCollection services)
        {
            services.AddHttpClient();

            services.AddSingleton<IZKillClient, ZKillClient>();
            services.AddSingleton<IZKillProcessingService, ZKillProcessingService>();
            
            services.AddHostedService<ZKillBackgroundService>();
        }
    }
}
