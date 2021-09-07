using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Serilog;
using Sniffer.KillBoard;
using Sniffer.Persistance.Model;
using System;
using System.Threading.Tasks;

namespace Sniffer.Bot.Modules
{
    public class SniffModule : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger _logger;
        private readonly KillBoardMonitor _killBoardMonitor;

        public SniffModule(
            ILogger logger,
            KillBoardMonitor killBoardMonitor
            )
        {
            _logger = logger;
            _killBoardMonitor = killBoardMonitor;
        }

        [Command("sniff"), Summary("Sniffs")]
        public async Task SniffInvokeAsync(int radius, string text, KillType killType = KillType.All)
        {
            if (radius < 0)
            {
                return;
            }

            if (!_killBoardMonitor.TrySetChannelSettings(Context.Channel, radius, text, killType, out var result))
            {
                await ReplyAsync($"Unable to set channel settings: {result.ErrorMessage}");
                return;
            }

            await Announcewatch(Context.Channel, radius, result.Value, killType);
        }

        private async Task Announcewatch(ISocketMessageChannel channel, int radius, string systemName, KillType killType)
        {
            var text = $"{radius} jumps from {systemName}";

            if (killType == KillType.Player)
            {
                text += ", player kills only.";
            }
            else if (killType == KillType.NPC)
            {
                text += ", only pure NPC kills.";
            }

            try
            {
                if (channel is ITextChannel textChannel)
                {
                    await textChannel.ModifyAsync(c => c.Topic = $"Currently watching: {text}");
                }

                await ReplyAsync($"Setting watch to {text}");
            }
            catch (Exception ex)
            {
                _logger.Error("Error when announcing new watch", ex);
            }
        }
    }
}
