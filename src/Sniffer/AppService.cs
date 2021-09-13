using Microsoft.Extensions.Hosting;
using Sniffer.KillBoard;
using Sniffer.Static;
using System.Threading;
using System.Threading.Tasks;

namespace Sniffer
{
    public class AppService : IHostedService
    {
        private readonly KillBoardMonitor _killBoardMonitor;

        public AppService(KillBoardMonitor killBoardMonitor)
        {
            _killBoardMonitor = killBoardMonitor;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _killBoardMonitor.Initialize();

            EveStaticDataProvider.Initialize();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
