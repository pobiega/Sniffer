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
using Sniffer.Data;
using System.Text;
using Sniffer.Data.ESI.Models;

namespace Sniffer.KillBoard
{
    public record Result<T>(T Value, string ErrorMessage);

    // TODO: rename this? its more of a manager type class at this point
    public class KillBoardMonitor
    {
        private readonly ILogger _logger;
        private readonly IOptions<KillBoardMonitorSettings> _settings;

        private MonitorSettings _monitorSettings;
        private readonly IZKillProcessingService _zKillProcessingService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IESIClient _esiClient;
        private readonly DiscordSocketClient _discordClient;

        public KillBoardMonitor(
            ILogger logger,
            IOptions<KillBoardMonitorSettings> settings,
            IServiceProvider serviceProvider,
            IZKillProcessingService zKillProcessingService,
            IESIClient esiClient,
            DiscordSocketClient discordClient
            )
        {
            _logger = logger;
            _settings = settings;
            _zKillProcessingService = zKillProcessingService;
            _serviceProvider = serviceProvider;
            _esiClient = esiClient;
            _discordClient = discordClient;
        }

        public Task Initialize()
        {
            _logger.Debug("Initializing KillBoardMonitor...");

            _monitorSettings = LoadSettingsFromDatabase();

            _zKillProcessingService.PackageArrived += async (s, e) => await OnPackageArrived(s, e);

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
            if (_monitorSettings is null)
            {
                return;
            }

            if (_discordClient.ConnectionState != ConnectionState.Connected)
            {
                return;
            }

            // TODO: send a message to discord, if the message matches our criteria.

            foreach (var channelSettings in _monitorSettings)
            {
                // determine if the kill was in range of any current watch
                var route = await _esiClient.GetRouteDataAsync(e.Package.killmail.solar_system_id, channelSettings.Value.systemId).ConfigureAwait(false);

                if (route == null)
                {
                    _logger.Debug("Route was null, {@Package}", e.Package);
                    continue;
                }

                var range = route.Count - 1; // subtract the origin system
                if (range <= channelSettings.Value.radius)
                {
                    // alert the channel
                    var channel = _discordClient.GetChannel(channelSettings.Key);
                    if (channel is not null && channel is ITextChannel textChannel)
                    {
                        await SendKillMessage(textChannel, e.Package, range);
                    }
                }
            }
        }

        private async Task SendKillMessage(ITextChannel textChannel, Package package, int range)
        {
            var kmVictim = package.killmail.victim;

            var victimTask = kmVictim.character_id != default
                ? _esiClient.GetCharacterDataAsync(kmVictim.character_id)
                : Task.FromResult<CharacterData>(null);

            var victimCorpTask = kmVictim.corporation_id != default
                ? _esiClient.GetCorporationDataAsync(kmVictim.corporation_id)
                : Task.FromResult<CorporationData>(null);

            var victimAllianceTask = kmVictim.alliance_id != default
                ? _esiClient.GetAllianceDataAsync(kmVictim.alliance_id)
                : Task.FromResult<AllianceData>(null);

            var kmKiller = package.killmail.attackers != null
                ? package.killmail.attackers[0]
                : null;

            var killerTask = kmKiller != null && kmKiller.character_id != default
                ? _esiClient.GetCharacterDataAsync(kmKiller.character_id)
                : Task.FromResult<CharacterData>(null);

            var killerCorpTask = kmKiller != null && kmKiller.corporation_id != default
                ? _esiClient.GetCorporationDataAsync(kmKiller.corporation_id)
                : Task.FromResult<CorporationData>(null);

            var killerAllianceTask = kmKiller != null && kmKiller.alliance_id != default
                ? _esiClient.GetAllianceDataAsync(kmKiller.alliance_id)
                : Task.FromResult<AllianceData>(null);

            var victim = await victimTask;
            var victimCorp = await victimCorpTask;
            var victimAlliance = await victimAllianceTask;

            var killer = await killerTask;
            var killerCorp = await killerCorpTask;
            var killerAlliance = await killerAllianceTask;

            var killLocation = EveStaticDataProvider.Instance.SystemIds[package.killmail.solar_system_id];

            var message = new EmbedBuilder
            {
                Url = package.zkb.href,
                Timestamp = package.killmail.killmail_time,
            };

            if (victim != null)
            {
                message.AddField("Victim", MakeShipMarkdown(victim, victimCorp, victimAlliance));
            }

            if (kmKiller != null)
            {
                message.AddField("Final Blow", MakeShipMarkdown(killer, killerCorp, killerAlliance));
            }

            if (package.zkb != null)
            {
                var currency = string.Format(EveOnlineNumberFormat.IskNumberFormat, "{0:C0}", package.zkb.totalValue);
                message.AddField("Details", $"Total value: {currency}");
            }

            var jumps = range == 1 ? "jump" : "jumps";
            message.AddField("Solar system", $"**{killLocation}**\nRange: {range} {jumps} away.");

            await textChannel.SendMessageAsync(embed: message.Build());
        }

        private static string MakeShipMarkdown(CharacterData victim, CorporationData corp, AllianceData alliance)
        {
            var sb = new StringBuilder();

            if (victim != null)
            {
                sb.Append("Name: ").AppendLine(victim.Name);
            }

            if (corp != null)
            {
                sb.Append("Corp: ").Append(corp.Name).Append(" [").Append(corp.Ticker).AppendLine("]");
            }

            if (alliance != null)
            {
                sb.Append("Alliance: ").Append(alliance.Name).Append(" [").Append(alliance.Ticker).AppendLine("]");
            }

            return sb.ToString();
        }

        public bool TrySetChannelSettings(ISocketMessageChannel channel, int radius, string text, out Result<string> result)
        {
            // TODO: revisit this.

            _ = channel ?? throw new ArgumentNullException(nameof(channel));

            if (!IsConfigurableChannel(channel))
            {
                result = new Result<string>(null, "Channel not configurable.");
                return false;
            }

            if (!EveStaticDataProvider.Instance.TryGetSystemIdByName(text, out var systemId, out var systemName))
            {
                result = new Result<string>(null, "No system by that name found.");
                return false;
            }

            SetChannelSettings(channel.Id, radius, systemId);

            result = new Result<string>(systemName, null);
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
