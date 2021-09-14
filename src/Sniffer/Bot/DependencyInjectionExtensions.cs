using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.Commands.Extensions;
using Remora.Commands.Extensions;
using Sniffer.Bot.Commands;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Hosting.Options;
using Remora.Extensions.Options.Immutable;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Remora.Discord.Hosting.Services;
using Microsoft.Extensions.Hosting;
using System;

namespace Sniffer.Bot
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddDiscordBot(this IServiceCollection services, Action<DiscordBotSettings> setupAction)
        {
            var settings = new DiscordBotSettings();
            setupAction?.Invoke(settings);

            services.AddHttpClient();

            services.Configure(() => new DiscordServiceOptions());

            services
                .AddDiscordGateway(_ => settings.DiscordToken)
                .AddDiscordCommands()
                .AddCommandGroup<SnifferCommandGroup>();

            services.TryAddSingleton<DiscordService>();

            services.AddSingleton<IHostedService, DiscordService>(serviceProvider =>
                serviceProvider.GetRequiredService<DiscordService>());

            return services;
        }
    }
}
