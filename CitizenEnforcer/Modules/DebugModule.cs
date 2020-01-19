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

using Discord.Commands;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitizenEnforcer.Modules
{
    [Group("debug")]
    [RequireOwner]
    public class DebugModule : ModuleBase<SocketCommandContext>
    {
        [Command("listGuilds")]
        public async Task ListGuilds()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("```");
            foreach (var guild in Context.Client.Guilds)
            {
                builder.AppendLine($"n: {guild.Name}, i: {guild.Id} o: {guild.Owner} ({guild.OwnerId})");
            }

            builder.Append("```");

            await ReplyAsync(builder.ToString());
        }

        [Command("listUsers")]
        public async Task ListUsers(ulong guildId)
        {
            var guild = Context.Client.Guilds.FirstOrDefault(x => x.Id == guildId);
            if (guild == null)
            {
                await ReplyAsync("Unable to find guild");
                return;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("```");
            int count = 0;
            foreach (var user in guild.Users)
            {
                if (count == 30) break;
                count++;
                builder.AppendLine($"n: {user}, i: {user.Id}, r[0]: {user.Roles.FirstOrDefault(x => !x.IsEveryone)?.Name}");
            }

            builder.Append("```");

            await ReplyAsync("Retrieving first 30 users...");
            await ReplyAsync(builder.ToString());
        }
    }
}
