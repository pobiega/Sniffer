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
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Sniffer.Persistance.Model;
using MoreLinq;
using System.Linq;
using Remora.Discord.Core;
using Remora.Results;
using Sniffer.KillBoard.Errors;

namespace Sniffer.KillBoard
{
    // TODO: rename this? its more of a manager type class at this point
    public class KillBoardMonitor
    {
        private readonly ILogger _logger;
        private readonly IOptions<KillBoardMonitorSettings> _settings;

        private MonitorSettings _monitorSettings;
        private readonly IZKillProcessingService _zKillProcessingService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IESIClient _esiClient;
        private readonly IDiscordRestChannelAPI _channelAPI;

        public KillBoardMonitor(
            ILogger logger,
            IOptions<KillBoardMonitorSettings> settings,
            IServiceProvider serviceProvider,
            IZKillProcessingService zKillProcessingService,
            IESIClient esiClient,
            IDiscordRestChannelAPI channelAPI
            )
        {
            _logger = logger;
            _settings = settings;
            _zKillProcessingService = zKillProcessingService;
            _serviceProvider = serviceProvider;
            _esiClient = esiClient;
            _channelAPI = channelAPI;
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
                        var channel = await _channelAPI.GetChannelAsync(new Remora.Discord.Core.Snowflake(channelRange.ChannelKey));

                        // alert the channel
                        if (channel.IsSuccess && channel.Entity.Type == ChannelType.GuildText)
                        {
                            await SendKillMessage(channel.Entity, e.Package, killData, channelRange.Range, channelRange.Value);
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

        private async Task SendKillMessage(IChannel channel, Package package, KillData killData, int range, (int radius, int systemId, Persistance.Model.KillType killType) configuredSettings)
        {
            List<EmbedField> fields = new List<EmbedField>();

            if (killData.Victim != null)
            {
                fields.Add(new EmbedField("Victim", MakeShipMarkdown(killData.Victim, killData.VictimCorp, killData.VictimAlliance, killData.VicimShip)));
            }

            var killerText = killData.AttackerCount > 1
                ? "Most damage done by"
                : "Solo killed by";

            if (killData.Killer != null)
            {
                fields.Add(new EmbedField("Final Blow", MakeShipMarkdown(killData.Killer, killData.KillerCorp, killData.KillerAlliance, killData.KillerShip)));
            }

            if (killData.AttackerCount > 1)
            {
                if (killData.MostExpensiveAttackerShip != null)
                {
                    fields.Add(new EmbedField("Most expensive attacker ship", killData.MostExpensiveAttackerShip.EnglishName));
                }
                fields.Add(new EmbedField("Number of attackers", killData.AttackerCount.ToString()));

            }

            if (package.zkb != null)
            {
                fields.Add(new EmbedField("Time", $"{package.killmail.killmail_time:g}"));

                var currency = string.Format(EveOnlineNumberFormat.IskNumberFormat, "{0:C0}", package.zkb.totalValue);
                fields.Add(new EmbedField("Details", $"[Total value: {currency}](https://zkillboard.com/kill/{package.killID}/)"));
            }

            var rangeJumps = range == 1 ? "jump" : "jumps";
            var radiusJumps = configuredSettings.radius == 1 ? "jump" : "jumps";

            var systemName = EveStaticDataProvider.Instance.SystemIds.GetValueOrDefault(configuredSettings.systemId);

            var systemText = new StringBuilder();

            systemText.AppendLine($"**{killData.KillLocation}**");
            systemText.AppendLine($"Range: {range} {rangeJumps} away.");

            if (systemName != null)
            {
                var text = $"Currently watching: {configuredSettings.radius} {radiusJumps} from {systemName}";

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

            fields.Add(new EmbedField("Solar system", systemText.ToString()));

            var embed = new Embed
            {
                Url = package.zkb.href,
                Timestamp = package.killmail.killmail_time,
                Fields = fields
            };

            await _channelAPI.CreateMessageAsync(channel.ID, embeds: new[] { embed });
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

        public async Task<Result<string>> TrySetChannelSettings(Snowflake channelID, int radius, string text, Persistance.Model.KillType killType)
        {
            // TODO: revisit this.

            var channel = await _channelAPI.GetChannelAsync(channelID);

            if (!channel.IsSuccess)
            {
                return Result<string>.FromError(new NotFoundError("No channel with that ID found."), channel);
            }

            if (!IsConfigurableChannel(channel.Entity))
            {
                return Result<string>.FromError(new NotConfigurableError("That channel is not configurable for kill monitoring."));
            }

            if (!EveStaticDataProvider.Instance.TryGetSystemIdByName(text, out var systemId, out var systemName))
            {
                return Result<string>.FromError(new NotFoundError("No system has that name."));
            }

            SetChannelSettings(channel.Entity.ID.Value, radius, systemId, killType);

            return Result<string>.FromSuccess(systemName);
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

        private bool IsConfigurableChannel(IChannel channel)
        {
            if (channel.Type != ChannelType.GuildText || !channel.Name.HasValue)
            {
                return false;
            }

            return channel.Name.Value.StartsWith(_settings.Value.ChannelPrefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}
