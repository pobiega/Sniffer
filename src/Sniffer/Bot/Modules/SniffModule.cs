using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Sniffer.KillBoard;
using System.Threading.Tasks;

namespace Sniffer.Bot.Modules
{
    public class SniffModule : ModuleBase<SocketCommandContext>
    {
        private readonly KillBoardMonitor _killBoardMonitor;

        public SniffModule(KillBoardMonitor killBoardMonitor)
        {
            _killBoardMonitor = killBoardMonitor;
        }

        [Command("sniff"), Summary("Sniffs")]
        public async Task SniffInvokeAsync(int radius, [Remainder] string text)
        {
            if (!_killBoardMonitor.TrySetChannelSettings(Context.Channel, radius, text, out var result))
            {
                await ReplyAsync($"Unable to set channel settings: {result.ErrorMessage}");
                return;
            }

            await Announcewatch(Context.Channel, radius, result.Value);
        }

        private async Task Announcewatch(ISocketMessageChannel channel, int radius, string systemName)
        {
            if (channel is ITextChannel textChannel)
            {
                await textChannel.ModifyAsync(c => c.Topic = $"Currently watching: {radius} jumps from {systemName}");
            }

            await ReplyAsync($"Setting watch to {radius} jumps from {systemName}");
        }
    }
}
