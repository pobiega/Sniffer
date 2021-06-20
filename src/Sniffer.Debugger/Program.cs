using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Sniffer.KillBoard.ZKill;
using System.Threading.Tasks;

namespace Sniffer.Debugger
{
    class Program
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

                    //services.AddZKillService();
                });

            var host = hostBuilder.Build();
            //var service = host.Services.GetRequiredService<IZKillProcessingService>();

            //service.PackageArrived += async (s, e) =>
            //{
            //    await Service_PackageArrived(s, e);
            //};

            //await host.RunAsync();

            var client = new MongoDB.Driver.MongoClient("mongodb://localhost:27017");
            var dataBase = client.GetDatabase("Sniffer");
            var t = dataBase.GetCollection<>()
        }

        private static Task Service_PackageArrived(object sender, PackageArrivedEventArgs e)
        {
            Log.Logger.Information("Package arrived: {killID}", e.Package.killID);
            return Task.CompletedTask;
        }
    }
}
