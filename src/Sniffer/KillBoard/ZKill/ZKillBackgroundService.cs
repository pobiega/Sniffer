using Microsoft.Extensions.Hosting;
using Serilog;
using Sniffer.Async;
using System.Threading;
using System.Threading.Tasks;

namespace Sniffer.KillBoard.ZKill
{
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    public record PackageArrivedEventArgs(Package Package);
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix

    public interface IZKillProcessingService
    {
        AsyncEvent<PackageArrivedEventArgs> PackageArrived { get; set; }
        Task ExecuteAsync();
    }

    public class ZKillProcessingService : IZKillProcessingService
    {
        private readonly ILogger _logger;
        private readonly IZKillClient _zKillClient;

        public ZKillProcessingService(ILogger logger, IZKillClient zKillClient)
        {
            _logger = logger;
            _zKillClient = zKillClient;
        }

        public AsyncEvent<PackageArrivedEventArgs> PackageArrived { get; set; }

        public async Task ExecuteAsync()
        {
            if (PackageArrived == null)
            {
                await Task.Delay(1000);
                return;
            }

            var response = await _zKillClient.GetKillmail();

            if (response?.package == null)
            {
                // blank response somehow
                // TODO: make sure its not a 429 or something like that
                _logger.Debug("Blank response from ZKillClient.");
                await Task.Delay(1000);
                return;
            }

            await (PackageArrived.InvokeAsyncSerial(this, new PackageArrivedEventArgs(response.package)) ?? Task.CompletedTask);
        }
    }

    public class ZKillBackgroundService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IZKillProcessingService _processingService;

        public ZKillBackgroundService(ILogger logger, IZKillProcessingService processingService)
        {
            _logger = logger;
            _processingService = processingService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _processingService.ExecuteAsync();
            }
        }
    }
}
