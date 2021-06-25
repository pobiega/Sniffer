using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Sniffer.Bot;
using Sniffer.KillBoard;
using Sniffer.KillBoard.ZKill;
using Sniffer.Persistance;
using System.Threading.Tasks;

namespace Sniffer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(Log.Logger);

            services.Configure<KillBoardMonitorSettings>(Configuration.GetSection(nameof(KillBoardMonitorSettings)));

            var connectionString = Configuration.GetConnectionString("Database");
            services.AddSnifferDatabase(connectionString);

            services.AddSingleton<KillBoardMonitor>();

            services.AddZKillService();
            services.AddDiscordBot();
            services.AddHostedService<AppService>();
        }
    }

    public static class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(host =>
                {
                    host.AddJsonFile("hostsettings.json", optional: true);
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddSerilog();

                    var loggerConfiguration = new LoggerConfiguration()
                        .MinimumLevel.Verbose()
                        .Enrich.FromLogContext()
                        .WriteTo.Console();

                    Log.Logger = loggerConfiguration.CreateLogger();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    var startup = new Startup(hostContext.Configuration);
                    startup.ConfigureServices(services);
                });
        }

        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args);
            await host.RunConsoleAsync().ConfigureAwait(false);
        }
    }
}
