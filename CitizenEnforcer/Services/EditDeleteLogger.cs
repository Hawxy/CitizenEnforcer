using System.Linq;
using System.Threading.Tasks;
using CitizenEnforcer.Common;
using CitizenEnforcer.Context;
using CitizenEnforcer.Settings;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CitizenEnforcer.Services
{
    public class EditDeleteLogger
    {
        private readonly BotContext _botContext;
        private readonly Configuration _configuration;
        private readonly IMemoryCache _banCache;
        public EditDeleteLogger(BotContext botContext, Configuration configuration, DiscordSocketClient client, IMemoryCache memoryCache)
        {
            _botContext = botContext;
            _configuration = configuration;
            _banCache = memoryCache;
            client.MessageDeleted += (cacheable, channel) => GenericMessageEvent(cacheable, channel);
            client.MessageUpdated += (cacheable, message, channel) => GenericMessageEvent(cacheable, channel, message);
        }
        private async Task GenericMessageEvent(Cacheable<IMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketMessage currentMessage = null)
        {
            var message = cachedMessage.Value;
            //if the message isn't cached, the author isn't a user, its a bot command, or the user is in the ban cache then don't do anything
            if (message == null || message.Source != MessageSource.User || message.Content.StartsWith(_configuration.Prefix) || _banCache.Get(message.Author.Id) != null)
                return;
            //don't bother doing anything if the message is a PM
            if (channel is SocketGuildChannel guildchannel)
            {
                //verify the guild is registered and has logging enabled
                var guild = await _botContext.Guilds.Include(x=> x.RegisteredChannels).AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guildchannel.Guild.Id && x.IsEditLoggingEnabled);

                //verify the channel is set to be logged
                // ReSharper disable once SimplifyLinqExpression
                if (guild == null || !guild.RegisteredChannels.Any(x=> x.ChannelId == channel.Id))
                    return;

                //get the logging channel
                var loggingchannel = guildchannel.Guild.GetTextChannel(guild.LoggingChannel);
                //if currentMessage is null then its a deletion event
                if (currentMessage == null)
                {
                    await loggingchannel.SendMessageAsync("**Message Deleted**\n" +
                                                          $"```Author: {FormatUtilities.GetFullName(message.Author)} | {message.Author.Id}\n" +
                                                          $"Posted at: {message.Timestamp.DateTime} UTC in #{channel.Name}\n" +
                                                          $"Content: {(string.IsNullOrWhiteSpace(message.Content) ? "None" : message.Content)}\n" +
                                                          $"Attachment: {message.Attachments.ElementAtOrDefault(0)?.Url ?? "None"}```");
                }
                //otherwise its a editing event
                else
                {
                    //prevent embeds from causing a pointless log
                    if (currentMessage.Content == message.Content) return;
                    await loggingchannel.SendMessageAsync("**Message Edited**\n" +
                                                          $"```Author: {FormatUtilities.GetFullName(message.Author)} | {message.Author.Id}\n" +
                                                          $"Posted at: {message.Timestamp.DateTime} UTC in #{channel.Name}\n" +
                                                          $"Original Content: {(string.IsNullOrWhiteSpace(message.Content) ? "None" : message.Content)}\n" +
                                                          $"Updated Content: {(string.IsNullOrWhiteSpace(currentMessage.Content) ? "None" : currentMessage.Content)}\n" +
                                                          $"Attachment: {message.Attachments.ElementAtOrDefault(0)?.Url ?? "None"}```");
                }
            }
        }
    }
}
