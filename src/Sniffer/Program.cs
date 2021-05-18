﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Sniffer.Bot;
using Sniffer.KillBoard;
using Sniffer.KillBoard.ZKill;
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
            services.AddSingleton<KillBoardMonitor>();

            services.AddZKillService();
            services.AddDiscordBot();
            services.AddHostedService<AppService>();
        }
    }

    public static class Program
    {
        public static IHostBuilder CreateHostBuilder()
        {
            return new HostBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddJsonFile("appsettings.json");
                    config.AddEnvironmentVariables();
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
            var host = CreateHostBuilder();
            await host.RunConsoleAsync();
        }
    }
}
