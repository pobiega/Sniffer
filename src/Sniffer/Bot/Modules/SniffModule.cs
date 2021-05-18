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
                await ReplyAsync($"Unable to set channel settings: {result.ErrorReason}");
                return;
            }

            await Announcewatch(Context.Channel, radius, text);
        }

        private async Task Announcewatch(ISocketMessageChannel channel, int radius, string text)
        {
            if (channel is ITextChannel textChannel)
            {
                await textChannel.ModifyAsync(c => c.Topic = $"Currently watching: {radius} jumps from {text}");
            }

            await ReplyAsync($"Setting watch to {radius} jumps from {text}");
        }
    }
}
