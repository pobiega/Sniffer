using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Serilog;
using Sniffer.KillBoard.ZKill;
using Sniffer.Persistance;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sniffer.Static;

namespace Sniffer.KillBoard
{
    public record Result<T>(bool IsSuccess, T Value, string ErrorReason = null);

    // TODO: rename this? its more of a manager type class at this point
    public class KillBoardMonitor
    {
        private readonly ILogger _logger;
        private readonly IOptions<KillBoardMonitorSettings> _settings;

        private MonitorSettings _monitorSettings;
        private readonly IZKillProcessingService _zKillProcessingService;
        private readonly IServiceProvider _serviceProvider;

        public KillBoardMonitor(
            ILogger logger,
            IOptions<KillBoardMonitorSettings> settings,
            IServiceProvider serviceProvider,
            IZKillProcessingService zKillProcessingService)
        {
            _logger = logger;
            _settings = settings;
            _zKillProcessingService = zKillProcessingService;
            _serviceProvider = serviceProvider;
        }

        public Task Initialize()
        {
            _logger.Debug("Initializing KillBoardMonitor...");

            _zKillProcessingService.PackageArrived += async (s, e) => await OnPackageArrived(s, e);

            _monitorSettings = LoadSettingsFromDatabase();

            return Task.CompletedTask;
        }

        private MonitorSettings LoadSettingsFromDatabase()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<SnifferDbContext>();

            var settings = new MonitorSettings();

            foreach (var item in dbContext.ChannelConfigurations)
            {
                settings.Add(item.DiscordChannelId, item.Radius, item.SystemId);
            }

            return settings;
        }

        private async Task OnPackageArrived(object sender, PackageArrivedEventArgs e)
        {
            // TODO: send a message to discord, if the message matches our criteria.
        }

        public bool TrySetChannelSettings(ISocketMessageChannel channel, int radius, string text, out Result<object> result)
        {
            // TODO: revisit this.

            _ = channel ?? throw new ArgumentNullException(nameof(channel));

            if (!IsConfigurableChannel(channel))
            {
                result = new Result<object>(false, null, "Channel not configurable.");
                return false;
            }

            if (!EveStaticDataProvider.Instance.TryGetSystemIdByName(text, out var systemId))
            {
                result = new Result<object>(false, null, "No system by that name found.");
                return false;
            }

            SetChannelSettings(channel.Id, radius, systemId);

            result = new Result<object>(true, null);
            return true;
        }

        private void SetChannelSettings(ulong id, int radius, int systemId)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<SnifferDbContext>();

            var existing = dbContext.ChannelConfigurations.Find(id);

            if (existing == null)
            {
                dbContext.ChannelConfigurations.Add(new Persistance.Entities.ChannelConfiguration()
                {
                    DiscordChannelId = id,
                    Radius = radius,
                    SystemId = systemId
                });
            }
            else
            {
                existing.Radius = radius;
                existing.SystemId = systemId;
            }

            // TODO: if this fails, we should let the user know
            dbContext.SaveChanges();

            _monitorSettings.Add(id, radius, systemId);
        }

        private bool IsConfigurableChannel(ISocketMessageChannel channel)
        {

            if (channel is not ITextChannel textChannel)
            {
                return false;
            }

            return textChannel.Name.StartsWith(_settings.Value.ChannelPrefix, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
