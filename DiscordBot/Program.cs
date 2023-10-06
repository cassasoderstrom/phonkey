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
                Token = "MTE1MzY1NzQxNTEyOTExNjc4Mw.GM3r5U.4W5HWP7GHEon4i49ASTgfrq8rOfW_kTO2XYD1Q",
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents,
                MinimumLogLevel = LogLevel.Debug
            });
            var endpoint = new ConnectionEndpoint
            {
                Hostname = "127.0.0.1", // From your server configuration.
                Port = 2333 // From your server configuration
            };

            var lavalinkConfig = new LavalinkConfiguration
            {
                Password = "youshallnotpass", // From your server configuration.
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };
            
            discord.UseInteractivity(new InteractivityConfiguration() 
            { 
                PollBehaviour = PollBehaviour.KeepEmojis,
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