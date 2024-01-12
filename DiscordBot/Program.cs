using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using Microsoft.Extensions.Logging;

namespace DiscordBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //Setups
            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = "Enter own token here",
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents,
                MinimumLogLevel = LogLevel.Debug
            });
            var endpoint = new ConnectionEndpoint
            {
                Hostname = "ownserver", // From your server configuration.
                Port = "ownport" // From your server configuration
            };

            var lavalinkConfig = new LavalinkConfiguration
            {
                Password = "yourpassword", // From your server configuration.
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };
            
            discord.UseInteractivity(new InteractivityConfiguration() 
            { 
                PollBehaviour = PollBehaviour.DeleteEmojis,
                Timeout = TimeSpan.FromSeconds(10)
                
            });
            var lavalink = discord.UseLavalink();
            
            //Commands
            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { "!" }
            });
            commands.RegisterCommands<FirstCommands>();
            commands.RegisterCommands<MusicCommands>();
            
            await discord.ConnectAsync();
            await lavalink.ConnectAsync(lavalinkConfig);
            
            await Task.Delay(-1);
        }
    }
}