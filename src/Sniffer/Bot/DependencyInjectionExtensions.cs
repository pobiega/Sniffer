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

namespace Sniffer.Bot
{
    public static class DependencyInjectionExtensions
    {
        public static void AddDiscordBot(this IServiceCollection services)
        {
            var token = "ODQzOTQyMTIyNDUzMjA1MDMz.YKLMWQ.5NPYflKG54akg__NR1S91CVGhLM";

            services.AddHttpClient();

            services.Configure(() => new DiscordServiceOptions());

            services
                .AddDiscordGateway(_ => token)
                .AddDiscordCommands()
                .AddCommandGroup<SnifferCommandGroup>();

            services.TryAddSingleton<DiscordService>();

            services.AddSingleton<IHostedService, DiscordService>(serviceProvider =>
                serviceProvider.GetRequiredService<DiscordService>());

        }
    }
}
