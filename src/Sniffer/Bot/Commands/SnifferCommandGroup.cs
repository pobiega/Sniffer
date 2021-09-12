using Humanizer;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Core;
using Remora.Results;
using Serilog;
using Sniffer.KillBoard;
using Sniffer.Persistance.Model;
using System.Threading.Tasks;

namespace Sniffer.Bot.Commands
{
    internal class SnifferCommandGroup : CommandGroup
    {
        private readonly ILogger _logger;
        private readonly KillBoardMonitor _killBoardMonitor;
        private readonly FeedbackService _feedbackService;
        private readonly ICommandContext _commandContext;

        public SnifferCommandGroup(
            ILogger logger,
            KillBoardMonitor killBoardMonitor,
            FeedbackService feedbackService,
            ICommandContext commandContext
            )
        {
            _logger = logger;
            _killBoardMonitor = killBoardMonitor;
            _feedbackService = feedbackService;
            _commandContext = commandContext;
        }

        [Command("sniff")]
        public async Task<IResult> SniffInvokeAsync(int radius, string text, KillType killType = KillType.All)
        {
            if (radius < 0)
            {
                return await ReplyWithFailureAsync("Radius must be zero or above.");
            }

            var setChannelSettings = await _killBoardMonitor.TrySetChannelSettings(_commandContext.ChannelID, radius, text, killType);

            if (!setChannelSettings.IsSuccess)
            {
                await ReplyWithFailureAsync("Can't update channel settings, verify system name is correct.");
                return setChannelSettings;
            }

            var response = $"{"jump".ToQuantity(radius)} from {setChannelSettings.Entity}";

            if (killType == KillType.Player)
            {
                response += ", player kills only";
            }
            else if (killType == KillType.NPC)
            {
                response += ", only pure NPC kills";
            }

            var announce = await _feedbackService.SendContextualSuccessAsync($"Setting watch to {response}.", ct: CancellationToken);

            return !announce.IsSuccess
                ? Result.FromError(announce)
                : Result.FromSuccess();
        }

        public async Task<Result> ReplyWithFailureAsync(string text, Snowflake? target = null)
        {
            var replyFail = await _feedbackService.SendContextualErrorAsync(
                contents: text,
                target: target,
                ct: CancellationToken);

            return !replyFail.IsSuccess
                ? Result.FromError(replyFail)
                : Result.FromSuccess();
        }
    }
}
