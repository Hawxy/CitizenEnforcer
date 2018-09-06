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
using CitizenEnforcer.Services;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Microsoft.Extensions.Caching.Memory;

namespace CitizenEnforcer.Modules
{
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    public class UtilitiesModule : ModuleBase<SocketCommandContext>
    {
        public InteractiveService _interactive { get; set; }
        public IMemoryCache _banCache { get; set; }

        [Command("purge")]
        [Alias("clean")]
        [Summary("Used to clean up a channel")]
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
