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
using System.Collections.Generic;
using Sniffer.Static.Models;
using System.Linq;
using MoreLinq;
using Sniffer.Persistance.Model;

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
                settings.Add(item.DiscordChannelId, item.Radius, item.SystemId, item.KillType);
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

            List<ChannelRange> channelRanges = new();

            // TODO: send a message to discord, if the message matches our criteria.
            foreach (var channelSettings in _monitorSettings)
            {
                var killType = GetKillTypeForPackage(e.Package);

                if (channelSettings.Value.killType != KillType.All && channelSettings.Value.killType != killType)
                {
                    continue;
                }

                // determine if the kill was in range of any current watch
                var route = await _esiClient.GetRouteDataAsync(e.Package.killmail.solar_system_id, channelSettings.Value.systemId).ConfigureAwait(false);

                if (route == null)
                {
                    _logger.Debug("Route was null, {@Package}", e.Package);
                    continue;
                }

                var actualRange = route.Count - 1; // subtract the origin system
                if (actualRange > channelSettings.Value.radius)
                {
                    continue;
                }

                channelRanges.Add(new ChannelRange(channelSettings.Key, channelSettings.Value, actualRange));

                if (channelRanges.Count > 0)
                {
                    KillData killData = null;

                    try
                    {
                        killData = await GetKillDataFromPackage(e.Package);
                    }
                    catch (Exception)
                    {
                        //TODO: Send Fallback message with link to zkb.
                        return;
                    }

                    foreach (var channelRange in channelRanges)
                    {
                        // alert the channel
                        var channel = _discordClient.GetChannel(channelRange.ChannelKey);
                        if (channel is not null && channel is ITextChannel textChannel)
                        {
                            await SendKillMessage(textChannel, e.Package, killData, channelRange.Range, channelRange.Value);
                        }
                    }
                }
            }
        }

        private KillType GetKillTypeForPackage(Package package)
            => IsNPCOnlyKillMail(package) ? KillType.NPC : KillType.Player;

        private async Task<KillData> GetKillDataFromPackage(Package package)
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
            var killLocation = EveStaticDataProvider.Instance.SystemIds[package.killmail.solar_system_id];

            TypeID victimShip;
            if (IsNPCOnlyKillMail(package))
            {
                victimShip = EveStaticDataProvider.Instance.ShipIds.GetValueOrDefault(kmVictim.ship_type_id);

                return new KillData(await victimTask, await victimCorpTask, await victimAllianceTask, victimShip, killLocation);
            }

            var kmHighestDamage = package.killmail.attackers?
                .MaxBy(a => a.damage_done).FirstOrDefault();

            var killerTask = kmHighestDamage != null && kmHighestDamage.character_id != default
                ? _esiClient.GetCharacterDataAsync(kmHighestDamage.character_id)
                : Task.FromResult<CharacterData>(null);

            var killerCorpTask = kmHighestDamage != null && kmHighestDamage.corporation_id != default
                ? _esiClient.GetCorporationDataAsync(kmHighestDamage.corporation_id)
                : Task.FromResult<CorporationData>(null);

            var killerAllianceTask = kmHighestDamage != null && kmHighestDamage.alliance_id != default
                ? _esiClient.GetAllianceDataAsync(kmHighestDamage.alliance_id)
                : Task.FromResult<AllianceData>(null);

            var kmMostExpensiveAttacker = package.killmail.attackers?
                .MaxBy(a => GetShipValue(a.ship_type_id)).FirstOrDefault();

            var mostExpensiveAttackerShip = EveStaticDataProvider.Instance.ShipIds.GetValueOrDefault(kmMostExpensiveAttacker.ship_type_id);

            var victim = await victimTask;
            var victimCorp = await victimCorpTask;
            var victimAlliance = await victimAllianceTask;
            victimShip = EveStaticDataProvider.Instance.ShipIds.GetValueOrDefault(kmVictim.ship_type_id);

            var killer = await killerTask;
            var killerCorp = await killerCorpTask;
            var killerAlliance = await killerAllianceTask;
            var killerShip = EveStaticDataProvider.Instance.ShipIds.GetValueOrDefault(kmHighestDamage.ship_type_id);

            return new KillData(
                Victim: victim,
                VictimCorp: victimCorp,
                VictimAlliance: victimAlliance,
                VicimShip: victimShip,
                AttackerCount: package.killmail.attackers.Length,
                Killer: killer,
                KillerCorp: killerCorp,
                KillerAlliance: killerAlliance,
                KillerShip: killerShip,
                MostExpensiveAttackerShip: mostExpensiveAttackerShip,
                KillLocation: killLocation);
        }

        private bool IsNPCOnlyKillMail(Package package)
        {
            return package?.killmail?.attackers?.All(a => a.faction_id != null) ?? false;
        }

        private static decimal GetShipValue(int ship_type_id)
        {
            return EveStaticDataProvider.Instance.ShipIds.TryGetValue(ship_type_id, out var typeid)
                ? typeid.BasePrice
                : -1;
        }

        private async Task SendKillMessage(ITextChannel textChannel, Package package, KillData killData, int range, (int radius, int systemId, Persistance.Model.KillType killType) configuredSettings)
        {
            var message = new EmbedBuilder
            {
                Url = package.zkb.href,
                Timestamp = package.killmail.killmail_time,
            };

            if (killData.Victim != null)
            {
                message.AddField("Victim", MakeShipMarkdown(killData.Victim, killData.VictimCorp, killData.VictimAlliance, killData.VicimShip));
            }

            var killerText = killData.AttackerCount > 1
                ? "Most damage done by"
                : "Solo killed by";

            if (killData.Killer != null)
            {
                message.AddField(killerText, MakeShipMarkdown(killData.Killer, killData.KillerCorp, killData.KillerAlliance, killData.KillerShip));
            }

            if (killData.AttackerCount > 1)
            {
                if (killData.MostExpensiveAttackerShip != null)
                {
                    message.AddField("Most expensive attacker ship", killData.MostExpensiveAttackerShip.EnglishName);
                }
                message.AddField("Number of attackers", killData.AttackerCount);
            }

            if (package.zkb != null)
            {
                message.AddField("Time", $"{package.killmail.killmail_time:g}");

                var currency = string.Format(EveOnlineNumberFormat.IskNumberFormat, "{0:C0}", package.zkb.totalValue);
                message.AddField("Details", $"[Total value: {currency}](https://zkillboard.com/kill/{package.killID}/)");
            }

            var rangeJumps = range == 1 ? "jump" : "jumps";
            var radiusJumps = configuredSettings.radius == 1 ? "jump" : "jumps";

            var systemName = EveStaticDataProvider.Instance.SystemIds.GetValueOrDefault(configuredSettings.systemId);

            var systemText = new StringBuilder();

            systemText.AppendLine($"**{killData.KillLocation}**");
            systemText.AppendLine($"Range: {range} {rangeJumps} away.");

            if (systemName != null)
            {
                var text = $"Currently watching: {configuredSettings.radius} jumps from {systemName}";

                if (configuredSettings.killType == KillType.Player)
                {
                    text += ", player kills only.";
                }
                else if (configuredSettings.killType == KillType.NPC)
                {
                    text += ", only pure NPC kills.";
                }

                systemText.Append(text);
            }

            message.AddField("Solar system", systemText.ToString());

            await textChannel.SendMessageAsync(embed: message.Build());
        }

        private static string MakeShipMarkdown(CharacterData victim, CorporationData corp, AllianceData alliance, TypeID ship)
        {
            var sb = new StringBuilder();

            if (victim != null)
            {
                sb.Append("Name: ").Append('[').Append(victim.Name).Append("](https://zkillboard.com/character/").Append(victim.Id).AppendLine("/)");
            }

            if (corp != null)
            {
                sb.Append("Corp: ").Append('[').Append(corp.Name).Append("](https://zkillboard.com/corporation/").Append(corp.Id).Append("/)").Append('[').Append(corp.Ticker).Append(']');

                if (alliance != null)
                {
                    sb.AppendLine();
                    sb.Append("Alliance: ").Append('[').Append(alliance.Name).Append("](https://zkillboard.com/alliance/").Append(alliance.Id).Append("/)").Append(" [").Append(alliance.Ticker).Append("]");
                }
            }

            if (ship != null)
            {
                sb.AppendLine();
                sb.Append("Ship: ").AppendLine(ship.EnglishName);
            }

            return sb.ToString();
        }

        public bool TrySetChannelSettings(ISocketMessageChannel channel, int radius, string text, Persistance.Model.KillType killType, out Result<string> result)
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

            SetChannelSettings(channel.Id, radius, systemId, killType);

            result = new Result<string>(systemName, null);
            return true;
        }

        private void SetChannelSettings(ulong id, int radius, int systemId, KillType killType)
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

            _monitorSettings.Add(id, radius, systemId, killType);
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
