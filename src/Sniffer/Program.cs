using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Sniffer.Bot;
using Sniffer.Data;
using Sniffer.Data.Caching;
using Sniffer.KillBoard;
using Sniffer.KillBoard.ZKill;
using Sniffer.Persistance;
using System;
using System.Threading.Tasks;

// False because of Discord.NET, try changing to true after we swap to remora
[assembly: CLSCompliant(false)]
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
            services.Configure<DatabaseSettings>(Configuration.GetSection(nameof(DatabaseSettings)));

            var databaseSettings = Configuration.GetSection(nameof(DatabaseSettings)).Get<DatabaseSettings>();

            var connectionString = Configuration.GetConnectionString("Database");
            services.AddSnifferDatabase(databaseSettings, connectionString);

            services.AddSingleton<KillBoardMonitor>();

            services.AddHttpClient();
            services.AddSingleton<ICache, DatabaseCache>();
            services.AddSingleton<IESIClient, CachingESIClient>();

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
                        .MinimumLevel.Debug()
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
