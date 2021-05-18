using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Threading.Tasks;

namespace Sniffer.Bot
{
    public static class DependencyInjectionExtensions
    {
        public static void AddDiscordBot(this IServiceCollection services)
        {
            var discordClient = new DiscordSocketClient();
            discordClient.Log += DiscordNetLog;
            services.AddSingleton(discordClient);

            services.AddHttpClient();

            var commandService = new CommandService();
            commandService.Log += DiscordNetLog;
            services.AddSingleton(commandService);

            services.AddSingleton<CommandHandler>();
            services.AddSingleton<DiscordBot>();
        }

        private static Task DiscordNetLog(LogMessage arg)
        {
            switch (arg.Severity)
            {
                case LogSeverity.Critical:
                    Log.Fatal(arg.Exception, arg.Message);
                    break;
                case LogSeverity.Error:
                    Log.Error(arg.Exception, arg.Message);
                    break;
                case LogSeverity.Warning:
                    Log.Warning(arg.Exception, arg.Message);
                    break;
                case LogSeverity.Info:
                    Log.Information(arg.Exception, arg.Message);
                    break;
                case LogSeverity.Debug:
                    Log.Debug(arg.Exception, arg.Message);
                    break;
            }

            return Task.CompletedTask;
        }
    }
}
