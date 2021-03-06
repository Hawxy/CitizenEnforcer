﻿#region License
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenEnforcer.Common;
using CitizenEnforcer.Context;
using CitizenEnforcer.Preconditions;
using CitizenEnforcer.Services;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Interactivity;
using Microsoft.Extensions.Caching.Memory;

namespace CitizenEnforcer.Modules
{
    [RequireContext(ContextType.Guild)]
    [RequireInitialized(InitializedType.All)]
    [RequireBotPermission(GuildPermission.ManageRoles | GuildPermission.ManageChannels | GuildPermission.SendMessages)]
    public class SecurityModule : ModuleBase<SocketCommandContext>
    {
        private readonly InteractivityService _interactive;
        private readonly IMemoryCache _banCache;
        private readonly ModerationService _moderationService;
        private readonly BotContext _context;

        public SecurityModule(InteractivityService interactive, IMemoryCache banCache, ModerationService moderationService, BotContext context)
        {
            _interactive = interactive;
            _banCache = banCache;
            _moderationService = moderationService;
            _context = context;
        }

        [Command("lockdown")]
        [Alias("panic")]
        [Summary("Prevents non-role users from speaking server-wide")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task Lockdown()
        {
            var role = Context.Guild.EveryoneRole;
            if (role.Permissions.SendMessages)
            {
                var perms = role.Permissions.Modify(sendMessages: false);
                await role.ModifyAsync(x => x.Permissions = perms);

                var pubEmbed = SecurityFormats.GetPublicLockdownBuilder(Context.User);
                await _moderationService.SendMessageToAnnounce(_context, Context.Guild, embed: pubEmbed);

                var logEmbed = SecurityFormats.GetLockdownBuilder(Context.User);
                await _moderationService.SendEmbedToModLog(_context, Context.Guild, logEmbed);
            }
            else
            {
                var perms = role.Permissions.Modify(sendMessages: true);
                await role.ModifyAsync(x => x.Permissions = perms);

                var pubEmbed = SecurityFormats.GetPublicLiftedBuilder(Context.User);
                await _moderationService.SendMessageToAnnounce(_context, Context.Guild, embed: pubEmbed);

                var logEmbed = SecurityFormats.GetLiftedBuilder(Context.User);
                await _moderationService.SendEmbedToModLog(_context, Context.Guild, logEmbed);
            }
            
        }
        [Command("freeze")]
        [Alias("lock")]
        [Summary("Prevents non-role users from speaking in a channel")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task Freeze(SocketTextChannel mentionedChannel = null)
        {
            var role = Context.Guild.EveryoneRole;
            if (!role.Permissions.SendMessages)
            {
                _interactive.DelayedSendMessageAndDeleteAsync(Context.Channel, text: "Unable to freeze channel: Server currently locked down", deleteDelay: TimeSpan.FromSeconds(15));
                return;
            }

            var channel = mentionedChannel ?? Context.Channel as SocketTextChannel;

            var channelRole = channel.GetPermissionOverwrite(role);
            if (!channelRole.HasValue || channelRole.Value.SendMessages == PermValue.Allow)
            {
                await CheckSelfPermissionsAsync(channel);
                //Freeze channel
                await channel.AddPermissionOverwriteAsync(role, new OverwritePermissions(sendMessages: PermValue.Deny));

                var pubEmbed = SecurityFormats.GetPublicFreezeBuilder(Context.User);
                await channel.SendMessageAsync(embed: pubEmbed.Build());

                var logEmbed = SecurityFormats.GetLoggedFreezeBuilder(Context.User, channel);
                await _moderationService.SendEmbedToModLog(_context, Context.Guild, logEmbed);

            }
            else if (channelRole.Value.SendMessages == PermValue.Deny)
            {
                //Unfreeze channel
                await channel.AddPermissionOverwriteAsync(role, new OverwritePermissions(sendMessages: PermValue.Allow));

                var embed = SecurityFormats.GetUnfrozenBuilder(Context.User);
                await channel.SendMessageAsync(embed: embed.Build());
                await _moderationService.SendEmbedToModLog(_context, Context.Guild, embed);

            }
        }

        //Make sure we don't block ourselves from sending messages
        private async Task CheckSelfPermissionsAsync(SocketTextChannel channel)
        {
            var guildChannel = channel as SocketGuildChannel;
            var perms = guildChannel.GetPermissionOverwrite(Context.Client.CurrentUser);
            if (!perms.HasValue || perms.Value.SendMessages != PermValue.Allow)
            {
                await guildChannel.AddPermissionOverwriteAsync(Context.Client.CurrentUser, new OverwritePermissions(sendMessages: PermValue.Allow));
            }
        }

        [Command("mute")]
        [Summary("Mutes a user guild-wide")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task MuteUser([ErrorPrevent]SocketGuildUser user)
        {
            var muteRole = Context.Guild.Roles.SingleOrDefault(x => x.Name.Equals("Muted", StringComparison.OrdinalIgnoreCase) && !x.Permissions.SendMessages);
            if (muteRole == null)
            {
                _interactive.DelayedSendMessageAndDeleteAsync(Context.Channel, text: "Unable to mute/unmute user: Muted role does not exist within guild or does not deny the ability to speak", deleteDelay: TimeSpan.FromSeconds(15));
                return;
            }

            if (user.Roles.Any(x => x.Name.Equals("Muted", StringComparison.OrdinalIgnoreCase)))
            {
                await user.RemoveRoleAsync(muteRole);
                await _moderationService.SendMessageToAnnounce(_context, Context.Guild, $"***User {user} has been unmuted***");
                await _moderationService.SendEmbedToModLog(_context, Context.Guild, SecurityFormats.GetUserUnMuteLoggedBuilder(user, Context.User));
            }
            else
            {
                await user.AddRoleAsync(muteRole);
                await _moderationService.SendMessageToAnnounce(_context, Context.Guild, $"***User {user} has been globally muted***");
                await _moderationService.SendEmbedToModLog(_context, Context.Guild, SecurityFormats.GetUserMuteLoggedBuilder(user, Context.User));
            }
        }

        [Command("purge")]
        [Alias("clean")]
        [Summary("Used to clean up a channel")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Purge (int count = 10, DeleteType deleteType = DeleteType.All)
        {
            if (count > 100)
            {
                _interactive.DelayedSendMessageAndDeleteAsync(Context.Channel, text: "Count should be less than 100", deleteDelay: TimeSpan.FromSeconds(10));
                return;
            }

            int index = 0;
            var deleteMessages = new List<IMessage>(count);
            var messages = Context.Channel.GetMessagesAsync();
            //suspend logging within the channel
            _banCache.Set(Context.Channel.Id, string.Empty, TimeSpan.FromSeconds(6));

            await messages.ForEachAsync(async m =>
            {
                IEnumerable<IMessage> delete = null;
                switch (deleteType)
                {
                    case DeleteType.User:
                        delete = m.Where(msg => !msg.Author.IsBot);
                        break;
                    case DeleteType.Bot:
                        delete = m.Where(msg => msg.Author.IsBot && msg.Author.Id != Context.Client.CurrentUser.Id);
                        break;
                    case DeleteType.All:
                        delete = m.Where(msg => msg.Author.Id != Context.Client.CurrentUser.Id);
                        break;
                }

                foreach (var msg in delete.OrderByDescending(msg => msg.Timestamp))
                {
                    if (index >= count)
                    {
                        try
                        {
                            await (Context.Channel as ITextChannel).DeleteMessagesAsync(deleteMessages);
                            return;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            return;
                        }
                        catch (HttpException)
                        {
                        }

                    }
                    deleteMessages.Add(msg);
                    index++;
                }
            });
        }
    }

    public enum DeleteType
    {
        User = 1,
        Bot = 2,
        All = 3
    }
}
