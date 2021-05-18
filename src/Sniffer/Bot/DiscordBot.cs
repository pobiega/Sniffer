using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Threading;
using System.Threading.Tasks;

namespace Sniffer.Bot
{
    public class DiscordBot : IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandHandler _commandHandler;

        public DiscordBot(ILogger logger, DiscordSocketClient client, CommandHandler commandHandler)
        {
            _client = client;
            _commandHandler = commandHandler;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _commandHandler.InstallCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, "ODQzOTQyMTIyNDUzMjA1MDMz.YKLMWQ.5NPYflKG54akg__NR1S91CVGhLM");
            await _client.StartAsync();

            await _client.SetGameAsync("Woof");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.StopAsync();
        }
    }
}
