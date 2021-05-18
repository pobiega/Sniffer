using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Serilog;
using Sniffer.KillBoard.ZKill;
using System.Threading.Tasks;

namespace Sniffer.KillBoard
{
    public record Result<T>(bool IsSuccess, T Value, string ErrorReason = null);

    // TODO: rename this? its more of a manager type class at this point
    public class KillBoardMonitor
    {
        private readonly ILogger _logger;
        private readonly IOptions<KillBoardMonitorSettings> _settings;

        private readonly MonitorSettings _monitorSettings = new();
        private readonly IZKillProcessingService _zKillProcessingService;

        public KillBoardMonitor(ILogger logger, IOptions<KillBoardMonitorSettings> settings, IZKillProcessingService zKillProcessingService)
        {
            _logger = logger;
            _settings = settings;
            _zKillProcessingService = zKillProcessingService;
        }

        public async Task Initialize()
        {
            _logger.Debug("Initializing KillBoardMonitor...");

            _zKillProcessingService.PackageArrived += async (s, e) => await OnPackageArrived(s, e);

            // TODO: load monitor settings from file.
            // this probably means doing channel lookup
        }

        private async Task OnPackageArrived(object sender, PackageArrivedEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        public bool TrySetChannelSettings(ISocketMessageChannel channel, int radius, string text, out Result<object> result)
        {
            if (!IsConfigurableChannel(channel))
            {
                result = new Result<object>(false, null, "Channel not configurable");
                return false;
            }

            result = new Result<object>(true, null);
            return true;
        }

        private bool IsConfigurableChannel(ISocketMessageChannel channel)
        {
            if (channel is not ITextChannel textChannel)
            {
                return false;
            }

            return textChannel.Name.StartsWith(_settings.Value.ChannelPrefix);
        }
    }
}