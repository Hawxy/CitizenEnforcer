#region License
/*CitizenEnforcer - Moderation and logging bot
Copyright(C) 2018 Hawx
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
using CitizenEnforcer.Preconditions;
using CitizenEnforcer.Services;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Microsoft.Extensions.Caching.Memory;

namespace CitizenEnforcer.Modules
{
    [RequireContext(ContextType.Guild)]
    [RequireInitialized(InitializedType.All)]
    public class SecurityModule : ModuleBase<SocketCommandContext>
    {
        public InteractiveService _interactive { get; set; }
        public IMemoryCache _banCache { get; set; }
        public ModerationService _moderationService { get; set; }

        [Command("lockdown")]
        [Alias("panic")]
        [Summary("Prevents non-role users from speaking server-wide")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task Lockdown()
        {
            var role = Context.Guild.EveryoneRole;
            if (role.Permissions.SendMessages)
            {
                var perms = role.Permissions.Modify(sendMessages: false);
                await role.ModifyAsync(x => x.Permissions = perms);

                var pubembed = SecurityFormats.GetPublicLockdownBuilder(Context.User);
                await _moderationService.SendEmbedToAnnounce(Context.Guild, pubembed);

                var logembed = SecurityFormats.GetLockdownBuilder(Context.User);
                await _moderationService.SendEmbedToModLog(Context.Guild, logembed);
            }
            else
            {
                var perms = role.Permissions.Modify(sendMessages: true);
                await role.ModifyAsync(x => x.Permissions = perms);

                var pubembed = SecurityFormats.GetPublicLiftedBuilder(Context.User);
                await _moderationService.SendEmbedToAnnounce(Context.Guild, pubembed);

                var logembed = SecurityFormats.GetLiftedBuilder(Context.User);
                await _moderationService.SendEmbedToModLog(Context.Guild, logembed);
            }
            
        }

        [Command("purge")]
        [Alias("clean")]
        [Summary("Used to clean up a channel")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        public async Task Purge (int count = 10, DeleteType deleteType = DeleteType.All)
        {
            if (count > 100)
            {
                await _interactive.ReplyAndDeleteAsync(Context, "Count should be less than 100", timeout: TimeSpan.FromSeconds(10));
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
                            await _interactive.ReplyAndDeleteAsync(Context, "Unable to continue deleting messages: Cannot delete messages older than 2 weeks", timeout:TimeSpan.FromSeconds(10));
                            return;
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
