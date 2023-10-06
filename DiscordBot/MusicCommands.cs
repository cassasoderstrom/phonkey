using System.Runtime.InteropServices.JavaScript;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;

namespace DiscordBot
{ 
    public class MusicCommands : BaseCommandModule
    {
        
        private Queue<LavalinkTrack> songQueue = new Queue<LavalinkTrack>();
        
        private bool isPlaying;
        private bool playerHandler;
        private bool allowedPlay = false;                
        
        private LavalinkGuildConnection? conn;
        private LavalinkNodeConnection? node;
        private LavalinkExtension? lava;
        private DiscordChannel? channel;

        public async Task musicPoll(CommandContext ctx, LavalinkTrack track )
        {
            DiscordEmoji[] emojis =
                { DiscordEmoji.FromName(ctx.Client, ":+1:"), DiscordEmoji.FromName(ctx.Client, ":-1:") };
            var interactivity = ctx.Client.GetInteractivity();
            var pollEmbed = new DiscordEmbedBuilder()
            {
                Title = $"**{track.Title}** requested by {ctx.Member?.DisplayName}?",
                Description = "Want to hear it?",
                Color = DiscordColor.Green,
                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                {
                    Url = $"https://avatar.glue-bot.xyz/youtube-thumbnail/q?url={track.Uri}"
                } 
            };

            var pollMessage = await ctx.Channel.SendMessageAsync(embed: pollEmbed).ConfigureAwait(false);

            await pollMessage.CreateReactionAsync(emojis[0]).ConfigureAwait(false);
            await pollMessage.CreateReactionAsync(emojis[1]).ConfigureAwait(false);

            var result = await interactivity.DoPollAsync(pollMessage, emojis, 0, TimeSpan.FromSeconds(10));

            if (result[0].Total < result[1].Total && result[0].Emoji.Equals(DiscordEmoji.FromName(ctx.Client, ":+1:")))
            {
                await ctx.Channel.SendMessageAsync("Vote failed");
                if (!isPlaying)
                {
                    await Leave(ctx);
                }
                
            }
            else if (result[0].Total > result[1].Total &&
                     result[0].Emoji.Equals(DiscordEmoji.FromName(ctx.Client, ":+1:")))
            {
                allowedPlay = true;
                await ctx.Channel.SendMessageAsync("Vote succeeded!");
                
            }
            else if (result[0].Total < result[1].Total && result[0].Emoji.Equals(DiscordEmoji.FromName(ctx.Client, ":-1:")))
            {
                allowedPlay = true;
                await ctx.Channel.SendMessageAsync("Vote succeeded!");
            }
            else if (result[0].Total > result[1].Total &&
                     result[0].Emoji.Equals(DiscordEmoji.FromName(ctx.Client, ":-1:")))
            {
                await ctx.Channel.SendMessageAsync("Vote failed");
                if (!isPlaying)
                {
                    await Leave(ctx);
                }
            }
            else
            {
                await ctx.Channel.SendMessageAsync("Vote failed");
                if (!isPlaying)
                {
                    await Leave(ctx);
                }
            }
           

        }
        [Command ("play")]
        public async Task Play(CommandContext ctx, [RemainingText] string search)
        {
            //Checks if user is admin in our server
            var hasRequiredRole = ctx.Member.Roles.Any(role => role.Name == "tude som tar hand om servern");

            
            
            //Checks if user is connected to a voice channel
            if (ctx.Member?.VoiceState == null || ctx.Member.VoiceState.Channel == null!)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }
            channel = ctx.Member.VoiceState.Channel;
            lava = ctx.Client.GetLavalink();
            
            //Checks if lavalink is running
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("The Lavalink connection is not established");
                return;
            }
            node = lava.ConnectedNodes.Values.First();
            
            if (channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("Not a valid voice channel.");
                return;
            }
            
            await node.ConnectAsync(channel);
            conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }
            
            LavalinkLoadResult loadResult;
            
            if (search.StartsWith("https://"))
            {
                
                loadResult = await node.Rest.GetTracksAsync(new Uri(search));
            }
            else
            {
                loadResult = await node.Rest.GetTracksAsync(search);
            }

            if (loadResult.LoadResultType is LavalinkLoadResultType.LoadFailed or LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync($"Track search failed for {search}.");
                return;
            }

            var track = loadResult.Tracks.First();
            
            if (!hasRequiredRole)
            {
                await musicPoll(ctx, track);
                if (!allowedPlay)
                {
                    return;
                }
                
            }
            if (!playerHandler)
            {
                node.PlaybackFinished += async (sender, args) => { await PlayNextInQueue(ctx); };
                playerHandler = true;
            }
            if (!isPlaying)
            {
                allowedPlay = false;
                isPlaying = true;
                await conn.PlayAsync(track);
                await ctx.RespondAsync($"Now playing {track.Title} in {channel.Name}!");
                
            }
            else
            {
                // Enqueue the requested track
                
                songQueue.Enqueue(track);
                await ctx.RespondAsync($"{track.Title} has been added to the queue.");
                
            }

            

        }
        private async Task PlayNextInQueue(CommandContext ctx)
        {
            if (conn != null)
            {
                if (songQueue.Count > 0)
                {
                    var nextTrack = songQueue.Dequeue();
                    await conn.PlayAsync(nextTrack);
                    await ctx.RespondAsync($"Now playing {nextTrack.Title} in {channel?.Name}!");
                }
                else
                {
                    isPlaying = false;
                    await conn.DisconnectAsync();
                    await ctx.RespondAsync("Queue empty, bye bye!");
                }
            }
        }
        
        [Command("skip")]
        public async Task Skip(CommandContext ctx)
        {
            conn = node?.GetGuildConnection(ctx.Member.VoiceState.Guild);
            if (conn == null)
            {
                await ctx.RespondAsync("The bot is not in a voice channel.");
                return;
            }
            
            // Skip the current track and play the next one
            
            if (songQueue.Count > 0)
            {
                await ctx.RespondAsync("Skipped to the next song.");
                await conn.StopAsync();
                
            }
            else
            {
                isPlaying = false;
                await conn.DisconnectAsync();
                await ctx.RespondAsync("Queue empty, bye bye!");
            }
            
        }
        
        [Command("queue")]
        public async Task Queue(CommandContext ctx)
        {
            if (songQueue.Count == 0)
            {
                await ctx.RespondAsync("The queue is empty.");
            }
            else
            {
                var queueList = songQueue.Select((track, index) => $"{index + 1}. {track.Title}");
                await ctx.RespondAsync($"**Queue:**\n{string.Join("\n", queueList)}");
            }
        }
        
        
        [Command ("leave")]
        public async Task Leave(CommandContext ctx)
        {
            channel = ctx.Member?.VoiceState.Channel;
            
            lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("The Lavalink connection is not established");
                return;
            }
            if (channel?.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("Not a valid voice channel.");
                return;
            }
            conn = node?.GetGuildConnection(ctx.Member.VoiceState.Guild);
            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }

            isPlaying = false;
            songQueue.Clear();
            await conn.DisconnectAsync();
            await ctx.RespondAsync($"Left {channel.Name}!");
        }
        
        
    }
}