#region License
/*CitizenEnforcer - Moderation and logging bot
Copyright(C) 2018-2020 Hawx
https://github.com/Hawxy/CitizenEnforcer

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.If not, see http://www.gnu.org/licenses/ */
#endregion

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CitizenEnforcer.Context;
using CitizenEnforcer.Models;
using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace CitizenEnforcer.Services
{
    public class UserUpdatedLogger : InitializedService
    {
        private readonly BotContext _botContext;
        private readonly DiscordSocketClient _client;
        public UserUpdatedLogger(BotContext botContext, DiscordSocketClient client)
        {
            _botContext = botContext;
            _client = client;
        }

        public override Task InitializeAsync(CancellationToken cancellationToken)
        {
            _client.UserUpdated += HandleUserUpdated;
            return Task.CompletedTask;
        }

        private async Task HandleUserUpdated(SocketUser before, SocketUser after)
        {
            if (after.IsBot 
                || string.IsNullOrEmpty(before.Username) 
                || before.Username == after.Username 
                && before.AvatarId == after.AvatarId)
                return;

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
