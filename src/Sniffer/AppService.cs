using Microsoft.Extensions.Hosting;
using Sniffer.Bot;
using Sniffer.KillBoard;
using System.Threading;
using System.Threading.Tasks;

namespace Sniffer
{
    public class AppService : IHostedService
    {
        private readonly KillBoardMonitor _killBoardMonitor;
        private readonly DiscordBot _discordBot;

        public AppService(KillBoardMonitor killBoardMonitor, DiscordBot discordBot)
        {
            _killBoardMonitor = killBoardMonitor;
            _discordBot = discordBot;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _killBoardMonitor.Initialize();

            await _discordBot.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _discordBot.StopAsync(cancellationToken);
        }
    }
}
