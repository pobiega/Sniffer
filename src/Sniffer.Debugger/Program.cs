using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Sniffer.Data;
using Sniffer.KillBoard.ZKill;
using Sniffer.Static;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sniffer.Debugger
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureLogging(logging =>
                {
                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.Console()
                        .CreateLogger();
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(Log.Logger);
                    services.AddHttpClient();
                    services.AddSingleton<IESIClient, ESIClient>();

                    services.AddHostedService<Whatever>();
                    //services.AddZKillService();
                });

            var host = hostBuilder.Build();
            //var service = host.Services.GetRequiredService<IZKillProcessingService>();

            //service.PackageArrived += async (s, e) =>
            //{
            //    await Service_PackageArrived(s, e);
            //};

            await host.RunAsync();

        }

        private static Task Service_PackageArrived(object sender, PackageArrivedEventArgs e)
        {
            Log.Logger.Information("Package arrived: {killID}", e.Package.killID);
            return Task.CompletedTask;
        }


    }

    public class Whatever : BackgroundService
    {
        private readonly IESIClient _esiClient;

        public Whatever(IESIClient esiClient)
        {
            _esiClient = esiClient;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            EveStaticDataProvider.Initialize();

            if (EveStaticDataProvider.Instance.SystemIds.TryGetValue(30000142, out var systemName))
            {
                Console.WriteLine(systemName);
            }

            return Task.CompletedTask;
        }
    }
}
