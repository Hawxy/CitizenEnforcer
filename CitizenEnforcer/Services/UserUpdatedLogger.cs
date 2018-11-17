using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenEnforcer.Context;
using CitizenEnforcer.Models;
using Discord;
using Discord.WebSocket;
using EFSecondLevelCache.Core;
using Microsoft.EntityFrameworkCore;

namespace CitizenEnforcer.Services
{
    public class UserUpdatedLogger
    {
        private readonly BotContext _botContext;
        private readonly DiscordSocketClient _client;
        public UserUpdatedLogger(BotContext botContext, DiscordSocketClient client)
        {
            _botContext = botContext;
            _client = client;
            client.UserUpdated += HandleUserUpdated;
        }

        private async Task HandleUserUpdated(SocketUser before, SocketUser after)
        {
            if (before.Username == after.Username && before.AvatarId == after.AvatarId) return;
            IReadOnlyCollection<SocketGuild> guilds = before.MutualGuilds;

            foreach (var guild in guilds)
            {
                Guild dbGuild = await _botContext.Guilds.AsNoTracking().SingleOrDefaultAsync(x => x.GuildId == guild.Id && x.IsEditLoggingEnabled);
                if (dbGuild == null) continue;

                SocketChannel channel = _client.GetChannel(dbGuild.LoggingChannel);
                if (channel is SocketTextChannel loggingChannel)
                {
                    if (before.Username != after.Username)
                    {
                        await loggingChannel.SendMessageAsync("**Username Changed**\n" +
                                                              $"```UserID: {before.Id}\n" +
                                                              $"{before} -> {after}```");
                    }

                    if (before.AvatarId != after.AvatarId)
                    {
                        string beforeAvatar = before.GetAvatarUrl();
                        string afterAvatar = after.GetAvatarUrl();

                        var builder =
                            new EmbedBuilder()
                                .WithColor(new Color(2, 136, 209))
                                .WithAuthor(after)
                                .WithCurrentTimestamp()
                                .WithTitle($"Avatar Updated")
                                .WithDescription("New avatar below")
                                .WithThumbnailUrl(beforeAvatar)
                                .WithImageUrl(afterAvatar);

                        await loggingChannel.SendMessageAsync(embed: builder.Build());
                    }
                }
            }
        }
    }
}
